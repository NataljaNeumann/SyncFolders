using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide possibility to choose a step for a file pair
    /// </summary>
    //*******************************************************************************************************
    public interface IFilePairStepChooser
    {
        //===================================================================================================
        /// <summary>
        /// Decides which step to execute for a given pair of files and performs it
        /// using the provided IStepsImpl implementation.
        /// </summary>
        /// <param name="strFilePath1">Path to the first file</param>
        /// <param name="strFilePath2">Path to the second file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        public void ProcessFilePair(
            string strFilePath1,
            string strFilePath2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter);
    }
}
