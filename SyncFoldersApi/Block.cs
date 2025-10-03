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

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// The implementation of a 4K block
    /// </summary>
    //*******************************************************************************************************
    [Serializable]
    public class Block : IEnumerable<byte>
    {
        //===================================================================================================
        /// <summary>
        /// Holds a pool of byte arrays for reusing
        /// </summary>
        static Stack<byte[]> s_oFreeBlocks = new Stack<byte[]>();

        //===================================================================================================
        /// <summary>
        /// An creator of arrays in a separate thread, so all arrays are created in a separate heap
        /// </summary>
        static ThreadBoundArrayFactory<byte> s_oArrayCreator = new ThreadBoundArrayFactory<byte>(4096);

        //===================================================================================================
        /// <summary>
        /// Frees memory, eventually used by the pool
        /// </summary>
        //===================================================================================================
        public static void FreeMemory()
        {
            lock (s_oFreeBlocks)
            {
                s_oFreeBlocks.Clear();
            }
        }


        //===================================================================================================
        /// <summary>
        /// Constructs a new block and inits it with zeros
        /// </summary>
        //===================================================================================================
        public Block()
        {
            lock (s_oFreeBlocks)
            {
                if (s_oFreeBlocks.Count > 0)
                    m_aData = s_oFreeBlocks.Pop();
                else
                {
                    // new array will init it with zeros
                    m_aData = s_oArrayCreator.Create();
                    return;
                }
            }

            // there we pulled array out of the pool and need to init it.
            // we do it outside of the lock for possibly better concurrency
            byte[] aData = m_aData;
            for (int i = aData.Length - 1; i >= 0; --i)
                aData[i] = 0;
        }


        //===================================================================================================
        /// <summary>
        /// Constructs a new block
        /// </summary>
        /// <param name="bInit">Indicates, if it needs to be initialized</param>
        //===================================================================================================
        protected Block(bool bInit)
        {
            lock (s_oFreeBlocks)
            {
                if (s_oFreeBlocks.Count > 0)
                    m_aData = s_oFreeBlocks.Pop();
                else
                {
                    // new array will init it with zeros
                    m_aData = s_oArrayCreator.Create();
                    return;
                }
            }

            if (bInit)
            {
                // there we pulled array out of the pool and need to init it.
                // we do it outside of the lock for possibly better concurrency
                byte[] aData = m_aData;
                for (int i = aData.Length - 1; i >= 0; --i)
                    aData[i] = 0;
            }
        }



        // Finalizer: Returns the byte array to the pool when the object is garbage collected
        ~Block()
        {
            if (m_aData != null)
            {
                lock (s_oFreeBlocks)
                {
                    s_oFreeBlocks.Push(m_aData); // Return the block to the pool
                }

                // we are in finalizer
                // there should be no more references of
                // the object, still we want to ensure thate
                // the array is not referenced anywhere
                // after we put it back to the pool
#pragma warning disable CS8625                
                m_aData = null; // Mark the data as unused
#pragma warning restore CS8625
            }
        }


        //===================================================================================================
        /// <summary>
        /// The data of the block
        /// </summary>
        public byte[] m_aData;

        //===================================================================================================
        /// <summary>
        /// Gets or sets the data of the block
        /// </summary>
        //===================================================================================================
        public byte this[int i]
        {
            get
            {
                return m_aData[i];
            }
            set
            {
                m_aData[i] = value;
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
                return m_aData.Length;
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
            Block rb = new Block(false);
            for (int i = b1.m_aData.Length - 1; i >= 0; --i)
                rb.m_aData[i] = (byte)(b1.m_aData[i] | b2.m_aData[i]);
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
            Block rb = new Block(false);
            for (int i = b1.m_aData.Length - 1; i >= 0; --i)
                rb.m_aData[i] = (byte)(~b1.m_aData[i]);
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
            Block rb = new Block(false);
            for (int i = b1.m_aData.Length - 1; i >= 0; --i)
                rb.m_aData[i] = (byte)(b1.m_aData[i] & b2.m_aData[i]);
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
            Block rb = new Block(false);
            for (int i = b1.m_aData.Length - 1; i >= 0; --i)
                rb.m_aData[i] = (byte)(b1.m_aData[i] ^ b2.m_aData[i]);
            return rb;
        }

        //===================================================================================================
        /// <summary>
        /// Bitwise XOR other block inside this block
        /// </summary>
        /// <param name="other">Other block</param>
        //===================================================================================================
        public void DoXor(Block other)
        {
            for (int i = m_aData.Length - 1; i >= 0; --i)
                m_aData[i] = (byte)(m_aData[i] ^ other.m_aData[i]);
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
            return s.Read(m_aData, 0, m_aData.Length);
        }


        //===================================================================================================
        /// <summary>
        /// Reads first part of a block from given stream
        /// </summary>
        /// <param name="s">Stream to read from</param>
        /// <<param name="nCount">Number of bytes to read</param>
        /// <returns>The number of bytes read</returns>
        //===================================================================================================
        public int ReadFirstPartFrom(System.IO.Stream s, int nCount)
        {
            return s.Read(m_aData, 0, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Reads first part of a block from given stream
        /// </summary>
        /// <param name="s">Stream to read from</param>
        /// <<param name="nCount">Number of bytes to read</param>
        /// <returns>The number of bytes read</returns>
        //===================================================================================================
        public int ReadLastPartFrom(System.IO.Stream s, int nCount)
        {
            return s.Read(m_aData, m_aData.Length-nCount, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Reads a block from given stream
        /// </summary>
        /// <param name="s">Stream to read from</param>
        /// <returns>The number of bytes read</returns>
        //===================================================================================================
        public int ReadFrom(IFile s)
        {
            return s.Read(m_aData, 0, m_aData.Length);
        }


        //===================================================================================================
        /// <summary>
        /// Reads first part of a block from given stream
        /// </summary>
        /// <param name="s">Stream to read from</param>
        /// <<param name="nCount">Number of bytes to read</param>
        /// <returns>The number of bytes read</returns>
        //===================================================================================================
        public int ReadFirstPartFrom(IFile s, int nCount)
        {
            return s.Read(m_aData, 0, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Reads first part of a block from given stream
        /// </summary>
        /// <param name="s">Stream to read from</param>
        /// <<param name="nCount">Number of bytes to read</param>
        /// <returns>The number of bytes read</returns>
        //===================================================================================================
        public int ReadLastPartFrom(IFile s, int nCount)
        {
            return s.Read(m_aData, m_aData.Length - nCount, nCount);
        }

        //===================================================================================================
        /// <summary>
        /// Writes the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        //===================================================================================================
        public void WriteTo(System.IO.Stream s)
        {
            s.Write(m_aData, 0, m_aData.Length);
        }


        //===================================================================================================
        /// <summary>
        /// Writes the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        //===================================================================================================
        public void WriteTo(IFile s)
        {
            s.Write(m_aData, 0, m_aData.Length);
        }

        //===================================================================================================
        /// <summary>
        /// Writes first part of the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        /// <param name="nCount">Number of bytes to write</param>
        //===================================================================================================
        public void WriteFirstPartTo(System.IO.Stream s, int nCount)
        {
            s.Write(m_aData, 0, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Writes last part of the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        /// <param name="nCount">Number of bytes to write</param>
        //===================================================================================================
        public void WriteLastPartTo(System.IO.Stream s, int nCount)
        {
            s.Write(m_aData, m_aData.Length-nCount, nCount);
        }

        //===================================================================================================
        /// <summary>
        /// Writes first part of the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        /// <param name="nCount">Number of bytes to write</param>
        //===================================================================================================
        public void WriteFirstPartTo(IFile s, int nCount)
        {
            s.Write(m_aData, 0, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Writes last part of the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        /// <param name="nCount">Number of bytes to write</param>
        //===================================================================================================
        public void WriteLastPartTo(IFile s, int nCount)
        {
            s.Write(m_aData, m_aData.Length - nCount, nCount);
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
            s.Write(m_aData, 0, length);
        }

        //===================================================================================================
        /// <summary>
        /// Writes the beginning of the block to stream
        /// </summary>
        /// <param name="s">Stream to write to</param>
        /// <param name="length">Numbe of bytes to write</param>
        //===================================================================================================
        public void WriteTo(IFile s, int length)
        {
            s.Write(m_aData, 0, length);
        }


        //===================================================================================================
        /// <summary>
        /// Compares to another block
        /// </summary>
        /// <param name="obj">Other block for comparison</param>
        /// <returns>true iff the contents of the blocks are equal</returns>
        //===================================================================================================
        public override bool Equals(object? obj)
        {
            if (!(obj is Block))
                return false;

            Block b = (Block)obj;
            if (b.m_aData.Length != m_aData.Length)
                return false;

            for (int i = m_aData.Length - 1; i >= 0; --i)
                if (m_aData[i] != b.m_aData[i])
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
            for (int i = m_aData.Length; i >= 0; --i)
            {
                h = ((h << 1) | (h < 0 ? 1 : 0)) ^ (m_aData[i]);
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
            return ((IEnumerable<byte>)m_aData).GetEnumerator();
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
            return m_aData.GetEnumerator();
        }

        #endregion

        //===================================================================================================
        /// <summary>
        /// Clones the block. Creates a whole new array of bytes
        /// </summary>
        /// <returns>A clone</returns>
        //===================================================================================================
        public Block Clone()
        {
            Block oNewBlock = new Block(false);

            for (int i = oNewBlock.Length-1; i >= 0; --i)
                oNewBlock.m_aData[i] = m_aData[i];

            return oNewBlock; 
        }

    }
}
