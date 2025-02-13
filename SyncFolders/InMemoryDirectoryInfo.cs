using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
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
        private readonly bool m_bExists;

        //===================================================================================================
        /// <summary>
        /// Constructs a new in-memory directory info object
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <param name="oFs">File system for operations</param>
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
                foreach (KeyValuePair<string, MemoryStream> oFile in m_oFs.m_oFiles)
                {
                    if (oFile.Key.StartsWith(m_strPath))
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
                    if (oDirectory.Key.StartsWith(m_strPath))
                    {
                        aDirectoriesInDirectory.Add(oDirectory.Value);
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
                return FileAttributes.Archive;
            }
            set
            {
                // ignore
            }
        }

    }

}
