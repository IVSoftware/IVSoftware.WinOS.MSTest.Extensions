using IVSoftware.WinOS.MSTest.Extensions;

namespace TestBench.MSTest
{
    [TestClass]
    public sealed class TestClass_LatestPublicContract
    {
        [TestMethod]
        public void Test_GetBreakingChanges()
        {
            string actual, expected;
            string baseline;


            subtest_AssemblyOnly();
            subtest_AssemblyAndDependencies();

            #region S U B T E S T S
            void subtest_AssemblyOnly()
            {
                baseline =
                    "TestBench.PublicContractLatest.MSTest.Witness.XBoundObject Version=2.0.3.xml"
                    .ReadManifestResourceFile<TestClass_LatestPublicContract>();
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
            void subtest_AssemblyAndDependencies()
            {
                baseline =
                    "TestBench.PublicContractLatest.MSTest.Witness.XBoundObject Version=2.0.3.Dependencies.xml"
                    .ReadManifestResourceFile<TestClass_LatestPublicContract>();
                string revision =
                    typeof(IVSoftware.Portable.Xml.Linq.XBoundAttribute)
                    .Assembly
                    .ToPublicContract(ManifestTypePolicy.IVSoftwareAssembliesOnly)
                    .ToString();

                if (baseline.IsContractValid(revision, ManifestTypePolicy.IVSoftwareAssembliesOnly))
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
