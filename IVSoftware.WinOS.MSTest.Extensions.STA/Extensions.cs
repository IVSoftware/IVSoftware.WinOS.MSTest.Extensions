namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    public static class Extensions
    {
        public static void CycleTopmost(this Form @this)
        {
            @this.BeginInvoke(() =>
            {
                @this.TopMost = true;
                @this.BeginInvoke(() =>
                {
                    @this.TopMost = false;
                });
            });
        }

        public static STARunner CreateSTAThread(this object? _, bool isVisible)
            => STARunner.CreateThread(isVisible);

        public static STARunner CreateSTAThread<T>(this object? _, bool isVisible)
            => STARunner.CreateThread<T>(isVisible);
    }
}
