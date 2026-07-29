# Black Noir — PowerShell Installer
# Part of the Black Numbers

$ErrorActionPreference = "Stop"
$repo = "shivamkumarmehta64-sketch/Black-Noir"
$appDir = "$env:APPDATA\Black Noir"
$exePath = "$appDir\BlackNoir.exe"

Write-Host "╔══════════════════════════════════╗" -ForegroundColor DarkMagenta
Write-Host "║        Black Noir v0.1.0         ║" -ForegroundColor Magenta
Write-Host "║   Part of the Black Numbers      ║" -ForegroundColor DarkMagenta
Write-Host "╚══════════════════════════════════╝" -ForegroundColor DarkMagenta
Write-Host ""

# Check WebView2 runtime
$wv = Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00FB3A9B7E4C}" -ErrorAction SilentlyContinue
if (-not $wv) {
    Write-Host "› Installing WebView2 Runtime (required)..." -ForegroundColor Yellow
    $wvUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    $wvInstaller = "$env:TEMP\WebView2Setup.exe"
    Invoke-WebRequest -Uri $wvUrl -OutFile $wvInstaller -UseBasicParsing
    Start-Process -Wait -FilePath $wvInstaller -ArgumentList "/silent /install"
    Remove-Item $wvInstaller -Force -ErrorAction SilentlyContinue
}

# Download Black Noir
Write-Host "› Downloading Black Noir..." -ForegroundColor Yellow
$zipUrl = "https://github.com/$repo/releases/latest/download/BlackNoir.zip"
$zipPath = "$env:TEMP\BlackNoir.zip"
Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath -UseBasicParsing

# Extract
Write-Host "› Installing to $appDir..." -ForegroundColor Yellow
if (-not (Test-Path $appDir)) { New-Item -ItemType Directory -Path $appDir -Force | Out-Null }
Expand-Archive -Path $zipPath -DestinationPath $appDir -Force
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

# Create shortcuts
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut("$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Black Noir.lnk")
$sc.TargetPath = $exePath
$sc.Description = "Black Noir Browser — Part of the Black Numbers"
$sc.WorkingDirectory = $appDir
$sc.Save()

$sc2 = $ws.CreateShortcut("$env:USERPROFILE\Desktop\Black Noir.lnk")
$sc2.TargetPath = $exePath
$sc2.Description = "Black Noir Browser — Part of the Black Numbers"
$sc2.WorkingDirectory = $appDir
$sc2.Save()

Write-Host ""
Write-Host "✓ Black Noir installed successfully!" -ForegroundColor Green
Write-Host "  Binary: $((Get-Item $exePath).Length/1MB -as [int]) MB" -ForegroundColor Gray
Write-Host ""
Write-Host "  Hotkeys:" -ForegroundColor Magenta
Write-Host "    Ctrl+L  Focus address bar" -ForegroundColor Gray
Write-Host "    Ctrl+R  Reload page" -ForegroundColor Gray
Write-Host "    Ctrl+Q  Quit" -ForegroundColor Gray
Write-Host ""
Write-Host "  Browser launched from Start Menu or Desktop shortcut." -ForegroundColor Gray
Write-Host ""

# Launch
Start-Process -FilePath $exePath
