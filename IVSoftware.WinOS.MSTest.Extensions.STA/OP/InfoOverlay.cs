using IVSoftware.Portable.SQLiteMarkdown;

namespace IVSoftware.WinOS.MSTest.Extensions.STA.OP
{
    /// <summary>
    /// This ZERO SIZE runtime container performs no visual work. 
    /// </summary>
    /// <remarks>
    /// The control exists as a logical anchor for two external forms: a translucent  
    /// backdrop that dims the parent window, and an InfoContentForm that displays  
    /// the active banner. Visibility changes propagate to these forms, and click  
    /// messages are filtered so the user can dismiss the banner without entering  
    /// a modal state. No rendering occurs on this control itself once created.
    /// </remarks>
    public partial class InfoOverlay
        : Control
        , IMessageFilter
        , IInfoContentForm
    {
        public InfoOverlay()
        {
            Application.AddMessageFilter(this);

            // Zero size this actual control so that the
            // translucent overlay shows content beneath it.
            SizeChanged += (sender, e) => Size = new Size();
            _overlay = new Form
            {
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                Opacity = 0.4F,
                BackColor = Color.Black,
            };
            _overlayContent = new InfoContentForm
            {
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
            };
            _overlayContent.PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(InfoContentForm.InfoText):
                        Visible = !string.IsNullOrWhiteSpace(InfoText);
                        break;
                }
            };
            Disposed += (sender, e) =>
            {
                Application.RemoveMessageFilter(this);
                _overlay.Dispose();
                _overlayContent.Dispose();
            };
        }

        // K E E P    D R I L L I N G
        // We'll get a callback from: _overlayContent.PropertyChanged event.
        public string InfoText
        {
            get => _overlayContent.InfoText;
            set => _overlayContent.InfoText = value;
        }
        public bool IsVisbleDSA { get => ((IInfoContentForm)_overlayContent).IsVisbleDSA; set => ((IInfoContentForm)_overlayContent).IsVisbleDSA = value; }

        string _infoText = string.Empty;

        public bool PreFilterMessage(ref Message m)
        {
            switch ((Win32Message)m.Msg)
            {
                case Win32Message.WM_LBUTTONDOWN:
                    break;
                case Win32Message.WM_LBUTTONUP:
                    if (Visible) Visible = false;
                    break;
                default:
                    break;
            }
            return false;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            if (TopLevelControl is { } mainWindow)
            {
                localTrackFromMainWindow();

                VisibleChanged += (sender, e) =>
                {
                    if (Visible)
                    {
                        _overlay.Show(mainWindow);
                        _overlayContent.Show(mainWindow);
                    }
                    else
                    {
                        _overlay.Hide();
                        _overlayContent.Hide();
                        InfoText = string.Empty;
                        if (IsHandleCreated)
                        {
                            BeginInvoke(() => TopLevelControl?.BringToFront());
                        }
                    }
                };

                mainWindow.Move += (sender, e) =>
                    localTrackFromMainWindow();

                mainWindow.SizeChanged += (sender, e) =>
                {
                    localTrackFromMainWindow();
                };

                void localTrackFromMainWindow()
                {
                    _overlay.Size = mainWindow.ClientSize;
                    _overlayContent.Size = new Size(mainWindow.Width - 50, 400);
                    TopLevelControl.CenterChildInClientRectangle(_overlay);
                    _overlay.CenterChildInClientRectangle(_overlayContent);
                }
                ;
            }
        }

        private readonly Form _overlay = null!;

        /// <summary>
        /// Strongly typed content form.
        /// </summary>
        private readonly InfoContentForm _overlayContent = null!;
    }
    static class InfoOverlayExtensions
    {

        public static void CenterChildInClientRectangle(this Control @this, Control child)
        {
            var parentClientRect = @this.ClientRectangle;
            var x = parentClientRect.Left + (parentClientRect.Width - child.Width) / 2;
            var y = parentClientRect.Top + (parentClientRect.Height - child.Height) / 2;

            var location = new Point(x, y);

            if (child is Form)
                child.Location = @this.PointToScreen(location);
            else
                child.Location = location;
        }
    }
}
