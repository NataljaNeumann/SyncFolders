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
    /// Provides info about real files
    /// </summary>
    //*******************************************************************************************************
    public class RealFileInfo : IFileInfo
    {
        //===================================================================================================
        /// <summary>
        /// The read file info
        /// </summary>
        private FileInfo m_oFileInfo;

        //===================================================================================================
        /// <summary>
        /// Constructs a new RealFileInfo object
        /// </summary>
        /// <param name="oFileInfo">Real file info</param>
        //===================================================================================================
        public RealFileInfo(FileInfo oFileInfo)
        {
            m_oFileInfo = oFileInfo;
        }

        //===================================================================================================
        /// <summary>
        /// Gets or sets last write time
        /// </summary>
        public DateTime LastWriteTimeUtc
        {
            get
            {
                return m_oFileInfo.LastWriteTimeUtc;
            }
            set
            {
                m_oFileInfo.LastWriteTimeUtc = value;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Provides information, if the file exists
        /// </summary>
        public bool Exists
        {
            get
            {
                return m_oFileInfo.Exists;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Gets the length of the file
        /// </summary>
        public long Length
        {
            get
            {
                return m_oFileInfo.Length;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Gets the path of the file
        /// </summary>
        public string FullName
        {
            get
            {
                return m_oFileInfo.FullName;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Deletes the file
        /// </summary>
        //===================================================================================================
        public void Delete()
        {
            m_oFileInfo.Delete();
        }


        //===================================================================================================
        /// <summary>
        /// Deletes the file
        /// </summary>
        /// <param name="strNewPath">New path for the file</param>
        //===================================================================================================
        public void MoveTo(
            string strNewPath
            )
        {
            m_oFileInfo.MoveTo(strNewPath);
        }

        //===================================================================================================
        /// <summary>
        /// Copies the file
        /// </summary>
        /// <param name="strDestPath">Destination path for the file</param>
        /// <param name="bOverwrite">Specifies, if the file shall be overwritten</param>
        //===================================================================================================
        public IFileInfo CopyTo(
            string strDestPath,
            bool bOverwrite
            )
        {
            return new RealFileInfo(m_oFileInfo.CopyTo(strDestPath,bOverwrite));
        }

        //===================================================================================================
        /// <summary>
        /// Copies the file
        /// </summary>
        /// <param name="strDestPath">Destination path for the file</param>
        //===================================================================================================
        public IFileInfo CopyTo(
            string strDestPath
            )
        {
            return new RealFileInfo(m_oFileInfo.CopyTo(strDestPath));
        }
        //===================================================================================================
        /// <summary>
        /// Name of the file
        /// </summary>
        //===================================================================================================
        public string Name
        {
            get
            {
                return m_oFileInfo.Name;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Extension of the file name
        /// </summary>
        //===================================================================================================
        public string Extension
        {
            get
            {
                return m_oFileInfo.Extension;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Directory of the file
        /// </summary>
        //===================================================================================================        
        public string DirectoryName
        {
            get
            {
                // we assume that we will always have directory specificaions
#pragma warning disable CS8603
                return m_oFileInfo.DirectoryName;
#pragma warning restore CS8603
            }
        }

        //===================================================================================================
        /// <summary>
        /// Directory of the file
        /// </summary>
        //===================================================================================================        
        public IDirectoryInfo Directory
        {
            get
            {
                // we assume that we will always have directory specificaions
#pragma warning disable CS8604
                return new RealDirectoryInfo(m_oFileInfo.Directory);
#pragma warning restore CS8604
            }
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
                return m_oFileInfo.Attributes;
            }
            set
            {
                m_oFileInfo.Attributes = value;
            }
        }
    }
}
