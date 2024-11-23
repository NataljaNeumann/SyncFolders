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
    class SaveInfo
    {
        long _file_length;
        DateTime _file_timestamp_utc;
        public List<Block> _blocks = new List<Block>();
        List<Block> _other_blocks = new List<Block>();
        List<byte[]> _checksums = new List<byte[]>();

        public long Length
        {
            get
            {
                return _file_length;
            }
        }

        public DateTime TimeStamp
        {
            get
            {
                return _file_timestamp_utc;
            }
        }


        public SaveInfo()
        {
            Block testb = Block.GetBlock();
            for (int i = 1024 * 64 / testb.Length - 1; i >= 0; --i)
                _blocks.Add(Block.GetBlock());
            _checksums = new List<byte[]>();
        }

        public SaveInfo(long fileLength, DateTime timestamp_utc, bool force_other_blocks)
        {
            _file_length = fileLength;
            _file_timestamp_utc = timestamp_utc;

            Block testb = Block.GetBlock();
            // maxblocks is at least 0.5 per cent of the file or at least 64K in 64K steps
            int max_blocks = ((int)(fileLength / 200)) / (1024 * 64) * (1024 * 64) / testb.Length;
            if (max_blocks < 1024 * 64 / testb.Length)
                max_blocks = 1024 * 64 / testb.Length;

            if (max_blocks > (fileLength + testb.Length - 1) / testb.Length)
                max_blocks = (int)((fileLength + testb.Length - 1) / testb.Length);

            for (int i = max_blocks - 1; i >= 0; --i)
                _blocks.Add(Block.GetBlock());

            int max_other_blocks = ((int)(fileLength / 100)) / (1024 * 64) * (1024 * 64) / testb.Length - max_blocks;

            if ( (max_other_blocks < 1024 * 64 / testb.Length) && !force_other_blocks)
                max_other_blocks = 0;
            else
            {
                if (max_blocks >= 48)
                {
                    max_other_blocks = max_blocks + 1;
                    for (int i = 17; i*2<max_blocks; i+=2)
                        if ((max_blocks % i) != 0)
                        {
                            max_other_blocks = max_blocks - i;
                            break;
                        }
                }
                else
                    if (max_blocks >= 24 && ( (max_blocks%9) != 0) )
                        max_other_blocks = max_blocks - 9;
                    else
                        if (max_blocks >= 12 && ((max_blocks % 5) != 0)  )
                            max_other_blocks = max_blocks - 5;
                        else
                            if (max_blocks >= 6 && ((max_blocks % 3) != 0) )
                                max_other_blocks = max_blocks - 3;
                            else
                                max_other_blocks = max_blocks + 1;
            }

            for (int i = max_other_blocks - 1; i >= 0; --i)
                _other_blocks.Add(Block.GetBlock());

            _checksums = new List<byte[]>();
        }

        private class CheckSumCalculator
        {
            public byte[] Checksum = new byte[31];
            private int _pos;
            public void PutByte(byte b)
            {
                Checksum[_pos++] ^= b;
                if (_pos >= Checksum.Length)
                    _pos = 0;
            }
            public void PutByte(int b)
            {
                Checksum[_pos++] ^= (byte)b;
                if (_pos >= Checksum.Length)
                    _pos = 0;
            }

        }

        public void ReadFrom(System.IO.Stream s, bool bForTestOnly)
        {
            //ignore the for test only flag
            bForTestOnly = false;

            CheckSumCalculator streamChecksum = new CheckSumCalculator();
            _blocks.Clear();
            _other_blocks.Clear();
            _checksums.Clear();

            Block dummy_block = null;
            Block dummy_block2 = null;
            if (bForTestOnly)
            {
                dummy_block = Block.GetBlock();

                for (int ii = dummy_block.Length - 1; ii >= 0; --ii)
                    dummy_block[ii] = 0;

                dummy_block2 = Block.GetBlock();
            }


            // read in the minimum version
            if (s.ReadByte() != 0)
            {
                // not supported version
                return;
            }

            streamChecksum.PutByte(0);

            // read the maximum supported version (should be different from -1, which means eof)
            int maxVersion = s.ReadByte();
            if (maxVersion == -1)
                return;

            streamChecksum.PutByte(maxVersion);

            // read in the time
            long ticks = 0;
            for (int i = 7; i >= 0; --i)
            {
                int b = s.ReadByte(); if (b == -1) return;
                streamChecksum.PutByte(b);
                ticks = ticks * 256 + b;
            };
            _file_timestamp_utc = new DateTime(ticks);

            // read in the original file length
            _file_length = 0;
            for (int i = 7; i >= 0; --i)
            {
                int b = s.ReadByte(); if (b == -1) return;
                streamChecksum.PutByte(b);
                _file_length = _file_length * 256 + b;
            };

            // read in the blocks number
            long primary_blocks = 0;
            long secondary_blocks = 0;

            for (int i = 7; i >= 0; --i)
            {
                int b = s.ReadByte(); if (b == -1) return;
                streamChecksum.PutByte(b);
                primary_blocks = primary_blocks * 256 + b;
            };

            for (int i = 7; i >= 0; --i)
            {
                int b = s.ReadByte(); if (b == -1) return;
                streamChecksum.PutByte(b);
                secondary_blocks = secondary_blocks * 256 + b;
            };


            for (long i = primary_blocks - 1; i >= 0; --i)
            {
                Block b = null;

                if (bForTestOnly)
                    b = dummy_block2;
                else
                    b = Block.GetBlock();

                try
                {
                    if (b.ReadFrom(s) != b.Length)
                    {
                        _blocks.Clear();
                        return;
                    }

                    if (bForTestOnly)
                        _blocks.Add(dummy_block);
                    else
                        _blocks.Add(b);
                }
                catch (System.IO.IOException)
                {
                    // we don't expect the restore file to be perfect
                    // add a null block for failed reads
                    _blocks.Add(null);
                    // seek the next block
                    s.Seek(b.Length * (primary_blocks - i) + 34, System.IO.SeekOrigin.Begin);
                }
            };

            for (long i = secondary_blocks - 1; i >= 0; --i)
            {
                Block b = null;

                if (bForTestOnly)
                    b = dummy_block2;
                else
                    b = Block.GetBlock();

                try
                {
                    if (b.ReadFrom(s) != b.Length)
                    {
                        // clear both lists if the file has been tampered with
                        _blocks.Clear();
                        _other_blocks.Clear();
                        return;
                    }

                    if (bForTestOnly)
                        _other_blocks.Add(dummy_block);
                    else
                        _other_blocks.Add(b);
                }
                catch (System.IO.IOException)
                {
                    // we don't expect the restore file to be perfect
                    // add a null block for failed reads
                    _other_blocks.Add(null);
                    // seek the next block
                    s.Seek(b.Length * (secondary_blocks - i + primary_blocks) + 34, System.IO.SeekOrigin.Begin);
                };
            };

            int checksumCount = 0;
            for (int i = 7; i >= 0; --i)
            {
                int b = s.ReadByte(); if (b == -1) return;
                streamChecksum.PutByte(b);
                checksumCount = checksumCount * 256 + b;
            };


            for (int i = 0; i < checksumCount; ++i)
            {
                byte[] checksum = new byte[3];
                if (s.Read(checksum, 0, checksum.Length) < checksum.Length)
                    return;

                streamChecksum.PutByte(checksum[0]);
                streamChecksum.PutByte(checksum[1]);
                streamChecksum.PutByte(checksum[2]);

                _checksums.Add(checksum);
            }

            CheckSumCalculator checksumInFile = new CheckSumCalculator();
            if (s.Read(checksumInFile.Checksum, 0, checksumInFile.Checksum.Length) < checksumInFile.Checksum.Length)
            {
                // saved checksums are not reliable, so clear them and trust the CRC checksums of the drive
                _checksums.Clear();
                return;
            }

            // compare calculated checksum to the one, stored in file
            for (int i = 0; i < checksumInFile.Checksum.Length; ++i)
            {
                if (checksumInFile.Checksum[i] != streamChecksum.Checksum[i])
                {
                    // if they differ then checksums in the file are not reliable
                    _checksums.Clear();
                    return;
                }
            }

        }

        public void ReadFrom(System.IO.Stream s)
        {
            ReadFrom(s, false);
        }

        public void ReadForTestOnlyFrom(System.IO.Stream s)
        {
            ReadFrom(s, true);
        }


        public void SaveTo(System.IO.Stream s)
        {
            CheckSumCalculator streamChecksum = new CheckSumCalculator();

            // minimum version
            s.WriteByte(0);
            streamChecksum.PutByte(0);

            // maximum version
            s.WriteByte(0);
            streamChecksum.PutByte(0);

            ulong tostore = (ulong)(_file_timestamp_utc.Ticks);
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(tostore >> 56);
                streamChecksum.PutByte(b);
                tostore = tostore * 256;
                s.WriteByte(b);
            };

            tostore = (ulong)_file_length;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(tostore >> 56);
                streamChecksum.PutByte(b);
                tostore = tostore * 256;
                s.WriteByte(b);
            };


            tostore = (ulong)_blocks.Count;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(tostore >> 56);
                streamChecksum.PutByte(b);
                tostore = tostore * 256;
                s.WriteByte(b);
            };

            tostore = (ulong)_other_blocks.Count;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(tostore >> 56);
                streamChecksum.PutByte(b);
                tostore = tostore * 256;
                s.WriteByte(b);
            };

            for (int i = 0; i < _blocks.Count; ++i)
                _blocks[i].WriteTo(s);

            for (int i = 0; i < _other_blocks.Count; ++i)
                _other_blocks[i].WriteTo(s);

            tostore = (ulong)_checksums.Count;
            for (int i = 7; i >= 0; --i)
            {
                byte b = (byte)(tostore >> 56);
                streamChecksum.PutByte(b);
                tostore = tostore * 256;
                s.WriteByte(b);
            };

            for (int i = 0; i < _checksums.Count; ++i)
            {
                s.Write(_checksums[i], 0, _checksums[i].Length);
                streamChecksum.PutByte(_checksums[i][0]);
                streamChecksum.PutByte(_checksums[i][1]);
                streamChecksum.PutByte(_checksums[i][2]);
            }

            // save the checksum of checksums, so we know if they are reliable
            s.Write(streamChecksum.Checksum, 0, streamChecksum.Checksum.Length);
        }

        public void Analyze(Block b, long index)
        {
            if (_blocks.Count == 0)
                return;

            //_blocks[(int)(index % _blocks.Count)] = _blocks[(int)(index % _blocks.Count)] ^ b;
            _blocks[(int)(index % _blocks.Count)].DoXor(b);

            if (_other_blocks.Count > 0)
                //_other_blocks[(int)(index % _other_blocks.Count)] = _blocks[(int)(index % _other_blocks.Count)] ^ b;
                _other_blocks[(int)(index % _other_blocks.Count)].DoXor(b);

            // if we can save checksum of the block in a list
            if (index < int.MaxValue)
            {
                // calc a checksum for later verification of the block
                // you may wonder why I used 3... Why not?
                byte[] checksum = new byte[3];
                int currentIndex = 0;
                foreach (byte by in b)
                {
                    checksum[currentIndex++] ^= by;
                    if (currentIndex >= checksum.Length)
                        currentIndex = 0;
                }
                while (_checksums.Count <= index)
                    _checksums.Add(new byte[] { 0, 0, 0 });
                _checksums[(int)index] = checksum;
            }
        }

        long _currentRestore = -1;
        List<long> _blocksToRestore;

        public void StartRestore()
        {
            _currentRestore = -1;
            _blocksToRestore = new List<long>();

            //for (int i = 0; i < _blocks.Count; ++i)
            //{
            //    for (int j = 2; j < 4096; ++j)
            //    {
            //        if (_blocks[i][j] == 253 && _blocks[i][j - 1] == 254 && _blocks[i][j - 2] == 63)
            //        {
            //            System.Console.WriteLine("position: " + (i.ToString()) + "/" + (j.ToString()));
            //        }
            //        if (_blocks[i][j] == 63 && _blocks[i][j - 1] == 254 && _blocks[i][j - 2] == 253)
            //        {
            //            System.Console.WriteLine("rposition: " + (i.ToString()) + "/" + (j.ToString()));
            //        }
            //    }
            //}
        }

        public bool AnalyzeForRestore(Block b, long index)
        {

            if (_blocks.Count == 0)
                return false;

            // if there are not enough checksums (e.g. .chk file from old version)
            // then simply add the block as OK
            if (_checksums.Count <= index)
            {
                //_blocks[(int)(index % _blocks.Count)] = _blocks[(int)(index % _blocks.Count)] ^ b;
                _blocks[(int)(index % _blocks.Count)].DoXor(b);

                // if there is a second row of blocks, then perform also preparation of other blocks;
                if (_other_blocks.Count > 0)
                    //_other_blocks[(int)(index % _other_blocks.Count)] = _other_blocks[(int)(index % _other_blocks.Count)] ^ b;
                    _other_blocks[(int)(index % _other_blocks.Count)].DoXor(b);

                // analyze which blocks have been skipped
                while (++_currentRestore < index)
                {
                    _blocksToRestore.Add(_currentRestore);
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
                if (checksum[i] != _checksums[(int)index][i])
                    bChecksumOk = false;

            if (!bChecksumOk)
            {
                // don't add to restore there, we could try several variants
                //_blocksToRestore.Add(index);
                return false;
            }
            else
            {
                //_blocks[(int)(index % _blocks.Count)] = _blocks[(int)(index % _blocks.Count)] ^ b;
                _blocks[(int)(index % _blocks.Count)].DoXor(b);

                // if there is a second row of blocks, then perform also preparation of other blocks;
                if (_other_blocks.Count > 0)
                    //_other_blocks[(int)(index % _other_blocks.Count)] = _other_blocks[(int)(index % _other_blocks.Count)] ^ b;
                    _other_blocks[(int)(index % _other_blocks.Count)].DoXor(b);

                // analyze which blocks have been skipped
                while (++_currentRestore < index)
                {
                    _blocksToRestore.Add(_currentRestore);
                };

                return true;
            }
        }

        public bool AnalyzeForTest(Block b, long index)
        {
            // ignore the for test only
            return AnalyzeForRestore(b, index);

            /*
            if (_blocks.Count == 0)
                return false;

            // throw away all the data, since we are only testing
            if (_blocks.Count>1 && _blocks[0]!=_blocks[1])
            {
                Block dummy = Block.GetBlock();
                for (int iii = _blocks.Count - 1; iii >= 0; --iii)
                    _blocks[iii] = dummy;
            }
            if (_other_blocks.Count > 1 && _other_blocks[0] != _other_blocks[1])
            {
                Block dummy = Block.GetBlock();
                for (int iii = _other_blocks.Count - 1; iii >= 0; --iii)
                    _other_blocks[iii] = dummy;
            }

            // if there are not enough checksums (e.g. .chk file from old version)
            // then simply add the block as OK
            if (_checksums.Count <= index)
            {
                // analyze which blocks have been skipped
                while (++_currentRestore < index)
                {
                    _blocksToRestore.Add(_currentRestore);
                };

                return true;
            }


            // calc a checksum for verification of the block
            byte[] checksum = new byte[3];
            int currentIndex = 0;
            int ii = 0;
            byte b0 = 0;
            byte b1 = 0;
            byte b2 = 0;
            while (ii < b.Length-9)
            {
                b0 = (byte)(b0 ^ b[ii] ^ b[ii + 3] ^ b[ii + 6]);
                b1 = (byte)(b1 ^ b[ii+1] ^ b[ii + 4] ^ b[ii + 7]);
                b2 = (byte)(b2 ^ b[ii+2] ^ b[ii + 5] ^ b[ii + 8]);
                ii += 9;
            }

            checksum[0] = b0;
            checksum[1] = b1;
            checksum[2] = b2;

            for (; ii<b.Length; ii++) { byte by = b[ii];
            //foreach (byte by in b) {

                checksum[currentIndex++] ^= by;
                if (currentIndex >= checksum.Length)
                    currentIndex = 0;
            }

            bool bChecksumOk = true;
            for (int i = checksum.Length - 1; i >= 0; --i)
                if (checksum[i] != _checksums[(int)index][i])
                    bChecksumOk = false;

            if (bChecksumOk)
            {
                // analyze which blocks have been skipped
                while (++_currentRestore < index)
                {
                    _blocksToRestore.Add(_currentRestore);
                };
                return true;
            }
            else
                return false;
             */
        }


        public List<RestoreInfo> EndRestore(out long notRestoredSize, string strCurrentFile, ILogWriter iLogWriter)
        {
            Block testb = Block.GetBlock();
            while (++_currentRestore < (_file_length + testb.Length - 1) / testb.Length)
            {
                _blocksToRestore.Add(_currentRestore);
            };

            List<RestoreInfo> result = new List<RestoreInfo>();
            if (_blocks.Count == 0)
            {
                notRestoredSize = 0;
                for (int i = 0; i < _blocksToRestore.Count; i++)
                {
                    long blockToRestore = _blocksToRestore[i];
                    notRestoredSize = notRestoredSize + testb.Length;
                    result.Add(new RestoreInfo(blockToRestore * testb.Length, Block.GetBlock(), true));
                }
                return result;
            };

            // if there is no second row of blocks, then just use the primary row
            if (_other_blocks.Count == 0)
            {
                List<int> usedBlocks = new List<int>();
                List<int> notRestorable = new List<int>();

                for (int i = 0; i < _blocksToRestore.Count; i++)
                {
                    long blockToRestore = _blocksToRestore[i];
                    int blockToUse = (int)(blockToRestore % _blocks.Count);
                    if (usedBlocks.Contains(blockToUse) || _blocks[blockToUse] == null)
                    {
                        if (!notRestorable.Contains(blockToUse))
                            notRestorable.Add(blockToUse);
                    }
                    else
                    {
                        usedBlocks.Add(blockToUse);
                    }
                }

                // check integrity of remaining blocks
                // if there is doubt that other blocks are valid then don't use 
                // the checksum blocks
                bool bOtherSeemOk = true;
                for (int i = _blocks.Count - 1; i >= 0; --i)
                {
                    if (!usedBlocks.Contains(i))
                    {
                        Block b = _blocks[i];
                        for (int j = b.Length - 1; j >= 0; --j)
                            if (b[j] != 0)
                            {
                                bOtherSeemOk = false;
                                break;
                            }
                    }

                    if (!bOtherSeemOk)
                    {
                        iLogWriter.WriteLog(1, "Warning: several blocks don't match in restoreinfo ", strCurrentFile, ", restoreinfo will be ignored completely");
                        break;
                    }
                }
                

                notRestoredSize = 0;
                for (int i = 0; i < _blocksToRestore.Count; i++)
                {
                    long blockToRestore = _blocksToRestore[i];
                    int blockToUse = (int)(blockToRestore % _blocks.Count);
                    if (notRestorable.Contains(blockToUse) || !bOtherSeemOk)
                    {
                        notRestoredSize = notRestoredSize + testb.Length;
                        result.Add(new RestoreInfo(blockToRestore * testb.Length, Block.GetBlock(), true));
                    }
                    else
                    {
                        result.Add(new RestoreInfo(blockToRestore * testb.Length, _blocks[blockToUse], false));
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
                    List<int> proposedBlocks = new List<int>();
                    List<int> proposedOtherBlocks = new List<int>();
                    List<int> notUsableBlocks = new List<int>();
                    List<int> notUsableOtherBlocks = new List<int>();

                    for (int i = 0; i < _blocksToRestore.Count; i++)
                    {
                        long blockToRestore = _blocksToRestore[i];
                        int blockToUse = (int)(blockToRestore % _blocks.Count);
                        if (proposedBlocks.Contains(blockToUse) || _blocks[blockToUse] == null)
                        {
                            if (!notUsableBlocks.Contains(blockToUse))
                                notUsableBlocks.Add(blockToUse);
                        }
                        else
                        {
                            proposedBlocks.Add(blockToUse);
                        }
                    }

                    for (int i = _blocksToRestore.Count - 1; i >= 0; i--)
                    {
                        long blockToRestore = _blocksToRestore[i];
                        int blockToUse = (int)(blockToRestore % _blocks.Count);
                        if (!notUsableBlocks.Contains(blockToUse))
                        {
                            bool bChecksumOk2 = true;

                            if (blockToRestore < _checksums.Count)
                            {
                                // calc a checksum for verification of the block
                                byte[] checksum = new byte[3];
                                int currentIndex = 0;
                                foreach (byte by in _blocks[blockToUse])
                                {
                                    checksum[currentIndex++] ^= by;
                                    if (currentIndex >= checksum.Length)
                                        currentIndex = 0;
                                }

                                for (int j = checksum.Length - 1; j >= 0; --j)
                                    if (checksum[j] != _checksums[(int)blockToRestore][j])
                                        bChecksumOk2 = false;
                            }

                            // if there is a checksum for the block then we'll use it only if checksum matches
                            if (bChecksumOk2)
                            {
                                result.Add(new RestoreInfo(blockToRestore * testb.Length, _blocks[blockToUse], false));

                                // we calculated the new block, it could improve the situation at the other row of blocks
                                _other_blocks[(int)(blockToRestore % _other_blocks.Count)] = _other_blocks[(int)(blockToRestore % _other_blocks.Count)] ^ _blocks[blockToUse];

                                _blocksToRestore.RemoveAt(i);

                                // no need to repeat if we restored something from the primary row of blocks,
                                // since we run analysis on the second row of blocks anyway
                                // bRepeat = true;
                            }
                            else
                            {
                                iLogWriter.WriteLog(1, "Warning: checksum of block at offsset ",blockToRestore * testb.Length," doesn't match available in primary blocks of restoreinfo ", strCurrentFile, ", primary restoreinfo for the block will be ignored");
                            }
                        }
                    }


                    for (int i = _blocksToRestore.Count - 1; i >= 0; i--)
                    {
                        long blockToRestore = _blocksToRestore[i];
                        int blockToUse = (int)(blockToRestore % _other_blocks.Count);
                        if (proposedOtherBlocks.Contains(blockToUse) || _other_blocks[blockToUse] == null)
                        {
                            if (!notUsableOtherBlocks.Contains(blockToUse))
                                notUsableOtherBlocks.Add(blockToUse);
                        }
                        else
                        {
                            proposedOtherBlocks.Add(blockToUse);
                        }
                    }

                    for (int i = _blocksToRestore.Count - 1; i >= 0; i--)
                    {
                        long blockToRestore = _blocksToRestore[i];
                        int blockToUse = (int)(blockToRestore % _other_blocks.Count);
                        if (!notUsableOtherBlocks.Contains(blockToUse))
                        {
                            bool bChecksumOk3 = true;

                            if (blockToRestore < _checksums.Count)
                            {
                                // calc a checksum for verification of the block
                                byte[] checksum = new byte[3];
                                int currentIndex = 0;
                                foreach (byte by in _other_blocks[blockToUse])
                                {
                                    checksum[currentIndex++] ^= by;
                                    if (currentIndex >= checksum.Length)
                                        currentIndex = 0;
                                }

                                for (int j = checksum.Length - 1; j >= 0; --j)
                                    if (checksum[j] != _checksums[(int)blockToRestore][j])
                                        bChecksumOk3 = false;
                            }

                            if (bChecksumOk3)
                            {
                                // we calculated the new block, it could improve the situation at the primary row of blocks
                                _blocks[(int)(blockToRestore % _blocks.Count)] = _blocks[(int)(blockToRestore % _blocks.Count)] ^ _other_blocks[blockToUse];

                                // skip these two lines in test case for testing the "repeat" case 
                                result.Add(new RestoreInfo(blockToRestore * testb.Length, _other_blocks[blockToUse], false));
                                _blocksToRestore.RemoveAt(i);

                                // repeat restoring using the primary and the secondary row of blocks:
                                // we restored something using the second row, this could improve the situation with the primary blocks.
                                bRepeat = true;
                            }
                            else
                            {
                                iLogWriter.WriteLog(1, "Warning: checksum of block at offset ", blockToRestore * testb.Length, " doesn't match available in secondary blocks of restoreinfo ", strCurrentFile, ", secondary restoreinfo for the block will be ignored");
                            }
                        }
                    }

                    // don't repeat if nothing more to restore
                    if (_blocksToRestore.Count == 0)
                        bRepeat = false;
                }

                // reset other blocks
                notRestoredSize = 0;
                for (int i = 0; i < _blocksToRestore.Count; i++)
                {
                    long blockToRestore = _blocksToRestore[i];
                    notRestoredSize = notRestoredSize + testb.Length;
                    result.Add(new RestoreInfo(blockToRestore * testb.Length, Block.GetBlock(), true));
                }

            }
            return result;
        }

        public bool VerifyIntegrityAfterRestoreTest()
        {
            // if the original file matches the chk file and there were no checksum errors
            //  then all contents of the _blocks shall be zero
            for (int i = _blocks.Count - 1; i >= 0; --i)
            {
                Block b = _blocks[i];
                for (int j = b.Length - 1; j >= 0; --j)
                    if (b[j] != 0)
                        return false;
            }

            if (_other_blocks.Count > 0)
            {
                // if the original file matches the chk file and there were no checksum errors
                //  then all contents of the _other_blocks also shall be zero
                for (int i = _other_blocks.Count - 1; i >= 0; --i)
                {
                    Block b = _other_blocks[i];
                    for (int j = b.Length - 1; j >= 0; --j)
                        if (b[j] != 0)
                            return false;
                }
            }
            return true;
        }

        public int NonEmptyBlocks()
        {
            int cnt = 0;
            foreach (Block blk in _blocks)
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


        public void ImproveWith(SaveInfo other)
        {
            if (this._checksums.Count == 0 && other._checksums.Count > 0)
            {
                this._checksums = other._checksums;
                // remove blocks on this saveinfo, if too many
                while (this._blocks.Count > other._blocks.Count)
                {
                    this._blocks.RemoveAt(this._blocks.Count - 1);
                }
                while (this._other_blocks.Count > other._other_blocks.Count)
                {
                    this._other_blocks.RemoveAt(this._other_blocks.Count - 1);
                }
            }
            else
            {
                if (other._checksums.Count == 0 && this._checksums.Count > 0)
                {
                    other._checksums = this._checksums;
                    // remove blocks on the other saveinfo, if too many
                    while (other._blocks.Count > this._blocks.Count)
                    {
                        other._blocks.RemoveAt(other._blocks.Count - 1);
                    }
                    while (other._other_blocks.Count > this._other_blocks.Count)
                    {
                        other._other_blocks.RemoveAt(other._other_blocks.Count - 1);
                    }
                }
            }

            // add missing blocks on the other
            for (int i=0;i<_blocks.Count;++i)
            {
                if (other._blocks.Count <= i)
                    other._blocks.Add(null);

                if (other._blocks[i]==null && this._blocks[i]!=null)
                {
                    Block newB = Block.GetBlock();
                    Block oldB = this._blocks[i];
                    for (int j = newB.Length - 1; j >= 0; --j)
                        newB[j] = oldB[j];
                    other._blocks[i] = newB;
                }
            }

            for (int i = 0; i < _other_blocks.Count; ++i)
            {
                if (other._other_blocks.Count <= i)
                    other._other_blocks.Add(null);

                if (other._other_blocks[i] == null && this._other_blocks[i] != null)
                {
                    Block newB = Block.GetBlock();
                    Block oldB = this._other_blocks[i];
                    for (int j = newB.Length - 1; j >= 0; --j)
                        newB[j] = oldB[j];
                    other._other_blocks[i] = newB;
                }
            }

            // add missing blocks on this
            for (int i = 0; i < other._blocks.Count; ++i)
            {
                if (this._blocks.Count <= i)
                    this._blocks.Add(null);

                if (this._blocks[i] == null && other._blocks[i] != null)
                {
                    Block newB = Block.GetBlock();
                    Block oldB = other._blocks[i];
                    for (int j = newB.Length - 1; j >= 0; --j)
                        newB[j] = oldB[j];
                    this._blocks[i] = newB;
                }
            }

            for (int i = 0; i < other._other_blocks.Count; ++i)
            {
                if (this._other_blocks.Count <= i)
                    this._other_blocks.Add(null);

                if (this._other_blocks[i] == null && other._other_blocks[i] != null)
                {
                    Block newB = Block.GetBlock();
                    Block oldB = other._other_blocks[i];
                    for (int j = newB.Length - 1; j >= 0; --j)
                        newB[j] = oldB[j];
                    this._other_blocks[i] = newB;
                }
            }

        }
    }
}
