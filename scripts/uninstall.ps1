$ErrorActionPreference = "Continue"

$INSTALL_DIR = Join-Path $HOME ".mullai"
$SERVICE_NAME = "MullaiWeb"

Write-Host "Uninstalling Mullai..." -ForegroundColor Cyan

# Service Removal
if (Get-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue) {
    Write-Host "Stopping and removing Windows service..." -ForegroundColor Gray
    try {
        Stop-Service -Name $SERVICE_NAME -ErrorAction SilentlyContinue
        # Using sc.exe for better compatibility across PS versions
        sc.exe delete $SERVICE_NAME
    } catch {
        Write-Host "Warning: Could not remove Windows service. You may need to run this script as Administrator." -ForegroundColor Yellow
    }
}

# Remove files
if (Test-Path $INSTALL_DIR) {
    Write-Host "Removing $INSTALL_DIR..." -ForegroundColor Gray
    Remove-Item $INSTALL_DIR -Recurse -Force
}

Write-Host "Mullai has been uninstalled." -ForegroundColor Green
Write-Host "Note: You may want to manually remove the Mullai directory from your User PATH." -ForegroundColor Gray
