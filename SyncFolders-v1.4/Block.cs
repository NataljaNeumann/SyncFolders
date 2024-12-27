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
    //*******************************************************************************************************
    /// <summary>
    /// The implementation of a 4K block
    /// </summary>
    //*******************************************************************************************************
    [Serializable]
    class Block : IEnumerable<byte>
    {
        private Block()
        {
            // the default constructor is private so consumers are forced to use pool
        }


        static System.Collections.Generic.Queue<Block> _freeBlocks = new Queue<Block>();
        //===================================================================================================
        /// <summary>
        /// Gets a new block, or from pool of released blocks
        /// </summary>
        /// <returns>A block</returns>
        //===================================================================================================
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

        //===================================================================================================
        /// <summary>
        /// Releases given block
        /// </summary>
        /// <param name="b">Block to release</param>
        //===================================================================================================
        public static void ReleaseBlock(Block b)
        {
            /*
            lock (_freeBlocks)
            {
                _freeBlocks.Enqueue(b);
            }
            */
        }

        //===================================================================================================
        /// <summary>
        /// The data of the block
        /// </summary>
        public byte[] _data = new byte[4096];

        //===================================================================================================
        /// <summary>
        /// Gets or sets the data of the block
        /// </summary>
        //===================================================================================================
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

        //===================================================================================================
        /// <summary>
        /// Gets the length of the block
        /// </summary>
        //===================================================================================================
        public int Length
        {
            get
            {
                return _data.Length;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Bitwise or
        /// </summary>
        /// <param name="b1">first block</param>
        /// <param name="b2">second block</param>
        /// <returns>a new block obtained by bitwise or from other two</returns>
        //===================================================================================================
        public static Block operator |(Block b1, Block b2)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(b1._data[i] | b2._data[i]);
            return rb;
        }

        //===================================================================================================
        /// <summary>
        /// Bitwise-inversion
        /// </summary>
        /// <param name="b1">block to compleent</param>
        /// <returns>a new block, obtainded by inversion of given</returns>
        //===================================================================================================
        public static Block operator ~(Block b1)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(~b1._data[i]);
            return rb;
        }

        //===================================================================================================
        /// <summary>
        /// Bitwise-and of two blocks
        /// </summary>
        /// <param name="b1">first block</param>
        /// <param name="b2">second block</param>
        /// <returns>A new block, obtained by bitwise-and of two other</returns>
        //===================================================================================================
        public static Block operator &(Block b1, Block b2)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(b1._data[i] & b2._data[i]);
            return rb;
        }

        //===================================================================================================
        /// <summary>
        /// Bitwise-xor of two blocks
        /// </summary>
        /// <param name="b1">First block</param>
        /// <param name="b2">Second block</param>
        /// <returns>A new block, obtained by bitwise-xor of two other blocks</returns>
        //===================================================================================================
        public static Block operator ^(Block b1, Block b2)
        {
            Block rb = GetBlock();
            for (int i = b1._data.Length - 1; i >= 0; --i)
                rb._data[i] = (byte)(b1._data[i] ^ b2._data[i]);
            return rb;
        }

        //===================================================================================================
        /// <summary>
        /// Bitwise XOR other block inside this block
        /// </summary>
        /// <param name="other">Other block</param>
        public void DoXor(Block other)
        {
            for (int i = _data.Length - 1; i >= 0; --i)
                _data[i] = (byte)(_data[i] ^ other._data[i]);
        }

        //===================================================================================================
        /// <summary>
        /// Reads a block from given stream
        /// </summary>
        /// <param name="s">Stream to read from</param>
        /// <returns>The number of bytes read</returns>
        //===================================================================================================
        public int ReadFrom(System.IO.Stream s)
        {
            return s.Read(_data, 0, _data.Length);
        }

        //===================================================================================================
        /// <summary>
        /// Writes the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        //===================================================================================================
        public void WriteTo(System.IO.Stream s)
        {
            s.Write(_data, 0, _data.Length);
        }

        //===================================================================================================
        /// <summary>
        /// Writes the beginning of the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        /// <param name="length">Numbe of bytes to write</param>
        //===================================================================================================
        public void WriteTo(System.IO.Stream s, int length)
        {
            s.Write(_data, 0, length);
        }

        //===================================================================================================
        /// <summary>
        /// Compares to another block
        /// </summary>
        /// <param name="obj">Other block for comparison</param>
        /// <returns>true iff the contents of the blocks are equal</returns>
        //===================================================================================================
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

        //===================================================================================================
        /// <summary>
        /// Calculates hash code of the object
        /// </summary>
        /// <returns>hash code</returns>
        //===================================================================================================
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

        //===================================================================================================
        /// <summary>
        /// Gets enumerator over contained bytes
        /// </summary>
        /// <returns>An enumerator object</returns>
        //===================================================================================================
        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)_data).GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        //===================================================================================================
        /// <summary>
        /// Gets enumerator over contained byte
        /// </summary>
        /// <returns>An enuerator over bytes as objects</returns>
        //===================================================================================================
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion
    }
}
