use actix_web::{web, App, HttpResponse, HttpServer, Responder};
use serde::{Deserialize, Serialize};
use enigo::{Enigo, Mouse, Settings, Coordinate, Button, Direction, Keyboard, Key};
use std::sync::Mutex;
use std::time::Duration;
use chrono::Utc;

#[derive(Debug, Clone, Copy)]
enum OperationMode {
    Assistant,   // Movimientos suaves y visibles
    Production,  // Llamadas directas sin animación
}

struct AppState {
    enigo: Mutex<Enigo>,
    mode: Mutex<OperationMode>,
}

#[derive(Serialize)]
struct ApiResponse<T> {
    success: bool,
    message: String,
    data: Option<T>,
    timestamp: i64,
}

impl<T> ApiResponse<T> {
    fn success(message: &str, data: Option<T>) -> Self {
        ApiResponse {
            success: true,
            message: message.to_string(),
            data,
            timestamp: Utc::now().timestamp(),
        }
    }

    fn error(message: &str) -> Self {
        ApiResponse {
            success: false,
            message: message.to_string(),
            data: None,
            timestamp: Utc::now().timestamp(),
        }
    }
}

#[derive(Serialize)]
struct StatusData {
    status: String,
    port: u16,
    version: String,
    mode: String,
}

#[derive(Deserialize)]
struct MouseMoveRequest {
    x: i32,
    y: i32,
}

#[derive(Deserialize)]
struct MouseClickRequest {
    x: Option<i32>,
    y: Option<i32>,
    button: Option<String>,
}

#[derive(Deserialize)]
struct KeyboardTypeRequest {
    text: String,
    interval: Option<u64>,
}

#[derive(Deserialize)]
struct KeyboardPressRequest {
    key: String,
}

#[derive(Deserialize)]
struct BrowserOpenRequest {
    url: String,
}

#[derive(Deserialize)]
struct ModeChangeRequest {
    mode: String,
}

async fn get_status(data: web::Data<AppState>) -> impl Responder {
    let mode = *data.mode.lock().unwrap();
    let mode_str = match mode {
        OperationMode::Assistant => "assistant",
        OperationMode::Production => "production",
    };

    let response = ApiResponse::success(
        "AION Server Online",
        Some(StatusData {
            status: "running".to_string(),
            port: 8080,
            version: "1.1.0".to_string(),
            mode: mode_str.to_string(),
        }),
    );
    HttpResponse::Ok().json(response)
}

async fn move_mouse(
    req: web::Json<MouseMoveRequest>,
    data: web::Data<AppState>,
) -> impl Responder {
    let mode = *data.mode.lock().unwrap();
    let mut enigo = data.enigo.lock().unwrap();

    match mode {
        OperationMode::Assistant => {
            // Movimiento suave con animación
            if let Ok(current_pos) = enigo.location() {
                let steps = 20;
                let dx = (req.x - current_pos.0) as f64 / steps as f64;
                let dy = (req.y - current_pos.1) as f64 / steps as f64;

                for i in 0..steps {
                    let new_x = current_pos.0 + (dx * i as f64) as i32;
                    let new_y = current_pos.1 + (dy * i as f64) as i32;
                    let _ = enigo.move_mouse(new_x, new_y, Coordinate::Abs);
                    std::thread::sleep(Duration::from_millis(10));
                }
            }
            let _ = enigo.move_mouse(req.x, req.y, Coordinate::Abs);
        }
        OperationMode::Production => {
            // Movimiento directo sin animación
            let _ = enigo.move_mouse(req.x, req.y, Coordinate::Abs);
        }
    }

    let response: ApiResponse<()> = ApiResponse::success(
        &format!("Mouse moved to ({}, {})", req.x, req.y),
        None,
    );
    HttpResponse::Ok().json(response)
}

async fn click_mouse(
    req: web::Json<MouseClickRequest>,
    data: web::Data<AppState>,
) -> impl Responder {
    let mut enigo = data.enigo.lock().unwrap();

    // Mover si se especifican coordenadas
    if let (Some(x), Some(y)) = (req.x, req.y) {
        let _ = enigo.move_mouse(x, y, Coordinate::Abs);
        std::thread::sleep(Duration::from_millis(50));
    }

    let button = match req.button.as_deref() {
        Some("right") => Button::Right,
        Some("middle") => Button::Middle,
        _ => Button::Left,
    };

    let _ = enigo.button(button, Direction::Click);

    let msg = if let (Some(x), Some(y)) = (req.x, req.y) {
        format!("Clicked at ({}, {}) with {:?} button", x, y, button)
    } else {
        format!("Clicked with {:?} button", button)
    };

    let response: ApiResponse<()> = ApiResponse::success(&msg, None);
    HttpResponse::Ok().json(response)
}

