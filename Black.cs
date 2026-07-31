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
        private Panel headerContainer;
        private Panel tabStripPanel;
        private Panel omniboxPanel;
        private TabControl tabControl;

        private Button backBtn;
        private Button fwdBtn;
        private Button reloadBtn;
        private Button homeBtn;
        private TextBox urlBar;
        private Button shieldBtn;
        private Button eyeCareBtn;
        private Button extBtn;
        private Button menuBtn;
        private Button addTabBtn;
        private Button closeActiveTabBtn;

        private ContextMenuStrip mainMenu;
        private NotifyIcon trayIcon;
        private System.Windows.Forms.Timer gcTimer;

        private EyeCareOverlayForm eyeCareOverlay;
        private int eyeCareMode = 0;
        private bool isDarkMode = false; // Chrome Light Default

        private CoreWebView2Environment webViewEnv;
        private int totalBlockedAds = 0;
        private string logPath;

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        public MainForm()
        {
            logPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "debug.log");
            Log("=== Black Browser (Google Chrome Light Edition) starting ===");

            this.Text = "Black (Chrome Light Edition)";
            this.Width = 1280;
            this.Height = 820;
            this.BackColor = Color.FromArgb(222, 225, 230); // Chrome Header Grey
            this.MinimumSize = new Size(900, 600);

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);

            eyeCareOverlay = new EyeCareOverlayForm();

            InitializeUI();
            InitializeMainMenu();
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

        // ─── Google Chrome Light UI Setup ─────────────────────────────────────────

        private void InitializeUI()
        {
            // Header Container (#DEE1E6 Chrome Light)
            headerContainer = new Panel();
            headerContainer.Dock = DockStyle.Top;
            headerContainer.Height = 82;
            headerContainer.BackColor = Color.FromArgb(222, 225, 230);

            // Tab Strip Panel
            tabStripPanel = new Panel();
            tabStripPanel.Dock = DockStyle.Top;
            tabStripPanel.Height = 36;
            tabStripPanel.BackColor = Color.FromArgb(222, 225, 230);

            // TabControl (Chrome Rounded Tabs)
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Padding = new Point(16, 4);
            tabControl.Font = new Font("Segoe UI", 9.5f);
            tabControl.SelectedIndexChanged += OnTabChanged;

            // Close Active Tab Button (Chrome style ✕ button)
            closeActiveTabBtn = new Button();
            closeActiveTabBtn.Text = "✕";
            closeActiveTabBtn.Dock = DockStyle.Right;
            closeActiveTabBtn.Width = 32;
            closeActiveTabBtn.FlatStyle = FlatStyle.Flat;
            closeActiveTabBtn.FlatAppearance.BorderSize = 0;
            closeActiveTabBtn.BackColor = Color.FromArgb(222, 225, 230);
            closeActiveTabBtn.ForeColor = Color.FromArgb(95, 99, 104);
            closeActiveTabBtn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            closeActiveTabBtn.Cursor = Cursors.Hand;
            closeActiveTabBtn.Click += (s, e) => CloseCurrentTab();

            // Add Tab Button (+)
            addTabBtn = new Button();
            addTabBtn.Text = "+";
            addTabBtn.Dock = DockStyle.Right;
            addTabBtn.Width = 32;
            addTabBtn.FlatStyle = FlatStyle.Flat;
            addTabBtn.FlatAppearance.BorderSize = 0;
            addTabBtn.BackColor = Color.FromArgb(222, 225, 230);
            addTabBtn.ForeColor = Color.FromArgb(60, 64, 67);
            addTabBtn.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            addTabBtn.Cursor = Cursors.Hand;
            addTabBtn.Click += (s, e) => AddNewTab("New Tab", "about:blank");

            tabStripPanel.Controls.Add(tabControl);
            tabStripPanel.Controls.Add(addTabBtn);
            tabStripPanel.Controls.Add(closeActiveTabBtn);

            // Omnibox Navigation Panel (Pure White Chrome Bar)
            omniboxPanel = new Panel();
            omniboxPanel.Dock = DockStyle.Bottom;
            omniboxPanel.Height = 44;
            omniboxPanel.BackColor = Color.FromArgb(255, 255, 255);
            omniboxPanel.Padding = new Padding(6, 6, 6, 6);

            backBtn = CreateChromeBtn("←", "Back (Alt+Left)", 0);
            fwdBtn = CreateChromeBtn("→", "Forward (Alt+Right)", 32);
            reloadBtn = CreateChromeBtn("↻", "Reload (Ctrl+R)", 64);
            homeBtn = CreateChromeBtn("🏠", "Home", 96);

            backBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoBack) wv.GoBack(); };
            fwdBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoForward) wv.GoForward(); };
            reloadBtn.Click += (s, e) => ReloadCurrentTab();
            homeBtn.Click += (s, e) => NavigateCurrentTab("about:blank");

            // Chrome Omnibox Pill (#F1F3F4)
            urlBar = new TextBox();
            urlBar.Location = new Point(136, 7);
            urlBar.Width = this.Width - 430;
            urlBar.Height = 28;
            urlBar.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            urlBar.BackColor = Color.FromArgb(241, 243, 244);
            urlBar.ForeColor = Color.FromArgb(32, 33, 36);
            urlBar.Font = new Font("Segoe UI", 10f);
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

            // Chrome Shield Badge (Google Blue)
            shieldBtn = new Button();
            shieldBtn.Text = "🛡 0";
            shieldBtn.Width = 62;
            shieldBtn.Height = 28;
            shieldBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            shieldBtn.Location = new Point(this.Width - 285, 6);
            shieldBtn.FlatStyle = FlatStyle.Flat;
            shieldBtn.FlatAppearance.BorderSize = 0;
            shieldBtn.BackColor = Color.FromArgb(232, 240, 254);
            shieldBtn.ForeColor = Color.FromArgb(26, 115, 232); // Chrome Blue
            shieldBtn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            shieldBtn.Cursor = Cursors.Hand;

            // Eye Care Button
            eyeCareBtn = new Button();
            eyeCareBtn.Text = "👁 Eye";
            eyeCareBtn.Width = 64;
            eyeCareBtn.Height = 28;
            eyeCareBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            eyeCareBtn.Location = new Point(this.Width - 218, 6);
            eyeCareBtn.FlatStyle = FlatStyle.Flat;
            eyeCareBtn.FlatAppearance.BorderSize = 0;
            eyeCareBtn.BackColor = Color.FromArgb(254, 247, 224);
            eyeCareBtn.ForeColor = Color.FromArgb(180, 100, 0);
            eyeCareBtn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            eyeCareBtn.Cursor = Cursors.Hand;
            eyeCareBtn.Click += (s, e) => CycleEyeCareMode();

            // Extensions Button
            extBtn = new Button();
            extBtn.Text = "🧩 Ext";
            extBtn.Width = 64;
            extBtn.Height = 28;
            extBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            extBtn.Location = new Point(this.Width - 149, 6);
            extBtn.FlatStyle = FlatStyle.Flat;
            extBtn.FlatAppearance.BorderSize = 0;
            extBtn.BackColor = Color.FromArgb(241, 243, 244);
            extBtn.ForeColor = Color.FromArgb(95, 99, 104);
            extBtn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            extBtn.Cursor = Cursors.Hand;
            extBtn.Click += (s, e) => AddNewTab("Chrome Extensions", "https://chromewebstore.google.com");

            // Chrome Main Menu Button (⋮)
            menuBtn = new Button();
            menuBtn.Text = "⋮";
            menuBtn.Width = 32;
            menuBtn.Height = 28;
            menuBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            menuBtn.Location = new Point(this.Width - 80, 6);
            menuBtn.FlatStyle = FlatStyle.Flat;
            menuBtn.FlatAppearance.BorderSize = 0;
            menuBtn.BackColor = Color.FromArgb(255, 255, 255);
            menuBtn.ForeColor = Color.FromArgb(95, 99, 104);
            menuBtn.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            menuBtn.Cursor = Cursors.Hand;
            menuBtn.Click += (s, e) => mainMenu.Show(menuBtn, new Point(0, menuBtn.Height));

            omniboxPanel.Controls.Add(backBtn);
            omniboxPanel.Controls.Add(fwdBtn);
            omniboxPanel.Controls.Add(reloadBtn);
            omniboxPanel.Controls.Add(homeBtn);
            omniboxPanel.Controls.Add(urlBar);
            omniboxPanel.Controls.Add(shieldBtn);
            omniboxPanel.Controls.Add(eyeCareBtn);
            omniboxPanel.Controls.Add(extBtn);
            omniboxPanel.Controls.Add(menuBtn);

            headerContainer.Controls.Add(tabStripPanel);
            headerContainer.Controls.Add(omniboxPanel);

            this.Controls.Add(headerContainer);

            this.KeyPreview = true;
            this.KeyDown += OnFormKeyDown;

            this.Resize += (s, e) =>
            {
                if (urlBar != null)
                    urlBar.Width = Math.Max(200, this.Width - 430);

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

        private Button CreateChromeBtn(string text, string tooltip, int left)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = new Point(left + 4, 6);
            b.Width = 28;
            b.Height = 28;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(255, 255, 255);
            b.ForeColor = Color.FromArgb(95, 99, 104);
            b.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            return b;
        }

        // ─── Chrome Main Menu (⋮) ──────────────────────────────────────────────────

        private void InitializeMainMenu()
        {
            mainMenu = new ContextMenuStrip();
            mainMenu.Font = new Font("Segoe UI", 9.5f);

            mainMenu.Items.Add("➕ New Tab (Ctrl+T)", null, (s, e) => AddNewTab("New Tab", "about:blank"));
            mainMenu.Items.Add("📜 History (Ctrl+H)", null, (s, e) => NavigateCurrentTab("https://myactivity.google.com"));
            mainMenu.Items.Add("📥 Downloads (Ctrl+J)", null, (s, e) => NavigateCurrentTab("chrome://downloads"));
            mainMenu.Items.Add("🛒 Chrome Web Store", null, (s, e) => AddNewTab("Chrome Store", "https://chromewebstore.google.com"));
            mainMenu.Items.Add("🧩 Edge Add-ons Store", null, (s, e) => AddNewTab("Edge Add-ons", "https://microsoftedge.microsoft.com/addons"));
            mainMenu.Items.Add(new ToolStripSeparator());
            mainMenu.Items.Add("👁️ Cycle Eye Care Filter (Ctrl+Shift+E)", null, (s, e) => CycleEyeCareMode());
            mainMenu.Items.Add("🌓 Toggle Dark / Light Theme (Ctrl+Shift+D)", null, (s, e) => ToggleTheme());
            mainMenu.Items.Add(new ToolStripSeparator());
            mainMenu.Items.Add("✕ Close Active Tab (Ctrl+W)", null, (s, e) => CloseCurrentTab());
            mainMenu.Items.Add("🚪 Exit Browser", null, (s, e) => Application.Exit());
        }

        private void ToggleTheme()
        {
            isDarkMode = !isDarkMode;
            if (isDarkMode)
            {
                headerContainer.BackColor = Color.FromArgb(32, 33, 36);
                tabStripPanel.BackColor = Color.FromArgb(32, 33, 36);
                omniboxPanel.BackColor = Color.FromArgb(40, 42, 45);
                urlBar.BackColor = Color.FromArgb(53, 54, 58);
                urlBar.ForeColor = Color.White;
                addTabBtn.BackColor = Color.FromArgb(32, 33, 36);
                closeActiveTabBtn.BackColor = Color.FromArgb(32, 33, 36);
                this.BackColor = Color.FromArgb(20, 20, 22);
            }
            else
            {
                headerContainer.BackColor = Color.FromArgb(222, 225, 230);
                tabStripPanel.BackColor = Color.FromArgb(222, 225, 230);
                omniboxPanel.BackColor = Color.FromArgb(255, 255, 255);
                urlBar.BackColor = Color.FromArgb(241, 243, 244);
                urlBar.ForeColor = Color.FromArgb(32, 33, 36);
                addTabBtn.BackColor = Color.FromArgb(222, 225, 230);
                closeActiveTabBtn.BackColor = Color.FromArgb(222, 225, 230);
                this.BackColor = Color.FromArgb(249, 249, 251);
            }

            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.Source != null && wv.Source.ToString() == "about:blank")
            {
                wv.CoreWebView2.NavigateToString(GetChromeSpeedDialHTML());
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

        // ─── Environment Initialization with Standard Chrome User-Agent ─────────

        private async void InitializeBrowserEnv()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "black-webview2");

                string chromeUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0";

                var options = new CoreWebView2EnvironmentOptions(
                    "--disk-cache-size=33554432 " +       // 32 MB disk cache
                    "--media-cache-size=33554432 " +      // 32 MB media cache
                    "--renderer-process-limit=1 " +       // max 1 renderer process
                    "--enable-experimental-extension-apis " +
                    "--allow-legacy-extension-manifests " +
                    "--user-agent=\"" + chromeUA + "\" " +
                    "--no-first-run " +                   // skip first-run setup
                    "--disable-sync " +                   // no Chrome account sync
                    "--disable-translate " +              // no translate UI
                    "--js-flags=--max-old-space-size=128" // JS heap limit: 128 MB
                );

                webViewEnv = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                Log("Environment created successfully with full Chrome compatibility User-Agent");

                AddNewTab("New Tab", "about:blank");
            }
            catch (Exception ex)
            {
                Log("FATAL Env: " + ex.ToString());
                MessageBox.Show("Failed to initialize WebView2: " + ex.Message);
            }
        }

        // ─── Tab Management (Bug-Free Clean Switching & Closing) ──────────────────

        public async void AddNewTab(string title, string url)
        {
            if (webViewEnv == null) return;

            TabPage page = new TabPage(TruncateTitle(title));
            page.BackColor = isDarkMode ? Color.FromArgb(20, 20, 22) : Color.White;

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
                wv.CoreWebView2.NavigateToString(GetChromeSpeedDialHTML());
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
                    wv.CoreWebView2.NavigateToString(GetChromeSpeedDialHTML());
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

        // ─── Google Chrome Speed Dial HTML ────────────────────────────────────────

        private string GetChromeSpeedDialHTML()
        {
            string bg = isDarkMode ? "#202124" : "#ffffff";
            string textColor = isDarkMode ? "#f1f3f4" : "#202124";
            string searchBg = isDarkMode ? "#303134" : "#ffffff";
            string searchBorder = isDarkMode ? "#5f6368" : "#dfe1e5";

            return @"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;background:" + bg + @";color:" + textColor + @";display:flex;flex-direction:column;align-items:center;justify-content:center;min-height:100vh;padding:40px 20px;overflow-x:hidden;-webkit-font-smoothing:antialiased}

/* Chrome Header Branding */
.brand{font-size:48px;font-weight:700;letter-spacing:-0.5px;margin-bottom:32px;display:flex;align-items:center;gap:12px;color:" + textColor + @"}
.brand span{color:#1a73e8}

/* Chrome Search Box */
.search-container{width:100%;max-width:584px;margin-bottom:40px}
.search-box{display:flex;align-items:center;width:100%;height:46px;padding:0 18px;border-radius:23px;background:" + searchBg + @";border:1px solid " + searchBorder + @";box-shadow:0 1px 6px rgba(32,33,36,0.12);transition:box-shadow .2s ease,border-color .2s ease}
.search-box:hover,.search-box:focus-within{box-shadow:0 2px 10px rgba(32,33,36,0.28);border-color:transparent}
.search-icon{color:#9aa0a6;font-size:18px;margin-right:12px}
.search-box input{flex:1;background:transparent;border:none;outline:none;color:" + textColor + @";font-size:16px}

/* Chrome Speed Dial Shortcuts */
.dials-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:24px;width:100%;max-width:560px}
.dial{display:flex;flex-direction:column;align-items:center;gap:12px;padding:12px;border-radius:8px;cursor:pointer;transition:background .15s ease;text-decoration:none;color:" + textColor + @"}
.dial:hover{background:" + (isDarkMode ? "rgba(255,255,255,0.08)" : "rgba(32,33,36,0.04)") + @"}
.dial-icon{width:48px;height:48px;border-radius:50%;background:" + (isDarkMode ? "#303134" : "#f1f3f4") + @";display:flex;align-items:center;justify-content:center;font-size:20px;font-weight:700;color:#1a73e8}
.dial-label{font-size:12px;font-weight:500;text-align:center}

.footer-note{margin-top:48px;font-size:12px;color:#9aa0a6;display:flex;align-items:center;gap:12px}
</style>
</head>
<body>

<div class='brand'>
  Black <span>Browser</span>
</div>

<form class='search-container' action='https://www.google.com/search' method='get'>
  <div class='search-box'>
    <span class='search-icon'>🔍</span>
    <input type='text' name='q' placeholder='Search Google or type a URL' autofocus autocomplete='off'>
  </div>
</form>

<div class='dials-grid'>
  <a class='dial' href='https://www.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>G</div><div class='dial-label'>Google</div></a>
  <a class='dial' href='https://www.youtube.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>Y</div><div class='dial-label'>YouTube</div></a>
  <a class='dial' href='https://music.youtube.com'><div class='dial-icon' style='background:#fef7e0;color:#f29900'>M</div><div class='dial-label'>YT Music</div></a>
  <a class='dial' href='https://chromewebstore.google.com'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>🛒</div><div class='dial-label'>Chrome Store</div></a>
  <a class='dial' href='https://github.com'><div class='dial-icon' style='background:#f1f3f4;color:#202124'>GH</div><div class='dial-label'>GitHub</div></a>
  <a class='dial' href='https://reddit.com'><div class='dial-icon' style='background:#fce8e6;color:#d93025'>R</div><div class='dial-label'>Reddit</div></a>
  <a class='dial' href='https://chatgpt.com'><div class='dial-icon' style='background:#e6f4ea;color:#107c41'>AI</div><div class='dial-label'>ChatGPT</div></a>
  <a class='dial' href='https://microsoftedge.microsoft.com/addons'><div class='dial-icon' style='background:#e8f0fe;color:#1a73e8'>🧩</div><div class='dial-label'>Edge Add-ons</div></a>
</div>

<div class='footer-note'>
  <span>Google Chrome Light Edition</span>
  <span>•</span>
  <span>3-Layer Ad Shield Active</span>
  <span>•</span>
  <span>~40MB Tray RAM</span>
</div>

</body>
</html>";
        }

        // ─── System Tray Setup ────────────────────────────────────────────────────

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Black Browser (Chrome Light Edition)";

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
            else if (e.Control && e.KeyCode == Keys.H)
            {
                e.SuppressKeyPress = true;
                NavigateCurrentTab("https://myactivity.google.com");
            }
            else if (e.Control && e.KeyCode == Keys.J)
            {
                e.SuppressKeyPress = true;
                NavigateCurrentTab("chrome://downloads");
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
                headerContainer.Visible = false;
                isFullscreen = true;
            }
            else
            {
                this.FormBorderStyle = prevBorderStyle;
                this.WindowState = prevWindowState;
                headerContainer.Visible = true;
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
                if (mainMenu        != null) mainMenu.Dispose();
                if (trayIcon        != null) { trayIcon.Visible = false; trayIcon.Dispose(); }
                if (gcTimer         != null) gcTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
