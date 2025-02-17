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
    /// Objects that implement this interface provide possibility to get informationn about directories
    /// </summary>
    //*******************************************************************************************************
    public interface IDirectoryInfo
    {
        //===================================================================================================
        /// <summary>
        /// Gets files inside the directory
        /// </summary>
        /// <returns>A list of files</returns>
        //===================================================================================================
        IFileInfo[] GetFiles();

        //===================================================================================================
        /// <summary>
        /// Gets subdirectories
        /// </summary>
        /// <returns>List of subdirectories</returns>
        //===================================================================================================
        IDirectoryInfo[] GetDirectories();

        //===================================================================================================
        /// <summary>
        /// Gets full name of the directory
        /// </summary>
        //===================================================================================================
        string FullName { get; }

        //===================================================================================================
        /// <summary>
        /// Gets name of the directory
        /// </summary>
        //===================================================================================================
        string Name{ get; }

        //===================================================================================================
        /// <summary>
        /// Gets parent directory
        /// </summary>
        //===================================================================================================
        IDirectoryInfo Parent{ get; }


        //===================================================================================================
        /// <summary>
        /// Does the directory exist?
        /// </summary>
        //===================================================================================================
        bool Exists { get; }


        //===================================================================================================
        /// <summary>
        /// Creates the directory
        /// </summary>
        //===================================================================================================
        void Create();

        //===================================================================================================
        /// <summary>
        /// Deletes the directory
        /// </summary>
        /// <param name="bIncludingContents">Indicates that the contents need to be deleted, as well</param>
        //===================================================================================================
        void Delete(
            bool bIncludingContents
            );

        //===================================================================================================
        /// <summary>
        /// Gets or sets directory attributes
        /// </summary>
        //===================================================================================================
        FileAttributes Attributes
        {
            get;
            set;
        }

    }
}
