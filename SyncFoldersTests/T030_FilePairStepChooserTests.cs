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
using System.Linq;

#pragma warning disable NUnit2005

namespace SyncFoldersTests
{
    /// <summary>
    /// This test fixture test
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class T030_FilePairStepChooserTests
    {
        //===================================================================================================
        /// <summary>
        /// This is the function that was called by the choice algorithm
        /// </summary>
        //===================================================================================================
        enum ChosenStepType
        {
            eNone = 0,
            eProcessFilePair_FirstToSecond_FirstReadonly_SecondExists,
            eProcessFilePair_FirstToSecond_FirstReadonly_FirstExists,
            eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy,
            eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy,
            eProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists,
            eProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists,
            eProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy,
            eProcessFilePair_Bidirectionally_FirstExists,
            eProcessFilePair_Bidirectionally_SecondExists,
            eProcessFilePair_Bidirectionally_BothExist_FirstNewer,
            eProcessFilePair_Bidirectionally_BothExist_SecondNewer,
            eProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual
        }

        //===================================================================================================
        /// <summary>
        /// Setp initializes resources of the API
        /// </summary>
        //===================================================================================================
        [SetUp]
        public void Setup()
        {
            // setup default localization for the API
            Properties.Resources = new SyncFoldersApiResources();
        }


        //===================================================================================================
        /// <summary>
        /// Executes step chooser for a particular configuration
        /// </summary>
        /// <param name="oFS">File system for the test, with readily available files for test</param>
        /// <param name="bFirstToSecond">
        /// Indicates, if the process shall go from first file/folder to second</param>
        /// <param name="bFirstReadOnly">
        /// Indicates, if first file/folder shall be considered read-only</param>
        /// <param name="bFirstToSecondSyncMode">
        /// Indicatess if first-to-second shall be dony in sync mode (in contrast to overwrite mode)</param>
        /// <param name="bFirstToSecondDeleteInSecond">
        /// Indicates, if first-to-second shall delete files of second folder that aren't present in first
        /// </param>
        /// <param name="bTestFiles">Indicates, if readability of files shall be tested</param>
        /// <param name="bTestFilesSkipRecentlyTested">
        /// Indicates if test of files shall randomly skip recently tested files</param>
        /// <param name="bRepairFiles">Indicates, if single block failures shall be repaired</param>
        /// <param name="bCreateInfo">Indicates, if SavedInfo shall be created, if it is missing</param>
        /// <param name="bIgnoreTimeDifferencesBetweenDataAndSaveInfo">
        /// Indicates if time difference between files and saved info shall be ignored</param>
        /// <param name="bPreferPhysicalCopies">
        /// Indicates, if physical copies shall be preferred over calculated restored info</param>
        /// <param name="oLogMessages">Contains log messages</param>
        /// <param name="oLocalizedLogMessages">Contains localized log messages</param>
        /// <param name="oSettings">Returns created settings object</param>
        /// <param name="strFile1">Path of file 1</param>
        /// <param name="strFile2">Path of file 2</param>
        /// <returns></returns>
        //===================================================================================================
        ChosenStepType RunConfiguration(
            InMemoryFileSystem oFS,
            bool bIgnoreTimeDifferencesBetweenDataAndSaveInfo,
            bool bCreateInfo,
            bool bFirstToSecond,
            bool bFirstReadOnly,
            bool bFirstToSecondSyncMode,
            bool bFirstToSecondDeleteInSecond,
            bool bTestFiles,
            bool bTestFilesSkipRecentlyTested,
            bool bRepairFiles,
            bool bPreferPhysicalCopies,
            HashSet<string> oLogMessages,
            HashSet<string> oLocalizedLogMessages,
            out SettingsAndEnvironment oSettings,
            string strFile1,
            string strFile2
            )
        {
            FilePairSteps oImpl = new FilePairSteps();
            ChosenStep oStep = new ChosenStep();

            oSettings = new SettingsAndEnvironment(
                bFirstToSecond,
                bFirstReadOnly,
                bFirstToSecondSyncMode,
                bFirstToSecondDeleteInSecond,
                bTestFiles,
                bTestFilesSkipRecentlyTested,
                bRepairFiles,
                bCreateInfo,
                bIgnoreTimeDifferencesBetweenDataAndSaveInfo,
                bPreferPhysicalCopies);

            HashSetLog oLog = new HashSetLog();
            oLog.Log = oLogMessages;
            oLog.LocalizedLog = oLocalizedLogMessages;

            oLogMessages.Clear();
            oLocalizedLogMessages.Clear();

            new FilePairStepChooser().ProcessFilePair(strFile1, strFile2,
                oFS, oSettings, oStep, oImpl, oLog);

            return oStep.Id;
        }

        /// <summary>
        /// Executes step chooser for a particular configuration
        /// </summary>
        /// <param name="oFS">File system for the test, with readily available files for test</param>
        /// <param name="nConfiguration">The index of configuration 0..1023</param>
        /// <param name="oLogMessages">Contains log messages</param>
        /// <param name="oLocalizedLogMessages">Contains localized log messages</param>
        /// <param name="oSettingsOut">Returns created settings object</param>
        /// <param name="strFile1">Path of file 1</param>
        /// <param name="strFile2">Path of file 2</param>
        /// <returns></returns>
        ChosenStepType RunConfiguration(InMemoryFileSystem oFS,
            int nConfiguration,
            HashSet<string> oLogMessages,
            HashSet<string> oLocalizedLogMessages,
            out SettingsAndEnvironment oSettingsOut,
            string strFile1,
            string strFile2)
        {
            return RunConfiguration(oFS,
                (nConfiguration & 1) != 0,
                (nConfiguration & 2) != 0,
                (nConfiguration & 4) != 0,
                (nConfiguration & 8) != 0,
                (nConfiguration & 16) != 0,
                (nConfiguration & 32) != 0,
                (nConfiguration & 64) != 0,
                (nConfiguration & 128) != 0,
                (nConfiguration & 256) != 0,
                (nConfiguration & 512) != 0,
                oLogMessages,
                oLocalizedLogMessages,
                out oSettingsOut,
                strFile1,
                strFile2);
        }


        const string c_strFile1 = "\\\\sim\\dir1\\test.jpg";
        const string c_strFile2 = "\\\\sim\\dir2\\test.jpg";
        const string c_strDir1 = "\\\\sim\\dir1";
        const string c_strDir2 = "\\\\sim\\dir2";

        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file is present, and the second is missing
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test01_FirstFilePresent()
        {

            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();
                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "Test");

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_FirstExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_FirstExists, eStep);

