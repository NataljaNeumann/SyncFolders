using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using SyncFoldersApi.Localization;

namespace SyncFoldersApi
{
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
        /// <returns>true iff the test or restore succeeded</returns>
        //===================================================================================================
        public bool TestAndRepairSingleFile(
            string strPathFile,
            string strPathSavedInfoFile,
            ref bool bForceCreateInfo,
            bool bOnlyIfCompletelyRecoverable,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
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
                    using (IFile s =
                        iFileSystem.CreateBufferedStream(
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
                    Block b = new Block();
                    for (long index = 0; ; index++)
                    {
                        try
                        {
                            // we simply read to end, no need in content
                            if (b.ReadFrom(s) != b.Length)
                                break;
                        }
                        catch (System.IO.IOException ex)
                        {
                            // fill bad block with zeros
                            for (int i = b.Length - 1; i >= 0; --i)
                                b[i] = 0;

                            int lengthToWrite =
                                (int)(finfo.Length - index * b.Length > b.Length ?
                                    b.Length :
                                    finfo.Length - index * b.Length);

                            if (bOnlyIfCompletelyRecoverable)
                            {
                                // we can't recover, so put only messages, don't write to file
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.IOErrorReadingFileOffset,
                                    finfo.FullName, index * b.Length, ex.Message);
                                iLogWriter.WriteLog(true, 1, "I/O error reading file ", finfo.FullName,
                                    " position ", index * b.Length, ": ", ex.Message);
                                s.Seek(index * b.Length + lengthToWrite, System.IO.SeekOrigin.Begin);
                            }
                            else
                            {
                                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ErrorReadingPositionWillFillWithDummy,
                                    finfo.FullName, index * b.Length, ex.Message);
                                iLogWriter.WriteLog(true, 0, "Error while reading file ", finfo.FullName,
                                    " position ", index * b.Length, ": ", ex.Message,
                                    ". Block will be filled with a dummy");
                                s.Seek(index * b.Length, System.IO.SeekOrigin.Begin);
                                if (lengthToWrite > 0)
                                    b.WriteTo(s, lengthToWrite);
                            }
                            bAllBlocksOk = false;
                        }
                    }

                    s.Close();
                }

