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
    /// Provides information about  in-memory files
    /// </summary>
    //*******************************************************************************************************
    public class InMemoryFileInfo : IFileInfo
    {
        //===================================================================================================
        /// <summary>
        /// The in-memory file system for performing operations
        /// </summary>
        private readonly InMemoryFileSystem m_oFs;

        //===================================================================================================
        /// <summary>
        /// Contains information, if the file existed when the object was created
        /// </summary>
        private bool m_bExists;

        //===================================================================================================
        /// <summary>
        /// Contains information about the length of the file, at the time when the object was created
        /// </summary>
        private readonly long m_lLength;

        //===================================================================================================
        /// <summary>
        /// Contains full name of the file
        /// </summary>
        private string m_strFullName;

        //===================================================================================================
        /// <summary>
        /// Constructs a new in-memory file info
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="oStream">File stream</param>
        //===================================================================================================
        public InMemoryFileInfo(string strPath, MemoryStream? oStream, InMemoryFileSystem oFS)
        {

            m_oFs = oFS;
            lock (m_oFs.m_oFileWriteTimes)
                m_bExists = m_oFs.m_oFileWriteTimes.ContainsKey(strPath);
            if (m_bExists)
                Attributes = FileAttributes.Archive;
            m_lLength = oStream?.Length??0;

            m_strFullName = strPath;
        }

        //===================================================================================================
        /// <summary>
        /// Gets or sets last write time
        /// </summary>
        public DateTime LastWriteTimeUtc
        {
            get
            {
                lock (m_oFs.m_oFileWriteTimes)
                    return Exists ? m_oFs.m_oFileWriteTimes[FullName] : DateTime.MinValue;
            }
            set
            {
                lock (m_oFs.m_oFileWriteTimes)
                    if (m_oFs.m_oFileWriteTimes.ContainsKey(FullName))
                    {
                        m_oFs.m_oFileWriteTimes[FullName] = value;
                    }
            }
        }

        //===================================================================================================
        /// <summary>
        /// Does the file exist?
        /// </summary>
        //===================================================================================================
        public bool Exists
        {
            get
            {
                return m_bExists;
            }
        }


        //===================================================================================================
        /// <summary>
        /// Length of the file
        /// </summary>
        //===================================================================================================
        public long Length
        {
            get
            {
                return m_lLength;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Full path of the file
        /// </summary>
        //===================================================================================================
        public string FullName
        {
            get
            {
                return m_strFullName;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Deletes the file
        /// </summary>
        //===================================================================================================
        public void Delete()
        {
            m_oFs.Delete(this);

            m_bExists = false;
        }

        //===================================================================================================
        /// <summary>
        /// Moves the file to a new location
        /// </summary>
        /// <param name="strNewPath">New path for the file</param>
        //===================================================================================================
        public void MoveTo(
            string strNewPath
            )
        {
            m_oFs.Move(FullName, strNewPath);
            m_strFullName = strNewPath;
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
            if (!bOverwrite)
                lock (m_oFs.m_oFiles)
                    if (m_oFs.m_oFiles.ContainsKey(strDestPath))
                        throw new System.IO.IOException("File " + strDestPath + " already present in memory");
            m_oFs.CopyFile(FullName, strDestPath);
            return m_oFs.GetFileInfo(strDestPath);
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
            return CopyTo(strDestPath, false);
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
                return Path.GetFileName(FullName);
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
                return Path.GetExtension(FullName);
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
                // we ignore the case that there could be no directory specification
#pragma warning disable CS8603
                return Path.GetDirectoryName(FullName);
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
                return new InMemoryDirectoryInfo(DirectoryName, m_oFs);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Gets or sets file attributes
        /// </summary>
        //===================================================================================================
        public FileAttributes Attributes
        {
            get;
            set;
        }

    }

}
