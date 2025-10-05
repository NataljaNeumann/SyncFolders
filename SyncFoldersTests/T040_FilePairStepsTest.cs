using SyncFoldersApi;

#pragma warning disable NUnit2005 

namespace SyncFoldersTests
{
    [TestFixture]
    [NonParallelizable]
    public class T040_FilePairStepsTest
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

        //===================================================================================================
        /// <summary>
        /// Tests TestAndRepairSingleFile
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test02_RepairFileInPlace()
        {
            // the called method uses only CancelClicked... no need to test all configs
            SettingsAndEnvironment oConfig = new SettingsAndEnvironment(
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false
                );

            DateTime dtmToUse = DateTime.Now;
            InMemoryFileSystem oFS = new InMemoryFileSystem();

            FilePairSteps oStepsImpl = new FilePairSteps();
            HashSetLog oLog = new HashSetLog();

            int nLengthKB = 33;

            for (int i = 0; i < 4; ++i)
            {
                for (int j = 0; j < 4; ++j)
                {
                    string strPath = $@"c:\temp\test{i}_{j}.dat";
                    string strPathSavedInfo = $@"c:\temp\RestoreInfo\test{i}_{j}.dat.chk";

                    List<long> aListOfReadErrorsInFile = new List<long>();
                    aListOfReadErrorsInFile.Add(i * 4096);

                    List<long> aListOfReadErrorsInSavedInfo = new List<long>();
                    aListOfReadErrorsInSavedInfo.Add(j * 4096);

                    oFS.CreateTestFile(strPath, i * 4 + j,
                        nLengthKB * 1024 + 1024 * (j % 4), dtmToUse, true, false, null,
                        new List<long>(aListOfReadErrorsInFile),
                        new List<long>(aListOfReadErrorsInSavedInfo),
                        true
                        );

                    //oStepsImpl.CreateSavedInfo(strPath, strPathSavedInfo, oFS, oConfig, oLog);


                    oLog.Log.Clear();
                    oLog.LocalizedLog.Clear();

                    bool bForceCreateInfo = false;
                    bool bResult = oStepsImpl.TestAndRepairSingleFile(strPath, strPathSavedInfo, ref bForceCreateInfo, true, oFS, oConfig, oLog);

                    if (i == j)
                    {
                        Assert.IsFalse(bResult);
                        Assert.IsTrue(oFS.IsTestFile(strPath, i * 4 + j,
                            nLengthKB * 1024 + 1024 * (j % 4), dtmToUse, true, false, null,
                            new List<long>(aListOfReadErrorsInFile),
                            new List<long>(aListOfReadErrorsInSavedInfo)
                            ));
                        Assert.IsTrue(bForceCreateInfo);

                        Assert.IsTrue(oLog.Log.Contains($"I/O Error reading file: \"{strPath}\", offset {aListOfReadErrorsInFile[0]}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}"));
                        Assert.IsTrue(oLog.Log.Contains($"There is one bad block in the file {strPath} and it can't be restored: 4096 bytes, file can't be used as backup"));
                        Assert.AreEqual(2, oLog.Log.Count);
                        Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);

                        bForceCreateInfo = false;
                        bResult = oStepsImpl.TestAndRepairSingleFile(strPath, strPathSavedInfo, ref bForceCreateInfo, false, oFS, oConfig, oLog);

                        Assert.IsFalse(bResult);
                        Assert.IsTrue(oFS.IsTestFile(strPath, i * 4 + j,
                            nLengthKB * 1024 + 1024 * (j % 4), null, true, false,
                            new List<long>(aListOfReadErrorsInFile),
                            null,
                            new List<long>(aListOfReadErrorsInSavedInfo)
                            ));
                        Assert.IsTrue(bForceCreateInfo);
                        Assert.IsTrue(oLog.Log.Contains($"I/O Error reading file: \"{strPath}\", offset {aListOfReadErrorsInFile[0]}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}"));
                        Assert.IsTrue(oLog.Log.Contains($"There is one bad block in the file {strPath} and it can't be restored: 4096 bytes, file can't be used as backup"));
                        Assert.IsTrue(oLog.Log.Contains($"Filling not recoverable block at offset {aListOfReadErrorsInFile[0]} with a dummy block"));
                        Assert.IsTrue(oLog.Log.Contains($"There was one bad block in the file {strPath}, not restored parts: 4096 bytes"));
                        Assert.AreEqual(4, oLog.Log.Count);
                        Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);

                    }
                    else
                    {
                        Assert.IsTrue(bResult);
                        Assert.IsTrue(oFS.IsTestFile(strPath, i * 4 + j,
                            nLengthKB * 1024 + 1024 * (j % 4), dtmToUse, true, false, null,
                            null,
                            new List<long>(aListOfReadErrorsInSavedInfo)
                            ));
                        Assert.IsTrue(bForceCreateInfo);

                        Assert.IsTrue(oLog.Log.Contains($"I/O Error reading file: \"{strPath}\", offset {aListOfReadErrorsInFile[0]}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}"));
                        Assert.IsTrue(oLog.Log.Contains($"Recovering block at offset {aListOfReadErrorsInFile[0]} of the file {strPath}"));
                        Assert.IsTrue(oLog.Log.Contains($"There was one bad block in the file {strPath}, not restored parts: 0 bytes"));
                        Assert.IsTrue(oLog.Log.Contains($"Saved info file \"{strPathSavedInfo}\" has been damaged and needs to be recreated from \"{strPath}\""));
                        Assert.AreEqual(4, oLog.Log.Count);
                        Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);

                    }
                }
            }

            for (int i = 0; i <= 1; ++i)
            {
                string strPath = $@"c:\temp\test_lastblock{i}.dat";
                string strPathSavedInfo = $@"c:\temp\RestoreInfo\test_lastblock{i}.dat.chk";

                List<long> aListOfReadErrorsInFile = new List<long>();
                aListOfReadErrorsInFile.Add(nLengthKB * 1024 - 1);

                oFS.CreateTestFile(strPath, i,
                    nLengthKB * 1024 + i * 1024 * ((4 - (nLengthKB % 4)) % 4), dtmToUse, true, false, null,
                    new List<long>(aListOfReadErrorsInFile),
                    null,
                    true
                    );

                oLog.Log.Clear();
                oLog.LocalizedLog.Clear();

                bool bForceCreateInfo = false;
                bool bResult = oStepsImpl.TestAndRepairSingleFile(strPath, strPathSavedInfo, ref bForceCreateInfo, true, oFS, oConfig, oLog);

                Assert.IsTrue(bResult);
                Assert.IsTrue(oFS.IsTestFile(strPath, i,
                    nLengthKB * 1024 + i * 1024 * ((4 - (nLengthKB % 4)) % 4), dtmToUse, true, false, null,
                    null,
                    null
                    ));
                Assert.IsFalse(bForceCreateInfo);

                Assert.IsTrue(oLog.Log.Contains($"I/O Error reading file: \"{strPath}\", offset {(aListOfReadErrorsInFile[0]) / 4096 * 4096}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}"));
                Assert.IsTrue(oLog.Log.Contains($"Recovering block at offset {(aListOfReadErrorsInFile[0]) / 4096 * 4096} of the file {strPath}"));
                Assert.IsTrue(oLog.Log.Contains($"There was one bad block in the file {strPath}, not restored parts: 0 bytes"));
                Assert.AreEqual(3, oLog.Log.Count);
                Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);

            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests TestAndRepairSingleFile
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test03_CopyFileSafely()
        {
            DateTime dtmToUse = DateTime.Now;
            DateTime dtmToUse2 = DateTime.Now.AddMinutes(-1);
            InMemoryFileSystem oFS = new InMemoryFileSystem();

            FilePairSteps oStepsImpl = new FilePairSteps();
            HashSetLog oLog = new HashSetLog();

            int nLengthB = 16;

            string strPath1 = $@"c:\temp\testCopyFileSafely.dat";
            string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\testCopyFileSafely.dat.chk";
            string strPath2 = $@"c:\temp2\testCopyFileSafely.dat";
            string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\testCopyFileSafely.dat.chk";

            List<long> aListOfReadErrorsInFile = new List<long>();
            aListOfReadErrorsInFile.Add(0);

            oFS.CreateTestFile(strPath1, 1,
                nLengthB, dtmToUse, false, false, null,
                new List<long>(aListOfReadErrorsInFile),
                null,
                true
                );

            oFS.CreateTestFile(strPath2, 2,
                nLengthB, dtmToUse, true, false, null,
                null,
                null,
                true
                );


            oLog.Log.Clear();
            oLog.LocalizedLog.Clear();

            Assert.Throws<IOException>(() => oStepsImpl.CopyFileSafely(oFS.GetFileInfo(strPath1), strPath2, "testing file", "testing file", oFS, oLog));
            Assert.True(oFS.IsTestFile(strPath1, 1,
                nLengthB, dtmToUse, false, false, null,
                new List<long>(aListOfReadErrorsInFile),
                null
                ));
            Assert.True(oFS.IsTestFile(strPath2, 2,
                nLengthB, dtmToUse, true, false, null,
                null,
                null
                ));
            Assert.AreEqual(0, oLog.Log.Count);
            Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);


            oLog.Log.Clear();
            oLog.LocalizedLog.Clear();
            oStepsImpl.CopyFileSafely(oFS.GetFileInfo(strPath2), strPath1, "testing file", "testing file", oFS, oLog);

            Assert.IsTrue(oLog.Log.Contains($"Copied {strPath2} to {strPath1} testing file"));
            Assert.AreEqual(1, oLog.Log.Count);
            Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);

            Assert.True(oFS.IsTestFile(strPath1, 2,
                nLengthB, dtmToUse, false, false, null,
                null,
                null
                ));
            Assert.True(oFS.IsTestFile(strPath2, 2,
                nLengthB, dtmToUse, true, false, null,
                null,
                null
                ));

        }
    }

}

