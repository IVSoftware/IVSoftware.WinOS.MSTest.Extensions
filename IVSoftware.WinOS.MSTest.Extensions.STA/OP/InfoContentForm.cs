using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IVSoftware.WinOS.MSTest.Extensions.STA.OP
{
    public partial class InfoContentForm : Form
    {
        public InfoContentForm() => InitializeComponent();

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value && !string.IsNullOrWhiteSpace(InfoText));
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using var path = new GraphicsPath();
            int radius = 30;
            var rect = ClientRectangle;
            if (rect.Width > 0 && rect.Height > 0)
            {
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                Region = new Region(path);
            }
        }
        public string InfoText
        {
            get => _infoText;
            set
            {
                if (!Equals(_infoText, value))
                {
                    _infoText = value;
                    labelInfo.Text = value;
                    Invalidate();
                }
            }
        }
        string _infoText = string.Empty;
    }
}
