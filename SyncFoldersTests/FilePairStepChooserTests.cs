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
    public class FilePairStepChooserTests
    {
        enum FileConfigurationsType
        {
            eSecondZeroLength = 6,
            eFirstZeroLength = 7,
            eBothZeroLength = 8,
            eFirstZeroLengthSecondMissing = 9,
            eSecondZeroLengthFirstMissing = 10,
            eLast
        };

        /// <summary>
        /// This is the function that was called by the choice algorithm
        /// </summary>
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

        [SetUp]
        public void Setup()
        {
            // setup default localization for the API
            Properties.Resources = new SyncFoldersApiResources();
        }


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

            for (int nConfig=0; nConfig<1024; ++nConfig)
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
                } else
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

                HashSet<string> oMessages = new HashSet<string>();
                HashSet<string> oLocalized = new HashSet<string>();
                SettingsAndEnvironment oSettings;

                ChosenStepType eStep = RunConfiguration(oFS, nConfig, oMessages, oLocalized, out oSettings, c_strFile1, c_strFile1);

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
        /// This test simulates the case that first file is present, and the second is missing
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

                Assert.IsTrue( oMessages.Contains("Warning: file has zero length, indicating a failed copy operation in the past: \\\\sim\\dir2\\test.jpg"));
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
        /// This test simulates the case that first file is present, and the second is missing
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