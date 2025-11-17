using IVSoftware.WinOS.MSTest.Extensions.STA.WinForms;

namespace IVSoftware.WinOS.MSTest.Extensions.STA;

[TestClass]
public class TestClass_CollectionViewRunner
{
#if INTERACTIVE
    [TestMethod]
#else
    [TestMethod, Ignore]
#endif
    public async Task Test_CVRInteractive()
    {
        using var sta = STARunner.CreateThread<CollectionViewRunner>(isVisible: true);

        await sta.RunAsync(localStaTest);

        // Encapsulate the local testing to be done on the STA thread.
        async Task localStaTest()
        {
            var tcs = new TaskCompletionSource();
            Assert.IsInstanceOfType<CollectionViewRunner>(sta.MainForm);

            // Interactive Return
            sta.MainForm.FormClosed += (sender, e) => tcs.SetResult();

            sta.MainForm.Text = "CVR - Interactive Mode";
            Assert.IsFalse(
                sta.MainForm.InvokeRequired,
                $"Expecting confirmation of UI thread context. No marshal is needed.");

            await tcs.Task;
        }
    }

    [TestMethod, Ignore]
    public async Task Test_CVR()
    {
        using var sta = STARunner.CreateThread<CollectionViewRunner>(isVisible: true);
        await sta.RunAsync(localStaTest);

        // Encapsulate the local testing to be done on the STA thread.
        async Task localStaTest()
        {
            Assert.IsFalse(
                sta.MainForm.InvokeRequired,
                $"Expecting confirmation of UI thread context. No marshal is needed.");

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Exercises the InfoText pathway to show and hide tool tip overlay.
    /// </summary>  
    /// <remarks>
    /// This test also demonstrates using a task completion source
    /// to induce an early return via TaskCanceledException (in 
    /// contrast to hanging the test like it did before).
    /// </remarks>
    [TestMethod]
    public async Task Test_InfoText()
    {
        using var sta = STARunner.CreateThread<CollectionViewRunner>(isVisible: true);

        await sta.RunAsync(localStaTest);

        // Encapsulate the local testing to be done on the STA thread.
        async Task localStaTest()
        {
            var cts = new CancellationTokenSource();
            // Early Return
            sta.MainForm.FormClosed += (sender, e) => cts.Cancel();


            Assert.IsInstanceOfType<CollectionViewRunner>(sta.MainForm);
            Assert.IsFalse(
                sta.MainForm.InvokeRequired,
                $"Expecting confirmation of UI thread context. No marshal is needed.");

            sta.MainForm.Text = "CVR - No Tool Tip";
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);

            sta.MainForm.Text = "CVR - Tool Tip Visible";
            sta.MainForm.InfoText = @"
This tool tip is made visible by:

- Setting the InfoTest property on this runner.

It will self-close in a few seconds.";
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);

            sta.MainForm.Text = "CVR - No Tool Tip";
            sta.MainForm.InfoText = string.Empty;
            await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
        }
    }
}
