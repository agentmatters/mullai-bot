#!/bin/bash
set -e

INSTALL_DIR="$HOME/.mullai"

# Detect OS
OS_NAME=$(uname -s | tr '[:upper:]' '[:lower:]')
if [ "$OS_NAME" == "darwin" ]; then
    OS="macos"
elif [ "$OS_NAME" == "linux" ]; then
    OS="linux"
else
    echo "Unsupported OS: $OS_NAME"
    exit 1
fi

echo "Uninstalling Mullai..."

# Service Removal
if [ "$OS" == "linux" ]; then
    if command -v systemctl >/dev/null 2>&1; then
        echo "Removing systemd service..."
        sudo systemctl stop mullai-web 2>/dev/null || true
        sudo systemctl disable mullai-web 2>/dev/null || true
        sudo rm -f /etc/systemd/system/mullai-web.service
        sudo systemctl daemon-reload
    fi
elif [ "$OS" == "macos" ]; then
    echo "Removing launchd agent..."
    PLIST_FILE="$HOME/Library/LaunchAgents/com.mullai.bot.web.plist"
    if [ -f "$PLIST_FILE" ]; then
        launchctl unload "$PLIST_FILE" 2>/dev/null || true
        rm -f "$PLIST_FILE"
    fi
fi

# Remove files
if [ -d "$INSTALL_DIR" ]; then
    echo "Removing $INSTALL_DIR..."
    rm -rf "$INSTALL_DIR"
fi

echo "Mullai has been uninstalled."
echo "Note: You may want to manually remove the Mullai path from your shell configuration file (e.g., .zshrc or .bashrc)."
