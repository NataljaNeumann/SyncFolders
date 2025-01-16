using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects of this class introduce artifical errors when reading and copying files
    /// </summary>
    //*******************************************************************************************************
    class FileOpenAndCopyWithSimulatedErrors : IFileOpenAndCopyAbstraction
    {
        /// <summary>
        /// Contains list of errors to simulate
        /// </summary>
        private Dictionary<string, List<long>> m_oSimulatedReadErrors = new Dictionary<string,List<long>>();


        //===================================================================================================
        /// <summary>
        /// Constructs a new simulator with error list
        /// </summary>
        /// <param name="oSimulatedReadErrors">List of simulated errors for each file</param>
        //===================================================================================================
        public FileOpenAndCopyWithSimulatedErrors(
            Dictionary<string, List<long>> oSimulatedReadErrors
            )
        {
            // copy file names in upper case
            foreach (string strFilePath in oSimulatedReadErrors.Keys)
            {
                m_oSimulatedReadErrors[strFilePath.ToUpper()] =
                    oSimulatedReadErrors[strFilePath];
            }
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
            // we compare in upper case
            string strFilePathUpper = strFilePath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strFilePathUpper))
            {
                m_oSimulatedReadErrors[strFilePathUpper].Clear();
            }
            File.Delete(strFilePath);
        }

        //===================================================================================================
        /// <summary>
        /// This method is used for notification of the simulator that it shall clear error list of a file
        /// because it has been replaced by another one. Also it physically deletes the file.
        /// </summary>
        /// <param name="fi">Path of the file</param>
        //===================================================================================================
        public void Delete(
            FileInfo fi
            )
        {
            // we compare in upper case
            string strFilePathUpper = fi.FullName.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strFilePathUpper))
            {
                m_oSimulatedReadErrors[strFilePathUpper].Clear();
            }
            fi.Delete();
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
                            string.Format(Resources.ThisIsASimulatedIOErrorAtPosition,
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
            FileInfo fi,
            long lStartPosition,
            long lLength
            )
        {
            ThrowSimulatedReadErrorIfNeeded(fi.FullName, lStartPosition, lLength);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        //===================================================================================================
        public FileInfo CopyTo(
            FileInfo fi, 
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
        public FileInfo CopyTo(
            FileInfo fi, 
            string strDestFileName, 
            bool bOverwrite
            )
        {
            ThrowSimulatedReadErrorIfNeeded(fi, 0, fi.Exists ? fi.Length : 0);
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
        public FileStream Open(
            string strPath,
            FileMode eMode
            )
        {
            string strPathUpper = strPath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strPathUpper))
            {
                switch (eMode)
                {
                    case FileMode.Append:
                    case FileMode.Create:
                    case FileMode.CreateNew:
                    case FileMode.Truncate:
                        return File.Open(strPath, eMode);
                    default:
                        return new FileStreamWithErrors(strPath,
                            eMode, FileAccess.ReadWrite, FileShare.Read, 
                            m_oSimulatedReadErrors[strPathUpper]);
                }
            }
            else
            {
                return File.Open(strPath, eMode);
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
        public FileStream Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess
            )
        {
            string strPathUpper = strPath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strPathUpper))
            {
                return new FileStreamWithErrors(strPath,
                    eMode, eAccess, FileShare.Read, m_oSimulatedReadErrors[strPathUpper]);
            }
            else
            {
                return File.Open(strPath, eMode, eAccess);
            }
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
        public FileStream Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess,
            FileShare eShare
            )
        {
            string strPathUpper = strPath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strPathUpper))
            {
                return new FileStreamWithErrors(strPath,
                    eMode, eAccess, eShare, m_oSimulatedReadErrors[strPathUpper]);
            }
            else
            {
                return File.Open(strPath, eMode, eAccess, eShare);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file for reading
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public FileStream OpenRead(
            string strPath
            )
        {
            string strPathUpper = strPath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strPathUpper))
            {
                return new FileStreamWithErrors(strPath, 
                    FileMode.Open, FileAccess.Read, FileShare.Read, m_oSimulatedReadErrors[strPathUpper]);
            }
            else
            {
                return File.OpenRead(strPath);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Opens a file for writing
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public FileStream OpenWrite(
            string strPath
            )
        {
            string strPathUpper = strPath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strPathUpper))
            {
                return new FileStreamWithErrors(strPath,
                    FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 
                    m_oSimulatedReadErrors[strPathUpper]);
            }
            else
            {
                return File.OpenWrite(strPath);
            }
        }

        //***************************************************************************************************
        /// <summary>
        /// Objects of this class provide means for simulation of read errors
        /// </summary>
        //***************************************************************************************************
        private class FileStreamWithErrors : FileStream
        {
            /// <summary>
            /// List of errors to simulate
            /// </summary>
            List<long> m_aListOfReadErrors;

            //===============================================================================================
            /// <summary>
            /// Constructs a new FileStreamWithErrors for simulation of reading errors
            /// </summary>
            /// <param name="strPath">File to open</param>
            /// <param name="eMode">Open mode</param>
            /// <param name="eAccess">File access type</param>
            /// <param name="eShare">File share type</param>
            /// <param name="aListOfReadErrors">List of errors to simulate</param>
            //===============================================================================================
            public FileStreamWithErrors(
                string strPath,
                FileMode eMode,
                FileAccess eAccess,
                FileShare eShare,
                List<long> aListOfReadErrors
                )
                :base(strPath, eMode, eAccess, eShare)
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
                                string.Format(Resources.ThisIsASimulatedIOErrorAtPosition,
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
                                string.Format(Resources.ThisIsASimulatedIOErrorAtPosition,
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
        }
    }
}
