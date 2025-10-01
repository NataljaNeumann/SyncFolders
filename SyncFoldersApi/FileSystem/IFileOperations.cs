/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024-2025 NataljaNeumann@gmx.de

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


using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide basic file system access functionality
    /// </summary>
    //*******************************************************************************************************
    public interface IFileOperations
    {
        //===================================================================================================
        /// <summary>
        /// Opens a file for write
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        IFile OpenWrite(
            string strPath
            );


        //===================================================================================================
        /// <summary>
        /// Creates a file and opens it for write
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        IFile Create(
            string strPath
            );

        //===================================================================================================
        /// <summary>
        /// Opens a file for read
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        IFile OpenRead(
            string strPath
            );

        //===================================================================================================
        /// <summary>
        /// Copies a file from source to destination inside the file system
        /// </summary>
        /// <param name="strSourcePath">Path to copy from</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        void CopyFile(
            string strSourcePath, 
            string strDestinationPath
            );

        //===================================================================================================
        /// <summary>
        /// Copies a file from real source to filesystem destination
        /// </summary>
        /// <param name="strSourcePath">Path to copy from a real file system</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        void CopyFileFromReal(
            string strSourcePath,
            string strDestinationPath
            );

        //===================================================================================================
        /// <summary>
        /// Reads all contents of the file
        /// </summary>
        /// <param name="strPath">Path to read from</param>
        /// <returns>The content of the file</returns>
        //===================================================================================================
        string ReadAllText(
            string strPath
            );


        //===================================================================================================
        /// <summary>
        /// Writes tontent to file
        /// </summary>
        /// <param name="strPath">The path of the file</param>
        /// <param name="strContent">Content to write</param>
        //===================================================================================================
        void WriteAllText(
            string strPath, 
            string strContent
            );

        //===================================================================================================
        /// <summary>
        /// Searches file in a directory
        /// </summary>
        /// <param name="strSearchPattern">Directory</param>
        /// <returns>A list of files</returns>
        //===================================================================================================
        List<string> SearchFiles(
            string strSearchPattern
            );


        //===================================================================================================
        /// <summary>
        /// Renames a file
        /// </summary>
        /// <param name="strOldPath">Old path of an existing file</param>
        /// <param name="strNewPath">New path for the file</param>
        //===================================================================================================
        void Move(
            string strOldPath, 
            string strNewPath
            );

        //===================================================================================================
        /// <summary>
        /// Gets information about a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        IFileInfo GetFileInfo(
            string strPath
            );


        //===================================================================================================
        /// <summary>
        /// Gets information about a directory
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        IDirectoryInfo GetDirectoryInfo(
            string strPath
            );


        //===================================================================================================
        /// <summary>
        /// Creates buffered stream object, if in real file, or does nothing for in-memory file
        /// </summary>
        /// <param name="iFile">IFile object</param>
        /// <returns>IFile object</returns>
        //===================================================================================================
        IFile CreateBufferedStream(
            IFile iFile,
            int nBufferLength
            );


        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        //===================================================================================================
        IFileInfo CopyTo(
            IFileInfo fi,
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
        IFileInfo CopyTo(
            IFileInfo fi,
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
        IFile Open(
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
        IFile Open(
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
        IFile Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess,
            FileShare eShare
            );


        //===================================================================================================
        /// <summary>
        /// This method is used for notification of the simulator that it shall clear error list of a file
        /// because it has been replaced by another one. Also it physically deletes the file.
        /// </summary>
        /// <param name="strFilePath">Path of the file</param>
        //===================================================================================================
        void Delete(
            string strFilePath
            );

        //===================================================================================================
        /// <summary>
        /// This method is used for notification of the simulator that it shall clear error list of a file
        /// because it has been replaced by another one. Also it physically deletes the file.
        /// </summary>
        /// <param name="fi">Path of the file</param>
        //===================================================================================================
        void Delete(
            IFileInfo fi
            );

        //===================================================================================================
        /// <summary>
        /// Sets last write time
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="dtmLastWriteTimeUtc">Last write time</param>
        //===================================================================================================
        void SetLastWriteTimeUtc(
            string strPath,
            DateTime dtmLastWriteTimeUtc
            );


        //===================================================================================================
        /// <summary>
        /// Gets last write time
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        //===================================================================================================
        DateTime GetLastWriteTimeUtc(
            string strPath
            );


        //===================================================================================================
        /// <summary>
        /// Gets information, if file exits in file system
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>true iff file exists</returns>
        //===================================================================================================
        bool Exists(
            string strPath
            );


        //===================================================================================================
        /// <summary>
        /// Gets attribues of the file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        //===================================================================================================
        FileAttributes GetAttributes(
            string strPath
            );


        //===================================================================================================
        /// <summary>
        /// Gets attribues of the file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eNewAttributes">New attributes to set</param>
        //===================================================================================================
        void SetAttributes(
            string strPath,
            FileAttributes eNewAttributes
            );

    }

}
