using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFoldersApi
{
    public interface IFilePairSteps
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
            );


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
            ILogWriter iLogWriter);


        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            );

        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <param name="bForceSecondBlocks">Indicates that a second row of blocks must be created</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            bool bForceSecondBlocks,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            );

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
        bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            int nVersion,
            bool bForceSecondBlocks,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            );


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
        bool CopyRepairSingleFile(
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
            ILogWriter iLogWriter);

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
        void TestAndRepairSecondFile(
            string strPathFile1,
            string strPathFile2,
            string strPathSavedInfo1,
            string strPathSavedInfo2,
            ref bool bForceCreateInfo,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            );

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
        void CopyFileSafely(
            IFileInfo fi,
            string strTargetPath,
            string strReasonEn,
            string strReasonTranslated,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter);


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
        bool TestSingleFileHealthyOrCanRepair(
            string strPathFile,
            string strPathSavedInfoFile,
            ref bool bForceCreateInfo,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            );


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
        void TestAndRepairTwoFiles(
            string strPathFile1,
            string strPathFile2,
            string strPathSavedInfo1,
            string strPathSavedInfo2,
            ref bool bForceCreateInfo,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            );


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
        bool CreateSavedInfoAndCopy(
            string strPathFile,
            string strPathSavedInfoFile,
            string strTargetPath,
            string strReasonEn,
            string strReasonTranslated,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter);


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
        bool TestSingleFile2(
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
            );

        //===================================================================================================
        /// <summary>
        /// Saves information, when the original file has been last read completely
        /// </summary>
        /// <param name="strPathSavedInfoFile">The path of restore info file (not original file)</param>
        //===================================================================================================
        void CreateOrUpdateFileChecked(
            string strPathSavedInfoFile,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            ILogWriter iLogWriter
            );

    }
}
