# Black-Noir Browser — Ultra-Lightweight Private Web Browser

A native, high-performance Windows web browser built with C# (.NET Framework 4.8) and Microsoft WebView2. Designed for maximum privacy, instant startup, low RAM consumption, and seamless ad blocking.

![Black-Noir Icon](icon.png)

---

## ✨ Key Features

- 🗂️ **Multi-Tab Browsing**:
  - Open (`Ctrl+T`), switch, and close (`Ctrl+W`) multiple tabs seamlessly.

- 🛑 **Built-in 3-Layer Ad-Blocker & Shield Badge**:
  - **Network Domain Filter**: Blocks 35+ top ad & tracking networks (`doubleclick.net`, `googlesyndication.com`, `adservice.google.com`, etc.).
  - **JSON Payload Stripping**: Intercepts YouTube & Web API ad definitions dynamically.
  - **DOM Muting & Fast-Forwarding**: Silently mutes video ad elements and skips them at 16x speed.
  - **Live Shield Counter**: Real-time counter in toolbar showing total blocked ads & trackers.

- 🏠 **Dark Speed Dial (New Tab Page)**:
  - Sleek dark aesthetic start page with quick dial shortcuts (Google, YouTube, YT Music, GitHub, Reddit, ChatGPT) and search bar.

- ⚡ **Resource & Memory Optimization**:
  - **Chromium Launch Flags**: Restricted disk/media cache (32MB), single renderer process, and JS heap limit (128MB).
  - **Process Suspension**: WebView2 process suspends when minimized to tray, freeing physical RAM.
  - **Automated GC**: Background garbage collection runs every 60 seconds (`SetProcessWorkingSetSize` memory trim).
  - **RAM Usage**: ~70–100 MB active | ~35–50 MB minimized in system tray.

- 🎛️ **Keyboard Shortcuts & Navigation**:
  - `Ctrl + T`: New Tab
  - `Ctrl + W`: Close Current Tab
  - `Ctrl + L`: Focus & Select Address Bar
  - `Ctrl + R` / `F5`: Reload Page
  - `Alt + Left` / `Alt + Right`: Back / Forward Navigation
  - `F11`: Fullscreen Toggle

---

## 🛠️ Build & Installation

- **OS**: Windows 10 / Windows 11 (64-bit)
- **Runtime**: [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) (Pre-installed on Windows 10/11) + [Microsoft WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

### Compilation

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /out:BlackNoir.exe BlackNoir.cs
```

Or simply run `setup.bat`.

---

## 📜 License

Distributed under the MIT License.
