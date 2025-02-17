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

namespace SyncFolders
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
        IFile OpenFileForWrite(string strPath);

        //===================================================================================================
        /// <summary>
        /// Opens a file for read
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        IFile OpenFileForRead(string strPath);

        //===================================================================================================
        /// <summary>
        /// Copies a file from source to destination
        /// </summary>
        /// <param name="strSourcePath">Path to copy from</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        void CopyFile(string strSourcePath, string strDestinationPath);


        //===================================================================================================
        /// <summary>
        /// Reads all contents of the file
        /// </summary>
        /// <param name="strPath">Path to read from</param>
        /// <returns>The content of the file</returns>
        //===================================================================================================
        string ReadFromFile(string strPath);


        //===================================================================================================
        /// <summary>
        /// Writes tontent to file
        /// </summary>
        /// <param name="strPath">The path of the file</param>
        /// <param name="strContent">Content to write</param>
        //===================================================================================================
        void WriteAllText(string strPath, string strContent);

        //===================================================================================================
        /// <summary>
        /// Searches file in a directory
        /// </summary>
        /// <param name="strSearchPattern">Directory</param>
        /// <returns>A list of files</returns>
        //===================================================================================================
        List<string> SearchFiles(string strSearchPattern);


        //===================================================================================================
        /// <summary>
        /// Renames a file
        /// </summary>
        /// <param name="strOldPath">Old path of an existing file</param>
        /// <param name="strNewPath">New path for the file</param>
        //===================================================================================================
        void Move(string strOldPath, string strNewPath);

        //===================================================================================================
        /// <summary>
        /// Gets information about a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        IFileInfo GetFileInfo(string strPath);


        //===================================================================================================
        /// <summary>
        /// Gets information about a directory
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        IDirectoryInfo GetDirectoryInfo(string strPath);
    }

}
