using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackBrowser
{
    public class BrowserForm : Form
    {
        private Panel headerContainer;
        private Panel omniboxPanel;
        private FlowLayoutPanel actionsPanel;
        private Panel softBanner;
        private Label softBannerLabel;
        private TabControl tabControl;

        private Button backBtn;
        private Button fwdBtn;
        private Button reloadBtn;
        private Button homeBtn;
        private TextBox urlBar;
        private Button starBtn;
        private Button shieldBtn;
        private Button eyeCareBtn;
        private Button notesBtn;
        private Button settingsBtn;
        private Button extBtn;
        private Button menuBtn;
        private Button addTabBtn;

        private ContextMenuStrip mainMenu;
        private NotifyIcon trayIcon;
        private Timer gcTimer;
        private Timer bannerTimer;

        private EyeCareOverlayForm eyeCareOverlay;
        private int eyeCareMode = 0;
        private bool isDarkMode = false;

        private CoreWebView2Environment webViewEnv;
        private int totalBlockedAds = 0;
        private string logPath;

        public BrowserForm()
        {
            logPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "debug.log");
            Log("=== Black Browser starting (YouTube Ad-Free Launcher v8.2) ===");

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;

            this.Text = "Black Browser";
            this.Width = 1280;
            this.Height = 820;
            this.BackColor = Color.FromArgb(245, 246, 250);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);

            eyeCareOverlay = new EyeCareOverlayForm();

            InitializeUI();
            InitializeMainMenu();
            SetupTray();
            SetupGCTimer();

            this.Show();
            this.BringToFront();
            this.Activate();

            InitializeBrowserEnv();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Program.WM_SHOW_BLACK_BROWSER)
            {
                ShowMainWindow();
            }
            base.WndProc(ref m);
        }

        private void Log(string msg)
        {
            try { File.AppendAllText(logPath, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\n"); }
            catch { }
        }

        private void SetupGCTimer()
        {
            gcTimer = new Timer();
            gcTimer.Interval = 60000;
            gcTimer.Tick += (s, e) => MemoryTrimmer.TrimProcessMemory();
            gcTimer.Start();
        }

        private void InitializeUI()
        {
            headerContainer = new Panel();
            headerContainer.Dock = DockStyle.Top;
            headerContainer.Height = 68;
            headerContainer.BackColor = Color.FromArgb(222, 225, 230);

            softBanner = new Panel();
            softBanner.Dock = DockStyle.Top;
            softBanner.Height = 24;
            softBanner.BackColor = Color.FromArgb(26, 115, 232);

            softBannerLabel = new Label();
            softBannerLabel.Dock = DockStyle.Fill;
            softBannerLabel.Text = "✨ Black Browser Active — 100% Ad-Free YouTube & Zero Trackers";
            softBannerLabel.ForeColor = Color.White;
            softBannerLabel.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            softBannerLabel.TextAlign = ContentAlignment.MiddleCenter;

            softBanner.Controls.Add(softBannerLabel);

            bannerTimer = new Timer();
            bannerTimer.Interval = 4000;
            bannerTimer.Tick += (s, e) =>
            {
                softBannerLabel.Text = "✨ Black Browser Active — 100% Ad-Free YouTube & Zero Trackers";
                bannerTimer.Stop();
            };

            omniboxPanel = new Panel();
            omniboxPanel.Dock = DockStyle.Fill;
            omniboxPanel.BackColor = Color.FromArgb(255, 255, 255);
            omniboxPanel.Padding = new Padding(6, 6, 6, 6);

            backBtn = CreateBtn("←", 0);
            fwdBtn = CreateBtn("→", 32);
            reloadBtn = CreateBtn("↻", 64);
            homeBtn = CreateBtn("🏠", 96);

            backBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoBack) wv.GoBack(); };
            fwdBtn.Click += (s, e) => { WebView2 wv = GetCurrentWebView(); if (wv != null && wv.CanGoForward) wv.GoForward(); };
            reloadBtn.Click += (s, e) => ReloadCurrentTab();
            homeBtn.Click += (s, e) => NavigateCurrentTab("about:blank");

            actionsPanel = new FlowLayoutPanel();
            actionsPanel.Dock = DockStyle.Right;
            actionsPanel.Height = 32;
            actionsPanel.AutoSize = true;
            actionsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            actionsPanel.FlowDirection = FlowDirection.RightToLeft;
            actionsPanel.WrapContents = false;
            actionsPanel.Padding = new Padding(0, 2, 4, 0);

            menuBtn = CreateActionBtn("⋮", Color.FromArgb(255, 255, 255), Color.FromArgb(95, 99, 104), 32);
            menuBtn.Click += (s, e) => mainMenu.Show(menuBtn, new Point(0, menuBtn.Height));

            addTabBtn = CreateActionBtn("+ Tab", Color.FromArgb(232, 240, 254), Color.FromArgb(26, 115, 232), 56);
            addTabBtn.Click += (s, e) => AddNewTab("New Tab", "about:blank");

            extBtn = CreateActionBtn("🧩 Ext", Color.FromArgb(241, 243, 244), Color.FromArgb(95, 99, 104), 64);
            extBtn.Click += (s, e) => AddNewTab("Chrome Extensions", "https://chromewebstore.google.com");

            settingsBtn = CreateActionBtn("⚙️", Color.FromArgb(241, 243, 244), Color.FromArgb(95, 99, 104), 36);
            settingsBtn.Click += (s, e) => OpenSettingsDialog(0);

            notesBtn = CreateActionBtn("📝 Notes", Color.FromArgb(235, 235, 245), Color.FromArgb(40, 40, 60), 68);
            notesBtn.Click += (s, e) => OpenSettingsDialog(2);

            eyeCareBtn = CreateActionBtn("👁 Eye", Color.FromArgb(254, 247, 224), Color.FromArgb(180, 100, 0), 64);
            eyeCareBtn.Click += (s, e) => CycleEyeCareMode();

            shieldBtn = CreateActionBtn("🛡 0", Color.FromArgb(232, 240, 254), Color.FromArgb(26, 115, 232), 62);
            shieldBtn.Click += (s, e) => ShowAdShieldStatus();

            starBtn = CreateActionBtn("⭐", Color.FromArgb(254, 247, 224), Color.FromArgb(180, 100, 0), 32);
            starBtn.Click += (s, e) => ToggleCurrentTabBookmark();

            actionsPanel.Controls.Add(menuBtn);
            actionsPanel.Controls.Add(addTabBtn);
            actionsPanel.Controls.Add(extBtn);
            actionsPanel.Controls.Add(settingsBtn);
            actionsPanel.Controls.Add(notesBtn);
            actionsPanel.Controls.Add(eyeCareBtn);
            actionsPanel.Controls.Add(shieldBtn);
            actionsPanel.Controls.Add(starBtn);

            urlBar = new TextBox();
            urlBar.Location = new Point(136, 7);
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
            urlBar.GotFocus += (s, e) => urlBar.SelectAll();

            omniboxPanel.Controls.Add(actionsPanel);
            omniboxPanel.Controls.Add(backBtn);
            omniboxPanel.Controls.Add(fwdBtn);
            omniboxPanel.Controls.Add(reloadBtn);
            omniboxPanel.Controls.Add(homeBtn);
            omniboxPanel.Controls.Add(urlBar);

            headerContainer.Controls.Add(omniboxPanel);
            headerContainer.Controls.Add(softBanner);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Padding = new Point(14, 4);
            tabControl.Font = new Font("Segoe UI", 9.5f);
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += OnDrawTabItem;
            tabControl.MouseDown += OnTabMouseDown;
            tabControl.SelectedIndexChanged += OnTabChanged;

            this.Controls.Add(headerContainer);
            this.Controls.Add(tabControl);
            headerContainer.SendToBack();
            tabControl.BringToFront();

            this.KeyPreview = true;
            this.KeyDown += OnFormKeyDown;

            this.Resize += (s, e) =>
            {
                if (urlBar != null && actionsPanel != null)
                {
                    urlBar.Width = Math.Max(200, this.Width - actionsPanel.Width - 160);
                }

                if (this.WindowState == FormWindowState.Minimized)
                {
                    SuspendAllWebViews();
                    MemoryTrimmer.TrimProcessMemory();
                }
                else
                {
                    ResumeActiveWebView();
                }
                this.PerformLayout();
            };
        }

        public void ShowSoftCommunication(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => ShowSoftCommunication(msg)));
                return;
            }

            softBannerLabel.Text = msg;
            bannerTimer.Stop();
            bannerTimer.Start();
        }

        private void ToggleCurrentTabBookmark()
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
            {
                string url = wv.Source != null ? wv.Source.ToString() : "";
                string title = wv.CoreWebView2.DocumentTitle;
                bool added = BookmarksManager.ToggleBookmark(title, url);

                if (added)
                {
                    starBtn.BackColor = Color.FromArgb(254, 235, 180);
                    ShowSoftCommunication("⭐ Bookmark Added Locally to Device!");
                }
                else
                {
                    starBtn.BackColor = Color.FromArgb(254, 247, 224);
                    ShowSoftCommunication("⭐ Bookmark Removed");
                }
            }
        }

        private void ShowAdShieldStatus()
        {
            ShowSoftCommunication("🛡️ AdShield Engine: " + totalBlockedAds + " Ads Blocked • Zero Trackers");
        }

        private void OpenSettingsDialog(int initialTab)
        {
            using (SettingsForm sf = new SettingsForm(
                isDarkMode ? 1 : 0,
                eyeCareMode,
                (themeIndex) => SetTheme(themeIndex == 1),
                (eyeCareIndex) => SetEyeCareMode(eyeCareIndex)))
            {
                sf.ShowDialog(this);
            }
        }

        private Button CreateBtn(string text, int left)
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

        private Button CreateActionBtn(string text, Color bg, Color fg, int width)
        {
            Button b = new Button();
            b.Text = text;
            b.Width = width;
            b.Height = 26;
            b.Margin = new Padding(2, 4, 2, 4);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = bg;
            b.ForeColor = fg;
            b.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
            return b;
        }

        private void OnDrawTabItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabControl.TabPages.Count) return;

            TabPage page = tabControl.TabPages[e.Index];
            Rectangle rect = tabControl.GetTabRect(e.Index);
            bool selected = (tabControl.SelectedIndex == e.Index);
            bool isPrivate = page.Tag != null && (bool)page.Tag == true;

            Color backColor = isPrivate
                ? (selected ? Color.FromArgb(32, 32, 42) : Color.FromArgb(22, 22, 28))
                : (selected ? Color.FromArgb(255, 255, 255) : Color.FromArgb(230, 233, 238));

            using (SolidBrush b = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(b, rect);
            }

            if (selected)
            {
                Color barColor = isPrivate ? Color.FromArgb(160, 90, 240) : Color.FromArgb(26, 115, 232);
                using (Pen p = new Pen(barColor, 2))
                {
                    e.Graphics.DrawLine(p, rect.Left, rect.Top, rect.Right, rect.Top);
                }
            }

            Color textColor = isPrivate ? Color.FromArgb(200, 180, 255) : (selected ? Color.FromArgb(32, 33, 36) : Color.FromArgb(95, 99, 104));

            TextRenderer.DrawText(e.Graphics, page.Text, tabControl.Font,
                new Rectangle(rect.X + 6, rect.Y + 4, rect.Width - 24, rect.Height - 4),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            Rectangle closeRect = new Rectangle(rect.Right - 20, rect.Y + (rect.Height - 14) / 2, 14, 14);
            using (Font f = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, "✕", f, closeRect,
                    Color.FromArgb(120, 120, 120), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void OnTabMouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                Rectangle rect = tabControl.GetTabRect(i);
                Rectangle closeRect = new Rectangle(rect.Right - 20, rect.Y + (rect.Height - 14) / 2, 14, 14);

                if (closeRect.Contains(e.Location))
                {
                    CloseTabAtIndex(i);
                    break;
                }
            }
        }

        private void CloseTabAtIndex(int index)
        {
            if (index < 0 || index >= tabControl.TabPages.Count) return;

            if (tabControl.TabPages.Count > 1)
            {
                TabPage page = tabControl.TabPages[index];
                WebView2 wv = GetWebView(page);
                if (wv != null)
                {
                    try { if (wv.CoreWebView2 != null) wv.CoreWebView2.Stop(); } catch { }
                    wv.Dispose();
                }
                tabControl.TabPages.Remove(page);
                page.Dispose();
            }
            else
            {
                WebView2 wv = GetCurrentWebView();
                if (wv != null && wv.CoreWebView2 != null)
                {
                    wv.CoreWebView2.Navigate(SpeedDialPage.GetSpeedDialFilePath(isDarkMode));
                    urlBar.Text = "";
                    tabControl.SelectedTab.Text = "New Tab";
                }
            }
        }

        private void InitializeMainMenu()
        {
            mainMenu = new ContextMenuStrip();
            mainMenu.Font = new Font("Segoe UI", 9.5f);

            mainMenu.Items.Add("➕ New Tab (Ctrl+T)", null, (s, e) => AddNewTab("New Tab", "about:blank"));
            mainMenu.Items.Add("🕵️ New Private Tab (Ctrl+Shift+P)", null, (s, e) => AddNewTab("Private Tab", "about:blank", isPrivate: true));
            mainMenu.Items.Add("🏠 Go to Speed Dial Home", null, (s, e) => NavigateCurrentTab("about:blank"));
            mainMenu.Items.Add("⭐ Local Bookmarks", null, (s, e) => NavigateCurrentTab("black://bookmarks"));
            mainMenu.Items.Add("📜 Local History (Ctrl+H)", null, (s, e) => NavigateCurrentTab("black://history"));
            mainMenu.Items.Add("📥 Local Downloads (Ctrl+J)", null, (s, e) => NavigateCurrentTab("black://downloads"));
            mainMenu.Items.Add("🛒 Chrome Web Store", null, (s, e) => AddNewTab("Chrome Store", "https://chromewebstore.google.com"));
            mainMenu.Items.Add("🧩 Edge Add-ons Store", null, (s, e) => AddNewTab("Edge Add-ons", "https://microsoftedge.microsoft.com/addons"));
            mainMenu.Items.Add(new ToolStripSeparator());
            mainMenu.Items.Add("⚙️ Settings & Device Info (Ctrl+,)", null, (s, e) => OpenSettingsDialog(0));
            mainMenu.Items.Add("📝 Dark Notes (Ctrl+Shift+N)", null, (s, e) => OpenSettingsDialog(2));
            mainMenu.Items.Add("👁️ Cycle Eye Care Filter (Ctrl+Shift+E)", null, (s, e) => CycleEyeCareMode());
            mainMenu.Items.Add("🌓 Toggle Dark / Light Theme (Ctrl+Shift+D)", null, (s, e) => ToggleTheme());
            mainMenu.Items.Add(new ToolStripSeparator());
            mainMenu.Items.Add("✕ Close Active Tab (Ctrl+W)", null, (s, e) => CloseCurrentTab());
            mainMenu.Items.Add("🚪 Exit Browser", null, (s, e) => Application.Exit());
        }

        private void SetTheme(bool dark)
        {
            isDarkMode = dark;
            Color defaultBg = isDarkMode ? Color.FromArgb(18, 18, 22) : Color.FromArgb(245, 246, 250);

            if (isDarkMode)
            {
                headerContainer.BackColor = Color.FromArgb(32, 33, 36);
                omniboxPanel.BackColor = Color.FromArgb(40, 42, 45);
                urlBar.BackColor = Color.FromArgb(53, 54, 58);
                urlBar.ForeColor = Color.White;
                this.BackColor = Color.FromArgb(20, 20, 22);
            }
            else
            {
                headerContainer.BackColor = Color.FromArgb(222, 225, 230);
                omniboxPanel.BackColor = Color.FromArgb(255, 255, 255);
                urlBar.BackColor = Color.FromArgb(241, 243, 244);
                urlBar.ForeColor = Color.FromArgb(32, 33, 36);
                this.BackColor = Color.FromArgb(245, 246, 250);
            }

            foreach (TabPage page in tabControl.TabPages)
            {
                WebView2 wv = GetWebView(page);
                if (wv != null)
                {
                    wv.DefaultBackgroundColor = defaultBg;
                    if (wv.CoreWebView2 != null && (wv.Source == null || wv.Source.ToString().EndsWith("speeddial.html") || wv.Source.ToString() == "about:blank"))
                    {
                        wv.CoreWebView2.Navigate(SpeedDialPage.GetSpeedDialFilePath(isDarkMode));
                    }
                }
            }
        }

        private void ToggleTheme()
        {
            SetTheme(!isDarkMode);
        }

        private void SetEyeCareMode(int mode)
        {
            eyeCareMode = mode;
            eyeCareOverlay.SetMode(eyeCareMode);

            if (eyeCareMode == 1)
            {
                eyeCareBtn.Text = "👁 Warm";
                eyeCareBtn.BackColor = Color.FromArgb(254, 235, 180);
                ShowSoftCommunication("👁️ Eye Care Filter: Warm Amber (18%)");
            }
            else if (eyeCareMode == 2)
            {
                eyeCareBtn.Text = "👁 Dimmed";
                eyeCareBtn.BackColor = Color.FromArgb(220, 220, 220);
                ShowSoftCommunication("👁️ Eye Care Filter: Night Dimmer (35%)");
            }
            else
            {
                eyeCareBtn.Text = "👁 Eye";
                eyeCareBtn.BackColor = Color.FromArgb(254, 247, 224);
                ShowSoftCommunication("👁️ Eye Care Filter: Disabled");
            }
        }

        private void CycleEyeCareMode()
        {
            SetEyeCareMode((eyeCareMode + 1) % 3);
        }

        private async void InitializeBrowserEnv()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "black-webview2");

                webViewEnv = await CoreWebView2Environment.CreateAsync(null, userDataFolder, null);
                Log("Environment created successfully with standard WebView2 environment settings");

                // Launch YouTube natively in the initial tab!
                AddNewTab("YouTube", "https://www.youtube.com");
            }
            catch (Exception ex)
            {
                Log("FATAL Env: " + ex.ToString());
            }
        }

        public async void AddNewTab(string title, string url, bool isPrivate = false)
        {
            try
            {
                if (webViewEnv == null) return;

                string tabTitle = isPrivate ? "🕵️ Private Tab" : TruncateTitle(title);
                TabPage page = new TabPage(tabTitle);
                page.Tag = isPrivate;

                Color defaultBg = isPrivate
                    ? Color.FromArgb(18, 18, 24)
                    : (isDarkMode ? Color.FromArgb(18, 18, 22) : Color.FromArgb(245, 246, 250));

                page.BackColor = defaultBg;

                WebView2 wv = new WebView2();
                wv.Dock = DockStyle.Fill;
                wv.DefaultBackgroundColor = defaultBg;
                page.Controls.Add(wv);
                tabControl.TabPages.Add(page);
                tabControl.SelectedTab = page;

                tabControl.Invalidate();
                this.PerformLayout();

                await wv.EnsureCoreWebView2Async(webViewEnv);

                wv.CoreWebView2.ProcessFailed += (s, e) =>
                {
                    Log("ProcessFailed: " + e.ProcessFailedKind.ToString());
                    try { wv.Reload(); } catch { }
                };

                wv.CoreWebView2.PermissionRequested += (s, e) =>
                {
                    if (e.PermissionKind == CoreWebView2PermissionKind.Notifications)
                        e.State = CoreWebView2PermissionState.Allow;
                };

                wv.CoreWebView2.DownloadStarting += (s, e) =>
                {
                    try
                    {
                        string path = e.ResultFilePath;
                        string name = !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "Download";
                        DownloadsManager.AddDownload(name, path ?? "", 0);
                        ShowSoftCommunication("📥 Download Started: " + name);
                    }
                    catch { }
                };

                wv.CoreWebView2.ContainsFullScreenElementChanged += (s, e) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        if (wv.CoreWebView2.ContainsFullScreenElement)
                        {
                            headerContainer.Visible = false;
                            this.FormBorderStyle = FormBorderStyle.None;
                            this.WindowState = FormWindowState.Maximized;
                        }
                        else
                        {
                            headerContainer.Visible = true;
                            this.FormBorderStyle = FormBorderStyle.Sizable;
                            this.WindowState = FormWindowState.Normal;
                        }
                        this.PerformLayout();
                    }));
                };

                AdShieldEngine.AttachAdShield(wv, () =>
                {
                    totalBlockedAds++;
                    try { this.Invoke((Action)(() => shieldBtn.Text = "🛡 " + totalBlockedAds)); } catch { }
                });

                wv.CoreWebView2.NavigationStarting += (s, e) =>
                {
                    if (e.Uri.Equals("black://history", StringComparison.OrdinalIgnoreCase) ||
                        e.Uri.Equals("about:history", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Cancel = true;
                        wv.CoreWebView2.NavigateToString(HistoryManager.GetHistoryHtml(isDarkMode));
                        if (tabControl.SelectedTab == page) { urlBar.Text = "black://history"; page.Text = "Local History"; }
                        return;
                    }

                    if (e.Uri.Equals("black://bookmarks", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Cancel = true;
                        wv.CoreWebView2.NavigateToString(BookmarksManager.GetBookmarksHtml(isDarkMode));
                        if (tabControl.SelectedTab == page) { urlBar.Text = "black://bookmarks"; page.Text = "Local Bookmarks"; }
                        return;
                    }

                    if (e.Uri.Equals("black://downloads", StringComparison.OrdinalIgnoreCase) ||
                        e.Uri.Equals("about:downloads", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Cancel = true;
                        wv.CoreWebView2.NavigateToString(DownloadsManager.GetDownloadsHtml(isDarkMode));
                        if (tabControl.SelectedTab == page) { urlBar.Text = "black://downloads"; page.Text = "Local Downloads"; }
                        return;
                    }

                    if (tabControl.SelectedTab == page)
                    {
                        string uriStr = e.Uri;
                        urlBar.Text = (uriStr == "about:blank" || uriStr.EndsWith("speeddial.html")) ? "" : uriStr;
                    }
                };

                wv.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (tabControl.SelectedTab == page)
                    {
                        string uriStr = wv.Source != null ? wv.Source.ToString() : "";
                        urlBar.Text = (uriStr == "about:blank" || uriStr.EndsWith("speeddial.html")) ? "" : uriStr;

                        string pageName = string.IsNullOrEmpty(wv.CoreWebView2.DocumentTitle) || wv.CoreWebView2.DocumentTitle == "speeddial.html"
                            ? "New Tab" : TruncateTitle(wv.CoreWebView2.DocumentTitle);

                        page.Text = isPrivate ? "🕵️ " + pageName : pageName;
                        tabControl.Invalidate();
                        UpdateNavButtons();
                    }

                    if (!isPrivate && wv.Source != null && !wv.Source.ToString().EndsWith("speeddial.html"))
                    {
                        HistoryManager.AddVisit(wv.CoreWebView2.DocumentTitle, wv.Source.ToString());
                    }
                };

                wv.CoreWebView2.SourceChanged += (s, e) =>
                {
                    if (tabControl.SelectedTab == page)
                    {
                        string uriStr = wv.Source != null ? wv.Source.ToString() : "";
                        urlBar.Text = (uriStr == "about:blank" || uriStr.EndsWith("speeddial.html")) ? "" : uriStr;
                    }
                };

                if (url == "about:blank" || string.IsNullOrEmpty(url) || url == "black://home")
                {
                    wv.CoreWebView2.Navigate(SpeedDialPage.GetSpeedDialFilePath(isDarkMode));
                }
                else if (url == "black://history" || url == "about:history")
                {
                    wv.CoreWebView2.NavigateToString(HistoryManager.GetHistoryHtml(isDarkMode));
                }
                else if (url == "black://bookmarks")
                {
                    wv.CoreWebView2.NavigateToString(BookmarksManager.GetBookmarksHtml(isDarkMode));
                }
                else if (url == "black://downloads" || url == "about:downloads")
                {
                    wv.CoreWebView2.NavigateToString(DownloadsManager.GetDownloadsHtml(isDarkMode));
                }
                else
                {
                    wv.CoreWebView2.Navigate(FormatUrl(url));
                }
            }
            catch (Exception ex)
            {
                Log("AddNewTab ERROR: " + ex.ToString());
            }
        }

        private string TruncateTitle(string title)
        {
            if (string.IsNullOrEmpty(title)) return "Tab";
            if (title.Length > 18) return title.Substring(0, 15) + "...";
            return title;
        }

        private void CloseCurrentTab()
        {
            if (tabControl.SelectedIndex >= 0)
                CloseTabAtIndex(tabControl.SelectedIndex);
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
            if (wv != null && wv.CoreWebView2 != null && wv.Source != null)
            {
                string uriStr = wv.Source.ToString();
                urlBar.Text = (uriStr == "about:blank" || uriStr.EndsWith("speeddial.html")) ? "" : uriStr;
                UpdateNavButtons();

                starBtn.BackColor = BookmarksManager.IsBookmarked(uriStr)
                    ? Color.FromArgb(254, 235, 180)
                    : Color.FromArgb(254, 247, 224);
            }
            else
            {
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
            else
            {
                backBtn.Enabled = false;
                fwdBtn.Enabled = false;
            }
        }

        private void NavigateCurrentTab(string input)
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
            {
                if (string.IsNullOrWhiteSpace(input) || input == "about:blank" || input.Equals("black://home", StringComparison.OrdinalIgnoreCase))
                {
                    wv.CoreWebView2.Navigate(SpeedDialPage.GetSpeedDialFilePath(isDarkMode));
                    urlBar.Text = "";
                    if (tabControl.SelectedTab != null) tabControl.SelectedTab.Text = "New Tab";
                    return;
                }

                if (input.Equals("black://history", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("about:history", StringComparison.OrdinalIgnoreCase))
                {
                    wv.CoreWebView2.NavigateToString(HistoryManager.GetHistoryHtml(isDarkMode));
                    urlBar.Text = "black://history";
                    if (tabControl.SelectedTab != null) tabControl.SelectedTab.Text = "Local History";
                    return;
                }

                if (input.Equals("black://bookmarks", StringComparison.OrdinalIgnoreCase))
                {
                    wv.CoreWebView2.NavigateToString(BookmarksManager.GetBookmarksHtml(isDarkMode));
                    urlBar.Text = "black://bookmarks";
                    if (tabControl.SelectedTab != null) tabControl.SelectedTab.Text = "Local Bookmarks";
                    return;
                }

                if (input.Equals("black://downloads", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("about:downloads", StringComparison.OrdinalIgnoreCase))
                {
                    wv.CoreWebView2.NavigateToString(DownloadsManager.GetDownloadsHtml(isDarkMode));
                    urlBar.Text = "black://downloads";
                    if (tabControl.SelectedTab != null) tabControl.SelectedTab.Text = "Local Downloads";
                    return;
                }

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

            if (input.StartsWith("http://") || input.StartsWith("https://") || input.StartsWith("file://") || input.StartsWith("black://"))
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
                {
                    try { wv.CoreWebView2.TrySuspendAsync(); } catch { }
                }
            }
        }

        private void ResumeActiveWebView()
        {
            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.CoreWebView2 != null)
            {
                try { wv.CoreWebView2.Resume(); } catch { }
            }
        }

        public void ShowMainWindow()
        {
            if (this.InvokeRequired)
            {
                this.Invoke((Action)ShowMainWindow);
                return;
            }
            this.Visible = true;
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.BringToFront();
            this.Activate();
            Program.ForceForegroundWindow(this.Handle);
        }

        private void SetupTray()
        {
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Black Browser";

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

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T)
            {
                e.SuppressKeyPress = true;
                AddNewTab("New Tab", "about:blank");
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.P)
            {
                e.SuppressKeyPress = true;
                AddNewTab("Private Tab", "about:blank", isPrivate: true);
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
            else if (e.Control && e.KeyCode == Keys.Oemcomma)
            {
                e.SuppressKeyPress = true;
                OpenSettingsDialog(0);
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.N)
            {
                e.SuppressKeyPress = true;
                OpenSettingsDialog(2);
            }
            else if (e.Control && e.KeyCode == Keys.R || e.KeyCode == Keys.F5)
            {
                e.SuppressKeyPress = true;
                ReloadCurrentTab();
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                e.SuppressKeyPress = true;
                NavigateCurrentTab("black://history");
            }
            else if (e.Control && e.KeyCode == Keys.J)
            {
                e.SuppressKeyPress = true;
                NavigateCurrentTab("black://downloads");
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                SuspendAllWebViews();
                MemoryTrimmer.TrimProcessMemory();
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
                if (bannerTimer     != null) bannerTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
