/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024-2025 NataljaNeumann@gmx.de

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

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

            string strPath1 = $@"c:\temp\TestCopyFileSafely.dat";
            string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\TestCopyFileSafely.dat.chk";
            string strPath2 = $@"c:\temp2\TestCopyFileSafely.dat";
            string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\TestCopyFileSafely.dat.chk";

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


        //===================================================================================================
        /// <summary>
        /// Tests CopyRepairSingleFile
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test04_CopyRepairSingleFile()
        {
            int nTotalCores = Environment.ProcessorCount;
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = nTotalCores - 1
            };
            //Parallel.For(0, 1024, options, nConfiguration =>
            for (int nConfiguration = 0; nConfiguration < 64; ++nConfiguration)
            {
               SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                   false,
                   false,
                   false,
                   false,
                   (nConfiguration & 1) != 0,
                   (nConfiguration & 2) != 0,
                   (nConfiguration & 4) != 0,
                   (nConfiguration & 8) != 0,
                   (nConfiguration & 16) != 0,
                   (nConfiguration & 32) != 0);

               for (int nLengthKB = 31; nLengthKB <= 32; ++nLengthKB)
               {
                   for (int nFailingBlock = 0; nFailingBlock < 8; ++nFailingBlock)
                   {
                       for (int nSavedInfoConfig = 0; nSavedInfoConfig < 4; ++nSavedInfoConfig)
                       {
                           // not implemented, so skip
                           if (nSavedInfoConfig == 3 && nFailingBlock > 4)
                               continue;

                           int bApplyRepairToSrc = oSettings.RepairFiles ? 1 : 0;
                           {
                               for (int bFailOnNonrecoverable = 0; bFailOnNonrecoverable <= 1; ++bFailOnNonrecoverable)
                               {
                                   DateTime dtmToUse = DateTime.Now;
                                   DateTime dtmToUse2 = DateTime.Now.AddMinutes(-1);
                                   InMemoryFileSystem oFS = new InMemoryFileSystem();

                                   FilePairSteps oStepsImpl = new FilePairSteps();
                                   HashSetLog oLog = new HashSetLog();

                                   string strPath1 = $@"c:\temp\TestCopyRepairSingleFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat";
                                   string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\TestCopyRepairSingleFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat.chk";
                                   string strPath2 = $@"c:\temp2\TestCopyRepairSingleFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat";
                                   string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\TestCopyRepairSingleFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat.chk";

                                   List<long> aListOfReadErrorsInFile = new List<long>();
                                   aListOfReadErrorsInFile.Add(nFailingBlock * 4096);
                                   List<long> aListOfReadErrorsOtherThanInFile = new List<long>();
                                   aListOfReadErrorsOtherThanInFile.Add(nFailingBlock % 4 == 0 ? 4096 : 0);

                                   oLog.Log.Clear();
                                   oLog.LocalizedLog.Clear();

                                   oFS.CreateTestFile(strPath1, 1,
                                       nLengthKB * 1024, dtmToUse, nSavedInfoConfig > 0, false, null,
                                       new List<long>(aListOfReadErrorsInFile),
                                       nSavedInfoConfig == 2 ? new List<long>(aListOfReadErrorsOtherThanInFile) :
                                       (nSavedInfoConfig == 3 ? new List<long>(aListOfReadErrorsInFile) : null),
                                       true
                                       );

                                   oFS.GetDirectoryInfo("c:\\temp2\\RestoreInfo").Create();


                                   bool bForceCreateInfo = false;
                                   bool bForceCreateInfoTarget = false;

                                   if ((nSavedInfoConfig == 3 || nSavedInfoConfig == 0) && bFailOnNonrecoverable != 0)
                                       Assert.Throws<IOException>(() =>
                                           oStepsImpl.CopyRepairSingleFile(strPath2, strPath1,
                                               strPathSavedInfo1, ref bForceCreateInfo,
                                               ref bForceCreateInfoTarget, "(testing)", "(testing)",
                                               bFailOnNonrecoverable != 0, bApplyRepairToSrc != 0,
                                               oFS, oSettings, oLog));
                                   else
                                       oStepsImpl.CopyRepairSingleFile(strPath2, strPath1,
                                           strPathSavedInfo1, ref bForceCreateInfo,
                                           ref bForceCreateInfoTarget, "(testing)", "(testing)",
                                           bFailOnNonrecoverable != 0, bApplyRepairToSrc != 0,
                                           oFS, oSettings, oLog);


                                   if (bApplyRepairToSrc != 0 && nSavedInfoConfig >= 1 && nSavedInfoConfig <= 2)
                                   {
                                       Assert.True(oFS.IsTestFile(strPath1, 1,
                                       nLengthKB * 1024, dtmToUse, nSavedInfoConfig > 0, false, null,
                                       null,
                                       nSavedInfoConfig == 2 ? new List<long>(aListOfReadErrorsOtherThanInFile) :
                                       (nSavedInfoConfig == 3 ? new List<long>(aListOfReadErrorsInFile) : null)));
                                   }
                                   else
                                   {
                                       // if we don't apply repairs to source then
                                       // source is to be unchanged
                                       Assert.True(oFS.IsTestFile(strPath1, 1,
                                       nLengthKB * 1024, dtmToUse, nSavedInfoConfig > 0, false, null,
                                       new List<long>(aListOfReadErrorsInFile),
                                       nSavedInfoConfig == 2 ? new List<long>(aListOfReadErrorsOtherThanInFile) :
                                       (nSavedInfoConfig == 3 ? new List<long>(aListOfReadErrorsInFile) : null)));
                                   }

                                   if (bFailOnNonrecoverable != 0 && (nSavedInfoConfig == 3 || nSavedInfoConfig == 0))
                                   {
                                       if (nSavedInfoConfig == 3)
                                       {
                                           Assert.IsTrue(oLog.Log.Contains($"I/O Error reading file: \"{strPath1}\", offset {aListOfReadErrorsInFile[0]}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}"));
                                           Assert.IsTrue(oLog.Log.Contains($"There are 1 bad blocks in the file {strPath1}, non-restorable parts: 4096 bytes. Can't proceed there because of non-recoverable, may retry later."));
                                           Assert.AreEqual(2, oLog.Log.Count);
                                           Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);
                                       }
                                       else
                                       {
                                           Assert.AreEqual(0, oLog.Log.Count);
                                           Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);
                                       }
                                   }
                                   else
                                   {


                                       if (nSavedInfoConfig == 3 || nSavedInfoConfig == 0)
                                       {
                                           Assert.IsTrue(oLog.Log.Contains($"Warning: copied {strPath1} to {strPath2} (testing) with errors"));
                                           if (nSavedInfoConfig == 0)
                                           {
                                               Assert.IsTrue(oLog.Log.Contains($"I/O Error while reading file {strPath1} position {aListOfReadErrorsInFile[0]}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}. Block will be replaced with a dummy during copy."));
                                               Assert.AreEqual(2, oLog.Log.Count);
                                               Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);
                                           }
                                           else
                                           {
                                               Assert.IsTrue(oLog.Log.Contains($"I/O Error reading file: \"{strPath1}\", offset {aListOfReadErrorsInFile[0]}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}"));
                                               Assert.IsTrue(oLog.Log.Contains($"There is one bad block in the file {strPath1}, non-restorable parts: 4096 bytes. The file can't be modified because of missing repair option, the restore process will be applied to copy.") ||
                                                             oLog.Log.Contains($"There is one bad block in the file {strPath1}, non-restorable parts: 4096 bytes. "));
                                               Assert.IsTrue(oLog.Log.Contains($"Filling not recoverable block at offset {aListOfReadErrorsInFile[0]} of copied file {strPath2} with a dummy"));
                                               Assert.IsTrue(oLog.Log.Contains($"There was one bad block in the original file, not restored parts in the copy {strPath2}: 4096 bytes."));
                                               Assert.AreEqual(5, oLog.Log.Count);
                                               Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);
                                           }
                                       }
                                       else
                                       {
                                           Assert.IsTrue(oLog.Log.Contains($"I/O Error reading file: \"{strPath1}\", offset {aListOfReadErrorsInFile[0]}: This is a simulated I/O error at position {aListOfReadErrorsInFile[0]}"));
                                           Assert.IsTrue(oLog.Log.Contains($"Recovering block at offset {aListOfReadErrorsInFile[0]} of copied file {strPath2}"));
                                           Assert.IsTrue(oLog.Log.Contains($"There was one bad block in the original file, not restored parts in the copy {strPath2}: 0 bytes."));
                                           Assert.IsTrue(oLog.Log.Contains($"Copied {strPath1} to {strPath2} (testing)"));
                                           Assert.AreEqual(oLog.Log.Count, oLog.LocalizedLog.Count);
                                           if (bApplyRepairToSrc != 0)
                                           {
                                               Assert.AreEqual(6, oLog.Log.Count);
                                               Assert.IsTrue(oLog.Log.Contains($"There is one bad block in the file {strPath1}, non-restorable parts: 0 bytes. "));
                                               Assert.IsTrue(oLog.Log.Contains($"Recovering block at offset {aListOfReadErrorsInFile[0]} of file {strPath1}"));
                                           }
                                           else
                                           {
                                               Assert.AreEqual(5, oLog.Log.Count);
                                               Assert.IsTrue(oLog.Log.Contains($"There is one bad block in the file {strPath1}, non-restorable parts: 0 bytes. The file can't be modified because of missing repair option, the restore process will be applied to copy."));
                                           }
                                       }
                                   }
                               }
                           }

                       }
                   }

               }
            }
            //);

        }




        //===================================================================================================
        /// <summary>
        /// Tests TestAndRepairSeconddFile
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test05_TestAndRepairSecondFile()
        {
            int nTotalCores = Environment.ProcessorCount;
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = nTotalCores - 1
            };
            //Parallel.For(0, 1024, options, nConfiguration =>
            for (int nConfiguration = 0; nConfiguration < 4; ++nConfiguration)
            {
                SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                    true,
                    true,
                    false,
                    false,
                    true,
                    true,
                    true,
                    true,
                    (nConfiguration & 1) != 0,
                    (nConfiguration & 2) != 0);

                DateTime dtmToUse = DateTime.Now;

                for (int nLengthKB = 31; nLengthKB <= 32; ++nLengthKB)
                {
                    for (int nFailingBlockConfig = 0; nFailingBlockConfig <= 2; ++nFailingBlockConfig)
                    {
                        // the first, the middle or the last
                        int nFailingBlock = 7 * nFailingBlockConfig / 2;

                        for (int nSavedInfoConfig = 0; nSavedInfoConfig <= 4; ++nSavedInfoConfig)
                        {
                            InMemoryFileSystem oFS = new InMemoryFileSystem();

                            string strPath1 = $@"c:\temp\TestAndRepairSecondFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat";
                            string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\TestAndRepairSecondFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat.chk";
                            string strPath2 = $@"c:\temp2\TestAndRepairSecondFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat";
                            string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\TestAndRepairSecondFile{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat.chk";

                            FilePairSteps oStepsImpl = new FilePairSteps();
                            HashSetLog oLog = new HashSetLog();


                            List<long> aListOfReadErrorsInFile2 = new List<long>();
                            aListOfReadErrorsInFile2.Add(nFailingBlock * 4096);

                            List<long> aListOfReadErrorsInFile1 = new List<long>();

                            // the block of second file will be non-recoverable using block from first file
                            if (nSavedInfoConfig >=1)
                                aListOfReadErrorsInFile1.Add(nFailingBlock * 4096);

                            List<long> aListOfReadErrorsInSavedInfo = new List<long>();
                            if (nSavedInfoConfig == 2 || nSavedInfoConfig == 3)
                            {
                                // the block of second file will be recoverable using saved info
                                aListOfReadErrorsInSavedInfo.Add(((nFailingBlock + 1) % 4) * 4096);
                            } else if (nSavedInfoConfig == 4)
                            {
                                // the bloc of second file will be non-recoverable using saved info
                                aListOfReadErrorsInSavedInfo.Add(((nFailingBlock) % 4) * 4096);
                            }


                            oLog.Log.Clear();
                            oLog.LocalizedLog.Clear();


                            oFS.CreateTestFile(strPath1, 0,
                                nLengthKB * 1024, dtmToUse,
                                nSavedInfoConfig == 2, false, null,
                                nSavedInfoConfig >= 1 ? new List<long>(aListOfReadErrorsInFile1) : null,
                                nSavedInfoConfig == 2 ? new List<long>(aListOfReadErrorsInSavedInfo) : null,
                                false);

                            oFS.CreateTestFile(strPath2, 0,
                                nLengthKB * 1024, dtmToUse,
                                nSavedInfoConfig >= 3, false, null,
                                new List<long>(aListOfReadErrorsInFile2),
                                nSavedInfoConfig >= 3 ? new List<long>(aListOfReadErrorsInSavedInfo) : null,
                                true);

                            bool bForceCreateInfo = false;
                            oStepsImpl.TestAndRepairSecondFile(strPath1, strPath2,
                                strPathSavedInfo1, strPathSavedInfo2,
                                ref bForceCreateInfo, oFS, oSettings, oLog);

                            Assert.True(oFS.IsTestFile(strPath1, 0,
                                nLengthKB * 1024, dtmToUse,
                                nSavedInfoConfig == 2, false, null,
                                nSavedInfoConfig >= 1 ? new List<long>(aListOfReadErrorsInFile1) : null,
                                nSavedInfoConfig == 2 ? new List<long>(aListOfReadErrorsInSavedInfo) : null));

                            Assert.True(oFS.IsTestFile(strPath2, 0,
                                nLengthKB * 1024,
                                (nSavedInfoConfig == 1 || nSavedInfoConfig == 4) ? null: dtmToUse,
                                nSavedInfoConfig >= 3, false,
                                (nSavedInfoConfig == 1 || nSavedInfoConfig == 4)? new List<long>(aListOfReadErrorsInFile1) : null,
                                null ,
                                nSavedInfoConfig >= 3 ? new List<long>(aListOfReadErrorsInSavedInfo) : null));


                        }
                    }

                }
            }
            //);

        }


        //===================================================================================================
        /// <summary>
        /// Tests CreateSavedInfoAndCopy
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test06_CreateSavedInfoAndCopy()
        {
            // we actually don't need anything from configuration, but need to provide one
            SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                false, false, false, false, false, false, false, false, false, false);

            DateTime dtmToUse = DateTime.Now;

            InMemoryFileSystem oFS = new InMemoryFileSystem();

            for (int nLengthKB = 31; nLengthKB <= 64; ++nLengthKB)
            {

                string strPath1 = $@"c:\temp\CreateSavedInfoAndCopy{nLengthKB}.dat";
                string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\CreateSavedInfoAndCopy{nLengthKB}.dat.chk";
                string strPath2 = $@"c:\temp2\CreateSavedInfoAndCopy{nLengthKB}.dat";
                string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\CreateSavedInfoAndCopy{nLengthKB}.dat.chk";

                FilePairSteps oStepsImpl = new FilePairSteps();
                HashSetLog oLog = new HashSetLog();


                if (nLengthKB == 33 || nLengthKB == 36)
                {
                    List<long> aListOfReadErrorsInFile1 = new List<long>();
                    aListOfReadErrorsInFile1.Add(0);

                    oFS.CreateTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        new List<long>(aListOfReadErrorsInFile1),
                        null,
                        false);

                    oFS.CreateTestFile(strPath2, 0,
                        1, dtmToUse,
                        false, false, null,
                        null,
                        null,
                        true);

                    Assert.IsFalse(oStepsImpl.CreateSavedInfoAndCopy(
                        strPath1, strPathSavedInfo2, strPath2, "(testing)", "(testing)", oFS, oSettings, oLog));

                    Assert.IsTrue(oFS.IsTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        new List<long>(aListOfReadErrorsInFile1),
                        null));

                    Assert.IsTrue(oFS.IsTestFile(strPath2, 0,
                        1, dtmToUse,
                        false, false, null,
                        null,
                        null));
                } else
                {

                    oFS.CreateTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        null,
                        null,
                        true);

                    // ensure dest directory exists
                    oFS.GetFileInfo(strPath2).Directory.Create();

                    Assert.IsTrue(oStepsImpl.CreateSavedInfoAndCopy(
                        strPath1, strPathSavedInfo1, strPath2, "(testing)", "(testing)", oFS, oSettings, oLog));

                    Assert.IsTrue(oFS.IsTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        true, false, null,
                        null,
                        null));

                    Assert.IsTrue(oFS.IsTestFile(strPath2, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        null,
                        null));
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// Tests TestAndRepairTwoFiles
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test07_TestAndRepairTwoFiles()
        {
            int nTotalCores = Environment.ProcessorCount;
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = nTotalCores - 1
            };
            //Parallel.For(0, 1024, options, nConfiguration =>
            for (int nConfiguration = 0; nConfiguration < 4; ++nConfiguration)
            {
                SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                    false,
                    false,
                    false,
                    false,
                    true,
                    true,
                    true,
                    true,
                    (nConfiguration & 1) != 0,
                    (nConfiguration & 2) != 0);

                DateTime dtmToUse = DateTime.Now;

                for (int nLengthKB = 31; nLengthKB <= 32; ++nLengthKB)
                {
                    for (int nFailingBlockConfig = 0; nFailingBlockConfig <= 2; ++nFailingBlockConfig)
                    {
                        // the first, the middle or the last
                        int nFailingBlock = 7 * nFailingBlockConfig / 2;

                        for (int nSavedInfoConfig = 0; nSavedInfoConfig <= 4; ++nSavedInfoConfig)
                        {
                            for (int bSwitchParams = 0; bSwitchParams <= 1; ++bSwitchParams)
                            {
                                InMemoryFileSystem oFS = new InMemoryFileSystem();

                                string strPath1 = $@"c:\temp\TestAndRepairTwoFiles{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat";
                                string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\TestAndRepairTwoFiles{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat.chk";
                                string strPath2 = $@"c:\temp2\TestAndRepairTwoFiles{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat";
                                string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\TestAndRepairTwoFiles{nLengthKB}_{nFailingBlock}_{nSavedInfoConfig}.dat.chk";

                                FilePairSteps oStepsImpl = new FilePairSteps();
                                HashSetLog oLog = new HashSetLog();


                                List<long> aListOfReadErrorsInFile2 = new List<long>();
                                aListOfReadErrorsInFile2.Add(nFailingBlock * 4096);

                                List<long> aListOfReadErrorsInFile1 = new List<long>();

                                // the block of second file will be non-recoverable using block from first file
                                if (nSavedInfoConfig >= 1)
                                    aListOfReadErrorsInFile1.Add(nFailingBlock * 4096);

                                List<long> aListOfReadErrorsInSavedInfo = new List<long>();
                                if (nSavedInfoConfig == 2 || nSavedInfoConfig == 3)
                                {
                                    // the block of second file will be recoverable using saved info
                                    aListOfReadErrorsInSavedInfo.Add(((nFailingBlock + 1) % 4) * 4096);
                                }
                                else if (nSavedInfoConfig == 4)
                                {
                                    // the bloc of second file will be non-recoverable using saved info
                                    aListOfReadErrorsInSavedInfo.Add(((nFailingBlock) % 4) * 4096);
                                }


                                oLog.Log.Clear();
                                oLog.LocalizedLog.Clear();


                                oFS.CreateTestFile(strPath1, 0,
                                    nLengthKB * 1024, dtmToUse,
                                    nSavedInfoConfig == 2, false, null,
                                    nSavedInfoConfig >= 1 ? new List<long>(aListOfReadErrorsInFile1) : null,
                                    nSavedInfoConfig == 2 ? new List<long>(aListOfReadErrorsInSavedInfo) : null,
                                    false);

                                oFS.CreateTestFile(strPath2, 0,
                                    nLengthKB * 1024, dtmToUse,
                                    nSavedInfoConfig >= 3, false, null,
                                    new List<long>(aListOfReadErrorsInFile2),
                                    nSavedInfoConfig >= 3 ? new List<long>(aListOfReadErrorsInSavedInfo) : null,
                                    true);

                                bool bForceCreateInfo = false;

                                if (bSwitchParams != 0)
                                {
                                    // this is the repair of first from second
                                    oStepsImpl.TestAndRepairTwoFiles(strPath1, strPath2,
                                        strPathSavedInfo1, strPathSavedInfo2,
                                        ref bForceCreateInfo, oFS, oSettings, oLog);
                                }
                                else
                                {
                                    // this is the repair of second from first (should be symmetric)
                                    oStepsImpl.TestAndRepairTwoFiles(strPath2, strPath1,
                                        strPathSavedInfo2, strPathSavedInfo1,
                                        ref bForceCreateInfo, oFS, oSettings, oLog);
                                }

                                Assert.True(oFS.IsTestFile(strPath1, 0,
                                    nLengthKB * 1024,
                                    (nSavedInfoConfig == 1 || nSavedInfoConfig == 4) ? null : dtmToUse,
                                    nSavedInfoConfig == 2, false,
                                    (nSavedInfoConfig == 1 || nSavedInfoConfig == 4) ? new List<long>(aListOfReadErrorsInFile1) : null,
                                    null,
                                    nSavedInfoConfig == 2 ? new List<long>(aListOfReadErrorsInSavedInfo) : null));

                                Assert.True(oFS.IsTestFile(strPath2, 0,
                                    nLengthKB * 1024,
                                    (nSavedInfoConfig == 1 || nSavedInfoConfig == 4) ? null : dtmToUse,
                                    nSavedInfoConfig >= 3, false,
                                    (nSavedInfoConfig == 1 || nSavedInfoConfig == 4) ? new List<long>(aListOfReadErrorsInFile1) : null,
                                    null,
                                    nSavedInfoConfig >= 3 ? new List<long>(aListOfReadErrorsInSavedInfo) : null));
                            }


                        }
                    }

                }
            }
            //);

        }

        //===================================================================================================
        /// <summary>
        /// Tests Create2SavedsInfosAndCopy
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test08_Create2SavedInfosAndCopy()
        {
            // we actually don't need anything from configuration, but need to provide one
            SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                false, false, false, false, false, false, false, false, false, false);

            DateTime dtmToUse = DateTime.Now;

            InMemoryFileSystem oFS = new InMemoryFileSystem();

            for (int nLengthKB = 31; nLengthKB <= 64; ++nLengthKB)
            {

                string strPath1 = $@"c:\temp\Create2SavedInfosAndCopy{nLengthKB}.dat";
                string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\Create2SavedInfosAndCopy{nLengthKB}.dat.chk";
                string strPath2 = $@"c:\temp2\Create2SavedInfosAndCopy{nLengthKB}.dat";
                string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\Create2SavedInfosAndCopy{nLengthKB}.dat.chk";

                FilePairSteps oStepsImpl = new FilePairSteps();
                HashSetLog oLog = new HashSetLog();


                if (nLengthKB == 33 || nLengthKB == 36)
                {
                    List<long> aListOfReadErrorsInFile1 = new List<long>();
                    aListOfReadErrorsInFile1.Add(0);

                    oFS.CreateTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        new List<long>(aListOfReadErrorsInFile1),
                        null,
                        false);

                    oFS.CreateTestFile(strPath2, 0,
                        1, dtmToUse,
                        false, false, null,
                        null,
                        null,
                        true);

                    Assert.IsFalse(oStepsImpl.Create2SavedInfosAndCopy(
                        strPath1, strPathSavedInfo1, strPath2, strPathSavedInfo2, 
                        "(testing)", "(testing)", oFS, oSettings, oLog));

                    Assert.IsTrue(oFS.IsTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        new List<long>(aListOfReadErrorsInFile1),
                        null));

                    Assert.IsTrue(oFS.IsTestFile(strPath2, 0,
                        1, dtmToUse,
                        false, false, null,
                        null,
                        null));
                }
                else
                {

                    oFS.CreateTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        null,
                        null,
                        true);

                    // ensure dest directory exists
                    oFS.GetFileInfo(strPath2).Directory.Create();

                    Assert.IsTrue(oStepsImpl.Create2SavedInfosAndCopy(
                        strPath1, strPathSavedInfo1, strPath2, strPathSavedInfo2, 
                        "(testing)", "(testing)", oFS, oSettings, oLog));

                    Assert.IsTrue(oFS.AreTwoTestFiles(
                        strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        true, null,
                        null, null,

                        strPath2,
                        nLengthKB,
                        true,
                        null,
                        null,  null)
                        );
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// Tests Create2SavedsInfos
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test09_Create2SavedInfos()
        {
            // we actually don't need anything from configuration, but need to provide one
            SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                false, false, false, false, false, false, false, false, false, false);

            DateTime dtmToUse = DateTime.Now;

            InMemoryFileSystem oFS = new InMemoryFileSystem();

            for (int nLengthKB = 31; nLengthKB <= 64; ++nLengthKB)
            {

                string strPath1 = $@"c:\temp\Create2SavedInfos{nLengthKB}.dat";
                string strPathSavedInfo1 = $@"c:\temp\RestoreInfo\Create2SavedInfos{nLengthKB}.dat.chk";
                string strPath2 = $@"c:\temp2\Create2SavedInfos{nLengthKB}.dat";
                string strPathSavedInfo2 = $@"c:\temp2\RestoreInfo\Create2SavedInfos{nLengthKB}.dat.chk";

                FilePairSteps oStepsImpl = new FilePairSteps();
                HashSetLog oLog = new HashSetLog();


                if (nLengthKB == 33 || nLengthKB == 36)
                {
                    List<long> aListOfReadErrorsInFile1 = new List<long>();
                    aListOfReadErrorsInFile1.Add(0);

                    oFS.CreateTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        new List<long>(aListOfReadErrorsInFile1),
                        null,
                        false);

                    oFS.CreateTestFile(strPath2, 0,
                        1, dtmToUse,
                        false, false, null,
                        null,
                        null,
                        true);

                    Assert.IsFalse(oStepsImpl.Create2SavedInfos(
                        strPath1, strPathSavedInfo1, strPathSavedInfo2,
                        oFS, oSettings, oLog));

                    Assert.IsTrue(oFS.IsTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        new List<long>(aListOfReadErrorsInFile1),
                        null));

                    Assert.IsTrue(oFS.IsTestFile(strPath2, 0,
                        1, dtmToUse,
                        false, false, null,
                        null,
                        null));
                }
                else
                {

                    oFS.CreateTestFile(strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        null,
                        null,
                        true);

                    oFS.CreateTestFile(strPath2, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        false, false, null,
                        null,
                        null,
                        true);

                    // ensure dest directory exists
                    oFS.GetFileInfo(strPath2).Directory.Create();

                    Assert.IsTrue(oStepsImpl.Create2SavedInfos(
                        strPath1, strPathSavedInfo1, strPathSavedInfo2,
                        oFS, oSettings, oLog));

                    Assert.IsTrue(oFS.AreTwoTestFiles(
                        strPath1, nLengthKB,
                        nLengthKB * 1024, dtmToUse,
                        true, null,
                        null, null,

                        strPath2,
                        nLengthKB,
                        true,
                        null,
                        null, null)
                        );
                }
            }
        }


    }
}
