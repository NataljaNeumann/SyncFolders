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
    [Serializable]
    class Block : IEnumerable<byte>
    {
        private Block()
        {
            // the default constructor is private so consumers are forced to use pool
        }


        static System.Collections.Generic.Queue<Block> _freeBlocks = new Queue<Block>();
        public static Block GetBlock()
        {
            // Fixme: I consider it to be unsafe to reuse blocks
            return new Block();
            /*
            lock (_freeBlocks)
            {
                if (_freeBlocks.Count > 0)
                {
                    Block b = _freeBlocks.Peek();
                    for (int i = b.Length - 1; i >= 0; --i)
                        b[i] = 0;
                    return b;
                }
                else
                    return new Block();
            }
            */
        }

        public static void ReleaseBlock(Block b)
        {
            /*
            lock (_freeBlocks)
            {
                _freeBlocks.Enqueue(b);
            }
            */
        }

        public byte[] _data = new byte[4096];

        public byte this[int i]
        {
            get
            {
                return _data[i];
            }
            set
            {
                _data[i] = value;
            }
        }

        public int Length
        {
            get
            {
                return _data.Length;
            }
        }

        public static Block operator |(Block b1, Block b2)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(b1._data[i] | b2._data[i]);
            return rb;
        }

        public static Block operator ~(Block b1)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(~b1._data[i]);
            return rb;
        }

        public static Block operator &(Block b1, Block b2)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(b1._data[i] & b2._data[i]);
            return rb;
        }

        public static Block operator ^(Block b1, Block b2)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(b1._data[i] ^ b2._data[i]);
            return rb;
        }

        public void DoXor(Block other)
        {
            for (int i = _data.Length - 1; i >= 0; --i)
                _data[i] = (byte)(_data[i] ^ other._data[i]);
        }

        public int ReadFrom(System.IO.Stream s)
        {
            return s.Read(_data, 0, _data.Length);
        }

        public void WriteTo(System.IO.Stream s)
        {
            s.Write(_data, 0, _data.Length);
        }

        public void WriteTo(System.IO.Stream s, int length)
        {
            s.Write(_data, 0, length);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Block))
                return false;

            Block b = (Block)obj;
            if (b._data.Length != _data.Length)
                return false;

            for (int i = _data.Length - 1; i >= 0; --i)
                if (_data[i] != b._data[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            int h = 0;
            for (int i = _data.Length; i >= 0; --i)
            {
                h = ((h * 2) | (h < 0 ? 1 : 0)) ^ (_data[i]);
            }
            return h;
        }


        #region IEnumerable<byte> Members

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)_data).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion
    }
}
