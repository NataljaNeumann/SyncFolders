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
using SyncFoldersApi.Localization;
using System.Security.Cryptography;

#pragma warning disable NUnit2005 


namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// This test fixture tests most common situations, ensuring the most common
    /// situations are handled correctly
    /// </summary>
    //*******************************************************************************************************
    public class T090_MostCommonSituations
    {

        //===================================================================================================
        /// <summary>
        /// Sets up the resources, needed by the API
        /// </summary>
        //===================================================================================================
        [SetUp]
        public void Setup()
        {
            Properties.Resources = new SyncFoldersApiResources();
        }

        //===================================================================================================
        /// <summary>
        /// This test 
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test01_NewFileSingleFolder()
        {
            Random oRandom = new Random((int)DateTime.UtcNow.Ticks);
            foreach (int nFileSizeKB in new int[] {
                0, 1, 4, 16, 17,
                6 * 1024,   6 * 1024 + 1,
                200 * 1024, 200 * 1024 + 1,
                512 * 1024, 512 * 1024 + 1 })
            {
                for (int nConfiguration = 0; nConfiguration < 1024; ++nConfiguration)
                {
                    // skip testing all configurations for big files 
                    if (nConfiguration > 0 && nFileSizeKB > 32)
                        continue;

                    bool bProcessLastBlock = (oRandom.Next() & 1) != 0;

                    SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                        (nConfiguration & 1) != 0,
                        (nConfiguration & 2) != 0,
                        (nConfiguration & 4) != 0,
                        (nConfiguration & 8) != 0,
                        (nConfiguration & 16) != 0,
                        (nConfiguration & 32) != 0,
                        (nConfiguration & 64) != 0,
                        (nConfiguration & 128) != 0,
                        (nConfiguration & 256) != 0,
                        (nConfiguration & 512) != 0);



                    string strDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                    string strPath1 = Path.Combine(strDir, $@"TestSingleFile{nFileSizeKB}K-{oSettings}.dat");
                    string strPath1_1 = (" " + strPath1).Substring(1);
                    string strPathSavedInfo1 = Path.Combine(strDir,
                        $@"RestoreInfo{Path.DirectorySeparatorChar}TestSingleFile{nFileSizeKB}K-{oSettings}.dat.chk");
                    DateTime dtmTimeToUse = DateTime.UtcNow;

                    InMemoryFileSystem oFs = new InMemoryFileSystem();
                    oFs.GetDirectoryInfo(strDir).Create();
                    oFs.GetDirectoryInfo(Path.Combine(strDir, "RestoreInfo"));

                    oFs.CreateTestFile(strPath1, 1, nFileSizeKB * 1024, dtmTimeToUse, false, false,
                        null, null, null, true);


                    FilePairStepChooser oAlgorithm = new FilePairStepChooser();
                    FilePairStepsDirectionLogic oLogic = new FilePairStepsDirectionLogic();
                    FilePairSteps oSteps = new FilePairSteps();
                    HashSetLog oLog = new HashSetLog();

                    if (oSettings.TestFiles && oSettings.RepairFiles && oSettings.FirstToSecond && oSettings.FirstReadOnly)
                    {
                        try
                        {
                            oAlgorithm.ProcessFilePair(strPath1, strPath1_1,
                                oFs, oSettings, oLogic, oSteps, oLog);

                            Assert.Fail("The previous statement should have thrown an exception!");

                        }
                        catch
                        {
                            // expected
                        }
                    }
                    else
                    {
                        oAlgorithm.ProcessFilePair(strPath1, strPath1_1,
                            oFs, oSettings, oLogic, oSteps, oLog);


                        Assert.IsTrue(oFs.IsTestFile(strPath1, 1, nFileSizeKB * 1024, dtmTimeToUse,
                            oSettings.CreateInfo && nFileSizeKB > 0 && !(oSettings.FirstToSecond && oSettings.FirstReadOnly),
                            false, null, null, null
                            ));

                        if (nFileSizeKB > 0)
                        {
                            List<long> oErrors = new List<long>();
                            oErrors.Add(nFileSizeKB <= 4096 ? 0 : (bProcessLastBlock ? nFileSizeKB / 4096 * 4096 : 0));

                            oFs.SetSimulatedReadError(strPath1, new List<long>(oErrors));

                            Assert.IsTrue(oFs.IsTestFile(strPath1, 1, nFileSizeKB * 1024, dtmTimeToUse,
                                oSettings.CreateInfo && !(oSettings.FirstToSecond && oSettings.FirstReadOnly),
                                false, null, new List<long>(oErrors), null
                                ));

                            oLog.Log.Clear();
                            oLog.LocalizedLog.Clear();

                            oAlgorithm.ProcessFilePair(strPath1, strPath1_1,
                                oFs, oSettings, oLogic, oSteps, oLog);

                            if (oSettings.FirstToSecond && oSettings.FirstReadOnly)
                            {
                                // if first read-only then nothin shall change
                                Assert.IsTrue(oFs.IsTestFile(strPath1, 1, nFileSizeKB * 1024, dtmTimeToUse,
                                    oSettings.CreateInfo && !(oSettings.FirstToSecond && oSettings.FirstReadOnly),
                                    false, null, new List<long>(oErrors), null
                                    ));
                            }
                            else
                            {


                                if (oSettings.TestFilesSkipRecentlyTested)
                                {
                                    Assert.IsTrue(oFs.IsTestFile(strPath1, 1, nFileSizeKB * 1024,
                                        dtmTimeToUse,
                                        oSettings.CreateInfo,
                                        false,
                                        null,
                                        new List<long>(oErrors),
                                        null));

                                }
                                else if (oSettings.CreateInfo)
                                {
                                    Assert.IsTrue(oFs.IsTestFile(strPath1, 1, nFileSizeKB * 1024,
                                        dtmTimeToUse,
                                        oSettings.CreateInfo,
                                        false,
                                        null,
                                        (oSettings.RepairFiles && oSettings.TestFiles) ? null : new List<long>(oErrors),
                                        null));
                                }
                                else if (oSettings.TestFiles && oSettings.RepairFiles)
                                {
                                    Assert.IsTrue(oFs.IsTestFile(strPath1, 1, nFileSizeKB * 1024,
                                        null,
                                        oSettings.CreateInfo,
                                        false,
                                        new List<long>(oErrors),
                                        null,
                                        null));
                                }
                            }
                        }
                    }
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// This test 
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test02_NewFileTwoFolders_FileIn1()
        {
            Random oRandom = new Random((int)DateTime.UtcNow.Ticks);
            foreach (int nFileSizeKB in new int[] {
                0, 1, 4, 16, 17,
                6 * 1024,   6 * 1024 + 1,
                200 * 1024, 200 * 1024 + 1})
            {
                for (int nConfiguration = 0; nConfiguration < 1024; ++nConfiguration)
                {
                    // skip testing all configurations for big files 
                    if (nConfiguration > 0 && nFileSizeKB > 32)
                        continue;

                    bool bProcessLastBlock = (oRandom.Next() & 1) != 0;

                    SettingsAndEnvironment oSettings = new SettingsAndEnvironment(
                        (nConfiguration & 1) != 0,
                        (nConfiguration & 2) != 0,
                        (nConfiguration & 4) != 0,
                        (nConfiguration & 8) != 0,
                        (nConfiguration & 16) != 0,
                        (nConfiguration & 32) != 0,
                        (nConfiguration & 64) != 0,
                        (nConfiguration & 128) != 0,
                        (nConfiguration & 256) != 0,
                        (nConfiguration & 512) != 0);


                    string strGuid = Guid.NewGuid().ToString();
                    string strDir1 = Path.Combine(Path.GetTempPath(), strGuid + Path.DirectorySeparatorChar + "Dir1");
                    string strDir2 = Path.Combine(Path.GetTempPath(), strGuid + Path.DirectorySeparatorChar + "Dir2");

                    string strPath1 = Path.Combine(strDir1, $@"TestSingleFile{nFileSizeKB}K-{oSettings}.dat");
                    string strPathSavedInfo1 = Path.Combine(strDir1,
                        $@"RestoreInfo{Path.DirectorySeparatorChar}TestSingleFile{nFileSizeKB}K-{oSettings}.dat.chk");
                    string strPath2 = Path.Combine(strDir2, $@"TestSingleFile{nFileSizeKB}K-{oSettings}.dat");
                    string strPathSavedInfo2 = Path.Combine(strDir2,
                        $@"RestoreInfo{Path.DirectorySeparatorChar}TestSingleFile{nFileSizeKB}K-{oSettings}.dat.chk");

                    DateTime dtmTimeToUse = DateTime.UtcNow;

                    InMemoryFileSystem oFs = new InMemoryFileSystem();
                    oFs.GetDirectoryInfo(Path.Combine(strDir1, "RestoreInfo")).Create();
                    oFs.GetDirectoryInfo(Path.Combine(strDir2, "RestoreInfo")).Create();

                    oFs.CreateTestFile(strPath1, 1, nFileSizeKB * 1024, dtmTimeToUse, false, false,
                        null, null, null, true);

                    FilePairStepChooser oAlgorithm = new FilePairStepChooser();
                    FilePairStepsDirectionLogic oLogic = new FilePairStepsDirectionLogic();
                    FilePairSteps oSteps = new FilePairSteps();
                    HashSetLog oLog = new HashSetLog();

                    oAlgorithm.ProcessFilePair(strPath1, strPath2,
                        oFs, oSettings, oLogic, oSteps, oLog);


                    if (oSettings.FirstToSecond && oSettings.FirstReadOnly)
                    {
                        Assert.IsTrue(oFs.IsTestFile(
                            strPath1, 1, nFileSizeKB * 1024, dtmTimeToUse, false, false,
                            null, null, null
                            ));

                        Assert.IsTrue(oFs.IsTestFile(
                            strPath2, 1, nFileSizeKB * 1024, dtmTimeToUse, oSettings.CreateInfo, false,
                            null, null, null
                            ));


                        if (nFileSizeKB > 0)
                        {

                            List<long> oErrors = new List<long>();
                            oErrors.Add(nFileSizeKB <= 4096 ? 0 : (bProcessLastBlock ? nFileSizeKB / 4096 * 4096 : 0));
                            oFs.SetSimulatedReadError(strPath2, new List<long>(oErrors));

                            Assert.IsTrue(oFs.IsTestFile(strPath2, 1, nFileSizeKB * 1024, dtmTimeToUse,
                                oSettings.CreateInfo,
                                false, null, new List<long>(oErrors), null
                                ));

                            oLog.Log.Clear();
                            oLog.LocalizedLog.Clear();

                            oAlgorithm.ProcessFilePair(strPath1, strPath2,
                                oFs, oSettings, oLogic, oSteps, oLog);

                            if (oSettings.TestFilesSkipRecentlyTested)
                            {
                                // nothing should have changed
                                Assert.IsTrue(oFs.IsTestFile(strPath2, 1, nFileSizeKB * 1024, dtmTimeToUse,
                                    oSettings.CreateInfo,
                                    false, null, new List<long>(oErrors), null
                                    ));
                            } else
                            {
                                // there it is expected that the first file should remained the same
                                Assert.IsTrue(oFs.IsTestFile(
                                    strPath1, 1, nFileSizeKB * 1024, dtmTimeToUse, false, false,
                                    null, null, null
                                    ));

                                if (oSettings.TestFiles && oSettings.RepairFiles)
                                {
                                    // and the second file shold have been repaired
                                    Assert.IsTrue(oFs.IsTestFile(strPath2, 1, nFileSizeKB * 1024, dtmTimeToUse,
                                        oSettings.CreateInfo,
                                        false, null,
                                        null, null
                                        ));
                                } else
                                {
                                    // or maybe not
                                    Assert.IsTrue(oFs.IsTestFile(strPath2, 1, nFileSizeKB * 1024, dtmTimeToUse,
                                        oSettings.CreateInfo,
                                        false, null, new List<long>(oErrors), null
                                        ));
                                }
                            }
                        }
                    }
                    else
                    {
                        Assert.IsTrue(oFs.AreTwoTestFiles(
                            strPath1, 1, nFileSizeKB * 1024,
                            dtmTimeToUse, oSettings.CreateInfo, null, null, null,

                            strPath2, oSettings.CreateInfo, null, null, null));

                        if (nFileSizeKB > 0)
                        {

                            List<long> oErrors = new List<long>();
                            oErrors.Add(nFileSizeKB <= 4096 ? 0 : (bProcessLastBlock ? nFileSizeKB / 4096 * 4096 : 0));
                            oFs.SetSimulatedReadError(strPath2, new List<long>(oErrors));


                            Assert.IsTrue(oFs.AreTwoTestFiles(
                                strPath1, 1, nFileSizeKB * 1024,
                                dtmTimeToUse, oSettings.CreateInfo, null, null, null,

                                strPath2, oSettings.CreateInfo, null, new List<long>(oErrors), null)); 
                            
                            oLog.Log.Clear();
                            oLog.LocalizedLog.Clear();

                            oAlgorithm.ProcessFilePair(strPath1, strPath2,
                                oFs, oSettings, oLogic, oSteps, oLog);

                            if (oSettings.TestFiles && oSettings.RepairFiles)
                            {
                                if (oSettings.TestFilesSkipRecentlyTested)
                                {
                                    Assert.IsTrue(oFs.AreTwoTestFiles(
                                        strPath1, 1, nFileSizeKB * 1024,
                                        dtmTimeToUse, oSettings.CreateInfo, null, null, null,

                                        strPath2, oSettings.CreateInfo, null, new List<long>(oErrors), null));
                                }
                                else
                                {

                                    // there it is expected that the first file should remained the same
                                    // and the second file shold have been repaired
                                    Assert.IsTrue(oFs.AreTwoTestFiles(
                                        strPath1, 1, nFileSizeKB * 1024,
                                        dtmTimeToUse, oSettings.CreateInfo, null, null, null,

                                        strPath2, oSettings.CreateInfo, null, null, null));
                                }
                            }
                            else
                            {
                                // or maybe not
                                Assert.IsTrue(oFs.AreTwoTestFiles(
                                    strPath1, 1, nFileSizeKB * 1024,
                                    dtmTimeToUse, oSettings.CreateInfo, null, null, null,

                                    strPath2, oSettings.CreateInfo, null, new List<long>(oErrors), null));
                            }
                        }
                    }
                }
            }
        }
    }
}