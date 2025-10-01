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
    /// <summary>
    /// Provides information about a real directory
    /// </summary>
    public class RealDirectoryInfo : IDirectoryInfo
    {
        //===================================================================================================
        /// <summary>
        /// Contains real directory info
        /// </summary>
        private DirectoryInfo m_oDirectoryInfo;

        //===================================================================================================
        /// <summary>
        /// Constructs a new real directory info object
        /// </summary>
        /// <param name="oDirectoryInfo">Real directory info</param>
        //===================================================================================================
        public RealDirectoryInfo(DirectoryInfo oDirectoryInfo)
        {
            m_oDirectoryInfo = oDirectoryInfo;
        }

        //===================================================================================================
        /// <summary>
        /// Gets files inside the directory
        /// </summary>
        /// <returns>A list of files</returns>
        //===================================================================================================
        public IFileInfo[] GetFiles()
        {
            var files = m_oDirectoryInfo.GetFiles();
            var fileList = new List<IFileInfo>();
            foreach (var file in files)
            {
                fileList.Add( new RealFileInfo(file) );
            }
            return fileList.ToArray();
        }



        //===================================================================================================
        /// <summary>
        /// Gets subdirectories
        /// </summary>
        /// <returns>List of subdirectories</returns>
        //===================================================================================================
        public IDirectoryInfo[] GetDirectories()
        {
            var directories = m_oDirectoryInfo.GetDirectories();
            var directoryList = new List<IDirectoryInfo>();
            foreach (var directory in directories)
            {
                directoryList.Add( new RealDirectoryInfo(directory));
            }
            return directoryList.ToArray();
        }

        //===================================================================================================
        /// <summary>
        /// Gets full name of the directory
        /// </summary>
        public string FullName
        {
            get
            {
                return m_oDirectoryInfo.FullName;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Gets name of the directory
        /// </summary>
        public string Name
        {
            get
            {
                return m_oDirectoryInfo.Name;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Gets parent directory
        /// </summary>
        public IDirectoryInfo Parent
        {
            get
            {
                return m_oDirectoryInfo.Parent != null ? new RealDirectoryInfo(m_oDirectoryInfo.Parent) : null;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Does the directory exist?
        /// </summary>
        //===================================================================================================
        public bool Exists
        {
            get
            {
                return m_oDirectoryInfo.Exists;
            }
        }


        //===================================================================================================
        /// <summary>
        /// Creates the directory
        /// </summary>
        //===================================================================================================
        public void Create()
        {
            m_oDirectoryInfo.Create();
        }

        //===================================================================================================
        /// <summary>
        /// Deletes the directory
        /// </summary>
        /// <param name="bIncludingContents">Indicates that the contents need to be deleted, as well</param>
        //===================================================================================================
        public void Delete(
            bool bIncludingContents
            )
        {
            m_oDirectoryInfo.Delete(true);
        }


        //===================================================================================================
        /// <summary>
        /// Gets or sets directory attributes
        /// </summary>
        //===================================================================================================
        public FileAttributes Attributes
        {
            get
            {
                return m_oDirectoryInfo.Attributes;
            }
            set
            {
                m_oDirectoryInfo.Attributes = value;
            }
        }

    }

}
