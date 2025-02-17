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
    }
}
