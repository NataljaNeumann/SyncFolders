﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:2.0.50727.9179
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SyncFolders {
    using System;
    
    
    /// <summary>
    ///   Eine stark typisierte Ressourcenklasse zum Suchen von lokalisierten Zeichenfolgen usw.
    /// </summary>
    // Diese Klasse wurde von der StronglyTypedResourceBuilder automatisch generiert
    // -Klasse über ein Tool wie ResGen oder Visual Studio automatisch generiert.
    // Um einen Member hinzuzufügen oder zu entfernen, bearbeiten Sie die .ResX-Datei und führen dann ResGen
    // mit der /str-Option erneut aus, oder Sie erstellen Ihr VS-Projekt neu.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Gibt die zwischengespeicherte ResourceManager-Instanz zurück, die von dieser Klasse verwendet wird.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SyncFolders.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Überschreibt die CurrentUICulture-Eigenschaft des aktuellen Threads für alle
        ///   Ressourcenzuordnungen, die diese stark typisierte Ressourcenklasse verwenden.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Block of &quot;{0}&quot; at position {1} can be restored from &quot;{2}&quot; but it is not possible to write to the first folder ähnelt.
        /// </summary>
        internal static string BlockOfAtPositionCanBeRestoredFromNoWriteFirst {
            get {
                return ResourceManager.GetString("BlockOfAtPositionCanBeRestoredFromNoWriteFirst", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Block of &quot;{0}&quot; position {1} is not recoverable and will be filled with a dummy ähnelt.
        /// </summary>
        internal static string BlockOfAtPositionNotRecoverableFillDumy {
            get {
                return ResourceManager.GetString("BlockOfAtPositionNotRecoverableFillDumy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Block of &quot;{0}&quot; position {1} will be copied from &quot;{2}&quot; despite the fact that checksum indicates the block is wrong ähnelt.
        /// </summary>
        internal static string BlockOfAtPositionWillBeCopiedFromNoMatterChecksum {
            get {
                return ResourceManager.GetString("BlockOfAtPositionWillBeCopiedFromNoMatterChecksum", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Block of &quot;{0}&quot; at position {1} will be restored from same position of &quot;{2}&quot; ähnelt.
        /// </summary>
        internal static string BlockOfAtPositionWillBeRestoredFrom {
            get {
                return ResourceManager.GetString("BlockOfAtPositionWillBeRestoredFrom", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Blocks of &quot;{0}&quot; and &quot;{1}&quot; at position {2} are not recoverable and will be filled with a dummy block ähnelt.
        /// </summary>
        internal static string BlocksOfAndAtPositionNonRecoverableFillDummy {
            get {
                return ResourceManager.GetString("BlocksOfAndAtPositionNonRecoverableFillDummy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die {0}: checksum of block at offset {1} not OK ähnelt.
        /// </summary>
        internal static string ChecksumOfBlockAtOffsetNotOK {
            get {
                return ResourceManager.GetString("ChecksumOfBlockAtOffsetNotOK", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Copied &quot;{0}&quot; to &quot;{1}&quot;, {2} ähnelt.
        /// </summary>
        internal static string CopiedFromToReason {
            get {
                return ResourceManager.GetString("CopiedFromToReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: couldn&apos;t use outdated file &quot;{0}&quot; with year 1975 or earlier, signaling this was a last chance restore, for restoring &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string CouldntUseOutdatedFileForRestoringOther {
            get {
                return ResourceManager.GetString("CouldntUseOutdatedFileForRestoringOther", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die yes ähnelt.
        /// </summary>
        internal static string DefaultCulture {
            get {
                return ResourceManager.GetString("DefaultCulture", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Deleted file &quot;{0}&quot; that is not present in &quot;{1}&quot; anymore ähnelt.
        /// </summary>
        internal static string DeletedFileNotPresentIn {
            get {
                return ResourceManager.GetString("DeletedFileNotPresentIn", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Deleted folder {0} including contents, because there is no {1} anymore ähnelt.
        /// </summary>
        internal static string DeletedFolder {
            get {
                return ResourceManager.GetString("DeletedFolder", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: Encountered I/O error while copying &quot;{0}&quot;. The older file &quot;{1}&quot; seems to be OK ähnelt.
        /// </summary>
        internal static string EncounteredErrorOlderOk {
            get {
                return ResourceManager.GetString("EncounteredErrorOlderOk", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: Encountered I/O error while copying &quot;{0}&quot;. Other file &quot;{1}&quot; has errors as well, or is a product of last chance restore. Trying to automatically repair &quot;{0}&quot; ähnelt.
        /// </summary>
        internal static string EncounteredErrorOtherBadToo {
            get {
                return ResourceManager.GetString("EncounteredErrorOtherBadToo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: Encountered error while copying &quot;{0}&quot; trying to automatically repair ähnelt.
        /// </summary>
        internal static string EncounteredErrorWhileCopyingTryingToRepair {
            get {
                return ResourceManager.GetString("EncounteredErrorWhileCopyingTryingToRepair", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Error while deleting &quot;{0}&quot;: {1} ähnelt.
        /// </summary>
        internal static string ErrorDeleting {
            get {
                return ResourceManager.GetString("ErrorDeleting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Error while processing file pair &quot;{0}&quot; | &quot;{1}&quot;: {2} ähnelt.
        /// </summary>
        internal static string ErrorProcessinngFilePair {
            get {
                return ResourceManager.GetString("ErrorProcessinngFilePair", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Error while reading file &quot;{0}&quot;, position {1}: {2}&quot;. Block will be filled with a dummy ähnelt.
        /// </summary>
        internal static string ErrorReadingPositionWillFillWithDummy {
            get {
                return ResourceManager.GetString("ErrorReadingPositionWillFillWithDummy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Error while testing file &quot;{0}&quot; ähnelt.
        /// </summary>
        internal static string ErrorWhileTestingFile {
            get {
                return ResourceManager.GetString("ErrorWhileTestingFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Copied &quot;{0}&quot; to &quot;{1}&quot;, {2} ähnelt.
        /// </summary>
        internal static string FileCopied {
            get {
                return ResourceManager.GetString("FileCopied", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die (file has a different date or length) ähnelt.
        /// </summary>
        internal static string FileHasDifferentTime {
            get {
                return ResourceManager.GetString("FileHasDifferentTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: file has zero length, indicating a failed copy operation in the past: {0} ähnelt.
        /// </summary>
        internal static string FileHasZeroLength {
            get {
                return ResourceManager.GetString("FileHasZeroLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die (file was healthy or repaired) ähnelt.
        /// </summary>
        internal static string FileHealthyOrRepaired {
            get {
                return ResourceManager.GetString("FileHealthyOrRepaired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: both files have zero length, indicating a failed copy operation in the past: &quot;{0}&quot; | &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string FilesHaveZeroLength {
            get {
                return ResourceManager.GetString("FilesHaveZeroLength", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die (file was healthy) ähnelt.
        /// </summary>
        internal static string FileWasHealthy {
            get {
                return ResourceManager.GetString("FileWasHealthy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die (file was new) ähnelt.
        /// </summary>
        internal static string FileWasNew {
            get {
                return ResourceManager.GetString("FileWasNew", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die (file was newer or bigger) ähnelt.
        /// </summary>
        internal static string FileWasNewer {
            get {
                return ResourceManager.GetString("FileWasNewer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Filling not recoverable block at offset {0} of copied file &quot;{1}&quot; with a dummy ähnelt.
        /// </summary>
        internal static string FillingNotRecoverableAtOffsetOfCopyWithDummy {
            get {
                return ResourceManager.GetString("FillingNotRecoverableAtOffsetOfCopyWithDummy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Filling not recoverable block at offset {0} with a dummy block ähnelt.
        /// </summary>
        internal static string FillingNotRecoverableAtOffsetWithDummy {
            get {
                return ResourceManager.GetString("FillingNotRecoverableAtOffsetWithDummy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: First file &quot;{0}&quot; has bad blocks, overwriting file &quot;{1}&quot; has been skipped, so the it remains as backup ähnelt.
        /// </summary>
        internal static string FirstFileHasBadBlocks {
            get {
                return ResourceManager.GetString("FirstFileHasBadBlocks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Found one file for possible synchronisation ähnelt.
        /// </summary>
        internal static string FoundFileForSync {
            get {
                return ResourceManager.GetString("FoundFileForSync", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Found {0} files for possible synchronisation ähnelt.
        /// </summary>
        internal static string FoundFilesForSync {
            get {
                return ResourceManager.GetString("FoundFilesForSync", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Internal error: Couldn&apos;t restore any of the copies of the file &quot;{0}&quot; | &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string InternalErrorCouldntRestoreAny {
            get {
                return ResourceManager.GetString("InternalErrorCouldntRestoreAny", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die I/O Error during repair copy to file: &quot;{0}&quot;: {1} ähnelt.
        /// </summary>
        internal static string IOErrorDuringRepairCopyOf {
            get {
                return ResourceManager.GetString("IOErrorDuringRepairCopyOf", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die I/O Error reading file &quot;{0}&quot;: {1} ähnelt.
        /// </summary>
        internal static string IOErrorReadingFile {
            get {
                return ResourceManager.GetString("IOErrorReadingFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die I/O Error reading file: &quot;{0}&quot;, offset {1}: {2} ähnelt.
        /// </summary>
        internal static string IOErrorReadingFileOffset {
            get {
                return ResourceManager.GetString("IOErrorReadingFileOffset", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die I/O Error while reading file &quot;{0}&quot; position {1}: {2}. Block will be replaced with a dummy during copy. ähnelt.
        /// </summary>
        internal static string IOErrorWhileReadingPositionFillDummyWhileCopy {
            get {
                return ResourceManager.GetString("IOErrorWhileReadingPositionFillDummyWhileCopy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die I/O Error writing file &quot;{0}&quot;: {1} ähnelt.
        /// </summary>
        internal static string IOErrorWritingFile {
            get {
                return ResourceManager.GetString("IOErrorWritingFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Keeping readable but not recoverable block at offset {0}, checksum indicates the block is wrong ähnelt.
        /// </summary>
        internal static string KeepingReadableButNotRecoverableBlockAtOffset {
            get {
                return ResourceManager.GetString("KeepingReadableButNotRecoverableBlockAtOffset", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Keeping readable but not recoverable block at offset {0} of original file &quot;{1}&quot; also in copy &quot;{2}&quot;, checksum indicates the block is wrong ähnelt.
        /// </summary>
        internal static string KeepingReadableNonRecovBBlockAtAlsoInCopy {
            get {
                return ResourceManager.GetString("KeepingReadableNonRecovBBlockAtAlsoInCopy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Operation canceled ähnelt.
        /// </summary>
        internal static string OperationCanceled {
            get {
                return ResourceManager.GetString("OperationCanceled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Operation finished ähnelt.
        /// </summary>
        internal static string OperationFinished {
            get {
                return ResourceManager.GetString("OperationFinished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Out of {0} bad blocks in the original file not restored parts in the copy &quot;{1}&quot;: {2} bytes. ähnelt.
        /// </summary>
        internal static string OutOfBadBlocksNotRestoredInCopyBytes {
            get {
                return ResourceManager.GetString("OutOfBadBlocksNotRestoredInCopyBytes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Recovering block at position {0} of copied destination file &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string RecoveringBlockAtOfCopiedFile {
            get {
                return ResourceManager.GetString("RecoveringBlockAtOfCopiedFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Recovering block at offset {0} of the file &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string RecoveringBlockAtOffsetOfFile {
            get {
                return ResourceManager.GetString("RecoveringBlockAtOffsetOfFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Running without repair option, so couldn&apos;t decide, if the file &quot;{0}&quot; can be restored using &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string RunningWithoutRepairOptionUndecided {
            get {
                return ResourceManager.GetString("RunningWithoutRepairOptionUndecided", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Saved info file &quot;{0}&quot; can&apos;t be used for testing file &quot;{1}&quot;: it was created for another version of the file ähnelt.
        /// </summary>
        internal static string SavedInfoFileCantBeUsedForTesting {
            get {
                return ResourceManager.GetString("SavedInfoFileCantBeUsedForTesting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Saved info file &quot;{0}&quot; has been damaged and needs to be recreated from &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string SavedInfoHasBeenDamagedNeedsRecreation {
            get {
                return ResourceManager.GetString("SavedInfoHasBeenDamagedNeedsRecreation", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Error: The second folder contains file &quot;{0}&quot;, the selected folder seem to be wrong for delete option. Skipping processing of the folder and subfolders ähnelt.
        /// </summary>
        internal static string SecondFolderNoDelete {
            get {
                return ResourceManager.GetString("SecondFolderNoDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Skipping file pair &quot;{0}&quot; | &quot;{1}&quot;. Special file prevents usage of delete option at wrong root folder. ähnelt.
        /// </summary>
        internal static string SkippingFilePairDontDelete {
            get {
                return ResourceManager.GetString("SkippingFilePairDontDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die The file can&apos;t be modified because of missing repair option, the restore process will be applied to copy. ähnelt.
        /// </summary>
        internal static string TheFileCantBeModifiedMissingRepairApplyToCopy {
            get {
                return ResourceManager.GetString("TheFileCantBeModifiedMissingRepairApplyToCopy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There are {0} bad blocks in the file &quot;{1}&quot;, non-restorable parts: {2} bytes. ähnelt.
        /// </summary>
        internal static string ThereAreBadBlocksInFileNonRestorableParts {
            get {
                return ResourceManager.GetString("ThereAreBadBlocksInFileNonRestorableParts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There are {0} bad blocks in the file &quot;{1}&quot;, non-restorable parts: {2} bytes. Can&apos;t proceed there because of non-recoverable, may retry later. ähnelt.
        /// </summary>
        internal static string ThereAreBadBlocksInNonRestorableMayRetryLater {
            get {
                return ResourceManager.GetString("ThereAreBadBlocksInNonRestorableMayRetryLater", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There are {0} bad blocks in the file &quot;{1}&quot;, non-restorable parts: {2} bytes, file can&apos;t be used as backup ähnelt.
        /// </summary>
        internal static string ThereAreBadBlocksNonRestorableCantBeBackup {
            get {
                return ResourceManager.GetString("ThereAreBadBlocksNonRestorableCantBeBackup", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There are {0} bad blocks in the file &quot;{1}&quot; non-restorable parts: {2} bytes. File remains unchanged, it was only tested ähnelt.
        /// </summary>
        internal static string ThereAreBadBlocksNonRestorableOnlyTested {
            get {
                return ResourceManager.GetString("ThereAreBadBlocksNonRestorableOnlyTested", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There is one bad block in the file &quot;{0}&quot;, non-restorable parts: {1} bytes. ähnelt.
        /// </summary>
        internal static string ThereIsBadBlockInFileNonRestorableParts {
            get {
                return ResourceManager.GetString("ThereIsBadBlockInFileNonRestorableParts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There is one bad block in the file &quot;{0}&quot; and it can&apos;t be restored: {1} bytes, file can&apos;t be used as backup ähnelt.
        /// </summary>
        internal static string ThereIsBadBlockNonRestorableCantBeBackup {
            get {
                return ResourceManager.GetString("ThereIsBadBlockNonRestorableCantBeBackup", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There is one bad block in the file &quot;{0}&quot;, non-restorable parts: {1} bytes, file remains unchanged, it was only tested ähnelt.
        /// </summary>
        internal static string ThereIsOneBadBlockNonRestorableOnlyTested {
            get {
                return ResourceManager.GetString("ThereIsOneBadBlockNonRestorableOnlyTested", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There remain {0} bad blocks in &quot;{1}&quot;, because it can&apos;t be modified ähnelt.
        /// </summary>
        internal static string ThereRemainBadBlocksInBecauseReadOnly {
            get {
                return ResourceManager.GetString("ThereRemainBadBlocksInBecauseReadOnly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There was one bad block in the file &quot;{0}&quot;, not restored parts: {1} bytes ähnelt.
        /// </summary>
        internal static string ThereWasBadBlockInFileNotRestoredParts {
            get {
                return ResourceManager.GetString("ThereWasBadBlockInFileNotRestoredParts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There was one bad block in the original file, not restored parts in the copy &quot;{0}&quot;: {1} bytes. ähnelt.
        /// </summary>
        internal static string ThereWasBadBlockNotRestoredInCopyBytes {
            get {
                return ResourceManager.GetString("ThereWasBadBlockNotRestoredInCopyBytes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die There were {0} bad blocks in the file &quot;{1}&quot;, not restored parts: {2} bytes ähnelt.
        /// </summary>
        internal static string ThereWereBadBlocksInFileNotRestoredParts {
            get {
                return ResourceManager.GetString("ThereWereBadBlocksInFileNotRestoredParts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die This is a simulated I/O error at position {0} ähnelt.
        /// </summary>
        internal static string ThisIsASimulatedIOErrorAtPosition {
            get {
                return ResourceManager.GetString("ThisIsASimulatedIOErrorAtPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: checksum of block at offsset {0} doesn&apos;t match available in primary blocks of saved info &quot;{1}&quot;, primary saved info for the block will be ignored ähnelt.
        /// </summary>
        internal static string WarningChecksumOffsetPrimarySavedInfoIgnored {
            get {
                return ResourceManager.GetString("WarningChecksumOffsetPrimarySavedInfoIgnored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: checksum of block at offset {0} doesn&apos;t match available in secondary blocks of saved info &quot;{1}&quot;, secondary saved info for the block will be ignored ähnelt.
        /// </summary>
        internal static string WarningChecksumOffsetSecondarySavedInfoIgnored {
            get {
                return ResourceManager.GetString("WarningChecksumOffsetSecondarySavedInfoIgnored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: copied &quot;{0}&quot; to &quot;{1}&quot; {2} with errors ähnelt.
        /// </summary>
        internal static string WarningCopiedToWithErrors {
            get {
                return ResourceManager.GetString("WarningCopiedToWithErrors", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: I/O Error while copying file &quot;{0}&quot; to &quot;{1}&quot;: {2} ähnelt.
        /// </summary>
        internal static string WarningIOErrorWhileCopyingToReason {
            get {
                return ResourceManager.GetString("WarningIOErrorWhileCopyingToReason", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: several blocks don&apos;t match in saved info &quot;{0}&quot;, saved info will be ignored completely ähnelt.
        /// </summary>
        internal static string WarningSeveralBlocksDontMatchInSIWillBeIgnored {
            get {
                return ResourceManager.GetString("WarningSeveralBlocksDontMatchInSIWillBeIgnored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: {0} while creating &quot;{1}&quot; ähnelt.
        /// </summary>
        internal static string WarningWhileCreating {
            get {
                return ResourceManager.GetString("WarningWhileCreating", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Sucht eine lokalisierte Zeichenfolge, die Warning: {0}, while discovering, if &quot;{1}&quot; needs to be rechecked. ähnelt.
        /// </summary>
        internal static string WarningWhileDiscoveringIfNeedsToBeRechecked {
            get {
                return ResourceManager.GetString("WarningWhileDiscoveringIfNeedsToBeRechecked", resourceCulture);
            }
        }
    }
}
