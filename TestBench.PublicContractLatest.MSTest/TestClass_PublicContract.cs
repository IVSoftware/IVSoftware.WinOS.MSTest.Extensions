using IVSoftware.WinOS.MSTest.Extensions;

namespace TestBench.MSTest
{
    [TestClass]
    public sealed class TestClass_PublicContract
    {
        [TestMethod]
        public void Test_GetBreakingChanges()
        {
            string actual, expected;
            string baseline =
                "TestBench.PublicContractLatest.MSTest.Witness.XBoundObject Version=2.0.3.xml"
                .ReadManifestResourceFile<TestClass_PublicContract>();


            subtest_AssemblyOnly();

            #region S U B T E S T S
            void subtest_AssemblyOnly()
            {
                string revision =
                    typeof(IVSoftware.Portable.Xml.Linq.XBoundAttribute)
                    .Assembly
                    .ToPublicContract()
                    .ToString();

                if (baseline.IsContractValid(revision, ManifestTypePolicy.AssemblyOnly))
                {   /* G T K */
                }
                else
                {
                    var diff = baseline.GetBreakingChanges(revision, ManifestTypePolicy.AssemblyOnly);

                    actual = diff!.ToString(); ;
                    actual.ToClipboardExpected();
                    { }
                    expected = @" 
<breakingChanges policy=""AssemblyOnly"" />";

                    Assert.AreEqual(
                        expected.NormalizeResult(),
                        actual.NormalizeResult(),
                        "Expecting no breaking changes. See diff for more."
                    );
                }
            }
            #endregion S U B T E S T S
        }
    }
}
