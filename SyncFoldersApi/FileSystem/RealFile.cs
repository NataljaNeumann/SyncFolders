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
    /// Access to a real file
    /// </summary>
    //*******************************************************************************************************
    public class RealFile : IFile
    {
        //===================================================================================================
        /// <summary>
        /// Contains the actual file stream
        /// </summary>
        public Stream m_oStream;

        //===================================================================================================
        /// <summary>
        /// Constructs a new real file object
        /// </summary>
        /// <param name="oStream">The real stream object</param>
        //===================================================================================================
        public RealFile(
            Stream oStream
            )
        {
            m_oStream = oStream;
        }

        //===================================================================================================
        /// <summary>
        /// Gets or sets the position inside the file
        /// </summary>
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
            int nCount
            )
        {
            m_oStream.Write(aBuffer, nOffset, nCount);
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
            m_oStream.WriteByte(by);
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
            int nCount
            )
        {
            return m_oStream.Read(aBuffer, nOffset, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Reads a byte from the file
        /// </summary>
        //===================================================================================================
        public int ReadByte()
        {
            return m_oStream.ReadByte();
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
        /// Closes the file and disposes the object
        /// </summary>
        //===================================================================================================
        public void Dispose()
        {
            m_oStream.Dispose();
        }

        //===================================================================================================
        /// <summary>
        /// Flushes buffers
        /// </summary>
        //===================================================================================================
        public void Flush()
        {
            m_oStream.Flush();
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

    }
}
