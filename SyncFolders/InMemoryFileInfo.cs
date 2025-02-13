using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
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
        private readonly bool m_bExists;

        //===================================================================================================
        /// <summary>
        /// Contains information about the length of the file, at the time when the object was created
        /// </summary>
        private readonly long m_lLength;

        //===================================================================================================
        /// <summary>
        /// Contains full name of the file
        /// </summary>
        private readonly string m_strFullName;

        //===================================================================================================
        /// <summary>
        /// Constructs a new in-memory file info
        /// </summary>
        /// <param name="path">Path of the file</param>
        /// <param name="stream">File stream</param>
        /// <param name="fileWriteTimes">Information about file write times</param>
        //===================================================================================================
        public InMemoryFileInfo(string path, MemoryStream stream, InMemoryFileSystem oFS)
        {
            lock (m_oFs.m_oFileWriteTimes)
                m_bExists = m_oFs.m_oFileWriteTimes.ContainsKey(path);
            if (m_bExists)
                Attributes = FileAttributes.Archive;
            m_lLength = stream.Length;
            m_strFullName = path;
            m_oFs = oFS;
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
            lock (m_oFs.m_oFiles)
            {
                m_oFs.m_oFiles.Remove(FullName);
                lock (m_oFs.m_oFileWriteTimes)
                {
                    m_oFs.m_oFileWriteTimes.Remove(FullName);
                }
            }
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
                return Path.GetDirectoryName(FullName);
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
