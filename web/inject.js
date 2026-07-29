function addAddressBar() {
  if (document.getElementById('bn-bar')) return;
  const bar = document.createElement('div');
  bar.id = 'bn-bar';
  bar.style.cssText = 'position:fixed;top:0;left:0;right:0;z-index:2147483647;display:flex;align-items:center;gap:6px;padding:6px 12px;background:rgba(20,20,20,0.95);border-bottom:1px solid #2a2a2a;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,sans-serif;';
  bar.innerHTML = '<button id="bn-back" style="background:none;border:none;color:#999;cursor:pointer;font-size:16px;padding:4px 6px;">&#8592;</button><button id="bn-fwd" style="background:none;border:none;color:#999;cursor:pointer;font-size:16px;padding:4px 6px;">&#8594;</button><button id="bn-ref" style="background:none;border:none;color:#999;cursor:pointer;font-size:16px;padding:4px 6px;">&#8635;</button><div style="flex:1;display:flex;align-items:center;background:#1a1a1a;border:1px solid #333;border-radius:20px;padding:2px 14px;margin:0 4px;"><span style="color:#777;font-size:11px;margin-right:6px;">&#128274;</span><input id="bn-input" type="text" style="flex:1;background:transparent;border:none;color:#ccc;font-size:13px;outline:none;padding:6px 0;" autocomplete="off" spellcheck="false"/></div><span id="bn-shield" style="color:#666;font-size:11px;font-weight:500;padding:4px 8px;">&#128737; Shield</span>';
  document.documentElement.insertBefore(bar, document.documentElement.firstChild);
  document.body.style.marginTop = '44px';

  const input = document.getElementById('bn-input');
  if (input) {
    input.addEventListener('keydown', e => {
      if (e.key === 'Enter') {
        let url = input.value.trim();
        if (url && !url.startsWith('http://') && !url.startsWith('https://')) url = 'https://' + url;
        if (url) window.location.href = url;
      }
    });
    setTimeout(() => input.value = location.href, 100);
  }
  document.getElementById('bn-back')?.addEventListener('click', () => window.history.back());
  document.getElementById('bn-fwd')?.addEventListener('click', () => window.history.forward());
  document.getElementById('bn-ref')?.addEventListener('click', () => location.reload());
}

document.addEventListener('DOMContentLoaded', () => { addAddressBar(); });
document.addEventListener('keydown', e => {
  if (e.ctrlKey && e.key === 'l') { e.preventDefault(); const i = document.getElementById('bn-input'); if (i) { i.focus(); i.select(); } }
});

let lastUrl = location.href;
new MutationObserver(() => {
  const input = document.getElementById('bn-input');
  if (location.href !== lastUrl) { lastUrl = location.href; if (input) input.value = location.href; }
}).observe(document, { subtree: true, childList: true });
window.addEventListener('popstate', () => {
  const input = document.getElementById('bn-input');
  if (input) setTimeout(() => input.value = location.href, 50);
});
