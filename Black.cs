using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackBrowser
{
    static class Program
    {
        private static Mutex mutex = null;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [STAThread]
        static void Main()
        {
            const string appName = "Black_SingleInstance_Mutex_9b2d0d52";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id && process.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(process.MainWindowHandle, 9); // SW_RESTORE
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ─── Eye Care Fullscreen Overlay ──────────────────────────────────────────

    public class EyeCareOverlayForm : Form
    {
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_LAYERED = 0x80000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
                return cp;
            }
        }

        public EyeCareOverlayForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.BackColor = Color.Black;
            this.Opacity = 0.25;
        }

        public void SetMode(int mode)
        {
            if (mode == 1)
            {
                this.BackColor = Color.FromArgb(255, 170, 0);
                this.Opacity = 0.18;
                this.Show();
            }
            else if (mode == 2)
            {
                this.BackColor = Color.Black;
                this.Opacity = 0.35;
                this.Show();
            }
            else
            {
                this.Hide();
            }
        }
    }

    public class MainForm : Form
    {
        private Panel topPanel;
        private Panel navPanel;
        private TabControl tabControl;
        private Button backBtn;
        private Button fwdBtn;
        private Button reloadBtn;
        private TextBox urlBar;
        private Button shieldBtn;
        private Button eyeCareBtn;
        private Button themeBtn;
        private Button extBtn;
        private Button newTabBtn;
        private Button closeTabBtn;
        private NotifyIcon trayIcon;
        private System.Windows.Forms.Timer gcTimer;

        private EyeCareOverlayForm eyeCareOverlay;
        private int eyeCareMode = 0;
        private bool isDarkMode = false; // Brave Light default

        private CoreWebView2Environment webViewEnv;
        private int totalBlockedAds = 0;
        private string logPath;

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        public MainForm()
        {
            logPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "debug.log");
            Log("=== Black Browser (Brave Light + Win11 Fluent) starting ===");

            this.Text = "Black (Brave Light Edition)";
            this.Width = 1280;
            this.Height = 820;
            this.BackColor = Color.FromArgb(249, 249, 251); // Win11 Light background
            this.MinimumSize = new Size(900, 600);

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);

            eyeCareOverlay = new EyeCareOverlayForm();

            InitializeUI();
            SetupTray();
            SetupGCTimer();
            InitializeBrowserEnv();
        }

        private void Log(string msg)
        {
            try { File.AppendAllText(logPath, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\n"); }
            catch { }
        }

        private void TrimMemory()
        {
            try
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Optimized, false);
                GC.WaitForPendingFinalizers();
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
            }
            catch { }
        }

        private void SetupGCTimer()
        {
            gcTimer = new System.Windows.Forms.Timer();
            gcTimer.Interval = 60000;
            gcTimer.Tick += (s, e) => TrimMemory();
            gcTimer.Start();
        }

        // ─── Windows 11 Fluent + Brave Light UI Setup ────────────────────────────

        private void InitializeUI()
        {
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 46;
            topPanel.BackColor = Color.FromArgb(255, 255, 255);
            topPanel.Padding = new Padding(6, 6, 6, 6);

            navPanel = new Panel();
            navPanel.Dock = DockStyle.Fill;
            navPanel.BackColor = Color.FromArgb(255, 255, 255);

            backBtn = CreateBraveButton("←", "Back (Alt+Left)", 0);
            fwdBtn = CreateBraveButton("→", "Forward (Alt+Right)", 36);
            reloadBtn = CreateBraveButton("↻", "Reload (Ctrl+R / F5)", 72);

            backBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoBack) wv.GoBack(); };
            fwdBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoForward) wv.GoForward(); };
            reloadBtn.Click += (s, e) => ReloadCurrentTab();

            // Brave Address Pill Bar
            urlBar = new TextBox();
            urlBar.Location = new Point(112, 6);
            urlBar.Width = this.Width - 570;
            urlBar.Height = 30;
            urlBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            urlBar.BackColor = Color.FromArgb(243, 243, 246);
            urlBar.ForeColor = Color.FromArgb(32, 32, 36);
            urlBar.Font = new Font("Segoe UI", 10.5f);
            urlBar.BorderStyle = BorderStyle.FixedSingle;

            urlBar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateCurrentTab(urlBar.Text.Trim());
                }
            };
            urlBar.Click += (s, e) => urlBar.SelectAll();

            // Brave Shield Button (Lion Orange Pill)
            shieldBtn = new Button();
            shieldBtn.Text = "🦁 0";
            shieldBtn.Width = 62;
            shieldBtn.Height = 30;
            shieldBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            shieldBtn.Location = new Point(this.Width - 445, 5);
            shieldBtn.FlatStyle = FlatStyle.Flat;
            shieldBtn.FlatAppearance.BorderSize = 0;
            shieldBtn.BackColor = Color.FromArgb(255, 240, 235);
            shieldBtn.ForeColor = Color.FromArgb(255, 80, 0); // Brave Lion Orange
            shieldBtn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            shieldBtn.Cursor = Cursors.Hand;

            // Eye Care Screen Overlay Button
            eyeCareBtn = new Button();
            eyeCareBtn.Text = "👁 Eye";
            eyeCareBtn.Width = 72;
            eyeCareBtn.Height = 30;
            eyeCareBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            eyeCareBtn.Location = new Point(this.Width - 378, 5);
            eyeCareBtn.FlatStyle = FlatStyle.Flat;
            eyeCareBtn.FlatAppearance.BorderSize = 0;
            eyeCareBtn.BackColor = Color.FromArgb(254, 247, 224);
            eyeCareBtn.ForeColor = Color.FromArgb(180, 100, 0);
            eyeCareBtn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            eyeCareBtn.Cursor = Cursors.Hand;
            eyeCareBtn.Click += (s, e) => CycleEyeCareMode();

            // Theme Switcher (🌙 Dark / ☀️ Light)
            themeBtn = new Button();
            themeBtn.Text = "☀️ Light";
            themeBtn.Width = 78;
            themeBtn.Height = 30;
            themeBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            themeBtn.Location = new Point(this.Width - 302, 5);
            themeBtn.FlatStyle = FlatStyle.Flat;
            themeBtn.FlatAppearance.BorderSize = 0;
            themeBtn.BackColor = Color.FromArgb(240, 242, 245);
            themeBtn.ForeColor = Color.FromArgb(32, 32, 36);
            themeBtn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            themeBtn.Cursor = Cursors.Hand;
            themeBtn.Click += (s, e) => ToggleTheme();

            // Extensions Button
            extBtn = new Button();
            extBtn.Text = "🧩 Extensions";
            extBtn.Width = 100;
            extBtn.Height = 30;
            extBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            extBtn.Location = new Point(this.Width - 220, 5);
            extBtn.FlatStyle = FlatStyle.Flat;
            extBtn.FlatAppearance.BorderSize = 0;
            extBtn.BackColor = Color.FromArgb(238, 242, 255);
            extBtn.ForeColor = Color.FromArgb(99, 102, 241); // Brave Indigo
            extBtn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            extBtn.Cursor = Cursors.Hand;
            extBtn.Click += (s, e) => AddNewTab("Chrome Extensions", "https://chromewebstore.google.com");

            // New Tab Button
            newTabBtn = new Button();
            newTabBtn.Text = "+";
            newTabBtn.Width = 32;
            newTabBtn.Height = 30;
            newTabBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            newTabBtn.Location = new Point(this.Width - 115, 5);
            newTabBtn.FlatStyle = FlatStyle.Flat;
            newTabBtn.FlatAppearance.BorderSize = 0;
            newTabBtn.BackColor = Color.FromArgb(243, 243, 246);
            newTabBtn.ForeColor = Color.FromArgb(60, 60, 60);
            newTabBtn.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            newTabBtn.Cursor = Cursors.Hand;
            newTabBtn.Click += (s, e) => AddNewTab("New Tab", "about:blank");

            // Close Tab Button
            closeTabBtn = new Button();
            closeTabBtn.Text = "✕";
            closeTabBtn.Width = 32;
            closeTabBtn.Height = 30;
            closeTabBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            closeTabBtn.Location = new Point(this.Width - 78, 5);
            closeTabBtn.FlatStyle = FlatStyle.Flat;
            closeTabBtn.FlatAppearance.BorderSize = 0;
            closeTabBtn.BackColor = Color.FromArgb(253, 231, 233);
            closeTabBtn.ForeColor = Color.FromArgb(232, 17, 35);
            closeTabBtn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            closeTabBtn.Cursor = Cursors.Hand;
            closeTabBtn.Click += (s, e) => CloseCurrentTab();

            navPanel.Controls.Add(backBtn);
            navPanel.Controls.Add(fwdBtn);
            navPanel.Controls.Add(reloadBtn);
            navPanel.Controls.Add(urlBar);
            navPanel.Controls.Add(shieldBtn);
            navPanel.Controls.Add(eyeCareBtn);
            navPanel.Controls.Add(themeBtn);
            navPanel.Controls.Add(extBtn);
            navPanel.Controls.Add(newTabBtn);
            navPanel.Controls.Add(closeTabBtn);
            topPanel.Controls.Add(navPanel);

            // TabControl
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Padding = new Point(14, 5);
            tabControl.Font = new Font("Segoe UI", 9.5f);
            tabControl.SelectedIndexChanged += OnTabChanged;

            this.Controls.Add(tabControl);
            this.Controls.Add(topPanel);

            this.KeyPreview = true;
            this.KeyDown += OnFormKeyDown;

            this.Resize += (s, e) =>
            {
                if (urlBar != null)
                    urlBar.Width = Math.Max(200, this.Width - 570);

                if (this.WindowState == FormWindowState.Minimized)
                {
                    SuspendAllWebViews();
                    TrimMemory();
                }
                else
                {
                    ResumeActiveWebView();
                }
            };
        }

        private Button CreateBraveButton(string text, string tooltip, int left)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = new Point(left + 4, 5);
            b.Width = 30;
            b.Height = 30;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(255, 255, 255);
            b.ForeColor = Color.FromArgb(96, 96, 96);
            b.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            return b;
        }

        private void ToggleTheme()
        {
            isDarkMode = !isDarkMode;
            if (isDarkMode)
            {
                themeBtn.Text = "🌙 Dark";
                topPanel.BackColor = Color.FromArgb(28, 28, 30);
                navPanel.BackColor = Color.FromArgb(28, 28, 30);
                urlBar.BackColor = Color.FromArgb(44, 44, 46);
                urlBar.ForeColor = Color.White;
                this.BackColor = Color.FromArgb(18, 18, 20);
            }
            else
            {
                themeBtn.Text = "☀️ Light";
                topPanel.BackColor = Color.FromArgb(255, 255, 255);
                navPanel.BackColor = Color.FromArgb(255, 255, 255);
                urlBar.BackColor = Color.FromArgb(243, 243, 246);
                urlBar.ForeColor = Color.FromArgb(32, 32, 36);
                this.BackColor = Color.FromArgb(249, 249, 251);
            }

            // Update current NTP tab if active
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.Source != null && wv.Source.ToString() == "about:blank")
            {
                wv.CoreWebView2.NavigateToString(GetBraveSpeedDialHTML());
            }
        }

        private void CycleEyeCareMode()
        {
            eyeCareMode = (eyeCareMode + 1) % 3;
            eyeCareOverlay.SetMode(eyeCareMode);

            if (eyeCareMode == 1)
            {
                eyeCareBtn.Text = "👁 Warm";
                eyeCareBtn.BackColor = Color.FromArgb(254, 235, 180);
            }
            else if (eyeCareMode == 2)
            {
                eyeCareBtn.Text = "👁 Dimmed";
                eyeCareBtn.BackColor = Color.FromArgb(220, 220, 220);
            }
            else
            {
                eyeCareBtn.Text = "👁 Eye";
                eyeCareBtn.BackColor = Color.FromArgb(254, 247, 224);
            }
        }

        // ─── Environment Initialization with Extension Engine Support ────────────

        private async void InitializeBrowserEnv()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "black-webview2");

                var options = new CoreWebView2EnvironmentOptions(
                    "--disk-cache-size=33554432 " +       // 32 MB disk cache
                    "--media-cache-size=33554432 " +      // 32 MB media cache
                    "--renderer-process-limit=1 " +       // max 1 renderer process
                    "--enable-experimental-extension-apis " +
                    "--allow-legacy-extension-manifests " +
                    "--enable-extension-activity-logging " +
                    "--no-first-run " +                   // skip first-run setup
                    "--disable-sync " +                   // no Chrome account sync
                    "--disable-translate " +              // no translate UI
                    "--js-flags=--max-old-space-size=128" // JS heap limit: 128 MB
                );

                webViewEnv = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                Log("Environment created successfully");

                AddNewTab("New Tab", "about:blank");
            }
            catch (Exception ex)
            {
                Log("FATAL Env: " + ex.ToString());
                MessageBox.Show("Failed to initialize WebView2: " + ex.Message);
            }
        }

        // ─── Tab Management ───────────────────────────────────────────────────────

        public async void AddNewTab(string title, string url)
        {
            if (webViewEnv == null) return;

            TabPage page = new TabPage(title);
            page.BackColor = isDarkMode ? Color.Black : Color.White;

            WebView2 wv = new WebView2();
            wv.Dock = DockStyle.Fill;
            page.Controls.Add(wv);
            tabControl.TabPages.Add(page);
            tabControl.SelectedTab = page;

            await wv.EnsureCoreWebView2Async(webViewEnv);

            wv.CoreWebView2.PermissionRequested += (s, e) =>
            {
                if (e.PermissionKind == CoreWebView2PermissionKind.Notifications)
                    e.State = CoreWebView2PermissionState.Allow;
            };

            SetupNetworkBlocking(wv);
            await wv.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(GetAdBlockerJS());

            wv.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                try
                {
                    string msg = e.TryGetWebMessageAsString();
                    if (msg == "AD_BLOCKED")
                    {
                        totalBlockedAds++;
                        shieldBtn.Text = "🦁 " + totalBlockedAds;
                    }
                }
                catch { }
            };

            wv.CoreWebView2.NavigationStarting += (s, e) =>
            {
                if (tabControl.SelectedTab == page)
                    urlBar.Text = e.Uri;
            };

            wv.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                if (tabControl.SelectedTab == page)
                {
                    urlBar.Text = wv.Source.ToString();
                    page.Text = string.IsNullOrEmpty(wv.CoreWebView2.DocumentTitle)
                        ? "Tab" : TruncateTitle(wv.CoreWebView2.DocumentTitle);
                    UpdateNavButtons();
                }
            };

            wv.CoreWebView2.SourceChanged += (s, e) =>
            {
                if (tabControl.SelectedTab == page)
                    urlBar.Text = wv.Source.ToString();
            };

            if (url == "about:blank" || string.IsNullOrEmpty(url))
            {
                wv.CoreWebView2.NavigateToString(GetBraveSpeedDialHTML());
            }
            else
            {
                wv.CoreWebView2.Navigate(FormatUrl(url));
            }
        }

        private string TruncateTitle(string title)
        {
            if (title.Length > 18) return title.Substring(0, 15) + "...";
            return title;
        }

        private void CloseCurrentTab()
        {
            if (tabControl.TabPages.Count > 1)
            {
                TabPage current = tabControl.SelectedTab;
                WebView2 wv = GetWebView(current);
                if (wv != null) wv.Dispose();
                tabControl.TabPages.Remove(current);
            }
            else
            {
                WebView2 wv = GetCurrentWebView();
                if (wv != null && wv.CoreWebView2 != null)
                {
                    wv.CoreWebView2.NavigateToString(GetBraveSpeedDialHTML());
                    urlBar.Text = "";
                    tabControl.SelectedTab.Text = "New Tab";
                }
            }
        }

        private WebView2 GetCurrentWebView()
        {
            if (tabControl.SelectedTab != null && tabControl.SelectedTab.Controls.Count > 0)
            {
                return tabControl.SelectedTab.Controls[0] as WebView2;
            }
            return null;
        }

        private WebView2 GetWebView(TabPage page)
        {
            if (page != null && page.Controls.Count > 0)
                return page.Controls[0] as WebView2;
            return null;
        }

        private void OnTabChanged(object sender, EventArgs e)
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
            {
                urlBar.Text = wv.Source.ToString() == "about:blank" ? "" : wv.Source.ToString();
                UpdateNavButtons();
            }
        }

        private void UpdateNavButtons()
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
            {
                backBtn.Enabled = wv.CanGoBack;
                fwdBtn.Enabled = wv.CanGoForward;
            }
        }

        private void NavigateCurrentTab(string input)
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
            {
                string target = FormatUrl(input);
                wv.CoreWebView2.Navigate(target);
            }
        }

        private void ReloadCurrentTab()
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
                wv.Reload();
        }

        private string FormatUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input) || input == "about:blank")
                return "about:blank";

            if (input.StartsWith("http://") || input.StartsWith("https://") || input.StartsWith("file://"))
                return input;

            if (input.Contains(".") && !input.Contains(" "))
                return "https://" + input;

            return "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
        }

        private void SuspendAllWebViews()
        {
            foreach (TabPage page in tabControl.TabPages)
            {
                WebView2 wv = GetWebView(page);
                if (wv != null && wv.CoreWebView2 != null)
                    wv.CoreWebView2.TrySuspendAsync();
            }
        }

        private void ResumeActiveWebView()
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
                wv.CoreWebView2.Resume();
        }

        // ─── Network Ad-Blocking ──────────────────────────────────────────────────

        private void SetupNetworkBlocking(WebView2 wv)
        {
            string[] adDomains = {
                "doubleclick.net", "googlesyndication.com", "googleadservices.com",
                "2mdn.net", "moatads.com", "adnxs.com", "advertising.com",
                "taboola.com", "outbrain.com", "scorecardresearch.com",
                "hotjar.com", "mixpanel.com", "bat.bing.com", "demdex.net",
                "bluekai.com", "criteo.com", "adsrvr.org", "pubmatic.com",
                "rubiconproject.com", "openx.net", "amazon-adsystem.com",
                "connect.facebook.net", "an.facebook.com", "google-analytics.com",
                "adservice.google.com", "adservice.google.co.in",
                "googleads.g.doubleclick.net", "pubads.g.doubleclick.net"
            };

            foreach (string domain in adDomains)
            {
                wv.CoreWebView2.AddWebResourceRequestedFilter(
                    "*" + domain + "*", CoreWebView2WebResourceContext.All);
            }

            wv.CoreWebView2.WebResourceRequested += (s, e) =>
            {
                try
                {
                    totalBlockedAds++;
                    this.Invoke((Action)(() => shieldBtn.Text = "🦁 " + totalBlockedAds));

                    e.Response = wv.CoreWebView2.Environment.CreateWebResourceResponse(
                        new MemoryStream(new byte[0]), 200, "OK", "Content-Type: text/plain");
                }
                catch { }
            };
        }

        // ─── Injected Ad-Block JS ─────────────────────────────────────────────────

        private string GetAdBlockerJS()
        {
            return @"
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
        }

        // ─── Brave Light / Glassy Space Dark Speed Dial HTML ──────────────────────

        private string GetBraveSpeedDialHTML()
        {
            string bg = isDarkMode ? "#121216" : "linear-gradient(180deg,#ffffff 0%,#f5f5f7 100%)";
            string textColor = isDarkMode ? "#ffffff" : "#1d1d1f";
            string cardBg = isDarkMode ? "rgba(255,255,255,0.05)" : "rgba(255,255,255,0.85)";
            string cardBorder = isDarkMode ? "rgba(255,255,255,0.08)" : "rgba(0,0,0,0.06)";

            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,Ubuntu,sans-serif;background:" + bg + @";color:" + textColor + @";display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:100vh;padding:40px 20px;overflow-x:hidden;-webkit-font-smoothing:antialiased}

