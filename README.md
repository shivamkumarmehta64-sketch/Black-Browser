# Black Noir

A minimal, ad-free Windows web browser built on WebView2 with native ad blocking powered by [Brave's `adblock-rust`](https://github.com/brave/adblock-rust) engine. No Electron, no Tauri — just a 2.3 MB binary and Windows' preinstalled WebView2.

## Features

- **Ultra‑light** — ~2.3 MB binary, ~25 MB RAM at idle
- **Ad blocking** — Built‑in `adblock-rust` engine with EasyList‑compatible filter lists; blocks ads, trackers, and malware domains on navigation and resource requests
- **Background playback** — Close to tray; YouTube / YouTube Music keeps playing. Exit via tray menu or `Ctrl+Q`
- **Dark theme** — Clean dark new‑tab page with search bar and speed‑dial shortcuts
- **Address bar in every page** — `Ctrl+L` or click injected bar; supports navigation, search (via URL), and back/forward/refresh
- **No dependencies** — Uses the WebView2 runtime already installed on Windows 11 (bundled on Win 10 via Evergreen distribution)
- **Privacy** — No telemetry, no auto‑updater, no account requirements; tracking prevention enabled by default

## Prerequisites

- **Windows 11** (or Windows 10 with [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/))
- **Rust** toolchain — install from [rustup.rs](https://rustup.rs/)

## Build & Run

```powershell
# Clone
git clone https://github.com/shivamkumarmehta64-sketch/Black-Noir.git
cd Black-Noir

# Build release (optimized, stripped)
cargo build --release

# Run
.\target\release\black-noir.exe
```

Or run directly without building:

```powershell
cargo run --release
```

## Usage

| Action | Input |
|--------|-------|
| Navigate / search | Type in the NTP search bar and press `Enter` |
| Focus address bar | `Ctrl+L` anywhere |
| Quit | `Ctrl+Q` in the browser, or tray icon → Exit |
| Hide to tray | Click the window close button |
| Restore from tray | Left‑click the tray icon |
| Background playback | Close the window while audio is playing (e.g., YouTube Music) |

When you close the window, Black Noir minimises to the system tray instead of quitting. The WebView2 process continues to run, letting audio keep playing. Use the tray context menu (right‑click) to exit completely.

## Ad Blocking

The built‑in ad‑blocker uses Brave's `adblock-rust` engine with a user‑supplied filter list. By default a minimal domain‑based filter list is included.

**To use a full filter list (EasyList + EasyPrivacy + uBlock filters, ~138K rules):**

1. Download the lists:
   ```powershell
   Invoke-WebRequest -Uri "https://easylist.to/easylist/easylist.txt" -OutFile "filters\easylist.txt"
   Invoke-WebRequest -Uri "https://easylist.to/easylist/easyprivacy.txt" -OutFile "filters\easyprivacy.txt"
   ```
2. Combine into a single file:
   ```powershell
   Get-Content filters\easylist.txt, filters\easyprivacy.txt | Set-Content filters\combined.txt
   ```
3. Rebuild:
   ```powershell
   cargo build --release
   ```

The blocker intercepts:
- Top‑level navigations to ad/malware domains
- Sub‑resource requests (scripts, images, XHR, beacons, etc.) against ad/tracker domains

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+L` | Focus address bar |
| `Ctrl+Q` | Quit Black Noir |
| `Alt+Left` | Back (browser default) |
| `Alt+Right` | Forward (browser default) |
| `F5` / `Ctrl+R` | Refresh |

## Project Structure

```
Black-Noir/
├── Cargo.toml          # Rust dependencies and build config
├── src/
│   └── main.rs         # Application entry point (Win32 + WebView2 + adblock)
├── web/
│   ├── index.html      # New tab page (NTP) with search bar and shortcuts
│   └── inject.js       # Injected into every page: address bar, back/fwd/refresh
├── filters/
│   └── combined.txt    # Ad‑blocker filter rules (EasyList‑compatible format)
└── README.md
```

## How It Works

Black Noir is a pure Win32 application. On startup:

1. A window is created with `RegisterClassExW` / `CreateWindowExW` (no framework, no CRT‑heavy init)
2. COM is initialised and the WebView2 environment is created via `CreateCoreWebView2EnvironmentWithOptions`
3. A `ICoreWebView2Controller` is created and its `ICoreWebView2` interface is obtained
4. The `adblock-rust` engine loads the filter list from `filters/combined.txt`
5. Event handlers are wired up:
   - `NavigationStarting` — block top‑level navigations to ad domains
   - `WebResourceRequested` — block sub‑resource requests (scripts, images, XHR…) to ad/tracker domains
   - `NavigationCompleted` — inject the address‑bar script into every page
   - `WebMessageReceived` — handle `quit` messages from JavaScript (`Ctrl+Q`)
6. A system‑tray icon is added for background‑playback support
7. The Win32 message pump runs until `PostQuitMessage` is called

## Comparison

| | Black Noir | Tauri (previous) | Electron |
|---|---|---|---|
| Binary size | ~2.3 MB | ~9.8 MB | ~150 MB |
| RAM (idle) | ~25 MB | ~37 MB | ~80+ MB |
| WebView2 | Preinstalled (Win11) | Bundled or preinstalled | Bundled Chromium |
| Ad blocking | Native `adblock-rust` | Native `adblock-rust` | Extensions |
| Background play | Tray icon (built‑in) | Tray plugin | Custom |

## License

MIT
