#!/bin/bash
# AION Desktop Assistant - Linux x64 Installer
# Version: 1.1.0
# Copyright (c) 2025 AION Technologies

set -e

VERSION="1.1.0"
APP_NAME="AION Desktop Assistant"
INSTALL_DIR="/opt/aion-desktop-assistant"
BIN_DIR="/usr/local/bin"
DESKTOP_DIR="/usr/share/applications"

echo "=========================================="
echo "$APP_NAME v$VERSION"
echo "Linux x64 Installer"
echo "=========================================="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "⚠️  This installer requires root privileges."
    echo "   Please run: sudo $0"
    exit 1
fi

echo "📦 Installing $APP_NAME..."
echo ""

# Create installation directory
echo "📁 Creating installation directory..."
mkdir -p "$INSTALL_DIR"
mkdir -p "$BIN_DIR"
mkdir -p "$DESKTOP_DIR"

# Copy binary
echo "📋 Copying application files..."
if [ -f "./aion-server" ]; then
    cp ./aion-server "$INSTALL_DIR/"
    chmod +x "$INSTALL_DIR/aion-server"
else
    echo "❌ Error: aion-server binary not found!"
    exit 1
fi

# Create symlink
echo "🔗 Creating symbolic link..."
ln -sf "$INSTALL_DIR/aion-server" "$BIN_DIR/aion-server"

# Create desktop entry
echo "🖥️  Creating desktop entry..."
cat > "$DESKTOP_DIR/aion-desktop-assistant.desktop" << EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=$APP_NAME
Comment=AI-Powered Desktop Accessibility Assistant
Exec=$BIN_DIR/aion-server
Icon=$INSTALL_DIR/icon.png
Terminal=false
Categories=Utility;Accessibility;
Keywords=accessibility;voice;automation;assistant;
EOF

chmod +x "$DESKTOP_DIR/aion-desktop-assistant.desktop"

# Create uninstaller
echo "🗑️  Creating uninstaller..."
cat > "$INSTALL_DIR/uninstall.sh" << 'EOF'
#!/bin/bash
echo "Uninstalling AION Desktop Assistant..."
rm -f /usr/local/bin/aion-server
rm -f /usr/share/applications/aion-desktop-assistant.desktop
rm -rf /opt/aion-desktop-assistant
echo "✅ Uninstallation complete!"
EOF

chmod +x "$INSTALL_DIR/uninstall.sh"

# Create systemd service (optional)
echo "⚙️  Creating systemd service..."
cat > /etc/systemd/system/aion-server.service << EOF
[Unit]
Description=AION Desktop Assistant Server
After=network.target

[Service]
Type=simple
User=$SUDO_USER
ExecStart=$INSTALL_DIR/aion-server
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload

echo ""
echo "=========================================="
echo "✅ Installation Complete!"
echo "=========================================="
echo ""
echo "Quick Start:"
echo "  • Run manually: aion-server"
echo "  • Enable service: sudo systemctl enable aion-server"
echo "  • Start service: sudo systemctl start aion-server"
echo "  • Check status: sudo systemctl status aion-server"
echo ""
echo "Uninstall:"
echo "  • Run: sudo $INSTALL_DIR/uninstall.sh"
echo ""
echo "Server will be available at: http://localhost:8080"
echo ""
echo "Documentation: https://github.com/Yatrogenesis/AION-Desktop-Assistant"
echo ""
