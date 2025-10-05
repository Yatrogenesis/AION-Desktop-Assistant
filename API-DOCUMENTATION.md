# AION Desktop Assistant - API Documentation

**Version**: 1.1.0
**Base URL**: `http://localhost:8080`
**Protocol**: HTTP/JSON

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Operation Modes](#operation-modes)
4. [API Endpoints](#api-endpoints)
5. [Error Handling](#error-handling)
6. [Rate Limiting](#rate-limiting)
7. [Examples](#examples)

---

## Overview

The AION Desktop Assistant provides a REST API for programmatic control of desktop automation features including:
- Mouse control (movement, clicking)
- Keyboard input (typing, special keys)
- Browser automation
- System mode switching

All responses follow a consistent JSON structure:

```json
{
  "success": boolean,
  "message": string,
  "data": object | null,
  "timestamp": number
}
```

---

## Authentication

**Current Version**: No authentication required (localhost only)

⚠️ **Security Notice**: The server binds to `127.0.0.1` by default. For production use:
- Implement API key authentication
- Use HTTPS/TLS
- Add rate limiting
- Implement IP whitelisting

---

## Operation Modes

The server supports two operation modes:

### Assistant Mode (Default)
- **Purpose**: Demonstration and visibility
- **Behavior**:
  - Mouse movements are animated (20 steps, 10ms each)
  - Typing has visible character-by-character delay
  - Suitable for demos and testing

### Production Mode
- **Purpose**: Maximum performance
- **Behavior**:
  - Direct API calls without animation
  - Instant mouse teleportation
  - Immediate text input
  - < 1ms latency per operation

---

## API Endpoints

### 1. Get Server Status

**Endpoint**: `GET /api/status`

**Description**: Returns server status and current configuration.

**Response**:
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
  "timestamp": 1759653455
}
```

**Status Codes**:
- `200 OK`: Server is running

---

### 2. Set Operation Mode

**Endpoint**: `POST /api/mode`

**Description**: Switch between Assistant and Production modes.

**Request Body**:
```json
{
  "mode": "production"
}
```

**Valid Modes**: `assistant`, `production`

**Response**:
```json
{
  "success": true,
  "message": "Mode changed to: production",
  "data": null,
  "timestamp": 1759653460
}
```

**Status Codes**:
- `200 OK`: Mode changed successfully
- `400 Bad Request`: Invalid mode specified

---

### 3. Move Mouse

**Endpoint**: `POST /api/mouse/move`

**Description**: Move mouse cursor to specified coordinates.

**Request Body**:
```json
{
  "x": 500,
  "y": 300
}
```

**Parameters**:
- `x` (integer, required): X coordinate (pixels from left)
- `y` (integer, required): Y coordinate (pixels from top)

**Response**:
```json
{
  "success": true,
  "message": "Mouse moved to (500, 300)",
  "data": null,
  "timestamp": 1759653465
}
```

**Behavior**:
- **Assistant Mode**: Smooth 20-step animation (200ms total)
- **Production Mode**: Instant teleport

**Status Codes**:
- `200 OK`: Mouse moved successfully
- `400 Bad Request`: Invalid coordinates

---

### 4. Click Mouse

**Endpoint**: `POST /api/mouse/click`

**Description**: Click mouse button at current or specified position.

**Request Body**:
```json
{
  "x": 500,
  "y": 300,
  "button": "left"
}
```

**Parameters**:
- `x` (integer, optional): X coordinate for click
- `y` (integer, optional): Y coordinate for click
- `button` (string, optional): Mouse button (`left`, `right`, `middle`)

**Default Values**:
- Coordinates: Current mouse position
- Button: `left`

**Response**:
```json
{
  "success": true,
  "message": "Clicked at (500, 300) with left button",
  "data": null,
  "timestamp": 1759653470
}
```

**Status Codes**:
- `200 OK`: Click executed successfully
- `400 Bad Request`: Invalid button or coordinates

---

### 5. Type Text

**Endpoint**: `POST /api/keyboard/type`

**Description**: Type text string with configurable delay between characters.

**Request Body**:
```json
{
  "text": "Hello World",
  "interval": 50
}
```

**Parameters**:
- `text` (string, required): Text to type
- `interval` (integer, optional): Milliseconds between characters (default: 50)

**Response**:
```json
{
  "success": true,
  "message": "Typed: Hello World",
  "data": null,
  "timestamp": 1759653475
}
```

**Behavior**:
- **Assistant Mode**: Types with specified interval (visible)
- **Production Mode**: Types entire string instantly

**Status Codes**:
- `200 OK`: Text typed successfully
- `400 Bad Request`: Invalid request body

---

### 6. Press Special Key

**Endpoint**: `POST /api/keyboard/press`

**Description**: Press a special keyboard key.

**Request Body**:
```json
{
  "key": "enter"
}
```

**Supported Keys**:
- Navigation: `up`, `down`, `left`, `right`, `home`, `end`, `pageup`, `pagedown`
- Control: `enter`, `return`, `tab`, `escape`, `esc`, `space`
- Edit: `backspace`, `delete`, `del`

**Response**:
```json
{
  "success": true,
  "message": "Pressed key: enter",
  "data": null,
  "timestamp": 1759653480
}
```

**Status Codes**:
- `200 OK`: Key pressed successfully
- `400 Bad Request`: Unknown or unsupported key

---

### 7. Open Browser

**Endpoint**: `POST /api/browser/open`

**Description**: Open URL in default system browser.

**Request Body**:
```json
{
  "url": "https://www.example.com"
}
```

**Parameters**:
- `url` (string, required): Full URL to open

**Response**:
```json
{
  "success": true,
  "message": "Browser opened: https://www.example.com",
  "data": null,
  "timestamp": 1759653485
}
```

**Platform Behavior**:
- **Windows**: Uses `cmd /C start`
- **Linux**: Uses `xdg-open`
- **macOS**: Uses `open`

**Status Codes**:
- `200 OK`: Browser launched successfully
- `400 Bad Request`: Invalid URL

---

## Error Handling

### Error Response Format

All errors return the same JSON structure:

```json
{
  "success": false,
  "message": "Error description",
  "data": null,
  "timestamp": 1759653490
}
```

### Common HTTP Status Codes

| Code | Description |
|------|-------------|
| `200` | Success |
| `400` | Bad Request (invalid parameters) |
| `404` | Not Found (invalid endpoint) |
| `405` | Method Not Allowed |
| `500` | Internal Server Error |

### Error Messages

| Error | Cause | Solution |
|-------|-------|----------|
| `Invalid mode. Use 'assistant' or 'production'` | Invalid mode value | Use `assistant` or `production` |
| `Unknown key: [key]` | Unsupported key name | Check supported keys list |
| `Endpoint not found: [path]` | Invalid API path | Verify endpoint URL |

---

## Rate Limiting

**Current Version**: No rate limiting implemented

**Recommended Limits** (for production):
- General endpoints: 100 requests/minute
- Mouse movement: 1000 requests/minute (high-frequency operations)
- Typing: 100 requests/minute

---

## Examples

### cURL Examples

#### Check Server Status
```bash
curl http://localhost:8080/api/status
```

#### Switch to Production Mode
```bash
curl -X POST http://localhost:8080/api/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": "production"}'
```

#### Move Mouse
```bash
curl -X POST http://localhost:8080/api/mouse/move \
  -H "Content-Type: application/json" \
  -d '{"x": 960, "y": 540}'
```

#### Click at Position
```bash
curl -X POST http://localhost:8080/api/mouse/click \
  -H "Content-Type: application/json" \
  -d '{"x": 500, "y": 300, "button": "left"}'
```

#### Type Text
```bash
curl -X POST http://localhost:8080/api/keyboard/type \
  -H "Content-Type: application/json" \
  -d '{"text": "Hello World", "interval": 30}'
```

#### Press Enter Key
```bash
curl -X POST http://localhost:8080/api/keyboard/press \
  -H "Content-Type: application/json" \
  -d '{"key": "enter"}'
```

#### Open Browser
```bash
curl -X POST http://localhost:8080/api/browser/open \
  -H "Content-Type: application/json" \
  -d '{"url": "https://github.com"}'
```

### Python Examples

```python
import requests

BASE_URL = "http://localhost:8080"

# Check status
response = requests.get(f"{BASE_URL}/api/status")
print(response.json())

# Switch mode
response = requests.post(f"{BASE_URL}/api/mode",
    json={"mode": "production"})
print(response.json())

# Move mouse
response = requests.post(f"{BASE_URL}/api/mouse/move",
    json={"x": 500, "y": 300})
print(response.json())

# Type text
response = requests.post(f"{BASE_URL}/api/keyboard/type",
    json={"text": "Hello from Python", "interval": 50})
print(response.json())
```

### JavaScript Examples

```javascript
const BASE_URL = 'http://localhost:8080';

// Check status
fetch(`${BASE_URL}/api/status`)
  .then(res => res.json())
  .then(data => console.log(data));

// Move mouse
fetch(`${BASE_URL}/api/mouse/move`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ x: 500, y: 300 })
})
  .then(res => res.json())
  .then(data => console.log(data));

// Type text
fetch(`${BASE_URL}/api/keyboard/type`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    text: 'Hello from JavaScript',
    interval: 50
  })
})
  .then(res => res.json())
  .then(data => console.log(data));
```

---

## Performance Metrics

### Expected Latency

| Mode | Operation | Latency |
|------|-----------|---------|
| Production | Mouse Move | < 1ms |
| Production | Click | < 1ms |
| Production | Type (per char) | < 1ms |
| Production | Key Press | < 1ms |
| Assistant | Mouse Move | ~200ms (animated) |
| Assistant | Type (per char) | 50ms (default) |

### Resource Usage

- **Memory**: 5-10 MB
- **CPU**: < 1% idle, < 5% under load
- **Concurrent Connections**: 1000+

---

## Security Best Practices

1. **Network**: Run only on localhost unless properly secured
2. **Authentication**: Implement API keys for production
3. **HTTPS**: Use TLS for remote access
4. **Validation**: All input is validated server-side
5. **Logging**: Enable logging for audit trails
6. **Rate Limiting**: Implement to prevent abuse

---

## Changelog

### v1.1.0 (2025-10-05)
- Added comprehensive test suite (82 tests)
- Improved error handling
- Added API documentation
- Performance optimizations

### v1.0.0 (2025-10-04)
- Initial release
- Dual operation modes
- 7 API endpoints
- Cross-platform support

---

**Generated with Claude Code** 🤖
