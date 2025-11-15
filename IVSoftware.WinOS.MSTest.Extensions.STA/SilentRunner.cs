namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    public class SilentRunner : Form
    {
        public SilentRunner() : this(true) { }
        public SilentRunner(bool isVisible)
        {
            IsSilent = !isVisible;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(500, 300);
        }
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value && !IsSilent);
        }

        public bool IsSilent
        {
            get => _isSilent;
            set
            {
                if(value && !IsHandleCreated)
                {
                    _ = Handle;
                }
                if (!Equals(_isSilent, value))
                {
                    _isSilent = value;
                    if(!IsSilent)
                    {
                        // Make a new call to SetVisibleCore.
                        Show();
                    }
                }
            }
        }
        bool _isSilent = false;
    }
}
