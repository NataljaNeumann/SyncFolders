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
    /// Objects that implement this interface provide information about files
    /// </summary>
    //*******************************************************************************************************
    public interface IFileInfo
    {
        //===================================================================================================
        /// <summary>
        /// Last write time
        /// </summary>
        //===================================================================================================
        DateTime LastWriteTimeUtc { get; set; }

        //===================================================================================================
        /// <summary>
        /// Does the file exist?
        /// </summary>
        //===================================================================================================
        bool Exists { get; }

        //===================================================================================================
        /// <summary>
        /// Length of the file
        /// </summary>
        //===================================================================================================
        long Length {get; }

        //===================================================================================================
        /// <summary>
        /// Full path of the file
        /// </summary>
        //===================================================================================================
        string FullName { get; }

        //===================================================================================================
        /// <summary>
        /// Deletes the file
        /// </summary>
        //===================================================================================================
        void Delete();


        //===================================================================================================
        /// <summary>
        /// Moves the file to a new location
        /// </summary>
        /// <param name="strNewPath">New path for the file</param>
        //===================================================================================================
        void MoveTo(string strNewPath);


        //===================================================================================================
        /// <summary>
        /// Copies the file
        /// </summary>
        /// <param name="strDestPath">Destination path for the file</param>
        /// <param name="bOverwrite">Specifies, if the file shall be overwritten</param>
        //===================================================================================================
        IFileInfo CopyTo(
            string strDestPath,
            bool bOverwrite
            );

        //===================================================================================================
        /// <summary>
        /// Copies the file
        /// </summary>
        /// <param name="strDestPath">Destination path for the file</param>
        //===================================================================================================
        IFileInfo CopyTo(
            string strDestPath
            );

        //===================================================================================================
        /// <summary>
        /// Name of the file
        /// </summary>
        //===================================================================================================
        string Name
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Extension of the file name
        /// </summary>
        //===================================================================================================
        string Extension
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Directory of the file
        /// </summary>
        //===================================================================================================        
        string DirectoryName
        {
            get;
        }


        //===================================================================================================
        /// <summary>
        /// Directory of the file
        /// </summary>
        //===================================================================================================        
        IDirectoryInfo Directory
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Gets or sets file attributes
        /// </summary>
        //===================================================================================================
        FileAttributes Attributes
        {
            get;
            set;
        }
    }

}
