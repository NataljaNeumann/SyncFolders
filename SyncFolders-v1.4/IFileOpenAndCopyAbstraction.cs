using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide ability to open and physically read files.
    /// Some classes of objects may do this directly, some other may introduce a layer between physical
    /// reads and the logical reads
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
        FileInfo CopyTo(
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
        FileInfo CopyTo(
            FileInfo fi, 
            string strDestFileName, 
            bool bOverwrite
            );


        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        FileStream Open(
            string strPath, 
            FileMode eMode
            );

        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <param name="eAccess">Access</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        FileStream Open(
            string strPath, 
            FileMode eMode, 
            FileAccess eAccess
            );

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
        FileStream Open(
            string strPath, 
            FileMode eMode, 
            FileAccess eAccess, 
            FileShare eShare
            );

        //===================================================================================================
        /// <summary>
        /// Opens a file for reading
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        FileStream OpenRead(
            string strPath
            );

        //===================================================================================================
        /// <summary>
        /// Opens a file for writing
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        FileStream OpenWrite(
            string path
            );


    }
}
