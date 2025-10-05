#!/bin/bash
# AION Desktop Assistant - macOS Universal Installer
# Version: 1.1.0
# Copyright (c) 2025 AION Technologies
# Supports: macOS 11+ (Intel & Apple Silicon)

set -e

VERSION="1.1.0"
APP_NAME="AION Desktop Assistant"
INSTALL_DIR="/Applications/AION.app"
BIN_DIR="/usr/local/bin"

echo "=========================================="
echo "$APP_NAME v$VERSION"
echo "macOS Universal Installer"
echo "=========================================="
echo ""

# Detect architecture
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    echo "🍎 Detected: Apple Silicon (M1/M2/M3)"
    BINARY="aion-server-arm64"
elif [ "$ARCH" = "x86_64" ]; then
    echo "💻 Detected: Intel x64"
    BINARY="aion-server-x64"
else
    echo "❌ Unsupported architecture: $ARCH"
    exit 1
fi

echo ""
echo "📦 Installing $APP_NAME..."
echo ""

# Create app bundle structure
echo "📁 Creating application bundle..."
mkdir -p "$INSTALL_DIR/Contents/MacOS"
mkdir -p "$INSTALL_DIR/Contents/Resources"

# Copy binary
echo "📋 Copying application files..."
if [ -f "./$BINARY" ]; then
    cp "./$BINARY" "$INSTALL_DIR/Contents/MacOS/aion-server"
    chmod +x "$INSTALL_DIR/Contents/MacOS/aion-server"
elif [ -f "./aion-server" ]; then
    cp "./aion-server" "$INSTALL_DIR/Contents/MacOS/aion-server"
    chmod +x "$INSTALL_DIR/Contents/MacOS/aion-server"
else
    echo "❌ Error: aion-server binary not found!"
    exit 1
fi

# Create Info.plist
echo "⚙️  Creating application metadata..."
cat > "$INSTALL_DIR/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>aion-server</string>
    <key>CFBundleIdentifier</key>
    <string>com.aion.desktop-assistant</string>
    <key>CFBundleName</key>
    <string>AION Desktop Assistant</string>
    <key>CFBundleDisplayName</key>
    <string>AION Desktop Assistant</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>AION</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2025 AION Technologies</string>
</dict>
</plist>
EOF

# Create symlink in /usr/local/bin
echo "🔗 Creating command-line symlink..."
sudo mkdir -p "$BIN_DIR"
sudo ln -sf "$INSTALL_DIR/Contents/MacOS/aion-server" "$BIN_DIR/aion-server"

# Create LaunchAgent for auto-start (optional)
LAUNCH_AGENT_DIR="$HOME/Library/LaunchAgents"
LAUNCH_AGENT_FILE="$LAUNCH_AGENT_DIR/com.aion.desktop-assistant.plist"

echo "🚀 Creating LaunchAgent..."
mkdir -p "$LAUNCH_AGENT_DIR"

cat > "$LAUNCH_AGENT_FILE" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.aion.desktop-assistant</string>
    <key>ProgramArguments</key>
    <array>
        <string>$INSTALL_DIR/Contents/MacOS/aion-server</string>
    </array>
    <key>RunAtLoad</key>
    <false/>
    <key>KeepAlive</key>
    <false/>
    <key>StandardOutPath</key>
    <string>/tmp/aion-server.log</string>
    <key>StandardErrorPath</key>
    <string>/tmp/aion-server.error.log</string>
</dict>
</plist>
EOF

# Create uninstaller
echo "🗑️  Creating uninstaller..."
cat > "$INSTALL_DIR/Contents/Resources/uninstall.sh" << 'EOFUNINSTALL'
#!/bin/bash
echo "Uninstalling AION Desktop Assistant..."
launchctl unload "$HOME/Library/LaunchAgents/com.aion.desktop-assistant.plist" 2>/dev/null
rm -f "$HOME/Library/LaunchAgents/com.aion.desktop-assistant.plist"
sudo rm -f /usr/local/bin/aion-server
sudo rm -rf /Applications/AION.app
echo "✅ Uninstallation complete!"
EOFUNINSTALL

chmod +x "$INSTALL_DIR/Contents/Resources/uninstall.sh"

echo ""
echo "=========================================="
echo "✅ Installation Complete!"
echo "=========================================="
echo ""
echo "Quick Start:"
echo "  • Run from Applications folder"
echo "  • Or run in terminal: aion-server"
echo ""
echo "Auto-start (optional):"
echo "  • Load service: launchctl load $LAUNCH_AGENT_FILE"
echo "  • Unload service: launchctl unload $LAUNCH_AGENT_FILE"
echo ""
echo "Uninstall:"
echo "  • Run: $INSTALL_DIR/Contents/Resources/uninstall.sh"
echo ""
echo "Server will be available at: http://localhost:8080"
echo ""
echo "Documentation: https://github.com/Yatrogenesis/AION-Desktop-Assistant"
echo ""
