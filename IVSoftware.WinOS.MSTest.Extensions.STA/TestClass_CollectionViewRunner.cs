using IVSoftware.WinOS.MSTest.Extensions.STA.WinForms;

namespace IVSoftware.WinOS.MSTest.Extensions.STA;

[TestClass]
public class TestClass_CollectionViewRunner
{
    [TestMethod]
    public async Task Test_CollectionViewRunner()
    {
        using var sta = STARunner.CreateThread<CollectionViewRunner>(isVisible: true);

        var tcs = new TaskCompletionSource();

        await sta.WaitReadyAsync(); // Wait for form to exist.
        sta.MainForm.FormClosed += (sender, e) => tcs.SetResult();

        await sta.RunAsync(localStaTest);

        // Encapsulate the local testing to be done on the STA thread.
        async Task localStaTest()
        {
            Assert.IsFalse(
                sta.MainForm.InvokeRequired,
                $"Expecting confirmation of UI thread context. No marshal is needed.");
            await Task.CompletedTask;
        }
        await tcs.Task;
    }
}
