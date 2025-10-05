// Library file for testing
pub mod api_types {
    use serde::{Deserialize, Serialize};

    #[derive(Debug, Clone, Copy, PartialEq)]
    pub enum OperationMode {
        Assistant,
        Production,
    }

    #[derive(Serialize, Deserialize, Debug, PartialEq)]
    pub struct ApiResponse<T> {
        pub success: bool,
        pub message: String,
        pub data: Option<T>,
        pub timestamp: i64,
    }

    impl<T> ApiResponse<T> {
        pub fn success(message: &str, data: Option<T>) -> Self {
            ApiResponse {
                success: true,
                message: message.to_string(),
                data,
                timestamp: chrono::Utc::now().timestamp(),
            }
        }

        pub fn error(message: &str) -> Self {
            ApiResponse {
                success: false,
                message: message.to_string(),
                data: None,
                timestamp: chrono::Utc::now().timestamp(),
            }
        }
    }

    #[derive(Serialize, Deserialize, Debug, PartialEq)]
    pub struct StatusData {
        pub status: String,
        pub port: u16,
        pub version: String,
        pub mode: String,
    }

    #[derive(Deserialize, Debug, PartialEq)]
    pub struct MouseMoveRequest {
        pub x: i32,
        pub y: i32,
    }

    #[derive(Deserialize, Debug, PartialEq)]
    pub struct MouseClickRequest {
        pub x: Option<i32>,
        pub y: Option<i32>,
        pub button: Option<String>,
    }

    #[derive(Deserialize, Debug, PartialEq)]
    pub struct KeyboardTypeRequest {
        pub text: String,
        pub interval: Option<u64>,
    }

    #[derive(Deserialize, Debug, PartialEq)]
    pub struct KeyboardPressRequest {
        pub key: String,
    }

    #[derive(Deserialize, Debug, PartialEq)]
    pub struct BrowserOpenRequest {
        pub url: String,
    }

    #[derive(Deserialize, Debug, PartialEq)]
    pub struct ModeChangeRequest {
        pub mode: String,
    }
}

pub mod mouse_algorithms {
    /// Calculate smooth mouse movement steps
    pub fn calculate_smooth_movement(
        current_x: i32,
        current_y: i32,
        target_x: i32,
        target_y: i32,
        steps: usize,
    ) -> Vec<(i32, i32)> {
        let dx = (target_x - current_x) as f64 / steps as f64;
        let dy = (target_y - current_y) as f64 / steps as f64;

        (0..steps)
            .map(|i| {
                let new_x = current_x + (dx * i as f64) as i32;
                let new_y = current_y + (dy * i as f64) as i32;
                (new_x, new_y)
            })
            .collect()
    }

    /// Validate coordinates are within screen bounds
    pub fn validate_coordinates(x: i32, y: i32, max_x: i32, max_y: i32) -> bool {
        x >= 0 && y >= 0 && x <= max_x && y <= max_y
    }
}

pub mod key_mapping {
    /// Validate if a key string is supported
    pub fn is_valid_key(key: &str) -> bool {
        matches!(
            key.to_lowercase().as_str(),
            "enter" | "return" | "tab" | "escape" | "esc" | "space"
            | "backspace" | "delete" | "del" | "up" | "down"
            | "left" | "right" | "home" | "end" | "pageup" | "pagedown"
        )
    }

