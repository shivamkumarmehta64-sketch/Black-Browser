# Black Browser

A native, ultra-lightweight Windows web browser styled after Microsoft Edge Light Mode, built with C# (.NET Framework 4.8) and Microsoft WebView2. Three-layer ad blocking, local bookmarks, custom speed dials, and minimal RAM usage — no Electron, no Tauri, no browser engine bloat.

![Black Icon](icon.png)

## ✨ Features

- **Edge Light Design**: clean white toolbar, pill address bar, rounded light tabs, light/dark theme.
- **3-Layer Ad Blocker** (`src/AdShieldEngine.cs`): native domain filter, JSON payload stripper, and DOM mute-skipper with a live blocked counter (🛡 N). Includes YouTube ad-free playback.
- **Local Bookmarks** (`black://bookmarks`): 100% local, stored in `%LOCALAPPDATA%\black-webview2\bookmarks.json` — no account required.
- **Custom Speed Dials** (`black://dial`): add/remove your own dials on the new-tab page, stored in `custom_dials.json`.
- **Downloads Manager** (`black://downloads`): tracks every file you download in `downloads.json`.
- **Local History** (`black://history`): private browsing history stored locally in `history.json` — no Google sign-in.
- **Edge Add-ons & Chrome Web Store**: direct extension access via the 🧩 button.
- **Memory Optimization**: 32 MB cache cap, single renderer, 128 MB JS heap limit, process suspension in tray (**~35–50 MB RAM when minimized to tray**).
- **Keyboard Shortcuts**:
  - `Ctrl + T` New Tab · `Ctrl + W` Close Tab · `Ctrl + L` Address Bar
  - `Ctrl + R` / `F5` Reload · `Alt + ←` / `Alt + →` Back / Forward · `F11` Fullscreen

## 🛠️ Requirements

- Windows 10 or 11
- .NET Framework 4.8 (preinstalled on Windows 10 1903+ / 11)
- Microsoft Edge WebView2 Runtime (preinstalled with Edge; the runtime DLLs in this repo are included for standalone builds)

## 🚀 Quick Start (fresh install)

```cmd
setup.bat
```

That's it — compiles `src\*.cs` into `Black.exe`, then creates a **"Black Browser"** shortcut on your desktop.

Manual build:

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /out:Black.exe src\*.cs
```

Launch anytime with `run.bat` or the desktop shortcut. All browser data lives in `%LOCALAPPDATA%\black-webview2\` — delete that folder to reset the browser completely.

## 📁 Project Layout

```
src/Program.cs           Entry point & single-instance guard
src/BrowserForm.cs       Main window, tabs, address bar, shortcuts
src/AdShieldEngine.cs    Native 3-layer ad blocker
src/BookmarksManager.cs  Local bookmarks (black://bookmarks)
src/CustomDialsManager.cs Custom speed dials (black://dial)
src/DownloadsManager.cs  Downloads tracker (black://downloads)
src/HistoryManager.cs    Local history (black://history)
src/SpeedDialPage.cs     New-tab speed dial page
src/SettingsForm.cs      Settings modal
src/EyeCareOverlay.cs    Night-light / blue light filter
src/MemoryTrimmer.cs     RAM trimming in tray
MakeIcon.cs              Icon generator (dev tool)
```

## 📜 License

Distributed under the MIT License. See [LICENSE](LICENSE).
