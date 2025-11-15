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
    }
}
