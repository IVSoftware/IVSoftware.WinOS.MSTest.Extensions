using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;

namespace IVSoftware.WinOS.MSTest.Extensions.STA.OP
{
    public partial class InfoContentForm
        : Form
        , INotifyPropertyChanged
        , IInfoContentForm
    {
        public InfoContentForm()
        {
            InitializeComponent();
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
                    OnPropertyChanged();
                }
            }
        }
        string _infoText = string.Empty;

        public bool IsVisbleDSA
        {
            get => _showDSA;
            set
            {
                if (!Equals(_showDSA, value))
                {
                    _showDSA = value;
                    labelHR.Visible = _showDSA;
                    labelDSA.Visible = _showDSA;
                    checkBoxDSA.Visible = _showDSA;

                    OnPropertyChanged();
                }
            }
        }
        bool _showDSA = true;



        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
