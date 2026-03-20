#!/bin/bash

# Stop and remove Mullai.Web background service on macOS

set -e

PLIST_NAME="com.mullai.web.plist"
PLIST_PATH="$HOME/Library/LaunchAgents/$PLIST_NAME"
INSTALL_DIR="$HOME/.mullai/service/web"
LOG_DIR="$HOME/.mullai/logs"

echo "Stopping background service..."
launchctl unload "$PLIST_PATH" 2>/dev/null || true

if [ -f "$PLIST_PATH" ]; then
    echo "Removing LaunchAgent plist..."
    rm "$PLIST_PATH"
fi

read -p "Do you also want to remove the installed files and logs in $HOME/.mullai? (y/N) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "Removing installed files and logs..."
    rm -rf "$INSTALL_DIR"
    rm -rf "$LOG_DIR"
    echo "Mullai.Web files and logs have been removed."
fi

echo "Mullai.Web service has been stopped and unregistered."