    /// Map button string to enum
    pub fn parse_mouse_button(button: &str) -> Option<&str> {
        match button.to_lowercase().as_str() {
            "left" => Some("left"),
            "right" => Some("right"),
            "middle" => Some("middle"),
            _ => None,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    mod api_response_tests {
        use super::*;
        use crate::api_types::*;

        #[test]
        fn test_api_response_success() {
            let response: ApiResponse<String> = ApiResponse::success("Test message", Some("data".to_string()));
            assert!(response.success);
            assert_eq!(response.message, "Test message");
            assert_eq!(response.data, Some("data".to_string()));
            assert!(response.timestamp > 0);
        }

        #[test]
        fn test_api_response_success_no_data() {
            let response: ApiResponse<()> = ApiResponse::success("Test", None);
            assert!(response.success);
            assert_eq!(response.message, "Test");
            assert_eq!(response.data, None);
        }

        #[test]
        fn test_api_response_error() {
            let response: ApiResponse<()> = ApiResponse::error("Error message");
            assert!(!response.success);
            assert_eq!(response.message, "Error message");
            assert_eq!(response.data, None);
            assert!(response.timestamp > 0);
        }

        #[test]
        fn test_status_data_serialization() {
            let status = StatusData {
                status: "running".to_string(),
                port: 8080,
                version: "1.1.0".to_string(),
                mode: "assistant".to_string(),
            };

            let json = serde_json::to_string(&status).unwrap();
            assert!(json.contains("running"));
            assert!(json.contains("8080"));
            assert!(json.contains("1.1.0"));
            assert!(json.contains("assistant"));
        }

        #[test]
        fn test_mouse_move_request_deserialization() {
            let json = r#"{"x": 100, "y": 200}"#;
            let request: MouseMoveRequest = serde_json::from_str(json).unwrap();
            assert_eq!(request.x, 100);
            assert_eq!(request.y, 200);
        }

        #[test]
        fn test_mouse_click_request_with_coordinates() {
            let json = r#"{"x": 500, "y": 300, "button": "left"}"#;
            let request: MouseClickRequest = serde_json::from_str(json).unwrap();
            assert_eq!(request.x, Some(500));
            assert_eq!(request.y, Some(300));
            assert_eq!(request.button, Some("left".to_string()));
        }

        #[test]
        fn test_mouse_click_request_without_coordinates() {
            let json = r#"{"button": "right"}"#;
            let request: MouseClickRequest = serde_json::from_str(json).unwrap();
            assert_eq!(request.x, None);
            assert_eq!(request.y, None);
            assert_eq!(request.button, Some("right".to_string()));
        }

        #[test]
        fn test_keyboard_type_request() {
            let json = r#"{"text": "Hello World", "interval": 50}"#;
            let request: KeyboardTypeRequest = serde_json::from_str(json).unwrap();
            assert_eq!(request.text, "Hello World");
            assert_eq!(request.interval, Some(50));
        }

        #[test]
        fn test_keyboard_type_request_default_interval() {
            let json = r#"{"text": "Test"}"#;
            let request: KeyboardTypeRequest = serde_json::from_str(json).unwrap();
            assert_eq!(request.text, "Test");
            assert_eq!(request.interval, None);
        }

        #[test]
        fn test_mode_change_request() {
            let json = r#"{"mode": "production"}"#;
            let request: ModeChangeRequest = serde_json::from_str(json).unwrap();
            assert_eq!(request.mode, "production");
        }
    }

    mod mouse_algorithm_tests {
        use super::*;
        use crate::mouse_algorithms::*;

        #[test]
        fn test_smooth_movement_calculation() {
            let movements = calculate_smooth_movement(0, 0, 100, 100, 10);
            assert_eq!(movements.len(), 10);
            assert_eq!(movements[0], (0, 0));
            assert_eq!(movements[9], (90, 90));
        }

        #[test]
        fn test_smooth_movement_negative_direction() {
            let movements = calculate_smooth_movement(100, 100, 0, 0, 5);
            assert_eq!(movements.len(), 5);
            assert_eq!(movements[0], (100, 100));
            assert!(movements[4].0 < movements[0].0);
            assert!(movements[4].1 < movements[0].1);
        }

        #[test]
        fn test_smooth_movement_single_step() {
            let movements = calculate_smooth_movement(50, 50, 100, 100, 1);
            assert_eq!(movements.len(), 1);
            assert_eq!(movements[0], (50, 50));
        }

        #[test]
        fn test_validate_coordinates_valid() {
            assert!(validate_coordinates(500, 300, 1920, 1080));
            assert!(validate_coordinates(0, 0, 1920, 1080));
            assert!(validate_coordinates(1920, 1080, 1920, 1080));
        }

        #[test]
        fn test_validate_coordinates_invalid() {
            assert!(!validate_coordinates(-10, 300, 1920, 1080));
            assert!(!validate_coordinates(500, -10, 1920, 1080));
            assert!(!validate_coordinates(2000, 300, 1920, 1080));
            assert!(!validate_coordinates(500, 1200, 1920, 1080));
        }
    }

    mod key_mapping_tests {
        use super::*;
        use crate::key_mapping::*;

        #[test]
        fn test_valid_keys() {
            assert!(is_valid_key("enter"));
            assert!(is_valid_key("return"));
            assert!(is_valid_key("tab"));
            assert!(is_valid_key("escape"));
            assert!(is_valid_key("esc"));
            assert!(is_valid_key("space"));
            assert!(is_valid_key("backspace"));
            assert!(is_valid_key("delete"));
            assert!(is_valid_key("del"));
            assert!(is_valid_key("up"));
            assert!(is_valid_key("down"));
            assert!(is_valid_key("left"));
            assert!(is_valid_key("right"));
            assert!(is_valid_key("home"));
            assert!(is_valid_key("end"));
            assert!(is_valid_key("pageup"));
            assert!(is_valid_key("pagedown"));
        }

        #[test]
        fn test_valid_keys_case_insensitive() {
            assert!(is_valid_key("ENTER"));
            assert!(is_valid_key("Enter"));
            assert!(is_valid_key("TAB"));
            assert!(is_valid_key("EsCaPe"));
        }

        #[test]
        fn test_invalid_keys() {
            assert!(!is_valid_key("f1"));
            assert!(!is_valid_key("ctrl"));
            assert!(!is_valid_key("alt"));
            assert!(!is_valid_key("invalid"));
            assert!(!is_valid_key(""));
        }

        #[test]
        fn test_parse_mouse_button_valid() {
            assert_eq!(parse_mouse_button("left"), Some("left"));
            assert_eq!(parse_mouse_button("right"), Some("right"));
            assert_eq!(parse_mouse_button("middle"), Some("middle"));
        }

        #[test]
        fn test_parse_mouse_button_case_insensitive() {
            assert_eq!(parse_mouse_button("LEFT"), Some("left"));
            assert_eq!(parse_mouse_button("Right"), Some("right"));
            assert_eq!(parse_mouse_button("MIDDLE"), Some("middle"));
        }

        #[test]
        fn test_parse_mouse_button_invalid() {
            assert_eq!(parse_mouse_button("invalid"), None);
            assert_eq!(parse_mouse_button(""), None);
            assert_eq!(parse_mouse_button("button4"), None);
        }
    }

    mod operation_mode_tests {
        use super::*;
        use crate::api_types::OperationMode;

        #[test]
        fn test_operation_mode_equality() {
            assert_eq!(OperationMode::Assistant, OperationMode::Assistant);
            assert_eq!(OperationMode::Production, OperationMode::Production);
            assert_ne!(OperationMode::Assistant, OperationMode::Production);
        }

        #[test]
        fn test_operation_mode_clone() {
            let mode = OperationMode::Assistant;
            let cloned = mode;
            assert_eq!(mode, cloned);
        }
    }
}
