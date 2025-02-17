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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects of this class implement in-memory file systems
    /// </summary>
    //*******************************************************************************************************
    public class InMemoryFileSystem : IFileOperations
    {
        //===================================================================================================
        /// <summary>
        /// Files in file system
        /// </summary>
        public Dictionary<string, MemoryStream> m_oFiles = new Dictionary<string, MemoryStream>();
        //===================================================================================================
        /// <summary>
        /// File write times UTC
        /// </summary>
        public Dictionary<string, DateTime> m_oFileWriteTimes = new Dictionary<string, DateTime>();
        //===================================================================================================
        /// <summary>
        /// Directories in file system
        /// </summary>
        public Dictionary<string,IDirectoryInfo> m_oDirectories = new Dictionary<string,IDirectoryInfo>();


        //===================================================================================================
        /// <summary>
        /// Constructs a new In-Memory File System
        /// </summary>
        //===================================================================================================
        public InMemoryFileSystem()
        {
            // Initialize the root directory
            m_oDirectories.Add("\\", new InMemoryDirectoryInfo("\\", this));
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file for write
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        public IFile OpenFileForWrite(string strPath)
        {
            TestDirectoryExists(Path.GetDirectoryName(strPath));
            if (!m_oFiles.ContainsKey(strPath))
            {
                m_oFiles[strPath] = new MemoryStream();
                m_oFileWriteTimes[strPath] = DateTime.UtcNow;
            }
            return new InMemoryFile(m_oFiles[strPath], m_oFileWriteTimes, strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Opens a file for read
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        public IFile OpenFileForRead(string strPath)
        {
            if (!m_oFiles.ContainsKey(strPath))
            {
                throw new FileNotFoundException("File not found in memory.");
            }
            return new InMemoryFile(m_oFiles[strPath], m_oFileWriteTimes, strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Copies a file from source to destination
        /// </summary>
        /// <param name="strSourcePath">Path to copy from</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        public void CopyFile(string strSourcePath, string strDestinationPath)
        {
            TestDirectoryExists(Path.GetDirectoryName(strDestinationPath));
            if (m_oFiles.ContainsKey(strSourcePath))
            {
                m_oFiles[strDestinationPath] = new MemoryStream(m_oFiles[strSourcePath].ToArray());
                m_oFileWriteTimes[strDestinationPath] = m_oFileWriteTimes[strSourcePath];
            }
            else
            {
                throw new FileNotFoundException("Source file not found in memory.");
            }
        }

        //===================================================================================================
        /// <summary>
        /// Reads all contents of the file
        /// </summary>
        /// <param name="strPath">Path to read from</param>
        /// <returns>The content of the file</returns>
        //===================================================================================================
        public string ReadFromFile(string strPath)
        {
            if (m_oFiles.ContainsKey(strPath))
            {
                using (MemoryStream oStream = new MemoryStream(m_oFiles[strPath].ToArray()))
                using (StreamReader oReader = new StreamReader(oStream))
                    return oReader.ReadToEnd();
            }
            else
            {
                throw new FileNotFoundException("File not found in memory.");
            }
        }


        //===================================================================================================
        /// <summary>
        /// Writes tontent to file
        /// </summary>
        /// <param name="strPath">The path of the file</param>
        /// <param name="strContent">Content to write</param>
        //===================================================================================================
        public void WriteAllText(string strPath, string strContent)
        {
            TestDirectoryExists(Path.GetDirectoryName(strPath));
            MemoryStream oStream = new MemoryStream(m_oFiles[strPath].ToArray());

            using (StreamWriter oWriter = new StreamWriter(oStream))
                oWriter.Write(strContent);

            if (m_oFiles.ContainsKey(strPath))
                m_oFiles[strPath].Dispose();

            lock (m_oFiles)
                m_oFiles[strPath] = oStream;

            lock (m_oFileWriteTimes)
                m_oFileWriteTimes[strPath] = DateTime.UtcNow;
        }

        //===================================================================================================
        /// <summary>
        /// Searches file in a directory
        /// </summary>
        /// <param name="strSearchPattern">Directory</param>
        /// <returns>A list of files</returns>
        //===================================================================================================
        public List<string> SearchFiles(string strSearchPattern)
        {
            Regex oRegex = new Regex(".*\\\\"+
                (strSearchPattern
                    .Replace("\\", "\\\\")
                    .Replace(")", "\\)")
                    .Replace("(", "\\(")
                    .Replace("]", "\\]")
                    .Replace("]", "\\]")
                    .Replace(".", "\\.")
                    .Replace("}", "\\}")
                    .Replace("{", "\\{")
                    .Replace("*", ".*")
                    .Replace("?", ".+")) + "$");

            var matchingFiles = new List<string>();
            foreach (string strFilePath in m_oFiles.Keys)
            {
                if (oRegex.IsMatch(strFilePath))
                    matchingFiles.Add(strFilePath);
            }
            return matchingFiles;
        }

        //===================================================================================================
        /// <summary>
        /// Renames a file
        /// </summary>
        /// <param name="strOldPath">Old path of an existing file</param>
        /// <param name="strNewPath">New path for the file</param>
        //===================================================================================================
        public void Move(string strOldPath, string strNewPath)
        {
            TestDirectoryExists(Path.GetDirectoryName(strNewPath));
            lock (m_oFiles)
            if (m_oFiles.ContainsKey(strOldPath))
            {
                if (m_oFiles.ContainsKey(strNewPath))
                    m_oFiles[strNewPath].Dispose();

                m_oFiles[strNewPath] = m_oFiles[strOldPath];
                m_oFiles.Remove(strOldPath);
                lock (m_oFileWriteTimes)
                {
                    m_oFileWriteTimes[strNewPath] = m_oFileWriteTimes[strOldPath];
                    m_oFileWriteTimes.Remove(strOldPath);
                }
            }
            else
            {
                throw new FileNotFoundException("File not found in memory.");
            }
        }


        //===================================================================================================
        /// <summary>
        /// Gets information about a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        public IFileInfo GetFileInfo(string strPath)
        {
            if (m_oFiles.ContainsKey(strPath))
            {
                return new InMemoryFileInfo(strPath, m_oFiles[strPath], this);
            }
            else
            {
                return new InMemoryFileInfo(strPath, null, null);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Gets information about a directory
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        public IDirectoryInfo GetDirectoryInfo(string strPath)
        {
            return new InMemoryDirectoryInfo(strPath, this);
        }


        //===================================================================================================
        /// <summary>
        /// Gets information about a directory
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        public void TestDirectoryExists(string path)
        {
            if (!m_oDirectories.ContainsKey(path))
                throw new IOException("Directory "+path+" doesn't exist in memory");
        }

        //===================================================================================================
        /// <summary>
        /// Creates a directory with all parents
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        public void EnsureDirectoryExists(string path)
        {
            if (path == null) return;
            var aDirectories = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var strCurrentPath = "\\";
            foreach (var directory in aDirectories)
            {
                strCurrentPath = Path.Combine(strCurrentPath, directory);
                if (!m_oDirectories.ContainsKey(strCurrentPath))
                {
                    m_oDirectories.Add(strCurrentPath, new InMemoryDirectoryInfo(strCurrentPath, this));
                }
            }
        }
    }

}
