Black Noir

Private browser with built-in ad blocking (webRequest + YouTube API stripping).

## Install
npm install
npm start

## Build
npm run dist        # NSIS installer
npm run dist-portable   # Portable .exe (no install needed)

## Windows Defender False Positive
Electron-packaged apps are sometimes flagged by Windows Defender. To fix:
1. Submit the exe as a false positive: https://www.microsoft.com/en-us/wdsi/filesubmission
2. Or add an exclusion: Windows Security > Virus & threat protection > Manage settings > Add or remove exclusions
3. Use the portable build (`npm run dist-portable`) — it often triggers fewer detections