async fn type_text(
    req: web::Json<KeyboardTypeRequest>,
    data: web::Data<AppState>,
) -> impl Responder {
    let mode = *data.mode.lock().unwrap();
    let mut enigo = data.enigo.lock().unwrap();
    let interval = req.interval.unwrap_or(50);

    match mode {
        OperationMode::Assistant => {
            // Tipeo con intervalo visible
            for ch in req.text.chars() {
                let _ = enigo.text(&ch.to_string());
                std::thread::sleep(Duration::from_millis(interval));
            }
        }
        OperationMode::Production => {
            // Tipeo directo
            let _ = enigo.text(&req.text);
        }
    }

    let response: ApiResponse<()> = ApiResponse::success(
        &format!("Typed: {}", req.text),
        None,
    );
    HttpResponse::Ok().json(response)
}

async fn press_key(
    req: web::Json<KeyboardPressRequest>,
    data: web::Data<AppState>,
) -> impl Responder {
    let mut enigo = data.enigo.lock().unwrap();

    let key = match req.key.to_lowercase().as_str() {
        "enter" | "return" => Key::Return,
        "tab" => Key::Tab,
        "escape" | "esc" => Key::Escape,
        "space" => Key::Space,
        "backspace" => Key::Backspace,
        "delete" | "del" => Key::Delete,
        "up" => Key::UpArrow,
        "down" => Key::DownArrow,
        "left" => Key::LeftArrow,
        "right" => Key::RightArrow,
        "home" => Key::Home,
        "end" => Key::End,
        "pageup" => Key::PageUp,
        "pagedown" => Key::PageDown,
        _ => {
            let response: ApiResponse<()> = ApiResponse::error(&format!("Unknown key: {}", req.key));
            return HttpResponse::BadRequest().json(response);
        }
    };

    let _ = enigo.key(key, Direction::Click);

    let response: ApiResponse<()> = ApiResponse::success(
        &format!("Pressed key: {}", req.key),
        None,
    );
    HttpResponse::Ok().json(response)
}

async fn open_browser(req: web::Json<BrowserOpenRequest>) -> impl Responder {
    #[cfg(target_os = "windows")]
    {
        let _ = std::process::Command::new("cmd")
            .args(&["/C", "start", &req.url])
            .spawn();
    }

    #[cfg(target_os = "linux")]
    {
        let _ = std::process::Command::new("xdg-open")
            .arg(&req.url)
            .spawn();
    }

    #[cfg(target_os = "macos")]
    {
        let _ = std::process::Command::new("open")
            .arg(&req.url)
            .spawn();
    }

    let response: ApiResponse<()> = ApiResponse::success(
        &format!("Browser opened: {}", req.url),
        None,
    );
    HttpResponse::Ok().json(response)
}

async fn set_mode(
    req: web::Json<ModeChangeRequest>,
    data: web::Data<AppState>,
) -> impl Responder {
    let new_mode = match req.mode.to_lowercase().as_str() {
        "assistant" => OperationMode::Assistant,
        "production" => OperationMode::Production,
        _ => {
            let response: ApiResponse<()> = ApiResponse::error("Invalid mode. Use 'assistant' or 'production'");
            return HttpResponse::BadRequest().json(response);
        }
    };

    *data.mode.lock().unwrap() = new_mode;

    let response: ApiResponse<()> = ApiResponse::success(
        &format!("Mode changed to: {}", req.mode),
        None,
    );
    HttpResponse::Ok().json(response)
}

#[actix_web::main]
async fn main() -> std::io::Result<()> {
    env_logger::init_from_env(env_logger::Env::new().default_filter_or("info"));

    let settings = Settings::default();
    let enigo = Enigo::new(&settings).expect("Failed to initialize Enigo");

    let app_state = web::Data::new(AppState {
        enigo: Mutex::new(enigo),
        mode: Mutex::new(OperationMode::Assistant), // Default: Assistant mode
    });

    println!("================================================================================");
    println!("AION Desktop Assistant - Rust HTTP Control Server");
    println!("================================================================================");
    println!("Server started on http://localhost:8080");
    println!("Mode: Assistant (smooth movements)");
    println!();
    println!("API Endpoints:");
    println!("  GET  /api/status           - Check server status");
    println!("  POST /api/mode             - Set mode (assistant/production)");
    println!("  POST /api/mouse/move       - Move mouse (x, y)");
    println!("  POST /api/mouse/click      - Click mouse (x, y, button)");
    println!("  POST /api/keyboard/type    - Type text (text, interval)");
    println!("  POST /api/keyboard/press   - Press key (key)");
    println!("  POST /api/browser/open     - Open URL (url)");
    println!("================================================================================");
    println!("Ready to accept connections...");
    println!("================================================================================");

    HttpServer::new(move || {
        App::new()
            .app_data(app_state.clone())
            .route("/api/status", web::get().to(get_status))
            .route("/api/mode", web::post().to(set_mode))
            .route("/api/mouse/move", web::post().to(move_mouse))
            .route("/api/mouse/click", web::post().to(click_mouse))
            .route("/api/keyboard/type", web::post().to(type_text))
            .route("/api/keyboard/press", web::post().to(press_key))
            .route("/api/browser/open", web::post().to(open_browser))
    })
    .bind(("127.0.0.1", 8080))?
    .run()
    .await
}
