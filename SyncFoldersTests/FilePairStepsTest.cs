using SyncFoldersApi;

namespace SyncFoldersTests
{
    [TestFixture]
    [NonParallelizable]
    public class FilePairStepsTest
    {
        //===================================================================================================
        /// <summary>
        /// Inits API resources
        /// </summary>
        //===================================================================================================
        [SetUp]
        public void Setup()
        {
            SyncFoldersApi.Localization.Properties.Resources = new SyncFoldersApiResources();
        }


        //===================================================================================================
        /// <summary>
        /// Tests, if InMemoryFileSystem .CreateTestFile and .IsTestFile work correctly  
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test01_InMemoryCreateFile()
        {
            DateTime dtmToUse = DateTime.Now;
            InMemoryFileSystem oFS = new InMemoryFileSystem();
            List<long> aBlockAt4096 = new List<long>();
            aBlockAt4096.Add(4096);


            // test file is created and recognized as such
            oFS.CreateTestFile("\\\\sim\\dir\\File1.dat", 1, 1024 * 1024, dtmToUse, false, false, null, null, null);
            Assert.IsTrue(oFS.IsTestFile("\\\\sim\\dir\\File1.dat", 1, 1024 * 1024, dtmToUse, false, false, null, null, null));

            // test time difference is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File1.dat", 1, 1024 * 1024, dtmToUse.AddMinutes(-1), false, false, null, null, null));

            // test length difference is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File1.dat", 1, 1024 * 1024 + 1, dtmToUse, false, false, null, null, null));

            // test file index difference is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File1.dat", 2, 1024 * 1024, dtmToUse, false, false, null, null, null));

            // test presence of saved info is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File1.dat", 1, 1024 * 1024 + 1, dtmToUse, true, false, null, null, null));

            // test presence of an erased block is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File1.dat", 1, 1024 * 1024 + 1, dtmToUse, false, false, new List<long>(aBlockAt4096), null, null));

            // test presence of a read error is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File1.dat", 1, 1024 * 1024 + 1, dtmToUse, false, false, null, new List<long>(aBlockAt4096), null));


            // continue with file 2 that has saved info and a different length
            oFS.CreateTestFile("\\\\sim\\dir\\File2.dat", 2, 1024 * 1025, dtmToUse, true, false, null, null, null);
            Assert.IsTrue(oFS.IsTestFile("\\\\sim\\dir\\File2.dat", 2, 1024 * 1025, dtmToUse, true, false, null, null, null));
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File2.dat", 1, 1024 * 1025, dtmToUse, true, false, null, null, null));

            // test that missing saved info is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File2.dat", 2, 1024 * 1025, dtmToUse, false, false, null, null, null));

            // test that another version of fileinfo is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File2.dat", 2, 1024 * 1025, dtmToUse, true, true, null, null, null));

            // test that saved info with errors is recognized
            Assert.IsFalse(oFS.IsTestFile("\\\\sim\\dir\\File2.dat", 2, 1024 * 1025, dtmToUse, true, true, null, null, new List<long>(aBlockAt4096)));
        }
    }

}