                if (bAllBlocksOk)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile, 
                        iFileSystem, iSettings, iLogWriter);
                }

                return bAllBlocksOk;
            }

            System.DateTime prevLastWriteTime = finfo.LastWriteTimeUtc;

            Dictionary<long, bool> readableButNotAccepted =
                new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;
                using (IFile s =
                    iFileSystem.OpenRead(finfo.FullName))
                {
                    si.StartRestore();
                    Block b = new Block();
                    for (long index = 0; ; index++)
                    {

                        try
                        {
                            bool bBlockOk = true;
                            int nReadCount = 0;
                            if ((nReadCount = b.ReadFrom(s)) == b.Length)
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    bAllBlocksOK = false;
                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName, index * b.Length);
                                    iLogWriter.WriteLog(true, 1, finfo.FullName,
                                        ": checksum of block at offset ",
                                        index * b.Length, " not OK");
                                    readableButNotAccepted[index] = true;
                                }
                            }
                            else
                            {
                                if (nReadCount > 0)
                                {
                                    for (int i = b.Length - 1; i >= nReadCount; --i)
                                        b[i] = 0;

                                    bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                    if (!bBlockOk)
                                    {
                                        bAllBlocksOK = false;
                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName, index * b.Length);
                                        iLogWriter.WriteLog(true, 1, finfo.FullName,
                                            ": checksum of block at offset ",
                                            index * b.Length, " not OK");
                                        readableButNotAccepted[index] = true;
                                    }
                                }
                                break;
                            }

                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }
                        catch (System.IO.IOException ex)
                        {
                            bAllBlocksOK = false;
                            iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.IOErrorReadingFileOffset,
                                finfo.FullName, index * b.Length, ex.Message);
                            iLogWriter.WriteLog(true, 1, "I/O Error reading file: \"",
                                finfo.FullName, "\", offset ",
                                index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length,
                                System.IO.SeekOrigin.Begin);
                        }

                    }
                    ;


                    s.Close();
                }
                ;

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file 
                    // match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
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
                            iFileSystem, iSettings, iLogWriter);
                    }
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
                long nonRestoredSize = 0;
                List<RestoreInfo> rinfos = si.EndRestore(
                    out nonRestoredSize, fiSavedInfo.FullName, iLogWriter);
                if (nonRestoredSize > 0)
                {
                    bForceCreateInfo = true;
                }

                if (nonRestoredSize == 0 || !bOnlyIfCompletelyRecoverable)
                {
                    using (IFile s =
                        iFileSystem.OpenWrite(finfo.FullName))
                    {
                        foreach (RestoreInfo ri in rinfos)
                        {
                            if (ri.NotRecoverableArea)
                            {
                                if (readableButNotAccepted.ContainsKey(ri.Position / ri.Data.Length))
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.KeepingReadableButNotRecoverableBlockAtOffset,
                                        ri.Position);
                                    iLogWriter.WriteLog(true, 1, "Keeping readable but not recoverable block at offset ",
                                        ri.Position, ", checksum indicates the block is wrong");
                                }
                                else
                                {
                                    s.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.FillingNotRecoverableAtOffsetWithDummy,
                                        ri.Position);
                                    iLogWriter.WriteLog(true, 1, "Filling not recoverable block at offset ",
                                        ri.Position, " with a dummy block");
                                    int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ?
                                        ri.Data.Length :
                                        si.Length - ri.Position);
                                    if (lengthToWrite > 0)
                                        ri.Data.WriteTo(s, lengthToWrite);
                                }
                                bForceCreateInfo = true;
                            }
                            else
                            {
                                s.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                    ri.Position, finfo.FullName);
                                iLogWriter.WriteLog(true, 1, "Recovering block at offset ",
                                    ri.Position, " of the file ", finfo.FullName);
                                int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ?
                                    ri.Data.Length :
                                    si.Length - ri.Position);
                                if (lengthToWrite > 0)
                                    ri.Data.WriteTo(s, lengthToWrite);
                            }
                        }

                        s.Close();
                    }
                }

                if (bOnlyIfCompletelyRecoverable && nonRestoredSize != 0)
                {
                    if (rinfos.Count > 1)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereAreBadBlocksNonRestorableCantBeBackup,
                            rinfos.Count, finfo.FullName, nonRestoredSize);
                        iLogWriter.WriteLog(true, 0, "There are ", rinfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", non-restorable parts: ", nonRestoredSize, " bytes, file can't be used as backup");
                    }
                    else
                        if (rinfos.Count > 0)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereIsBadBlockNonRestorableCantBeBackup,
                            finfo.FullName, nonRestoredSize);
                        iLogWriter.WriteLog(true, 0, "There is one bad block in the file ", finfo.FullName,
                            " and it can't be restored: ", nonRestoredSize, " bytes, file can't be used as backup");
                    }

                    finfo.LastWriteTimeUtc = prevLastWriteTime;
                }
                else
                {
                    if (rinfos.Count > 1)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                            rinfos.Count, finfo.FullName, nonRestoredSize);
                        iLogWriter.WriteLog(true, 0, "There were ", rinfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", not restored parts: ", nonRestoredSize, " bytes");
                    }
                    else
                        if (rinfos.Count > 0)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereWasBadBlockInFileNotRestoredParts,
                            finfo.FullName, nonRestoredSize);
                        iLogWriter.WriteLog(true, 0, "There was one bad block in the file ", finfo.FullName,
                            ", not restored parts: ", nonRestoredSize, " bytes");
                    }

                    if (nonRestoredSize == 0 && rinfos.Count == 0)
                    {
                        CreateOrUpdateFileChecked(strPathSavedInfoFile,
                            iFileSystem, iSettings, iLogWriter);
                    }

                    if (nonRestoredSize > 0)
                    {
                        int countErrors = (int)(nonRestoredSize / (new Block().Length));
                        finfo.LastWriteTimeUtc = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 23 -
                            (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                        bForceCreateInfo = true;
                    }
                    else
                        finfo.LastWriteTimeUtc = prevLastWriteTime;
                }

                return nonRestoredSize == 0;
            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorWritingFile,
                    finfo.FullName, ex.Message);
                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"", finfo.FullName, "\": " + ex.Message);
                finfo.LastWriteTimeUtc = prevLastWriteTime;
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
        /// <param name="strReason">The reason of the copy for log messages</param>
        /// <param name="bApplyRepairsToSrc">If set to true, method will also repair source file,
        /// not only the copy</param>
        /// <param name="bFailOnNonRecoverable">If there are non-recoverable blocks and this flag
        /// is set to true, then method throws an exception, instead of continuing</param>
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
                    return TestAndRepairSingleFile(strPathFile, strPathSavedInfoFile,
                        ref bForceCreateInfo, false,
                        iFileSystem, iSettings, iLogWriter);
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
                            string strMessage = string.Format(Properties.Resources.ErrorWhileTestingFile, strPathFile);
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

            SavedInfo si = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
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

            if (bNotReadableSi || si.Length != finfo.Length ||
                !(iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo || Utils.FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc)))
            {
                bool bAllBlocksOk = true;
                bForceCreateInfo = true;

                if (!bNotReadableSi)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.SavedInfoFileCantBeUsedForTesting,
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
                        int countErrors = 0;
                        using (IFile s2 = iFileSystem.Open(
                            strPathTargetFile + ".tmp", System.IO.FileMode.Create,
                            System.IO.FileAccess.Write, System.IO.FileShare.None))
                        {
                            for (long index = 0; ; index++)
                            {
                                Block b = new Block();
                                try
                                {
                                    int lengthToWrite = b.ReadFrom(s);
                                    if (lengthToWrite > 0)
                                        b.WriteTo(s2, lengthToWrite);
                                    if (lengthToWrite != b.Length)
                                        break;
                                }
                                catch (System.IO.IOException ex)
                                {
                                    if (bFailOnNonRecoverable)
                                        throw;

                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.IOErrorWhileReadingPositionFillDummyWhileCopy,
                                        finfo.FullName, index * b.Length, ex.Message);
                                    iLogWriter.WriteLog(true, 1, "I/O Error while reading file ",
                                        finfo.FullName, " position ", index * b.Length, ": ",
                                        ex.Message, ". Block will be replaced with a dummy during copy.");
                                    int lengthToWrite = (int)(finfo.Length - index * b.Length > b.Length ?
                                        b.Length :
                                        finfo.Length - index * b.Length);
                                    if (lengthToWrite > 0)
                                        b.WriteTo(s2, lengthToWrite);
                                    bAllBlocksOk = false;
                                    ++countErrors;

                                    if (lengthToWrite != b.Length)
                                        break;

                                    s.Seek(index * b.Length + lengthToWrite, System.IO.SeekOrigin.Begin);
                                }
                            }
                            ;

                            s2.Close();
                        }

                        // after the file has been copied to a ".tmp" delete old one
                        IFileInfo fi2 = iFileSystem.GetFileInfo(strPathTargetFile);

                        if (fi2.Exists)
                            iFileSystem.Delete(fi2);

                        // and replace it with the new one
                        IFileInfo fi2tmp = iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");
                        if (bAllBlocksOk)
                            // if everything OK set original time
                            fi2tmp.LastWriteTimeUtc = dtmOriginalTime;
                        else
                        {
                            // set the time to very old, so any existing newer or with less errors appears to be better.
                            fi2tmp.LastWriteTimeUtc = new DateTime(1975, 9, 24 - countErrors / 60 / 24,
                                23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                            bForceCreateInfoTarget = true;
                        }
                        //fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                        fi2tmp.MoveTo(strPathTargetFile);

                        if (!bAllBlocksOk)
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

                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(5000);

                        throw;
                    }
                    s.Close();
                }

                return bAllBlocksOk;
            }

            Dictionary<long, bool> readableButNotAccepted = new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;
                using (IFile s =
                    iFileSystem.OpenRead(finfo.FullName))
                {
                    si.StartRestore();
                    Block b = new Block();
                    for (long index = 0; ; index++)
                    {
                        try
                        {
                            bool bBlockOk = true;
                            int nRead = 0;
                            if ((nRead = b.ReadFrom(s)) == b.Length)
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    bAllBlocksOK = false;
                                    iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName, index * b.Length);
                                    iLogWriter.WriteLog(true, 2, finfo.FullName,
                                        ": checksum of block at offset ",
                                        index * b.Length, " not OK");
                                    readableButNotAccepted[index] = true;
                                }
                            }
                            else
                            {
                                if (nRead > 0)
                                {
                                    //  fill the rest with zeros
                                    while (nRead < b.Length)
                                        b[nRead++] = 0;

                                    bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                    if (!bBlockOk)
                                    {
                                        bAllBlocksOK = false;
                                        iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName, index * b.Length);
                                        iLogWriter.WriteLog(true, 2, finfo.FullName,
                                            ": checksum of block at offset ",
                                            index * b.Length, " not OK");
                                        readableButNotAccepted[index] = true;
                                    }
                                }
                                break;
                            }
                        }
                        catch (System.IO.IOException ex)
                        {
                            bAllBlocksOK = false;
                            iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFileOffset,
                                finfo.FullName, index * b.Length, ex.Message);
                            iLogWriter.WriteLog(true, 2, "I/O Error reading file: \"",
                                finfo.FullName, "\", offset ",
                                index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length,
                                System.IO.SeekOrigin.Begin);
                        }

                        if (iSettings.CancelClicked)
                            throw new OperationCanceledException();

                    }
                    ;

                    s.Close();
                }
                ;


                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file match 
                    // the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                            strPathSavedInfoFile, strPathFile);
                        iLogWriter.WriteLog(true, 0, "Saved info file \"",
                            strPathSavedInfoFile,
                            "\" has been damaged and needs to be recreated from \"",
                            strPathFile, "\"");
                        bForceCreateInfo = true;
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, ex.Message);
                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + ex.Message);

                if (bFailOnNonRecoverable)
                    throw;

                return false;
            }


            try
            {
                long nonRestoredSize = 0;
                List<RestoreInfo> rinfos = si.EndRestore(
                    out nonRestoredSize, strPathSavedInfoFile, iLogWriter);

                if (nonRestoredSize > 0)
                {
                    if (bFailOnNonRecoverable)
                    {
                        iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.ThereAreBadBlocksInNonRestorableMayRetryLater,
                            rinfos.Count, finfo.FullName, nonRestoredSize);
                        iLogWriter.WriteLog(true, 1, "There are ", rinfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", non-restorable parts: ", nonRestoredSize,
                            " bytes. Can't proceed there because of non-recoverable, may retry later.");
                        throw new Exception("Non-recoverable blocks discovered, failing");
                    }
                    else
                        bForceCreateInfoTarget = true;
                }

                if (rinfos.Count > 1)
                {
                    iLogWriter.WriteLogFormattedLocalized(1,
                        Properties.Resources.ThereAreBadBlocksInFileNonRestorableParts +
                            (bApplyRepairsToSrc ? "" :
                             Properties.Resources.TheFileCantBeModifiedMissingRepairApplyToCopy),
                        rinfos.Count, finfo.FullName, nonRestoredSize);
                    iLogWriter.WriteLog(true, 1, "There are ", rinfos.Count,
                        " bad blocks in the file ", finfo.FullName,
                        ", non-restorable parts: ", nonRestoredSize, " bytes. " +
                        (bApplyRepairsToSrc ? "" :
                            "The file can't be modified because of missing repair option, " +
                            "the restore process will be applied to copy."));
                }
                else
                    if (rinfos.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(1,
                       Properties.Resources.ThereIsBadBlockInFileNonRestorableParts +
                           (bApplyRepairsToSrc ? "" :
                           Properties.Resources.TheFileCantBeModifiedMissingRepairApplyToCopy),
                       finfo.FullName, nonRestoredSize);
                    iLogWriter.WriteLog(true, 1, "There is one bad block in the file ", finfo.FullName,
                       ", non-restorable parts: ", nonRestoredSize, " bytes. " +
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
                            bApplyRepairsToSrc && (rinfos.Count > 0) ?
                                System.IO.FileAccess.ReadWrite : System.IO.FileAccess.Read,
                            System.IO.FileShare.Read))
                    {
                        using (IFile s =
                            iFileSystem.Open(
                            strPathTargetFile + ".tmp", System.IO.FileMode.Create,
                            System.IO.FileAccess.Write, System.IO.FileShare.None))
                        {
                            long blockSize = new Block().Length;
                            for (long position = 0; position < finfo.Length; position += blockSize)
                            {
                                bool bBlockWritten = false;
                                foreach (RestoreInfo ri in rinfos)
                                {
                                    if (ri.Position == position)
                                    {
                                        bBlockWritten = true;
                                        if (ri.NotRecoverableArea)
                                        {
                                            if (readableButNotAccepted.ContainsKey(ri.Position / blockSize))
                                            {
                                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.KeepingReadableNonRecovBBlockAtAlsoInCopy,
                                                    ri.Position, finfo.FullName, strPathTargetFile);
                                                iLogWriter.WriteLog(true, 1, "Keeping readable but not recoverable block at offset ",
                                                    ri.Position, " of original file ", finfo.FullName,
                                                    " also in copy ", strPathTargetFile,
                                                    ", checksum indicates the block is wrong");
                                            }
                                            else
                                            {
                                                s2.Seek(ri.Position + ri.Data.Length, System.IO.SeekOrigin.Begin);

                                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.FillingNotRecoverableAtOffsetOfCopyWithDummy,
                                                    ri.Position, strPathTargetFile);

                                                iLogWriter.WriteLog(true, 1, "Filling not recoverable block at offset ",
                                                    ri.Position, " of copied file ", strPathTargetFile, " with a dummy");

                                                //bNonRecoverablePresent = true;
                                                int lengthToWrite = (int)(finfo.Length - position > blockSize ?
                                                    blockSize :
                                                    finfo.Length - position);
                                                if (lengthToWrite > 0)
                                                    ri.Data.WriteTo(s, lengthToWrite);
                                            }
                                            bForceCreateInfoTarget = true;
                                        }
                                        else
                                        {
                                            iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.RecoveringBlockAtOfCopiedFile,
                                                ri.Position, strPathTargetFile);
                                            iLogWriter.WriteLog(true, 1, "Recovering block at offset ",
                                                ri.Position, " of copied file ", strPathTargetFile);
                                            int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ?
                                                ri.Data.Length :
                                                si.Length - ri.Position);
                                            if (lengthToWrite > 0)
                                                ri.Data.WriteTo(s, lengthToWrite);

                                            if (bApplyRepairsToSrc)
                                            {
                                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                                    ri.Position, finfo.FullName);
                                                iLogWriter.WriteLog(true, 1, "Recovering block at offset ",
                                                    ri.Position, " of file ", finfo.FullName);
                                                s2.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                                if (lengthToWrite > 0)
                                                    ri.Data.WriteTo(s2, lengthToWrite);
                                            }
                                            else
                                                s2.Seek(ri.Position + lengthToWrite,
                                                    System.IO.SeekOrigin.Begin);
                                        }
                                        break;
                                    }
                                }

                                if (!bBlockWritten)
                                {
                                    // there we land in case we didn't overwrite the block with restore info,
                                    // so read from source and write to destination
                                    Block b = new Block();
                                    int lengthToWrite = b.ReadFrom(s2);
                                    b.WriteTo(s, lengthToWrite);
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
                    if (bApplyRepairsToSrc && (rinfos.Count > 0))
                        finfo.LastWriteTimeUtc = dtmOriginalTime;
                }
                IFileInfo finfoTmp = iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");
                if (iFileSystem.Exists(strPathTargetFile))
                    iFileSystem.Delete(strPathTargetFile);
                finfoTmp.MoveTo(strPathTargetFile);

                if (si.NeedsRebuild())
                {
                    if ((!iSettings.FirstToSecond || !iSettings.FirstReadOnly) && rinfos.Count == 0)
                        bForceCreateInfo = true;

                    bForceCreateInfoTarget = true;
                }


                IFileInfo finfo2 = iFileSystem.GetFileInfo(strPathTargetFile);
                if (rinfos.Count > 1)
                {
                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.OutOfBadBlocksNotRestoredInCopyBytes,
                        rinfos.Count, finfo2.FullName, nonRestoredSize);
                    iLogWriter.WriteLog(true, 1, "Out of ", rinfos.Count,
                        " bad blocks in the original file not restored parts in the copy ",
                        finfo2.FullName, ": ", nonRestoredSize, " bytes.");
                }
                else
                    if (rinfos.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.ThereWasBadBlockNotRestoredInCopyBytes,
                        finfo2.FullName, nonRestoredSize);
                    iLogWriter.WriteLog(true, 1, "There was one bad block in the original file, " +
                        "not restored parts in the copy ", finfo2.FullName, ": ",
                        nonRestoredSize, " bytes.");
                }

                if (nonRestoredSize > 0)
                {
                    int countErrors = (int)(nonRestoredSize / (new Block().Length));
                    finfo2.LastWriteTimeUtc = new DateTime(1975, 9, 24 - countErrors / 60 / 24,
                        23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                    bForceCreateInfoTarget = true;
                }
                else
                    finfo2.LastWriteTimeUtc = dtmOriginalTime;

                if (nonRestoredSize != 0)
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

                return nonRestoredSize == 0;
            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorDuringRepairCopyOf,
                    strPathTargetFile, ex.Message);
                iLogWriter.WriteLog(true, 0, "I/O Error during repair copy to file: \"",
                    strPathTargetFile, "\": " + ex.Message);
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
            if (iSettings.TestFilesSkipRecentlyTested)
                if (TestSingleFile(strPathFile2, strPathSavedInfo2, ref bForceCreateInfo, false, false, true,
                        iFileSystem, iSettings, iLogWriter))
                    return;

            IFileInfo fi1 = iFileSystem.GetFileInfo(strPathFile1);
            IFileInfo fi2 = iFileSystem.GetFileInfo(strPathFile2);
            IFileInfo fiSavedInfo1 = iFileSystem.GetFileInfo(strPathSavedInfo1);
            IFileInfo fiSavedInfo2 = iFileSystem.GetFileInfo(strPathSavedInfo2);

            SavedInfo si1 = new SavedInfo();
            SavedInfo si2 = new SavedInfo();

            bool bSaveInfo1Present = false;
            if (fiSavedInfo1.Exists &&
                (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo || fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc))
            {
                using (IFile s = iFileSystem.OpenRead(fiSavedInfo1.FullName))
                {
                    si1.ReadFrom(s);
                    bSaveInfo1Present = si1.Length == fi1.Length &&
                        (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo || Utils.FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc));
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
                (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo || fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc))
            {
                using (IFile s = iFileSystem.OpenRead(fiSavedInfo2.FullName))
                {
                    SavedInfo si3 = new SavedInfo();
                    si3.ReadFrom(s);
                    if (si3.Length == fi2.Length &&
                        (iSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo || Utils.FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc)))
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
                        bForceCreateInfo = true;
                    s.Close();
                }
            }


            if (bSaveInfo1Present)
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // improve the available saved infos, if needed 
                si1.ImproveThisAndOther(si2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> equalBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        si1.StartRestore();
                        si2.StartRestore();

                        for (int index = 0; ; ++index)
                        {
                            Block b1 = new Block();
                            Block b2 = new Block();

                            bool b1Present = false;
                            bool b1Ok = false;
                            bool s1Continue = false;
                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = b1.ReadFrom(s1)) == b1.Length)
                                {
                                    b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                    s1Continue = true;
                                }
                                else
                                {
                                    if (nReadCount > 0)
                                    {
                                        for (int i = b1.Length - 1; i >= nReadCount; --i)
                                            b1[i] = 0;
                                        b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                    }
                                }
                                readableBlocks1[index] = true;
                                b1Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFile,
                                    strPathFile1, ex.Message);
                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"", strPathFile1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, System.IO.SeekOrigin.Begin);
                            }

                            bool b2Present = false;
                            bool b2Ok = false;
                            bool s2Continue = false;
                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = b2.ReadFrom(s2)) == b2.Length)
                                {
                                    b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                    s2Continue = true;
                                }
                                else
                                {
                                    for (int i = b2.Length - 1; i >= nReadCount; --i)
                                        b2[i] = 0;
                                    b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                }
                                readableBlocks2[index] = true;
                                b2Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);
                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"",
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                if (si2.AnalyzeForTestOrRestore(b1, index))
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, index * b1.Length, fi1.FullName);
                                    iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                        index * b1.Length, " will be restored from ", fi1.FullName);
                                    restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                }
                            }
                            else
                                if (b2Present && !b1Present)
                            {
                                if (si1.AnalyzeForTestOrRestore(b2, index))
                                {
                                    restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi1.FullName, index * b1.Length, fi2.FullName);
                                    iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " position ", index *
                                        b1.Length, " could be restored from ", fi2.FullName,
                                        " but it is not possible to write to the first folder");
                                }
                            }
                            else
                            {
                                if (b2Present && !b1Ok)
                                {
                                    if (si1.AnalyzeForTestOrRestore(b2, index))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.BlockOfAtPositionCanBeRestoredFromNoWriteFirst,
                                            fi1.FullName, index * b1.Length, fi2.FullName);

                                        iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " at position ",
                                            index * b1.Length, " can be restored from ", fi2.FullName,
                                            " but it is not possible to write to the first folder");
                                        restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    }
                                }
                                ;

                                if (b1Present && !b2Ok)
                                {
                                    if (si2.AnalyzeForTestOrRestore(b1, index))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, index * b1.Length, fi1.FullName);
                                        iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " at position ",
                                            index * b1.Length, " will be restored from ", fi1.FullName);
                                        restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                    }
                                }
                            }

                            if (b1Present && b2Present)
                            {
                                // if both blocks are present we'll compare their contents
                                // equal blocks could have higher priority compared to their checksums and saved infos
                                bool bDifferent = false;
                                for (int i = b1.Length - 1; i >= 0; --i)
                                    if (b1[i] != b2[i])
                                    {
                                        bDifferent = true;
                                        break;
                                    }

                                if (!bDifferent)
                                {
                                    equalBlocks[index] = true;
                                }
                            }

                            if (!s1Continue && !s2Continue)
                                break;

                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }
                    s1.Close();
                }

                long notRestoredSize1 = 0;
                restore1.AddRange(si1.EndRestore(
                    out notRestoredSize1, fiSavedInfo1.FullName, iLogWriter));
                notRestoredSize1 = 0;

                long notRestoredSize2 = 0;
                restore2.AddRange(si2.EndRestore(
                    out notRestoredSize2, fiSavedInfo2.FullName, iLogWriter));
                notRestoredSize2 = 0;

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
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            foreach (RestoreInfo ri2 in restore2)
                            {
                                if (ri2.Position == ri1.Position &&
                                    ri2.NotRecoverableArea &&
                                    !ri1.NotRecoverableArea)
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, ri2.Position, fi1.FullName);

                                    iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                        " position ", ri2.Position,
                                        " will be restored from ", fi1.FullName);
                                    ri2.Data = ri1.Data;
                                    ri2.NotRecoverableArea = false;
                                }
                            }
                        }

                        // let'oInputStream apply the definitive improvements
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea ||
                                (iSettings.PreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
                                ; // bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                    ri2.Position, fi2.FullName);
                                iLogWriter.WriteLog(true, 1, "Recovering block of ",
                                    fi2.FullName, " at position ", ri2.Position);
                                s1.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ?
                                    ri2.Data.Length :
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        }
                        ;



                        // let'oInputStream try to copy non-recoverable blocks from one file to another, whenever possible
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea &&
                                !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                readableBlocks1.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1,
                                    Properties.Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi2.FullName, ri2.Position, fi1.FullName);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " at position ",
                                    ri2.Position, " will be copied from ",
                                    fi1.FullName, " even if checksum indicates the block is wrong");
                                s1.Seek(ri2.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                Block temp = new Block();
                                int length = temp.ReadFrom(s1);
                                temp.WriteTo(s2, length);
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        }
                        ;

                        // after all fill non-readable blocks with zeroes
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea &&
                                !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, ri2.Position);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                    ri2.Position, " is not recoverable and will be filled with a dummy");

                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ?
                                    ri2.Data.Length :
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                notRestoredSize2 += lengthToWrite;
                            }
                        }
                        ;

                        s2.Close();
                    }
                    s1.Close();
                }

                if (restore2.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        restore2.Count, fi2.FullName, notRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", restore2.Count,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", notRestoredSize2);
                }
                if (restore1.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        restore1.Count, fi1.FullName);
                    iLogWriter.WriteLog(true, 0, "There remain ", restore1.Count,
                        " bad blocks in ", fi1.FullName,
                        ", because it can't be modified ");
                }
                fi2.LastWriteTimeUtc = prevLastWriteTime;

            }
            else
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // let'oInputStream read both copies of the 
                // file that obviously are both present, without saved info
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                long notRestoredSize2 = 0;
                long badBlocks2 = 0;
                long badBlocks1 = 0;
                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        for (int index = 0; ; ++index)
                        {
                            Block b1 = new Block();
                            Block b2 = new Block();

                            bool b1Present = false;
                            bool s1Continue = false;
                            try
                            {
                                if (b1.ReadFrom(s1) == b1.Length)
                                    s1Continue = true;
                                b1Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                ++badBlocks1;
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFile,
                                    strPathFile1, ex.Message, ex.Message);
                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"",
                                    strPathFile1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            bool b2Present = false;
                            bool s2Continue = false;
                            try
                            {
                                if (b2.ReadFrom(s2) == b2.Length)
                                    s2Continue = true;
                                b2Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                ++badBlocks2;
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);

                                iLogWriter.WriteLog(true, 2, "I/O error while reading file \"",
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, index * b1.Length, fi1.FullName);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                    index * b1.Length, " will be restored from ", fi1.FullName);
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                            }
                            else
                            if (!b1Present && !b2Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, index * b1.Length);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " at position ",
                                    index * b1.Length, " is not recoverable and will be filled with a dummy block");
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, true));
                            }


                            if (!s1Continue && !s2Continue)
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
                    foreach (RestoreInfo ri2 in restore2)
                    {
                        s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ?
                            ri2.Data.Length :
                            si2.Length - ri2.Position);
                        if (lengthToWrite > 0)
                            ri2.Data.WriteTo(s2, lengthToWrite);
                    }
                    ;
                }

                if (badBlocks2 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        badBlocks2, fi2.FullName, notRestoredSize2);
                    iLogWriter.WriteLog(true, 0, "There were ", badBlocks2, " bad blocks in ",
                        fi2.FullName, " not restored bytes: ", notRestoredSize2);
                }
                if (badBlocks1 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0,
                        Properties.Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        badBlocks1, fi1.FullName);
                    iLogWriter.WriteLog(true, 0, "There remain ", badBlocks1, " bad blocks in ",
                        fi1.FullName, ", because it can't be modified ");
                }

                fi2.LastWriteTimeUtc = prevLastWriteTime;

            }
        }


        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
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
                    Block b = new Block();

                    for (int index = 0; ; index++)
                    {
                        int nReadCount = 0;
                        if ((nReadCount = b.ReadFrom(s)) == b.Length)
                        {
                            si.AnalyzeForInfoCollection(b, index);
                        }
                        else
                        {
                            if (nReadCount > 0)
                            {
                                // fill remaining part with zeros
                                for (int i = b.Length - 1; i >= nReadCount; --i)
                                    b[i] = 0;

                                si.AnalyzeForInfoCollection(b, index);
                            }
                            break;
                        }
                        if (iSettings.CancelClicked)
                            throw new OperationCanceledException();

                    }
                    ;


                    s.Close();
                }
                ;
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
                fiSavedInfo.Attributes = fiSavedInfo.Attributes | System.IO.FileAttributes.Hidden
                    | System.IO.FileAttributes.System;

                CreateOrUpdateFileChecked(strPathSavedChkInfoFile,
                    iFileSystem, iSettings, iLogWriter);

            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorWritingFile,
                    strPathSavedChkInfoFile, ex.Message);
                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"",
                    strPathSavedChkInfoFile, "\": " + ex.Message);
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
        /// <param name="strReason">Reason of the copy for messages</param>
        //===================================================================================================
        public void CopyFileSafely(
            IFileInfo fi,
            string strTargetPath,
            string strReasonEn,
            string strReasonTranslated,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
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
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileCopied, fi.FullName,
                    strTargetPath, strReasonTranslated);
                iLogWriter.WriteLog(true, 0, "Copied ", fi.FullName, " to ",
                    strTargetPath, " ", strReasonEn);
            }
            catch
            {
                try
                {
                    System.Threading.Thread.Sleep(5000);
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
            ILogWriter iLogWriter
            )
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
                        bForceCreateInfo = true;
                    s.Close();
                }
            }


            if (bSaveInfo1Present)
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // improve the available saved infos, if needed 
                si1.ImproveThisAndOther(si2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> equalBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        si1.StartRestore();
                        si2.StartRestore();

                        Block b1 = new Block();
                        Block b2 = new Block();

                        for (int index = 0; ; ++index)
                        {
                            for (int i = b1.Length - 1; i >= 0; --i)
                            {
                                b1[i] = 0;
                                b2[i] = 0;
                            }

                            bool b1Present = false;
                            bool b1Ok = false;
                            bool s1Continue = false;
                            try
                            {
                                int nRead = 0;
                                if ((nRead = b1.ReadFrom(s1)) == b1.Length)
                                {
                                    b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                    s1Continue = true;
                                    readableBlocks1[index] = true;
                                    b1Present = true;
                                }
                                else
                                {
                                    if (nRead > 0)
                                    {
                                        // fill the rest with zeros
                                        while (nRead < b1.Length)
                                            b1[nRead++] = 0;

                                        b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                        readableBlocks1[index] = true;
                                        b1Present = true;
                                    }
                                }

                                if (!b1Ok)
                                {
                                    iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        strPathFile1, index * b1.Length);
                                    iLogWriter.WriteLog(true, 2, strPathFile1, ": checksum of block at offset ",
                                        index * b1.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException ex)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFile,
                                    strPathFile1, ex.Message);
                                iLogWriter.WriteLog(true, 2, "I/O exception while reading file \"",
                                    strPathFile1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            bool b2Present = false;
                            bool b2Ok = false;
                            bool s2Continue = false;
                            try
                            {
                                int nRead = 0;
                                if ((nRead = b2.ReadFrom(s2)) == b2.Length)
                                {
                                    b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                    s2Continue = true;
                                    readableBlocks2[index] = true;
                                    b2Present = true;
                                }
                                else
                                {
                                    if (nRead > 0)
                                    {
                                        // fill the rest with zeros
                                        while (nRead < b2.Length)
                                            b2[nRead++] = 0;
                                        b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                        readableBlocks2[index] = true;
                                        b2Present = true;
                                    }
                                }

                                if (!b2Ok)
                                {
                                    iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        strPathFile2, index * b2.Length);
                                    iLogWriter.WriteLog(true, 2, strPathFile2, ": checksum of block at offset ",
                                        index * b2.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException ex)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);
                                iLogWriter.WriteLog(true, 2, "I/O exception while reading file \"",
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                if (si2.AnalyzeForTestOrRestore(b1, index))
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, index * b1.Length, fi1.FullName);
                                    iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                        " position ", index * b1.Length,
                                        " will be restored from ", fi1.FullName);
                                    restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                }
                            }
                            else
                                if (b2Present && !b1Present)
                            {
                                if (si1.AnalyzeForTestOrRestore(b2, index))
                                {
                                    iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi1.FullName, index * b1.Length, fi2.FullName);
                                    iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                        " position ", index * b1.Length,
                                        " will be restored from ", fi2.FullName);
                                    restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                }
                            }
                            else
                            {
                                if (b2Present && !b1Ok)
                                {
                                    if (si1.AnalyzeForTestOrRestore(b2, index))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi1.FullName, index * b1.Length, fi2.FullName);
                                        iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                            " position ", index * b1.Length,
                                            " will be restored from ", fi2.FullName);
                                        restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    }
                                }
                                ;

                                if (b1Present && !b2Ok)
                                {
                                    if (si2.AnalyzeForTestOrRestore(b1, index))
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, index * b1.Length, fi1.FullName);
                                        iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                            " position ", index * b1.Length,
                                            " will be restored from ", fi1.FullName);
                                        restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                    }
                                }
                            }

                            if (b1Present && b2Present)
                            {
                                // if both blocks are present we'll compare their contents
                                // equal blocks have higher priority compared to their checksums and saved infos
                                bool bDifferent = false;
                                for (int i = b1.Length - 1; i >= 0; --i)
                                    if (b1[i] != b2[i])
                                    {
                                        bDifferent = true;
                                        break;
                                    }

                                if (!bDifferent)
                                {
                                    equalBlocks[index] = true;
                                }
                            }

                            if (!s1Continue && !s2Continue)
                                break;

                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();

                    }
                    s1.Close();


                }

                long notRestoredSize1 = 0;
                restore1.AddRange(si1.EndRestore(out notRestoredSize1, fiSavedInfo1.FullName, iLogWriter));
                long notRestoredSize2 = 0;
                restore2.AddRange(si2.EndRestore(out notRestoredSize2, fiSavedInfo2.FullName, iLogWriter));

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
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            foreach (RestoreInfo ri2 in restore2)
                            {
                                if (ri2.Position == ri1.Position)
                                    if (ri2.NotRecoverableArea && !ri1.NotRecoverableArea)
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, ri2.Position, fi1.FullName);
                                        iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                            " position ", ri2.Position,
                                            " will be restored from ", fi1.FullName);
                                        ri2.Data = ri1.Data;
                                        ri2.NotRecoverableArea = false;
                                    }
                                    else
                                        if (ri1.NotRecoverableArea && !ri2.NotRecoverableArea)
                                    {
                                        iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi1.FullName, ri1.Position, fi2.FullName);
                                        iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                            " position ", ri1.Position,
                                            " will be restored from ", fi2.FullName);
                                        ri1.Data = ri2.Data;
                                        ri1.NotRecoverableArea = false;
                                    }
                            }
                        }


                        // let'oInputStream apply the definitive improvements
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea ||
                                (iSettings.PreferPhysicalCopies && equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length)))
                                ;// bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                    ri1.Position, fi1.FullName);
                                iLogWriter.WriteLog(true, 1, "Recovering block of ", fi1.FullName,
                                    " at position ", ri1.Position);
                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ?
                                    ri1.Data.Length :
                                    si1.Length - ri1.Position);
                                if (lengthToWrite > 0)
                                    ri1.Data.WriteTo(s1, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks1[ri1.Position / ri1.Data.Length] = true;
                            }
                        }
                        ;


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea ||
                                (iSettings.PreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
                                ; // bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.RecoveringBlockAtOffsetOfFile,
                                    ri2.Position, fi2.FullName);
                                iLogWriter.WriteLog(true, 1, "Recovering block of ", fi2.FullName,
                                    " at position ", ri2.Position);
                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ?
                                    ri2.Data.Length :
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        }
                        ;



                        // let'oInputStream try to copy non-recoverable 
                        // blocks from one file to another, whenever possible
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea && !equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length) &&
                                readableBlocks2.ContainsKey(ri1.Position / ri1.Data.Length) &&
                                !readableBlocks1.ContainsKey(ri1.Position / ri1.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi1.FullName, ri1.Position, fi2.FullName);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " position ",
                                    ri1.Position, " will be copied from ",
                                    fi2.FullName, " even if checksum indicates the block is wrong");

                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                Block temp = new Block();
                                int length = temp.ReadFrom(s2);
                                temp.WriteTo(s1, length);
                                readableBlocks1[ri1.Position / ri1.Data.Length] = true;
                            }
                        }
                        ;


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea && !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                               readableBlocks1.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))

                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi2.FullName, ri2.Position, fi1.FullName);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName, " position ",
                                    ri2.Position, " will be copied from ", fi1.FullName,
                                    " even if checksum indicates the block is wrong");

                                s1.Seek(ri2.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                Block temp = new Block();
                                int length = temp.ReadFrom(s1);
                                temp.WriteTo(s2, length);
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        }
                        ;

                        // after all fill non-readable blocks with zeroes
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea && !equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length) &&
                                !readableBlocks1.ContainsKey(ri1.Position / ri1.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi1.FullName, ri1.Position);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName, " position ",
                                    ri1.Position, " is not recoverable and will be filled with a dummy");

                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ?
                                    ri1.Data.Length :
                                    si1.Length - ri1.Position);
                                if (lengthToWrite > 0)
                                    ri1.Data.WriteTo(s1, lengthToWrite);
                            }
                        }
                        ;


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea &&
                                !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, ri2.Position);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                    " position ", ri2.Position,
                                    " is not recoverable and will be filled with a dummy");

                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ?
                                    ri2.Data.Length :
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                            }
                        }
                        ;




                        s2.Close();
                    }
                    s1.Close();
                }

                if (restore1.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        restore1.Count, fi1.FullName, notRestoredSize1);
                    iLogWriter.WriteLog(true, 0, "There were ", restore1.Count,
                        " bad blocks in ", fi1.FullName,
                        " not restored bytes: ", notRestoredSize1);
                }
                if (restore2.Count > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        restore2.Count, fi2.FullName, notRestoredSize2);

                    iLogWriter.WriteLog(true, 0, "There were ", restore2.Count,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", notRestoredSize2);
                }

                fi1.LastWriteTimeUtc = prevLastWriteTime;
                fi2.LastWriteTimeUtc = prevLastWriteTime;

                if (notRestoredSize1 == 0 && restore1.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo1,
                        iFileSystem, iSettings, iLogWriter);
                }

                if (notRestoredSize2 == 0 && restore2.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo2,
                        iFileSystem, iSettings, iLogWriter);
                }

            }
            else
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // let'oInputStream read both copies of the file 
                // that obviously are both present, without saved info
                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                long notRestoredSize1 = 0;
                long notRestoredSize2 = 0;
                long badBlocks1 = 0;
                long badBlocks2 = 0;
                using (IFile s1 =
                    iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 =
                        iFileSystem.OpenRead(strPathFile2))
                    {
                        for (int index = 0; ; ++index)
                        {
                            Block b1 = new Block();
                            Block b2 = new Block();

                            bool b1Present = false;
                            bool s1Continue = false;
                            try
                            {
                                if (b1.ReadFrom(s1) == b1.Length)
                                    s1Continue = true;
                                b1Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorWritingFile,
                                    strPathFile1, ex.Message);
                                iLogWriter.WriteLog(true, 2, "I/O exception while reading file \"",
                                    strPathFile1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length,
                                    System.IO.SeekOrigin.Begin);
                                ++badBlocks1;
                            }

                            bool b2Present = false;
                            bool s2Continue = false;
                            try
                            {
                                if (b2.ReadFrom(s2) == b2.Length)
                                    s2Continue = true;
                                b2Present = true;
                                ++badBlocks2;
                            }
                            catch (System.IO.IOException ex)
                            {
                                iLogWriter.WriteLogFormattedLocalized(2, Properties.Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);
                                iLogWriter.WriteLog(true, 2, "I/O exception while reading file \"",
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length,
                                    System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, index * b1.Length, fi1.FullName);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi2.FullName,
                                    " position ", index * b1.Length,
                                    " will be restored from ", fi1.FullName);
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                            }
                            else
                            if (b2Present && !b1Present)
                            {
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi1.FullName, index * b1.Length, fi2.FullName);
                                iLogWriter.WriteLog(true, 1, "Block of ", fi1.FullName,
                                    " position ", index * b1.Length,
                                    " will be restored from ", fi2.FullName);
                                restore1.Add(new RestoreInfo(index * b2.Length, b2, false));
                            }
                            else
                            if (!b1Present && !b2Present)
                            {
                                Block b = new Block();
                                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.BlocksOfAndAtPositionNonRecoverableFillDummy,
                                    fi1.FullName, fi2.FullName, index * b1.Length);
                                iLogWriter.WriteLog(true, 1, "Blocks of ", fi1.FullName,
                                    " and ", fi2.FullName, " at position ",
                                    index * b1.Length,
                                    " are not recoverable and will be filled with a dummy block");
                                restore1.Add(new RestoreInfo(index * b1.Length, b1, true));
                                restore2.Add(new RestoreInfo(index * b2.Length, b2, true));
                                notRestoredSize1 += b1.Length;
                                notRestoredSize2 += b2.Length;
                            }

                            if (!s1Continue && !s2Continue)
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
                    foreach (RestoreInfo ri1 in restore1)
                    {
                        s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ?
                            ri1.Data.Length :
                            si1.Length - ri1.Position);
                        if (lengthToWrite > 0)
                            ri1.Data.WriteTo(s1, lengthToWrite);
                    }
                    ;
                }
                ;
                fi1.LastWriteTimeUtc = prevLastWriteTime;

                if (badBlocks1 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        badBlocks1, fi1.FullName, notRestoredSize1);
                    iLogWriter.WriteLog(true, 0, "There were ", badBlocks1,
                        " bad blocks in ", fi1.FullName,
                        " not restored bytes: ", notRestoredSize1);
                }


                using (IFile s2 = iFileSystem.Open(
                    strPathFile2, System.IO.FileMode.Open,
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo ri2 in restore2)
                    {
                        s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ?
                            ri2.Data.Length :
                            si2.Length - ri2.Position);
                        if (lengthToWrite > 0)
                            ri2.Data.WriteTo(s2, lengthToWrite);
                    }
                    ;
                }
                fi2.LastWriteTimeUtc = prevLastWriteTime;
                if (badBlocks2 > 0)
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        badBlocks2, fi2.FullName, notRestoredSize2);
                    iLogWriter.WriteLog(true, 0, "There were ", badBlocks2,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", notRestoredSize2);
                }

                if (notRestoredSize1 == 0 && restore1.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo1,
                        iFileSystem, iSettings, iLogWriter);
                }

                if (notRestoredSize2 == 0 && restore2.Count == 0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo2,
                        iFileSystem, iSettings, iLogWriter);
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
        /// <param name="strReason">The reason of copy for messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        public bool CreateSavedInfoAndCopy(
            string strPathFile,
            string strPathSavedInfoFile,
            string strTargetPath,
            string strReasonEn,
            string strReasonTranslated,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter)
        {
            string pathFileCopy = strTargetPath + ".tmp";


            IFileInfo finfo = iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, finfo.LastWriteTimeUtc, false);
            try
            {
                using (IFile s =
                    iFileSystem.CreateBufferedStream(iFileSystem.OpenRead(finfo.FullName),
                        (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    try
                    {
                        using (IFile s2 =
                            iFileSystem.CreateBufferedStream(iFileSystem.Create(pathFileCopy),
                            (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                        {
                            Block b = new Block();

                            for (int index = 0; ; index++)
                            {
                                int readCount = 0;
                                if ((readCount = b.ReadFrom(s)) == b.Length)
                                {
                                    b.WriteTo(s2);
                                    si.AnalyzeForInfoCollection(b, index);
                                }
                                else
                                {
                                    if (readCount > 0)
                                    {
                                        for (int i = b.Length - 1; i >= readCount; --i)
                                            b[i] = 0;
                                        b.WriteTo(s2, readCount);
                                        si.AnalyzeForInfoCollection(b, index);
                                    }
                                    break;
                                }
                            }
                            ;


                            s2.Close();
                            IFileInfo fi2tmp = iFileSystem.GetFileInfo(pathFileCopy);
                            fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                            IFileInfo fi2 = iFileSystem.GetFileInfo(strTargetPath);
                            if (fi2.Exists)
                                iFileSystem.Delete(fi2);

                            fi2tmp.MoveTo(strTargetPath);

                            iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.CopiedFromToReason,
                                strPathFile, strTargetPath, strReasonTranslated);
                            iLogWriter.WriteLog(true, 0, "Copied ", strPathFile, " to ", strTargetPath, " ", strReasonEn);
                        }
                    }
                    catch
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(5000);
                            IFileInfo finfoCopy = iFileSystem.GetFileInfo(pathFileCopy);
                            if (finfoCopy.Exists)
                                iFileSystem.Delete(finfoCopy);
                        }
                        catch
                        {
                            // ignore
                        }
                        throw;
                    }

                    s.Close();
                }
                ;
            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.WarningIOErrorWhileCopyingToReason,
                    finfo.FullName, strTargetPath, ex.Message);
                iLogWriter.WriteLog(true, 0, "Warning: I/O Error while copying file: \"",
                    finfo.FullName, "\" to \"", strTargetPath, "\": " + ex.Message);
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
                fiSavedInfo.Attributes = fiSavedInfo.Attributes | System.IO.FileAttributes.Hidden |
                    System.IO.FileAttributes.System;

            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorWritingFile, strPathSavedInfoFile, ex.Message);
                iLogWriter.WriteLog(true, 0, "I/O Error writing file: \"", strPathSavedInfoFile, "\": " + ex.Message);
                return false;
            }

            // we just created the file, so assume we checked everything, no need to double-check immediately
            CreateOrUpdateFileChecked(strPathSavedInfoFile,
                iFileSystem, iSettings, iLogWriter);

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
        /// <returns>true iff the test succeeded</returns>
        //===================================================================================================
        public bool TestSingleFile2(
            string pathFile,
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
                iFileSystem.GetFileInfo(pathFile);
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
            catch (Exception ex)
            {
                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.WarningWhileDiscoveringIfNeedsToBeRechecked,
                    ex.Message, pathFile);
                iLogWriter.WriteLog(true, 1, "Warning: ", ex.Message,
                    " while discovering, if ", pathFile,
                    " needs to be rechecked.");
            }

        repeat:
            SavedInfo si = new SavedInfo();
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
                        bSaveInfoUnreadable = true;
                        bForceCreateInfo = true;
                        bForcePhysicalTest = true;
                    }
                }
            }


            if (bSaveInfoUnreadable || si.Length != finfo.Length ||
                !Utils.FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc))
            {
                bool bAllBlocksOK = true;

                bForceCreateInfo = true;
                if (!bSaveInfoUnreadable)
                    if (bNeedsMessageAboutOldSavedInfo)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.SavedInfoFileCantBeUsedForTesting,
                            strPathSavedInfoFile, pathFile);
                        iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                            "\" can't be used for testing file \"", pathFile,
                            "\": it was created for another version of the file");
                    }

                Block b = new Block();
                try
                {
                    using (IFile s =
                        iFileSystem.CreateBufferedStream(
                            iFileSystem.OpenRead(finfo.FullName),
                            (int)Math.Min(finfo.Length + 1, 32 * 1024 * 1024)))
                    {
                        for (int index = 0; ; index++)
                        {
                            if (b.ReadFrom(s) != b.Length)
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
                        for (int index = 0; ; index++)
                        {
                            try
                            {
                                if (b.ReadFrom(s) != b.Length)
                                    break;
                            }
                            catch (System.IO.IOException ex)
                            {
                                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFileOffset,
                                    finfo.FullName, index * b.Length, ex.Message);
                                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                                    finfo.FullName, "\", offset ",
                                    index * b.Length, ": " + ex.Message);
                                s.Seek((index + 1) * b.Length,
                                    System.IO.SeekOrigin.Begin);
                                bAllBlocksOK = false;
                            }
                        }
                        s.Close();
                    }
                }

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile,
                        iFileSystem, iSettings, iLogWriter);
                }

                return bAllBlocksOK;
            }


            try
            {
                long nonRestoredSize = 0;
                bool bAllBlocksOK = true;

                IFile s =
                    iFileSystem.OpenRead(finfo.FullName);
                if (!bSkipBufferedFile)
                    s = iFileSystem.CreateBufferedStream(s,
                        (int)Math.Min(finfo.Length + 1, 8 * 1024 * 1024));

                using (s)
                {
                    si.StartRestore();
                    Block b = new Block();
                    for (int index = 0; ; index++)
                    {


                        try
                        {
                            bool bBlockOk = true;
                            int nRead = 0;
                            if ((nRead = b.ReadFrom(s)) == b.Length)
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    if (bFailASAPwoMessage)
                                        return false;

                                    iLogWriter.WriteLogFormattedLocalized(1,
                                        Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName,
                                        index * b.Length);

                                    iLogWriter.WriteLog(true, 1, finfo.FullName,
                                        ": checksum of block at offset ",
                                        index * b.Length, " not OK");
                                    bAllBlocksOK = false;
                                }
                            }
                            else
                            {
                                if (nRead > 0)
                                {
                                    while (nRead < b.Length)
                                        b[nRead++] = 0;

                                    bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                    if (!bBlockOk)
                                    {
                                        if (bFailASAPwoMessage)
                                            return false;

                                        iLogWriter.WriteLogFormattedLocalized(1,
                                            Properties.Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName,
                                            index * b.Length);

                                        iLogWriter.WriteLog(true, 1, finfo.FullName,
                                            ": checksum of block at offset ",
                                            index * b.Length, " not OK");
                                        bAllBlocksOK = false;
                                    }
                                }
                                break;
                            }

                            if (iSettings.CancelClicked)
                                throw new OperationCanceledException();
                        }
                        catch (System.IO.IOException ex)
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

                            iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.IOErrorReadingFileOffset,
                                finfo.FullName, index * b.Length, ex.Message);
                            iLogWriter.WriteLog(true, 1, "I/O Error reading file: \"",
                                finfo.FullName, "\", offset ",
                                index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length,
                                System.IO.SeekOrigin.Begin);
                        }
                    }
                    ;

                    List<RestoreInfo> ri = si.EndRestore(out nonRestoredSize, fiSavedInfo.FullName, iLogWriter);
                    if (ri.Count > 1)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereAreBadBlocksNonRestorableOnlyTested,
                            ri.Count, finfo.FullName, nonRestoredSize);
                        iLogWriter.WriteLog(true, 0, "There are ", ri.Count, " bad blocks in the file ",
                            finfo.FullName, ", non-restorable parts: ", nonRestoredSize,
                            " bytes, file remains unchanged, it was only tested");
                    }
                    else
                        if (ri.Count > 0)
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.ThereIsOneBadBlockNonRestorableOnlyTested,
                            finfo.FullName, nonRestoredSize);
                        iLogWriter.WriteLog(true, 0, "There is one bad block in the file ", finfo.FullName,
                            ", non-restorable parts: ", nonRestoredSize,
                            " bytes, file remains unchanged, it was only tested");
                    }

                    s.Close();
                }
                ;

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file 
                    // match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        if (bNeedsMessageAboutOldSavedInfo)
                        {
                            iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                                strPathSavedInfoFile, pathFile);
                            iLogWriter.WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                                "\" has been damaged and needs to be recreated from \"",
                                pathFile, "\"");
                        }
                        bForceCreateInfo = true;
                    }
                }

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile,
                        iFileSystem, iSettings, iLogWriter);
                }

                if (bReturnFalseIfNonRecoverableNotIfDamaged)
                    return nonRestoredSize == 0;
                else
                    return bAllBlocksOK;
            }
            catch (System.IO.IOException ex)
            {
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.IOErrorReadingFile,
                    finfo.FullName, ex.Message);
                iLogWriter.WriteLog(true, 0, "I/O Error reading file: \"",
                    finfo.FullName, "\": " + ex.Message);
                return false;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Saves information, when the original file has been last read completely
        /// </summary>
        /// <param name="strPathSavedInfoFile">The path of restore info file (not original file)</param>
        //===================================================================================================
        public void CreateOrUpdateFileChecked(
            string strPathSavedInfoFile,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            )
        {
            // no need in ".chked" files, if we are creating a release
            if (Properties.Resources.CreateRelease)
                return;

            string strPath = strPathSavedInfoFile + "ed";

            try
            {
                if (iFileSystem.Exists(strPath))
                {
                    iFileSystem.SetLastWriteTimeUtc(strPath, DateTime.UtcNow);
                }
                else
                {
                    // there we use the simple File.OpenWrite since we need only the date of the file
                    using (IFile s = iFileSystem.OpenWrite(strPath))
                    {
                        s.Close();
                    }
                    ;
                }

                iFileSystem.SetAttributes(
                    strPath, System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System);
            }
            catch (Exception ex)
            {
                iLogWriter.WriteLogFormattedLocalized(1, Properties.Resources.WarningWhileCreating,
                    ex.Message, strPath);
                iLogWriter.WriteLog(true, 1, "Warning: ", ex.Message,
                    " while creating ", strPath);
            }
        }


    }
}
