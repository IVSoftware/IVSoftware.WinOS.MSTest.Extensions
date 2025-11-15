namespace IVSoftware.WinOS.MSTest.Extensions.STA
{
    [TestClass]
    public sealed class TestClass_STA
    {
        [TestMethod]
        public async Task Test_DisposalWithNOOP()
        {
            STARunner sta;
            using (sta = new STARunner(isVisible: true))
            {
                await sta.RunAsync(async () =>
                {
                    sta.MainForm.Text = "Main Form";
                    await Task.CompletedTask;
                });
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await sta.RunAsync(async () =>
                    {
                        sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                        await Task.CompletedTask;
                    });
                }
            }

            // This tests not only the robustness of the dispose, but also
            // the ability of the second instance to bring itself to front.
            using (sta = new STARunner(isVisible: true))
            {
                await sta.RunAsync(async () =>
                {
                    sta.MainForm.Text = "Main Form";
                    await Task.CompletedTask;
                });
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await sta.RunAsync(async () =>
                    {
                        sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                        await Task.CompletedTask;
                    });
                }
            }
        }

        [TestMethod]
        public async Task Test_SilentDisposalWithNOOP()
        {
            STARunner sta;
            using (sta = new STARunner(isVisible: false))
            {
                await sta.RunAsync(async () =>
                {
                    sta.MainForm.Text = "Main Form";
                    await Task.CompletedTask;
                });
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await sta.RunAsync(async () =>
                    {
                        sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                        await Task.CompletedTask;
                    });
                }
            }
        }
    }
}
