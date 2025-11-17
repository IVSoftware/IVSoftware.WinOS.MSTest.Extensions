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
    /// Exercises the InfoText pathway on a UI form hosted by STARunner.
    /// </summary>  
    /// <remarks>
    /// The test boots a CollectionViewRunner on an isolated STA thread and  
    /// verifies that all updates occur on the correct UI context. It then  
    /// toggles the form’s InfoText property to confirm that the runner  
    /// displays and hides its informational banner as expected, with  
    /// timed pauses allowing visual confirmation during execution.
    /// </remarks>
    [TestMethod]
    public async Task Test_InfoText()
    {
        using var sta = STARunner.CreateThread<CollectionViewRunner>(isVisible: true);

        await sta.RunAsync(localStaTest);

        // Encapsulate the local testing to be done on the STA thread.
        async Task localStaTest()
        {
            Assert.IsInstanceOfType<CollectionViewRunner>(sta.MainForm);
            Assert.IsFalse(
                sta.MainForm.InvokeRequired,
                $"Expecting confirmation of UI thread context. No marshal is needed.");

            sta.MainForm.Text = "CVR - No Tool Tip";
            await Task.Delay(TimeSpan.FromSeconds(2));

            sta.MainForm.Text = "CVR - Tool Tip Visible";
            sta.MainForm.InfoText = @"
This tool tip is made visible by:

- Setting the InfoTest property on this runner.

It will self-close in a few seconds.";
            await Task.Delay(TimeSpan.FromSeconds(5));

            sta.MainForm.Text = "CVR - No Tool Tip";
            sta.MainForm.InfoText = string.Empty;
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
