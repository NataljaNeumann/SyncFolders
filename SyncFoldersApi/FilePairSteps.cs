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
        /// This method tests a single file, and repairs it, if there are read or checkum errors
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bOnlyIfCompletelyRecoverable">Indicates, if the operation shall be performmed
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
            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile);

            SavedInfo si = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (IFile s = iFileSystem.CreateBufferedStream(
                            iFileSystem.OpenRead(
                            strPathSavedInfoFile),
                            (int)Math.Min(fiSavedInfo.Length + 1, 8 * 1024 * 1024)))
                    {
                        si.ReadFrom(s);
                        s.Close();
                    }
                }
                catch // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (IFile s =
                            iFileSystem.OpenRead(strPathSavedInfoFile))
                        {
                            si.ReadFrom(s);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFile,
                            strPathSavedInfoFile, ex.Message);

                        iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                            strPathSavedInfoFile, "\": " + ex.Message);

                        bNotReadableSi = true;
                    }
                }
            }

            if (bNotReadableSi ||
                si.Length != finfo.Length ||
                !Utils.FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc))
            {
                bool bAllBlocksOk = true;

                if (fiSavedInfo.Exists)
                    bForceCreateInfo = true;

                if (!bNotReadableSi)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.SavedInfoFileCantBeUsedForTesting,
                        strPathSavedInfoFile, strPathFile);

                    iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                        "\" can't be used for testing file \"", strPathFile,
                        "\": it was created for another version of the file");
                }

                using (IFile s = iFileSystem.Open(
                    finfo.FullName, System.IO.FileMode.Open,
                    bOnlyIfCompletelyRecoverable ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite,
                    System.IO.FileShare.Read))
                {
                    Block oBlock = new Block();

                    for (long lIndex = 0; ; lIndex++)
                    {
                        try
                        {
                            // we simply read to end, no need in content
                            if (oBlock.ReadFrom(s) != oBlock.Length)
                                break;
                        }
                        catch (System.IO.IOException oEx)
                        {
                            // fill bad block with zeros
                            for (int i = oBlock.Length - 1; i >= 0; --i)
                                oBlock[i] = 0;

                            int nLengthToWrite =
                                (int)(finfo.Length - lIndex * oBlock.Length > oBlock.Length ?
                                    oBlock.Length :
                                    finfo.Length - lIndex * oBlock.Length);

                            if (bOnlyIfCompletelyRecoverable)
                            {
                                // we can't recover, so put only messages, don't write to file
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.IOErrorReadingFileOffset,
                                    finfo.FullName, lIndex * oBlock.Length, oEx.Message);

                                iLogWriter.WriteLog(true, 1, "I/O error reading file ", finfo.FullName,
                                    " position ", lIndex * oBlock.Length, ": ", oEx.Message);

                                s.Seek(lIndex * oBlock.Length + nLengthToWrite, System.IO.SeekOrigin.Begin);
                            }
                            else
                            {
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

                if (bAllBlocksOk)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile, 
                        iFileSystem, iLogWriter);
                }

                return bAllBlocksOk;
            }

            System.DateTime dtmPrevLastWriteTime = finfo.LastWriteTimeUtc;

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
                                    for (int i = oBlock.Length - 1; i >= nReadCount; --i)
                                        oBlock[i] = 0;

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

                            if (iCancelable.CancelClicked)
                                throw new OperationCanceledException();

                        }
                        catch (System.IO.IOException ex)
                        {
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
                

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file 
                    // match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest() || si.NeedsRebuild())
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
            }
            catch (System.IO.IOException oEx)
            {
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

                List<RestoreInfo> aResoreInfos = si.EndRestore(
                    out lNonRestoredSize, fiSavedInfo.FullName, iLogWriter);

                if (aResoreInfos.Count > 0)
                {
                    if (lNonRestoredSize > 0)
                    {
                        bForceCreateInfo = true;
                    }

                    if (lNonRestoredSize == 0 || !bOnlyIfCompletelyRecoverable)
                    {
                        using (IFile s =
                            iFileSystem.OpenWrite(finfo.FullName))
                        {
                            foreach (RestoreInfo oRestoreInfo in aResoreInfos)
                            {
                                if (oRestoreInfo.NotRecoverableArea)
                                {
                                    if (oReadableButNotAccepted.ContainsKey(oRestoreInfo.Position / oRestoreInfo.Data.Length))
                                    {
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
                    }
                }

                if (bOnlyIfCompletelyRecoverable && lNonRestoredSize != 0)
                {
                    if (aResoreInfos.Count > 1)
                    {
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

                return lNonRestoredSize == 0;
            }
            catch (System.IO.IOException oEx)
            {
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
            // if same file then try to repair in place
            if (string.Equals(strPathTargetFile, strPathFile,
                StringComparison.InvariantCultureIgnoreCase))
            {
                if (iSettings.TestFiles && iSettings.RepairFiles)
                {
                    return TestAndRepairSingleFile(strPathFile, strPathSavedInfoFile,
                        ref bForceCreateInfo, false,
                        iFileSystem, iSettings, iLogWriter);
                }
                else
                {
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
                                throw new Exception(strMessage);

                            return false;
                        }
                    }
                    else
                        return true;
                }
            }


            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedInfoFile);

            DateTime dtmOriginalTime = finfo.LastWriteTimeUtc;

            SavedInfo oSavedInfo = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (IFile s =
                        iFileSystem.OpenRead(strPathSavedInfoFile))
                    {
                        oSavedInfo.ReadFrom(s);
                        s.Close();
                    }
                }
                catch (System.IO.IOException oEx)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, 
                        Properties.Resources.IOErrorReadingFile,
                        strPathSavedInfoFile, oEx.Message);

                    iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                        strPathSavedInfoFile, "\": " + oEx.Message);

                    bNotReadableSi = true;
                }
            }

            if (bNotReadableSi || oSavedInfo.Length != finfo.Length ||
                !(iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                Utils.FileTimesEqual(oSavedInfo.TimeStamp, finfo.LastWriteTimeUtc)))
            {
                bool bAllBlocksOk = true;
                bForceCreateInfo = true;

                if (!bNotReadableSi)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, 
                        Properties.Resources.SavedInfoFileCantBeUsedForTesting,
                        strPathSavedInfoFile, strPathFile);

                    iLogWriter.WriteLog(true, 0, "RestoreInfo file \"", strPathSavedInfoFile,
                        "\" can't be used for restoring file \"",
                        strPathFile, "\": it was created for another version of the file");
                }

                using (IFile s = iFileSystem.Open(
                    finfo.FullName, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    try
                    {
                        int nCountErrors = 0;

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
                                    if (bFailOnNonRecoverable)
                                        throw;

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
                                        for (int i = nLengthToWrite-1; i >= 0; --i)
                                            oBlock[i] = 0;

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

                        // after the file has been copied to a ".tmp" delete old one
                        IFileInfo fi2 = iFileSystem.GetFileInfo(strPathTargetFile);

                        if (fi2.Exists)
                            iFileSystem.Delete(fi2);

                        // and replace it with the new one
                        IFileInfo fi2tmp = iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");

                        if (bAllBlocksOk)
                        {
                            // if everything OK set original time
                            fi2tmp.LastWriteTimeUtc = dtmOriginalTime;
                        }
                        else
                        {
                            // set the time to very old, so any existing newer or with less errors appears to be better.
                            fi2tmp.LastWriteTimeUtc = new DateTime(1975, 9, 24 - nCountErrors / 60 / 24,
                                23 - (nCountErrors / 60) % 24, 59 - nCountErrors % 60, 0);

                            bForceCreateInfoTarget = true;
                        }

                        //fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                        fi2tmp.MoveTo(strPathTargetFile);

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

                    }
                    catch
                    {
                        // System.Threading.Thread.Sleep(100);

                        throw;
                    }
                    s.Close();
                }

                return bAllBlocksOk;
            }

            Dictionary<long, bool> oReadableButNotAccepted = new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;

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
                                    //  fill the rest with zeros
                                    while (nRead < oBlock.Length)
                                        oBlock[nRead++] = 0;

                                    bBlockOk = oSavedInfo.AnalyzeForTestOrRestore(oBlock, lIndex);

                                    if (!bBlockOk)
                                    {
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

                        if (iSettings.CancelClicked)
                            throw new OperationCanceledException();

                    }

                    s.Close();
                }


                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file match 
                    // the file itself, or if they have been corrupted somehow
                    if (!oSavedInfo.VerifyIntegrityAfterRestoreTest())
                    {
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

                List<RestoreInfo> aRestoreInfos = oSavedInfo.EndRestore(
                    out lNonRestoredSize, strPathSavedInfoFile, iLogWriter);

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

                if (aRestoreInfos.Count > 1)
                {
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

                //bool bNonRecoverablePresent = false;
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

                                                //bNonRecoverablePresent = true;
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
                                    // there we land in case we didn't overwrite the block with restore info,
                                    // so read from source and write to destination
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
                    // if we applied some repairs to the source, then restore its timestamp
                    if (bApplyRepairsToSrc && (aRestoreInfos.Count > 0))
                        finfo.LastWriteTimeUtc = dtmOriginalTime;
                }
                IFileInfo finfoTmp = iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");

                if (iFileSystem.Exists(strPathTargetFile))
                    iFileSystem.Delete(strPathTargetFile);

                finfoTmp.MoveTo(strPathTargetFile);

                if (oSavedInfo.NeedsRebuild())
                {
                    if ((!iSettings.FirstToSecond || !iSettings.FirstReadOnly) && aRestoreInfos.Count == 0)
                        bForceCreateInfo = true;

                    bForceCreateInfoTarget = true;
                }


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

                //finfo2.LastWriteTimeUtc = prevLastWriteTime;

                return lNonRestoredSize == 0;
            }
            catch (System.IO.IOException oEx)
            {
                if (bFailOnNonRecoverable)
                    throw;

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
            // if we can skip repairs, then try to test first and repair only in case there are some errors.
            if (iSettings.TestFilesSkipRecentlyTested &&
                TestSingleFile(strPathFile2, strPathSavedInfo2, 
                    ref bForceCreateInfo, false, false, true,
                    iFileSystem, iSettings, iLogWriter))
            {
                return;
            }

            IFileInfo fi1 = iFileSystem.GetFileInfo(strPathFile1);
            IFileInfo fi2 = iFileSystem.GetFileInfo(strPathFile2);
            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(strPathSavedInfo1);
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(strPathSavedInfo2);

            SavedInfo oSavedInfo1 = new SavedInfo();
            SavedInfo oSavedInfo2 = new SavedInfo();

            bool bSaveInfo1Present = false;

            if (fiSavedInfo1.Exists &&
                (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc))
            {
                using (IFile s = iFileSystem.OpenRead(fiSavedInfo1.FullName))
                {
                    oSavedInfo1.ReadFrom(s);

                    bSaveInfo1Present = oSavedInfo1.Length == fi1.Length &&
                        (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                        Utils.FileTimesEqual(oSavedInfo1.TimeStamp, fi1.LastWriteTimeUtc));

                    if (!bSaveInfo1Present)
                    {
                        oSavedInfo1 = new SavedInfo();
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        s.Seek(0, System.IO.SeekOrigin.Begin);
                        oSavedInfo2.ReadFrom(s);
                    }
                    s.Close();
                }
            }

            if (fiSavedInfo2.Exists &&
                (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc))
            {
                using (IFile s = iFileSystem.OpenRead(fiSavedInfo2.FullName))
                {
                    SavedInfo oSavedInfo2_1 = new SavedInfo();
                    oSavedInfo2_1.ReadFrom(s);

                    if (oSavedInfo2_1.Length == fi2.Length &&
                        (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo ||
                        Utils.FileTimesEqual(oSavedInfo2_1.TimeStamp, fi2.LastWriteTimeUtc)))
                    {
                        oSavedInfo2 = oSavedInfo2_1;
                        if (!bSaveInfo1Present)
                        {
                            s.Seek(0, System.IO.SeekOrigin.Begin);
                            oSavedInfo1.ReadFrom(s);
                            bSaveInfo1Present = true;
                        }
                    }
                    else
                    {
                        bForceCreateInfo = true;
                    }
                    s.Close();
                }
            }


            if (bSaveInfo1Present)
            {
                System.DateTime dtmPrevLastWriteTime = fi1.LastWriteTimeUtc;

                // improve the available saved infos, if needed 
                oSavedInfo1.ImproveThisAndOther(oSavedInfo2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> oEqualBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> oReadableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> oReadableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> aRestore1 = new List<RestoreInfo>();
                List<RestoreInfo> aRestore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
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
                            bool bBlockStream1Ok = false;

                            try
                            {
                                int nReadCount = 0;

                                if ((nReadCount = oBlock1.ReadFrom(s1)) == oBlock1.Length)
                                {
                                    bBlockStream1Ok = oSavedInfo1.AnalyzeForTestOrRestore(oBlock1, lIndex);
                                }
                                else
                                {
                                    if (nReadCount > 0)
                                    {
                                        for (int i = oBlock1.Length - 1; i >= nReadCount; --i)
                                            oBlock1[i] = 0;

                                        bBlockStream1Ok = oSavedInfo1.AnalyzeForTestOrRestore(oBlock1, lIndex);
                                    }
                                }

                                oReadableBlocks1[lIndex] = nReadCount>0;
                                bBlock1Present = nReadCount>0;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, 
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile1, oEx.Message);

                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"",
                                    strPathFile1, "\": ", oEx.Message);
                                    s1.Seek((lIndex + 1) * oBlock1.Length, System.IO.SeekOrigin.Begin);
                            }

                            bool bBlock2Present = false;
                            bool bBlock2Ok = false;

                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = oBlock2.ReadFrom(s2)) == oBlock2.Length)
                                {
                                    bBlock2Ok = oSavedInfo2.AnalyzeForTestOrRestore(oBlock2, lIndex);
                                }
                                else
                                {
                                    for (int i = oBlock2.Length - 1; i >= nReadCount; --i)
                                        oBlock2[i] = 0;

                                    bBlock2Ok = oSavedInfo2.AnalyzeForTestOrRestore(oBlock2, lIndex);
                                }

                                oReadableBlocks2[lIndex] = nReadCount>0;
                                bBlock2Present = nReadCount>0;
                            }
                            catch (System.IO.IOException oEx)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, 
                                    Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, oEx.Message);

                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"",
                                    strPathFile2, "\": ", oEx.Message);

                                s2.Seek((lIndex + 1) * oBlock2.Length, System.IO.SeekOrigin.Begin);
                            }

                            if (bBlock1Present && !bBlock2Present)
                            {
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
                                if (bBlock2Present && !bBlockStream1Ok)
                                {
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

                            if (bBlock1Present && bBlock2Present)
                            {
                                // if both blocks are present we'll compare their contents
                                // equal blocks could have higher priority compared to their checksums and saved infos
                                bool bDifferent = false;

                                for (int i = oBlock1.Length - 1; i >= 0; --i)
                                {
                                    if (oBlock1[i] != oBlock2[i])
                                    {
                                        bDifferent = true;
                                        break;
                                    }
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

                long lNotRestoredSize1 = 0;
                aRestore1.AddRange(oSavedInfo1.EndRestore(
                    out lNotRestoredSize1, fiSavedInfo1.FullName, iLogWriter));
                lNotRestoredSize1 = 0;

                long lNotRestoredSize2 = 0;
                aRestore2.AddRange(oSavedInfo2.EndRestore(
                    out lNotRestoredSize2, fiSavedInfo2.FullName, iLogWriter));
                lNotRestoredSize2 = 0;

                // now we've got the list of improvements for both files
                using (IFile s1 = iFileSystem.Open(
                    strPathFile1, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {

                    using (IFile s2 = iFileSystem.Open(
                        strPathFile2, System.IO.FileMode.Open,
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {

                        // let's apply improvements of one file to the list 
                        // of the other, whenever possible (we are in first folder readonly case)
                        foreach (RestoreInfo oRestoreInfo1 in aRestore1)
                        {
                            foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                            {
                                if (oRestoreInfo2.Position == oRestoreInfo1.Position &&
                                    oRestoreInfo2.NotRecoverableArea &&
                                    !oRestoreInfo1.NotRecoverableArea)
                                {
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

                        // let's apply the definitive improvements
                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            if (oRestoreInfo2.NotRecoverableArea ||
                                (iSettings.PreferPhysicalCopies &&
                                    oEqualBlocks.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length)))
                                ; // bForceCreateInfoBecauseDamaged = true;
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

                                // we assume the block is readbable now
                                oReadableBlocks2[oRestoreInfo2.Position / oRestoreInfo2.Data.Length] = true;
                            }
                        }



                        // let's try to copy non-recoverable blocks from one file to
                        // another, whenever possible
                        foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                        {
                            if (oRestoreInfo2.NotRecoverableArea &&
                                !oEqualBlocks.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                oReadableBlocks1.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length) &&
                                !oReadableBlocks2.ContainsKey(oRestoreInfo2.Position / oRestoreInfo2.Data.Length))
                            {
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

                        // after all fill non-readable blocks with zeroes
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

                if (aRestore2.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        aRestore2.Count, fi2.FullName, lNotRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", aRestore2.Count,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", lNotRestoredSize2);
                }

                if (aRestore1.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        aRestore1.Count, fi1.FullName);

                    iLogWriter.WriteLog(true, 0, "There remain ", aRestore1.Count,
                        " bad blocks in ", fi1.FullName,
                        ", because it can't be modified ");
                }

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
                }

            }
            else
            {
                System.DateTime dtmPrevLastWriteTime = fi1.LastWriteTimeUtc;

                // let's read both copies of the 
                // file that obviously are both present, without saved info
                List<RestoreInfo> aRestore2 = new List<RestoreInfo>();

                // now let's' try to read the files and compare 
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

                        for (long lIndex = 0; ; ++lIndex)
                        {

                            bool bBlock1Present = false;
                            bool bStillCreateEmptyBlock = false;

                            try
                            {
                                int nReadCount = oBlock1.ReadFrom(s1);

                                if (nReadCount > 0)
                                {
                                    for (int i = oBlock1.Length - 1; i >= nReadCount; --i)
                                        oBlock1[i] = 0;

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

                                // fill the block with zeros, so dummy block is empty, just in case
                                for (int i = oBlock1.Length - 1; i >= 0; --i)
                                    oBlock1[i] = 0;
                            }

                            bool bBlock2Present = false;

                            try
                            {
                                int nReadCount = oBlock2.ReadFrom(s2);

                                if (nReadCount > 0)
                                {
                                    for (int i = oBlock2.Length - 1; i >= nReadCount; --i)
                                        oBlock2[i] = 0;

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

                            if (bBlock1Present && !bBlock2Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, lIndex * oBlock1.Length, fi1.FullName);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                    lIndex * oBlock1.Length, " will be restored from ", fi1.FullName);

                                aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, false));
                            }
                            else if (!bBlock1Present && !bBlock2Present && bStillCreateEmptyBlock)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, lIndex * oBlock1.Length);

                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " at position ",
                                    lIndex * oBlock1.Length, " is not recoverable and will be filled with a dummy block");

                                aRestore2.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, true));
                            }

                            if (s1.Position >= s1.Length && s2.Position>=s2.Length)
                                break;

                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }

                    s1.Close();
                }


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

                if (lBadBlocks2 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        lBadBlocks2, fi2.FullName, lNotRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", lBadBlocks2, " bad blocks in ",
                        fi2.FullName, " not restored bytes: ", lNotRestoredSize2);
                }

                if (lBadBlocks1 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        lBadBlocks1, fi1.FullName);

                    iLogWriter.WriteLog(true, 0, "There remain ", lBadBlocks1, " bad blocks in ",
                        fi1.FullName, ", because it can't be modified ");
                }


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
            IFilePairStepsSettings iSettings,
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
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            )
        {
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
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            )
        {
            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, finfo.LastWriteTimeUtc, bForceSecondBlocks);

            try
            {
                using (IFile s =
                    iFileSystem.CreateBufferedStream(iFileSystem.OpenRead(finfo.FullName),
                        (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    Block oBlock = new Block();

                    for (long lIndex = 0; ; lIndex++)
                    {
                        int nReadCount = 0;
                        if ((nReadCount = oBlock.ReadFrom(s)) == oBlock.Length)
                        {
                            si.AnalyzeForInfoCollection(oBlock, lIndex);
                        }
                        else
                        {
                            if (nReadCount > 0)
                            {
                                // fill remaining part with zeros
                                for (int i = oBlock.Length - 1; i >= nReadCount; --i)
                                    oBlock[i] = 0;

                                si.AnalyzeForInfoCollection(oBlock, lIndex);
                            }
                            break;
                        }

                        if (iSettings.CancelClicked)
                            throw new OperationCanceledException();
                    }


                    s.Close();
                }
            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, ex.Message);

                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + ex.Message);

                return false;
            }

            try
            {
                IDirectoryInfo di = iFileSystem.GetDirectoryInfo(
                    strPathSavedChkInfoFile.Substring(0,
                    strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                if (!di.Exists)
                {
                    di.Create();

                    di = iFileSystem.GetDirectoryInfo(
                        strPathSavedChkInfoFile.Substring(0,
                        strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));

                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden |
                        System.IO.FileAttributes.System;
                }

                IFileInfo fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedChkInfoFile);

                if (fiSavedInfo.Exists)
                {
                    iFileSystem.Delete(fiSavedInfo);
                }

                using (IFile s = iFileSystem.CreateBufferedStream(iFileSystem.Create(strPathSavedChkInfoFile),
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

                // save last write time also at the time of the .chk file
                fiSavedInfo = iFileSystem.GetFileInfo(strPathSavedChkInfoFile);
                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                fiSavedInfo.Attributes = fiSavedInfo.Attributes 
                    | System.IO.FileAttributes.Hidden
                    | System.IO.FileAttributes.System;

                CreateOrUpdateFileChecked(strPathSavedChkInfoFile,
                    iFileSystem, iLogWriter);

            }
            catch (System.IO.IOException oEx)
            {
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
            string strTargetPath2 = strTargetPath + ".tmp";
            try
            {
                iFileSystem.CopyTo(fi, strTargetPath2, true);

                IFileInfo fi2 = iFileSystem.GetFileInfo(strTargetPath);

                if (fi2.Exists)
                    iFileSystem.Delete(fi2);

                IFileInfo fi2tmp = iFileSystem.GetFileInfo(strTargetPath2);
                fi2tmp.MoveTo(strTargetPath);

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
                    IFileInfo fi2 = iFileSystem.GetFileInfo(strTargetPath2);

                    if (fi2.Exists)
                        iFileSystem.Delete(fi2);
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
                    si1.ReadFrom(s);

                    bSaveInfo1Present = si1.Length == fi1.Length &&
                        Utils.FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc);

                    if (!bSaveInfo1Present)
                    {
                        si1 = new SavedInfo();
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        s.Seek(0, System.IO.SeekOrigin.Begin);
                        si2.ReadFrom(s);
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
                    si3.ReadFrom(s);

                    if (si3.Length == fi2.Length &&
                        Utils.FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc))
                    {
                        si2 = si3;
                        if (!bSaveInfo1Present)
                        {
                            s.Seek(0, System.IO.SeekOrigin.Begin);
                            si1.ReadFrom(s);
                            bSaveInfo1Present = true;
                        }
                    }
                    else
                    {
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

                // now let'oInputStream try to read the files and compare 
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

                long lNotRestoredSize1 = 0;
                aRestore1.AddRange(si1.EndRestore(out lNotRestoredSize1, fiSavedInfo1.FullName, iLogWriter));
                long lNotRestoredSize2 = 0;
                aRestore2.AddRange(si2.EndRestore(out lNotRestoredSize2, fiSavedInfo2.FullName, iLogWriter));

                // now we've got the list of improvements for both files
                using (IFile s1 = iFileSystem.Open(
                    strPathFile1, System.IO.FileMode.Open,
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {

                    using (IFile s2 = iFileSystem.Open(
                        strPathFile2, System.IO.FileMode.Open,
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {
                        // let's apply improvements of one file 
                        // to the list of the other, whenever possible
                        foreach (RestoreInfo oRestoreInfo1 in aRestore1)
                        {
                            foreach (RestoreInfo oRestoreInfo2 in aRestore2)
                            {
                                if (oRestoreInfo2.Position == oRestoreInfo1.Position)
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
                                    }
                                    else  if (oRestoreInfo1.NotRecoverableArea && !oRestoreInfo2.NotRecoverableArea)
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi1.FullName, oRestoreInfo1.Position, fi2.FullName);

                                        iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                            " position ", oRestoreInfo1.Position,
                                            " will be restored from ", fi2.FullName);

                                        oRestoreInfo1.Data = oRestoreInfo2.Data;
                                        oRestoreInfo1.NotRecoverableArea = false;
                                    }
                            }
                        }


                        // let'oInputStream apply the definitive improvements
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

                            bool bBlock1Present = false;
                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = oBlock1.ReadFrom(s1)) < oBlock1.Length)
                                {
                                    for (int i = oBlock1.Length - 1; i >= nReadCount; --i)
                                        oBlock1[i] = 0;
                                }

                                bBlock1Present = true;
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

                                bBlock2Present = true;
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
                            else
                            if (bBlock2Present && !bBlock1Present)
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

                                aRestore1.Add(new RestoreInfo(lIndex * oBlock1.Length, oBlock1, true));
                                aRestore2.Add(new RestoreInfo(lIndex * oBlock2.Length, oBlock2, true));

                                lNotRestoredSize1 += oBlock1.Length;
                                lNotRestoredSize2 += oBlock2.Length;
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


                // now we've got the list of improvements for both files
                using (IFile s1 = iFileSystem.Open(
                    strPathFile1, System.IO.FileMode.Open,
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
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

                fi1.LastWriteTimeUtc = prevLastWriteTime;

                if (lBadBlocks1 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        lBadBlocks1, fi1.FullName, lNotRestoredSize1);

                    iLogWriter.WriteLog(true, 0, "There were ", lBadBlocks1,
                        " bad blocks in ", fi1.FullName,
                        " not restored bytes: ", lNotRestoredSize1);
                }


                using (IFile s2 = iFileSystem.Open(
                    strPathFile2, System.IO.FileMode.Open,
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
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

                fi2.LastWriteTimeUtc = prevLastWriteTime;

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
                        oSavedInfo.ReadFrom(s);
                        s.Close();
                    }
                }
                catch // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (IFile s =
                            iFileSystem.OpenRead(strPathSavedInfoFile))
                        {
                            oSavedInfo.ReadFrom(s);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, 
                            Properties.Resources.IOErrorReadingFile,
                            strPathSavedInfoFile, ex.Message);

                        iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                            strPathSavedInfoFile, "\": " + ex.Message);

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

                bForceCreateInfo = true;

                if (!bSaveInfoUnreadable && bNeedsMessageAboutOldSavedInfo)
                {
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
                                break;
                        }
                        s.Close();
                    }
                }
                catch // in case there are any errors simply switch to unbuffered, so we have authentic results
                {
                    if (bFailASAPwoMessage)
                        return false;

                    if (bReturnFalseIfNonRecoverableNotIfDamaged)
                        return false;

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
                                return false;

                            if (!bSkipBufferedFile)
                            {
                                // we need to re-read saveinfo
                                bSkipBufferedFile = true;

                                if (!iSettings.CancelClicked)
                                    goto repeat;
                                else
                                    throw;
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
                    CreateOrUpdateFileChecked(strPathSavedInfoFile,
                        iFileSystem, iLogWriter);
                }

                if (bReturnFalseIfNonRecoverableNotIfDamaged)
                    return lNonRestoredSize == 0;
                else
                    return bAllBlocksOK;
            }
            catch (System.IO.IOException oEx)
            {
                if (bFailASAPwoMessage)
                    throw;

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
            // no need in ".chked" files, if we are creating a release
            if (Properties.Resources.CreateRelease)
                return;

            string strPathCheckedTime = strPathSavedInfoFile + "ed";

            try
            {
                if (iFileSystem.Exists(strPathCheckedTime))
                {
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