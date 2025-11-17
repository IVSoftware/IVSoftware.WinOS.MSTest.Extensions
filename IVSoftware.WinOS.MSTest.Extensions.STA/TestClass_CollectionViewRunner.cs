using IVSoftware.WinOS.MSTest.Extensions.STA.WinForms;

namespace IVSoftware.WinOS.MSTest.Extensions.STA;

[TestClass]
public class TestClass_CollectionViewRunner
{
    [TestMethod]
    public async Task Test_CollectionViewRunner()
    {
        using var sta = STARunner.CreateThread<CollectionViewRunner>(isVisible: true);

        await sta.RunAsync(localStaTest);

        // Encapsulate the local testing to be done on the STA thread.
        async Task localStaTest()
        {
            var tcs = new TaskCompletionSource();
            Assert.IsInstanceOfType<CollectionViewRunner>(sta.MainForm);
            sta.MainForm.FormClosed += (sender, e) => tcs.SetResult();
            Assert.IsFalse(
                sta.MainForm.InvokeRequired,
                $"Expecting confirmation of UI thread context. No marshal is needed.");

            sta.MainForm.InfoText = "This is a test.";

            await tcs.Task;
        }
    }
}
