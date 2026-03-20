#!/bin/bash

# Setup Mullai.Web as a background service on macOS

set -e

USER_NAME=$(whoami)
INSTALL_DIR="$HOME/.mullai/service/web"
LOG_DIR="$HOME/.mullai/logs"
PLIST_NAME="com.mullai.web.plist"
PLIST_PATH="$HOME/Library/LaunchAgents/$PLIST_NAME"
PROJECT_DIR="$(pwd)/src/Mullai.Web.Wasm/Mullai.Web.Wasm"

echo "Building and Publishing Mullai.Web..."
mkdir -p "$INSTALL_DIR"
mkdir -p "$LOG_DIR"

dotnet publish "$PROJECT_DIR/Mullai.Web.Wasm.csproj" -c Release -o "$INSTALL_DIR"

echo "Creating LaunchAgent plist..."
cat <<EOF > "$PLIST_PATH"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.mullai.web</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/local/share/dotnet/dotnet</string>
        <string>$INSTALL_DIR/Mullai.Web.Wasm.dll</string>
    </array>
    <key>WorkingDirectory</key>
    <string>$INSTALL_DIR</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>$LOG_DIR/web.log</string>
    <key>StandardErrorPath</key>
    <string>$LOG_DIR/web.error.log</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>ASPNETCORE_ENVIRONMENT</key>
        <string>Development</string>
        <key>ASPNETCORE_URLS</key>
        <string>http://localhost:5024</string>
    </dict>
</dict>
</plist>
EOF

echo "Loading background service..."
launchctl unload "$PLIST_PATH" 2>/dev/null || true
launchctl load "$PLIST_PATH"

echo "Mullai.Web is now running in the background."
echo "You can check the logs at: $LOG_DIR/web.log"
echo "Web UI available at: http://localhost:5024"
