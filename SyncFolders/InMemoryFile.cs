using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Provides access to an in-memory file
    /// </summary>
    //*******************************************************************************************************
    public class InMemoryFile : IFile
    {
        //=======================================================================================================
        /// <summary>
        /// Memory stream of the file
        /// </summary>
        private MemoryStream m_oStream;
        //=======================================================================================================
        /// <summary>
        /// File write times UTC from file system
        /// </summary>
        private Dictionary<string, DateTime> m_oFileWriteTimes;
        //=======================================================================================================
        /// <summary>
        /// Path of current file
        /// </summary>
        private string m_strPath;

        //=======================================================================================================
        /// <summary>
        /// Constructs a new In-Memory File object
        /// </summary>
        /// <param name="oStream">In-Memory stream of the file</param>
        /// <param name="oFileWriteTimes">Information about file write times from file system</param>
        /// <param name="path">Path of this file</param>
        //=======================================================================================================
        public InMemoryFile(
            MemoryStream oStream, 
            Dictionary<string, DateTime> oFileWriteTimes, 
            string strPath)
        {
            m_oStream = oStream;
            m_oStream.Seek(0, SeekOrigin.Begin);
            m_oFileWriteTimes = oFileWriteTimes;
            m_strPath = strPath;
        }

        //===================================================================================================
        /// <summary>
        /// Gets or sets position inside the file
        /// </summary>
        //===================================================================================================
        public long Position
        {
            get
            {
                return m_oStream.Position;
            }
            set
            {
                m_oStream.Position = value;
            }
        }


        //===================================================================================================
        /// <summary>
        /// Writes to the file
        /// </summary>
        /// <param name="aBuffer">Buffer to write from</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to write</param>
        //===================================================================================================
        public void Write(
            byte[] aBuffer, 
            int nOffset, 
            int nCount)
        {
            m_oStream.Write(aBuffer, nOffset, nCount);
            m_oFileWriteTimes[m_strPath] = DateTime.UtcNow;
        }

        //===================================================================================================
        /// <summary>
        /// Reads from file
        /// </summary>
        /// <param name="aBuffer">Buffer to read to</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to read</param>
        /// <returns>Count of bytes actually read</returns>
        //===================================================================================================
        public int Read(
            byte[] aBuffer, 
            int nOffset, 
            int nCount)
        {
            return m_oStream.Read(aBuffer, nOffset, nCount);
        }

        //===================================================================================================
        /// <summary>
        /// Seeks a position insider the file
        /// </summary>
        /// <param name="lOffset">Offset, in regards to the origin</param>
        /// <param name="eOrigin">Origin for the seek</param>
        //===================================================================================================
        public void Seek(
            long lOffset, 
            SeekOrigin eOrigin)
        {
            m_oStream.Seek(lOffset, eOrigin);
        }

        //===================================================================================================
        /// <summary>
        /// Closes the file
        /// </summary>
        //===================================================================================================
        public void Close()
        {
            m_oStream.Close();
        }

        //===================================================================================================
        /// <summary>
        /// Closes the file
        /// </summary>
        //===================================================================================================
        public void Dispose()
        {
        }
    }
}