                Assert.AreEqual(0, oMessages.Count);
                Assert.AreEqual(0, oLocalized.Count);
            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file is present, and the second is missing
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test02_SecondFilePresent()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile2, "Test");

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_SecondExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_SecondExists, eStep);

                Assert.AreEqual(0, oMessages.Count);
                Assert.AreEqual(0, oLocalized.Count);
            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that both files are equal
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test03_EqualFiles()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "Test");
                oFS.WriteAllText(c_strFile2, "Test");

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual, eStep);

                Assert.AreEqual(0, oMessages.Count);
                Assert.AreEqual(0, oLocalized.Count);
            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that both files are present but have different timestamps because
        /// of FS differences, in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test04_EqualFilesDifferentFileSystemsTimeStampMismatch()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "Test");
                oFS.WriteAllText(c_strFile2, "Test");

                // set times, with hight probability they will differ a little
                // this is the case for different file systems (FAT aliases)
                DateTime dtmNow = DateTime.UtcNow;
                oFS.SetLastWriteTimeUtc(c_strFile1, new DateTime(dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second));

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual, eStep);

                Assert.AreEqual(0, oMessages.Count);
                Assert.AreEqual(0, oLocalized.Count);
            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file is newer
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test05_FirstNewer()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "Test");
                oFS.WriteAllText(c_strFile2, "Test");

                // make first file noticeably newer by making second older
                oFS.SetLastWriteTimeUtc(c_strFile2, DateTime.UtcNow.AddMinutes(-1));

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_FirstNewer, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_FirstNewer, eStep);

                Assert.AreEqual(0, oMessages.Count);
                Assert.AreEqual(0, oLocalized.Count);
            }
        }

        //===================================================================================================
        /// <summary>
        /// This test simulates the case that second file is newer
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test06_SecondNewer()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "Test");
                oFS.WriteAllText(c_strFile2, "Test");

                // make second file noticeably newer by making first older
                oFS.SetLastWriteTimeUtc(c_strFile1, DateTime.UtcNow.AddMinutes(-1));

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        if (oSettings.FirstToSecondSyncMode)
                            Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy, eStep);
                        else
                            Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy, eStep);
                    else
                        if (oSettings.FirstToSecondSyncMode)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_FirstNewer, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_SecondNewer, eStep);

                Assert.AreEqual(0, oMessages.Count);
                Assert.AreEqual(0, oLocalized.Count);
            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file is present, and the second has zero length
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test07_SecondZeroLength()
        {

            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();
                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "Test");
                oFS.WriteAllText(c_strFile2, "");

                // Set same time to both files
                DateTime dtmNow = DateTime.UtcNow;
                oFS.SetLastWriteTimeUtc(c_strFile1, dtmNow);
                oFS.SetLastWriteTimeUtc(c_strFile2, dtmNow);

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_FirstExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_FirstExists, eStep);

                Assert.IsTrue(oMessages.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir2\\test.jpg"));
                Assert.IsTrue(oLocalized.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir2\\test.jpg"));
                Assert.AreEqual(1, oMessages.Count);
                Assert.AreEqual(1, oLocalized.Count);


                // Make second file newer by making first older
                oFS.SetLastWriteTimeUtc(c_strFile1, dtmNow.AddMinutes(-1));

                oMessages = new HashSet<string>();
                oLocalized = new HashSet<string>();

                eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_FirstExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_FirstExists, eStep);

                Assert.IsTrue(oMessages.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir2\\test.jpg"));
                Assert.IsTrue(oLocalized.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir2\\test.jpg"));
                Assert.AreEqual(1, oMessages.Count);
                Assert.AreEqual(1, oLocalized.Count);

            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file has zero length and the second is ok
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test08_FirstZeroLength()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile2, "Test");
                oFS.WriteAllText(c_strFile1, "");

                // Set same time to both files
                DateTime dtmNow = DateTime.UtcNow;
                oFS.SetLastWriteTimeUtc(c_strFile1, dtmNow);
                oFS.SetLastWriteTimeUtc(c_strFile2, dtmNow);

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_SecondExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_SecondExists, eStep);

                Assert.IsTrue(oMessages.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir1\\test.jpg"));
                Assert.IsTrue(oLocalized.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir1\\test.jpg"));
                Assert.AreEqual(1, oMessages.Count);
                Assert.AreEqual(1, oLocalized.Count);

                // Make first file newer by making second older
                oFS.SetLastWriteTimeUtc(c_strFile2, dtmNow.AddMinutes(-1));

                oMessages = new HashSet<string>();
                oLocalized = new HashSet<string>();

                eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_SecondExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_SecondExists, eStep);

                Assert.IsTrue(oMessages.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir1\\test.jpg"));
                Assert.IsTrue(oLocalized.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir1\\test.jpg"));
                Assert.AreEqual(1, oMessages.Count);
                Assert.AreEqual(1, oLocalized.Count);

            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file is present but has zero length, and the no second
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test09_FirstZeroLengthSecondMissing()
        {

            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();
                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "");

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_FirstExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_FirstExists, eStep);

                Assert.IsTrue(oMessages.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir1\\test.jpg"));
                Assert.IsTrue(oLocalized.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir1\\test.jpg"));
                Assert.AreEqual(1, oMessages.Count);
                Assert.AreEqual(1, oLocalized.Count);
            }
        }


        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file is missing and the second has zero length
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test10_FirstMissingSecondZeroLength()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile2, "");

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);

                if (oSettings.FirstToSecond)
                {
                    if (oSettings.FirstReadOnly)
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_SecondExists, eStep);
                    else
                        Assert.AreEqual(ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists, eStep);
                }
                else
                    Assert.AreEqual(ChosenStepType.eProcessFilePair_Bidirectionally_SecondExists, eStep);

                Assert.IsTrue(oMessages.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir2\\test.jpg"));
                Assert.IsTrue(oLocalized.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir2\\test.jpg"));
                Assert.AreEqual(1, oMessages.Count);
                Assert.AreEqual(1, oLocalized.Count);
            }
        }

        //===================================================================================================
        /// <summary>
        /// This test simulates the case that first file is missing and the second has zero length
        /// in all configurations
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test11_BothZeroLength()
        {
            for (int nConfig = 0; nConfig < 1024; ++nConfig)
            {
                InMemoryFileSystem oFS = new InMemoryFileSystem();

                oFS.EnsureDirectoryExists(c_strDir1);
                oFS.EnsureDirectoryExists(c_strDir2);
                oFS.WriteAllText(c_strFile1, "");
                oFS.WriteAllText(c_strFile2, "");

                // Set same time to both files
                DateTime dtmNow = DateTime.UtcNow;
                oFS.SetLastWriteTimeUtc(c_strFile1, dtmNow);
                oFS.SetLastWriteTimeUtc(c_strFile2, dtmNow);

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile2);


                Assert.AreEqual(ChosenStepType.eNone, eStep);

                Assert.IsTrue(oMessages.Contains("Warning: both files have zero length, indicating a failed copy operation in the past: \\\\sim\\dir1\\test.jpg, \\\\sim\\dir2\\test.jpg"));
                Assert.IsTrue(oLocalized.Contains("Warning: both files have zero length, indicating a failed copy operation in the past: \"\\\\sim\\dir1\\test.jpg\" | \"\\\\sim\\dir2\\test.jpg\""));
                Assert.AreEqual(1, oMessages.Count);
                Assert.AreEqual(1, oLocalized.Count);
            }
        }



        //***************************************************************************************************
        /// <summary>
        /// This class provides means for verification of called steps
        /// </summary>
        //***************************************************************************************************
        class ChosenStep : IFilePairStepsDirectionLogic
        {
            //===============================================================================================
            /// <summary>
            /// The last caled step
            /// </summary>
            public ChosenStepType Id
            {
                get; set;
            }

            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_FirstToSecond_FirstReadonly_SecondExists(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_SecondExists;
            }

            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================

            public void ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_FirstExists;
            }

            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================

            public void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy;
            }

            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy;
            }


            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists;
            }

            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists;
            }


            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy;
            }

            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_Bidirectionally_FirstExists(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_Bidirectionally_FirstExists;
            }


            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_Bidirectionally_SecondExists(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_Bidirectionally_SecondExists;
            }

            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_Bidirectionally_BothExist_FirstNewer(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                string strReasonEn,
                string strReasonTranslated,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_FirstNewer;
            }


            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_Bidirectionally_BothExist_SecondNewer(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_SecondNewer;
            }


            //===============================================================================================
            /// <summary>
            /// This method remembers that it has been called, for verification of correct calls
            /// </summary>
            /// <param name="strFilePath1">ignored</param>
            /// <param name="strFilePath2">ignored</param>
            /// <param name="fi1">ignored</param>
            /// <param name="fi2">ignored</param>
            /// <param name="iFileSystem">ignored</param>
            /// <param name="iLogWriter">ignored</param>
            /// <param name="iSettings">ignored</param>
            /// <param name="iStepsImpl">ignored</param>
            //===============================================================================================
            public void ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(
                string strFilePath1,
                string strFilePath2,
                IFileInfo fi1,
                IFileInfo fi2,
                IFileOperations iFileSystem,
                IFilePairStepsSettings iSettings,
                IFilePairSteps iStepsImpl,
                ILogWriter iLogWriter
                )
            {
                Id = ChosenStepType.eProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual;
            }

        }

    }


}