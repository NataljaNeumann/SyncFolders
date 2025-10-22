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
    /// This class provide basic implementation for copying and testing files
    /// </summary>
    //*******************************************************************************************************
    public class FilePairSteps: IFilePairSteps
    {
        //===================================================================================================
        /// <summary>
        /// This method tests a single file, and repairs it, if there are read or checksum errors
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bOnlyIfCompletelyRecoverable">Indicates, if the operation shall be performed
        /// only if the file is healthy or completely recoverable</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iCancelable">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the test or restore succeeded</returns>
        //===================================================================================================
        public bool TestAndRepairSingleFile(
            string strPathFile,
            string strPathSavedInfoFile,
            ref bool bForceCreateInfo,
            bool bOnlyIfCompletelyRecoverable,
            IFileOperations iFileSystem,
            ICancelable iCancelable,
            ILogWriter iLogWriter
            )
        {
            // Check if resources are available
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // Get file info objects for the original file and its saved info
            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile);

            // Create a new SavedInfo object
            SavedInfo si = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            // Try to read the saved info file, first buffered, then unbuffered if needed
            if (!bNotReadableSi)
            {
                try
                {
                    using (IFile s = iFileSystem.CreateBufferedStream(
                            iFileSystem.OpenRead(
                            strPathSavedInfoFile),
                            (int)Math.Min(fiSavedInfo.Length + 1, 8 * 1024 * 1024)))
                    {
                        si.ReadFrom(s, true);
                        s.Close();
                    }
                }
                catch (IOException) // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (IFile s =
                            iFileSystem.OpenRead(strPathSavedInfoFile))
                        {
                            si.ReadFrom(s, false);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException oEx)
                    {
                        // Log error and mark saved info as unreadable
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFile,
                            strPathSavedInfoFile, oEx.Message);

                        iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                            strPathSavedInfoFile, "\": " + oEx.Message);

                        bNotReadableSi = true;
                    }
                }
            }

            // Store previous last write time for possible restoration
            System.DateTime dtmPrevLastWriteTime = finfo.LastWriteTimeUtc;

            // If saved info is not readable, or does not match file length/timestamp, do a block-level test/repair
            if (bNotReadableSi ||
                si.Length != finfo.Length ||
                !Utils.FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc))
            {
                bool bAllBlocksOk = true;

                // If saved info exists, mark that it needs to be recreated
                if (fiSavedInfo.Exists)
                    bForceCreateInfo = true;

                // Log that saved info can't be used for testing
                if (!bNotReadableSi)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.SavedInfoFileCantBeUsedForTesting,
                        strPathSavedInfoFile, strPathFile);

                    iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                        "\" can't be used for testing file \"", strPathFile,
                        "\": it was created for another version of the file");
                }

                int nCountErrors = 0;
                try
                {
                    // Open the file for reading or read/write depending on recoverability
                    using (IFile s = iFileSystem.Open(
                        finfo.FullName, System.IO.FileMode.Open,
                        bOnlyIfCompletelyRecoverable ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite,
                        System.IO.FileShare.Read))
                    {
                        Block oBlock = new Block();

                        // Read blocks sequentially
                        for (long lIndex = 0; ; lIndex++)
                        {
                            try
                            {
                                // Read block, break if end of file
                                if (oBlock.ReadFrom(s) != oBlock.Length)
                                    break;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                nCountErrors++;

                                // Fill bad block with zeros
                                oBlock.Erase();

                                int nLengthToWrite =
                                    (int)(finfo.Length - lIndex * oBlock.Length > oBlock.Length ?
                                        oBlock.Length :
                                        finfo.Length - lIndex * oBlock.Length);

                                if (bOnlyIfCompletelyRecoverable)
                                {
                                    // Log error, skip writing
                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.IOErrorReadingFileOffset,
                                        finfo.FullName, lIndex * oBlock.Length, oEx.Message);

                                    iLogWriter.WriteLog(true, 1, "I/O error reading file ", finfo.FullName,
                                        " position ", lIndex * oBlock.Length, ": ", oEx.Message);

                                    s.Seek(lIndex * oBlock.Length + nLengthToWrite, System.IO.SeekOrigin.Begin);
                                }
                                else
                                {
                                    // Log error and write dummy block
                                    iLogWriter.WriteLogFormattedLocalized(0,
                                        Properties.Resources.ErrorReadingPositionWillFillWithDummy,
                                        finfo.FullName, lIndex * oBlock.Length, oEx.Message);

                                    iLogWriter.WriteLog(true, 0, "Error while reading file ", finfo.FullName,
                                        " position ", lIndex * oBlock.Length, ": ", oEx.Message,
                                        ". Block will be filled with a dummy");

                                    s.Seek(lIndex * oBlock.Length, System.IO.SeekOrigin.Begin);

                                    if (nLengthToWrite > 0)
                                        oBlock.WriteTo(s, nLengthToWrite);
                                }
                                bAllBlocksOk = false;
                            }
                        }

                        s.Close();
                    }

                    // If all blocks are OK, update checked info
                    if (bAllBlocksOk)
                    {
                        CreateOrUpdateFileChecked(strPathSavedInfoFile,
                            iFileSystem, iLogWriter);
                    }
                } finally
                {
                    // If not completely recoverable, update last write time based on errors
                    if (!bOnlyIfCompletelyRecoverable)
                    {
                        if (nCountErrors > 0)
                        {
                            finfo.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24, 23 -
                                (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                            bForceCreateInfo = true;
                        }
                        else
                        {
                            finfo.LastWriteTimeUtc = dtmPrevLastWriteTime;
                        }
                    }
                }

                return bAllBlocksOk;
            }

            // Dictionary to track readable but not accepted blocks
            Dictionary<long, bool> oReadableButNotAccepted =
                new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;
                using (IFile s =
                    iFileSystem.OpenRead(finfo.FullName))
                {
                    si.StartRestore();

                    Block oBlock = new Block();

                    // Read and analyze each block
                    for (long lIndex = 0; ; lIndex++)
                    {
                        try
                        {
                            bool bBlockOk = true;
                            int nReadCount = 0;

                            if ((nReadCount = oBlock.ReadFrom(s)) == oBlock.Length)
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(oBlock, lIndex);
                                if (!bBlockOk)
                                {
                                    // Log checksum error
                                    bAllBlocksOK = false;

                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName, lIndex * oBlock.Length);

                                    iLogWriter.WriteLog(true, 1, finfo.FullName,
                                        ": checksum of block at offset ",
                                        lIndex * oBlock.Length, " not OK");

                                    oReadableButNotAccepted[lIndex] = true;
                                }
                            }
                            else
                            {
                                if (nReadCount > 0)
                                {
                                    // Fill the rest of the block with zeros
                                    oBlock.EraseFrom(nReadCount);

                                    bBlockOk = si.AnalyzeForTestOrRestore(oBlock, lIndex);

                                    if (!bBlockOk)
                                    {
                                        bAllBlocksOK = false;

                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName, lIndex * oBlock.Length);

                                        iLogWriter.WriteLog(true, 1, finfo.FullName,
                                            ": checksum of block at offset ",
                                            lIndex * oBlock.Length, " not OK");

                                        oReadableButNotAccepted[lIndex] = true;
                                    }
                                }
                                break;
                            }

                            // Check for cancellation
                            if (iCancelable.CancelClicked)
                                throw new OperationCanceledException();

                        }
                        catch (System.IO.IOException ex)
                        {
                            // Log I/O error and skip to next block
                            bAllBlocksOK = false;

                            iLogWriter.WriteLogFormattedLocalized(1,
                                Properties.Resources.IOErrorReadingFileOffset,
                                finfo.FullName, lIndex * oBlock.Length, ex.Message);

                            iLogWriter.WriteLog(true, 1, "I/O Error reading file: \"",
                                finfo.FullName, "\", offset ",
                                lIndex * oBlock.Length, ": " + ex.Message);

                            if ((lIndex + 1) * oBlock.Length >= s.Length)
                                break;

                            s.Seek((lIndex + 1) * oBlock.Length,
                                System.IO.SeekOrigin.Begin);
                        }

                    }

                    s.Close();
                }

                // If all blocks are OK, verify integrity and possibly update checked info
                if (bAllBlocksOK)
                {
                    if (!si.VerifyIntegrityAfterRestoreTest() || si.NeedsRebuild())
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        // Log that saved info is damaged and needs recreation
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                            strPathSavedInfoFile, strPathFile);

                        iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                            "\" has been damaged and needs to be recreated from \"",
                            strPathFile, "\"");

                        bForceCreateInfo = true;
                    }
                    else
                    {
                        CreateOrUpdateFileChecked(strPathSavedInfoFile,
                            iFileSystem, iLogWriter);
                    }
                }
            }
            catch (System.IO.IOException oEx)
            {
                // Log I/O error and return false
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + oEx.Message);

                return false;
            }

            try
            {
                long lNonRestoredSize = 0;

                // Get restore info for blocks that need to be restored
                List<RestoreInfo> aResoreInfos = si.EndRestore(
                    out lNonRestoredSize, fiSavedInfo.FullName, iLogWriter);

                if (aResoreInfos.Count > 0)
                {
                    if (lNonRestoredSize > 0)
                    {
                        bForceCreateInfo = true;
                    }

                    // If all non-restored size is zero or not strictly requiring complete recovery, restore blocks
                    if (lNonRestoredSize == 0 || !bOnlyIfCompletelyRecoverable)
                    {
                        try
                        {
                            using (IFile s =
                                iFileSystem.OpenWrite(finfo.FullName))
                            {
                                foreach (RestoreInfo oRestoreInfo in aResoreInfos)
                                {
                                    if (oRestoreInfo.NotRecoverableArea)
                                    {
                                        // If block is readable but not accepted, keep it, else fill with dummy
                                        if (oReadableButNotAccepted.ContainsKey(oRestoreInfo.Position / oRestoreInfo.Data.Length))
                                        {
                                            /* TODO: this line of code isn't hit by any unit tests */
                                            iLogWriter.WriteLogFormattedLocalized(1,
                                                Properties.Resources.KeepingReadableButNotRecoverableBlockAtOffset,
                                                oRestoreInfo.Position);

                                            iLogWriter.WriteLog(true, 1,
                                                "Keeping readable but not recoverable block at offset ",
                                                oRestoreInfo.Position, ", checksum indicates the block is wrong");
                                        }
                                        else
                                        {
                                            s.Seek(oRestoreInfo.Position, System.IO.SeekOrigin.Begin);

                                            iLogWriter.WriteLogFormattedLocalized(1,
                                                Properties.Resources.FillingNotRecoverableAtOffsetWithDummy,
                                                oRestoreInfo.Position);

                                            iLogWriter.WriteLog(true, 1, "Filling not recoverable block at offset ",
                                                oRestoreInfo.Position, " with a dummy block");

                                            int nLengthToWrite = (int)(si.Length - oRestoreInfo.Position >= oRestoreInfo.Data.Length ?
                                                oRestoreInfo.Data.Length :
                                                si.Length - oRestoreInfo.Position);

                                            if (nLengthToWrite > 0)
                                                oRestoreInfo.Data.WriteTo(s, nLengthToWrite);
                                        }
                                        bForceCreateInfo = true;
                                    }
                                    else
                                    {
                                        // Restore block from saved info
                                        s.Seek(oRestoreInfo.Position, System.IO.SeekOrigin.Begin);

                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                            oRestoreInfo.Position, finfo.FullName);

                                        iLogWriter.WriteLog(true, 1, "Recovering block at offset ",
                                            oRestoreInfo.Position, " of the file ", finfo.FullName);

                                        int nLengthToWrite = (int)(si.Length - oRestoreInfo.Position >= oRestoreInfo.Data.Length ?
                                            oRestoreInfo.Data.Length :
                                            si.Length - oRestoreInfo.Position);

                                        if (nLengthToWrite > 0)
                                            oRestoreInfo.Data.WriteTo(s, nLengthToWrite);
                                    }
                                }

                                s.Close();
                            }
                        } finally
                        {
                            // If not completely recoverable, update last write time based on errors
                            if (!bOnlyIfCompletelyRecoverable)
                            {
                                if (lNonRestoredSize > 0)
                                {
                                    int nCountErrors = (int)(lNonRestoredSize / 4096);

                                    finfo.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24, 23 -
                                        (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                                    bForceCreateInfo = true;
                                }
                                else
                                {
                                    finfo.LastWriteTimeUtc = dtmPrevLastWriteTime;
                                }
                            }
                        }
                    }
                }

                // If file is not completely recoverable, log and restore last write time
                if (bOnlyIfCompletelyRecoverable && lNonRestoredSize != 0)
                {
                    if (aResoreInfos.Count > 1)
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.ThereAreBadBlocksNonRestorableCantBeBackup,
                            aResoreInfos.Count, finfo.FullName, lNonRestoredSize);

                        iLogWriter.WriteLog(true, 0, "There are ", aResoreInfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", non-restorable parts: ", lNonRestoredSize, " bytes, file can't be used as backup");
                    }
                    else if (aResoreInfos.Count > 0)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.ThereIsBadBlockNonRestorableCantBeBackup,
                            finfo.FullName, lNonRestoredSize);

                        iLogWriter.WriteLog(true, 0, "There is one bad block in the file ", finfo.FullName,
                            " and it can't be restored: ", lNonRestoredSize, " bytes, file can't be used as backup");
                    }

                    finfo.LastWriteTimeUtc = dtmPrevLastWriteTime;
                }
                else
                {
                    // Log summary of bad blocks and update checked info if needed
                    if (aResoreInfos.Count > 1)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                            aResoreInfos.Count, finfo.FullName, lNonRestoredSize);

                        iLogWriter.WriteLog(true, 0, "There were ", aResoreInfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", not restored parts: ", lNonRestoredSize, " bytes");
                    }
                    else if (aResoreInfos.Count > 0)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.ThereWasBadBlockInFileNotRestoredParts,
                            finfo.FullName, lNonRestoredSize);

                        iLogWriter.WriteLog(true, 0, "There was one bad block in the file ", finfo.FullName,
                            ", not restored parts: ", lNonRestoredSize, " bytes");
                    }

                    if (lNonRestoredSize == 0)
                    {
                        if (si.NeedsRebuild())
                        {
                            iLogWriter.WriteLogFormattedLocalized(0,
                                Properties.Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                                strPathSavedInfoFile, strPathFile);

                            iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                                "\" has been damaged and needs to be recreated from \"",
                                strPathFile, "\"");

                            bForceCreateInfo = true;
                        }
                        else
                        {
                            CreateOrUpdateFileChecked(strPathSavedInfoFile,
                                iFileSystem, iLogWriter);
                        }
                    }

                    if (lNonRestoredSize > 0)
                    {
                        int nCountErrors = (int)(lNonRestoredSize / (new Block().Length));

                        finfo.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24, 23 -
                            (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                        bForceCreateInfo = true;
                    }
                    else
                    {
                        finfo.LastWriteTimeUtc = dtmPrevLastWriteTime;
                    }
                }

                // Return true if all blocks restored
                return lNonRestoredSize == 0;
            }
            catch (System.IO.IOException oEx)
            {
                /* TODO: this line of code isn't hit by any unit tests */
                // Log I/O error during writing and restore last write time
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorWritingFile,
                    finfo.FullName, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"",
                    finfo.FullName, "\": " + oEx.Message);

                finfo.LastWriteTimeUtc = dtmPrevLastWriteTime;

                return false;
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method tests a single file, together with its saved info, if present
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bNeedsMessageAboutOldSavedInfo">Specifies, if method shall add a message
        /// in case saved info is outdated or wrong</param>
        /// <param name="bForcePhysicalTest">If set to false, method will analyze the last date and
        /// time the original file has been tested or copied and skip physical test, if possible</param>
        /// <param name="bCreateConfirmationFile">If test succeeds then information about succeeded
        /// test is saved in file system</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the test succeeded</returns>
        //===================================================================================================
        public bool TestSingleFile(
            string strPathFile,
            string strPathSavedInfoFile,
            ref bool bForceCreateInfo,
            bool bNeedsMessageAboutOldSavedInfo,
            bool bForcePhysicalTest,
            bool bCreateConfirmationFile,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter)
        {
            return TestSingleFile2(strPathFile, strPathSavedInfoFile, ref bForceCreateInfo,
                bNeedsMessageAboutOldSavedInfo, bForcePhysicalTest, bCreateConfirmationFile,
                false, false,
                iFileSystem, iSettings, iLogWriter);
        }


        //===================================================================================================
        /// <summary>
        /// This method copies a single file, and repairs it on the way, if it encounters some bad blocks.
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathTargetFile">The path of target file for copy</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bForceCreateInfoTarget">If saved info of target file needs to be updated then 
        /// method sets given var to true</param>
        /// <param name="strReasonEn">The reason of copy for messages</param>
        /// <param name="strReasonTranslated">The reason of copy for messages, localized</param>
        /// <param name="bApplyRepairsToSrc">If set to true, method will also repair source file,
        /// not only the copy</param>
        /// <param name="bFailOnNonRecoverable">If there are non-recoverable blocks and this flag
        /// is set to true, then method throws an exception, instead of continuing</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff copy succeeded</returns>
        //===================================================================================================
        public bool CopyRepairSingleFile(
            string strPathTargetFile,
            string strPathFile,
            string strPathSavedInfoFile,
            ref bool bForceCreateInfo,
            ref bool bForceCreateInfoTarget,
            string strReasonEn,
            string strReasonTranslated,
            bool bFailOnNonRecoverable,
            bool bApplyRepairsToSrc,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter)
        {
            // Check if resources are available
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // If source and target are the same file, try to repair in place
            if (string.Equals(strPathTargetFile, strPathFile,
                StringComparison.InvariantCultureIgnoreCase))
            {
                /* TODO: this line of code isn't hit by any unit tests */
                // If both test and repair are enabled, test and repair the file
                if (iSettings.TestFiles && iSettings.RepairFiles)
                {
                    if (!TestAndRepairSingleFile(strPathFile, strPathSavedInfoFile,
                        ref bForceCreateInfo, !bFailOnNonRecoverable,
                        iFileSystem, iSettings, iLogWriter))
                    {
                        string strMessage = string.Format(
                            Properties.Resources.ErrorWhileTestingFile, strPathFile);

                        iLogWriter.WriteLog(false, 1, strMessage);
                        iLogWriter.WriteLog(true, 1, "Error while testing file ", strPathFile);

                        if (bFailOnNonRecoverable)
                            throw new IOException(strMessage);

                        return false;
                    } else
                    {
                        return true;
                    }
                }
                else
                {
                    // If only test is enabled, test the file
                    if (iSettings.TestFiles)
                    {
                        if (TestSingleFile2(strPathFile, strPathSavedInfoFile,
                            ref bForceCreateInfo, false, true, true, true, false,
                            iFileSystem, iSettings, iLogWriter))
                        {
                            return true;
                        }
                        else
                        {
                            string strMessage = string.Format(
                                Properties.Resources.ErrorWhileTestingFile, strPathFile);

                            iLogWriter.WriteLog(false, 1, strMessage);
                            iLogWriter.WriteLog(true, 1, "Error while testing file ", strPathFile);

                            if (bFailOnNonRecoverable)
                                throw new IOException(strMessage);

                            return false;
                        }
                    }
                    else
                        return true;
                }
            }

            // Get file info objects for source and saved info
            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile);

            DateTime dtmOriginalTime = finfo.LastWriteTimeUtc;

            // Try to read saved info file
            SavedInfo oSavedInfo = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (IFile s =
                        iFileSystem.OpenRead(strPathSavedInfoFile))
                    {
                        oSavedInfo.ReadFrom(s, false);
                        s.Close();
                    }
                }
                catch (System.IO.IOException oEx)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    // Log error and mark saved info as unreadable
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.IOErrorReadingFile,
                        strPathSavedInfoFile, oEx.Message);

                    iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                        strPathSavedInfoFile, "\": " + oEx.Message);

                    bNotReadableSi = true;
                }
            }

            // If saved info is not readable or does not match file length/timestamp, do block-level copy/repair
            if (bNotReadableSi || oSavedInfo.Length != finfo.Length ||
                !(iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                Utils.FileTimesEqual(oSavedInfo.TimeStamp, finfo.LastWriteTimeUtc)))
            {
                bool bAllBlocksOk = true;

                if (!bNotReadableSi)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    // Mark that saved info needs to be recreated and log message
                    bForceCreateInfo = true;
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.SavedInfoFileCantBeUsedForTesting,
                        strPathSavedInfoFile, strPathFile);

                    iLogWriter.WriteLog(true, 0, "RestoreInfo file \"", strPathSavedInfoFile,
                        "\" can't be used for restoring file \"",
                        strPathFile, "\": it was created for another version of the file");
                }

                try
                {
                    // Try to copy file safely (buffered copy)
                    CopyFileSafely(finfo, strPathTargetFile,
                        strReasonEn, strReasonTranslated,
                        iFileSystem, iLogWriter);

                    // update information that we just tested the two files
                    IFileInfo fi2 = iFileSystem.GetFileInfo(strPathTargetFile);

                    CreateOrUpdateFileChecked(Utils.CreatePathOfChkFile(
                            fi2.Directory.FullName, "RestoreInfo", fi2.Name, ".chk"),
                        iFileSystem, iLogWriter);

                    // If saved info does not exist, create it if allowed
                    if (!fiSavedInfo.Exists)
                    {
                        if (!iSettings.FirstToSecond || !iSettings.FirstReadOnly)
                        {
                            CreateOrUpdateFileChecked(fiSavedInfo.FullName, iFileSystem, iLogWriter);
                        }
                    }

                    return true;

                } catch (IOException)
                {
                    // If simple copy failed, continue with block-level copy/repair
                }

                // Open source file for reading
                using (IFile s = iFileSystem.Open(
                    finfo.FullName, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {

                    int nCountErrors = 0;

                    // Open target file for writing (as .tmp)
                    using (IFile s2 = iFileSystem.Open(
                        strPathTargetFile + ".tmp", System.IO.FileMode.Create,
                        System.IO.FileAccess.Write, System.IO.FileShare.None))
                    {
                        Block oBlock = new Block();

                        for (long lIndex = 0; ; lIndex++)
                        {
                            try
                            {
                                int nLengthToWrite = oBlock.ReadFrom(s);

                                if (nLengthToWrite > 0)
                                    oBlock.WriteTo(s2, nLengthToWrite);

                                if (nLengthToWrite != oBlock.Length)
                                    break;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                // If non-recoverable blocks are not allowed, throw
                                if (bFailOnNonRecoverable)
                                    throw;

                                // Log error and fill block with zeros
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.IOErrorWhileReadingPositionFillDummyWhileCopy,
                                    finfo.FullName, lIndex * oBlock.Length, oEx.Message);

                                iLogWriter.WriteLog(true, 1, "I/O Error while reading file ",
                                    finfo.FullName, " position ", lIndex * oBlock.Length, ": ",
                                    oEx.Message, ". Block will be replaced with a dummy during copy.");

                                int nLengthToWrite = (int)(finfo.Length - lIndex * oBlock.Length > oBlock.Length ?
                                    oBlock.Length :
                                    finfo.Length - lIndex * oBlock.Length);

                                if (nLengthToWrite > 0)
                                {
                                    // Erase the beginning of the block that we need to write
                                    oBlock.EraseTo(nLengthToWrite - 1);
                                    
                                    oBlock.WriteTo(s2, nLengthToWrite);
                                }

                                bAllBlocksOk = false;
                                ++nCountErrors;

                                if (nLengthToWrite != oBlock.Length)
                                    break;

                                s.Seek(lIndex * oBlock.Length + nLengthToWrite, System.IO.SeekOrigin.Begin);

                            }
                        }

                        s2.Close();
                    }

                    // After copying to .tmp, delete old target file
                    IFileInfo fi2 = iFileSystem.GetFileInfo(strPathTargetFile);

                    if (fi2.Exists)
                    {
                        iFileSystem.Delete(fi2);
                    }

                    // Replace target file with new .tmp file
                    IFileInfo fi2tmp = iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");

                    if (bAllBlocksOk)
                    {
                        // If all blocks OK, set original time
                        fi2tmp.LastWriteTimeUtc = dtmOriginalTime;

                        // Update information that we just tested the two files
                        CreateOrUpdateFileChecked(Utils.CreatePathOfChkFile(
                                fi2.Directory.FullName, "RestoreInfo", fi2.Name, ".chk"),
                            iFileSystem, iLogWriter);

                        if (!fiSavedInfo.Exists)
                        {
                            if (!iSettings.FirstToSecond || !iSettings.FirstReadOnly)
                            {
                                CreateOrUpdateFileChecked(fiSavedInfo.FullName, iFileSystem, iLogWriter);
                            }
                        }
                    }
                    else
                    {
                        // If errors, set time to very old so newer files are preferred
                        fi2tmp.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24,
                            23 - (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                        bForceCreateInfoTarget = true;
                    }

                    fi2tmp.MoveTo(strPathTargetFile);

                    // Log result
                    if (!bAllBlocksOk)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.WarningCopiedToWithErrors,
                            strPathFile, strPathTargetFile, strReasonTranslated);

                        iLogWriter.WriteLog(true, 0, "Warning: copied ", strPathFile, " to ",
                            strPathTargetFile, " ", strReasonEn, " with errors");
                    }
                    else
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.CopiedFromToReason,
                            strPathFile, strPathTargetFile, strReasonTranslated);

                        iLogWriter.WriteLog(true, 0, "Copied ", strPathFile, " to ",
                            strPathTargetFile, " ", strReasonEn);
                    }


                    s.Close();
                }

                return bAllBlocksOk;
            }

            // Dictionary to track readable but not accepted blocks
            Dictionary<long, bool> oReadableButNotAccepted = new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;

                // Read and analyze each block from source file
                using (IFile s =
                    iFileSystem.OpenRead(finfo.FullName))
                {
                    oSavedInfo.StartRestore();

                    Block oBlock = new Block();

                    for (long lIndex = 0; ; lIndex++)
                    {
                        try
                        {
                            bool bBlockOk = true;
                            int nRead = 0;

                            if ((nRead = oBlock.ReadFrom(s)) == oBlock.Length)
                            {
                                bBlockOk = oSavedInfo.AnalyzeForTestOrRestore(oBlock, lIndex);

                                if (!bBlockOk)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    // Log checksum error
                                    bAllBlocksOK = false;

                                    iLogWriter.WriteLogFormattedLocalized(2,
                                        Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName, lIndex * oBlock.Length);

                                    iLogWriter.WriteLog(true, 2, finfo.FullName,
                                        ": checksum of block at offset ",
                                        lIndex * oBlock.Length, " not OK");

                                    oReadableButNotAccepted[lIndex] = true;
                                }
                            }
                            else
                            {
                                if (nRead > 0)
                                {
                                    // Fill the rest of the block with zeros
                                    oBlock.EraseFrom(nRead);

                                    bBlockOk = oSavedInfo.AnalyzeForTestOrRestore(oBlock, lIndex);

                                    if (!bBlockOk)
                                    {
                                        /* TODO: this line of code isn't hit by any unit tests */
                                        bAllBlocksOK = false;

                                        iLogWriter.WriteLogFormattedLocalized(2,
                                            Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName, lIndex * oBlock.Length);

                                        iLogWriter.WriteLog(true, 2, finfo.FullName,
                                            ": checksum of block at offset ",
                                            lIndex * oBlock.Length, " not OK");

                                        oReadableButNotAccepted[lIndex] = true;
                                    }
                                }
                                break;
                            }
                        }
                        catch (System.IO.IOException oEx)
                        {
                            // Log I/O error and skip to next block
                            bAllBlocksOK = false;

                            iLogWriter.WriteLogFormattedLocalized(2,
                                Properties.Resources.IOErrorReadingFileOffset,
                                finfo.FullName, lIndex * oBlock.Length, oEx.Message);

                            iLogWriter.WriteLog(true, 2, "I/O Error reading file: \"",
                                finfo.FullName, "\", offset ",
                                lIndex * oBlock.Length, ": " + oEx.Message);

                            s.Seek((lIndex + 1) * oBlock.Length,
                                System.IO.SeekOrigin.Begin);
                        }

                        // Check for cancellation
                        if (iSettings.CancelClicked)
                            throw new OperationCanceledException();

                    }

                    s.Close();
                }

                // If all blocks are OK
                if (bAllBlocksOK)
                {
                    // Verify integrity of save into,
                    // if the contents of the checksum file match 
                    // the file itself, or if they have been corrupted somehow
                    if (!oSavedInfo.VerifyIntegrityAfterRestoreTest())
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                            strPathSavedInfoFile, strPathFile);

                        iLogWriter.WriteLog(true, 0, "Saved info file \"",
                            strPathSavedInfoFile,
                            "\" has been damaged and needs to be recreated from \"",
                            strPathFile, "\"");

                        bForceCreateInfo = true;
                    }
                }
            }
            catch (System.IO.IOException oEx)
            {
                /* TODO: this line of code isn't hit by any unit tests */
                // Log I/O error and return false
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + oEx.Message);

                if (bFailOnNonRecoverable)
                    throw;

                return false;
            }


            try
            {
                long lNonRestoredSize = 0;

                // Get restore info for blocks that need to be restored
                List<RestoreInfo> aRestoreInfos = oSavedInfo.EndRestore(
                    out lNonRestoredSize, strPathSavedInfoFile, iLogWriter);

                // If there are non-restorable blocks, handle according to flag
                if (lNonRestoredSize > 0)
                {
                    if (bFailOnNonRecoverable)
                    {
                        iLogWriter.WriteLogFormattedLocalized(1,
                            Properties.Resources.ThereAreBadBlocksInNonRestorableMayRetryLater,
                            aRestoreInfos.Count, finfo.FullName, lNonRestoredSize);

                        iLogWriter.WriteLog(true, 1, "There are ", aRestoreInfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", non-restorable parts: ", lNonRestoredSize,
                            " bytes. Can't proceed there because of non-recoverable, may retry later.");

                        throw new IOException("Non-recoverable blocks discovered, failing");
                    }
                    else
                        bForceCreateInfoTarget = true;
                }

                // Log summary of bad blocks
                if (aRestoreInfos.Count > 1)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    iLogWriter.WriteLogFormattedLocalized(1,
                        Properties.Resources.ThereAreBadBlocksInFileNonRestorableParts +
                            (bApplyRepairsToSrc ? "" :
                             Properties.Resources.TheFileCantBeModifiedMissingRepairApplyToCopy),
                        aRestoreInfos.Count, finfo.FullName, lNonRestoredSize);

                    iLogWriter.WriteLog(true, 1, "There are ", aRestoreInfos.Count,
                        " bad blocks in the file ", finfo.FullName,
                        ", non-restorable parts: ", lNonRestoredSize, " bytes. " +
                        (bApplyRepairsToSrc ? "" :
                            "The file can't be modified because of missing repair option, " +
                            "the restore process will be applied to copy."));
                }
                else if (aRestoreInfos.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(1,
                       Properties.Resources.ThereIsBadBlockInFileNonRestorableParts +
                           (bApplyRepairsToSrc ? "" :
                           Properties.Resources.TheFileCantBeModifiedMissingRepairApplyToCopy),
                       finfo.FullName, lNonRestoredSize);

                    iLogWriter.WriteLog(true, 1, "There is one bad block in the file ", finfo.FullName,
                       ", non-restorable parts: ", lNonRestoredSize, " bytes. " +
                       (bApplyRepairsToSrc ? "" :
                           "The file can't be modified because of missing repair option, " +
                           "the restore process will be applied to copy."));
                }

                // Open source and target files for restore/copy
                try
                {
                    using (IFile s2 =
                        iFileSystem.Open(
                            finfo.FullName, System.IO.FileMode.Open,
                            bApplyRepairsToSrc && (aRestoreInfos.Count > 0) ?
                                System.IO.FileAccess.ReadWrite : System.IO.FileAccess.Read,
                            System.IO.FileShare.Read))
                    {
                        using (IFile s =
                            iFileSystem.Open(
                            strPathTargetFile + ".tmp", System.IO.FileMode.Create,
                            System.IO.FileAccess.Write, System.IO.FileShare.None))
                        {
                            Block oBlock = new Block();
                            int nBlockSize = oBlock.Length;

                            for (long lPosition = 0; lPosition < finfo.Length; lPosition += nBlockSize)
                            {
                                bool bBlockWritten = false;

                                foreach (RestoreInfo oRestoreInfo in aRestoreInfos)
                                {
                                    if (oRestoreInfo.Position == lPosition)
                                    {
                                        bBlockWritten = true;

                                        if (oRestoreInfo.NotRecoverableArea)
                                        {
                                            if (oReadableButNotAccepted.ContainsKey(oRestoreInfo.Position / nBlockSize))
                                            {
                                                /* TODO: this line of code isn't hit by any unit tests */
                                                iLogWriter.WriteLogFormattedLocalized(1,
                                                    Properties.Resources.KeepingReadableNonRecovBBlockAtAlsoInCopy,
                                                    oRestoreInfo.Position, finfo.FullName, strPathTargetFile);

                                                iLogWriter.WriteLog(true, 1,
                                                    "Keeping readable but not recoverable block at offset ",
                                                    oRestoreInfo.Position, " of original file ", finfo.FullName,
                                                    " also in copy ", strPathTargetFile,
                                                    ", checksum indicates the block is wrong");
                                            }
                                            else
                                            {
                                                s2.Seek(oRestoreInfo.Position + oRestoreInfo.Data.Length, System.IO.SeekOrigin.Begin);

                                                iLogWriter.WriteLogFormattedLocalized(1,
                                                    Properties.Resources.FillingNotRecoverableAtOffsetOfCopyWithDummy,
                                                    oRestoreInfo.Position, strPathTargetFile);

                                                iLogWriter.WriteLog(true, 1, "Filling not recoverable block at offset ",
                                                    oRestoreInfo.Position, " of copied file ", strPathTargetFile, " with a dummy");

                                                int nLengthToWrite = (int)(finfo.Length - lPosition > nBlockSize ?
                                                    nBlockSize :
                                                    finfo.Length - lPosition);

                                                if (nLengthToWrite > 0)
                                                    oRestoreInfo.Data.WriteTo(s, nLengthToWrite);
                                            }
                                            bForceCreateInfoTarget = true;
                                        }
                                        else
                                        {
                                            iLogWriter.WriteLogFormattedLocalized(1,
                                                Properties.Resources.RecoveringBlockAtOfCopiedFile,
                                                oRestoreInfo.Position, strPathTargetFile);

                                            iLogWriter.WriteLog(true, 1, "Recovering block at offset ",
                                                oRestoreInfo.Position, " of copied file ", strPathTargetFile);

                                            int nLengthToWrite = (int)(finfo.Length - oRestoreInfo.Position >= oRestoreInfo.Data.Length ?
                                                oRestoreInfo.Data.Length :
                                                finfo.Length - oRestoreInfo.Position);

                                            if (nLengthToWrite > 0)
                                                oRestoreInfo.Data.WriteTo(s, nLengthToWrite);

                                            if (bApplyRepairsToSrc)
                                            {
                                                iLogWriter.WriteLogFormattedLocalized(1,
                                                    Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                                    oRestoreInfo.Position, finfo.FullName);

                                                iLogWriter.WriteLog(true, 1, "Recovering block at offset ",
                                                    oRestoreInfo.Position, " of file ", finfo.FullName);

                                                s2.Seek(oRestoreInfo.Position, System.IO.SeekOrigin.Begin);

                                                if (nLengthToWrite > 0)
                                                    oRestoreInfo.Data.WriteTo(s2, nLengthToWrite);
                                            }
                                            else
                                            {
                                                s2.Seek(oRestoreInfo.Position + nLengthToWrite,
                                                    System.IO.SeekOrigin.Begin);
                                            }
                                        }
                                        break;
                                    }
                                }

                                if (!bBlockWritten)
                                {
                                    // If no restore info, copy block from source to destination
                                    int nLengthToWrite = oBlock.ReadFrom(s2);
                                    oBlock.WriteTo(s, nLengthToWrite);
                                }
                            }

                            s.Close();
                        }

                        s2.Close();
                    }
                }
                finally
                {
                    // If repairs applied to source, restore its timestamp
                    if (bApplyRepairsToSrc && (aRestoreInfos.Count > 0))
                        finfo.LastWriteTimeUtc = dtmOriginalTime;
                }
                IFileInfo finfoTmp = iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");

                // Replace target file with .tmp file
                if (iFileSystem.Exists(strPathTargetFile))
                    iFileSystem.Delete(strPathTargetFile);

                finfoTmp.MoveTo(strPathTargetFile);

                // If saved info needs rebuild, set flags
                if (oSavedInfo.NeedsRebuild())
                {
                    if ((!iSettings.FirstToSecond || !iSettings.FirstReadOnly) && aRestoreInfos.Count == 0)
                        bForceCreateInfo = true;

                    bForceCreateInfoTarget = bForceCreateInfoTarget || iSettings.CreateInfo;
                }

                // Log summary of bad blocks in copy
                IFileInfo finfo2 = iFileSystem.GetFileInfo(strPathTargetFile);
                if (aRestoreInfos.Count > 1)
                {
                    iLogWriter.WriteLogFormattedLocalized(1,
                        Properties.Resources.OutOfBadBlocksNotRestoredInCopyBytes,
                        aRestoreInfos.Count, finfo2.FullName, lNonRestoredSize);

                    iLogWriter.WriteLog(true, 1, "Out of ", aRestoreInfos.Count,
                        " bad blocks in the original file not restored parts in the copy ",
                        finfo2.FullName, ": ", lNonRestoredSize, " bytes.");
                }
                else if (aRestoreInfos.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(1,
                        Properties.Resources.ThereWasBadBlockNotRestoredInCopyBytes,
                        finfo2.FullName, lNonRestoredSize);

                    iLogWriter.WriteLog(true, 1, "There was one bad block in the original file, " +
                        "not restored parts in the copy ", finfo2.FullName, ": ",
                        lNonRestoredSize, " bytes.");
                }

                // Set last write time of target file based on errors
                if (lNonRestoredSize > 0)
                {
                    int countErrors = (int)(lNonRestoredSize / (new Block().Length));

                    finfo2.LastWriteTimeUtc = new DateTime(1975, 9, 24 - countErrors / 60 / 24,
                        23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);

                    bForceCreateInfoTarget = true;
                }
                else
                {
                    finfo2.LastWriteTimeUtc = dtmOriginalTime;
                }

                // Log final result
                if (lNonRestoredSize != 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.WarningCopiedToWithErrors,
                        strPathFile, strPathTargetFile, strReasonTranslated);

                    iLogWriter.WriteLog(true, 0, "Warning: copied ", strPathFile, " to ",
                        strPathTargetFile, " ", strReasonEn, " with errors");
                }
                else
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.CopiedFromToReason,
                        strPathFile, strPathTargetFile, strReasonTranslated);

                    iLogWriter.WriteLog(true, 0, "Copied ", strPathFile, " to ",
                        strPathTargetFile, " ", strReasonEn);
                }

                return lNonRestoredSize == 0;
            }
            catch (System.IO.IOException oEx)
            {
                if (bFailOnNonRecoverable)
                    throw;

                // Log I/O error during repair/copy
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorDuringRepairCopyOf,
                    strPathTargetFile, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error during repair copy to file: \"",
                    strPathTargetFile, "\": " + oEx.Message);

                return false;
            }
        }



        //===================================================================================================
        /// <summary>
        /// This method tests and repairs the second file with all available means
        /// </summary>
        /// <param name="strPathFile1">Path of first file</param>
        /// <param name="strPathFile2">Path of second file to be tested and repaired</param>
        /// <param name="strPathSavedInfo1">Saved info of the first file</param>
        /// <param name="strPathSavedInfo2">Saved info of the second file</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// var to true</param>
        //===================================================================================================
        public void TestAndRepairSecondFile(
            string strPathFile1,
            string strPathFile2,
            string strPathSavedInfo1,
            string strPathSavedInfo2,
            ref bool bForceCreateInfo,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            )
        {
            // Check if resources are available
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // If we can skip repairs, then try to test first and repair only in case there are some errors.
            if (iSettings.TestFilesSkipRecentlyTested &&
                TestSingleFile(strPathFile2, strPathSavedInfo2,
                    ref bForceCreateInfo, false, false, true,
                    iFileSystem, iSettings, iLogWriter))
            {
                return;
            }

            // Get file info objects for both files and their saved info
            IFileInfo fi1 = iFileSystem.GetFileInfo(strPathFile1);
            IFileInfo fi2 = iFileSystem.GetFileInfo(strPathFile2);
            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(strPathSavedInfo1);
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(strPathSavedInfo2);

            SavedInfo oSavedInfo1 = new SavedInfo();
            SavedInfo oSavedInfo2 = new SavedInfo();

            bool bSaveInfo1Present = false;

            // Try to read saved info for file 1 and validate it
            if (fiSavedInfo1.Exists &&
                (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc))
            {
                using (IFile s = iFileSystem.OpenRead(fiSavedInfo1.FullName))
                {
                    oSavedInfo1.ReadFrom(s, false);

                    bSaveInfo1Present = oSavedInfo1.Length == fi1.Length &&
                        (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                        Utils.FileTimesEqual(oSavedInfo1.TimeStamp, fi1.LastWriteTimeUtc));

                    if (!bSaveInfo1Present)
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        // Saved info 1 is not valid, reset and mark for recreation
                        oSavedInfo1 = new SavedInfo();
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        // If valid, try to read saved info 2 from same stream
                        s.Seek(0, System.IO.SeekOrigin.Begin);
                        oSavedInfo2.ReadFrom(s, false);
                    }
                    s.Close();
                }
            }

            // Try to read saved info for file 2 and validate it
            if (fiSavedInfo2.Exists &&
                (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc))
            {
                using (IFile s = iFileSystem.OpenRead(fiSavedInfo2.FullName))
                {
                    SavedInfo oSavedInfo2_1 = new SavedInfo();
                    oSavedInfo2_1.ReadFrom(s, false);

                    if (oSavedInfo2_1.Length == fi2.Length &&
                        (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                        Utils.FileTimesEqual(oSavedInfo2_1.TimeStamp, fi2.LastWriteTimeUtc)))
                    {
                        oSavedInfo2 = oSavedInfo2_1;
                        if (!bSaveInfo1Present)
                        {
                            // If saved info 1 not present, try to read it from same stream
                            s.Seek(0, System.IO.SeekOrigin.Begin);
                            oSavedInfo1.ReadFrom(s, false);
                            bSaveInfo1Present = true;
                        }
                    }
                    else
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        // Saved info 2 is not valid, mark for recreation
                        bForceCreateInfo = true;
                    }
                    s.Close();
                }
            }

            if (bSaveInfo1Present)
            {
                // If we have valid saved info for file 1, try to improve both saved infos
                System.DateTime dtmPrevLastWriteTime = fi1.LastWriteTimeUtc;

                oSavedInfo1.ImproveThisAndOther(oSavedInfo2);

                // Dictionaries to track block status
                Dictionary<long, bool> oEqualBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> oReadableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> oReadableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> aRestore1 = new List<RestoreInfo>();
                List<RestoreInfo> aRestore2 = new List<RestoreInfo>();

                // Read and compare blocks from both files
                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        oSavedInfo1.StartRestore();
                        oSavedInfo2.StartRestore();

                        Block oBlock1 = new Block();
                        Block oBlock2 = new Block();

                        for (long lIndex = 0; ; ++lIndex)
                        {

                            bool bBlock1Present = false;
                            bool bBlock1Ok = false;

                            // Read block from file 1 and analyze
                            try
                            {
                                int nReadCount = 0;

                                if ((nReadCount = oBlock1.ReadFrom(s1)) == oBlock1.Length)
                                {
                                    bBlock1Ok = oSavedInfo1.AnalyzeForTestOrRestore(oBlock1, lIndex);
                                }
                                else
                                {
                                    if (nReadCount > 0)
                                    {
                                        // Fill the rest with zeros
                                        oBlock1.EraseFrom(nReadCount);

                                        bBlock1Ok = oSavedInfo1.AnalyzeForTestOrRestore(oBlock1, lIndex);
                                    }
                                }

                                oReadableBlocks1[lIndex] = nReadCount > 0;
                                bBlock1Present = nReadCount > 0;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                // Log error and skip to next block
                                iLogWriter.WriteLogFormattedLocalized(2,
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile1, oEx.Message);

                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"",
                                    strPathFile1, "\": ", oEx.Message);
                                s1.Seek((lIndex + 1) * oBlock1.Length, System.IO.SeekOrigin.Begin);
                            }

                            bool bBlock2Present = false;
                            bool bBlock2Ok = false;

                            // Read block from file 2 and analyze
                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = oBlock2.ReadFrom(s2)) == oBlock2.Length)
                                {
                                    bBlock2Ok = oSavedInfo2.AnalyzeForTestOrRestore(oBlock2, lIndex);
                                }
                                else
                                {
                                    // Fill the rest with zeros
                                    oBlock2.EraseFrom(nReadCount);

                                    bBlock2Ok = oSavedInfo2.AnalyzeForTestOrRestore(oBlock2, lIndex);
                                }

                                oReadableBlocks2[lIndex] = nReadCount > 0;
                                bBlock2Present = nReadCount > 0;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                // Log error and skip to next block of file
                                iLogWriter.WriteLogFormattedLocalized(2,
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, oEx.Message);

                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"",
                                    strPathFile2, "\": ", oEx.Message);

                                s2.Seek((lIndex + 1) * oBlock2.Length, System.IO.SeekOrigin.Begin);
                            }

                            // Decide how to restore blocks based on their status
                            if (bBlock1Present && !bBlock2Present)
                            {
                                // Block present in file 1, missing in file 2, try to restore
                                if (oSavedInfo2.AnalyzeForTestOrRestore(oBlock1, lIndex))
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, lIndex * oBlock1.Length, fi1.FullName);

                                    iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                        lIndex * oBlock1.Length, " will be restored from ", fi1.FullName);

                                    aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, false));
                                }
                            }
                            else if (bBlock2Present && !bBlock1Present)
                            {
                                /* TODO: this line of code isn't hit by any unit tests */
                                // Block present in file 2, missing in file 1, try to restore
                                if (oSavedInfo1.AnalyzeForTestOrRestore(oBlock2, lIndex))
                                {
                                    aRestore1.Add(new RestoreInfo(lIndex * oBlock2.Length, oBlock2, false));

                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi1.FullName, lIndex * oBlock1.Length, fi2.FullName);

                                    iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " position ", lIndex *
                                        oBlock1.Length, " could be restored from ", fi2.FullName,
                                        " but it is not possible to write to the first folder");
                                }
                            }
                            else
                            {
                                // Handle cases where blocks are present but not accepted
                                if (bBlock2Present && !bBlock1Ok)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    if (oSavedInfo1.AnalyzeForTestOrRestore(oBlock2, lIndex))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.BlockOfAtPositionCanBeRestoredFromNoWriteFirst,
                                            fi1.FullName, lIndex * oBlock1.Length, fi2.FullName);

                                        iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " at position ",
                                            lIndex * oBlock1.Length, " can be restored from ", fi2.FullName,
                                            " but it is not possible to write to the first folder");

                                        aRestore1.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock2, false));
                                    }
                                }

                                if (bBlock1Present && !bBlock2Ok)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    if (oSavedInfo2.AnalyzeForTestOrRestore(oBlock1, lIndex))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, lIndex * oBlock1.Length, fi1.FullName);

                                        iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " at position ",
                                            lIndex * oBlock1.Length, " will be restored from ", fi1.FullName);

                                        aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, false));
                                    }
                                }
                            }

                            // If both blocks are present, compare their contents
                            if (bBlock1Present && bBlock2Present)
                            {
                                // if both blocks are present we'll compare their contents
                                // equal blocks could have higher priority compared to their checksums and saved infos
                                bool bDifferent = false;

                                for (int i = oBlock1.Length - 1; i >= 0; --i)
                                {
                                    if (oBlock1[i] != oBlock2[i])
                                    {
                                        /* TODO: this line of code isn't hit by any unit tests */
                                        bDifferent = true;
                                        break;
                                    }
                                }

                                if (!bDifferent)
                                {
                                    oEqualBlocks[lIndex] = true;
                                }
                            }

                            // Break if end of both files reached
                            if (s1.Position >= s1.Length && s2.Position >= s2.Length)
                                break;

                            // Check for cancellation
                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }

                    s1.Close();
                }

                // Finalize restore info for both files
                long lNotRestoredSize1 = 0;
                aRestore1.AddRange(oSavedInfo1.EndRestore(
                    out lNotRestoredSize1, fiSavedInfo1.FullName, iLogWriter));
                lNotRestoredSize1 = 0;

                long lNotRestoredSize2 = 0;
                aRestore2.AddRange(oSavedInfo2.EndRestore(
                    out lNotRestoredSize2, fiSavedInfo2.FullName, iLogWriter));
                lNotRestoredSize2 = 0;

                // Apply improvements to file 2 (read/write), file 1 is read-only
                using (IFile s1 = iFileSystem.Open(
                    strPathFile1, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {

                    using (IFile s2 = iFileSystem.Open(
                        strPathFile2, System.IO.FileMode.Open,
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {

                        // Apply improvements from file 1 to file 2
                        // (we are in first folder readonly case)
                        foreach (RestoreInfo oRestoreInfo1 in aRestore1)
                        {
                            foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                            {
                                if (oRestoreInfo2.Position == oRestoreInfo1.Position &&
                                    oRestoreInfo2.NotRecoverableArea &&
                                    !oRestoreInfo1.NotRecoverableArea)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, oRestoreInfo2.Position, fi1.FullName);

                                    iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                        " position ", oRestoreInfo2.Position,
                                        " will be restored from ", fi1.FullName);

                                    oRestoreInfo2.Data = oRestoreInfo1.Data;
                                    oRestoreInfo2.NotRecoverableArea = false;
                                }
                            }
                        }

                        // Apply definitive improvements to file 2
                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            if (oRestoreInfo2.NotRecoverableArea ||
                                (iSettings.PreferPhysicalCopies &&
                                    oEqualBlocks.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length)))
                            {
                                ; // Block not recoverable or prefer physical copy, skip
                            }
                            else
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                    oRestoreInfo2.Position, fi2.FullName);

                                iLogWriter.WriteLog(true, 1, "Recovering block of ",
                                    fi2.FullName, " at position ", oRestoreInfo2.Position);

                                s2.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);

                                int nLengthToWrite = (int)(oSavedInfo2.Length - oRestoreInfo2.Position >= oRestoreInfo2.Data.Length ?
                                    oRestoreInfo2.Data.Length :
                                    oSavedInfo2.Length - oRestoreInfo2.Position);

                                if (nLengthToWrite > 0)
                                    oRestoreInfo2.Data.WriteTo(s2, nLengthToWrite);

                                // Mark block as readable now
                                oReadableBlocks2[oRestoreInfo2.Position / oRestoreInfo2.Data.Length] = true;
                            }
                        }



                        // Try to copy non-recoverable blocks from file 1 to file 2 if possible
                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            if (oRestoreInfo2.NotRecoverableArea &&
                                !oEqualBlocks.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                oReadableBlocks1.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                !oReadableBlocks2.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length))
                            {
                                /* TODO: this line of code isn't hit by any unit tests */
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi2.FullName, oRestoreInfo2.Position, fi1.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " at position ",
                                    oRestoreInfo2.Position, " will be copied from ",
                                    fi1.FullName, " even if checksum indicates the block is wrong");

                                s1.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);

                                Block oTempBlock = new Block();
                                int nLength = oTempBlock.ReadFrom(s1);
                                oTempBlock.WriteTo(s2, nLength);

                                oReadableBlocks2[oRestoreInfo2.Position / oRestoreInfo2.Data.Length] = true;
                            }
                        }

                        // Fill non-readable blocks with zeroes in file 2
                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            if (oRestoreInfo2.NotRecoverableArea &&
                                !oEqualBlocks.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                !oReadableBlocks2.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, oRestoreInfo2.Position);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                    oRestoreInfo2.Position, " is not recoverable and will be filled with a dummy");

                                s2.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);

                                int nLengthToWrite = (int)(oSavedInfo2.Length - oRestoreInfo2.Position >= oRestoreInfo2.Data.Length ?
                                    oRestoreInfo2.Data.Length :
                                    oSavedInfo2.Length - oRestoreInfo2.Position);

                                if (nLengthToWrite > 0)
                                    oRestoreInfo2.Data.WriteTo(s2, nLengthToWrite);

                                lNotRestoredSize2 += nLengthToWrite;
                            }
                        }

                        s2.Close();
                    }

                    s1.Close();
                }

                // Log summary of bad blocks in file 2
                if (aRestore2.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        aRestore2.Count, fi2.FullName, lNotRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", aRestore2.Count,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", lNotRestoredSize2);
                }

                // Log summary of bad blocks in file 1 (read-only)
                if (aRestore1.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        aRestore1.Count, fi1.FullName);

                    iLogWriter.WriteLog(true, 0, "There remain ", aRestore1.Count,
                        " bad blocks in ", fi1.FullName,
                        ", because it can't be modified ");
                }

                // Update last write time and saved info for file 2
                if (lNotRestoredSize2 > 0)
                {
                    int nCountErrors = (int)(lNotRestoredSize2 / (new Block().Length));

                    fi2.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24, 23 -
                        (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                    bForceCreateInfo = true;
                }
                else
                {
                    fi2.LastWriteTimeUtc = dtmPrevLastWriteTime;

                    CreateOrUpdateFileChecked(fiSavedInfo2.FullName, iFileSystem, iLogWriter);
                }

            }
            else
            {
                // If no valid saved info, compare blocks directly between files
                System.DateTime dtmPrevLastWriteTime = fi1.LastWriteTimeUtc;

                List<RestoreInfo> aRestore2 = new List<RestoreInfo>();

                long lNotRestoredSize2 = 0;
                long lBadBlocks2 = 0;
                long lBadBlocks1 = 0;

                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        Block oBlock1 = new Block();
                        Block oBlock2 = new Block();

                        // Read all blocks paralelly
                        for (long lIndex = 0; ; ++lIndex)
                        {

                            bool bBlock1Present = false;
                            bool bStillCreateEmptyBlock = false;

                            // Read block from file 1
                            try
                            {
                                int nReadCount = oBlock1.ReadFrom(s1);

                                if (nReadCount > 0)
                                {
                                    // Fill the rest with zeros
                                    oBlock1.EraseFrom(nReadCount);

                                    bBlock1Present = true;
                                }
                            }
                            catch (System.IO.IOException oEx)
                            {
                                ++lBadBlocks1;

                                iLogWriter.WriteLogFormattedLocalized(2,
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile1, oEx.Message, oEx.Message);

                                iLogWriter.WriteLog(true, 2,
                                    "I/O error while reading file \"",
                                    strPathFile1, "\": ", oEx.Message);

                                s1.Seek((lIndex + 1) * oBlock1.Length,
                                    System.IO.SeekOrigin.Begin);

                                // Fill the block with zeros, so dummy block is empty, just in case
                                oBlock1.Erase();
                            }

                            bool bBlock2Present = false;

                            // Read block from file 2
                            try
                            {
                                int nReadCount = oBlock2.ReadFrom(s2);

                                if (nReadCount > 0)
                                {
                                    // Fill the rest with zeros
                                    oBlock2.EraseFrom(nReadCount);

                                    bBlock2Present = true;
                                }
                            }
                            catch (System.IO.IOException oEx)
                            {
                                bStillCreateEmptyBlock = true;

                                ++lBadBlocks2;

                                iLogWriter.WriteLogFormattedLocalized(2,
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, oEx.Message);

                                iLogWriter.WriteLog(true, 2,
                                    "I/O error while reading file \"",
                                    strPathFile2, "\": ", oEx.Message);

                                s2.Seek((lIndex + 1) * oBlock2.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            // Restore blocks in file 2 based on file 1
                            if (bBlock1Present && !bBlock2Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, lIndex * oBlock1.Length, fi1.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                    lIndex * oBlock1.Length, " will be restored from ", fi1.FullName);

                                aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, false));
                            }
                            // Non-recoverable in file 2, but there is still a block in 1
                            // so need to overwrite
                            else if (!bBlock1Present && !bBlock2Present && bStillCreateEmptyBlock)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, lIndex * oBlock1.Length);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " at position ",
                                    lIndex * oBlock1.Length, " is not recoverable and will be filled with a dummy block");

                                aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, true));
                            }

                            // Break if end of both files reached
                            if (s1.Position >= s1.Length && s2.Position >= s2.Length)
                                break;

                            // Check for cancellation
                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }

                    s1.Close();
                }

                // Write restored blocks to file 2
                using (IFile s2 = iFileSystem.Open(
                    strPathFile2, System.IO.FileMode.Open,
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                    {
                        s2.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);

                        int nLengthToWrite = (int)(s2.Length - oRestoreInfo2.Position >= oRestoreInfo2.Data.Length ?
                            oRestoreInfo2.Data.Length :
                            s2.Length - oRestoreInfo2.Position);

                        if (nLengthToWrite > 0)
                            oRestoreInfo2.Data.WriteTo(s2, nLengthToWrite);

                        if (oRestoreInfo2.NotRecoverableArea)
                            lNotRestoredSize2 += nLengthToWrite;

                    }
                }

                // Log summary of bad blocks in file 2
                if (lBadBlocks2 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        lBadBlocks2, fi2.FullName, lNotRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", lBadBlocks2, " bad blocks in ",
                        fi2.FullName, " not restored bytes: ", lNotRestoredSize2);
                }

                // Log summary of bad blocks in file 1 (read-only)
                if (lBadBlocks1 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        lBadBlocks1, fi1.FullName);

                    iLogWriter.WriteLog(true, 0, "There remain ", lBadBlocks1, " bad blocks in ",
                        fi1.FullName, ", because it can't be modified ");
                }

                // Update last write time and saved info for file 2
                if (lNotRestoredSize2 > 0)
                {
                    int nCountErrors = (int)(lNotRestoredSize2 / (new Block().Length));

                    fi2.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24, 23 -
                        (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                    bForceCreateInfo = true;
                }
                else
                {
                    fi2.LastWriteTimeUtc = dtmPrevLastWriteTime;

                    CreateOrUpdateFileChecked(fiSavedInfo2.FullName, iFileSystem, iLogWriter);
                }

            }
        }


        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        public bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            IFileOperations iFileSystem,
            ICancelable iSettings,
            ILogWriter iLogWriter
            )
        {
            return CreateSavedInfo(strPathFile, strPathSavedChkInfoFile, 1, false,
                iFileSystem, iSettings, iLogWriter);
        }


        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <param name="bForceSecondBlocks">Indicates that a second row of blocks must be created</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        public bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            bool bForceSecondBlocks,
            IFileOperations iFileSystem,
            ICancelable iSettings,
            ILogWriter iLogWriter
            )
        {
            /* TODO: this line of code isn't hit by any unit tests */
            return CreateSavedInfo(strPathFile, strPathSavedChkInfoFile, 1, bForceSecondBlocks,
                iFileSystem, iSettings, iLogWriter);
        }

        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <param name="nVersion">The version to save supported values: 0, 1</param>
        /// <param name="bForceSecondBlocks">Indicates that a second row of blocks must be created</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        public bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            int nVersion,
            bool bForceSecondBlocks,
            IFileOperations iFileSystem,
            ICancelable iSettings,
            ILogWriter iLogWriter
            )
        {
            // Check if resources are available
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // Get file info and create SavedInfo object
            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, 
                finfo.LastWriteTimeUtc, bForceSecondBlocks);

            try
            {
                // Open the file for reading with buffering
                using (IFile s =
                    iFileSystem.CreateBufferedStream(iFileSystem.OpenRead(finfo.FullName),
                        (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    Block oBlock = new Block();

                    // Read blocks sequentially from the file
                    for (long lIndex = 0; ; lIndex++)
                    {
                        int nReadCount = 0;
                        if ((nReadCount = oBlock.ReadFrom(s)) == oBlock.Length)
                        {
                            // Analyze block for info collection
                            si.AnalyzeForInfoCollection(oBlock, lIndex);
                        }
                        else
                        {
                            if (nReadCount > 0)
                            {
                                // Fill remaining part with zeros
                                oBlock.EraseFrom(nReadCount);

                                // Analyze last block
                                si.AnalyzeForInfoCollection(oBlock, lIndex);
                            }
                            break;
                        }

                        // Check for cancellation
                        if (iSettings.CancelClicked)
                            throw new OperationCanceledException();
                    }

                    s.Close();
                }
            }
            catch (System.IO.IOException oEx)
            {
                // Log I/O error during reading
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + oEx.Message);

                return false;
            }

            try
            {
                // Ensure target directory exists and set attributes
                IDirectoryInfo di = iFileSystem.GetDirectoryInfo(
                    strPathSavedChkInfoFile.Substring(0,
                    strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                if (!di.Exists)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    di.Create();

                    di = iFileSystem.GetDirectoryInfo(
                        strPathSavedChkInfoFile.Substring(0,
                        strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden |
                        System.IO.FileAttributes.System;
                }

                // Delete existing .chk file if present
                IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedChkInfoFile);

                if (fiSavedInfo.Exists)
                {
                    iFileSystem.Delete(fiSavedInfo);
                }

                // Save calculated info to .chk file (version 0 or 2)
                using (IFile s = iFileSystem.CreateBufferedStream(
                    iFileSystem.Create(strPathSavedChkInfoFile),
                    1024 * 1024))
                {
                    if (nVersion == 0)
                    {
                        si.SaveTo_v0(s);
                    }
                    else
                    {
                        si.SaveTo(s);
                    }
                    s.Close();
                }

                // Set last write time and attributes for .chk file
                fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedChkInfoFile);
                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                fiSavedInfo.Attributes = fiSavedInfo.Attributes
                    | System.IO.FileAttributes.Hidden
                    | System.IO.FileAttributes.System;

                // Create or update confirmation file for checked info
                CreateOrUpdateFileChecked(strPathSavedChkInfoFile,
                    iFileSystem, iLogWriter);

            }
            catch (System.IO.IOException oEx)
            {
                /* TODO: this line of code isn't hit by any unit tests */
                // Log I/O error during writing
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorWritingFile,
                    strPathSavedChkInfoFile, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"",
                    strPathSavedChkInfoFile, "\": " + oEx.Message);

                return false;
            }
            return true;
        }


        //===================================================================================================
        /// <summary>
        /// A safe method to copy files doesn't copy to the target path, because there can be a problem
        /// with USB cable. This will cause the incomplete file to look newer than the complete original.
        /// 
        /// This method, as well as all other methods in this app first copy to .tmp file and then replace
        /// the target file by .tmp
        /// </summary>
        /// <param name="fi">File info of the source file</param>
        /// <param name="strTargetPath">target path</param>
        /// <param name="strReasonEn">The reason of copy for messages</param>
        /// <param name="strReasonTranslated">The reason of copy for messages, localized</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        public void CopyFileSafely(
            IFileInfo fi,
            string strTargetPath,
            string strReasonEn,
            string strReasonTranslated,
            IFileOperations iFileSystem,
            ILogWriter iLogWriter)
        {
            // Check if resources are present
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            string strTempTargetPath = strTargetPath + ".tmp";
            try
            {
                // Copy to target destination as .tmp
                iFileSystem.CopyTo(fi, strTempTargetPath, true);

                // Get informatio about target file exists
                IFileInfo fi2 = iFileSystem.GetFileInfo(strTargetPath);

                // If it exists - delete it
                if (fi2.Exists)
                    iFileSystem.Delete(fi2);

                // Then rename the copied .tmp file as target
                IFileInfo fi2tmp = iFileSystem.GetFileInfo(strTempTargetPath);
                fi2tmp.MoveTo(strTargetPath);

                // Add log message about the copy
                iLogWriter.WriteLogFormattedLocalized(0, 
                    Properties.Resources.FileCopied, fi.FullName,
                    strTargetPath, strReasonTranslated);

                iLogWriter.WriteLog(true, 0, "Copied ", fi.FullName, " to ",
                    strTargetPath, " ", strReasonEn);
            }
            catch
            {
                try
                {
                    // System.Threading.Thread.Sleep(100);
                    IFileInfo fi2 = iFileSystem.GetFileInfo(strTempTargetPath);

                    if (fi2.Exists)
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        iFileSystem.Delete(fi2);
                    }
                }
                catch
                {
                    // ignore additional exceptions
                }

                throw;
            }
        }



        //===================================================================================================
        /// <summary>
        /// This method tests a single file, together with its saved info, if the original file is healthy,
        /// or can be restored using the saved info.
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the file is healthy or can be restored</returns>
        //===================================================================================================
        public bool TestSingleFileHealthyOrCanRepair(
            string strPathFile,
            string strPathSavedInfoFile,
            ref bool bForceCreateInfo,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            )
        {
            return TestSingleFile2(strPathFile, strPathSavedInfoFile,
                ref bForceCreateInfo, true, true, true, false, true,
                iFileSystem, iSettings, iLogWriter);
        }

        //===================================================================================================
        /// <summary>
        /// This method bidirectionylly tests and/or repairs two original files, together with two saved 
        /// info files
        /// </summary>
        /// <param name="strPathFile1">The path of first original file</param>
        /// <param name="strPathFile2">The path of second original file</param>
        /// <param name="strPathSavedInfo1">The path of saved info for first file</param>
        /// <param name="strPathSavedInfo2">The path of saved info for second file</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// var to true</param>
        //===================================================================================================
        public void TestAndRepairTwoFiles(
            string strPathFile1,
            string strPathFile2,
            string strPathSavedInfo1,
            string strPathSavedInfo2,
            ref bool bForceCreateInfo,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter)
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // if it is only a single file, then fallback to the single file method
            if (strPathFile1.Equals(strPathFile2, StringComparison.InvariantCultureIgnoreCase))
            {
                TestAndRepairSingleFile(strPathFile1, strPathSavedInfo1, ref bForceCreateInfo, false, 
                    iFileSystem, iSettings, iLogWriter);
                return;
            }

            IFileInfo fi1 = iFileSystem.GetFileInfo(strPathFile1);
            IFileInfo fi2 = iFileSystem.GetFileInfo(strPathFile2);
            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(strPathSavedInfo1);
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(strPathSavedInfo2);

            SavedInfo si1 = new SavedInfo();
            SavedInfo si2 = new SavedInfo();

            bool bSaveInfo1Present = false;

            if (fiSavedInfo1.Exists &&
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc)
            {
                using (IFile s =
                    iFileSystem.OpenRead(fiSavedInfo1.FullName))
                {
                    si1.ReadFrom(s, false);

                    bSaveInfo1Present = si1.Length == fi1.Length &&
                        Utils.FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc);

                    if (!bSaveInfo1Present)
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        si1 = new SavedInfo();
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        s.Seek(0, System.IO.SeekOrigin.Begin);
                        si2.ReadFrom(s, false);
                    }
                    s.Close();
                }
            }

            if (fiSavedInfo2.Exists &&
                fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc)
            {
                using (IFile s =
                    iFileSystem.OpenRead(fiSavedInfo2.FullName))
                {
                    SavedInfo si3 = new SavedInfo();
                    si3.ReadFrom(s, false);

                    if (si3.Length == fi2.Length &&
                        Utils.FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc))
                    {
                        si2 = si3;
                        if (!bSaveInfo1Present)
                        {
                            s.Seek(0, System.IO.SeekOrigin.Begin);
                            si1.ReadFrom(s, false);
                            bSaveInfo1Present = true;
                        }
                    }
                    else
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        bForceCreateInfo = true;
                    }
                    s.Close();
                }
            }


            if (bSaveInfo1Present)
            {
                System.DateTime dtmPrevLastWriteTime = fi1.LastWriteTimeUtc;

                // improve the available saved infos, if needed 
                si1.ImproveThisAndOther(si2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> oEqualBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> oReadableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> oReadableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> aRestore1 = new List<RestoreInfo>();
                List<RestoreInfo> aRestore2 = new List<RestoreInfo>();

                // now let's try to read the files and compare 
                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        si1.StartRestore();
                        si2.StartRestore();

                        Block oBlock1 = new Block();
                        Block oBlock2 = new Block();

                        for (long lIndex = 0; ; ++lIndex)
                        {
                            //for (int i = oBlock1.Length - 1; i >= 0; --i)
                            //{
                            //    oBlock1[i] = 0;
                            //    oBlock2[i] = 0;
                            //}

                            bool bBlock1Present = false;
                            bool bBlock1Ok = false;

                            try
                            {
                                int nRead = 0;

                                if ((nRead = oBlock1.ReadFrom(s1)) == oBlock1.Length)
                                {
                                    bBlock1Ok = si1.AnalyzeForTestOrRestore(oBlock1, lIndex);
                                    oReadableBlocks1[lIndex] = true;
                                    bBlock1Present = true;
                                }
                                else
                                {
                                    if (nRead > 0)
                                    {
                                        // fill the rest with zeros
                                        while (nRead < oBlock1.Length)
                                            oBlock1[nRead++] = 0;

                                        bBlock1Ok = si1.AnalyzeForTestOrRestore(oBlock1, lIndex);
                                        oReadableBlocks1[lIndex] = true;
                                        bBlock1Present = true;
                                    }
                                }

                                if (!bBlock1Ok)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    iLogWriter.WriteLogFormattedLocalized(2,
                                        Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        strPathFile1, lIndex * oBlock1.Length);

                                    iLogWriter.WriteLog(true, 2, strPathFile1, 
                                        ": checksum of block at offset ",
                                        lIndex * oBlock1.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException oEx)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, 
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile1, oEx.Message);

                                iLogWriter.WriteLog(true, 2, 
                                    "I/O exception while reading file \"",
                                    strPathFile1, "\": ", oEx.Message);

                                s1.Seek((lIndex + 1) * oBlock1.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            bool bBlock2Present = false;
                            bool bBlock2Ok = false;

                            try
                            {
                                int nRead = 0;
                                if ((nRead = oBlock2.ReadFrom(s2)) == oBlock2.Length)
                                {
                                    bBlock2Ok = si2.AnalyzeForTestOrRestore(oBlock2, lIndex);
                                    oReadableBlocks2[lIndex] = true;
                                    bBlock2Present = true;
                                }
                                else
                                {
                                    if (nRead > 0)
                                    {
                                        // fill the rest with zeros
                                        while (nRead < oBlock2.Length)
                                            oBlock2[nRead++] = 0;

                                        bBlock2Ok = si2.AnalyzeForTestOrRestore(oBlock2, lIndex);
                                        oReadableBlocks2[lIndex] = true;
                                        bBlock2Present = true;
                                    }
                                }

                                if (!bBlock2Ok)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    iLogWriter.WriteLogFormattedLocalized(2,
                                        Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        strPathFile2, lIndex * oBlock2.Length);

                                    iLogWriter.WriteLog(true, 2, strPathFile2, 
                                        ": checksum of block at offset ",
                                        lIndex * oBlock2.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException oEx)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, 
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, oEx.Message);

                                iLogWriter.WriteLog(true, 2, 
                                    "I/O exception while reading file \"",
                                    strPathFile2, "\": ", oEx.Message);

                                s2.Seek((lIndex + 1) * oBlock2.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            if (bBlock1Present && !bBlock2Present)
                            {
                                if (si2.AnalyzeForTestOrRestore(oBlock1, lIndex))
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, lIndex * oBlock1.Length, fi1.FullName);

                                    iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                        " position ", lIndex * oBlock1.Length,
                                        " will be restored from ", fi1.FullName);

                                    aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, false));
                                }
                            }
                            else if (bBlock2Present && !bBlock1Present)
                            {
                                if (si1.AnalyzeForTestOrRestore(oBlock2, lIndex))
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi1.FullName, lIndex * oBlock1.Length, fi2.FullName);

                                    iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                        " position ", lIndex * oBlock1.Length,
                                        " will be restored from ", fi2.FullName);

                                    aRestore1.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock2, false));
                                }
                            }
                            else
                            {
                                if (bBlock2Present && !bBlock1Ok)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    if (si1.AnalyzeForTestOrRestore(oBlock2, lIndex))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi1.FullName, lIndex * oBlock1.Length, fi2.FullName);

                                        iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                            " position ", lIndex * oBlock1.Length,
                                            " will be restored from ", fi2.FullName);

                                        aRestore1.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock2, false));
                                    }
                                }

                                if (bBlock1Present && !bBlock2Ok)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    if (si2.AnalyzeForTestOrRestore(oBlock1, lIndex))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, lIndex * oBlock1.Length, fi1.FullName);

                                        iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                            " position ", lIndex * oBlock1.Length,
                                            " will be restored from ", fi1.FullName);

                                        aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, false));
                                    }
                                }
                            }

                            if (bBlock1Present && bBlock2Present)
                            {
                                // if both blocks are present we'll compare their contents
                                // equal blocks have higher priority compared to their checksums and saved infos
                                bool bDifferent = false;

                                for (int i = oBlock1.Length - 1; i >= 0; --i)
                                    if (oBlock1[i] != oBlock2[i])
                                    {
                                        /* TODO: this line of code isn't hit by any unit tests */
                                        bDifferent = true;
                                        break;
                                    }

                                if (!bDifferent)
                                {
                                    oEqualBlocks[lIndex] = true;
                                }
                            }

                            if (s1.Position >= s1.Length && s2.Position >= s2.Length)
                                break;

                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }

                    s1.Close();
                }

                bool bRepeat;
                SortedDictionary<long, RestoreInfo> oRestoreDic1 = new SortedDictionary<long, RestoreInfo>();
                SortedDictionary<long, RestoreInfo> oRestoreDic2 = new SortedDictionary<long, RestoreInfo>();
                long lNotRestoredSize1;
                long lNotRestoredSize2;
                do
                {
                    lNotRestoredSize1 = 0;
                    lNotRestoredSize2 = 0;

                    bRepeat = false;

                    foreach (RestoreInfo ri1 in si1.EndRestore(out lNotRestoredSize1, fiSavedInfo1.FullName, iLogWriter))
                    {
                        if (!oRestoreDic1.ContainsKey(ri1.Position) ||
                            oRestoreDic1[ri1.Position].NotRecoverableArea)
                        {
                            oRestoreDic1[ri1.Position] = ri1;
                        }

                    }

                    foreach (RestoreInfo ri2 in si2.EndRestore(out lNotRestoredSize2, fiSavedInfo2.FullName, iLogWriter))
                    {
                        if (!oRestoreDic2.ContainsKey(ri2.Position) ||
                            oRestoreDic2[ri2.Position].NotRecoverableArea)
                        {
                            oRestoreDic2[ri2.Position] = ri2;
                        }
                    }


                    // let's apply improvements of one file 
                    // to the list of the other, whenever possible
                    foreach (RestoreInfo oRestoreInfo1 in oRestoreDic1.Values)
                    {
                        if (oRestoreDic2.ContainsKey(oRestoreInfo1.Position))
                        {
                            RestoreInfo oRestoreInfo2 = oRestoreDic2[oRestoreInfo1.Position];

                            if (oRestoreInfo2.NotRecoverableArea && !oRestoreInfo1.NotRecoverableArea)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, oRestoreInfo2.Position, fi1.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                    " position ", oRestoreInfo2.Position,
                                    " will be restored from ", fi1.FullName);

                                oRestoreInfo2.Data = oRestoreInfo1.Data;
                                oRestoreInfo2.NotRecoverableArea = false;

                                si2.AnalyzeForTestOrRestore(oRestoreInfo2.Data,
                                    oRestoreInfo2.Position / oRestoreInfo2.Data.Length);

                                bRepeat = true;
                            }
                        }
                    }

                    foreach (RestoreInfo oRestoreInfo2 in oRestoreDic2.Values)
                    {
                        if (oRestoreDic1.ContainsKey(oRestoreInfo2.Position))
                        {
                            RestoreInfo oRestoreInfo1 = oRestoreDic1[oRestoreInfo2.Position];

                            if (oRestoreInfo1.NotRecoverableArea && !oRestoreInfo2.NotRecoverableArea)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi1.FullName, oRestoreInfo1.Position, fi2.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                    " position ", oRestoreInfo1.Position,
                                    " will be restored from ", fi2.FullName);

                                oRestoreInfo1.Data = oRestoreInfo2.Data;
                                oRestoreInfo1.NotRecoverableArea = false;

                                si1.AnalyzeForTestOrRestore(oRestoreInfo1.Data,
                                    oRestoreInfo1.Position / oRestoreInfo1.Data.Length);

                                bRepeat = true;
                            }
                        }
                    }

                    if (bRepeat)
                    {
                        iLogWriter.WriteLog(true, 1, "Found some cross-restores between files, repeating the process");
                    }

                } while (bRepeat);

                lNotRestoredSize1 = 0;
                lNotRestoredSize2 = 0;

                // transfer to arrays
                foreach (var oPair in oRestoreDic1)
                {
                    aRestore1.Add(oPair.Value);
                }

                foreach (var oPair in oRestoreDic2)
                {
                    aRestore2.Add(oPair.Value);
                }

                // now we've got the list of improvements for both files
                using (IFile s1 = iFileSystem.Open(
                    strPathFile1, System.IO.FileMode.Open,
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {

                    using (IFile s2 = iFileSystem.Open(
                        strPathFile2, System.IO.FileMode.Open,
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {

                        // let's apply the definitive improvements
                        foreach (RestoreInfo oRestoreInfo1 in aRestore1)
                        {
                            if (oRestoreInfo1.NotRecoverableArea ||
                                (iSettings.PreferPhysicalCopies &&
                                    oEqualBlocks.ContainsKey(oRestoreInfo1.Position / oRestoreInfo1.Data.Length)))
                                ;// bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                    oRestoreInfo1.Position, fi1.FullName);

                                iLogWriter.WriteLog(true, 1, "Recovering block of ", fi1.FullName,
                                    " at position ", oRestoreInfo1.Position);

                                s1.Seek(oRestoreInfo1.Position, System.IO.SeekOrigin.Begin);

                                int nLengthToWrite = (int)(si1.Length - oRestoreInfo1.Position >= oRestoreInfo1.Data.Length ?
                                    oRestoreInfo1.Data.Length :
                                    si1.Length - oRestoreInfo1.Position);

                                if (nLengthToWrite > 0)
                                    oRestoreInfo1.Data.WriteTo(s1, nLengthToWrite);

                                // we assume the block is readbable now
                                oReadableBlocks1[oRestoreInfo1.Position / oRestoreInfo1.Data.Length] = true;
                            }
                        }


                        foreach (RestoreInfo oRestoredInfo2 in aRestore2)
                        {
                            if (oRestoredInfo2.NotRecoverableArea ||
                                (iSettings.PreferPhysicalCopies &&
                                    oEqualBlocks.ContainsKey(oRestoredInfo2.Position / oRestoredInfo2.Data.Length)))
                                ; // bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                    oRestoredInfo2.Position, fi2.FullName);

                                iLogWriter.WriteLog(true, 1, "Recovering block of ", fi2.FullName,
                                    " at position ", oRestoredInfo2.Position);

                                s2.Seek(oRestoredInfo2.Position, System.IO.SeekOrigin.Begin);

                                int nLengthToWrite = (int)(si2.Length - oRestoredInfo2.Position >= oRestoredInfo2.Data.Length ?
                                    oRestoredInfo2.Data.Length :
                                    si2.Length - oRestoredInfo2.Position);

                                if (nLengthToWrite > 0)
                                    oRestoredInfo2.Data.WriteTo(s2, nLengthToWrite);

                                // we assume the block is readbable now
                                oReadableBlocks2[oRestoredInfo2.Position / oRestoredInfo2.Data.Length] = true;
                            }
                        }


                        // let's try to copy non-recoverable 
                        // blocks from one file to another, whenever possible
                        foreach (RestoreInfo oRestoreInfo1 in aRestore1)
                        {
                            if (oRestoreInfo1.NotRecoverableArea && 
                                !oEqualBlocks.ContainsKey(oRestoreInfo1.Position / oRestoreInfo1.Data.Length) &&
                                oReadableBlocks2.ContainsKey(oRestoreInfo1.Position / oRestoreInfo1.Data.Length) &&
                                !oReadableBlocks1.ContainsKey(oRestoreInfo1.Position / oRestoreInfo1.Data.Length))
                            {
                                /* TODO: this line of code isn't hit by any unit tests */
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi1.FullName, oRestoreInfo1.Position, fi2.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " position ",
                                    oRestoreInfo1.Position, " will be copied from ",
                                    fi2.FullName, " even if checksum indicates the block is wrong");

                                s1.Seek(oRestoreInfo1.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(oRestoreInfo1.Position, System.IO.SeekOrigin.Begin);

                                Block oTempBlock = new Block();
                                int nLength = oTempBlock.ReadFrom(s2);
                                oTempBlock.WriteTo(s1, nLength);

                                oReadableBlocks1[oRestoreInfo1.Position / oRestoreInfo1.Data.Length] = true;
                            }
                        }


                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            if (oRestoreInfo2.NotRecoverableArea && 
                                !oEqualBlocks.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                oReadableBlocks1.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                !oReadableBlocks2.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length))

                            {
                                /* TODO: this line of code isn't hit by any unit tests */
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi2.FullName, oRestoreInfo2.Position, fi1.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                    oRestoreInfo2.Position, " will be copied from ", fi1.FullName,
                                    " even if checksum indicates the block is wrong");

                                s1.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);

                                Block oTempBlock = new Block();
                                int nLength = oTempBlock.ReadFrom(s1);
                                oTempBlock.WriteTo(s2, nLength);

                                oReadableBlocks2[oRestoreInfo2.Position / oRestoreInfo2.Data.Length] = true;
                            }
                        }

                        // after all fill non-readable blocks with zeroes
                        foreach (RestoreInfo oRestoreInfo1 in aRestore1)
                        {
                            if (oRestoreInfo1.NotRecoverableArea && 
                                !oEqualBlocks.ContainsKey(oRestoreInfo1.Position / oRestoreInfo1.Data.Length) &&
                                !oReadableBlocks1.ContainsKey(oRestoreInfo1.Position / oRestoreInfo1.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi1.FullName, oRestoreInfo1.Position);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " position ",
                                    oRestoreInfo1.Position, " is not recoverable and will be filled with a dummy");

                                s1.Seek(oRestoreInfo1.Position, System.IO.SeekOrigin.Begin);

                                int nLengthToWrite = (int)(si1.Length - oRestoreInfo1.Position >= oRestoreInfo1.Data.Length ?
                                    oRestoreInfo1.Data.Length :
                                    si1.Length - oRestoreInfo1.Position);

                                if (nLengthToWrite > 0)
                                    oRestoreInfo1.Data.WriteTo(s1, nLengthToWrite);
                            }
                        }


                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            if (oRestoreInfo2.NotRecoverableArea &&
                                !oEqualBlocks.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                !oReadableBlocks2.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, oRestoreInfo2.Position);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                    " position ", oRestoreInfo2.Position,
                                    " is not recoverable and will be filled with a dummy");

                                s2.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);

                                int nLengthToWrite = (int)(si2.Length - oRestoreInfo2.Position >= oRestoreInfo2.Data.Length ?
                                    oRestoreInfo2.Data.Length :
                                    si2.Length - oRestoreInfo2.Position);

                                if (nLengthToWrite > 0)
                                    oRestoreInfo2.Data.WriteTo(s2, nLengthToWrite);
                            }
                        }

                        s2.Close();
                    }

                    s1.Close();
                }

                if (aRestore1.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        aRestore1.Count, fi1.FullName, lNotRestoredSize1);

                    iLogWriter.WriteLog(true, 0, "There were ", aRestore1.Count,
                        " bad blocks in ", fi1.FullName,
                        " not restored bytes: ", lNotRestoredSize1);
                }

                if (aRestore2.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        aRestore2.Count, fi2.FullName, lNotRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", aRestore2.Count,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", lNotRestoredSize2);
                }

                fi1.LastWriteTimeUtc = dtmPrevLastWriteTime;
                fi2.LastWriteTimeUtc = dtmPrevLastWriteTime;

                if (lNotRestoredSize1 == 0 && aRestore1.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo1,
                        iFileSystem, iLogWriter);
                }

                if (lNotRestoredSize2 == 0 && aRestore2.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo2,
                        iFileSystem, iLogWriter);
                }

            }
            else
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // let's read both copies of the file 
                // that obviously are both present, without saved info
                List<RestoreInfo> aRestore1 = new List<RestoreInfo>();
                List<RestoreInfo> aRestore2 = new List<RestoreInfo>();

                // now let's try to read the files and compare 
                long lNotRestoredSize1 = 0;
                long lNotRestoredSize2 = 0;
                long lBadBlocks1 = 0;
                long lBadBlocks2 = 0;

                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        Block oBlock1 = new Block();
                        Block oBlock2 = new Block();

                        for (long lIndex = 0; ; ++lIndex)
                        {
                            if (s1.Position >= s1.Length && s2.Position >= s2.Length)
                                break;

                            bool bBlock1Present = false;
                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = oBlock1.ReadFrom(s1)) < oBlock1.Length)
                                {
                                    for (int i = oBlock1.Length - 1; i >= nReadCount; --i)
                                        oBlock1[i] = 0;
                                }

                                bBlock1Present = nReadCount>0;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2,
                                    Properties.Resources.IOErrorWritingFile,
                                    strPathFile1, oEx.Message);

                                iLogWriter.WriteLog(true, 2, "I/O exception while reading file \"",
                                    strPathFile1, "\": ", oEx.Message);

                                s1.Seek((lIndex + 1) * oBlock1.Length,
                                    System.IO.SeekOrigin.Begin);

                                ++lBadBlocks1;
                            }

                            bool bBlock2Present = false;

                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = oBlock2.ReadFrom(s2)) < oBlock2.Length)
                                {
                                    for (int i = oBlock2.Length - 1; i >= nReadCount; --i)
                                        oBlock2[i] = 0;
                                }

                                bBlock2Present = nReadCount>0;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, 
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, oEx.Message);

                                iLogWriter.WriteLog(true, 2, 
                                    "I/O exception while reading file \"",
                                    strPathFile2, "\": ", oEx.Message);

                                s2.Seek((lIndex + 1) * oBlock2.Length,
                                    System.IO.SeekOrigin.Begin);

                                ++lBadBlocks2;
                            }

                            if (bBlock1Present && !bBlock2Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, lIndex * oBlock1.Length, fi1.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                    " position ", lIndex * oBlock1.Length,
                                    " will be restored from ", fi1.FullName);


                                aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, false));
                            }
                            else if (bBlock2Present && !bBlock1Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi1.FullName, lIndex * oBlock1.Length, fi2.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                    " position ", lIndex * oBlock1.Length,
                                    " will be restored from ", fi2.FullName);

                                aRestore1.Add(new RestoreInfo(lIndex * oBlock2.Length, oBlock2, false));
                            }
                            else
                            if (!bBlock1Present && !bBlock2Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlocksOfAndAtPositionNonRecoverableFillDummy,
                                    fi1.FullName, fi2.FullName, lIndex * oBlock1.Length);

                                iLogWriter.WriteLog(true, 1, "Blocks of ", fi1.FullName,
                                    " and ", fi2.FullName, " at position ",
                                    lIndex * oBlock1.Length,
                                    " are not recoverable and will be filled with a dummy block");

                                for (int i=oBlock1.Length-1;i>=0;--i)
                                {
                                    oBlock1[i] = oBlock2[i] = 0;
                                }

                                aRestore1.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, true));
                                aRestore2.Add(new RestoreInfo(lIndex * oBlock2.Length, oBlock2, true));

                                lNotRestoredSize1 += oBlock1.Length;
                                lNotRestoredSize2 += oBlock2.Length;
                            }


                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }

                    s1.Close();
                }


                // now we've got the list of improvements for both files
                try
                {
                    using (IFile s1 = iFileSystem.Open(
                        strPathFile1, System.IO.FileMode.Open,
                        System.IO.FileAccess.Write, System.IO.FileShare.Read))
                    {
                        foreach (RestoreInfo oRestoreInfo1 in aRestore1)
                        {
                            s1.Seek(oRestoreInfo1.Position, System.IO.SeekOrigin.Begin);

                            int nLengthToWrite = (int)(s1.Length - oRestoreInfo1.Position >= oRestoreInfo1.Data.Length ?
                                oRestoreInfo1.Data.Length :
                                s1.Length - oRestoreInfo1.Position);

                            if (nLengthToWrite > 0)
                                oRestoreInfo1.Data.WriteTo(s1, nLengthToWrite);
                        }
                    }
                }
                finally
                {
                    if (lNotRestoredSize1 > 0)
                    {
                        int nCountErrors = (int)(lNotRestoredSize1 / (new Block().Length));

                        fi1.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24, 23 -
                            (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                        bForceCreateInfo = true;
                    }
                    else
                    {
                        fi1.LastWriteTimeUtc = prevLastWriteTime;
                    }
                }

                if (lBadBlocks1 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        lBadBlocks1, fi1.FullName, lNotRestoredSize1);

                    iLogWriter.WriteLog(true, 0, "There were ", lBadBlocks1,
                        " bad blocks in ", fi1.FullName,
                        " not restored bytes: ", lNotRestoredSize1);
                }

                try
                {
                    using (IFile s2 = iFileSystem.Open(
                        strPathFile2, System.IO.FileMode.Open,
                        System.IO.FileAccess.Write, System.IO.FileShare.Read))
                    {
                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            s2.Seek(oRestoreInfo2.Position, System.IO.SeekOrigin.Begin);

                            int lengthToWrite = (int)(s2.Length - oRestoreInfo2.Position >= oRestoreInfo2.Data.Length ?
                                oRestoreInfo2.Data.Length :
                                s2.Length - oRestoreInfo2.Position);

                            if (lengthToWrite > 0)
                                oRestoreInfo2.Data.WriteTo(s2, lengthToWrite);
                        }
                    }
                } finally
                {
                    if (lNotRestoredSize2 > 0)
                    {
                        int nCountErrors = (int)(lNotRestoredSize2 / (new Block().Length));

                        fi2.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24, 23 -
                            (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                        bForceCreateInfo = true;
                    }
                    else
                    {
                        fi2.LastWriteTimeUtc = prevLastWriteTime;
                    }
                }


                if (lBadBlocks2 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        lBadBlocks2, fi2.FullName, lNotRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", lBadBlocks2,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", lNotRestoredSize2);
                }

                if (lNotRestoredSize1 == 0 && aRestore1.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo1,
                        iFileSystem, iLogWriter);
                }

                if (lNotRestoredSize2 == 0 && aRestore2.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo2,
                        iFileSystem, iLogWriter);
                }

            }
        }


        //===================================================================================================
        /// <summary>
        /// This method combines copying of a file with creation of SavedInfo (.chk) file. So there is no
        /// need to read a big data file twice.
        /// </summary>
        /// <param name="strPathFile">The source path for copy</param>
        /// <param name="strTargetPath">The target path for copy</param>
        /// <param name="strPathSavedInfoFile">The target path for saved info</param>
        /// <param name="strReasonEn">The reason of copy for messages</param>
        /// <param name="strReasonTranslated">The reason of copy for messages, localized</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iCancelable">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        public bool CreateSavedInfoAndCopy(
            string strPathFile,
            string strPathSavedInfoFile,
            string strTargetPath,
            string strReasonEn,
            string strReasonTranslated,
            IFileOperations iFileSystem,
            ICancelable iCancelable,
            ILogWriter iLogWriter)
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // if by any means we get the same file, then just create saved info
            if (strPathFile.Equals(strTargetPath, StringComparison.InvariantCultureIgnoreCase))
            {
                /* TODO: this line of code isn't hit by any unit tests */
                return !CreateSavedInfo(strTargetPath, strPathSavedInfoFile, iFileSystem, iCancelable, iLogWriter);
            };

            string strPathTmpFileCopy = strTargetPath + ".tmp";

            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, finfo.LastWriteTimeUtc, false);

            try
            {
                using (IFile s =
                    iFileSystem.CreateBufferedStream(iFileSystem.OpenRead(finfo.FullName),
                        (int)Math.Min(finfo.Length + 1, 16 * 1024 * 1024)))
                {
                    try
                    {
                        using (IFile s2 =
                            iFileSystem.CreateBufferedStream(iFileSystem.Create(strPathTmpFileCopy),
                            (int)Math.Min(finfo.Length + 1, 16 * 1024 * 1024)))
                        {
                            Block oBlock = new Block();

                            for (long lIndex = 0; ; lIndex++)
                            {
                                int nReadCount = 0;
                                if ((nReadCount = oBlock.ReadFrom(s)) == oBlock.Length)
                                {
                                    oBlock.WriteTo(s2);
                                    si.AnalyzeForInfoCollection(oBlock, lIndex);
                                }
                                else
                                {
                                    if (nReadCount > 0)
                                    {
                                        for (int i = oBlock.Length - 1; i >= nReadCount; --i)
                                            oBlock[i] = 0;

                                        oBlock.WriteTo(s2, nReadCount);
                                        si.AnalyzeForInfoCollection(oBlock, lIndex);
                                    }
                                    break;
                                }

                                if (iCancelable.CancelClicked)
                                    throw new OperationCanceledException();
                            }


                            s2.Close();

                            IFileInfo fi2tmp = iFileSystem.GetFileInfo(strPathTmpFileCopy);
                            fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                            IFileInfo fi2 = iFileSystem.GetFileInfo(strTargetPath);

                            if (fi2.Exists)
                            {
                                iFileSystem.Delete(fi2);
                            }

                            fi2tmp.MoveTo(strTargetPath);

                            iLogWriter.WriteLogFormattedLocalized(0, 
                                Properties.Resources.CopiedFromToReason,
                                strPathFile, strTargetPath, strReasonTranslated);

                            iLogWriter.WriteLog(true, 0, "Copied ", strPathFile, " to ",
                                strTargetPath, " ", strReasonEn);
                        }
                    }
                    catch
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(100);

                            IFileInfo fiTmpCopy = iFileSystem.GetFileInfo(strPathTmpFileCopy);

                            if (fiTmpCopy.Exists)
                                iFileSystem.Delete(fiTmpCopy);
                        }
                        catch
                        {
                            // ignore
                        }
                        throw;
                    }

                    s.Close();
                }
            }
            catch (System.IO.IOException oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(0, 
                    Properties.Resources.WarningIOErrorWhileCopyingToReason,
                    finfo.FullName, strTargetPath, oEx.Message);

                iLogWriter.WriteLog(true, 0, "Warning: I/O Error while copying file: \"",
                    finfo.FullName, "\" to \"", strTargetPath, "\": " + oEx.Message);

                return false;
            }

            try
            {
                IDirectoryInfo di = iFileSystem.GetDirectoryInfo(
                    strPathSavedInfoFile.Substring(0,
                    strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                if (!di.Exists)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    di.Create();
                    di = iFileSystem.GetDirectoryInfo(
                        strPathSavedInfoFile.Substring(0,
                        strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden
                        | System.IO.FileAttributes.System;
                }
                using (IFile s = iFileSystem.Create(strPathSavedInfoFile))
                {
                    si.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile);

                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                fiSavedInfo.Attributes =  fiSavedInfo.Attributes | 
                    System.IO.FileAttributes.Hidden |
                    System.IO.FileAttributes.System;

            }
            catch (System.IO.IOException oEx)
            {
                /* TODO: this line of code isn't hit by any unit tests */
                iLogWriter.WriteLogFormattedLocalized(0, 
                    Properties.Resources.IOErrorWritingFile,
                    strPathSavedInfoFile, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"", 
                    strPathSavedInfoFile, "\": " + oEx.Message);

                return false;
            }

            // we just created the file, so assume we checked everything,
            // no need to double-check immediately
            CreateOrUpdateFileChecked(strPathSavedInfoFile,
                iFileSystem, iLogWriter);

            return true;
        }



        //===================================================================================================
        /// <summary>
        /// This method combines copying of a file with creation of SavedInfo (.chk) file. So there is no
        /// need to read a big data file twice.
        /// </summary>
        /// <param name="strPathFile">The source path for copy</param>
        /// <param name="strTargetPath">The target path for copy</param>
        /// <param name="strPathSavedInfoFile">The target path for saved info</param>
        /// <param name="strPathSavedInfoFile2">The target path for second saved info</param>
        /// <param name="strReasonEn">The reason of copy for messages</param>
        /// <param name="strReasonTranslated">The reason of copy for messages, localized</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iCancelable">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        public bool Create2SavedInfosAndCopy(
            string strPathFile,
            string strPathSavedInfoFile,
            string strTargetPath,
            string strPathSavedInfoFile2,
            string strReasonEn,
            string strReasonTranslated,
            IFileOperations iFileSystem,
            ICancelable iCancelable,
            ILogWriter iLogWriter)
        {
            
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // if by any means we get the same file, then just create saved info
            if (strPathFile.Equals(strTargetPath, StringComparison.InvariantCultureIgnoreCase))
            {
                /* TODO: this line of code isn't hit by any unit tests */
                return !CreateSavedInfo(strTargetPath, strPathSavedInfoFile, iFileSystem, iCancelable, iLogWriter);
            }

            string strPathTmpFileCopy = strTargetPath + ".tmp";

            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si1, si2;
            SavedInfo.Create2DifferentSavedInfos(out si1, out si2, finfo.Length, finfo.LastWriteTimeUtc);

            try
            {
                using (IFile s =
                    iFileSystem.CreateBufferedStream(iFileSystem.OpenRead(finfo.FullName),
                        (int)Math.Min(finfo.Length + 1, 16 * 1024 * 1024)))
                {
                    try
                    {
                        using (IFile s2 =
                            iFileSystem.CreateBufferedStream(iFileSystem.Create(strPathTmpFileCopy),
                            (int)Math.Min(finfo.Length + 1, 16 * 1024 * 1024)))
                        {
                            Block oBlock = new Block();

                            for (long lIndex = 0; ; lIndex++)
                            {
                                int nReadCount = 0;
                                if ((nReadCount = oBlock.ReadFrom(s)) == oBlock.Length)
                                {
                                    oBlock.WriteTo(s2);
                                    si1.AnalyzeForInfoCollection(oBlock, lIndex);
                                    si2.AnalyzeForInfoCollection(oBlock, lIndex);
                                }
                                else
                                {
                                    if (nReadCount > 0)
                                    {
                                        for (int i = oBlock.Length - 1; i >= nReadCount; --i)
                                            oBlock[i] = 0;

                                        oBlock.WriteTo(s2, nReadCount);
                                        si1.AnalyzeForInfoCollection(oBlock, lIndex);
                                        si2.AnalyzeForInfoCollection(oBlock, lIndex);
                                    }
                                    break;
                                }

                                if (iCancelable.CancelClicked)
                                    throw new OperationCanceledException();
                            }


                            s2.Close();

                            IFileInfo fi2tmp = iFileSystem.GetFileInfo(strPathTmpFileCopy);
                            fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                            IFileInfo fi2 = iFileSystem.GetFileInfo(strTargetPath);

                            if (fi2.Exists)
                                iFileSystem.Delete(fi2);

                            fi2tmp.MoveTo(strTargetPath);

                            iLogWriter.WriteLogFormattedLocalized(0,
                                Properties.Resources.CopiedFromToReason,
                                strPathFile, strTargetPath, strReasonTranslated);

                            iLogWriter.WriteLog(true, 0, "Copied ", strPathFile, " to ",
                                strTargetPath, " ", strReasonEn);
                        }
                    }
                    catch
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(100);

                            IFileInfo fiTmpCopy = iFileSystem.GetFileInfo(strPathTmpFileCopy);

                            if (fiTmpCopy.Exists)
                                iFileSystem.Delete(fiTmpCopy);
                        }
                        catch
                        {
                            // ignore
                        }
                        throw;
                    }

                    s.Close();
                }
            }
            catch (System.IO.IOException oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.WarningIOErrorWhileCopyingToReason,
                    finfo.FullName, strTargetPath, oEx.Message);

                iLogWriter.WriteLog(true, 0, "Warning: I/O Error while copying file: \"",
                    finfo.FullName, "\" to \"", strTargetPath, "\": " + oEx.Message);

                return false;
            }

            // saving first saved info
            try
            {
                IDirectoryInfo di = iFileSystem.GetDirectoryInfo(
                    strPathSavedInfoFile.Substring(0,
                    strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                if (!di.Exists)
                {
                    di.Create();
                    di = iFileSystem.GetDirectoryInfo(
                        strPathSavedInfoFile.Substring(0,
                        strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden
                        | System.IO.FileAttributes.System;
                }

                using (IFile s = iFileSystem.Create(strPathSavedInfoFile))
                {
                    si1.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile);

                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                fiSavedInfo.Attributes = fiSavedInfo.Attributes |
                    System.IO.FileAttributes.Hidden |
                    System.IO.FileAttributes.System;
            }
            catch (System.IO.IOException oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorWritingFile,
                    strPathSavedInfoFile, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"",
                    strPathSavedInfoFile, "\": " + oEx.Message);

                return false;
            }


            // saving second saved info
            try
            {
                IDirectoryInfo di = iFileSystem.GetDirectoryInfo(
                    strPathSavedInfoFile2.Substring(0,
                    strPathSavedInfoFile2.LastIndexOfAny(new char[] { '\\', '/' })));

                if (!di.Exists)
                {
                    di.Create();
                    di = iFileSystem.GetDirectoryInfo(
                        strPathSavedInfoFile2.Substring(0,
                        strPathSavedInfoFile2.LastIndexOfAny(new char[] { '\\', '/' })));

                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden
                        | System.IO.FileAttributes.System;
                }

                using (IFile s = iFileSystem.Create(strPathSavedInfoFile2))
                {
                    si2.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile2);

                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                fiSavedInfo.Attributes = fiSavedInfo.Attributes |
                    System.IO.FileAttributes.Hidden |
                    System.IO.FileAttributes.System;
            }
            catch (System.IO.IOException oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorWritingFile,
                    strPathSavedInfoFile2, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"",
                    strPathSavedInfoFile2, "\": " + oEx.Message);

                return false;
            }

            // we just created the file, so assume we checked everything,
            // no need to double-check immediately
            CreateOrUpdateFileChecked(strPathSavedInfoFile,
                iFileSystem, iLogWriter);
            CreateOrUpdateFileChecked(strPathSavedInfoFile2,
                iFileSystem, iLogWriter);

            return true;
        }

        //===================================================================================================
        /// <summary>
        /// This method combines copying of a file with creation of SavedInfo (.chk) file. So there is no
        /// need to read a big data file twice.
        /// </summary>
        /// <param name="strPathFile">The source path for copy</param>
        /// <param name="strPathSavedInfoFile">The target path for saved info</param>
        /// <param name="strPathSavedInfoFile2">The target path for second saved info</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iCancelable">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        public bool Create2SavedInfos(
            string strPathFile,
            string strPathSavedInfoFile,
            string strPathSavedInfoFile2,
            IFileOperations iFileSystem,
            ICancelable iCancelable,
            ILogWriter iLogWriter)
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // if by any means we get the same file, then just create saved info
            if (strPathSavedInfoFile2.Equals(strPathSavedInfoFile, StringComparison.InvariantCultureIgnoreCase))
            {
                /* TODO: this line of code isn't hit by any unit tests */
                return !CreateSavedInfo(strPathFile, strPathSavedInfoFile, iFileSystem, iCancelable, iLogWriter);
            }

            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si1, si2;
            SavedInfo.Create2DifferentSavedInfos(out si1, out si2, finfo.Length, finfo.LastWriteTimeUtc);

            try
            {
                using (IFile s =
                    iFileSystem.CreateBufferedStream(iFileSystem.OpenRead(finfo.FullName),
                        (int)Math.Min(finfo.Length + 1, 16 * 1024 * 1024)))
                {

                    Block oBlock = new Block();

                    for (long lIndex = 0; ; lIndex++)
                    {
                        int nReadCount = 0;
                        if ((nReadCount = oBlock.ReadFrom(s)) == oBlock.Length)
                        {
                            si1.AnalyzeForInfoCollection(oBlock, lIndex);
                            si2.AnalyzeForInfoCollection(oBlock, lIndex);
                        }
                        else
                        {
                            if (nReadCount > 0)
                            {
                                for (int i = oBlock.Length - 1; i >= nReadCount; --i)
                                    oBlock[i] = 0;

                                si1.AnalyzeForInfoCollection(oBlock, lIndex);
                                si2.AnalyzeForInfoCollection(oBlock, lIndex);
                            }
                            break;
                        }

                        if (iCancelable.CancelClicked)
                            throw new OperationCanceledException();
                    }

                    s.Close();
                }
            }
            catch (System.IO.IOException oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + oEx.Message);

                return false;
            }

            // saving first saved info
            try
            {
                IDirectoryInfo di = iFileSystem.GetDirectoryInfo(
                    strPathSavedInfoFile.Substring(0,
                    strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                if (!di.Exists)
                {
                    di.Create();
                    di = iFileSystem.GetDirectoryInfo(
                        strPathSavedInfoFile.Substring(0,
                        strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden
                        | System.IO.FileAttributes.System;
                }

                using (IFile s = iFileSystem.Create(strPathSavedInfoFile))
                {
                    si1.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile);

                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                fiSavedInfo.Attributes = fiSavedInfo.Attributes |
                    System.IO.FileAttributes.Hidden |
                    System.IO.FileAttributes.System;
            }
            catch (System.IO.IOException oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorWritingFile,
                    strPathSavedInfoFile, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"",
                    strPathSavedInfoFile, "\": " + oEx.Message);

                return false;
            }


            // saving second saved info
            try
            {
                IDirectoryInfo di = iFileSystem.GetDirectoryInfo(
                    strPathSavedInfoFile2.Substring(0,
                    strPathSavedInfoFile2.LastIndexOfAny(new char[] { '\\', '/' })));

                if (!di.Exists)
                {
                    di.Create();
                    di = iFileSystem.GetDirectoryInfo(
                        strPathSavedInfoFile2.Substring(0,
                        strPathSavedInfoFile2.LastIndexOfAny(new char[] { '\\', '/' })));

                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden
                        | System.IO.FileAttributes.System;
                }

                using (IFile s = iFileSystem.Create(strPathSavedInfoFile2))
                {
                    si2.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile2);

                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                fiSavedInfo.Attributes = fiSavedInfo.Attributes |
                    System.IO.FileAttributes.Hidden |
                    System.IO.FileAttributes.System;
            }
            catch (System.IO.IOException oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(0,
                    Properties.Resources.IOErrorWritingFile,
                    strPathSavedInfoFile2, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"",
                    strPathSavedInfoFile2, "\": " + oEx.Message);

                return false;
            }

            // we just created the file, so assume we checked everything,
            // no need to double-check immediately
            CreateOrUpdateFileChecked(strPathSavedInfoFile,
                iFileSystem, iLogWriter);
            CreateOrUpdateFileChecked(strPathSavedInfoFile2,
                iFileSystem, iLogWriter);

            return true;
        }


        //===================================================================================================
        /// <summary>
        /// This method tests a single file, together with its saved info, if present
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bNeedsMessageAboutOldSavedInfo">Specifies, if method shall add a message
        /// in case saved info is outdated or wrong</param>
        /// <param name="bForcePhysicalTest">If set to false, method will analyze the last date and
        /// time the original file has been tested or copied and skip physical test, if possible</param>
        /// <param name="bCreateConfirmationFile">If test succeeds then information about succeeded
        /// test is saved in file system</param>
        /// <param name="bFailASAPwoMessage">If set to true method silently exits on first error</param>
        /// <param name="bReturnFalseIfNonRecoverableNotIfDamaged">Usually a test returns true if
        /// the file is healthy, but if this flag is set to true then method will also return true, 
        /// if the file can be completely restored using saved info</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        /// <returns>true iff the test succeeded</returns>
        //===================================================================================================
        public bool TestSingleFile2(
            string strPathFile,
            string strPathSavedInfoFile,
            ref bool bForceCreateInfo,
            bool bNeedsMessageAboutOldSavedInfo,
            bool bForcePhysicalTest,
            bool bCreateConfirmationFile,
            bool bFailASAPwoMessage,
            bool bReturnFalseIfNonRecoverableNotIfDamaged,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            )
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            IFileInfo finfo =
                iFileSystem.GetFileInfo(strPathFile);

            IFileInfo fiSavedInfo =
                iFileSystem.GetFileInfo(strPathSavedInfoFile);

            bool bSkipBufferedFile = false;

            try
            {
                if (!bForcePhysicalTest)
                {
                    IFileInfo fichecked =
                        iFileSystem.GetFileInfo(strPathSavedInfoFile + "ed");

                    // this randomly skips testing of files,
                    // so the user doesn't have to wait long, when performing checks annually:
                    // 100% of files are skipped within first 2 years after last check
                    // 0% after 7 years after last check

                    if (fichecked.Exists && finfo.Exists &&
                        (!fiSavedInfo.Exists || fiSavedInfo.LastWriteTimeUtc == finfo.LastWriteTimeUtc) &&
                        fichecked.LastWriteTimeUtc.CompareTo(finfo.LastWriteTimeUtc) > 0 &&
                        Math.Abs(DateTime.UtcNow.Year * 366 + DateTime.UtcNow.DayOfYear
                            - fichecked.LastWriteTimeUtc.Year * 366 - fichecked.LastWriteTimeUtc.DayOfYear)
                        < 366 * 2.2 + 366 * 4.6 * Utils.RandomForRecentlyChecked.NextDouble())
                    {
                        return true;
                    }
                }
            }
            catch (Exception oEx)
            {
                /* TODO: this line of code isn't hit by any unit tests */
                iLogWriter.WriteLogFormattedLocalized(1,
                    Properties.Resources.WarningWhileDiscoveringIfNeedsToBeRechecked,
                    oEx.Message, strPathFile);

                iLogWriter.WriteLog(true, 1, "Warning: ", oEx.Message,
                    " while discovering, if ", strPathFile,
                    " needs to be rechecked.");
            }

        repeat:
            SavedInfo oSavedInfo = new SavedInfo();
            bool bSaveInfoUnreadable = !fiSavedInfo.Exists;
            if (!bSaveInfoUnreadable)
            {
                try
                {
                    using (IFile s =
                        iFileSystem.CreateBufferedStream(
                            iFileSystem.OpenRead(strPathSavedInfoFile),
                            (int)Math.Min(fiSavedInfo.Length + 1, 32 * 1024 * 1024)))
                    {
                        oSavedInfo.ReadFrom(s, true);
                        s.Close();
                    }
                }
                catch (IOException) // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (IFile s =
                            iFileSystem.OpenRead(strPathSavedInfoFile))
                        {
                            oSavedInfo.ReadFrom(s, false);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException oEx)
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        iLogWriter.WriteLogFormattedLocalized(0, 
                            Properties.Resources.IOErrorReadingFile,
                            strPathSavedInfoFile, oEx.Message);

                        iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                            strPathSavedInfoFile, "\": " + oEx.Message);

                        bSaveInfoUnreadable = true;
                        bForceCreateInfo = true;
                        bForcePhysicalTest = true;
                    }
                }
            }


            if (bSaveInfoUnreadable || oSavedInfo.Length != finfo.Length ||
                !Utils.FileTimesEqual(oSavedInfo.TimeStamp, finfo.LastWriteTimeUtc))
            {
                bool bAllBlocksOK = true;

                bForceCreateInfo = fiSavedInfo.Exists;

                if (!bSaveInfoUnreadable && bNeedsMessageAboutOldSavedInfo)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    iLogWriter.WriteLogFormattedLocalized(0, 
                        Properties.Resources.SavedInfoFileCantBeUsedForTesting,
                        strPathSavedInfoFile, strPathFile);

                    iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                        "\" can't be used for testing file \"", strPathFile,
                        "\": it was created for another version of the file");
                }

                Block oBlock = new Block();

                try
                {
                    using (IFile s =
                        iFileSystem.CreateBufferedStream(
                            iFileSystem.OpenRead(finfo.FullName),
                            (int)Math.Min(finfo.Length + 1, 32 * 1024 * 1024)))
                    {
                        for (long lIndex = 0; ; lIndex++)
                        {
                            if (oBlock.ReadFrom(s) != oBlock.Length)
                            {
                                break;
                            }
                        }

                        s.Close();
                    }
                }
                catch (IOException)// in case there are any errors simply switch to unbuffered, so we have authentic results
                {
                    if (bFailASAPwoMessage)
                    {
                        return false;
                    }

                    if (bReturnFalseIfNonRecoverableNotIfDamaged)
                    {
                        return false;
                    }

                    using (IFile s =
                        iFileSystem.OpenRead(finfo.FullName))
                    {
                        for (long lIndex = 0; ; lIndex++)
                        {
                            try
                            {
                                if (oBlock.ReadFrom(s) != oBlock.Length)
                                    break;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                iLogWriter.WriteLogFormattedLocalized(0, 
                                    Properties.Resources.IOErrorReadingFileOffset,
                                    finfo.FullName, lIndex * oBlock.Length, oEx.Message);

                                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                                    finfo.FullName, "\", offset ",
                                    lIndex * oBlock.Length, ": " + oEx.Message);

                                s.Seek((lIndex + 1) * oBlock.Length,
                                    System.IO.SeekOrigin.Begin);

                                bAllBlocksOK = false;

                                if ((lIndex + 1) * oBlock.Length > s.Length)
                                    break;
                            }
                        }
                        s.Close();
                    }
                }

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile,
                        iFileSystem, iLogWriter);
                }

                return bAllBlocksOK;
            }


            try
            {
                long lNonRestoredSize = 0;
                bool bAllBlocksOK = true;

                IFile s =
                    iFileSystem.OpenRead(finfo.FullName);

                if (!bSkipBufferedFile)
                {
                    s = iFileSystem.CreateBufferedStream(s,
                        (int)Math.Min(finfo.Length + 1, 8 * 1024 * 1024));
                }

                using (s)
                {
                    oSavedInfo.StartRestore();

                    Block oBlock = new Block();

                    for (long lIndex = 0; ; lIndex++)
                    {

                        try
                        {
                            bool bBlockOk = true;
                            int nRead = 0;

                            if ((nRead = oBlock.ReadFrom(s)) == oBlock.Length)
                            {
                                bBlockOk = oSavedInfo.AnalyzeForTestOrRestore(oBlock, lIndex);

                                if (!bBlockOk)
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    if (bFailASAPwoMessage)
                                        return false;

                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName,
                                        lIndex * oBlock.Length);

                                    iLogWriter.WriteLog(true, 1, finfo.FullName,
                                        ": checksum of block at offset ",
                                        lIndex * oBlock.Length, " not OK");

                                    bAllBlocksOK = false;
                                }
                            }
                            else
                            {
                                if (nRead > 0)
                                {
                                    while (nRead < oBlock.Length)
                                        oBlock[nRead++] = 0;

                                    bBlockOk = oSavedInfo.AnalyzeForTestOrRestore(oBlock, lIndex);

                                    if (!bBlockOk)
                                    {
                                        /* TODO: this line of code isn't hit by any unit tests */
                                        if (bFailASAPwoMessage)
                                            return false;

                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName,
                                            lIndex * oBlock.Length);

                                        iLogWriter.WriteLog(true, 1, finfo.FullName,
                                            ": checksum of block at offset ",
                                            lIndex * oBlock.Length, " not OK");

                                        bAllBlocksOK = false;
                                    }
                                }
                                break;
                            }

                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }
                        catch (System.IO.IOException oEx)
                        {
                            if (bFailASAPwoMessage)
                            {
                                return false;
                            }

                            if (!bSkipBufferedFile)
                            {
                                // we need to re-read saveinfo
                                bSkipBufferedFile = true;

                                if (!iSettings.CancelClicked)
                                {
                                    goto repeat; 
                                }
                                else
                                {
                                    /* TODO: this line of code isn't hit by any unit tests */
                                    throw;
                                }
                            }

                            bAllBlocksOK = false;

                            iLogWriter.WriteLogFormattedLocalized(1, 
                                Properties.Resources.IOErrorReadingFileOffset,
                                finfo.FullName, lIndex * oBlock.Length, oEx.Message);

                            iLogWriter.WriteLog(true, 1, "I/O Error reading file: \"",
                                finfo.FullName, "\", offset ",
                                lIndex * oBlock.Length, ": " + oEx.Message);

                            s.Seek((lIndex + 1) * oBlock.Length,
                                System.IO.SeekOrigin.Begin);
                        }
                    }

                    List<RestoreInfo> oRestoreInfo = oSavedInfo.EndRestore(
                        out lNonRestoredSize, fiSavedInfo.FullName, iLogWriter);

                    if (oRestoreInfo.Count > 1)
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.ThereAreBadBlocksNonRestorableOnlyTested,
                            oRestoreInfo.Count, finfo.FullName, lNonRestoredSize);

                        iLogWriter.WriteLog(true, 0, "There are ", oRestoreInfo.Count, " bad blocks in the file ",
                            finfo.FullName, ", non-restorable parts: ", lNonRestoredSize,
                            " bytes, file remains unchanged, it was only tested");
                    }
                    else if (oRestoreInfo.Count > 0)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0,
                            Properties.Resources.ThereIsOneBadBlockNonRestorableOnlyTested,
                            finfo.FullName, lNonRestoredSize);

                        iLogWriter.WriteLog(true, 0, "There is one bad block in the file ", 
                            finfo.FullName, ", non-restorable parts: ", lNonRestoredSize,
                            " bytes, file remains unchanged, it was only tested");
                    }

                    s.Close();
                }

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file 
                    // match the file itself, or if they have been corrupted somehow
                    if (!oSavedInfo.VerifyIntegrityAfterRestoreTest())
                    {
                        /* TODO: this line of code isn't hit by any unit tests */
                        if (bNeedsMessageAboutOldSavedInfo)
                        {
                            iLogWriter.WriteLogFormattedLocalized(0,
                                Properties.Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                                strPathSavedInfoFile, strPathFile);

                            iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                                "\" has been damaged and needs to be recreated from \"",
                                strPathFile, "\"");
                        }
                        bForceCreateInfo = true;
                    }
                }

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    CreateOrUpdateFileChecked(strPathSavedInfoFile,
                        iFileSystem, iLogWriter);
                }

                if (bReturnFalseIfNonRecoverableNotIfDamaged)
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    return lNonRestoredSize == 0;
                }
                else
                {
                    return bAllBlocksOK;
                }
            }
            catch (System.IO.IOException oEx)
            {
                /* TODO: this line of code isn't hit by any unit tests */
                if (bFailASAPwoMessage)
                    return false;

                iLogWriter.WriteLogFormattedLocalized(0, 
                    Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, oEx.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + oEx.Message);

                return false;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Saves information, when the original file has been last read completely
        /// </summary>
        /// <param name="strPathSavedInfoFile">The path of restore info file (not original file)</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        public void CreateOrUpdateFileChecked(
            string strPathSavedInfoFile,
            IFileOperations iFileSystem,
            ILogWriter iLogWriter
            )
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // no need in ".chked" files, if we are creating a release
            if (Properties.Resources.CreateRelease)
                return;

            string strPathCheckedTime = strPathSavedInfoFile + "ed";

            try
            {
                if (iFileSystem.Exists(strPathCheckedTime))
                {
                    /* TODO: this line of code isn't hit by any unit tests */
                    iFileSystem.SetLastWriteTimeUtc(strPathCheckedTime, DateTime.UtcNow);
                }
                else
                {
                    // there we use the simple File.OpenWrite since we need only the date of the file
                    using (IFile s = iFileSystem.OpenWrite(strPathCheckedTime))
                    {
                        s.Close();
                    }
                }

                iFileSystem.SetAttributes( strPathCheckedTime, 
                    System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System);
            }
            catch (Exception oEx)
            {
                iLogWriter.WriteLogFormattedLocalized(1, 
                    Properties.Resources.WarningWhileCreating,
                    oEx.Message, strPathCheckedTime);

                iLogWriter.WriteLog(true, 1, "Warning: ", oEx.Message,
                    " while creating ", strPathCheckedTime);
            }
        }


    }
}