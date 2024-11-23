/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024 NataljaNeumann@gmx.de

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
    class RestoreInfo
    {
        public long Position = 0;
        public Block Data = Block.GetBlock();
        public bool NotRecoverableArea;

        public RestoreInfo()
        {
        }

        public RestoreInfo(long pos, Block data, bool notRecoverable)
        {
            Position = pos;
            // copy data to a new block, so the original can be reused
            Block b = Data;
            for (int i=b.Length-1;i>=0;--i)
                b[i] = data[i];
            NotRecoverableArea = notRecoverable;
        }
    }
}
