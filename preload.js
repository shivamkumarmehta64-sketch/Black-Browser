const { ipcRenderer } = require('electron');
window.api = {
  getConfig: () => ipcRenderer.invoke('get-config'),
  setConfig: (cfg) => ipcRenderer.invoke('set-config', cfg),
  navigate: (url) => ipcRenderer.invoke('navigate', url)
};
