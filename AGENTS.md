# Black Noir — Ultra-lightweight Windows Browser

A **~4.6 MB** desktop browser inspired by the Black Numbers of *That Time I Got Reincarnated as a Slime*, with built-in ad blocking.

## Features
- 🖤 **Black Noir aesthetic** — Dark UI with purple/blue accents
- 🚫 **Ad blocking** — Network (navigation) + JS (fetch/XHR) + DOM removal
- 🔍 **Ctrl+L to browse** — Minimal hotkey-driven UI
- ⚡ **4.6 MB binary** — ~50 MB RAM, instant startup

## Hotkeys
| Key | Action |
|-----|--------|
| `Ctrl+L` | Focus address bar |
| `Ctrl+R` | Reload page |
| `Ctrl+Q` | Quit |

## Building
```powershell
$env:CARGO_TARGET_DIR = "C:\tmp\build-lb"
cd black-noir
npm install
npx tauri build --target x86_64-pc-windows-gnu
```

## Installing
```powershell
irm https://github.com/shivamkumarmehta64-sketch/Black-Noir/releases/latest/download/install.ps1 | iex
```
