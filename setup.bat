@echo off
title Black Browser Setup
echo ========================================================
echo             Black Browser - Setup Build
echo ========================================================
echo.

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [ERROR] .NET Framework 4.8 compiler (csc.exe) not found.
    pause
    exit /b 1
)

echo [1/2] Compiling Black.cs...
"%CSC%" /nologo /target:winexe /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /out:Black.exe Black.cs

if errorlevel 1 (
    echo [ERROR] Compilation failed. Check error messages above.
    pause
    exit /b 1
)
echo [OK] Black.exe compiled successfully!
echo.

echo [2/2] Creating Desktop shortcut...
powershell -NoProfile -Command "^
    $desktop = [Environment]::GetFolderPath('Desktop'); ^
    $ws = New-Object -ComObject WScript.Shell; ^
    $sc = $ws.CreateShortcut(\"$desktop\Black Browser.lnk\"); ^
    $sc.TargetPath = '%~dp0Black.exe'; ^
    $sc.WorkingDirectory = '%~dp0'; ^
    $sc.IconLocation = '%~dp0icon.ico,0'; ^
    $sc.Description = 'Black - Edge Light Version Web Browser'; ^
    $sc.Save();"

echo [OK] Desktop shortcut created!
echo.
echo ========================================================
echo Setup complete! Launch Black Browser from your Desktop.
echo ========================================================
echo.
