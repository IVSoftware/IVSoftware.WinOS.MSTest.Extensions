namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    class SilentRunner : Form
    {
        public SilentRunner(bool isSilent = true)
        {
            IsSilent = isSilent;
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
