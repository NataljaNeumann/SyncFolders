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

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide basic file functionality
    /// </summary>
    //*******************************************************************************************************
    public interface IFile: IDisposable
    {

        //===================================================================================================
        /// <summary>
        /// Gets or sets position inside the file
        /// </summary>
        //===================================================================================================
        long Position
        {
            get;
            set;
        }

        //===================================================================================================
        /// <summary>
        /// Writes to the file
        /// </summary>
        /// <param name="aBuffer">Buffer to write from</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to write</param>
        //===================================================================================================
        void Write(
            byte[] aBuffer, 
            int nOffset, 
            int nCount
            );


        //===================================================================================================
        /// <summary>
        /// Writes a byte to the file
        /// </summary>
        /// <param name="by">Byte to write</param>
        //===================================================================================================
        void WriteByte(
            byte by
            );


        //===================================================================================================
        /// <summary>
        /// Reads from file
        /// </summary>
        /// <param name="aBuffer">Buffer to read to</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to read</param>
        /// <returns>Count of bytes actually read</returns>
        //===================================================================================================
        int Read(
            byte[] aBuffer, 
            int nOffset, 
            int nCount
            );


        //===================================================================================================
        /// <summary>
        /// Reads a byte from the file
        /// </summary>
        //===================================================================================================
        int ReadByte();

        //===================================================================================================
        /// <summary>
        /// Seeks a position insider the file
        /// </summary>
        /// <param name="lOffset">Offset, in regards to the origin</param>
        /// <param name="eOrigin">Origin for the seek</param>
        //===================================================================================================
        void Seek(
            long lOffset, 
            System.IO.SeekOrigin eOrigin
            );

        //===================================================================================================
        /// <summary>
        /// Closes the file
        /// </summary>
        //===================================================================================================
        void Close();

        //===================================================================================================
        /// <summary>
        /// Flushes buffers
        /// </summary>
        //===================================================================================================
        void Flush();


        //===================================================================================================
        /// <summary>
        /// Returns length
        /// </summary>
        //===================================================================================================
        long Length
        {
            get;
        }
    }
}
