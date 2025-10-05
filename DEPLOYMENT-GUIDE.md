# 🚀 AION Desktop Assistant - Deployment Guide

**Version**: 1.1.0
**Status**: Production Ready
**Last Updated**: 2025-10-05

---

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [Platform-Specific Installation](#platform-specific-installation)
3. [Docker Deployment](#docker-deployment)
4. [Build from Source](#build-from-source)
5. [Configuration](#configuration)
6. [Monitoring](#monitoring)
7. [Troubleshooting](#troubleshooting)

---

## 🎯 Quick Start

### Prerequisites
- **Windows**: Windows 10/11, .NET 8.0 Runtime
- **Linux**: Ubuntu 20.04+, systemd
- **macOS**: macOS 11+, Rosetta 2 (for Intel on Apple Silicon)
- **Docker**: Docker 20.10+, Docker Compose 2.0+

### Fastest Deployment (Docker)

```bash
# Clone repository
git clone https://github.com/Yatrogenesis/AION-Desktop-Assistant.git
cd AION-Desktop-Assistant

# Build and run
docker-compose up -d

# Check status
curl http://localhost:8080/api/status
```

---

## 💻 Platform-Specific Installation

### Windows x64 / ARM64

#### Option 1: Installer (Recommended)
```powershell
# Download latest release
$VERSION = "1.1.0"
$ARCH = "x64"  # or "arm64"
Invoke-WebRequest -Uri "https://github.com/Yatrogenesis/AION-Desktop-Assistant/releases/download/v$VERSION/AION-Setup-$ARCH.exe" -OutFile "AION-Setup.exe"

# Run installer
.\AION-Setup.exe
```

#### Option 2: Manual Installation
```powershell
# Download binary
Invoke-WebRequest -Uri "https://github.com/Yatrogenesis/AION-Desktop-Assistant/releases/download/v$VERSION/aion-server-windows-x64.exe" -OutFile "aion-server.exe"

# Run
.\aion-server.exe
```

#### Windows Service Installation
```powershell
# Using NSSM
nssm install AIONServer "C:\path\to\aion-server.exe"
nssm set AIONServer Description "AION Desktop Assistant Server"
nssm set AIONServer Start SERVICE_AUTO_START
nssm start AIONServer
```

---

### Linux x64

#### Option 1: Installer Script (Recommended)
```bash
# Download and run installer
sudo ./installers/install-linux-x64.sh

# Enable and start service
sudo systemctl enable aion-server
sudo systemctl start aion-server
sudo systemctl status aion-server
```

#### Option 2: Manual Installation
```bash
# Download binary
wget https://github.com/Yatrogenesis/AION-Desktop-Assistant/releases/download/v1.1.0/aion-server-linux-x64
chmod +x aion-server-linux-x64

# Run
./aion-server-linux-x64
```

#### systemd Service
```bash
# Create service file
sudo tee /etc/systemd/system/aion-server.service << EOF
[Unit]
Description=AION Desktop Assistant Server
After=network.target

[Service]
Type=simple
User=$USER
ExecStart=/opt/aion-desktop-assistant/aion-server
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

# Enable and start
sudo systemctl daemon-reload
sudo systemctl enable aion-server
sudo systemctl start aion-server
```

---

### macOS (Intel & Apple Silicon)

#### Option 1: Installer Script (Recommended)
```bash
# Download and run installer
sudo ./installers/install-macos-universal.sh

# Launch
open /Applications/AION.app
```

#### Option 2: Manual Installation
```bash
# Download appropriate binary
# Intel
wget https://github.com/Yatrogenesis/AION-Desktop-Assistant/releases/download/v1.1.0/aion-server-macos-x64

# Apple Silicon
wget https://github.com/Yatrogenesis/AION-Desktop-Assistant/releases/download/v1.1.0/aion-server-macos-arm64

# Make executable and run
chmod +x aion-server-*
./aion-server-*
```

#### LaunchAgent (Auto-start)
```bash
# Load service
launchctl load ~/Library/LaunchAgents/com.aion.desktop-assistant.plist

# Unload service
launchctl unload ~/Library/LaunchAgents/com.aion.desktop-assistant.plist
```

---

## 🐳 Docker Deployment

### Basic Deployment

```bash
# Build image
docker build -t aion-desktop-assistant:1.1.0 .

# Run container
docker run -d \
  --name aion-server \
  -p 8080:8080 \
  --restart unless-stopped \
  aion-desktop-assistant:1.1.0

# Check logs
docker logs aion-server

# Stop container
docker stop aion-server
```

### Docker Compose (Recommended)

#### Basic Setup
```bash
docker-compose up -d
```

#### With Monitoring Stack
```bash
docker-compose --profile monitoring up -d
```

Services:
- **AION Server**: http://localhost:8080
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)

#### With Reverse Proxy
```bash
docker-compose --profile proxy up -d
```

#### All Services
```bash
docker-compose --profile monitoring --profile proxy up -d
```

### Docker Health Check

```bash
docker inspect --format='{{.State.Health.Status}}' aion-server
```

---

## 🔨 Build from Source

### Prerequisites
- **Rust**: 1.75+
- **Cargo**: Latest
- **Python**: 3.9+ (for tests)
- **Make**: GNU Make (optional)

### Build with Make

```bash
# Install dependencies
make update-deps

# Build
make build

# Test
make test

# Install
make install
```

### Build with Cargo

```bash
cd aion-server-rust

# Development build
cargo build

# Production build (optimized)
cargo build --release

# Run tests
cargo test

# Run
cargo run --release
```

### Build Binary Size Optimization

Current optimizations (Cargo.toml):
```toml
[profile.release]
opt-level = 3           # Maximum optimization
lto = true             # Link-time optimization
codegen-units = 1      # Better optimization
strip = true           # Remove debug symbols
```

**Result**: 4.4 MB optimized binary

---

## ⚙️ Configuration

### Environment Variables

```bash
# Logging level
export RUST_LOG=info  # debug, info, warn, error

# Backtrace
export RUST_BACKTRACE=1

# Custom port (requires code modification)
# Default: 8080
```

### Operation Modes

Switch via API:
```bash
# Assistant mode (default)
curl -X POST http://localhost:8080/api/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": "assistant"}'

# Production mode
curl -X POST http://localhost:8080/api/mode \
  -H "Content-Type: application/json" \
  -d '{"mode": "production"}'
```

---

## 📊 Monitoring

### Prometheus Metrics

Endpoint: `http://localhost:9090` (if monitoring profile enabled)

Metrics to track:
- Request latency
- Success/error rates
- Active connections
- Memory usage
- CPU usage

### Grafana Dashboards

URL: `http://localhost:3000`
Credentials: admin/admin

Pre-configured dashboards:
- Server overview
- API performance
- Resource utilization

### Logs

#### Docker
```bash
docker logs -f aion-server
```

#### systemd (Linux)
```bash
journalctl -u aion-server -f
```

#### Windows
Check Windows Event Viewer

---

## 🔧 Troubleshooting

### Server Won't Start

**Port already in use**:
```bash
# Check port 8080
netstat -an | grep 8080

# Kill process (Linux/macOS)
lsof -ti:8080 | xargs kill -9

# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F
```

**Permission denied (Linux/macOS)**:
```bash
chmod +x aion-server
```

### API Not Responding

**Check if server is running**:
```bash
curl http://localhost:8080/api/status
```

**Check firewall** (Linux):
```bash
sudo ufw allow 8080/tcp
```

### Docker Issues

**Container won't start**:
```bash
docker logs aion-server
```

**Rebuild image**:
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

**Remove all containers**:
```bash
docker-compose down -v
```

### Performance Issues

**High CPU usage**:
- Switch to Production mode for better performance
- Check for infinite loops in automation scripts

**High memory usage**:
- Restart server
- Check for memory leaks in custom integrations

### Mouse/Keyboard Not Working

**Permissions** (Linux):
```bash
# Add user to input group
sudo usermod -a -G input $USER

# Install dependencies
sudo apt-get install libxtst6 libxi6
```

**Accessibility permissions** (macOS):
```
System Preferences > Security & Privacy > Accessibility
Enable AION Desktop Assistant
```

---

## 🔐 Security Hardening

### Production Deployment

1. **Network Isolation**:
   ```bash
   # Bind to localhost only (default)
   # For remote access, use reverse proxy with authentication
   ```

2. **API Authentication** (Recommended):
   - Implement API key authentication
   - Use HTTPS/TLS for remote access
   - Add rate limiting

3. **Firewall**:
   ```bash
   # Linux
   sudo ufw allow from 192.168.1.0/24 to any port 8080

   # Windows
   New-NetFirewallRule -DisplayName "AION Server" -Direction Inbound -LocalPort 8080 -Protocol TCP -Action Allow
   ```

4. **User Privileges**:
   - Run as non-root user (Linux/macOS)
   - Use standard user account (Windows)

---

## 📦 Package Distribution

### Docker Hub

```bash
# Tag and push
docker tag aion-desktop-assistant:1.1.0 youruser/aion-desktop-assistant:1.1.0
docker tag aion-desktop-assistant:1.1.0 youruser/aion-desktop-assistant:latest
docker push youruser/aion-desktop-assistant:1.1.0
docker push youruser/aion-desktop-assistant:latest
```

### GitHub Packages

```bash
# Login
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Tag and push
docker tag aion-desktop-assistant:1.1.0 ghcr.io/yatrogenesis/aion-desktop-assistant:1.1.0
docker push ghcr.io/yatrogenesis/aion-desktop-assistant:1.1.0
```

---

## 🎓 Next Steps

1. **Read API Documentation**: See [API-DOCUMENTATION.md](API-DOCUMENTATION.md)
2. **Run Tests**: See [TESTING.md](TESTING.md)
3. **Review Security**: See security section above
4. **Set up Monitoring**: Enable Prometheus/Grafana
5. **Configure Backups**: Set up automated backups

---

## 📚 Additional Resources

- [API Documentation](API-DOCUMENTATION.md)
- [Testing Guide](TESTING.md)
- [GitHub Repository](https://github.com/Yatrogenesis/AION-Desktop-Assistant)
- [Issue Tracker](https://github.com/Yatrogenesis/AION-Desktop-Assistant/issues)

---

**Generated with Claude Code** 🤖
