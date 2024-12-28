using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// This class implements opening files directly in the file system. No special errors are introduced 
    /// </summary>
    //*******************************************************************************************************
    public class FileOpenAndCopyDirectly : IFileOpenAndCopyAbstraction
    {
        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        //===================================================================================================
        public void CopyTo(
            FileInfo fi, 
            string strDestFileName
            )
        {
            fi.CopyTo(strDestFileName);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        /// <param name="bOverwrite">Indicates, if the file shall be overwritten, if exists</param>
        //===================================================================================================
        public void CopyTo(
            FileInfo fi, 
            string strDestFileName, 
            bool bOverwrite
            )
        {
            fi.CopyTo(strDestFileName, bOverwrite);
        }
    }
}
