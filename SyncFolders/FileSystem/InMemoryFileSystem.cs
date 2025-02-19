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
        public Dictionary<string, MemoryStreamWithErrors> m_oFiles = 
            new Dictionary<string, MemoryStreamWithErrors>();
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
        /// Contains list of errors to simulate
        /// </summary>
        private Dictionary<string, List<long>> m_oSimulatedReadErrors = new Dictionary<string, List<long>>();


        //===================================================================================================
        /// <summary>
        /// Constructs a new In-Memory File System
        /// </summary>
        //===================================================================================================
        public InMemoryFileSystem()
        {
            // Initialize the root directory
            // m_oDirectories.Add("\\", new InMemoryDirectoryInfo("\\", this));
        }


        //===================================================================================================
        /// <summary>
        /// Constructs a new In-Memory File System with a list of simulated errors
        /// </summary>
        /// <param name="oSimulatedReadErrors">List of simulated errors for each file</param>
        //===================================================================================================
        public InMemoryFileSystem(
            Dictionary<string, List<long>> oSimulatedReadErrors
            )
        {
            // Initialize the root directory
            // m_oDirectories.Add("\\", new InMemoryDirectoryInfo("\\", this));

            SetSimulatedReadErrors(oSimulatedReadErrors);
        }


        //===================================================================================================
        /// <summary>
        /// Sets read errors to simulate
        /// </summary>
        /// <param name="oSimulatedReadErrors">List of simulated errors for each file</param>
        //===================================================================================================
        public void SetSimulatedReadErrors(
            Dictionary<string, List<long>> oSimulatedReadErrors
            )
        {
            // copy file names in upper case
            foreach (string strFilePath in oSimulatedReadErrors.Keys)
            {
                m_oSimulatedReadErrors[strFilePath.ToUpper()] =
                    oSimulatedReadErrors[strFilePath];

                if (m_oFiles.ContainsKey(strFilePath))
                    m_oFiles[strFilePath].SetSimulatedErrors(oSimulatedReadErrors[strFilePath]);
            }
        }


        //===================================================================================================
        /// <summary>
        /// If the file is in the list and one of specified positions is in the list then throws an
        /// intentional I/O exception
        /// </summary>
        /// <param name="strFilePath">Path of the file</param>
        /// <param name="lStartPosition">Start position of read</param>
        /// <param name="lLength">Intended read length</param>
        //===================================================================================================
        private void ThrowSimulatedReadErrorIfNeeded(
            string strFilePath,
            long lStartPosition,
            long lLength
            )
        {
            // we compare in upper case
            string strFilePathUpper = strFilePath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strFilePathUpper))
            {
                // there is such a file, let's see if this read hits one of the mines
                long lEndPosition = lStartPosition + lLength;
                foreach (long lErrorPosition in m_oSimulatedReadErrors[strFilePathUpper])
                {
                    // we simulate a complete range of 4096 bytes from each position
                    if (lErrorPosition + 4095 >= lStartPosition && lErrorPosition < lEndPosition)
                    {
                        throw new IOException(
                            string.Format(Properties.Resources.ThisIsASimulatedIOErrorAtPosition,
                               SyncFolders.FormSyncFolders.FormatNumber(lErrorPosition)));

                        //throw new IOException("This is a simulated I/O error for testing");
                    }
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// If the file is in the list and one of specified positions is in the list then throws an
        /// intentional I/O exception
        /// </summary>
        /// <param name="fi">Path of the file</param>
        /// <param name="lStartPosition">Start position of read</param>
        /// <param name="lLength">Intended read length</param>
        //===================================================================================================
        private void ThrowSimulatedReadErrorIfNeeded(
            IFileInfo fi,
            long lStartPosition,
            long lLength
            )
        {
            ThrowSimulatedReadErrorIfNeeded(fi.FullName, lStartPosition, lLength);
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file for write
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        public IFile OpenWrite(string strPath)
        {
            TestDirectoryExists(Path.GetDirectoryName(strPath));
            lock (m_oFiles)
            {
                if (!m_oFiles.ContainsKey(strPath))
                {
                    m_oFiles[strPath] = new MemoryStreamWithErrors(new List<long>());
                    m_oFileWriteTimes[strPath] = DateTime.UtcNow;
                }
                return new InMemoryFile(m_oFiles[strPath], m_oFileWriteTimes, strPath);
            }
        }



        //===================================================================================================
        /// <summary>
        /// Creates a file and opens it for write
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        public IFile Create(string strPath)
        {
            lock (m_oFiles)
            {
                // if there is already a file, then dispose it
                if (m_oFiles.ContainsKey(strPath))
                {
                    m_oFiles[strPath].Dispose();
                }
                m_oFiles[strPath] = new MemoryStreamWithErrors(new List<long>());
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
        public IFile OpenRead(string strPath)
        {
            if (!m_oFiles.ContainsKey(strPath))
            {
                throw new FileNotFoundException("File not found in memory.");
            }
            return new InMemoryFile(m_oFiles[strPath], m_oFileWriteTimes, strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Copies a file from source to destination inside the in-memory file system
        /// </summary>
        /// <param name="strSourcePath">Path to copy from</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        public void CopyFile(string strSourcePath, string strDestinationPath)
        {
            TestDirectoryExists(Path.GetDirectoryName(strDestinationPath));
            if (m_oFiles.ContainsKey(strSourcePath))
            {
                ThrowSimulatedReadErrorIfNeeded(strSourcePath, 0, m_oFiles.ContainsKey(strSourcePath) ? m_oFiles[strSourcePath].Length : 0);
                m_oFiles[strDestinationPath] = new MemoryStreamWithErrors(m_oFiles[strSourcePath].ToArray(), new List<long>());
                m_oFileWriteTimes[strDestinationPath] = m_oFileWriteTimes[strSourcePath];
            }
            else
            {
                throw new FileNotFoundException("Source file not found in memory.");
            }
        }


        //===================================================================================================
        /// <summary>
        /// Copies a file from source to destination inside the in-memory file system
        /// </summary>
        /// <param name="strSourcePath">Path to copy from a real file system</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        public void CopyFileFromReal(string strSourcePath, string strDestinationPath)
        {
            TestDirectoryExists(Path.GetDirectoryName(strDestinationPath));
            using (FileStream s = File.OpenRead(strSourcePath))
            {
                byte[] buffer = new byte[s.Length];
                m_oFiles[strDestinationPath] = new MemoryStreamWithErrors(buffer, new List<long>());
                m_oFileWriteTimes[strDestinationPath] = File.GetLastWriteTimeUtc(strSourcePath);
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

            MemoryStreamWithErrors oStream = new MemoryStreamWithErrors(new List<long>());

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
            string[] astrDirectories = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string strCurrentPath = "";
            foreach (string strDirectory in astrDirectories)
            {
                if (strCurrentPath.Length == 0)
                {
                    strCurrentPath = strDirectory;
                    if (!strCurrentPath.EndsWith("\\"))
                        strCurrentPath = strCurrentPath + "\\";
                }
                else
                {
                    strCurrentPath = Path.Combine(strCurrentPath, strDirectory);
                }
                if (!m_oDirectories.ContainsKey(strCurrentPath))
                {
                    m_oDirectories.Add(strCurrentPath, null);
                    // the direcory must exist before we create the object
                    m_oDirectories[strCurrentPath] = new InMemoryDirectoryInfo(strCurrentPath, this);
                }
            }
        }



        //===================================================================================================
        /// <summary>
        /// Creates buffered stream object, if in real file, or does nothing for in-memory file
        /// </summary>
        /// <param name="iFile">IFile object</param>
        /// <returns>IFile object</returns>
        //===================================================================================================
        public IFile CreateBufferedStream(
            IFile iFile,
            int nBufferLength
            )
        {
            return iFile;
        }


        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        //===================================================================================================
        public IFileInfo CopyTo(
            IFileInfo fi,
            string strDestFileName
            )
        {
            ThrowSimulatedReadErrorIfNeeded(fi, 0, fi.Exists ? fi.Length : 0);
            return fi.CopyTo(strDestFileName);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        /// <param name="bOverwrite">Indicates, if the file shall be overwritten, if exists</param>
        //===================================================================================================
        public IFileInfo CopyTo(
            IFileInfo fi,
            string strDestFileName,
            bool bOverwrite
            )
        {
            return fi.CopyTo(strDestFileName, bOverwrite);
        }


        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public IFile Open(
            string strPath,
            FileMode eMode
            )
        {
            switch (eMode)
            {
                case FileMode.Append:
                    {
                        IFile s = OpenWrite(strPath);
                        s.Seek(0, SeekOrigin.End);
                        return s;
                    }

                case FileMode.Create:
                    return Create(strPath);

                case FileMode.CreateNew:
                    return Create(strPath);

                case FileMode.Truncate:
                    return Create(strPath);

                case FileMode.Open:
                    return OpenRead(strPath);

                default:
                case FileMode.OpenOrCreate:
                    return OpenWrite(strPath);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <param name="eAccess">Access</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public IFile Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess
            )
        {
            return Open(strPath, eMode);
        }

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
        public IFile Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess,
            FileShare eShare
            )
        {
            return Open(strPath, eMode);
        }


        //===================================================================================================
        /// <summary>
        /// This method is used for notification of the simulator that it shall clear error list of a file
        /// because it has been replaced by another one. Also it physically deletes the file.
        /// </summary>
        /// <param name="strFilePath">Path of the file</param>
        //===================================================================================================
        public void Delete(
            string strFilePath
            )
        {
        }

        //===================================================================================================
        /// <summary>
        /// This method is used for notification of the simulator that it shall clear error list of a file
        /// because it has been replaced by another one. Also it physically deletes the file.
        /// </summary>
        /// <param name="fi">Path of the file</param>
        //===================================================================================================
        public void Delete(
            IFileInfo fi
            )
        {
        }


        //===================================================================================================
        /// <summary>
        /// Sets last write time
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="dtmLastWriteTimeUtc">Last write time</param>
        //===================================================================================================
        public void SetLastWriteTimeUtc(
            string strPath,
            DateTime dtmLastWriteTimeUtc
            )
        {
            m_oFileWriteTimes[strPath] = dtmLastWriteTimeUtc;
        }


        //===================================================================================================
        /// <summary>
        /// Gets last write time
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        //===================================================================================================
        public DateTime GetLastWriteTimeUtc(
            string strPath
            )
        {
            if (m_oFileWriteTimes.ContainsKey(strPath))
                return m_oFileWriteTimes[strPath];
            else
                throw new IOException("File not present in memory");
        }


        //===================================================================================================
        /// <summary>
        /// Gets information, if file exits in file system
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>true iff file exists</returns>
        //===================================================================================================
        public bool Exists(
            string strPath
            )
        {
            return m_oFiles.ContainsKey(strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Gets attribues of the file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        //===================================================================================================
        public FileAttributes GetAttributes(
            string strPath
            )
        {
            return FileAttributes.Archive;
        }


        //===================================================================================================
        /// <summary>
        /// Gets attribues of the file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eNewAttributes">New attributes to set</param>
        //===================================================================================================
        public void SetAttributes(
            string strPath,
            FileAttributes eNewAttributes
            )
        {
            // ignore
        }


        //***************************************************************************************************
        /// <summary>
        /// Objects of this class provide means for simulation of read errors
        /// </summary>
        //***************************************************************************************************
        public class MemoryStreamWithErrors : MemoryStream
        {
            /// <summary>
            /// List of errors to simulate
            /// </summary>
            List<long> m_aListOfReadErrors;

            //===============================================================================================
            /// <summary>
            /// Constructs a new FileStreamWithErrors for simulation of reading errors
            /// </summary>
            /// <param name="aListOfReadErrors">List of errors to simulate</param>
            //===============================================================================================
            public MemoryStreamWithErrors(
                List<long> aListOfReadErrors
                )
                : base()
            {
                m_aListOfReadErrors = aListOfReadErrors;
            }


            //===============================================================================================
            /// <summary>
            /// Constructs a new FileStreamWithErrors for simulation of reading errors
            /// </summary>
            /// <param name="aBuffer">The data of the file</param>
            /// <param name="aListOfReadErrors">List of errors to simulate</param>
            //===============================================================================================
            public MemoryStreamWithErrors(
                byte [] aBuffer,
                List<long> aListOfReadErrors
                )
                : base(aBuffer)
            {
                m_aListOfReadErrors = aListOfReadErrors;
            }


            //===============================================================================================
            /// <summary>
            /// Reads from file, or throws an intentional I/O error at specific position
            /// </summary>
            /// <param name="aArray">Array for storing read data</param>
            /// <param name="nOffset">Offset inside the array</param>
            /// <param name="nCount">Count of bytes to read</param>
            /// <returns>The counnt of bytes actually read</returns>
            //===============================================================================================
            public override int Read(byte[] aArray, int nOffset, int nCount)
            {
                // simulate reading errors
                if (m_aListOfReadErrors.Count > 0)
                {
                    long lCurrentPos = Position;
                    for (int i = m_aListOfReadErrors.Count - 1; i >= 0; --i)
                    {
                        if (m_aListOfReadErrors[i] + 4095 >= lCurrentPos &&
                            m_aListOfReadErrors[i] < lCurrentPos + nCount)
                        {
                            throw new IOException(
                                string.Format(Properties.Resources.ThisIsASimulatedIOErrorAtPosition,
                                   SyncFolders.FormSyncFolders.FormatNumber(m_aListOfReadErrors[i])));
                        }
                    }
                }
                return base.Read(aArray, nOffset, nCount);
            }

            //===============================================================================================
            /// <summary>
            /// Reads a byte from file, or throws an intentional I/O exception at specific position
            /// </summary>
            /// <returns>Read byte, or -1 if end of file</returns>
            //===============================================================================================
            public override int ReadByte()
            {
                // simulate reading errors
                if (m_aListOfReadErrors.Count > 0)
                {
                    long lCurrentPos = Position;
                    for (int i = m_aListOfReadErrors.Count - 1; i >= 0; --i)
                    {
                        if (m_aListOfReadErrors[i] + 4095 >= lCurrentPos &&
                            m_aListOfReadErrors[i] <= lCurrentPos)
                        {
                            throw new IOException(
                                string.Format(Properties.Resources.ThisIsASimulatedIOErrorAtPosition,
                                SyncFolders.FormSyncFolders.FormatNumber(m_aListOfReadErrors[i])));
                        }
                    }
                }
                return base.ReadByte();
            }


            //===============================================================================================
            /// <summary>
            /// Writes data to the destination file. If a simulated error location is overwritten then it
            /// disappears from the list
            /// </summary>
            /// <param name="aArray">Source data to write</param>
            /// <param name="nOffset">Position inside the array</param>
            /// <param name="nCount">Count of bytes to write</param>
            //===============================================================================================
            public override void Write(byte[] aArray, int nOffset, int nCount)
            {
                if (m_aListOfReadErrors.Count > 0)
                {
                    // if the program overwrites the error then we simulate its
                    // disappearance
                    long lCurrentPos = Position;
                    for (int i = m_aListOfReadErrors.Count - 1; i >= 0; --i)
                    {
                        if (m_aListOfReadErrors[i] >= lCurrentPos &&
                            m_aListOfReadErrors[i] < lCurrentPos + nCount)
                        {
                            m_aListOfReadErrors.RemoveAt(i);
                        }
                    }
                }
                base.Write(aArray, nOffset, nCount);
            }

            //===============================================================================================
            /// <summary>
            /// Writes a byte to the file. If a simulated error location is overwritten then it disappears
            /// </summary>
            /// <param name="byValue">The byte to write</param>
            //===============================================================================================
            public override void WriteByte(byte byValue)
            {
                if (m_aListOfReadErrors.Count > 0)
                {
                    // if the program overwrites the error then we simulate its
                    // disappearance
                    long lCurrentPos = Position;
                    for (int i = m_aListOfReadErrors.Count - 1; i >= 0; --i)
                    {
                        if (m_aListOfReadErrors[i] == lCurrentPos)
                        {
                            m_aListOfReadErrors.RemoveAt(i);
                        }
                    }
                }
                base.WriteByte(byValue);
            }


            //===============================================================================================
            /// <summary>
            /// Prevents closing of underlying MemoryStream
            /// </summary>
            /// <param name="disposing">ignored</param>
            //===============================================================================================
            protected override void Dispose(bool disposing)
            {
                // does nothing
            }

            //===============================================================================================
            /// <summary>
            /// Sets new list of simulated read errors
            /// </summary>
            /// <param name="aListOfReadErrors">New list of simulated read errors</param>
            //===============================================================================================
            public void SetSimulatedErrors(List<long> aListOfReadErrors)
            {
                m_aListOfReadErrors = aListOfReadErrors;
            }

        }
    }

}
