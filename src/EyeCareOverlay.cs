using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlackBrowser
{
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
}