/* Header Branding */
.brand{display:flex;align-items:center;gap:14px;margin-bottom:28px;user-select:none;animation:fadeInDown .5s ease}
.brand-badge{width:54px;height:54px;border-radius:18px;background:linear-gradient(135deg,#ff5000 0%,#fb542b 100%);display:flex;align-items:center;justify-content:center;color:#ffffff;font-size:26px;font-weight:800;box-shadow:0 8px 24px rgba(255,80,0,0.3)}
.brand-title{font-size:38px;font-weight:800;letter-spacing:-0.5px;color:" + textColor + @"}
.brand-title span{color:#ff5000;font-weight:400}

/* Stats Pill Dashboard (Brave Lion Shield Style) */
.stats-bar{display:flex;gap:16px;margin-bottom:32px;animation:fadeIn .6s ease}
.stat-card{background:" + cardBg + @";border:1px solid " + cardBorder + @";backdrop-filter:blur(16px);border-radius:16px;padding:14px 22px;display:flex;align-items:center;gap:14px;box-shadow:0 4px 16px rgba(0,0,0,0.04);transition:all .2s ease}
.stat-card:hover{transform:translateY(-2px);box-shadow:0 8px 24px rgba(255,80,0,0.12);border-color:#ff5000}
.stat-icon{font-size:22px}
.stat-info{display:flex;flex-direction:column}
.stat-val{font-size:16px;font-weight:700;color:" + textColor + @"}
.stat-lbl{font-size:11.5px;color:#86868b;font-weight:500}

/* Search Box (Brave Lion Pill Design) */
.search-container{width:100%;max-width:620px;margin-bottom:40px;animation:fadeInUp .5s ease}
.search-box{display:flex;align-items:center;width:100%;height:52px;padding:0 20px;border-radius:26px;background:" + cardBg + @";border:1.5px solid " + cardBorder + @";backdrop-filter:blur(16px);box-shadow:0 4px 20px rgba(0,0,0,0.06);transition:all .25s ease}
.search-box:hover,.search-box:focus-within{box-shadow:0 8px 30px rgba(255,80,0,0.2);border-color:#ff5000}
.search-icon{color:#ff5000;font-size:18px;margin-right:14px}
.search-box input{flex:1;background:transparent;border:none;outline:none;color:" + textColor + @";font-size:16px;font-weight:400}
.search-box input::placeholder{color:#86868b}
.search-box button{background:linear-gradient(135deg,#ff5000 0%,#fb542b 100%);border:none;color:#ffffff;font-weight:600;font-size:14.5px;cursor:pointer;padding:0 22px;border-radius:20px;height:38px;box-shadow:0 3px 10px rgba(255,80,0,0.3);transition:all .15s ease}
.search-box button:hover{transform:scale(1.03);box-shadow:0 6px 16px rgba(255,80,0,0.4)}

/* Speed Dial Grid */
.dials-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:20px;width:100%;max-width:640px;animation:fadeInUp .6s ease}
.dial{display:flex;flex-direction:column;align-items:center;gap:12px;padding:18px;border-radius:18px;background:" + cardBg + @";border:1px solid " + cardBorder + @";backdrop-filter:blur(16px);cursor:pointer;transition:all .2s ease;text-decoration:none;color:" + textColor + @";box-shadow:0 2px 8px rgba(0,0,0,0.03)}
.dial:hover{transform:translateY(-4px) scale(1.02);border-color:#ff5000;box-shadow:0 12px 32px rgba(255,80,0,0.15)}
.dial-icon{width:52px;height:52px;border-radius:16px;display:flex;align-items:center;justify-content:center;font-size:22px;font-weight:700;box-shadow:0 2px 8px rgba(0,0,0,0.08);transition:transform .2s ease}
.dial:hover .dial-icon{transform:scale(1.08)}
.dial-label{font-size:12.5px;font-weight:600;color:" + textColor + @"}

/* Footer info */
.footer-note{margin-top:40px;font-size:12px;color:#86868b;font-weight:500;display:flex;align-items:center;gap:16px;animation:fadeIn .7s ease}
.footer-tag{display:flex;align-items:center;gap:6px}

@keyframes fadeInDown{from{opacity:0;transform:translateY(-12px)}to{opacity:1;transform:translateY(0)}}
@keyframes fadeInUp{from{opacity:0;transform:translateY(12px)}to{opacity:1;transform:translateY(0)}}
@keyframes fadeIn{from{opacity:0}to{opacity:1}}
</style>
</head>
<body>

<div class='brand'>
  <div class='brand-badge'>🦁</div>
  <div class='brand-title'>BLACK <span>BROWSER</span></div>
</div>

<div class='stats-bar'>
  <div class='stat-card'>
    <div class='stat-icon'>🛡️</div>
    <div class='stat-info'>
      <div class='stat-val'>Brave Shield</div>
      <div class='stat-lbl'>Zero Ads & Trackers</div>
    </div>
  </div>
  <div class='stat-card'>
    <div class='stat-icon'>⚡</div>
    <div class='stat-info'>
      <div class='stat-val'>Ultra-Fast</div>
      <div class='stat-lbl'>Low RAM Engine</div>
    </div>
  </div>
  <div class='stat-card'>
    <div class='stat-icon'>🧩</div>
    <div class='stat-info'>
      <div class='stat-val'>Extensions</div>
      <div class='stat-lbl'>Chrome Store Ready</div>
    </div>
  </div>
</div>

<form class='search-container' action='https://www.google.com/search' method='get'>
  <div class='search-box'>
    <span class='search-icon'>🦁</span>
    <input type='text' name='q' placeholder='Search Google or type a URL...' autofocus autocomplete='off'>
    <button type='submit'>Search</button>
  </div>
</form>

<div class='dials-grid'>
  <a class='dial' href='https://www.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>G</div><div class='dial-label'>Google</div></a>
  <a class='dial' href='https://www.youtube.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>Y</div><div class='dial-label'>YouTube</div></a>
  <a class='dial' href='https://music.youtube.com'><div class='dial-icon' style='background:#fef7e0;color:#f29900'>M</div><div class='dial-label'>YT Music</div></a>
  <a class='dial' href='https://chromewebstore.google.com'><div class='dial-icon' style='background:#fff0eb;color:#ff5000'>🧩</div><div class='dial-label'>Chrome Store</div></a>
  <a class='dial' href='https://github.com'><div class='dial-icon' style='background:#e8eaed;color:#202124'>GH</div><div class='dial-label'>GitHub</div></a>
  <a class='dial' href='https://reddit.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>R</div><div class='dial-label'>Reddit</div></a>
  <a class='dial' href='https://chatgpt.com'><div class='dial-icon' style='background:#e6f4ea;color:#107c41'>AI</div><div class='dial-label'>ChatGPT</div></a>
  <a class='dial' href='https://microsoftedge.microsoft.com/addons'><div class='dial-icon' style='background:#ebf3fc;color:#0078d4'>🛒</div><div class='dial-label'>Edge Add-ons</div></a>
</div>

<div class='footer-note'>
  <span class='footer-tag'>🦁 Brave Light & Windows 11 Fluent UI</span>
  <span>•</span>
  <span class='footer-tag'>👁️ Eye Care Filter Ready</span>
  <span>•</span>
  <span class='footer-tag'>⚡ ~40MB Tray RAM</span>
</div>

</body>
</html>";
        }

        // ─── System Tray Setup ────────────────────────────────────────────────────

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Black Browser (Brave Light)";

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
                trayIcon.Icon = new Icon(iconPath);

            var menu = new ContextMenuStrip();
            menu.Items.Add("Show Black Browser", null, (s, e) => ShowMainWindow());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Quit", null, (s, e) =>
            {
                trayIcon.Visible = false;
                Application.Exit();
            });

            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;

            trayIcon.MouseDoubleClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    ShowMainWindow();
            };
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        // ─── Keyboard Shortcuts ───────────────────────────────────────────────────

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T)
            {
                e.SuppressKeyPress = true;
                AddNewTab("New Tab", "about:blank");
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                e.SuppressKeyPress = true;
                CloseCurrentTab();
            }
            else if (e.Control && e.KeyCode == Keys.L)
            {
                e.SuppressKeyPress = true;
                urlBar.Focus();
                urlBar.SelectAll();
            }
            else if (e.Control && e.KeyCode == Keys.R || e.KeyCode == Keys.F5)
            {
                e.SuppressKeyPress = true;
                ReloadCurrentTab();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.E)
            {
                e.SuppressKeyPress = true;
                CycleEyeCareMode();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.D)
            {
                e.SuppressKeyPress = true;
                ToggleTheme();
            }
            else if (e.Alt && e.KeyCode == Keys.Left)
            {
                e.SuppressKeyPress = true;
                WebView2 wv = GetCurrentWebView();
                if (wv != null && wv.CanGoBack) wv.GoBack();
            }
            else if (e.Alt && e.KeyCode == Keys.Right)
            {
                e.SuppressKeyPress = true;
                WebView2 wv = GetCurrentWebView();
                if (wv != null && wv.CanGoForward) wv.GoForward();
            }
            else if (e.KeyCode == Keys.F11)
            {
                e.SuppressKeyPress = true;
                ToggleFullscreen();
            }
        }

        private bool isFullscreen = false;
        private FormWindowState prevWindowState;
        private FormBorderStyle prevBorderStyle;

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                prevWindowState = this.WindowState;
                prevBorderStyle = this.FormBorderStyle;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                topPanel.Visible = false;
                isFullscreen = true;
            }
            else
            {
                this.FormBorderStyle = prevBorderStyle;
                this.WindowState = prevWindowState;
                topPanel.Visible = true;
                isFullscreen = false;
            }
        }

        // ─── Form Lifecycle ───────────────────────────────────────────────────────

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                SuspendAllWebViews();
                TrimMemory();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (eyeCareOverlay != null) eyeCareOverlay.Dispose();
                if (trayIcon       != null) { trayIcon.Visible = false; trayIcon.Dispose(); }
                if (gcTimer        != null) gcTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
