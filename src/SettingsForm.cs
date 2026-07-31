using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BlackBrowser
{
    public class SettingsForm : Form
    {
        private TabControl tabControl;
        private TabPage generalPage;
        private TabPage eyeCarePage;
        private TabPage notesPage;
        private TabPage deviceInfoPage;

        private ComboBox themeCombo;
        private ComboBox eyeCareCombo;
        private TextBox notesTextBox;
        private Label sysInfoLabel;

        private Action<int> onThemeChanged;
        private Action<int> onEyeCareChanged;
        private string notesPath;

        public SettingsForm(int currentTheme, int currentEyeCare, Action<int> themeCallback, Action<int> eyeCareCallback)
        {
            this.onThemeChanged = themeCallback;
            this.onEyeCareChanged = eyeCareCallback;

            notesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "black-webview2", "dark_notes.txt");

            this.Text = "Black Browser — Settings & Dark Notes";
            this.Width = 620;
            this.Height = 480;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(24, 24, 28);
            this.ForeColor = Color.White;

            InitializeComponents(currentTheme, currentEyeCare);
            LoadNotes();
        }

        private void InitializeComponents(int currentTheme, int currentEyeCare)
        {
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 9.5f);
            tabControl.Padding = new Point(14, 6);

            generalPage = new TabPage("⚙️ General & Theme");
            eyeCarePage = new TabPage("👁️ Eye Care & Screen");
            notesPage = new TabPage("📝 Dark Notes");
            deviceInfoPage = new TabPage("💻 Device & Hardware");

            generalPage.BackColor = Color.FromArgb(28, 28, 32);
            eyeCarePage.BackColor = Color.FromArgb(28, 28, 32);
            notesPage.BackColor = Color.FromArgb(18, 18, 22);
            deviceInfoPage.BackColor = Color.FromArgb(28, 28, 32);

            // ─── General & Theme Page ──────────────────────────────────────────────
            Label themeLbl = new Label();
            themeLbl.Text = "Browser Theme / Visual Style:";
            themeLbl.Location = new Point(24, 28);
            themeLbl.AutoSize = true;
            themeLbl.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            themeLbl.ForeColor = Color.White;

            themeCombo = new ComboBox();
            themeCombo.Location = new Point(24, 58);
            themeCombo.Width = 320;
            themeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            themeCombo.Font = new Font("Segoe UI", 10f);
            themeCombo.Items.Add("☀️ Google Chrome Light (Default)");
            themeCombo.Items.Add("🌙 Obsidian Dark Mode");
            themeCombo.SelectedIndex = currentTheme;

            themeCombo.SelectedIndexChanged += (s, e) =>
            {
                if (onThemeChanged != null) onThemeChanged(themeCombo.SelectedIndex);
            };

            generalPage.Controls.Add(themeLbl);
            generalPage.Controls.Add(themeCombo);

            // ─── Eye Care Page ─────────────────────────────────────────────────────
            Label eyeLbl = new Label();
            eyeLbl.Text = "Eye Care Overlay Filter:";
            eyeLbl.Location = new Point(24, 28);
            eyeLbl.AutoSize = true;
            eyeLbl.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            eyeLbl.ForeColor = Color.White;

            eyeCareCombo = new ComboBox();
            eyeCareCombo.Location = new Point(24, 58);
            eyeCareCombo.Width = 320;
            eyeCareCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            eyeCareCombo.Font = new Font("Segoe UI", 10f);
            eyeCareCombo.Items.Add("Disabled");
            eyeCareCombo.Items.Add("👁️ Warm Amber (Night Light Filter - 18%)");
            eyeCareCombo.Items.Add("🌙 Night Dimmer (Dark Screen Filter - 35%)");
            eyeCareCombo.SelectedIndex = currentEyeCare;

            eyeCareCombo.SelectedIndexChanged += (s, e) =>
            {
                if (onEyeCareChanged != null) onEyeCareChanged(eyeCareCombo.SelectedIndex);
            };

            eyeCarePage.Controls.Add(eyeLbl);
            eyeCarePage.Controls.Add(eyeCareCombo);

            // ─── Dark Notes Page ───────────────────────────────────────────────────
            Label notesHeader = new Label();
            notesHeader.Text = "📝 Quick Dark Notes (Auto-Saved)";
            notesHeader.Dock = DockStyle.Top;
            notesHeader.Height = 32;
            notesHeader.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            notesHeader.ForeColor = Color.FromArgb(26, 115, 232);
            notesHeader.Padding = new Padding(10, 6, 0, 0);

            notesTextBox = new TextBox();
            notesTextBox.Dock = DockStyle.Fill;
            notesTextBox.Multiline = true;
            notesTextBox.ScrollBars = ScrollBars.Vertical;
            notesTextBox.BackColor = Color.FromArgb(14, 14, 18);
            notesTextBox.ForeColor = Color.FromArgb(230, 235, 245);
            notesTextBox.Font = new Font("Consolas", 10.5f);
            notesTextBox.BorderStyle = BorderStyle.None;

            notesTextBox.TextChanged += (s, e) => SaveNotes();

            notesPage.Controls.Add(notesTextBox);
            notesPage.Controls.Add(notesHeader);

            // ─── Device Info Page ──────────────────────────────────────────────────
            sysInfoLabel = new Label();
            sysInfoLabel.Location = new Point(24, 28);
            sysInfoLabel.Size = new Size(540, 320);
            sysInfoLabel.Font = new Font("Consolas", 9.5f);
            sysInfoLabel.ForeColor = Color.FromArgb(200, 205, 215);

            UpdateDeviceInfo();

            deviceInfoPage.Controls.Add(sysInfoLabel);

            tabControl.TabPages.Add(generalPage);
            tabControl.TabPages.Add(eyeCarePage);
            tabControl.TabPages.Add(notesPage);
            tabControl.TabPages.Add(deviceInfoPage);

            this.Controls.Add(tabControl);
        }

        private void UpdateDeviceInfo()
        {
            Process proc = Process.GetCurrentProcess();
            double ramMB = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 2);

            sysInfoLabel.Text =
                "==========================================================\n" +
                "               BLACK BROWSER DEVICE DIAGNOSTICS          \n" +
                "==========================================================\n\n" +
                " OS Version          : " + Environment.OSVersion.ToString() + "\n" +
                " 64-Bit OS           : " + (Environment.Is64BitOperatingSystem ? "Yes (x64)" : "No (x86)") + "\n" +
                " CPU Cores           : " + Environment.ProcessorCount.ToString() + " Logical Cores\n" +
                " Device Machine Name : " + Environment.MachineName + "\n" +
                " Current User        : " + Environment.UserName + "\n\n" +
                " Process ID          : " + proc.Id.ToString() + "\n" +
                " RAM Usage (Working) : " + ramMB.ToString() + " MB\n" +
                " Framework Runtime   : .NET Framework " + Environment.Version.ToString() + "\n" +
                " Rendering Engine    : Microsoft WebView2 (Chromium 128)\n" +
                " User-Agent          : Chrome 128 Windows 10/11 Compatible\n" +
                " 3-Layer Ad Shield   : Active\n" +
                "==========================================================";
        }

        private void LoadNotes()
        {
            try
            {
                if (File.Exists(notesPath))
                    notesTextBox.Text = File.ReadAllText(notesPath);
            }
            catch { }
        }

        private void SaveNotes()
        {
            try
            {
                string dir = Path.GetDirectoryName(notesPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(notesPath, notesTextBox.Text);
            }
            catch { }
        }
    }
}
