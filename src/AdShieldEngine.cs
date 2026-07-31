using System;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace BlackBrowser
{
    public static class AdShieldEngine
    {
        private static readonly string[] AdDomains = new string[]
        {
            "doubleclick.net", "googlesyndication.com", "googleadservices.com",
            "adservice.google.com", "adnxs.com", "rubiconproject.com",
            "pubmatic.com", "openx.net", "criteo.com", "taboola.com",
            "outbrain.com", "scorecardresearch.com", "quantserve.com"
        };

        private static readonly string[] AllowedStreamDomains = new string[]
        {
            "googlevideo.com", "youtube.com", "ytimg.com", "netflix.com",
            "nflxvideo.net", "twitch.tv", "ttvnw.net", "vimeo.com",
            "akamaized.net", "cloudfront.net", ".m3u8", ".mpd", "blob:"
        };

        public static void AttachAdShield(WebView2 wv, Action onAdBlocked)
        {
            if (wv == null || wv.CoreWebView2 == null) return;

            // Layer 1: Network Filtering (Exempting Video Stream Media)
            wv.CoreWebView2.WebResourceRequested += (sender, args) =>
            {
                string uri = args.Request.Uri.ToLower();

                // Explicitly allow all video streaming manifests & media fragments to prevent black screens
                foreach (string streamDomain in AllowedStreamDomains)
                {
                    if (uri.Contains(streamDomain))
                        return; // Allow stream byte transfer
                }

                foreach (string domain in AdDomains)
                {
                    if (uri.Contains(domain))
                    {
                        args.Response = wv.CoreWebView2.Environment.CreateWebResourceResponse(
                            null, 200, "OK", "Content-Type: text/plain");

                        if (onAdBlocked != null) onAdBlocked();
                        break;
                    }
                }
            };

            foreach (string domain in AdDomains)
            {
                wv.CoreWebView2.AddWebResourceRequestedFilter("*" + domain + "*", CoreWebView2WebResourceContext.All);
            }

            // Layer 2 & 3: CSS Element Hiding & 500ms JS Video Ad Fast-Forwarder
            wv.CoreWebView2.NavigationCompleted += async (s, e) =>
            {
                string cssScript = @"
                    (function() {
                        var style = document.createElement('style');
                        style.id = 'black-adshield-css';
                        style.innerHTML = `
                            .video-ads, .ytp-ad-module, .ytp-ad-overlay-container,
                            [id^='google_ads_'], .ad-container, .ad-wrapper,
                            iframe[src*='doubleclick'], iframe[src*='googlesyndication'] {
                                display: none !important; visibility: hidden !important; width: 0 !important; height: 0 !important;
                            }
                        `;
                        if (!document.getElementById('black-adshield-css')) {
                            (document.head || document.documentElement).appendChild(style);
                        }
                    })();
                ";

                string jsAutoSkip = @"
                    setInterval(function() {
                        try {
                            var video = document.querySelector('video');
                            if (video) {
                                var skipBtn = document.querySelector('.ytp-ad-skip-button, .ytp-ad-skip-button-modern, .ytp-skip-ad-button');
                                if (skipBtn) {
                                    skipBtn.click();
                                }
                                var adShowing = document.querySelector('.ad-showing, .video-ads');
                                if (adShowing) {
                                    video.muted = true;
                                    video.playbackRate = 16.0;
                                    if (isFinite(video.duration) && video.duration > 0) {
                                        video.currentTime = video.duration - 0.1;
                                    }
                                }
                            }
                            var overlay = document.querySelector('.ytp-ad-overlay-close-button');
                            if (overlay) overlay.click();
                        } catch(e) {}
                    }, 500);
                ";

                try
                {
                    await wv.CoreWebView2.ExecuteScriptAsync(cssScript);
                    await wv.CoreWebView2.ExecuteScriptAsync(jsAutoSkip);
                }
                catch { }
            };
        }
    }
}
