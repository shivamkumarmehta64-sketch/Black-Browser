# Black Browser — Edge Light Version on Windows

A native, ultra-lightweight Windows web browser styled after Microsoft Edge Light Mode, built with C# (.NET Framework 4.8) and Microsoft WebView2. Packed with Chrome extension availability, Edge Add-ons integration, 3-layer ad blocking, and minimum RAM consumption.

![Black Icon](icon.png)

---

## ✨ Features

- 🌐 **Microsoft Edge Light Design**:
  - Clean white Edge toolbar (`#FFFFFF`), Edge pill address bar (`#F3F3F3`), and rounded light tabs.

- 🧩 **Chrome & Edge Extensions Integration**:
  - Direct access to Microsoft Edge Add-ons and Chrome Web Store via `🧩 Extensions` toolbar button.

- 🛑 **3-Layer Ad Blocker & Shield Badge Counter**:
  - Network domain filter, JSON payload stripper, and DOM mute-skipper with real-time blocked counter (`🛡 N`).

- 🏠 **Edge Light Speed Dial (New Tab Page)**:
  - Clean speed dial page with quick links (Google, YouTube, YT Music, Edge Add-ons, Chrome Web Store, GitHub, Reddit, ChatGPT) and search bar.

- ⚡ **Resource & Memory Optimization**:
  - Restricted Chromium cache (32MB), single renderer process, and JS heap limit (128MB).
  - Process suspension (`TrySuspendAsync()`) + `SetProcessWorkingSetSize` memory trim on tray minimize (**~35–50 MB RAM in tray**).

- 🎛️ **Keyboard Shortcuts & Navigation**:
  - `Ctrl + T`: New Tab
  - `Ctrl + W`: Close Current Tab
  - `Ctrl + L`: Focus Address Bar
  - `Ctrl + R` / `F5`: Reload Page
  - `Alt + Left` / `Alt + Right`: Back / Forward Navigation
  - `F11`: Fullscreen Toggle

---

## 🛠️ Build & Installation

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /out:Black.exe Black.cs
```

Or run `setup.bat`.

---

## 📜 License

Distributed under the MIT License.
