/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024-2026 NataljaNeumann@gmx.de

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
