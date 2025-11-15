using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
            string actual, expected;
            var builder = new List<string>();

            STARunner sta;
            using (sta = new STARunner(isVisible: false))
            {
                await sta.RunAsync(async () =>
                {
                    sta.MainForm.Text = "Main Form";
                    await Task.CompletedTask;
                });

                using (Form popup = await sta.RunAsync(async () =>
                {
                    var popup = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        StartPosition = FormStartPosition.Manual,
                        Size = new Size(300, 100)
                    };

                    var label = new Label
                    {
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Consolas", 16, FontStyle.Bold),
                        ForeColor = Color.White,
                        BackColor = ColorTranslator.FromHtml("#444444"),
                    };

                    popup.Controls.Add(label);
                    popup.Tag = label;

                    popup.Show(null);
                    popup.CycleTopmost();
                    await Task.CompletedTask;
                    return popup;
                }))
                {
                    for (int countdown = 5; countdown >= 0; countdown--)
                    {
                        var msg = $"Shutdown in {countdown}";

                        await Task.Delay(TimeSpan.FromSeconds(1));
                        await sta.RunAsync(async () =>
                        {
                            if (popup.Tag is Label lbl) lbl.Text = msg;
                            sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                            builder.Add(sta.MainForm.Text);
                            Debug.WriteLine($"SilentDisposalWithNOOP: {sta.MainForm.Text}");
                            await Task.CompletedTask;
                        });
                    }
                }
            }

            actual = string.Join(Environment.NewLine, builder);
            actual.ToClipboardExpected();
            { }
            expected = @" 
Main Form - Shutdown in 5
Main Form - Shutdown in 4
Main Form - Shutdown in 3
Main Form - Shutdown in 2
Main Form - Shutdown in 1
Main Form - Shutdown in 0";

            Assert.AreEqual(
                expected.NormalizeResult(),
                actual.NormalizeResult(),
                "Expecting loopback of shutdown messages for invisible form."
            );
        }

        [TestMethod]
        public async Task Test_CanonicalPOC()
        {
            string actual, expected;
            var builder = new List<string>();

            using var sta = new STARunner(isVisible: false);
            await sta.RunAsync(localStaTest);

            #region L o c a l F x 
            async Task localStaTest()
            {
                Assert.IsFalse(sta.MainForm.InvokeRequired);
                await Task.CompletedTask;
            }
            #endregion L o c a l F x
        }


        [TestMethod]
        public async Task Test_MonolithicVisible()
        {
            using var sta = new STARunner(isVisible: true);

            await sta.RunAsync(localTestOnStaThread);

            #region L o c a l F x 
            async Task localTestOnStaThread()
            {
                Assert.IsFalse(sta.MainForm.InvokeRequired);

                sta.MainForm.Text = "Main Form";

                // Mutate UI normally
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                    await Task.Delay(1000); // allowed inside UI callback
                }
            }
            #endregion L o c a l F x
        }

        [TestMethod]
        public async Task Test_MonolithicSilent()
        {
            using var sta = new STARunner(isVisible: false);

            await sta.RunAsync(localTestOnStaThread);

            #region L o c a l F x 
            async Task localTestOnStaThread()
            {
                Assert.IsFalse(sta.MainForm.InvokeRequired);

                sta.MainForm.Text = "Main Form";

                // Build UI in place
                var popup = new Form
                {
                    Size = new Size(300, 100),
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = Color.Black
                };

                var label = new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Consolas", 16, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = ColorTranslator.FromHtml("#444444")
                };

                popup.Controls.Add(label);
                popup.Show();

                // Stabilize layout + paint before we ever await
                popup.PerformLayout();
                popup.Update();

                // Mutate UI normally
                for (int countdown = 5; countdown >= 0; countdown--)
                {
                    label.Text = $"Shutdown in {countdown}";
                    sta.MainForm.Text = $"Main Form - Shutdown in {countdown}";
                    await Task.Delay(1000); // allowed inside UI callback
                }
            };
            #endregion L o c a l F x
        }
    }
}
