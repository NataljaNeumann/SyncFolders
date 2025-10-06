using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide the functionality to decide, which basic steps need
    /// to be done in a particular situation
    /// </summary>
    //*******************************************************************************************************
    public interface IFilePairStepsDirectionLogic
    {
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
        void ProcessFilePair_FirstToSecond_FirstReadonly_SecondExists(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );

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
        void ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );

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
        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );

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
        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );


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
        void ProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );

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
        void ProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );


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
        void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );

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
        void ProcessFilePair_Bidirectionally_FirstExists(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );


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
        void ProcessFilePair_Bidirectionally_SecondExists(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist and first file has a more recent date
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="strReasonEn">Reason, in english (hardcoded)</param>
        /// <param name="strReasonTranslated">Reason, localized in user language</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally_BothExist_FirstNewer(
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
            );


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
        void ProcessFilePair_Bidirectionally_BothExist_SecondNewer(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );


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
        void ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            );

     }
}
