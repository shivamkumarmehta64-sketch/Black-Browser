/*
 * Light Browser — injected ad blocking script
 * Runs on every page load. Handles:
 *   - Address bar injection (fixed top bar)
 *   - Fetch/XHR interception for ad domains
 *   - DOM element removal (CSS selectors)
 *   - SPA navigation detection
 *   - Ctrl+L hotkey, URL display polling
 */

/*
 * Black Noir Browser — ad blocking engine
 */

const AD_DOMAINS = [
  "doubleclick.net", "googlesyndication.com", "googleadservices.com",
  "pagead2.googlesyndication.com", "tpc.googlesyndication.com",
  "adservice.google.com", "2mdn.net", "google-analytics.com",
  "googletagmanager.com", "googletagservices.com", "adnxs.com",
  "rubiconproject.com", "criteo.com", "criteo.net", "pubmatic.com",
  "openx.net", "casalemedia.com", "moatads.com", "adsrvr.org",
  "sharethrough.com", "taboola.com", "outbrain.com", "amazon-adsystem.com",
  "sovrn.com", "indexww.com", "adsafeprotected.com", "contextweb.com",
  "lijit.com", "exelator.com", "bluekai.com", "agkn.com", "media.net",
  "revcontent.com", "zergnet.com", "advertising.com", "atdmt.com",
  "atwola.com", "bidswitch.net", "demdex.net", "krxd.net", "mathtag.com",
  "quantserve.com", "rlcdn.com", "ru4.com", "scorecardresearch.com",
  "serving-sys.com", "tynt.com", "adsymptotic.com", "appnexus.com",
  "adzerk.net", "exoclick.com", "propellerads.com", "popads.net",
  "onclickads.net", "mgid.com", "infolinks.com", "bidvertiser.com",
  "adf.ly", "adbrite.com", "adbutler.com", "adcash.com", "adf.ly",
  "adlure.net", "admedia.com", "adperium.com", "adrecover.com",
  "adriot.com", "adroll.com", "adscale.de", "adserver.com",
  "adsmogo.com", "adspeed.net", "adspirit.de", "adtech.com",
  "adtech.de", "adventory.com", "adzerk.com", "affiliate.com",
  "bang.com", "bidclix.com", "buysellads.com", "carbonads.com",
  "clickbooth.com", "clicksor.com", "clicktrack.xyz", "codefund.com",
  "commissionjunction.com", "conversantmedia.com", "convertro.com",
  "cpalead.com", "cpxinteractive.com", "decknetwork.net",
  "dianomi.com", "dotandad.com", "dothomereports.com",
  "doublepimp.com", "dwin1.com", "ebay.com/partner", "epom.com",
  "etahub.com", "exponential.com", "eyenewton.ru", "fasternet.biz",
  "filehorse.com", "flashtalking.com", "fout.jp", "futureads.com",
  "g.doubleclick.net", "gamemonetize.com", "gemius.pl",
  "groovinads.com", "hooklogic.com", "hyperbanner.net",
  "i.liadm.com", "ibillboard.com", "igaworks.com", "improvedigital.com",
  "impulseclick.com", "inmobi.com", "intentiq.com", "interactivecircle.com",
  "iocnt.net", "ipromote.com", "justpremium.com", "kiosked.com",
  "leadboltads.net", "lockerdome.com", "madadsmedia.com",
  "mbistream.com",
];

const AD_SELECTORS = [
  '[id*="google_ads"]', '[id*="ad-slot"]', '[id*="ad-"]',
  '[class*="ad-slot"]', '[class*="adsbygoogle"]', '[class*="ad-container"]',
  '[class*="advertisement"]', '[class*="ad-unit"]', '[class*="ad-box"]',
  '[class*="ad-wrapper"]', '[class*="ad-banner"]', '[class*="adsbox"]',
  'ins.adsbygoogle', 'div[data-ad*]', 'div[id*="ad-"]',
  'div[class*="ad-"]', 'iframe[src*="doubleclick"]', 'iframe[src*="ads"]',
  'amp-ad', '[data-ad-manager]', '[data-ad-client]',
  '[data-ad-slot]', '.Ad--empty', '.ad_item', '.advertising',
  '.sponsored-content', '.sponsored', '[id*="sponsored"]',
  '[class*="sponsored"]', '#sidebar-ads', '#right-ads',
  '.promoted-content', '.recommended-ad',
];

function isAdUrl(url) {
  try {
    const u = new URL(url);
    return AD_DOMAINS.some(d => u.hostname.includes(d));
  } catch { return false; }
}

