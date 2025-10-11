using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using SyncFoldersApi.Localization;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// This class implements the direction logic: given a situation, it decides which basic
    /// test, copy and restore steps need to be done for transferring files in this situation
    /// </summary>
    //*******************************************************************************************************
    public class FilePairStepsDirectionLogic: IFilePairStepsDirectionLogic
    {
        /// <summary>
        /// This is used for changing order of tests in different threads.
        /// One tests first file first, the next tests the second file first,
        /// so we reduce the competition on drives and use both drives more efficiently
        /// </summary>
        private volatile bool m_bRandomOrder;

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only and only second file exists
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            if (iSettings.FirstToSecondDeleteInSecond)
            {
                IFileInfo fiSavedInfo2 =
                    iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

                if (fiSavedInfo2.Exists)
                    iFileSystem.Delete(fiSavedInfo2);

                iFileSystem.Delete(fi2);

                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.DeletedFileNotPresentIn,
                    fi2.FullName,
                    fi1.Directory.FullName);

                iLogWriter.WriteLog(true, 0, "Deleted file ", fi2.FullName,
                    " that is not present in ", fi1.Directory.FullName, " anymore");
            }
            else
            {
                if (iSettings.TestFiles)
                {
                    IFileInfo fiSavedInfo2 =
                        iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    bool bForceCreateInfo = false;
                    if (iSettings.RepairFiles)
                    {
                        iStepsImpl.TestAndRepairSingleFile(
                            fi2.FullName, fiSavedInfo2.FullName,
                            ref bForceCreateInfo, false,
                            iFileSystem, iSettings, iLogWriter);
                    }
                    else
                    {
                        iStepsImpl.TestSingleFile(fi2.FullName, fiSavedInfo2.FullName,
                            ref bForceCreateInfo, true,
                            !iSettings.TestFilesSkipRecentlyTested, true,
                            iFileSystem, iSettings, iLogWriter);
                    }

                    if (iSettings.CreateInfo && (!fiSavedInfo2.Exists || bForceCreateInfo))
                    {
                        iStepsImpl.CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName,
                            iFileSystem, iSettings, iLogWriter);
                    }
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only and only first file exists
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreatInfo = false;
            bool bForceCreatInfo2 = false;
            if (iSettings.CancelClicked)
                return;
            try
            {
                //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();

                if (iSettings.CancelClicked)
                    return;

                iStepsImpl.CopyRepairSingleFile(fi2.FullName, fi1.FullName, fiSavedInfo1.FullName,
                    ref bForceCreatInfo, ref bForceCreatInfo2, "(file was new)",
                    Properties.Resources.FileWasNew, false, false,
                    iFileSystem, iSettings, iLogWriter);
            }
            finally
            {
                //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
            }

            if (iSettings.CreateInfo || fiSavedInfo1.Exists || fiSavedInfo2.Exists)
            {
                if (fiSavedInfo1.Exists && !bForceCreatInfo && !bForceCreatInfo2)
                {
                    try
                    {
                        //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();

                        //CopyFileSafely(fiSavedInfo1, fiSavedInfo2.FullName);
                        iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    }
                    catch
                    {
                        iStepsImpl.CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName,
                            iFileSystem, iSettings, iLogWriter);
                    }
                    finally
                    {
                        //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                    }
                }
                else
                    iStepsImpl.CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName,
                            iFileSystem, iSettings, iLogWriter);
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only, both files exist and first needs to be copied over second file
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo = false;

            // if the first file is ok
            if (iStepsImpl.TestSingleFile(strFilePath1, fiSavedInfo1.FullName,
                ref bForceCreateInfo, iSettings.TestFiles, true, false,
                iFileSystem, iSettings, iLogWriter))
            {
                if (iSettings.CancelClicked)
                    return;
                // then simply copy it
                try
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();

                    if (iSettings.CancelClicked)
                        return;

                    iStepsImpl.CopyFileSafely(fi1, strFilePath2, "(file newer or bigger)",
                        Properties.Resources.FileWasNewer,
                        iFileSystem, iLogWriter);
                    //iFileSystem.CopyTo(fi1,strFilePath2, true);
                }
                finally
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                }

                if (iSettings.CreateInfo || fiSavedInfo2.Exists || fiSavedInfo1.Exists)
                {
                    if (bForceCreateInfo)
                    {
                        iStepsImpl.CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName,
                            iFileSystem, iSettings, iLogWriter);
                    }
                    else
                    {
                        try
                        {
                            //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();
                            iFileSystem.CopyTo(fiSavedInfo1, Utils.CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                        }
                        finally
                        {
                            //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                        }

                    }
                }
            }
            else
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FirstFileHasBadBlocks,
                    strFilePath1, strFilePath2);

                iLogWriter.WriteLog(true, 0, "Warning: First file ", strFilePath1,
                    " has bad blocks, overwriting file ", strFilePath2,
                    " has been skipped, so the it remains as backup");
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only, both files exist and there is no obvious neeed to copy anything
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            if (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) &&
                fi1.Length == fi2.Length)
            {
                // we are in first readonly path
                // both files are present and have same modification date and lentgh

                // if the second restoreinfo is missing or has wrong date, 
                // but the other is OK then copy the one to the other
                if (fiSavedInfo1.Exists && fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc &&
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                {
                    try
                    {
                        //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();
                        iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    }
                    finally
                    {
                        //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                    }

                    fiSavedInfo2 = iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                bool bForceCreateInfoBecauseDamaged = false;
                if (iSettings.TestFiles)
                {
                    if (iSettings.RepairFiles)
                    {
                        iStepsImpl.TestAndRepairSecondFile(fi1.FullName, fi2.FullName,
                            fiSavedInfo1.FullName, fiSavedInfo2.FullName,
                            ref bForceCreateInfoBecauseDamaged,
                            iFileSystem, iSettings, iLogWriter);
                    }
                    else
                    {
                        if (!fi1.FullName.Equals(fi2.FullName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // also test first file
                            bool bDummy = false;
                            iStepsImpl.TestSingleFile(fi1.FullName, fiSavedInfo1.FullName,
                                ref bDummy, true,
                                !iSettings.TestFilesSkipRecentlyTested, true,
                                iFileSystem, iSettings, iLogWriter);
                        }

                        iStepsImpl.TestSingleFile(fi2.FullName, fiSavedInfo2.FullName,
                            ref bForceCreateInfoBecauseDamaged, true,
                            !iSettings.TestFilesSkipRecentlyTested, true,
                            iFileSystem, iSettings, iLogWriter);
                    }
                }


                if (iSettings.CreateInfo &&
                    (!fiSavedInfo2.Exists ||
                        fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc ||
                        bForceCreateInfoBecauseDamaged))
                {
                    if (!fi1.FullName.Equals(fi2.FullName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // create info only if first and second files aren't the same
                        iStepsImpl.CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName,
                        iFileSystem, iSettings, iLogWriter);
                    }
                }

                fiSavedInfo2 = iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                fiSavedInfo1 = iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

                // if one of the files is missing or has wrong date, 
                // but the other is OK then copy the one to the other
                if (fiSavedInfo1.Exists &&
                    fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc &&
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                {
                    try
                    {
                        //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();
                        iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    }
                    finally
                    {
                        //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                    }
                }
            }
            else
            {
                bool bForceCreateInfoBecauseDamaged = false;
                bool bOK = true;

                if (iSettings.TestFiles)
                {
                    // test first file
                    iStepsImpl.TestSingleFile(strFilePath1, Utils.CreatePathOfChkFile(
                        fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"),
                        ref bForceCreateInfoBecauseDamaged, true, !iSettings.TestFilesSkipRecentlyTested, true,
                        iFileSystem, iSettings, iLogWriter);

                    // test or repair second file, which is different from first
                    if (iSettings.RepairFiles)
                    {
                        iStepsImpl.TestAndRepairSingleFile(strFilePath2, Utils.CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                            ref bForceCreateInfoBecauseDamaged, false,
                            iFileSystem, iSettings, iLogWriter);
                    }
                    else
                    {
                        bOK = iStepsImpl.TestSingleFile(strFilePath2, Utils.CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                            ref bForceCreateInfoBecauseDamaged, true, 
                            !iSettings.TestFilesSkipRecentlyTested, true,
                            iFileSystem, iSettings, iLogWriter);
                    }

                    if (bOK && iSettings.CreateInfo &&
                        (!fiSavedInfo2.Exists || bForceCreateInfoBecauseDamaged))
                    {
                        iStepsImpl.CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName,
                            iFileSystem, iSettings, iLogWriter);
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to and only file in second folder exists
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            if (iSettings.FirstToSecondDeleteInSecond)
            {
                IFileInfo fiSavedInfo2 =
                    iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

                if (fiSavedInfo2.Exists)
                    iFileSystem.Delete(fiSavedInfo2);

                iFileSystem.Delete(fi2);

                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.DeletedFileNotPresentIn,
                    fi2.FullName, fi1.Directory.FullName);

                iLogWriter.WriteLog(true, 0, "Deleted file ", fi2.FullName,
                    " that is not present in ", fi1.Directory.FullName, " anymore");
            }
            else
            {


                if (iSettings.TestFiles)
                {
                    IFileInfo fiSavedInfo2 =
                        iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

                    bool bForceCreateInfo = false;
                    bool bOK = true;

                    if (iSettings.RepairFiles)
                    {
                        iStepsImpl.TestAndRepairSingleFile(fi2.FullName, fiSavedInfo2.FullName,
                            ref bForceCreateInfo, false,
                            iFileSystem, iSettings, iLogWriter);
                    }
                    else
                    {
                        bOK = iStepsImpl.TestSingleFile(fi2.FullName, fiSavedInfo2.FullName,
                            ref bForceCreateInfo, true, !iSettings.TestFilesSkipRecentlyTested, true,
                            iFileSystem, iSettings, iLogWriter);
                    }

                    if (bOK && iSettings.CreateInfo &&
                        (!fiSavedInfo2.Exists || bForceCreateInfo))
                    {
                        iStepsImpl.CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName, 
                            iFileSystem, iSettings, iLogWriter  );
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to and only file in first folder exists
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            ProcessFilePair_Bidirectionally_FirstExists(strFilePath1, strFilePath2, fi1, fi2,
                iFileSystem, iSettings, iStepsImpl, iLogWriter);
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to, both files exist and there is no obvious reason for
        /// copying anything
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // both files are present and have same modification date
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // first to second, but first can be written to
            if (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) &&
                fi1.Length == fi2.Length)
            {
                ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(
                    strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
            }
            else
            {
                bool bForceCreateInfo = false;
                bool bOK = true;
                if (iSettings.TestFiles)
                {
                    bOK = iStepsImpl.TestSingleFile(strFilePath2, Utils.CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                        ref bForceCreateInfo, true, !iSettings.TestFilesSkipRecentlyTested, true,
                            iFileSystem, iSettings, iLogWriter);

                    if (!bOK && iSettings.RepairFiles)
                    {
                        // first try to repair second file internally
                        if (iStepsImpl.TestSingleFileHealthyOrCanRepair(strFilePath2,
                            Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo",
                            fi2.Name, ".chk"), ref bForceCreateInfo,
                            iFileSystem, iSettings, iLogWriter))
                        {
                            bOK = iStepsImpl.TestAndRepairSingleFile(strFilePath2, Utils.CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                                ref bForceCreateInfo, false,
                                iFileSystem, iSettings, iLogWriter);
                        }

                        if (bOK && bForceCreateInfo)
                        {
                            iStepsImpl.CreateSavedInfo(strFilePath2,
                                Utils.CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                                iFileSystem, iSettings, iLogWriter);
                        }
                        bForceCreateInfo = false;

                        // if it didn't work, then try to repair using first file
                        if (!bOK)
                        {
                            bOK = iStepsImpl.TestSingleFile(strFilePath1,
                                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name,
                                    ".chk"), ref bForceCreateInfo, true, true, true,
                                iFileSystem, iSettings, iLogWriter);

                            if (!bOK)
                            {
                                bOK = iStepsImpl.TestAndRepairSingleFile(strFilePath1,
                                    Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo",
                                        fi1.Name, ".chk"), ref bForceCreateInfo, true,
                                    iFileSystem, iSettings, iLogWriter);
                            }

                            if (bOK && bForceCreateInfo)
                            {
                                bOK = iStepsImpl.CreateSavedInfo(strFilePath1,
                                    Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo",
                                        fi1.Name, ".chk"),
                                    iFileSystem, iSettings, iLogWriter);
                                bForceCreateInfo = false;
                            }

                            if (bOK)
                            {
                                if (fi1.LastWriteTimeUtc.Year > 1975)
                                {
                                    iStepsImpl.CopyFileSafely(fi1, strFilePath2,
                                        "(file was healthy, or repaired)",
                                        Properties.Resources.FileHealthyOrRepaired,
                                        iFileSystem, iLogWriter);
                                }
                                else
                                {
                                    iLogWriter.WriteLogFormattedLocalized(0,
                                        Properties.Resources.CouldntUseOutdatedFileForRestoringOther,
                                        strFilePath1, strFilePath2);
                                    iLogWriter.WriteLog(true, 0, "Warning: couldn't use outdated file ",
                                        strFilePath1, " with year 1975 or earlier for restoring ",
                                        strFilePath2, ", signaling this was a last chance restore");
                                }
                            }
                        }
                    }
                    else
                    {
                        // second file was OK, or no repair option, still need to process first file
                        bOK = iStepsImpl.TestSingleFile(strFilePath1, Utils.CreatePathOfChkFile(
                            fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"),
                            ref bForceCreateInfo, true, true, true,
                            iFileSystem, iSettings, iLogWriter);

                        if (!bOK && iSettings.RepairFiles)
                        {
                            if (iStepsImpl.TestAndRepairSingleFile(strFilePath1,
                                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name,
                                    ".chk"), ref bForceCreateInfo, true,
                                iFileSystem, iSettings, iLogWriter))
                            {
                                bOK = true;
                            }

                            if (bOK && bForceCreateInfo)
                            {
                                iStepsImpl.CreateSavedInfo(strFilePath1, Utils.CreatePathOfChkFile(
                                    fi1.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                                iFileSystem, iSettings, iLogWriter);
                            }
                            bForceCreateInfo = false;

                            // if it didn't work, then try to repair using second file
                            if (!bOK)
                            {
                                bOK = iStepsImpl.TestSingleFile(strFilePath2,
                                    Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo",
                                        fi2.Name, ".chk"), ref bForceCreateInfo, true, true, true,
                                    iFileSystem, iSettings, iLogWriter);
                                if (!bOK)
                                {
                                    bOK = iStepsImpl.TestAndRepairSingleFile(strFilePath2,
                                    Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo",
                                        fi2.Name, ".chk"), ref bForceCreateInfo, true,
                                    iFileSystem, iSettings, iLogWriter);
                                }

                                if (bOK && bForceCreateInfo)
                                {
                                    bOK = iStepsImpl.CreateSavedInfo(strFilePath2,
                                        Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo",
                                            fi2.Name, ".chk"),
                                        iFileSystem, iSettings, iLogWriter);
                                    bForceCreateInfo = false;
                                }

                                if (bOK)
                                {
                                    if (fi2.LastWriteTimeUtc.Year > 1975)
                                    {
                                        iStepsImpl.CopyFileSafely(fi2, strFilePath1, "(file was healthy, or repaired)",
                                            Properties.Resources.FileHealthyOrRepaired,
                                            iFileSystem, iLogWriter);
                                    }
                                    else
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(0,
                                            Properties.Resources.CouldntUseOutdatedFileForRestoringOther,
                                            strFilePath2, strFilePath1);

                                        iLogWriter.WriteLog(true, 0, 
                                            "Warning: couldn't use outdated file ", strFilePath2,
                                            " with year 1975 or earlier for restoring ",
                                            strFilePath1, ", signaling this was a last chance restore");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case only first of
        /// the two files exists
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo = false;
            bool bForceCreateInfo2 = false;
            bool bInTheEndOK = true;

            try
            {
                //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();

                if (iSettings.CancelClicked)
                    return;

                if (iSettings.CreateInfo &&
                    (!fiSavedInfo1.Exists ||
                        fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc ||
                        bForceCreateInfo))
                {
                    if (!iStepsImpl.Create2SavedInfosAndCopy(
                        fi1.FullName, fiSavedInfo1.FullName,
                        fi2.FullName, fiSavedInfo2.FullName, "(file was new)",
                        Properties.Resources.FileWasNew,
                        iFileSystem, iSettings, iLogWriter))
                    {
                        iStepsImpl.CopyRepairSingleFile(strFilePath2, strFilePath1,
                            fiSavedInfo1.FullName, ref bForceCreateInfo,
                            ref bForceCreateInfo2, "(file was new)",
                            Properties.Resources.FileWasNew, false, false,
                            iFileSystem, iSettings, iLogWriter);

                        iStepsImpl.CreateSavedInfo(strFilePath2,
                            Utils.CreatePathOfChkFile(fi2.DirectoryName,
                            "RestoreInfo", fi2.Name, ".chk"),
                            iFileSystem, iSettings, iLogWriter);

                        return;
                    }

                    fiSavedInfo1 = iFileSystem.GetFileInfo(
                        Utils.CreatePathOfChkFile(fi1.DirectoryName,
                        "RestoreInfo", fi1.Name, ".chk"));

                    fiSavedInfo2 = iFileSystem.GetFileInfo(
                        Utils.CreatePathOfChkFile(fi2.DirectoryName,
                        "RestoreInfo", fi2.Name, ".chk"));

                    bForceCreateInfo = false;
                }
                else
                {
                    try
                    {
                        if (iSettings.TestFiles)
                        {
                            iStepsImpl.CopyRepairSingleFile(strFilePath2, fi1.FullName,
                                fiSavedInfo1.FullName, ref bForceCreateInfo, ref bForceCreateInfo2,
                                "(file was new)", Properties.Resources.FileWasNew, 
                                true, iSettings.RepairFiles,
                                iFileSystem, iSettings, iLogWriter);
                        }
                        else
                        {
                            iStepsImpl.CopyFileSafely(fi1, strFilePath2, "(file was new)",
                                Properties.Resources.FileWasNew,
                                iFileSystem, iLogWriter);
                        }
                    }
                    catch (Exception)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.EncounteredErrorWhileCopyingTryingToRepair,
                            fi1.FullName);

                        iLogWriter.WriteLog(true, 0, "Warning: Encountered error while copying ",
                            fi1.FullName, ", trying to automatically repair");

                        if (iSettings.TestFiles && iSettings.RepairFiles)
                        {
                            iStepsImpl.TestAndRepairSingleFile(fi1.FullName,
                                fiSavedInfo1.FullName, ref bForceCreateInfo, false,
                                iFileSystem, iSettings, iLogWriter);
                        }

                        if (bInTheEndOK)
                        {
                            bInTheEndOK = iStepsImpl.CopyRepairSingleFile(strFilePath2,
                                fi1.FullName, fiSavedInfo1.FullName,
                                ref bForceCreateInfo, ref bForceCreateInfo2,
                                "(file was new)", Properties.Resources.FileWasNew, false,
                                iSettings.TestFiles && iSettings.RepairFiles,
                                iFileSystem, iSettings, iLogWriter);
                        }
                    }
                }


                if (bInTheEndOK)
                {
                    if (iSettings.CreateInfo || fiSavedInfo1.Exists)
                    {
                        if (!fiSavedInfo1.Exists || bForceCreateInfo ||
                            fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc)
                        {
                            iStepsImpl.CreateSavedInfo(strFilePath1,
                                Utils.CreatePathOfChkFile(fi1.DirectoryName,
                                "RestoreInfo", fi1.Name, ".chk"),
                                 iFileSystem, iSettings, iLogWriter);
                        }

                        if (bForceCreateInfo2)
                        {
                            iStepsImpl.CreateSavedInfo(strFilePath2,
                                Utils.CreatePathOfChkFile(fi2.DirectoryName,
                                "RestoreInfo", fi2.Name, ".chk"),
                                iFileSystem, iSettings, iLogWriter);

                        }
                    }
                }
            }
            finally
            {
                //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case only second of
        /// the two files exists
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            // symmetric situation
            ProcessFilePair_Bidirectionally_FirstExists(
                strFilePath2, strFilePath1, fi2, fi1,
                iFileSystem, iSettings, iStepsImpl, iLogWriter);
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist and first file has a more recent date
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="strReasonEn">The reason, in english language (hardcoded)</param>
        /// <param name="strReasonTranslated">The reason, localized in user language</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            IFileInfo fiSavedInfo1 =
                iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            IFileInfo fiSavedInfo2 =
                iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo1 = false;
            bool bForceCreateInfo2 = false;
            bool bCopied2To1 = false;
            bool bCopy2To1 = false;
            bool bCopied1To2 = true;

            try
            {
                //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();

                if (iSettings.CancelClicked)
                    return;

                if (iSettings.CreateInfo &&
                    (!fiSavedInfo1.Exists ||
                        fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc ||
                        bForceCreateInfo1))
                {
                    bCopied1To2 = iStepsImpl.Create2SavedInfosAndCopy(
                        fi1.FullName, fiSavedInfo1.FullName, 
                        fi2.FullName, fiSavedInfo2.FullName,
                        strReasonEn, strReasonTranslated,
                        iFileSystem, iSettings, iLogWriter);

                    fiSavedInfo1 = iFileSystem.GetFileInfo(
                        Utils.CreatePathOfChkFile(fi1.DirectoryName,
                        "RestoreInfo", fi1.Name, ".chk"));
                    fiSavedInfo2 = iFileSystem.GetFileInfo(
                        Utils.CreatePathOfChkFile(fi1.DirectoryName,
                        "RestoreInfo", fi1.Name, ".chk"));

                    if (bCopied1To2)
                    {
                        bForceCreateInfo1 = false;
                        bForceCreateInfo2 = false;
                    }
                }
                else
                {
                    try
                    {
                        if (iSettings.TestFiles)
                        {
                            iStepsImpl.CopyRepairSingleFile(
                                strFilePath2, fi1.FullName, fiSavedInfo1.FullName,
                                ref bForceCreateInfo1, ref bForceCreateInfo2,
                                "(file was new)", Properties.Resources.FileWasNew,
                                true, iSettings.RepairFiles,
                                iFileSystem, iSettings, iLogWriter);
                        }
                        else
                        {
                            iStepsImpl.CopyFileSafely(fi1, strFilePath2,
                                strReasonEn, strReasonTranslated,
                                iFileSystem, iLogWriter);
                        }
                    }
                    catch (Exception)
                    {
                        bCopied1To2 = false;
                    }
                }

                if (!bCopied1To2)
                {
                    if (!iSettings.TestFiles || !iSettings.RepairFiles)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.RunningWithoutRepairOptionUndecided,
                            fi1.FullName, fi2.FullName);

                        iLogWriter.WriteLog(true, 0, "Running without repair option, "
                        + "so couldn't decide, if the file ",
                        fi1.FullName, " can be restored using ", fi2.FullName);

                        // first failed,  still need to test the second
                        if (iSettings.TestFiles)
                        {
                            iStepsImpl.TestSingleFileHealthyOrCanRepair(strFilePath2,
                                fiSavedInfo2.FullName, ref bForceCreateInfo2,
                                iFileSystem, iSettings, iLogWriter);
                        }
                        return;
                    }

                    // first try to copy the first/needed file, if it can be restored
                    if (iStepsImpl.TestSingleFileHealthyOrCanRepair(strFilePath1,
                            fiSavedInfo1.FullName, ref bForceCreateInfo1,
                            iFileSystem, iSettings, iLogWriter) &&
                        iStepsImpl.TestAndRepairSingleFile(
                            strFilePath1, fiSavedInfo1.FullName, 
                            ref bForceCreateInfo1, true,
                            iFileSystem, iSettings, iLogWriter))
                    {
                        if (bForceCreateInfo1)
                        {
                            bCopied1To2 = iStepsImpl.Create2SavedInfosAndCopy(
                                fi1.FullName, fiSavedInfo1.FullName, 
                                fi2.FullName, fiSavedInfo2.FullName,
                                strReasonEn, strReasonTranslated,
                                iFileSystem, iSettings, iLogWriter);

                            fiSavedInfo1 = iFileSystem.GetFileInfo(
                                Utils.CreatePathOfChkFile(fi1.DirectoryName,
                                "RestoreInfo", fi1.Name, ".chk"));
                            fiSavedInfo2 = iFileSystem.GetFileInfo(
                                Utils.CreatePathOfChkFile(fi1.DirectoryName,
                                "RestoreInfo", fi1.Name, ".chk"));

                            if (bCopied1To2)
                            {
                                bForceCreateInfo1 = false;
                                bForceCreateInfo2 = false;
                            }
                        }
                        else
                        {
                            iStepsImpl.CopyFileSafely(fi1, strFilePath2, 
                                strReasonEn, strReasonTranslated,
                                iFileSystem, iLogWriter);
                            bCopied1To2 = true;
                        }
                    }

                    if (!bCopied1To2)
                    {
                        // well, then try the second, older file. Let's see if it is OK,
                        // or can be restored in place
                        if (iStepsImpl.TestAndRepairSingleFile(
                            strFilePath2, fiSavedInfo2.FullName,
                            ref bForceCreateInfo2, true,
                            iFileSystem, iSettings, iLogWriter)
                             && fi2.LastWriteTimeUtc.Year > 1975)
                        {
                            iLogWriter.WriteLogFormattedLocalized(0, 
                                Properties.Resources.EncounteredErrorOlderOk,
                                fi1.FullName, strFilePath2);

                            iLogWriter.WriteLog(true, 0, 
                                "Warning: Encountered I/O error while copying ",
                                fi1.FullName, ". The older file ", strFilePath2, 
                                " seems to be OK");

                            bCopied1To2 = false;
                            bCopy2To1 = true;
                        }
                        else
                        {
                            iLogWriter.WriteLogFormattedLocalized(0, 
                                Properties.Resources.EncounteredErrorOtherBadToo,
                                fi1.FullName, strFilePath2);

                            iLogWriter.WriteLog(true, 0, 
                                "Warning: Encountered I/O error while copying ", fi1.FullName,
                                ". Other file has errors as well: ", strFilePath2,
                                ", or is a product of last chance restore, trying to automatically repair ",
                                strFilePath1);

                            iStepsImpl.TestAndRepairSingleFile(
                                fi1.FullName, fiSavedInfo1.FullName,
                                ref bForceCreateInfo1, false,
                                iFileSystem, iSettings, iLogWriter);

                            bForceCreateInfo2 = false;

                            iStepsImpl.CopyRepairSingleFile(
                                strFilePath2, fi1.FullName, fiSavedInfo1.FullName,
                                ref bForceCreateInfo1, ref bForceCreateInfo2, strReasonEn,
                                strReasonTranslated, false, true,
                                iFileSystem, iSettings, iLogWriter);

                            bCopied1To2 = true;
                        }
                    }
                }
                ;

                if (bCopied1To2)
                {
                    if ((iSettings.CreateInfo &&
                        (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc)) ||
                        (fiSavedInfo1.Exists && bForceCreateInfo1))
                    {
                        iStepsImpl.CreateSavedInfo(strFilePath1,
                            Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"),
                            iFileSystem, iSettings, iLogWriter);

                        fiSavedInfo1 = iFileSystem.GetFileInfo(
                            Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    }

                    if (fiSavedInfo1.Exists)
                    {
                        if (bForceCreateInfo2)
                        {
                            if (iSettings.CreateInfo || fiSavedInfo2.Exists)
                                iStepsImpl.CreateSavedInfo(strFilePath2, 
                                    Utils.CreatePathOfChkFile(
                                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                                    iFileSystem, iSettings, iLogWriter);
                        }
                        else
                            iFileSystem.CopyTo(fiSavedInfo1, Utils.CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                    }

                    return;
                }


                if (!bCopy2To1)
                    return;

                if (!iSettings.TestFiles || !iSettings.RepairFiles)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.RunningWithoutRepairOptionUndecided,
                        fi1.FullName, fi2.FullName);
                    iLogWriter.WriteLog(true, 0, 
                        "Running without repair option, so couldn't decide, " +
                        "if the file ", fi1.FullName, 
                        " can be restored using ", fi2.FullName);
                    return;
                }

                // there we try to restore the older file 2, since it seems to be OK,
                // while newer file 1 failed.
                fiSavedInfo2 = iFileSystem.GetFileInfo(
                    Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

                bForceCreateInfo2 = false;

                if (iSettings.CreateInfo &&
                    (!fiSavedInfo2.Exists ||
                        fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc ||
                        bForceCreateInfo2))
                {
                    bCopied2To1 = iStepsImpl.Create2SavedInfosAndCopy(
                        fi2.FullName, fiSavedInfo2.FullName, 
                        strFilePath1, fiSavedInfo1.FullName,
                        "(file was healthy)", Properties.Resources.FileWasHealthy,
                        iFileSystem, iSettings, iLogWriter);

                    fiSavedInfo1 = iFileSystem.GetFileInfo(
                        Utils.CreatePathOfChkFile(fi1.DirectoryName,
                        "RestoreInfo", fi1.Name, ".chk"));
                    fiSavedInfo2 = iFileSystem.GetFileInfo(
                        Utils.CreatePathOfChkFile(fi1.DirectoryName,
                        "RestoreInfo", fi1.Name, ".chk"));

                    if (bCopied2To1)
                    {
                        bForceCreateInfo2 = false;
                        bForceCreateInfo1 = false;
                    }
                    else
                    {
                        // should actually never happen, since we go there only if file 2 could be restored above
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.InternalErrorCouldntRestoreAny,
                            fi1.FullName, fi2.FullName);
                        iLogWriter.WriteLog(true, 0, "Internal error: Couldn't " +
                            "restore any of the copies of the file ",
                            fi1.FullName, ", ", fi2.FullName);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        iStepsImpl.CopyRepairSingleFile(
                            strFilePath1, strFilePath2, fiSavedInfo2.FullName,
                            ref bForceCreateInfo2, ref bForceCreateInfo1,
                            "(file was healthy or repaired)", 
                            Properties.Resources.FileHealthyOrRepaired, true, true,
                            iFileSystem, iSettings, iLogWriter);
                    }
                    catch (Exception)
                    {
                        // should actually never happen, since we go there only
                        // if file 2 could be restored above
                        iLogWriter.WriteLogFormattedLocalized(0, 
                            Properties.Resources.InternalErrorCouldntRestoreAny,
                            fi1.FullName, fi2.FullName);

                        iLogWriter.WriteLog(true, 0, "Internal error: Couldn't " +
                        "restore any of the copies of the file ", 
                        fi1.FullName, ", ", fi2.FullName);
                        return;
                    }
                }


                if ((iSettings.CreateInfo &&
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc)) ||
                    (fiSavedInfo2.Exists && bForceCreateInfo2))
                {
                    iStepsImpl.CreateSavedInfo(strFilePath2,
                        Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                        iFileSystem, iSettings, iLogWriter);

                    fiSavedInfo2 = iFileSystem.GetFileInfo(
                        Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                if (fiSavedInfo2.Exists)
                {
                    if (bForceCreateInfo1)
                    {
                        if (iSettings.CreateInfo || fiSavedInfo1.Exists)
                            iStepsImpl.CreateSavedInfo(strFilePath1,
                                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"),
                                iFileSystem, iSettings, iLogWriter);
                    }
                    else
                        iFileSystem.CopyTo(fiSavedInfo2,
                            Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), true);
                }
            }
            finally
            {
                //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist and second file has a more recent date
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            ProcessFilePair_Bidirectionally_BothExist_FirstNewer(
                strFilePath2, strFilePath1, fi2, fi1,
                "(file was newer or bigger)", Properties.Resources.FileWasNewer,
                iFileSystem, iSettings, iStepsImpl, iLogWriter);
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist and seem to have same last-write-time and length
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
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
            // bidirectionally path
            // both files are present and have same modification date
            IFileInfo fiSavedInfo2 =
                iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            IFileInfo fiSavedInfo1 =
                iFileSystem.GetFileInfo(Utils.CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the restoreinfo files is missing or has wrong date, 
            // but the other is OK then copy the one to the other
            if (fiSavedInfo1.Exists &&
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc &&
                (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();
                    iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                }
                finally
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                }

                fiSavedInfo2 = iFileSystem.GetFileInfo(
                    Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            }
            else
                if (fiSavedInfo2.Exists &&
                    fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc &&
                    (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
            {
                try
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();
                    iFileSystem.CopyTo(fiSavedInfo2, fiSavedInfo1.FullName, true);
                }
                finally
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                }

                fiSavedInfo1 = iFileSystem.GetFileInfo(
                    Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            }


            bool bCreateInfo = false;
            bool bCreateInfo1 = false;
            bool bCreateInfo2 = false;
            if (iSettings.TestFiles)
            {
                bool bFirstOrSecond;
                lock (this)
                {
                    bFirstOrSecond = m_bRandomOrder = !m_bRandomOrder;
                }

                if (iSettings.RepairFiles)
                {
                    bool bTotalResultOk = true;
                    if (bFirstOrSecond)
                    {
                        bTotalResultOk = iStepsImpl.TestSingleFile2(fi1.FullName,
                            fiSavedInfo1.FullName, ref bCreateInfo1, true,
                            !iSettings.TestFilesSkipRecentlyTested, true, true, false,
                            iFileSystem, iSettings, iLogWriter);

                        if (!string.Equals(fi1.FullName, fi2.FullName,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            bTotalResultOk = bTotalResultOk &&
                            iStepsImpl.TestSingleFile2(
                                fi2.FullName, fiSavedInfo2.FullName,
                                ref bCreateInfo2, true,
                                !iSettings.TestFilesSkipRecentlyTested,
                                true, true, false,
                                iFileSystem, iSettings, iLogWriter);
                        }
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        bTotalResultOk = iStepsImpl.TestSingleFile2(fi2.FullName,
                            fiSavedInfo2.FullName, ref bCreateInfo2, true,
                            !iSettings.TestFilesSkipRecentlyTested, true, true, false,
                            iFileSystem, iSettings, iLogWriter);

                        if (!string.Equals(fi1.FullName, fi2.FullName,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            bTotalResultOk = bTotalResultOk &&
                            iStepsImpl.TestSingleFile2(
                                fi1.FullName, fiSavedInfo1.FullName, 
                                ref bCreateInfo1, true, 
                                !iSettings.TestFilesSkipRecentlyTested, 
                                true, true, false,
                                iFileSystem, iSettings, iLogWriter);
                        }
                        else
                        {
                            bCreateInfo1 = bCreateInfo2;
                            bCreateInfo2 = false;
                        }
                    }

                    if (!bTotalResultOk)
                    {
                        iStepsImpl.TestAndRepairTwoFiles(fi1.FullName, fi2.FullName,
                            fiSavedInfo1.FullName, fiSavedInfo2.FullName, ref bCreateInfo,
                            iFileSystem, iSettings, iLogWriter);
                        bCreateInfo1 = bCreateInfo;
                        bCreateInfo2 = bCreateInfo;
                    }
                }
                else
                {
                    if (bFirstOrSecond)
                    {
                        iStepsImpl.TestSingleFile2(
                            fi1.FullName, fiSavedInfo1.FullName, 
                            ref bCreateInfo1, true, 
                            !iSettings.TestFilesSkipRecentlyTested, 
                            true, false, false,
                            iFileSystem, iSettings, iLogWriter);

                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            iStepsImpl.TestSingleFile2(
                                fi2.FullName, fiSavedInfo2.FullName, 
                                ref bCreateInfo2, true, 
                                !iSettings.TestFilesSkipRecentlyTested, 
                                true, false, false,
                                iFileSystem, iSettings, iLogWriter);
                        }
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        iStepsImpl.TestSingleFile2(
                            fi2.FullName, fiSavedInfo2.FullName, 
                            ref bCreateInfo2, true, 
                            !iSettings.TestFilesSkipRecentlyTested, 
                            true, false, false,
                            iFileSystem, iSettings, iLogWriter);

                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            iStepsImpl.TestSingleFile2(
                                fi1.FullName, fiSavedInfo1.FullName,
                                ref bCreateInfo1, true, 
                                !iSettings.TestFilesSkipRecentlyTested, 
                                true, false, false,
                                iFileSystem, iSettings, iLogWriter);
                        }
                        else
                            bCreateInfo1 = false;
                    }

                    //iStepsImpl.TestSingleFile(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1,
                    //    iFileSystem, iSettings, iLogWriter);
                    //iStepsImpl.TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2,
                    //    iFileSystem, iSettings, iLogWriter);
                }
            }

            // if at least one of the saved info files needs to be created, then create both at once
            if ((iSettings.CreateInfo && (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc !=
                                    fi1.LastWriteTimeUtc || bCreateInfo1)) ||
                (iSettings.CreateInfo && (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc !=
                                    fi2.LastWriteTimeUtc || bCreateInfo2)))
            {
                if (fiSavedInfo1.FullName.Equals(fiSavedInfo2.FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    // in this case it is the same chk file
                    iStepsImpl.CreateSavedInfo(fi1.FullName, fiSavedInfo1.FullName,
                        iFileSystem, iSettings, iLogWriter);
                } else
                {
                    // use sometimes first folder, sometimes second for reading the original data file
                    if (m_bRandomOrder)
                    {
                        m_bRandomOrder = false;
                        if (!iStepsImpl.Create2SavedInfos(fi2.FullName,
                                fiSavedInfo1.FullName, fiSavedInfo2.FullName,
                                iFileSystem, iSettings, iLogWriter))
                        {
                            // fallback to the other, if first try failed
                            iStepsImpl.Create2SavedInfos(fi1.FullName,
                                fiSavedInfo1.FullName, fiSavedInfo2.FullName,
                                iFileSystem, iSettings, iLogWriter);
                        }
                    }
                    else
                    {
                        m_bRandomOrder= true;
                        if (!iStepsImpl.Create2SavedInfos(fi1.FullName,
                                fiSavedInfo1.FullName, fiSavedInfo2.FullName,
                                iFileSystem, iSettings, iLogWriter))
                        {
                            // fallback to the other, if first try failed
                            iStepsImpl.Create2SavedInfos(fi2.FullName,
                                fiSavedInfo1.FullName, fiSavedInfo2.FullName,
                                iFileSystem, iSettings, iLogWriter);
                        }
                    }
                }
            }

            fiSavedInfo2 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            fiSavedInfo1 = iFileSystem.GetFileInfo(
                Utils.CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the files is missing or has wrong date, but the 
            // other is OK then copy the one to the other
            if (fiSavedInfo1.Exists &&
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc &&
                (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();
                    iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                }
                finally
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                }
            }
            else
                if (fiSavedInfo2.Exists &&
                    fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc &&
                    (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
            {
                try
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.WaitOne();
                    iFileSystem.CopyTo(fiSavedInfo2, fiSavedInfo1.FullName, true);
                }
                finally
                {
                    //TODO: reactivate semaphores m_oSemaphoreCopyFiles.Release();
                }
            }
        }

    }
}