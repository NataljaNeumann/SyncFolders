using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide ability to open and physically read files
    /// </summary>
    //*******************************************************************************************************
    interface IFileOpenAndCopyAbstraction
    {
        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        //===================================================================================================
        void CopyTo(
            FileInfo fi, 
            string strDestFileName
            );

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        /// <param name="bOverwrite">Indicates, if the file shall be overwritten, if exists</param>
        //===================================================================================================
        void CopyTo(
            FileInfo fi, 
            string strDestFileName, 
            bool bOverwrite
            );
    }
}
