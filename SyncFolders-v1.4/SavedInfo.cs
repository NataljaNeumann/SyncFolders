﻿/*  SyncFolders aims to help you to synchronize two folders or drives, 
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

    /// <summary>
    /// Objects of this class provide means for analyzing and saving information
    /// about a file, as well as possibility to restore some of the missing parts.
    /// 
    /// The restore algorithm works as XOR of blocks. E.g. if there are 16 blocks
    /// (64K) stored in m_aBlocks then a continuous error range of 64K can be restored.
    /// Additionally, if only one block in a range is bad, then there is a good
    /// probability that two or three ranges, consisting of 1 block each can be 
    /// restored.
    /// 
    /// But, if two blocks (e.g. block 0 of the original file and block 32 of the
    /// original file) hit the same XORed lBlockIndex 0 in m_aBlocks (since 0%16 = 0 = 32%16)
    /// then neither of the blocks can be restored. We need to XOR all but one block
    /// for each lBlockIndex in m_aBlocks, so the remaining data reads exactly the missing
    /// block.
    /// 
    /// Having two rows of blocks with different lenths improves the probability
    /// of restoring single block failures (e.g. if length of m_aBlocks is 16 and
    /// length of m_aOtherBlocks is 17 then non restorable single blocks will be
    /// every 16*17 = 272 blocks, not every 16+17=33 blocks) but it reduces the
    /// maximum number of blocks that can be restored in a continuous range by half.
    /// </summary>
    [Serializable]
    class SavedInfo
    {

        /// <summary>
        /// Holds the file length of the original file
        /// </summary>
        long m_lFileLength;
        /// <summary>
        /// Holds the time stamp of the original file
        /// </summary>
        DateTime m_dtmFileTimestampUtc;
        /// <summary>
        /// Holds first row of blocks for restoring single block failures
        /// </summary>
        public List<Block> m_aBlocks = new List<Block>();
        /// <summary>
        /// Holds second row of blocks for restoring single block failures
        /// </summary>
        List<Block> m_aOtherBlocks = new List<Block>();
        /// <summary>
        /// Simple checksums of the blocks of the original file
        /// </summary>
        List<byte[]> m_aChecksums = new List<byte[]>();

        /// <summary>
        /// Gets the length of the original file
        /// </summary>
        public long Length
        {
            get
            {
                return m_lFileLength;
            }
        }

        /// <summary>
        /// Gets the UTC timestamp of the original file.
        /// </summary>
        public DateTime TimeStamp
        {
            get
            {
                return m_dtmFileTimestampUtc;
            }
        }


        /// <summary>
        /// Constructs a new empty SavedInfo (64 K, no checksums)
        /// </summary>
        public SavedInfo()
        {
            Block testb = Block.GetBlock();
            for (int i = 1024 * 64 / testb.Length - 1; i >= 0; --i)
                m_aBlocks.Add(Block.GetBlock());
            m_aChecksums = new List<byte[]>();
        }

        /// <summary>
        /// Constructs a new SavedInfo with data
        /// </summary>
        /// <param name="lFileLength">The length of the file</param>
        /// <param name="dtmFileTimestampUtc">The UTC timestamp of the file</param>
        /// <param name="bForceOtherBlocks">Force creation of second row of blocks, even if
        /// the file is too small for it</param>
        public SavedInfo(long lFileLength, DateTime dtmFileTimestampUtc, bool bForceOtherBlocks)
        {
            m_lFileLength = lFileLength;
            m_dtmFileTimestampUtc = dtmFileTimestampUtc;

            Block oTestBlock = Block.GetBlock();
            // maxblocks is at least 0.5 per cent of the file or at least 64K in 64K steps
            int nMaxBlocks = ((int)(lFileLength / 200)) / (1024 * 64) * (1024 * 64) / oTestBlock.Length;
            if (nMaxBlocks < 1024 * 64 / oTestBlock.Length)
                nMaxBlocks = 1024 * 64 / oTestBlock.Length;

            // no more blocks than in original file, if the file is small
            if (nMaxBlocks > (lFileLength + oTestBlock.Length - 1) / oTestBlock.Length)
                nMaxBlocks = (int)((lFileLength + oTestBlock.Length - 1) / oTestBlock.Length);

            // fill first row of the blocks
            for (int i = nMaxBlocks - 1; i >= 0; --i)
                m_aBlocks.Add(Block.GetBlock());

            // calculate the length of the second row: Total number of blocks ~ 1% in
            // both rows
            int nMaxOtherBlocks = ((int)(lFileLength / 100)) / (1024 * 64) * (1024 * 64) 
                / oTestBlock.Length - nMaxBlocks;

            // if the original file is too small for two rows then no second row
            if ( (nMaxOtherBlocks < 1024 * 64 / oTestBlock.Length) && !bForceOtherBlocks)
                nMaxOtherBlocks = 0;
            else
            {
                // if there are many blocks
                if (nMaxBlocks >= 48)
                {
                    // default: one block more
                    nMaxOtherBlocks = nMaxBlocks + 1;
                    // try to find a length of at least 17 blocks
                    // so there is a chance for restoring two different 16K ranges
                    for (int i = 17; i*2<nMaxBlocks; i+=2)
                        if ((nMaxBlocks % i) != 0)
                        {
                            nMaxOtherBlocks = nMaxBlocks - i;
                            break;
                        }
                }
                else
                    // same procedure for 32K, 16K, 8K
                    if (nMaxBlocks >= 24 && ( (nMaxBlocks%9) != 0) )
                        nMaxOtherBlocks = nMaxBlocks - 9;
                    else
                        if (nMaxBlocks >= 12 && ((nMaxBlocks % 5) != 0)  )
                            nMaxOtherBlocks = nMaxBlocks - 5;
                        else
                            if (nMaxBlocks >= 6 && ((nMaxBlocks % 3) != 0) )
                                nMaxOtherBlocks = nMaxBlocks - 3;
                            else
                                // default: one block more
                                nMaxOtherBlocks = nMaxBlocks + 1;
            }

            // fill second row of blocks
            for (int i = nMaxOtherBlocks - 1; i >= 0; --i)
                m_aOtherBlocks.Add(Block.GetBlock());

            // init checksums
            m_aChecksums = new List<byte[]>();
        }

        /// <summary>
        /// This simple checksum calculator is used to verify
        /// that we can trust m_aChecksums
        /// </summary>
        private class CheckSumCalculator
        {
            /// <summary>
            /// The stored checksum
            /// </summary>
            public byte[] Checksum = new byte[31];
            /// <summary>
            /// Current position in Checksum
            /// </summary>
            private int _pos;
            /// <summary>
            /// Adds a byte to the checksum (XORs it somewhere)
            /// </summary>
            /// <param name="oBlockOfOriginalFile">Byte to addd</param>
            public void AddByte(byte b)
            {
                Checksum[_pos++] ^= b;
                if (_pos >= Checksum.Length)
                    _pos = 0;
            }
            /// <summary>
            /// Adds a byte to the checksum (XORs it somewhere)
            /// </summary>
            /// <param name="oBlockOfOriginalFile">Byte to addd</param>
            public void AddByte(int b)
            {
                Checksum[_pos++] ^= (byte)b;
                if (_pos >= Checksum.Length)
                    _pos = 0;
            }
        }

        /// <summary>
        /// Reads information about an original file from stream, containg saved info
        /// </summary>
        /// <param name="oInputStream">The stream to read from</param>
        public void ReadFrom(System.IO.Stream oInputStream)
        {
            CheckSumCalculator oMetadataChecksum = new CheckSumCalculator();
            m_aBlocks.Clear();
            m_aOtherBlocks.Clear();
            m_aChecksums.Clear();


            // read in the minimum version
            if (oInputStream.ReadByte() != 0)
            {
                // not supported version
                return;
            }

            oMetadataChecksum.AddByte(0);

            // read the maximum supported version (should be different from -1, which means eof)
            int nMaxVersion = oInputStream.ReadByte();
            if (nMaxVersion == -1)
                return;

            oMetadataChecksum.AddByte(nMaxVersion);

            // read in the time
            long ticks = 0;
            for (int i = 7; i >= 0; --i)
            {
                int b = oInputStream.ReadByte(); if (b == -1) return;
                oMetadataChecksum.AddByte(b);
                ticks = ticks * 256 + b;
            };
            m_dtmFileTimestampUtc = new DateTime(ticks);

            // read in the original file length
            m_lFileLength = 0;
            for (int i = 7; i >= 0; --i)
            {
                int b = oInputStream.ReadByte(); if (b == -1) return;
                oMetadataChecksum.AddByte(b);
                m_lFileLength = m_lFileLength * 256 + b;
            };

            // read in the number of blocks in each row
            long lBlocksInFirstRow = 0;
            long lBlocksInSecondRow = 0;

            for (int i = 7; i >= 0; --i)
            {
                int b = oInputStream.ReadByte(); if (b == -1) return;
                oMetadataChecksum.AddByte(b);
                lBlocksInFirstRow = lBlocksInFirstRow * 256 + b;
            };

            for (int i = 7; i >= 0; --i)
            {
                int b = oInputStream.ReadByte(); if (b == -1) return;
                oMetadataChecksum.AddByte(b);
                lBlocksInSecondRow = lBlocksInSecondRow * 256 + b;
            };

            // read blocks of the first row
            for (long i = lBlocksInFirstRow - 1; i >= 0; --i)
            {
                Block b = null;

                b = Block.GetBlock();

                try
                {
                    if (b.ReadFrom(oInputStream) != b.Length)
                    {
                        m_aBlocks.Clear();
                        return;
                    }

                    m_aBlocks.Add(b);
                }
                catch (System.IO.IOException)
                {
                    // we don't expect the restore file to be perfect
                    // add a null block for failed reads
                    m_aBlocks.Add(null);
                    // seek the next block
                    oInputStream.Seek(b.Length * (lBlocksInFirstRow - i) + 34, System.IO.SeekOrigin.Begin);
                }
            };

            // read blocks of second row
            for (long i = lBlocksInSecondRow - 1; i >= 0; --i)
            {
                Block b = null;

                b = Block.GetBlock();

                try
                {
                    if (b.ReadFrom(oInputStream) != b.Length)
                    {
                        // clear both lists if the file has been tampered with
                        m_aBlocks.Clear();
                        m_aOtherBlocks.Clear();
                        return;
                    }

                    m_aOtherBlocks.Add(b);
                }
                catch (System.IO.IOException)
                {
                    // we don't expect the restore file to be perfect
                    // add a null block for failed reads
                    m_aOtherBlocks.Add(null);
                    // seek the next block
                    oInputStream.Seek(b.Length * (lBlocksInSecondRow - i + lBlocksInFirstRow) + 34, System.IO.SeekOrigin.Begin);
                };
            };

            // read the number of checksums of original file.
            // this should be exactly the same as the nuber of blocks in original file
            int nChecksumCount = 0;
            for (int i = 7; i >= 0; --i)
            {
                int b = oInputStream.ReadByte(); if (b == -1) return;
                oMetadataChecksum.AddByte(b);
                nChecksumCount = nChecksumCount * 256 + b;
            };

            // read the checksums
            for (int i = 0; i < nChecksumCount; ++i)
            {
                byte[] checksum = new byte[3];
                if (oInputStream.Read(checksum, 0, checksum.Length) < checksum.Length)
                    return;

                oMetadataChecksum.AddByte(checksum[0]);
                oMetadataChecksum.AddByte(checksum[1]);
                oMetadataChecksum.AddByte(checksum[2]);

                m_aChecksums.Add(checksum);
            }

            // read the final checksum over metadata from stream
            CheckSumCalculator checksumInFile = new CheckSumCalculator();
            if (oInputStream.Read(checksumInFile.Checksum, 0, checksumInFile.Checksum.Length) < checksumInFile.Checksum.Length)
            {
                // saved checksums are not reliable, so clear them and trust the CRC checksums of the drive
                m_aChecksums.Clear();
                return;
            }

            // compare calculated checksum to the one, read from stream
            for (int i = 0; i < checksumInFile.Checksum.Length; ++i)
            {
                if (checksumInFile.Checksum[i] != oMetadataChecksum.Checksum[i])
                {
                    // if they differ then checksums in the file are not reliable
                    m_aChecksums.Clear();
                    return;
                }
            }

        }

        /// <summary>
        /// Saves calculated information about the original file to stream
        /// </summary>
        /// <param name="oOutputStream">The stream to save to</param>
        public void SaveTo(System.IO.Stream oOutputStream)
        {
            CheckSumCalculator oMetadataChecksum = new CheckSumCalculator();

            // minimum version
            oOutputStream.WriteByte(0);
            oMetadataChecksum.AddByte(0);

            // maximum version
            oOutputStream.WriteByte(0);
            oMetadataChecksum.AddByte(0);

            // save the timestamp of the original file
            ulong ulToWriteBytewise = (ulong)(m_dtmFileTimestampUtc.Ticks);
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(ulToWriteBytewise >> 56);
                oMetadataChecksum.AddByte(b);
                ulToWriteBytewise = ulToWriteBytewise * 256;
                oOutputStream.WriteByte(b);
            };

            // save the length of the original file
            ulToWriteBytewise = (ulong)m_lFileLength;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(ulToWriteBytewise >> 56);
                oMetadataChecksum.AddByte(b);
                ulToWriteBytewise = ulToWriteBytewise * 256;
                oOutputStream.WriteByte(b);
            };

            // save the number of blocks
            ulToWriteBytewise = (ulong)m_aBlocks.Count;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(ulToWriteBytewise >> 56);
                oMetadataChecksum.AddByte(b);
                ulToWriteBytewise = ulToWriteBytewise * 256;
                oOutputStream.WriteByte(b);
            };

            // save the number of blocks in second row
            ulToWriteBytewise = (ulong)m_aOtherBlocks.Count;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(ulToWriteBytewise >> 56);
                oMetadataChecksum.AddByte(b);
                ulToWriteBytewise = ulToWriteBytewise * 256;
                oOutputStream.WriteByte(b);
            };

            // save the blocks in first row
            for (int i = 0; i < m_aBlocks.Count; ++i)
                m_aBlocks[i].WriteTo(oOutputStream);

            // save the blocks in second row
            for (int i = 0; i < m_aOtherBlocks.Count; ++i)
                m_aOtherBlocks[i].WriteTo(oOutputStream);

            // save the number of checksums (shall be same as number
            // of blocks in original file
            ulToWriteBytewise = (ulong)m_aChecksums.Count;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(ulToWriteBytewise >> 56);
                oMetadataChecksum.AddByte(b);
                ulToWriteBytewise = ulToWriteBytewise * 256;
                oOutputStream.WriteByte(b);
            };

            // save the checksum of blocks of original file
            for (int i = 0; i < m_aChecksums.Count; ++i)
            {
                oOutputStream.Write(m_aChecksums[i], 0, m_aChecksums[i].Length);
                oMetadataChecksum.AddByte(m_aChecksums[i][0]);
                oMetadataChecksum.AddByte(m_aChecksums[i][1]);
                oMetadataChecksum.AddByte(m_aChecksums[i][2]);
            }

            // save the checksum of metadata, so we know if checksums are reliable
            oOutputStream.Write(oMetadataChecksum.Checksum, 0, oMetadataChecksum.Checksum.Length);
        }

        /// <summary>
        /// Analyzes a block, read from original file, for creating SavedInfo
        /// It is expected that all blocks are readable and come 
        /// sequentially from beginning to end of original file
        /// </summary>
        /// <param name="oBlockOfOriginalFile">The block of original file</param>
        /// <param name="lBlockIndex">Zero-based lBlockIndex of the block</param>
        public void AnalyzeForInfoCollection(Block oBlockOfOriginalFile, long lBlockIndex)
        {
            if (m_aBlocks.Count == 0)
                return;

            m_aBlocks[(int)(lBlockIndex % m_aBlocks.Count)].DoXor(oBlockOfOriginalFile);

            if (m_aOtherBlocks.Count > 0)
                m_aOtherBlocks[(int)(lBlockIndex % m_aOtherBlocks.Count)].DoXor(oBlockOfOriginalFile);

            // if we can save checksum of the block in a list
            if (lBlockIndex < int.MaxValue)
            {
                // calc a checksum for later verification of the block
                // you may wonder why I used 3... Why not?
                byte[] checksum = new byte[3];
                int currentIndex = 0;
                foreach (byte by in oBlockOfOriginalFile)
                {
                    checksum[currentIndex++] ^= by;
                    if (currentIndex >= checksum.Length)
                        currentIndex = 0;
                }
                while (m_aChecksums.Count <= lBlockIndex)
                    m_aChecksums.Add(new byte[] { 0, 0, 0 });
                m_aChecksums[(int)lBlockIndex] = checksum;
            }
        }

        /// <summary>
        /// Last analyzed block for restore
        /// </summary>
        long m_lCurrentlyRestoredBlock = -1;
        /// <summary>
        /// List of blocks that need to be restored
        /// </summary>
        List<long> m_aListOfBlocksToRestore;

        /// <summary>
        /// Starts to restore a file. This shall only be executed after ReadFrom
        /// </summary>
        public void StartRestore()
        {
            m_lCurrentlyRestoredBlock = -1;
            m_aListOfBlocksToRestore = new List<long>();
        }

        /// <summary>
        /// Analyzes a block for test or restore. There some blocks
        /// can be missing. All readable blocks are expected to come from
        /// beginning to end of original file.
        /// </summary>
        /// <param name="b">The block from original file</param>
        /// <param name="index">Index of the block</param>
        /// <returns>true iff the block has been accepted (e.g. if its checksum matches)</returns>
        public bool AnalyzeForTestOrRestore(Block b, long index)
        {

            if (m_aBlocks.Count == 0)
                return false;

            // if there are not enough checksums (e.g. .chk file from old version)
            // then simply add the block as OK
            if (m_aChecksums.Count <= index)
            {
                //m_aBlocks[(int)(lBlockIndex % m_aBlocks.Count)] = m_aBlocks[(int)(lBlockIndex % m_aBlocks.Count)] ^ oBlockOfOriginalFile;
                m_aBlocks[(int)(index % m_aBlocks.Count)].DoXor(b);

                // if there is a second row of blocks, then perform also preparation of oOtherSaveInfo blocks;
                if (m_aOtherBlocks.Count > 0)
                    //m_aOtherBlocks[(int)(lBlockIndex % m_aOtherBlocks.Count)] = m_aOtherBlocks[(int)(lBlockIndex % m_aOtherBlocks.Count)] ^ oBlockOfOriginalFile;
                    m_aOtherBlocks[(int)(index % m_aOtherBlocks.Count)].DoXor(b);

                // analyze which blocks have been skipped
                while (++m_lCurrentlyRestoredBlock < index)
                {
                    m_aListOfBlocksToRestore.Add(m_lCurrentlyRestoredBlock);
                };

                return true;
            }

            // calc a checksum for verification of the block
            byte[] checksum = new byte[3];
            int currentIndex = 0;
            foreach (byte by in b)
            {
                checksum[currentIndex++] ^= by;
                if (currentIndex >= checksum.Length)
                    currentIndex = 0;
            }

            bool bChecksumOk = true;
            for (int i = checksum.Length - 1; i >= 0; --i)
                if (checksum[i] != m_aChecksums[(int)index][i])
                    bChecksumOk = false;

            if (!bChecksumOk)
            {
                // don't add to restore there, we could try several variants
                //m_aListOfBlocksToRestore.Add(lBlockIndex);
                return false;
            }
            else
            {
                //m_aBlocks[(int)(lBlockIndex % m_aBlocks.Count)] = m_aBlocks[(int)(lBlockIndex % m_aBlocks.Count)] ^ oBlockOfOriginalFile;
                m_aBlocks[(int)(index % m_aBlocks.Count)].DoXor(b);

                // if there is a second row of blocks, then perform also preparation of oOtherSaveInfo blocks;
                if (m_aOtherBlocks.Count > 0)
                    //m_aOtherBlocks[(int)(lBlockIndex % m_aOtherBlocks.Count)] = m_aOtherBlocks[(int)(lBlockIndex % m_aOtherBlocks.Count)] ^ oBlockOfOriginalFile;
                    m_aOtherBlocks[(int)(index % m_aOtherBlocks.Count)].DoXor(b);

                // analyze which blocks have been skipped
                while (++m_lCurrentlyRestoredBlock < index)
                {
                    m_aListOfBlocksToRestore.Add(m_lCurrentlyRestoredBlock);
                };

                return true;
            }
        }

        /// <summary>
        /// Ends a restore proces
        /// </summary>
        /// <param name="outlNotRestoredSize">[OUT] the size of the file that could not be restored</param>
        /// <param name="strCurrentFile">The file name of current file for messages</param>
        /// <param name="iLogWriter">The log writer</param>
        /// <returns>List of restore informations</returns>
        public List<RestoreInfo> EndRestore(out long outlNotRestoredSize, string strCurrentFile, ILogWriter iLogWriter)
        {
            // add the missing blocks at the end of file
            Block oTestBlock = Block.GetBlock();
            while (++m_lCurrentlyRestoredBlock < (m_lFileLength + oTestBlock.Length - 1) / oTestBlock.Length)
            {
                m_aListOfBlocksToRestore.Add(m_lCurrentlyRestoredBlock);
            };

            // this is the resulting list that we will return
            List<RestoreInfo> oResult = new List<RestoreInfo>();
            // if there is not even the first row of blocks, then we can't restore anything
            if (m_aBlocks.Count == 0)
            {
                outlNotRestoredSize = 0;
                for (int i = 0; i < m_aListOfBlocksToRestore.Count; i++)
                {
                    // all blocks are non-restorable
                    long blockToRestore = m_aListOfBlocksToRestore[i];
                    outlNotRestoredSize = outlNotRestoredSize + oTestBlock.Length;
                    oResult.Add(new RestoreInfo(blockToRestore * oTestBlock.Length, Block.GetBlock(), true));
                }
                return oResult;
            };

            // if there is no second row of blocks, then just use the primary row
            if (m_aOtherBlocks.Count == 0)
            {
                List<int> aUsedIndexesInFirstRow = new List<int>();
                List<int> aNonRecoverableIndexesInFirstRow = new List<int>();

                for (int i = 0; i < m_aListOfBlocksToRestore.Count; i++)
                {
                    long blockToRestore = m_aListOfBlocksToRestore[i];
                    int nIndexToUseFromFirstRow = (int)(blockToRestore % m_aBlocks.Count);
                    if (aUsedIndexesInFirstRow.Contains(nIndexToUseFromFirstRow) || m_aBlocks[nIndexToUseFromFirstRow] == null)
                    {
                        if (!aNonRecoverableIndexesInFirstRow.Contains(nIndexToUseFromFirstRow))
                            aNonRecoverableIndexesInFirstRow.Add(nIndexToUseFromFirstRow);
                    }
                    else
                    {
                        aUsedIndexesInFirstRow.Add(nIndexToUseFromFirstRow);
                    }
                }

                // check integrity of remaining blocks
                // if there is doubt that oOtherSaveInfo blocks are valid then don't use 
                // the checksum blocks
                bool bOtherSeemOk = true;
                for (int i = m_aBlocks.Count - 1; i >= 0; --i)
                {
                    if (!aUsedIndexesInFirstRow.Contains(i))
                    {
                        Block b = m_aBlocks[i];
                        for (int j = b.Length - 1; j >= 0; --j)
                            if (b[j] != 0)
                            {
                                bOtherSeemOk = false;
                                break;
                            }
                    }

                    if (!bOtherSeemOk)
                    {
                        iLogWriter.WriteLog(1, "Warning: several blocks don't match in saved info ", strCurrentFile, ", saved info will be ignored completely");
                        break;
                    }
                }
                

                outlNotRestoredSize = 0;
                for (int i = 0; i < m_aListOfBlocksToRestore.Count; i++)
                {
                    long blockToRestore = m_aListOfBlocksToRestore[i];
                    int nIndexToUseFromFirstRow = (int)(blockToRestore % m_aBlocks.Count);
                    if (aNonRecoverableIndexesInFirstRow.Contains(nIndexToUseFromFirstRow) || !bOtherSeemOk)
                    {
                        outlNotRestoredSize = outlNotRestoredSize + oTestBlock.Length;
                        oResult.Add(new RestoreInfo(blockToRestore * oTestBlock.Length, Block.GetBlock(), true));
                    }
                    else
                    {
                        oResult.Add(new RestoreInfo(blockToRestore * oTestBlock.Length, m_aBlocks[nIndexToUseFromFirstRow], false));
                    }
                }
            }
            else
            {
                // there we have two rows of blocks that can be used for restoring
                // so sometimes we will be able to restore based on the first row,
                // sometimes we will be able to restore based on the second row

                bool bRepeat = true;
                while (bRepeat)
                {
                    bRepeat = false;
                    List<int> aProposedIndexesInFirstRow = new List<int>();
                    List<int> aProposedIndexesInOtherBlocks = new List<int>();
                    List<int> aNonUsableIndexesInFirstRow = new List<int>();
                    List<int> aNonUsableIndexesInOtherBlocks = new List<int>();

                    // test, if we can restore something from the first row of saved blocks
                    for (int i = 0; i < m_aListOfBlocksToRestore.Count; i++)
                    {
                        long blockToRestore = m_aListOfBlocksToRestore[i];
                        int nIndexToUseInFirstRow = (int)(blockToRestore % m_aBlocks.Count);
                        if (aProposedIndexesInFirstRow.Contains(nIndexToUseInFirstRow) || m_aBlocks[nIndexToUseInFirstRow] == null)
                        {
                            if (!aNonUsableIndexesInFirstRow.Contains(nIndexToUseInFirstRow))
                                aNonUsableIndexesInFirstRow.Add(nIndexToUseInFirstRow);
                        }
                        else
                        {
                            aProposedIndexesInFirstRow.Add(nIndexToUseInFirstRow);
                        }
                    }

                    // restore, if we can restore something from the first row of saved blocks
                    for (int i = m_aListOfBlocksToRestore.Count - 1; i >= 0; i--)
                    {
                        long blockToRestore = m_aListOfBlocksToRestore[i];
                        int nIndexToUseInFirstRow = (int)(blockToRestore % m_aBlocks.Count);
                        if (!aNonUsableIndexesInFirstRow.Contains(nIndexToUseInFirstRow))
                        {
                            bool bChecksumOk2 = true;

                            if (blockToRestore < m_aChecksums.Count)
                            {
                                // calc a checksum for verification of the block
                                byte[] checksum = new byte[3];
                                int currentIndex = 0;
                                foreach (byte by in m_aBlocks[nIndexToUseInFirstRow])
                                {
                                    checksum[currentIndex++] ^= by;
                                    if (currentIndex >= checksum.Length)
                                        currentIndex = 0;
                                }

                                for (int j = checksum.Length - 1; j >= 0; --j)
                                    if (checksum[j] != m_aChecksums[(int)blockToRestore][j])
                                        bChecksumOk2 = false;
                            }

                            // if there is a checksum for the block then we'll use restored block only if checksum matches
                            if (bChecksumOk2)
                            {
                                oResult.Add(new RestoreInfo(blockToRestore * oTestBlock.Length, m_aBlocks[nIndexToUseInFirstRow], false));

                                // we calculated the new block, it could improve the situation at the oOtherSaveInfo row of blocks
                                m_aOtherBlocks[(int)(blockToRestore % m_aOtherBlocks.Count)] = 
                                    m_aOtherBlocks[(int)(blockToRestore % m_aOtherBlocks.Count)] ^ m_aBlocks[nIndexToUseInFirstRow];

                                // we need i to run backwards for this to work
                                m_aListOfBlocksToRestore.RemoveAt(i);

                                // no need to repeat if we restored something from the primary row of blocks,
                                // since we run analysis on the second row of blocks afterwards
                                // bRepeat = true;
                            }
                            else
                            {
                                iLogWriter.WriteLog(1, "Warning: checksum of block at offsset ",blockToRestore * oTestBlock.Length,
                                    " doesn't match available in primary blocks of restoreinfo ", strCurrentFile, 
                                    ", primary restoreinfo for the block will be ignored");
                            }
                        }
                    }

                    // test, if we can restore something from the second row of blocks
                    for (int i = m_aListOfBlocksToRestore.Count - 1; i >= 0; i--)
                    {
                        long blockToRestore = m_aListOfBlocksToRestore[i];
                        int nIndexToUseFromOtherBlocks = (int)(blockToRestore % m_aOtherBlocks.Count);
                        if (aProposedIndexesInOtherBlocks.Contains(nIndexToUseFromOtherBlocks) || m_aOtherBlocks[nIndexToUseFromOtherBlocks] == null)
                        {
                            if (!aNonUsableIndexesInOtherBlocks.Contains(nIndexToUseFromOtherBlocks))
                                aNonUsableIndexesInOtherBlocks.Add(nIndexToUseFromOtherBlocks);
                        }
                        else
                        {
                            aProposedIndexesInOtherBlocks.Add(nIndexToUseFromOtherBlocks);
                        }
                    }

                    // restore, if we can restore something from the second row of blocks
                    for (int i = m_aListOfBlocksToRestore.Count - 1; i >= 0; i--)
                    {
                        long blockToRestore = m_aListOfBlocksToRestore[i];
                        int blockToUse = (int)(blockToRestore % m_aOtherBlocks.Count);
                        if (!aNonUsableIndexesInOtherBlocks.Contains(blockToUse))
                        {
                            bool bChecksumOk3 = true;

                            if (blockToRestore < m_aChecksums.Count)
                            {
                                // calc a checksum for verification of the block
                                byte[] checksum = new byte[3];
                                int currentIndex = 0;
                                foreach (byte by in m_aOtherBlocks[blockToUse])
                                {
                                    checksum[currentIndex++] ^= by;
                                    if (currentIndex >= checksum.Length)
                                        currentIndex = 0;
                                }

                                for (int j = checksum.Length - 1; j >= 0; --j)
                                    if (checksum[j] != m_aChecksums[(int)blockToRestore][j])
                                        bChecksumOk3 = false;
                            }

                            if (bChecksumOk3)
                            {
                                // we calculated the new block, it could improve the situation at the primary row of blocks
                                m_aBlocks[(int)(blockToRestore % m_aBlocks.Count)] = 
                                    m_aBlocks[(int)(blockToRestore % m_aBlocks.Count)] ^ m_aOtherBlocks[blockToUse];

                                // skip these two lines in test case for testing the "repeat" case 
                                oResult.Add(new RestoreInfo(blockToRestore * oTestBlock.Length, m_aOtherBlocks[blockToUse], false));
                                m_aListOfBlocksToRestore.RemoveAt(i);

                                // repeat restoring using the primary and the secondary row of blocks:
                                // we restored something using the second row, this could improve the situation with the primary blocks.
                                bRepeat = true;
                            }
                            else
                            {
                                iLogWriter.WriteLog(1, "Warning: checksum of block at offset ", blockToRestore * oTestBlock.Length, 
                                    " doesn't match available in secondary blocks of restoreinfo ", strCurrentFile, 
                                    ", secondary restoreinfo for the block will be ignored");
                            }
                        }
                    }

                    // don't repeat if nothing more to restore
                    if (m_aListOfBlocksToRestore.Count == 0)
                        bRepeat = false;
                }

                // the remaining blocks are non-recoverable, fill them with zeros
                outlNotRestoredSize = 0;
                for (int i = 0; i < m_aListOfBlocksToRestore.Count; i++)
                {
                    long blockToRestore = m_aListOfBlocksToRestore[i];
                    outlNotRestoredSize = outlNotRestoredSize + oTestBlock.Length;
                    oResult.Add(new RestoreInfo(blockToRestore * oTestBlock.Length, Block.GetBlock(), true));
                }

            }
            return oResult;
        }

        /// <summary>
        /// After we tested readability of the file and the saved info we can verify
        /// that all saved blocks XOR to zero.
        /// </summary>
        /// <returns>true iff all blocks matched</returns>
        public bool VerifyIntegrityAfterRestoreTest()
        {
            // if the original file matches the chk file and there were no checksum errors
            //  then all contents of the m_aBlocks shall be zero
            for (int i = m_aBlocks.Count - 1; i >= 0; --i)
            {
                Block b = m_aBlocks[i];
                for (int j = b.Length - 1; j >= 0; --j)
                    if (b[j] != 0)
                        return false;
            }

            if (m_aOtherBlocks.Count > 0)
            {
                // if the original file matches the chk file and there were no checksum errors
                //  then all contents of the m_aOtherBlocks also shall be zero
                for (int i = m_aOtherBlocks.Count - 1; i >= 0; --i)
                {
                    Block b = m_aOtherBlocks[i];
                    for (int j = b.Length - 1; j >= 0; --j)
                        if (b[j] != 0)
                            return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Counts non-empty blocks in first row
        /// </summary>
        /// <returns>The number of non-empty blocks</returns>
        public int NonEmptyBlocks()
        {
            int cnt = 0;
            foreach (Block blk in m_aBlocks)
            {
                foreach (byte b in blk)
                {
                    if (b != 0)
                    {
                        ++cnt;
                        break;
                    }
                }
            }
            return cnt;
        }

        /// <summary>
        /// If there are two copies of saved info then we can improve both copies
        /// from each other
        /// </summary>
        /// <param name="oOtherSaveInfo">Other saved info</param>
        public void ImproveWith(SavedInfo oOtherSaveInfo)
        {
            // Match checksums
            if (this.m_aChecksums.Count == 0 && oOtherSaveInfo.m_aChecksums.Count > 0)
            {
                this.m_aChecksums = oOtherSaveInfo.m_aChecksums;
                // remove blocks on this saveinfo, if too many
                while (this.m_aBlocks.Count > oOtherSaveInfo.m_aBlocks.Count)
                {
                    this.m_aBlocks.RemoveAt(this.m_aBlocks.Count - 1);
                }
                while (this.m_aOtherBlocks.Count > oOtherSaveInfo.m_aOtherBlocks.Count)
                {
                    this.m_aOtherBlocks.RemoveAt(this.m_aOtherBlocks.Count - 1);
                }
            }
            else
            {
                if (oOtherSaveInfo.m_aChecksums.Count == 0 && this.m_aChecksums.Count > 0)
                {
                    oOtherSaveInfo.m_aChecksums = this.m_aChecksums;
                    // remove blocks on the oOtherSaveInfo saveinfo, if too many
                    while (oOtherSaveInfo.m_aBlocks.Count > this.m_aBlocks.Count)
                    {
                        oOtherSaveInfo.m_aBlocks.RemoveAt(oOtherSaveInfo.m_aBlocks.Count - 1);
                    }
                    while (oOtherSaveInfo.m_aOtherBlocks.Count > this.m_aOtherBlocks.Count)
                    {
                        oOtherSaveInfo.m_aOtherBlocks.RemoveAt(oOtherSaveInfo.m_aOtherBlocks.Count - 1);
                    }
                }
            }

            // add primary missing blocks on the oOtherSaveInfo
            if (m_aBlocks.Count == oOtherSaveInfo.m_aBlocks.Count ||
                oOtherSaveInfo.m_aBlocks.Count == 0)
            {
                for (int i = 0; i < m_aBlocks.Count; ++i)
                {
                    if (oOtherSaveInfo.m_aBlocks.Count <= i)
                        oOtherSaveInfo.m_aBlocks.Add(null);

                    if (oOtherSaveInfo.m_aBlocks[i] == null && this.m_aBlocks[i] != null)
                    {
                        Block newB = Block.GetBlock();
                        Block oldB = this.m_aBlocks[i];
                        for (int j = newB.Length - 1; j >= 0; --j)
                            newB[j] = oldB[j];
                        oOtherSaveInfo.m_aBlocks[i] = newB;
                    }
                }
            }

            // add secondary missing blocks on the oOtherSaveInfo
            if (m_aOtherBlocks.Count == oOtherSaveInfo.m_aOtherBlocks.Count ||
                oOtherSaveInfo.m_aOtherBlocks.Count == 0)
            {
                for (int i = 0; i < m_aOtherBlocks.Count; ++i)
                {
                    if (oOtherSaveInfo.m_aOtherBlocks.Count <= i)
                        oOtherSaveInfo.m_aOtherBlocks.Add(null);

                    if (oOtherSaveInfo.m_aOtherBlocks[i] == null && this.m_aOtherBlocks[i] != null)
                    {
                        Block newB = Block.GetBlock();
                        Block oldB = this.m_aOtherBlocks[i];
                        for (int j = newB.Length - 1; j >= 0; --j)
                            newB[j] = oldB[j];
                        oOtherSaveInfo.m_aOtherBlocks[i] = newB;
                    }
                }
            }

            // add primary missing blocks on this
            if (m_aBlocks.Count == oOtherSaveInfo.m_aBlocks.Count ||
                m_aBlocks.Count == 0)
            {
                for (int i = 0; i < oOtherSaveInfo.m_aBlocks.Count; ++i)
                {
                    if (this.m_aBlocks.Count <= i)
                        this.m_aBlocks.Add(null);

                    if (this.m_aBlocks[i] == null && oOtherSaveInfo.m_aBlocks[i] != null)
                    {
                        Block newB = Block.GetBlock();
                        Block oldB = oOtherSaveInfo.m_aBlocks[i];
                        for (int j = newB.Length - 1; j >= 0; --j)
                            newB[j] = oldB[j];
                        this.m_aBlocks[i] = newB;
                    }
                }
            }

            // add secondary missing blocks on this
            if (m_aOtherBlocks.Count == oOtherSaveInfo.m_aOtherBlocks.Count ||
                oOtherSaveInfo.m_aOtherBlocks.Count == 0)
            {
                for (int i = 0; i < oOtherSaveInfo.m_aOtherBlocks.Count; ++i)
                {
                    if (this.m_aOtherBlocks.Count <= i)
                        this.m_aOtherBlocks.Add(null);

                    if (this.m_aOtherBlocks[i] == null && oOtherSaveInfo.m_aOtherBlocks[i] != null)
                    {
                        Block newB = Block.GetBlock();
                        Block oldB = oOtherSaveInfo.m_aOtherBlocks[i];
                        for (int j = newB.Length - 1; j >= 0; --j)
                            newB[j] = oldB[j];
                        this.m_aOtherBlocks[i] = newB;
                    }
                }
            }
        }
    }
}