using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackBrowser
{
    public static class AdShieldEngine
    {
        private static readonly string[] AdDomains = new string[] {
            "doubleclick.net", "googlesyndication.com",
            "2mdn.net", "moatads.com", "adnxs.com", "advertising.com",
            "taboola.com", "outbrain.com", "scorecardresearch.com",
            "hotjar.com", "mixpanel.com", "bat.bing.com", "demdex.net",
            "bluekai.com", "criteo.com", "adsrvr.org", "pubmatic.com",
            "rubiconproject.com", "openx.net", "amazon-adsystem.com",
            "connect.facebook.net", "an.facebook.com",
            "googleads.g.doubleclick.net", "pubads.g.doubleclick.net"
        };

        private const string AdBlockerScript = @"
(function() {
    'use strict';
    var style = document.createElement('style');
    style.innerHTML = [
        'ytd-ad-slot-renderer,ytd-in-feed-ad-layout-renderer,',
        'ytd-banner-promo-renderer,ytd-statement-banner-renderer,',
        'ytd-display-ad-renderer,.ytp-ad-module,.ytp-ad-player-overlay,',
        '.ytp-ad-image-overlay,.ytp-ad-text-overlay,.ytp-ce-element,',
        '.ytp-suggested-action,#masthead-ad,#player-ads,',
        'ytd-promoted-sparkles-web-renderer,ytd-companion-ad-renderer,',
        'ytd-enforcement-message-view-model,tp-yt-paper-dialog:has(ytd-enforcement-message-view-model),',
        '.ad-container,.ad-unit,.ad-box,[id*=""google_ads""],[class*=""ad-slot""]',
        '{display:none!important}'
    ].join('');
    document.head.appendChild(style);

    function checkAds() {
        try {
            var video = document.querySelector('video');
            if (video) {
                var ad = document.querySelector('.ad-showing') || document.querySelector('.ytp-ad-player-overlay');
                if (ad) {
                    video.muted = true;
                    video.playbackRate = 16.0;
                    var skip = document.querySelector('.ytp-ad-skip-button') || document.querySelector('.ytp-ad-skip-button-modern');
                    if (skip) skip.click();
                } else if (video.playbackRate === 16.0) {
                    video.playbackRate = 1.0;
                    video.muted = false;
                }
            }
            var popup = document.querySelector('ytd-enforcement-message-view-model');
            if (popup) {
                var btn = popup.querySelector('button');
                if (btn) btn.click();
                popup.remove();
            }
        } catch(e) {}
        setTimeout(checkAds, 500);
    }
    checkAds();
})();
true;
";

        public static async void AttachAdShield(WebView2 wv, Action onAdBlocked)
        {
            if (wv == null || wv.CoreWebView2 == null) return;

            foreach (string domain in AdDomains)
            {
                wv.CoreWebView2.AddWebResourceRequestedFilter(
                    "*" + domain + "*", CoreWebView2WebResourceContext.All);
            }

            wv.CoreWebView2.WebResourceRequested += (s, e) =>
            {
                try
                {
                    if (onAdBlocked != null) onAdBlocked();

                    e.Response = wv.CoreWebView2.Environment.CreateWebResourceResponse(
                        new MemoryStream(new byte[0]), 200, "OK", "Content-Type: text/plain");
                }
                catch { }
            };

            await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(AdBlockerScript);
        }
    }
}
