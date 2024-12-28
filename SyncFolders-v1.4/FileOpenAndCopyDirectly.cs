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
        public FileInfo CopyTo(
            FileInfo fi, 
            string strDestFileName
            )
        {
            return fi.CopyTo(strDestFileName);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        /// <param name="bOverwrite">Indicates, if the file shall be overwritten, if exists</param>
        //===================================================================================================
        public FileInfo CopyTo(
            FileInfo fi, 
            string strDestFileName, 
            bool bOverwrite
            )
        {
            return fi.CopyTo(strDestFileName, bOverwrite);
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public FileStream Open(
            string strPath,
            FileMode eMode
            )
        {
            return File.Open(strPath, eMode);
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <param name="eAccess">Access</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public FileStream Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess
            )
        {
            return File.Open(strPath, eMode, eAccess);
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <param name="eAccess">Access</param>
        /// <param name="eShare">File share</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public FileStream Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess,
            FileShare eShare
            )
        {
            return File.Open(strPath, eMode, eAccess, eShare);
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file for reading
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public FileStream OpenRead(
            string strPath
            )
        {
            return File.OpenRead(strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Opens a file for writing
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public FileStream OpenWrite(
            string strPath
            )
        {
            return File.OpenWrite(strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Physically deletes the file.
        /// </summary>
        /// <param name="strFilePath">Path of the file</param>
        //===================================================================================================
        public void Delete(
            string strFilePath
            )
        {
            File.Delete(strFilePath);
        }

        //===================================================================================================
        /// <summary>
        /// Physically deletes the file.
        /// </summary>
        /// <param name="fi">Path of the file</param>
        //===================================================================================================
        public void Delete(
            FileInfo fi
            )
        {
            fi.Delete();
        }
    }
}
