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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// Provides strings for messages in unit tests
    /// </summary>
    //*******************************************************************************************************
    internal class SyncFoldersApiResources: SyncFoldersApi.Localization.ILocalizedStrings
    {
        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Block of &quot;{0}&quot; at position {1} can be restored from &quot;{2}&quot; but it is not possible to write to the first folder
        /// </summary>
        //===================================================================================================
        public string BlockOfAtPositionCanBeRestoredFromNoWriteFirst
        {
            get
            {
                return "Block of \"{0}\" at position {1} can be restored from \"{2}\" but it is not possible to write to the first folder";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Block of &quot;{0}&quot; position {1} is not recoverable and will be filled with a dummy
        /// </summary>
        //===================================================================================================
        public string BlockOfAtPositionNotRecoverableFillDumy
        {
            get
            {
               return "Block of \"{0}\" position {1} is not recoverable and will be filled with a dummy";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Block of &quot;{0}&quot; position {1} will be copied from &quot;{2}&quot; despite the fact that checksum indicates the block is wrong
        /// </summary>
        //===================================================================================================
        public string BlockOfAtPositionWillBeCopiedFromNoMatterChecksum
        {
            get
            {
               return "Block of \"{0}\" position {1} will be copied from \"{2}\" despite the fact that checksum indicates the block is wrong";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Block of &quot;{0}&quot; at position {1} will be restored from same position of &quot;{2}&quot;
        /// </summary>
        //===================================================================================================
        public string BlockOfAtPositionWillBeRestoredFrom
        {
            get
            {
               return "Block of \"{0}\" at position {1} will be restored from same position of \"{2}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Blocks of &quot;{0}&quot; and &quot;{1}&quot; at position {2} are not recoverable and will be filled with a dummy block
        /// </summary>
        //===================================================================================================
        public string BlocksOfAndAtPositionNonRecoverableFillDummy
        {
            get
            {
               return "Blocks of \"{0}\" and \"{1}\" at position {2} are not recoverable and will be filled with a dummy block";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// {0}: checksum of block at offset {1} not OK
        /// </summary>
        //===================================================================================================
        public string ChecksumOfBlockAtOffsetNotOK
        {
            get
            {
               return "{0}: checksum of block at offset {1} not OK";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Copied &quot;{0}&quot; to &quot;{1}&quot;, {2}
        /// </summary>
        //===================================================================================================
        public string CopiedFromToReason
        {
            get
            {
               return "Copied \"{0}\" to \"{1}\", {2}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: File &quot;{0}&quot; with year 1975 or earlier could not be used to restore file &quot;{1}&quot;. Such an old date indicates that it was the last chance to recover the file “{0}”.
        /// </summary>
        //===================================================================================================
        public string CouldntUseOutdatedFileForRestoringOther
        {
            get
            {
               return "Warning: File \"{0}\" with year 1975 or earlier could not be used to restore file \"{1}\". Such an old date indicates that it was the last chance to recover the file “{0}”.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// {0}
        /// </summary>
        //===================================================================================================
        public string DateFormat
        {
            get
            {
               return "{0}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Indicates if we are in default culture
        /// </summary>
        //===================================================================================================
        public string DefaultCulture
        {
            get
            {
               return "yes";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Deleted file &quot;{0}&quot; that is not present in &quot;{1}&quot; anymore
        /// </summary>
        //===================================================================================================
        public string DeletedFileNotPresentIn
        {
            get
            {
               return "Deleted file \"{0}\" that is not present in \"{1}\" anymore";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Deleted folder {0} including contents, because there is no {1} anymore
        /// </summary>
        //===================================================================================================
        public string DeletedFolder
        {
            get
            {
               return "Deleted folder {0} including contents, because there is no {1} anymore";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Removing saved infos that became obsolete...
        /// </summary>
        //===================================================================================================
        public string DeletingObsoleteSavedInfos
        {
            get
            {
               return "Removing saved infos that became obsolete...";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, which digits are used
        /// </summary>
        //===================================================================================================
        public string Digits
        {
            get
            {
               return "standard/arab";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: Encountered I/O error while copying &quot;{0}&quot;. The older file &quot;{1}&quot; seems to be OK
        /// </summary>
        //===================================================================================================
        public string EncounteredErrorOlderOk
        {
            get
            {
               return "Warning: Encountered I/O error while copying \"{0}\". The older file \"{1}\" seems to be OK";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: Encountered I/O error while copying &quot;{0}&quot;. Other file &quot;{1}&quot; has errors as well, or is a product of last chance restore. Trying to automatically repair &quot;{0}&quot;
        /// </summary>
        //===================================================================================================
        public string EncounteredErrorOtherBadToo
        {
            get
            {
               return "Warning: Encountered I/O error while copying \"{0}\". Other file \"{1}\" has errors as well, or is a product of last chance restore. Trying to automatically repair \"{0}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: Encountered error while copying &quot;{0}&quot; trying to automatically repair
        /// </summary>
        //===================================================================================================
        public string EncounteredErrorWhileCopyingTryingToRepair
        {
            get
            {
               return "Warning: Encountered error while copying \"{0}\" trying to automatically repair";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Error while deleting &quot;{0}&quot;: {1}
        /// </summary>
        //===================================================================================================
        public string ErrorDeleting
        {
            get
            {
               return "Error while deleting \"{0}\": {1}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Error while processing file pair &quot;{0}&quot; | &quot;{1}&quot;: {2}
        /// </summary>
        //===================================================================================================
        public string ErrorProcessinngFilePair
        {
            get
            {
               return "Error while processing file pair \"{0}\" | \"{1}\": {2}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Error while reading file &quot;{0}&quot;, position {1}: {2}&quot;. Block will be filled with a dummy
        /// </summary>
        //===================================================================================================
        public string ErrorReadingPositionWillFillWithDummy
        {
            get
            {
               return "Error while reading file \"{0}\", position {1}: {2}\". Block will be filled with a dummy";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Error while testing file &quot;{0}&quot;
        /// </summary>
        //===================================================================================================
        public string ErrorWhileTestingFile
        {
            get
            {
               return "Error while testing file \"{0}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Copied &quot;{0}&quot; to &quot;{1}&quot;, {2}
        /// </summary>
        //===================================================================================================
        public string FileCopied
        {
            get
            {
               return "Copied \"{0}\" to \"{1}\", {2}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// (file has a different date or length)
        /// </summary>
        //===================================================================================================
        public string FileHasDifferentTime
        {
            get
            {
               return "(file has a different date or length)";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: file has zero length, indicating a failed copy operation in the past: {0}
        /// </summary>
        //===================================================================================================
        public string FileHasZeroLength
        {
            get
            {
               return "Warning: file has zero length, indicating a failed copy operation in the past: {0}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// (file was healthy or repaired)
        /// </summary>
        //===================================================================================================
        public string FileHealthyOrRepaired
        {
            get
            {
               return "(file was healthy or repaired)";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: both files have zero length, indicating a failed copy operation in the past: &quot;{0}&quot; | &quot;{1}&quot;
        /// </summary>
        //===================================================================================================
        public string FilesHaveZeroLength
        {
            get
            {
               return "Warning: both files have zero length, indicating a failed copy operation in the past: \"{0}\" | \"{1}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// (file was healthy)
        /// </summary>
        //===================================================================================================
        public string FileWasHealthy
        {
            get
            {
               return "(file was healthy)";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// (file was new)
        /// </summary>
        //===================================================================================================
        public string FileWasNew
        {
            get
            {
               return "(file was new)";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// (file was newer or bigger)
        /// </summary>
        //===================================================================================================
        public string FileWasNewer
        {
            get
            {
               return "(file was newer or bigger)";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Filling not recoverable block at offset {0} of copied file &quot;{1}&quot; with a dummy
        /// </summary>
        //===================================================================================================
        public string FillingNotRecoverableAtOffsetOfCopyWithDummy
        {
            get
            {
               return "Filling not recoverable block at offset {0} of copied file \"{1}\" with a dummy";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Filling not recoverable block at offset {0} with a dummy block
        /// </summary>
        //===================================================================================================
        public string FillingNotRecoverableAtOffsetWithDummy
        {
            get
            {
               return "Filling not recoverable block at offset {0} with a dummy block";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: First file &quot;{0}&quot; has bad blocks, overwriting file &quot;{1}&quot; has been skipped, so the it remains as backup
        /// </summary>
        //===================================================================================================
        public string FirstFileHasBadBlocks
        {
            get
            {
               return "Warning: First file \"{0}\" has bad blocks, overwriting file \"{1}\" has been skipped, so the it remains as backup";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Found one file for possible synchronisation
        /// </summary>
        //===================================================================================================
        public string FoundFileForSync
        {
            get
            {
               return "Found one file for possible synchronisation";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Found {0} files for possible synchronisation
        /// </summary>
        //===================================================================================================
        public string FoundFilesForSync
        {
            get
            {
               return "Found {0} files for possible synchronisation";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Internal error: Couldn&apos;t restore any of the copies of the file &quot;{0}&quot; | &quot;{1}&quot;
        /// </summary>
        //===================================================================================================
        public string InternalErrorCouldntRestoreAny
        {
            get
            {
               return "Internal error: Couldn&apos;t restore any of the copies of the file \"{0}\" | \"{1}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// I/O Error during repair copy to file: &quot;{0}&quot;: {1}
        /// </summary>
        //===================================================================================================
        public string IOErrorDuringRepairCopyOf
        {
            get
            {
               return "I/O Error during repair copy to file: \"{0}\": {1}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// I/O Error reading file &quot;{0}&quot;: {1}
        /// </summary>
        //===================================================================================================
        public string IOErrorReadingFile
        {
            get
            {
               return "I/O Error reading file \"{0}\": {1}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// I/O Error reading file: &quot;{0}&quot;, offset {1}: {2}
        /// </summary>
        //===================================================================================================
        public string IOErrorReadingFileOffset
        {
            get
            {
               return "I/O Error reading file: \"{0}\", offset {1}: {2}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// I/O Error while reading file &quot;{0}&quot; position {1}: {2}. Block will be replaced with a dummy during copy.
        /// </summary>
        //===================================================================================================
        public string IOErrorWhileReadingPositionFillDummyWhileCopy
        {
            get
            {
               return "I/O Error while reading file \"{0}\" position {1}: {2}. Block will be replaced with a dummy during copy.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// I/O Error writing file &quot;{0}&quot;: {1}
        /// </summary>
        //===================================================================================================
        public string IOErrorWritingFile
        {
            get
            {
               return "I/O Error writing file \"{0}\": {1}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Keeping readable but not recoverable block at offset {0}, checksum indicates the block is wrong
        /// </summary>
        //===================================================================================================
        public string KeepingReadableButNotRecoverableBlockAtOffset
        {
            get
            {
               return "Keeping readable but not recoverable block at offset {0}, checksum indicates the block is wrong";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Keeping readable but not recoverable block at offset {0} of original file &quot;{1}&quot; also in copy &quot;{2}&quot;, checksum indicates the block is wrong
        /// </summary>
        //===================================================================================================
        public string KeepingReadableNonRecovBBlockAtAlsoInCopy
        {
            get
            {
               return "Keeping readable but not recoverable block at offset {0} of original file \"{1}\" also in copy \"{2}\", checksum indicates the block is wrong";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides the name for localized log file
        /// </summary>
        //===================================================================================================
        public string LogFileName
        {
            get
            {
               return "SyncFoldersUnitTest.log";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Message from copy process
        /// </summary>
        //===================================================================================================
        public string Message
        {
            get
            {
               return "Message from copy process";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// no
        /// </summary>
        //===================================================================================================
        public string No
        {
            get
            {
               return "no";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Operation canceled
        /// </summary>
        //===================================================================================================
        public string OperationCanceled
        {
            get
            {
               return "Operation canceled";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Operation finished
        /// </summary>
        //===================================================================================================
        public string OperationFinished
        {
            get
            {
               return "Operation finished";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Out of {0} bad blocks in the original file not restored parts in the copy &quot;{1}&quot;: {2} bytes.
        /// </summary>
        //===================================================================================================
        public string OutOfBadBlocksNotRestoredInCopyBytes
        {
            get
            {
               return "Out of {0} bad blocks in the original file not restored parts in the copy \"{1}\": {2} bytes.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// №
        /// </summary>
        //===================================================================================================
        public string ProcessNo
        {
            get
            {
               return "№";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides jumping point addon for the readme.html in localized language, if available
        /// </summary>
        //===================================================================================================
        public string ReadmeHtmlHelpJumpPoint
        {
            get
            {
               return "#en";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Recovering block at position {0} of copied destination file &quot;{1}&quot;
        /// </summary>
        //===================================================================================================
        public string RecoveringBlockAtOfCopiedFile
        {
            get
            {
               return "Recovering block at position {0} of copied destination file \"{1}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Recovering block at offset {0} of the file &quot;{1}&quot;
        /// </summary>
        //===================================================================================================
        public string RecoveringBlockAtOffsetOfFile
        {
            get
            {
               return "Recovering block at offset {0} of the file \"{1}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides indication, if writing is right-to-left in current culture
        /// </summary>
        //===================================================================================================
        public string RightToLeft
        {
            get
            {
               return "no";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Running without repair option, so couldn&apos;t decide, if the file &quot;{0}&quot; can be restored using &quot;{1}&quot;
        /// </summary>
        //===================================================================================================
        public string RunningWithoutRepairOptionUndecided
        {
            get
            {
               return "Running without repair option, so couldn&apos;t decide, if the file \"{0}\" can be restored using \"{1}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Saved info file &quot;{0}&quot; can&apos;t be used for testing file &quot;{1}&quot;: it was created for another version of the file
        /// </summary>
        //===================================================================================================
        public string SavedInfoFileCantBeUsedForTesting
        {
            get
            {
               return "Saved info file \"{0}\" can&apos;t be used for testing file \"{1}\": it was created for another version of the file";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Saved info file &quot;{0}&quot; has been damaged and needs to be recreated from &quot;{1}&quot;
        /// </summary>
        //===================================================================================================
        public string SavedInfoHasBeenDamagedNeedsRecreation
        {
            get
            {
               return "Saved info file \"{0}\" has been damaged and needs to be recreated from \"{1}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Scanning folders...
        /// </summary>
        //===================================================================================================
        public string ScanningFolders
        {
            get
            {
               return "Scanning folders...";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Error: The second folder contains file &quot;{0}&quot;, the selected folder seem to be wrong for delete option. Skipping processing of the folder and subfolders
        /// </summary>
        //===================================================================================================
        public string SecondFolderNoDelete
        {
            get
            {
               return "Error: The second folder contains file \"{0}\", the selected folder seem to be wrong for delete option. Skipping processing of the folder and subfolders";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Skipping file pair &quot;{0}&quot; | &quot;{1}&quot;. Special file prevents usage of delete option at wrong root folder.
        /// </summary>
        //===================================================================================================
        public string SkippingFilePairDontDelete
        {
            get
            {
               return "Skipping file pair \"{0}\" | \"{1}\". Special file prevents usage of delete option at wrong root folder.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// The file can&apos;t be modified because of missing repair option, the restore process will be applied to copy.
        /// </summary>
        //===================================================================================================
        public string TheFileCantBeModifiedMissingRepairApplyToCopy
        {
            get
            {
               return "The file can&apos;t be modified because of missing repair option, the restore process will be applied to copy.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There are {0} bad blocks in the file &quot;{1}&quot;, non-restorable parts: {2} bytes.
        /// </summary>
        //===================================================================================================
        public string ThereAreBadBlocksInFileNonRestorableParts
        {
            get
            {
               return "There are {0} bad blocks in the file \"{1}\", non-restorable parts: {2} bytes.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There are {0} bad blocks in the file &quot;{1}&quot;, non-restorable parts: {2} bytes. Can&apos;t proceed there because of non-recoverable, may retry later.
        /// </summary>
        //===================================================================================================
        public string ThereAreBadBlocksInNonRestorableMayRetryLater
        {
            get
            {
               return "There are {0} bad blocks in the file \"{1}\", non-restorable parts: {2} bytes. Can&apos;t proceed there because of non-recoverable, may retry later.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There are {0} bad blocks in the file &quot;{1}&quot;, non-restorable parts: {2} bytes, file can&apos;t be used as backup
        /// </summary>
        //===================================================================================================
        public string ThereAreBadBlocksNonRestorableCantBeBackup
        {
            get
            {
               return "There are {0} bad blocks in the file \"{1}\", non-restorable parts: {2} bytes, file can&apos;t be used as backup";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There are {0} bad blocks in the file &quot;{1}&quot; non-restorable parts: {2} bytes. File remains unchanged, it was only tested
        /// </summary>
        //===================================================================================================
        public string ThereAreBadBlocksNonRestorableOnlyTested
        {
            get
            {
               return "There are {0} bad blocks in the file \"{1}\" non-restorable parts: {2} bytes. File remains unchanged, it was only tested";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There is one bad block in the file &quot;{0}&quot;, non-restorable parts: {1} bytes.
        /// </summary>
        //===================================================================================================
        public string ThereIsBadBlockInFileNonRestorableParts
        {
            get
            {
               return "There is one bad block in the file \"{0}\", non-restorable parts: {1} bytes.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There is one bad block in the file &quot;{0}&quot; and it can&apos;t be restored: {1} bytes, file can&apos;t be used as backup
        /// </summary>
        //===================================================================================================
        public string ThereIsBadBlockNonRestorableCantBeBackup
        {
            get
            {
               return "There is one bad block in the file \"{0}\" and it can&apos;t be restored: {1} bytes, file can&apos;t be used as backup";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There is one bad block in the file &quot;{0}&quot;, non-restorable parts: {1} bytes, file remains unchanged, it was only tested
        /// </summary>
        //===================================================================================================
        public string ThereIsOneBadBlockNonRestorableOnlyTested
        {
            get
            {
               return "There is one bad block in the file \"{0}\", non-restorable parts: {1} bytes, file remains unchanged, it was only tested";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There remain {0} bad blocks in &quot;{1}&quot;, because it can&apos;t be modified
        /// </summary>
        //===================================================================================================
        public string ThereRemainBadBlocksInBecauseReadOnly
        {
            get
            {
               return "There remain {0} bad blocks in \"{1}\", because it can&apos;t be modified";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There was one bad block in the file &quot;{0}&quot;, not restored parts: {1} bytes
        /// </summary>
        //===================================================================================================
        public string ThereWasBadBlockInFileNotRestoredParts
        {
            get
            {
               return "There was one bad block in the file \"{0}\", not restored parts: {1} bytes";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There was one bad block in the original file, not restored parts in the copy &quot;{0}&quot;: {1} bytes.
        /// </summary>
        //===================================================================================================
        public string ThereWasBadBlockNotRestoredInCopyBytes
        {
            get
            {
               return "There was one bad block in the original file, not restored parts in the copy \"{0}\": {1} bytes.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// There were {0} bad blocks in the file &quot;{1}&quot;, not restored parts: {2} bytes
        /// </summary>
        //===================================================================================================
        public string ThereWereBadBlocksInFileNotRestoredParts
        {
            get
            {
               return "There were {0} bad blocks in the file \"{1}\", not restored parts: {2} bytes";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// This is a simulated I/O error at position {0}
        /// </summary>
        //===================================================================================================
        public string ThisIsASimulatedIOErrorAtPosition
        {
            get
            {
               return "This is a simulated I/O error at position {0}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: checksum of block at offset {0} doesn&apos;t match available in primary blocks of saved info &quot;{1}&quot;, primary saved info for the block will be ignored
        /// </summary>
        //===================================================================================================
        public string WarningChecksumOffsetPrimarySavedInfoIgnored
        {
            get
            {
               return "Warning: checksum of block at offset {0} doesn&apos;t match available in primary blocks of saved info \"{1}\", primary saved info for the block will be ignored";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: checksum of block at offset {0} doesn&apos;t match available in secondary blocks of saved info &quot;{1}&quot;, secondary saved info for the block will be ignored
        /// </summary>
        //===================================================================================================
        public string WarningChecksumOffsetSecondarySavedInfoIgnored
        {
            get
            {
               return "Warning: checksum of block at offset {0} doesn&apos;t match available in secondary blocks of saved info \"{1}\", secondary saved info for the block will be ignored";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: copied &quot;{0}&quot; to &quot;{1}&quot; {2} with errors
        /// </summary>
        //===================================================================================================
        public string WarningCopiedToWithErrors
        {
            get
            {
               return "Warning: copied \"{0}\" to \"{1}\" {2} with errors";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: I/O Error while copying file &quot;{0}&quot; to &quot;{1}&quot;: {2}
        /// </summary>
        //===================================================================================================
        public string WarningIOErrorWhileCopyingToReason
        {
            get
            {
               return "Warning: I/O Error while copying file \"{0}\" to \"{1}\": {2}";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: several blocks don&apos;t match in saved info &quot;{0}&quot;, saved info will be ignored completely
        /// </summary>
        //===================================================================================================
        public string WarningSeveralBlocksDontMatchInSIWillBeIgnored
        {
            get
            {
               return "Warning: several blocks don&apos;t match in saved info \"{0}\", saved info will be ignored completely";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: {0} while creating &quot;{1}&quot;
        /// </summary>
        //===================================================================================================
        public string WarningWhileCreating
        {
            get
            {
               return "Warning: {0} while creating \"{1}\"";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// Warning: {0}, while discovering, if &quot;{1}&quot; needs to be rechecked.
        /// </summary>
        //===================================================================================================
        public string WarningWhileDiscoveringIfNeedsToBeRechecked
        {
            get
            {
               return "Warning: {0}, while discovering, if \"{1}\" needs to be rechecked.";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides a localized string for a message like the following:
        /// yes
        /// </summary>
        //===================================================================================================
        public string Yes
        {
            get
            {
               return "yes";
            }
        }

        //===================================================================================================
        /// <summary>
        /// Formats a localized number
        /// </summary>
        /// <param name="oNumber">Number to format</param>
        /// <returns>Formatted object, if possible, or the object itself</returns>
        //===================================================================================================
        public object FormatNumber(
            object oNumber
            )
        {
            return oNumber;
        }

        //===================================================================================================
        /// <summary>
        /// Provides information if the application is running in "create release" mode
        /// </summary>
        //===================================================================================================
        public bool CreateRelease
        {
            get
            {
               return false;
            }
        }

    }
}
