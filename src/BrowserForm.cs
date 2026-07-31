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
        private TabControl tabControl;

        private Button backBtn;
        private Button fwdBtn;
        private Button reloadBtn;
        private Button homeBtn;
        private TextBox urlBar;
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

        private EyeCareOverlayForm eyeCareOverlay;
        private int eyeCareMode = 0;
        private bool isDarkMode = false;

        private CoreWebView2Environment webViewEnv;
        private int totalBlockedAds = 0;
        private string logPath;

        public BrowserForm()
        {
            logPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "debug.log");
            Log("=== Black Browser starting (Local History & Privacy v5.1) ===");

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;

            this.Text = "Black Browser";
            this.Width = 1280;
            this.Height = 820;
            this.BackColor = Color.FromArgb(222, 225, 230);
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
            headerContainer.Height = 44;
            headerContainer.BackColor = Color.FromArgb(222, 225, 230);

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

            urlBar = new TextBox();
            urlBar.Location = new Point(136, 7);
            urlBar.Width = this.Width - 580;
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

            shieldBtn = CreateActionBtn("🛡 0", Color.FromArgb(232, 240, 254), Color.FromArgb(26, 115, 232), 62, this.Width - 435);
            shieldBtn.Click += (s, e) => ShowAdShieldStatus();

            eyeCareBtn = CreateActionBtn("👁 Eye", Color.FromArgb(254, 247, 224), Color.FromArgb(180, 100, 0), 64, this.Width - 368);
            eyeCareBtn.Click += (s, e) => CycleEyeCareMode();

            notesBtn = CreateActionBtn("📝 Notes", Color.FromArgb(235, 235, 245), Color.FromArgb(40, 40, 60), 68, this.Width - 299);
            notesBtn.Click += (s, e) => OpenSettingsDialog(2);

            settingsBtn = CreateActionBtn("⚙️", Color.FromArgb(241, 243, 244), Color.FromArgb(95, 99, 104), 36, this.Width - 226);
            settingsBtn.Click += (s, e) => OpenSettingsDialog(0);

            extBtn = CreateActionBtn("🧩 Ext", Color.FromArgb(241, 243, 244), Color.FromArgb(95, 99, 104), 64, this.Width - 185);
            extBtn.Click += (s, e) => AddNewTab("Chrome Extensions", "https://chromewebstore.google.com");

            addTabBtn = CreateActionBtn("+ Tab", Color.FromArgb(232, 240, 254), Color.FromArgb(26, 115, 232), 56, this.Width - 116);
            addTabBtn.Click += (s, e) => AddNewTab("New Tab", "about:blank");

            menuBtn = CreateBtn("⋮", this.Width - 54);
            menuBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            menuBtn.Click += (s, e) => mainMenu.Show(menuBtn, new Point(0, menuBtn.Height));

            omniboxPanel.Controls.Add(backBtn);
            omniboxPanel.Controls.Add(fwdBtn);
            omniboxPanel.Controls.Add(reloadBtn);
            omniboxPanel.Controls.Add(homeBtn);
            omniboxPanel.Controls.Add(urlBar);
            omniboxPanel.Controls.Add(shieldBtn);
            omniboxPanel.Controls.Add(eyeCareBtn);
            omniboxPanel.Controls.Add(notesBtn);
            omniboxPanel.Controls.Add(settingsBtn);
            omniboxPanel.Controls.Add(extBtn);
            omniboxPanel.Controls.Add(addTabBtn);
            omniboxPanel.Controls.Add(menuBtn);

            headerContainer.Controls.Add(omniboxPanel);

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Padding = new Point(14, 4);
            tabControl.Font = new Font("Segoe UI", 9.5f);
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += OnDrawTabItem;
            tabControl.MouseDown += OnTabMouseDown;
            tabControl.SelectedIndexChanged += OnTabChanged;

            this.Controls.Add(tabControl);
            this.Controls.Add(headerContainer);

            this.KeyPreview = true;
            this.KeyDown += OnFormKeyDown;

            this.Resize += (s, e) =>
            {
                if (urlBar != null)
                    urlBar.Width = Math.Max(200, this.Width - 580);

                if (this.WindowState == FormWindowState.Minimized)
                {
                    SuspendAllWebViews();
                    MemoryTrimmer.TrimProcessMemory();
                }
                else
                {
                    ResumeActiveWebView();
                }
            };
        }

        private void ShowAdShieldStatus()
        {
            MessageBox.Show(
                "🛡️ 3-Layer AdShield Protection Engine Status:\n\n" +
                "• Total Blocked Ads & Trackers: " + totalBlockedAds + "\n" +
                "• Network Filtering Layer: Active\n" +
                "• Injected CSS Hiding Layer: Active\n" +
                "• 500ms JS Video Ad Fast-Forwarder: Active\n\n" +
                "Your browsing is 100% ad-free and tracking protected!",
                "Black Browser — AdShield Protection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private Button CreateActionBtn(string text, Color bg, Color fg, int width, int left)
        {
            Button b = new Button();
            b.Text = text;
            b.Width = width;
            b.Height = 28;
            b.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            b.Location = new Point(left, 6);
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
            TabPage page = tabControl.TabPages[e.Index];
            Rectangle rect = tabControl.GetTabRect(e.Index);
            bool selected = (tabControl.SelectedIndex == e.Index);

            Color backColor = selected ? Color.FromArgb(255, 255, 255) : Color.FromArgb(230, 233, 238);
            using (SolidBrush b = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(b, rect);
            }

            if (selected)
            {
                using (Pen p = new Pen(Color.FromArgb(26, 115, 232), 2))
                {
                    e.Graphics.DrawLine(p, rect.Left, rect.Top, rect.Right, rect.Top);
                }
            }

            TextRenderer.DrawText(e.Graphics, page.Text, tabControl.Font,
                new Rectangle(rect.X + 6, rect.Y + 4, rect.Width - 24, rect.Height - 4),
                selected ? Color.FromArgb(32, 33, 36) : Color.FromArgb(95, 99, 104),
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
                    wv.CoreWebView2.NavigateToString(SpeedDialPage.GetHtml(isDarkMode));
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
            mainMenu.Items.Add("📜 Local History (Ctrl+H)", null, (s, e) => NavigateCurrentTab("black://history"));
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
                this.BackColor = Color.FromArgb(249, 249, 251);
            }

            WebView2 wv = GetCurrentWebView();
            if (wv != null && wv.Source != null && wv.Source.ToString() == "about:blank")
            {
                wv.CoreWebView2.NavigateToString(SpeedDialPage.GetHtml(isDarkMode));
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

                string chromeUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0";

                var options = new CoreWebView2EnvironmentOptions(
                    "--disk-cache-size=33554432 " +
                    "--media-cache-size=33554432 " +
                    "--renderer-process-limit=1 " +
                    "--enable-experimental-extension-apis " +
                    "--allow-legacy-extension-manifests " +
                    "--user-agent=\"" + chromeUA + "\" " +
                    "--no-first-run " +
                    "--disable-sync " +
                    "--disable-translate " +
                    "--js-flags=--max-old-space-size=128"
                );

                webViewEnv = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                Log("Environment created successfully with full Chrome compatibility User-Agent");

                AddNewTab("New Tab", "about:blank");
            }
            catch (Exception ex)
            {
                Log("FATAL Env: " + ex.ToString());
            }
        }

        public async void AddNewTab(string title, string url)
        {
            try
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
                        if (tabControl.SelectedTab == page)
                        {
                            urlBar.Text = "black://history";
                            page.Text = "Local History";
                        }
                        return;
                    }

                    if (tabControl.SelectedTab == page)
                    {
                        string uriStr = e.Uri;
                        urlBar.Text = uriStr == "about:blank" ? "" : uriStr;
                    }
                };

                wv.CoreWebView2.NavigationCompleted += (s, e) =>
                {
                    if (tabControl.SelectedTab == page)
                    {
                        string uriStr = wv.Source.ToString();
                        urlBar.Text = uriStr == "about:blank" ? "" : uriStr;
                        page.Text = string.IsNullOrEmpty(wv.CoreWebView2.DocumentTitle)
                            ? "Tab" : TruncateTitle(wv.CoreWebView2.DocumentTitle);
                        tabControl.Invalidate();
                        UpdateNavButtons();
                    }

                    // Record to 100% Local History
                    HistoryManager.AddVisit(wv.CoreWebView2.DocumentTitle, wv.Source.ToString());
                };

                wv.CoreWebView2.SourceChanged += (s, e) =>
                {
                    if (tabControl.SelectedTab == page)
                    {
                        string uriStr = wv.Source.ToString();
                        urlBar.Text = uriStr == "about:blank" ? "" : uriStr;
                    }
                };

                if (url == "about:blank" || string.IsNullOrEmpty(url))
                {
                    wv.CoreWebView2.NavigateToString(SpeedDialPage.GetHtml(isDarkMode));
                }
                else if (url == "black://history" || url == "about:history")
                {
                    wv.CoreWebView2.NavigateToString(HistoryManager.GetHistoryHtml(isDarkMode));
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
            if (wv != null && wv.CoreWebView2 != null)
            {
                string uriStr = wv.Source.ToString();
                urlBar.Text = uriStr == "about:blank" ? "" : uriStr;
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
                if (input.Equals("black://history", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("about:history", StringComparison.OrdinalIgnoreCase))
                {
                    wv.CoreWebView2.NavigateToString(HistoryManager.GetHistoryHtml(isDarkMode));
                    urlBar.Text = "black://history";
                    if (tabControl.SelectedTab != null) tabControl.SelectedTab.Text = "Local History";
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
            }
            base.Dispose(disposing);
        }
    }
}
