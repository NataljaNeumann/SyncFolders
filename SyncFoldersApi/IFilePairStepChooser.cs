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
        /// Decides the step to execute
        /// </summary>
        /// <param name="strFilePath1">Path to first file</param>
        /// <param name="strFilePath2">Path to second file</param>
        /// <param name="iFileSystem">File system for performing operations</param>
        /// <param name="iSettings">Settings for operations</param>
        /// <param name="iLogic">Implementation of logic</param>
        /// <param name="iStepsImpl">Implementation of steps</param>
        /// <param name="iLogWriter">Log writer</param>
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