function removeAdElements() {
  AD_SELECTORS.forEach(sel => {
    document.querySelectorAll(sel).forEach(el => el.remove());
  });
}

function addAddressBar() {
  if (document.getElementById('lb-bar')) return;

  const bar = document.createElement('div');
  bar.id = 'lb-bar';
  bar.innerHTML = `
<div style="display:flex;align-items:center;gap:6px;padding:4px 8px;background:#0a0a0f;color:#e0e0e0;font-family:Segoe UI,sans-serif;font-size:13px;position:fixed;top:0;left:0;right:0;z-index:2147483647;height:34px;box-shadow:0 2px 8px rgba(0,0,0,0.5);border-bottom:1px solid #2a0a3a">
  <button id="lb-back" style="background:#1a0a2a;border:none;color:#c084fc;cursor:pointer;font-size:16px;padding:2px 8px;border-radius:4px;transition:all 0.2s" title="Back">&#9664;</button>
  <button id="lb-fwd" style="background:#1a0a2a;border:none;color:#c084fc;cursor:pointer;font-size:16px;padding:2px 8px;border-radius:4px;transition:all 0.2s" title="Forward">&#9654;</button>
  <button id="lb-ref" style="background:#1a0a2a;border:none;color:#c084fc;cursor:pointer;font-size:16px;padding:2px 8px;border-radius:4px;transition:all 0.2s" title="Refresh">&#8635;</button>
  <input id="lb-input" type="text" style="flex:1;padding:3px 10px;border-radius:6px;border:1px solid #3a1a5a;background:#0a0a0f;color:#e0e0e0;font-size:13px;font-family:Segoe UI,sans-serif;outline:none;margin:0 4px" autocomplete="off" spellcheck="false"/>
  <span id="lb-status" style="color:#a855f7;font-size:10px;font-weight:bold;margin-right:4px" title="Ad blocking active">&#9733; Noir Shield</span>
</div>`;
  document.documentElement.insertBefore(bar, document.documentElement.firstChild);
  document.body.style.marginTop = '38px';
  document.body.style.position = 'relative';

  const input = document.getElementById('lb-input');
  if (input) {
    input.placeholder = 'Enter URL and press Enter...';
    input.addEventListener('keydown', e => {
      if (e.key === 'Enter') {
        let url = input.value.trim();
        if (url && !url.startsWith('http://') && !url.startsWith('https://')) {
          url = 'https://' + url;
        }
        if (url) window.location.href = url;
      }
    });
    setTimeout(() => input.value = location.href, 100);
  }

  document.getElementById('lb-back')?.addEventListener('click', () => window.history.back());
  document.getElementById('lb-fwd')?.addEventListener('click', () => window.history.forward());
  document.getElementById('lb-ref')?.addEventListener('click', () => location.reload());
}

function interceptFetch() {
  const origFetch = window.fetch;
  window.fetch = function(...args) {
    const url = typeof args[0] === 'string' ? args[0] : args[0]?.url || '';
    if (isAdUrl(url)) return Promise.resolve(new Response('', { status: 204 }));
    return origFetch.apply(this, args);
  };

  const origXHR = window.XMLHttpRequest;
  const XHRProxy = function() {
    const xhr = new origXHR();
    const origOpen = xhr.open.bind(xhr);
    xhr.open = function(method, url, ...rest) {
      if (isAdUrl(url)) {
        setTimeout(() => xhr.dispatchEvent(new Event('loadend')), 0);
        return;
      }
      return origOpen(method, url, ...rest);
    };
    return xhr;
  };
  XHRProxy.prototype = origXHR.prototype;
  window.XMLHttpRequest = XHRProxy;
}

document.addEventListener('DOMContentLoaded', () => {
  addAddressBar();
  removeAdElements();
  interceptFetch();
  setInterval(removeAdElements, 2000);
  if (window.location.href === 'https://www.google.com/' || window.location.href === 'about:blank') {
    const input = document.getElementById('lb-input');
    if (input) setTimeout(() => input.focus(), 200);
  }
});

document.addEventListener('keydown', e => {
  if (e.ctrlKey && e.key === 'l') {
    e.preventDefault();
    const input = document.getElementById('lb-input');
    if (input) { input.focus(); input.select(); }
  }
});

let lastUrl = location.href;
new MutationObserver(() => {
  const input = document.getElementById('lb-input');
  if (location.href !== lastUrl) {
    lastUrl = location.href;
    if (input) input.value = location.href;
  }
}).observe(document, { subtree: true, childList: true });

window.addEventListener('popstate', () => {
  const input = document.getElementById('lb-input');
  if (input) setTimeout(() => input.value = location.href, 50);
});
