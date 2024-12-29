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
    /// Object of this class provide information about single restores
    /// </summary>
    //*******************************************************************************************************
    class RestoreInfo
    {
        /// <summary>
        /// The position to write to
        /// </summary>
        public long Position = 0;
        /// <summary>
        /// The block to write. (Actual length needs to be calculated outside, based on
        /// position and file size
        /// </summary>
        public Block Data = Block.GetBlock();
        /// <summary>
        /// Indicates that this is non-recoverable area, so the block is empty
        /// to fill non-recoverable space.
        /// </summary>
        public bool NotRecoverableArea;

        //===================================================================================================
        /// <summary>
        /// Constructs a new restore info
        /// </summary>
        //===================================================================================================
        public RestoreInfo()
        {
        }

        //===================================================================================================
        /// <summary>
        /// Constructs a new restore info
        /// </summary>
        /// <param name="nPos">The position for restoring</param>
        /// <param name="oData">The data to write</param>
        /// <param name="bNotRecoverable">Indicates that this is an empty block for non-recoverabl
        /// area</param>
        //===================================================================================================
        public RestoreInfo(long nPos, Block oData, bool bNotRecoverable)
        {
            Position = nPos;
            // copy oData to a new block, so the original can be reused
            Block b = Data;
            for (int i=b.Length-1;i>=0;--i)
                b[i] = oData[i];
            NotRecoverableArea = bNotRecoverable;
        }
    }
}
