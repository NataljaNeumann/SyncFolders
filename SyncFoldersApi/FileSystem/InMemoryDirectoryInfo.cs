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
    /// Provides functionality for information about in-memory directories
    /// </summary>
    //*******************************************************************************************************
    public class InMemoryDirectoryInfo : IDirectoryInfo
    {
        //===================================================================================================
        /// <summary>
        /// The path of the directory
        /// </summary>
        private string m_strPath;

        //===================================================================================================
        /// <summary>
        /// In-Memory file system
        /// </summary>
        InMemoryFileSystem m_oFs;

        //===================================================================================================
        /// <summary>
        /// Indicates, if the directory existed when the object was created
        /// </summary>
        private bool m_bExists;

        //===================================================================================================
        /// <summary>
        /// Attributes of the directory, just for testing once
        /// </summary>
        private FileAttributes m_eAttributes = FileAttributes.Directory;

        //===================================================================================================
        /// <summary>
        /// Constructs a new in-memory directory info object
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <param name="oFs">File system for operations</param>
        //===================================================================================================
        public InMemoryDirectoryInfo(string strPath, InMemoryFileSystem oFs)
        {
            m_strPath = strPath;
            m_oFs = oFs;
            lock (oFs.m_oDirectories)
                m_bExists = oFs.m_oDirectories.ContainsKey(strPath);
        }

        //===================================================================================================
        /// <summary>
        /// Gets files inside the directory
        /// </summary>
        /// <returns>A list of files</returns>
        //===================================================================================================
        public IFileInfo[] GetFiles()
        {
            var oFilesInDirectory = new List<IFileInfo>();
            lock (m_oFs.m_oFiles)
            {
                foreach (KeyValuePair<string, InMemoryFileSystem.MemoryStreamWithErrors> oFile in m_oFs.m_oFiles)
                {
                    if (oFile.Key.StartsWith(m_strPath) && oFile.Key.IndexOf("\\", m_strPath.Length+1)<0)
                    {
                        oFilesInDirectory.Add(new InMemoryFileInfo(oFile.Key, oFile.Value, m_oFs));
                    }
                }
            }
            return oFilesInDirectory.ToArray();
        }

        //===================================================================================================
        /// <summary>
        /// Gets subdirectories
        /// </summary>
        /// <returns>List of subdirectories</returns>
        //===================================================================================================
        public IDirectoryInfo[] GetDirectories()
        {
            var aDirectoriesInDirectory = new List<IDirectoryInfo>();
            lock (m_oFs.m_oDirectories)
            {
                foreach (KeyValuePair<string, IDirectoryInfo> oDirectory in m_oFs.m_oDirectories)
                {
                    if (m_strPath.EndsWith("\\"))
                    {
                        if (oDirectory.Key.StartsWith(m_strPath) && 
                            oDirectory.Key.Length > m_strPath.Length + 1)
                        {
                            aDirectoriesInDirectory.Add(oDirectory.Value);
                        }
                    }
                    else
                    {
                        if (oDirectory.Key.StartsWith(m_strPath + "\\") && 
                            oDirectory.Key.Length > m_strPath.Length + 2)
                        {
                            aDirectoriesInDirectory.Add(oDirectory.Value);
                        }
                    }
                }
            }
            return aDirectoriesInDirectory.ToArray();
        }


        //===================================================================================================
        /// <summary>
        /// Gets full name of the directory
        /// </summary>
        //===================================================================================================
        public string FullName
        {
            get
            {
                return m_strPath;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Gets name of the directory
        /// </summary>
        //===================================================================================================
        public string Name
        {
            get
            {
                return new DirectoryInfo(m_strPath).Name;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Gets parent directory
        /// </summary>
        //===================================================================================================
        public IDirectoryInfo Parent
        {
            get
            {
                DirectoryInfo oParentDirectoryInfo = Directory.GetParent(m_strPath);
                if (oParentDirectoryInfo == null)
                    return null;
                string oParentPath = oParentDirectoryInfo.FullName;
                return oParentPath != null ? new InMemoryDirectoryInfo(oParentPath, m_oFs) : null;
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
                return m_bExists;
            }
        }


        //===================================================================================================
        /// <summary>
        /// Creates the directory
        /// </summary>
        //===================================================================================================
        public void Create()
        {
            m_oFs.EnsureDirectoryExists(m_strPath);
            m_bExists = true;
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
            IFileInfo[] aFiles = GetFiles();
            if (bIncludingContents)
            {
                foreach (IFileInfo fi in aFiles)
                    fi.Delete();
            }
            else
            {
                if (aFiles.Length > 0)
                    throw new IOException("Directory not empty");
            }
            m_oFs.m_oDirectories.Remove(m_strPath);
            m_bExists = false;
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
                return m_eAttributes;
            }
            set
            {
                m_eAttributes = FileAttributes.Directory | value;
            }
        }

    }

}
