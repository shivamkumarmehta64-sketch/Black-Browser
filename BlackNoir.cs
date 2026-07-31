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

namespace BlackNoirBrowser
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
            const string appName = "BlackNoir_SingleInstance_Mutex_9b2d0d52";
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
        private Button newTabBtn;
        private Button closeTabBtn;
        private NotifyIcon trayIcon;
        private System.Windows.Forms.Timer gcTimer;

        private CoreWebView2Environment webViewEnv;
        private int totalBlockedAds = 0;
        private string logPath;

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        public MainForm()
        {
            logPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "debug.log");
            Log("=== Black-Noir Browser (Chrome Light Edition) starting ===");

            this.Text = "Black-Noir Browser (Chrome Light)";
            this.Width = 1280;
            this.Height = 820;
            this.BackColor = Color.FromArgb(241, 243, 244); // Chrome light background
            this.MinimumSize = new Size(900, 600);

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);

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

        // ─── Chrome Light UI Setup ────────────────────────────────────────────────

        private void InitializeUI()
        {
            // Top Panel (Chrome Light Header - #DEE1E6)
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 46;
            topPanel.BackColor = Color.FromArgb(255, 255, 255); // Chrome toolbar white
            topPanel.Padding = new Padding(6, 6, 6, 6);

            // Nav Panel layout
            navPanel = new Panel();
            navPanel.Dock = DockStyle.Fill;
            navPanel.BackColor = Color.FromArgb(255, 255, 255);

            backBtn = CreateChromeButton("←", "Back (Alt+Left)", 0);
            fwdBtn = CreateChromeButton("→", "Forward (Alt+Right)", 36);
            reloadBtn = CreateChromeButton("↻", "Reload (Ctrl+R / F5)", 72);

            backBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoBack) wv.GoBack(); };
            fwdBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoForward) wv.GoForward(); };
            reloadBtn.Click += (s, e) => ReloadCurrentTab();

            // Chrome Pill Address Bar
            urlBar = new TextBox();
            urlBar.Location = new Point(112, 6);
            urlBar.Width = this.Width - 325;
            urlBar.Height = 30;
            urlBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            urlBar.BackColor = Color.FromArgb(241, 243, 244); // Chrome light URL bar fill
            urlBar.ForeColor = Color.FromArgb(32, 33, 36);      // Dark text
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

            // Shield Button (Chrome Green Badge)
            shieldBtn = new Button();
            shieldBtn.Text = "🛡 0";
            shieldBtn.Width = 65;
            shieldBtn.Height = 30;
            shieldBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            shieldBtn.Location = new Point(this.Width - 200, 5);
            shieldBtn.FlatStyle = FlatStyle.Flat;
            shieldBtn.FlatAppearance.BorderSize = 0;
            shieldBtn.BackColor = Color.FromArgb(230, 244, 234);
            shieldBtn.ForeColor = Color.FromArgb(19, 115, 51);
            shieldBtn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            shieldBtn.Cursor = Cursors.Hand;

            // New Tab Button
            newTabBtn = new Button();
            newTabBtn.Text = "+";
            newTabBtn.Width = 32;
            newTabBtn.Height = 30;
            newTabBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            newTabBtn.Location = new Point(this.Width - 130, 5);
            newTabBtn.FlatStyle = FlatStyle.Flat;
            newTabBtn.FlatAppearance.BorderSize = 0;
            newTabBtn.BackColor = Color.FromArgb(241, 243, 244);
            newTabBtn.ForeColor = Color.FromArgb(60, 64, 67);
            newTabBtn.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            newTabBtn.Cursor = Cursors.Hand;
            newTabBtn.Click += (s, e) => AddNewTab("New Tab", "about:blank");

            // Close Tab Button
            closeTabBtn = new Button();
            closeTabBtn.Text = "✕";
            closeTabBtn.Width = 32;
            closeTabBtn.Height = 30;
            closeTabBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            closeTabBtn.Location = new Point(this.Width - 92, 5);
            closeTabBtn.FlatStyle = FlatStyle.Flat;
            closeTabBtn.FlatAppearance.BorderSize = 0;
            closeTabBtn.BackColor = Color.FromArgb(252, 232, 230);
            closeTabBtn.ForeColor = Color.FromArgb(217, 48, 37);
            closeTabBtn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            closeTabBtn.Cursor = Cursors.Hand;
            closeTabBtn.Click += (s, e) => CloseCurrentTab();

            navPanel.Controls.Add(backBtn);
            navPanel.Controls.Add(fwdBtn);
            navPanel.Controls.Add(reloadBtn);
            navPanel.Controls.Add(urlBar);
            navPanel.Controls.Add(shieldBtn);
            navPanel.Controls.Add(newTabBtn);
            navPanel.Controls.Add(closeTabBtn);
            topPanel.Controls.Add(navPanel);

            // TabControl (Chrome Light styling)
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
                    urlBar.Width = Math.Max(200, this.Width - 325);

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

        private Button CreateChromeButton(string text, string tooltip, int left)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = new Point(left + 4, 5);
            b.Width = 30;
            b.Height = 30;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(255, 255, 255);
            b.ForeColor = Color.FromArgb(95, 99, 104);
            b.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            return b;
        }

        // ─── Environment Initialization ──────────────────────────────────────────

        private async void InitializeBrowserEnv()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "blacknoir-webview2");

                var options = new CoreWebView2EnvironmentOptions(
                    "--disk-cache-size=33554432 " +       // 32 MB disk cache
                    "--media-cache-size=33554432 " +      // 32 MB media cache
                    "--renderer-process-limit=1 " +       // max 1 renderer process
                    "--disable-extensions " +             // no extensions overhead
                    "--disable-background-networking " +  // less background traffic
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
            page.BackColor = Color.White;

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
                        shieldBtn.Text = "🛡 " + totalBlockedAds;
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
                wv.CoreWebView2.NavigateToString(GetChromeLightSpeedDialHTML());
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
                    wv.CoreWebView2.NavigateToString(GetChromeLightSpeedDialHTML());
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
                    this.Invoke((Action)(() => shieldBtn.Text = "🛡 " + totalBlockedAds));

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

        // ─── Chrome Light Speed Dial HTML Page ─────────────────────────────────────

        private string GetChromeLightSpeedDialHTML()
        {
            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:#ffffff;color:#202124;display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;overflow:hidden;-webkit-font-smoothing:antialiased}
.logo{font-size:56px;font-weight:600;letter-spacing:-1px;margin-bottom:28px;display:flex;align-items:center;gap:2px;user-select:none}
.g-blue{color:#4285f4}.g-red{color:#ea4335}.g-yellow{color:#fbbc05}.g-green{color:#34a853}
.search-container{width:100%;max-width:584px;margin-bottom:32px}
.search-box{display:flex;align-items:center;width:100%;height:46px;padding:0 16px;border-radius:24px;background:#ffffff;border:1px solid #dfe1e5;box-shadow:0 1px 6px rgba(32,33,36,0.12);transition:box-shadow .2s ease,border-color .2s ease}
.search-box:hover,.search-box:focus-within{box-shadow:0 1px 6px rgba(32,33,36,0.28);border-color:transparent}
.search-icon{color:#9aa0a6;font-size:16px;margin-right:12px}
.search-box input{flex:1;background:transparent;border:none;outline:none;color:#202124;font-size:16px}
.search-box button{background:none;border:none;color:#1a73e8;font-weight:600;font-size:14px;cursor:pointer;padding:0 8px}
.dials{display:grid;grid-template-columns:repeat(3,1fr);gap:20px;width:100%;max-width:560px}
.dial{display:flex;flex-direction:column;align-items:center;gap:12px;padding:16px;border-radius:12px;cursor:pointer;transition:all .15s ease;text-decoration:none;color:#3c4043}
.dial:hover{background:#f1f3f4}
.dial-icon{width:48px;height:48px;border-radius:50%;background:#f1f3f4;display:flex;align-items:center;justify-content:center;font-size:20px;font-weight:bold;color:#1a73e8;box-shadow:0 1px 3px rgba(0,0,0,0.1)}
.dial-label{font-size:12px;color:#3c4043;font-weight:500;text-align:center}
</style>
</head>
<body>
<div class='logo'>
  <span class='g-blue'>G</span><span class='g-red'>o</span><span class='g-yellow'>o</span><span class='g-blue'>g</span><span class='g-green'>l</span><span class='g-red'>e</span>
</div>
<form class='search-container' action='https://www.google.com/search' method='get'>
  <div class='search-box'>
    <span class='search-icon'>🔍</span>
    <input type='text' name='q' placeholder='Search Google or type a URL' autofocus autocomplete='off'>
    <button type='submit'>Search</button>
  </div>
</form>
<div class='dials'>
  <a class='dial' href='https://www.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>G</div><div class='dial-label'>Google</div></a>
  <a class='dial' href='https://www.youtube.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>Y</div><div class='dial-label'>YouTube</div></a>
  <a class='dial' href='https://music.youtube.com'><div class='dial-icon' style='background:#fef7e0;color:#f29900'>M</div><div class='dial-label'>YT Music</div></a>
  <a class='dial' href='https://github.com'><div class='dial-icon' style='background:#e8eaed;color:#202124'>GH</div><div class='dial-label'>GitHub</div></a>
  <a class='dial' href='https://reddit.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>R</div><div class='dial-label'>Reddit</div></a>
  <a class='dial' href='https://chatgpt.com'><div class='dial-icon' style='background:#e6f4ea;color:#137333'>AI</div><div class='dial-label'>ChatGPT</div></a>
</div>
</body>
</html>";
        }

        // ─── System Tray Setup ────────────────────────────────────────────────────

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Black-Noir Browser (Chrome Light)";

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
                trayIcon.Icon = new Icon(iconPath);

            var menu = new ContextMenuStrip();
            menu.Items.Add("Show Black-Noir Browser", null, (s, e) => ShowMainWindow());
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
                if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); }
                if (gcTimer  != null) gcTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
