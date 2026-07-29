const { app, BrowserWindow, session, ipcMain } = require('electron');
const path = require('path');
const fs = require('fs');

const ublockPath = fs.existsSync(path.join(__dirname, '..', 'ublock', 'uBlock0.chromium'))
  ? path.join(__dirname, '..', 'ublock', 'uBlock0.chromium')
  : path.join(process.resourcesPath, 'uBlock0.chromium');
const configPath = path.join(app.getPath('userData'), 'config.json');

const adDomains = [
  'doubleclick.net','googlesyndication.com','googleadservices.com',
  'google-analytics.com','googletagmanager.com','pagead2.googlesyndication.com',
  '2mdn.net','gstatic.com','tpc.googlesyndication.com',
  'adservice.google.com','adsafeprotected.com','moatads.com','moat.com',
  'adsrvr.org','serving-sys.com','casalemedia.com','rfihub.com','openx.net',
  'pubmatic.com','rubiconproject.com','indexww.com','sonobi.com','appnexus.com',
  'criteo.com','criteo.net','taboola.com','outbrain.com','scorecardresearch.com',
  'exelator.com','demdex.net','bluekai.com','bat.bing.com',
  'adsymptotic.com','adnxs.com','advertising.com','yieldmo.com',
  'sharethrough.com','improvedigital.com','smartadserver.com',
  'adform.net','adzerk.net','media.net','contextweb.com',
  'amazon-adsystem.com','aax.amazon-adsystem.com'
];

let mainWindow;

function loadConfig() {
  try { return JSON.parse(fs.readFileSync(configPath, 'utf8')); } catch (e) { return {}; }
}
function saveConfig(cfg) {
  try { fs.writeFileSync(configPath, JSON.stringify(cfg, null, 2)); } catch (e) {}
}

app.whenReady().then(async () => {
  const filter = { urls: adDomains.map(d => '*://*.' + d + '/*') };
  session.defaultSession.webRequest.onBeforeRequest(filter, (d, c) => c({ cancel: true }));
  try { await session.defaultSession.loadExtension(ublockPath); } catch (e) {}
  mainWindow = new BrowserWindow({
    width: 1280, height: 800, minWidth: 900, minHeight: 600,
    autoHideMenuBar: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      webviewTag: true,
      contextIsolation: false,
      nodeIntegration: false
    }
  });
  mainWindow.loadFile('browser.html');
});

ipcMain.handle('get-config', () => loadConfig());
ipcMain.handle('set-config', (e, cfg) => { saveConfig(cfg); return true; });

const allowedSchemes = ['http:', 'https:', 'about:', 'data:', 'file:'];
ipcMain.handle('navigate', (e, url) => {
  try {
    const u = new URL(url);
    if (!allowedSchemes.includes(u.protocol)) return 'blocked';
  } catch { return 'invalid'; }
  return 'ok';
});
