# AION Desktop Assistant - Rust Server

High-performance HTTP control server for desktop automation.

## Features

- **Dual Operation Modes**:
  - **Assistant Mode**: Smooth, visible cursor movements and typing for demonstration
  - **Production Mode**: Direct API calls without animation for maximum speed

- **High Performance**:
  - Built with Rust for minimal latency
  - Async HTTP server with Actix-web
  - Thread-safe operation handling

- **Cross-Platform**:
  - Windows (native)
  - Linux (X11/Wayland)
  - macOS

## Build

```bash
cargo build --release
```

Binary location: `target/release/aion-server.exe` (Windows) or `target/release/aion-server` (Unix)

## Run

```bash
cargo run --release
```

Or run the compiled binary directly:

```bash
./target/release/aion-server
```

## API Endpoints

### GET /api/status
Get server status and current mode

Response:
```json
{
  "success": true,
  "message": "AION Server Online",
  "data": {
    "status": "running",
    "port": 8080,
    "version": "1.1.0",
    "mode": "assistant"
  },
  "timestamp": 1759620000
}
```

### POST /api/mode
Set operation mode

Request:
```json
{
  "mode": "production"
}
```

Valid modes: `assistant`, `production`

### POST /api/mouse/move
Move mouse cursor

Request:
```json
{
  "x": 500,
  "y": 300
}
```

- **Assistant mode**: Smooth animated movement (20 steps, 10ms each)
- **Production mode**: Instant teleport

### POST /api/mouse/click
Click mouse button

Request:
```json
{
  "x": 500,
  "y": 300,
  "button": "left"
}
```

Valid buttons: `left`, `right`, `middle`
Coordinates are optional (clicks at current position if omitted)

### POST /api/keyboard/type
Type text

Request:
```json
{
  "text": "Hello World",
  "interval": 50
}
```

- **Assistant mode**: Types with visible interval between characters
- **Production mode**: Types entire string instantly
- Interval in milliseconds (default: 50ms)

### POST /api/keyboard/press
Press special key

Request:
```json
{
  "key": "enter"
}
```

Supported keys: `enter`, `tab`, `escape`, `space`, `backspace`, `delete`, `up`, `down`, `left`, `right`, `home`, `end`, `pageup`, `pagedown`

### POST /api/browser/open
Open URL in default browser

Request:
```json
{
  "url": "https://www.example.com"
}
```

## Usage Examples

### Change to Production Mode
```bash
curl -X POST http://localhost:8080/api/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": "production"}'
```

### Move Mouse (Smooth in Assistant Mode)
```bash
curl -X POST http://localhost:8080/api/mouse/move \
  -H "Content-Type: application/json" \
  -d '{"x": 960, "y": 540}'
```

### Type Text
```bash
curl -X POST http://localhost:8080/api/keyboard/type \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello from AION", "interval": 30}'
```

### Open Browser
```bash
curl -X POST http://localhost:8080/api/browser/open \
  -H "Content-Type: application/json" \
  -d '{"url": "https://youtube.com"}'
```

## Production Deployment

### Build Optimized Binary
```bash
cargo build --release
strip target/release/aion-server  # Remove debug symbols (Unix)
```

Binary size: ~5-8 MB

### Run as Service

#### Windows (NSSM)
```powershell
nssm install AIONServer "C:\path\to\aion-server.exe"
nssm start AIONServer
```

#### Linux (systemd)
```ini
[Unit]
Description=AION Desktop Assistant Server
After=network.target

[Service]
Type=simple
User=aion
ExecStart=/usr/local/bin/aion-server
Restart=always

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable aion-server
sudo systemctl start aion-server
```

## Performance Metrics

- Latency: < 1ms per operation
- Memory: ~5-10 MB
- CPU: < 1% idle, < 5% under load
- Concurrent connections: 1000+

## Security Notes

**WARNING**: This server provides full desktop control. Only run on:
- Local machine (127.0.0.1)
- Trusted networks
- Behind authentication/firewall

For production use, implement:
- API key authentication
- HTTPS/TLS
- Rate limiting
- IP whitelist

## License

Proprietary - AION Technologies 2025
