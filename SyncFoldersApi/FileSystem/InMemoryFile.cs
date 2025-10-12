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

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Provides access to an in-memory file
    /// </summary>
    //*******************************************************************************************************
    public class InMemoryFile : IFile
    {
        //===================================================================================================
        /// <summary>
        /// Memory stream of the file
        /// </summary>
        private MemoryStream m_oStream;
        //===================================================================================================
        /// <summary>
        /// File write times UTC from file system
        /// </summary>
        private Dictionary<string, DateTime> m_oFileWriteTimes;
        //===================================================================================================
        /// <summary>
        /// Path of current file
        /// </summary>
        private string m_strPath;
        //===================================================================================================
        /// <summary>
        /// Indicates if file has been closed
        /// </summary>
        private bool m_bClosed;
        //===================================================================================================
        /// <summary>
        /// Holds the position inside the file
        /// </summary>
        private long m_lPosition;
        //===================================================================================================
        /// <summary>
        /// Indicates if the file has been opened read-only
        /// </summary>
        private bool m_bReadOnly;

        //===================================================================================================
        /// <summary>
        /// Constructs a new In-Memory File object
        /// </summary>
        /// <param name="oStream">In-Memory stream of the file</param>
        /// <param name="oFileWriteTimes">Information about file write times from file system</param>
        /// <param name="strPath">Path of this file</param>
        /// <param name="bReadOnly">Indicates if the file shall be opened read-only</param>
        //===================================================================================================
        public InMemoryFile(
            MemoryStream oStream, 
            Dictionary<string, DateTime> oFileWriteTimes, 
            string strPath, 
            bool bReadOnly)
        {
            m_oStream = oStream;
            m_lPosition = 0;
            m_oFileWriteTimes = oFileWriteTimes;
            m_strPath = strPath;
            m_bReadOnly = bReadOnly;
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
                if (m_bClosed)
                {
                    throw new ObjectDisposedException(m_strPath);
                }

                return m_lPosition;
            }
            set
            {
                if (m_bClosed)
                {
                    throw new ObjectDisposedException(m_strPath);
                }

                m_oStream.Position = value;
                m_lPosition = m_oStream.Position;
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
            if (m_bClosed)
            {
                throw new ObjectDisposedException(m_strPath);
            }

            if (m_bReadOnly)
            {
                throw new IOException("File has been opened in read-only mode");
            }

            m_oStream.Position = m_lPosition;
            try
            {
                m_oStream.Write(aBuffer, nOffset, nCount);
                m_oFileWriteTimes[m_strPath] = DateTime.UtcNow;
            }
            finally
            {
                m_lPosition = m_oStream.Position;
            }

        }


        //===================================================================================================
        /// <summary>
        /// Writes a byte to the file
        /// </summary>
        /// <param name="by">Byte to write</param>
        //===================================================================================================
        public void WriteByte(
            byte by
            )
        {
            if (m_bClosed)
            {
                throw new ObjectDisposedException(m_strPath);
            }

            if (m_bReadOnly)
            {
                throw new IOException("File has been opened in read-only mode");
            }

            m_oStream.Position = m_lPosition;
            try
            {
                m_oStream.WriteByte(by);
                m_oFileWriteTimes[m_strPath] = DateTime.UtcNow;
            }
            finally
            {
                m_lPosition = m_oStream.Position;
            }
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
            if (m_bClosed)
            {
                throw new ObjectDisposedException(m_strPath);
            }

            m_oStream.Position = m_lPosition;
            try
            {
                return m_oStream.Read(aBuffer, nOffset, nCount);
            }
            finally
            {
                m_lPosition = m_oStream.Position;
            }
        }


        //===================================================================================================
        /// <summary>
        /// Reads a byte from the file
        /// </summary>
        //===================================================================================================
        public int ReadByte()
        {
            if (m_bClosed)
            {
                throw new ObjectDisposedException(m_strPath);
            }

            m_oStream.Position = m_lPosition;
            try
            {
                return m_oStream.ReadByte();
            }
            finally
            {
                m_lPosition = m_oStream.Position;
            }
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
            if (m_bClosed)
            {
                throw new ObjectDisposedException(m_strPath);
            }

            try
            {
                m_oStream.Seek(lOffset, eOrigin);
            }
            finally
            {
                m_lPosition = m_oStream.Position;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Closes the file
        /// </summary>
        //===================================================================================================
        public void Close()
        {
            if (m_bClosed)
            {
                throw new ObjectDisposedException(m_strPath);
            }

            m_bClosed = true;
            // does nothing
            //m_oStream.Close();
        }

        //===================================================================================================
        /// <summary>
        /// Closes the file
        /// </summary>
        //===================================================================================================
        public void Dispose()
        {
            // doees nothing
        }

        //===================================================================================================
        /// <summary>
        /// Flushes buffers
        /// </summary>
        //===================================================================================================
        public void Flush()
        {
            // does nothing, since we are in memory
        }

        //===================================================================================================
        /// <summary>
        /// Returns length
        /// </summary>
        //===================================================================================================
        public long Length
        {
            get
            {
                return m_oStream.Length;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Returns contents as array
        /// </summary>
        /// <returns>Contents as byte array</returns>
        //===================================================================================================
        public byte[] ToArray()
        {
            return m_oStream.ToArray();
        }

    }
}
