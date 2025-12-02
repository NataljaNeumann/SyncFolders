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

using SyncFoldersApi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable NUnit2005

namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// This class tests block functionality
    /// </summary>
    //*******************************************************************************************************
    [TestFixture]
    [NonParallelizable]
    public class T010_BlockTests
    {
        //===================================================================================================
        /// <summary>
        /// The expected block size. All files expect this block size
        /// </summary>
        private const int BlockSize = 4096;

        //===================================================================================================
        /// <summary>
        /// Creates a block with given data
        /// </summary>
        /// <param name="data">Initialization dataa</param>
        /// <returns>Block</returns>
        //===================================================================================================
        private Block CreateBlock(params byte[] data)
        {
            var oBlock = new Block();

            for (int i = data.Length - 1; i >= 0; --i)
                oBlock[i] = data[i];

            return oBlock;
        }

        //===================================================================================================
        /// <summary>
        /// Creates a memory stream from given bytes
        /// </summary>
        /// <param name="aData">bytes</param>
        /// <returns>new memory stream. The caller must dispose the object</returns>
        //===================================================================================================
        private MemoryStream CreateStream(params byte[] aData)
        {
            return new MemoryStream(aData);
        }

        //===================================================================================================
        /// <summary>
        /// Creates an IFile object from given data
        /// </summary>
        /// <param name="aData">Initialization data</param>
        /// <returns>A new IFile object. The caller needs to dispose the object</returns>
        //===================================================================================================
        private IFile CreateMockFile(params byte[] aData)
        {
            var oStream = new MemoryStream();
            oStream.Write(aData, 0, aData.Length);
            oStream.Position = 0;
            Dictionary<string, DateTime> oDic = new Dictionary<string, DateTime>();
            oDic["MockFile"] = DateTime.UtcNow;
            return new InMemoryFile(oStream, oDic, "MockFile", false);
        }

        //===================================================================================================
        /// <summary>
        /// Tests creation of a new empty block
        /// </summary>
        //===================================================================================================
        [Test]
        public void Constructor_ShouldCreateEmptyBlock()
        {
            var block = new Block();
            Assert.AreEqual(BlockSize, block.Length);
            for (int i = block.Length - 1; i >= 0; --i)
                Assert.Zero(block[i]);
        }

        //===================================================================================================
        /// <summary>
        /// Tests indexer
        /// </summary>
        //===================================================================================================
        [Test]
        public void Indexer_ShouldReturnCorrectByte()
        {
            var block = CreateBlock(10, 20, 30);
            Assert.AreEqual(20, block[1]);
        }

        //===================================================================================================
        /// <summary>
        /// Tests bitwise or
        /// </summary>
        //===================================================================================================
        [Test]
        public void BitwiseOr_ShouldCombineBytes()
        {
            var b1 = CreateBlock(0b00001111);
            var b2 = CreateBlock(0b10110000);
            var result = b1 | b2;
            Assert.AreEqual(0b10111111, result[0]);
        }

        //===================================================================================================
        /// <summary>
        /// Tests bitwise and
        /// </summary>
        //===================================================================================================
        [Test]
        public void BitwiseAnd_ShouldIntersectBytes()
        {
            var b1 = CreateBlock(0b11001100);
            var b2 = CreateBlock(0b10101010);
            var result = b1 & b2;
            Assert.AreEqual(0b10001000, result[0]);
        }

        //===================================================================================================
        /// <summary>
        /// Tests bitwise xor operator
        /// </summary>
        //===================================================================================================
        [Test]
        public void BitwiseXor_ShouldToggleBits()
        {
            var b1 = CreateBlock(0b11110000);
            var b2 = CreateBlock(0b00001101);
            var result = b1 ^ b2;
            Assert.AreEqual(0b11111101, result[0]);
        }

        //===================================================================================================
        /// <summary>
        /// Tests bitwise inversion
        /// </summary>
        //===================================================================================================
        [Test]
        public void BitwiseNot_ShouldInvertBits()
        {
            var b = CreateBlock(0b00000010);
            var result = ~b;
            Assert.AreEqual(0b11111101, result[0]);
        }

        //===================================================================================================
        /// <summary>
        /// Tests application of XOR on the block itself
        /// </summary>
        //===================================================================================================
        [Test]
        public void DoXor_ShouldMutateBlock()
        {
            var b1 = CreateBlock(0b10101010);
            var b2 = CreateBlock(0b00010101);
            b1.DoXor(b2);
            Assert.AreEqual(0b10111111, b1[0]);
        }

        //===================================================================================================
        /// <summary>
        /// Reading from stream
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadFromStream_ShouldLoadData()
        {
            var oBlock = new Block();
            using (var oStream = CreateStream(1, 2, 3))
            {
                int nRead = oBlock.ReadFrom(oStream);
                Assert.AreEqual(3, nRead);
                Assert.AreEqual(2, oBlock[1]);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Reading from IFile
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadFromIFile_ShouldLoadData()
        {
            var oBlock = new Block();
            using (var oStream = CreateMockFile(1, 2, 3))
            {
                int nRead = oBlock.ReadFrom(oStream);
                Assert.AreEqual(3, nRead);
                Assert.AreEqual(2, oBlock[1]);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Reading first part of block
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadFirstPartFromStream_ShouldLoadPartialData()
        {
            var oBlock = new Block();
            using (var oStream = CreateStream(10, 20, 30))
            {
                int nRead = oBlock.ReadFirstPartFrom(oStream, 2);
                Assert.AreEqual(2, nRead);
                Assert.AreEqual(10, oBlock[0]);
                Assert.AreEqual(20, oBlock[1]);
                for (int i = oBlock.Length - 1; i >= 2; --i)
                    Assert.Zero(oBlock[i]);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Reading first part of block
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadFirstPartFromIFile_ShouldLoadPartialData()
        {
            var oBlock = new Block();
            using (var oStream = CreateMockFile(10, 20, 30))
            {
                int nRead = oBlock.ReadFirstPartFrom(oStream, 2);
                Assert.AreEqual(2, nRead);
                Assert.AreEqual(10, oBlock[0]);
                Assert.AreEqual(20, oBlock[1]);
                for (int i = oBlock.Length - 1; i >= 2; --i)
                    Assert.Zero(oBlock[i]);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Reads last part of the block
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadLastPartFromStream_ShouldLoadTailData()
        {
            var oBlock = new Block();
            using (var oStream = CreateStream(10, 20, 30))
            {
                int nRead = oBlock.ReadLastPartFrom(oStream, 2);
                Assert.AreEqual(2, nRead);
                Assert.AreEqual(10, oBlock[oBlock.Length - 2]);
                Assert.AreEqual(20, oBlock[oBlock.Length - 1]);
                for (int i = oBlock.Length - 3; i >= 0; --i)
                    Assert.Zero(oBlock[i]);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Reads last part of the block
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadLastPartFromIFile_ShouldLoadTailData()
        {
            var oBlock = new Block();
            using (var oStream = CreateMockFile(10, 20, 30))
            {
                int nRead = oBlock.ReadLastPartFrom(oStream, 2);
                Assert.AreEqual(2, nRead);
                Assert.AreEqual(10, oBlock[oBlock.Length - 2]);
                Assert.AreEqual(20, oBlock[oBlock.Length - 1]);
                for (int i = oBlock.Length - 3; i >= 0; --i)
                    Assert.Zero(oBlock[i]);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests if writing works
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteToStream_ShouldOutputData()
        {
            var oBlock = CreateBlock(5, 6, 7);
            using (var oStream = new MemoryStream())
            {
                oBlock.WriteTo(oStream);
                var aResult = oStream.ToArray();
                var aExpected = new byte[BlockSize];
                aExpected[0] = 5;
                aExpected[1] = 6;
                aExpected[2] = 7;
                Assert.AreEqual(aExpected, aResult);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests if writing works
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteToIFile_ShouldOutputData()
        {
            var oBlock = CreateBlock(5, 6, 7);
            using (var oStream = CreateMockFile())
            {
                oBlock.WriteTo(oStream);
                var aResult = ((InMemoryFile)oStream).ToArray();
                var aExpected = new byte[BlockSize];
                aExpected[0] = 5;
                aExpected[1] = 6;
                aExpected[2] = 7;
                Assert.AreEqual(aExpected, aResult);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Tests, if writing first part works
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteFirstPartToStream_ShouldWritePartialData()
        {
            var oBlock = CreateBlock(1, 2, 3, 4);
            using (var oStream = new MemoryStream())
            {
                oBlock.WriteFirstPartTo(oStream, 2);
                var result = oStream.ToArray();
                Assert.AreEqual(new byte[] { 1, 2 }, result);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if writing first part works
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteFirstPartToIFile_ShouldWritePartialData()
        {
            var oBlock = CreateBlock(1, 2, 3, 4);
            using (var oStream = CreateMockFile())
            {
                oBlock.WriteFirstPartTo(oStream, 2);
                var result = ((InMemoryFile)oStream).ToArray();
                Assert.AreEqual(new byte[] { 1, 2 }, result);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if writing last part works
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteLastPartToStream_ShouldWriteTailData()
        {
            var oBlock = CreateBlock();
            oBlock[oBlock.Length - 1] = 4;
            oBlock[oBlock.Length - 2] = 3;
            oBlock[oBlock.Length - 3] = 2;
            oBlock[oBlock.Length - 4] = 1;
            using (var oStream = new MemoryStream())
            {
                oBlock.WriteLastPartTo(oStream, 2);
                var result = oStream.ToArray();
                Assert.AreEqual(new byte[] { 3, 4 }, result);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if writing last part works
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteLastPartToIFile_ShouldWriteTailData()
        {
            var oBlock = CreateBlock();
            oBlock[oBlock.Length - 1] = 4;
            oBlock[oBlock.Length - 2] = 3;
            oBlock[oBlock.Length - 3] = 2;
            oBlock[oBlock.Length - 4] = 1;
            using (var oStream = CreateMockFile())
            {
                oBlock.WriteLastPartTo(oStream, 2);
                var result = ((InMemoryFile)oStream).ToArray();
                Assert.AreEqual(new byte[] { 3, 4 }, result);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Tests equality comparer
        /// </summary>
        //===================================================================================================
        [Test]
        public void Equals_ShouldCompareBlocks()
        {
            var b1 = CreateBlock(1, 2, 3);
            var b2 = CreateBlock(1, 2, 3);
            var b3 = CreateBlock(3, 2, 1);
            Assert.IsTrue(b1.Equals(b2));
            Assert.IsFalse(b1.Equals(b3));
        }

        //===================================================================================================
        /// <summary>
        /// Tests iterator
        /// </summary>
        //===================================================================================================
        [Test]
        public void GetEnumerator_ShouldIterateBytes()
        {
            var oBlock = CreateBlock(9, 8, 7);
            var oList = oBlock.ToList();

            var aExpected = new byte[BlockSize];
            aExpected[0] = 9;
            aExpected[1] = 8;
            aExpected[2] = 7;

            CollectionAssert.AreEqual(aExpected, oList.ToArray());
        }

        //===================================================================================================
        /// <summary>
        /// Testing reading some more bytes
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadFromStream_PartialFill_ShouldZeroTail()
        {
            byte[] aInput = Enumerable.Range(1, 100).Select(i => (byte)i).ToArray();
            var oBlock = new Block();
            using (var oStream = new MemoryStream(aInput))
            {
                int nRead = oBlock.ReadFrom(oStream);

                Assert.AreEqual(100, nRead);
                Assert.AreEqual(1, oBlock[0]);
                Assert.AreEqual(100, oBlock[99]);

                // zero-filled tail
                for (int i = oBlock.Length - 1; i >= 100; --i)
                    Assert.Zero(oBlock[i]);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Writing a longer part
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteToStream_PartialWrite_ShouldWriteOnlySpecifiedLength()
        {
            var oBlock = CreateBlock(Enumerable.Repeat((byte)7, BlockSize).ToArray());
            using (var oStream = new MemoryStream())
            {
                oBlock.WriteTo(oStream, 100);
                byte[] aResult = oStream.ToArray();

                Assert.AreEqual(100, aResult.Length);
                Assert.IsTrue(aResult.All(b => b == 7));
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests reading first part
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadFirstPartFromStream_ShouldZeroRemaining()
        {
            byte[] aInput = Enumerable.Range(1, 150).Select(i => (byte)i).ToArray();
            var oBlock = new Block();
            using (var oStream = new MemoryStream(aInput))
            {
                int nRead = oBlock.ReadFirstPartFrom(oStream, 100);

                Assert.AreEqual(nRead, oStream.Position);
                Assert.AreEqual(100, nRead);
                Assert.AreEqual(1, oBlock[0]);
                Assert.AreEqual(100, oBlock[99]);

                // zero-filled tail
                for (int i = oBlock.Length - 1; i >= 100; --i)
                    Assert.Zero(oBlock[i]);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Reading last part of the stream
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadLastPartFromStream_ShouldZeroPrefix()
        {
            byte[] input = Enumerable.Range(1, 50).Select(i => (byte)i).ToArray();
            var block = new Block();
            using (var stream = new MemoryStream(input))
            {
                int nRead = block.ReadLastPartFrom(stream, 50);

                Assert.AreEqual(50, nRead);
                Assert.AreEqual(0, block[0]);
                Assert.AreEqual(0, block[BlockSize - 51]);
                Assert.AreEqual(1, block[BlockSize - 50]);
                Assert.AreEqual(50, block[BlockSize - 1]);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests that accessing out of range throws exception
        /// </summary>
        //===================================================================================================
        [Test]
        public void Indexer_OutOfRange_Throws()
        {
            var oBlock = new Block();
            byte b = 0;
            Assert.Throws<IndexOutOfRangeException>(() => b = oBlock[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => b = oBlock[BlockSize]);
        }

        //===================================================================================================
        /// <summary>
        /// Writing a complete block
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteToStream_FullBlock_ShouldWrite4096Bytes()
        {
            var oBlock = CreateBlock(Enumerable.Repeat((byte)1, 100).ToArray());
            using (var oStream = new MemoryStream())
            {
                oBlock.WriteTo(oStream);
                Assert.AreEqual(BlockSize, oStream.Length);
            }
        }

        //===================================================================================================
        /// <summary>
        /// This function is used in MultithreadedBlockCreationAndUsage test,
        /// so all pointers of blocks run out of scope before garbage collection
        /// </summary>
        //===================================================================================================
        private void BlockUsageFunction()
        {
            Block b1 = new Block();
            Assert.Zero(b1[0]);
            Assert.Zero(b1[b1.Length - 1]);
            b1[0] = 1;
            b1[b1.Length - 1] = 1;
            Assert.AreEqual(1, b1[0]);
            Block b2 = new Block();
            Assert.AreEqual(1, b1[0]);
            Assert.Zero(b2[0]);
            Assert.Zero(b2[1]);
            Assert.Zero(b2[b2.Length - 1]);
            b2 = b2 | b1;
            Assert.AreEqual(1, b1[0]);
            Assert.AreEqual(1, b2[0]);
            Assert.Zero(b2[1]);
            Assert.AreEqual(1, b2[b2.Length - 1]);
            Block b3 = b2 & b1;
            Assert.AreEqual(1, b3[0]);
            Assert.Zero(b3[1]);
            Assert.AreEqual(1, b3[b3.Length - 1]);
            Block b4 = ~b3;
            Assert.AreEqual(1, b3[0]);
            Assert.AreEqual(254, b4[0]);
            Assert.AreEqual(255, b4[1]);
            Assert.AreEqual(254, b4[b4.Length - 1]);
            Block b5 = b3 ^ b4;
            Assert.AreEqual(1, b3[0]);
            Assert.AreEqual(254, b4[0]);
            Assert.AreEqual(255, b5[0]);
            Assert.AreEqual(255, b5[1]);
            Assert.AreEqual(255, b5[b5.Length - 1]);
        }


        //===================================================================================================
        /// <summary>
        /// Tests creation of a new empty block
        /// </summary>
        //===================================================================================================
        [Test]
        public void MultithreadedBlockCreationAndUsage()
        {
            int nTotalCores = Environment.ProcessorCount;
            var oOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = nTotalCores - 1
            };

            Parallel.For(0, 1000, oOptions, i =>
            {
                BlockUsageFunction();

                var oBlock = new Block();
                Assert.AreEqual(BlockSize, oBlock.Length);


                // collect garbage and start from the beginning
                GC.Collect();
                if ((i / 10) % 10 == 0)
                    System.Threading.Thread.Sleep(100);

                for (int j = oBlock.Length - 1; j >= 0; --j)
                    Assert.Zero(oBlock[j]);
            });
        }

        //===================================================================================================
        /// <summary>
        /// Tests that Block.Erase() works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void Erase_FillsBlockWithZeros()
        {
            var oBlock = new Block();
            for (int i = 0; i < oBlock.Length; i++)
                oBlock[i] = 0xFF;

            oBlock.Erase();

            for (int i = 0; i < oBlock.Length; i++)
                Assert.AreEqual(0, oBlock[i]);
        }


        //===================================================================================================
        /// <summary>
        /// Tests that Block.EraseFrom works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void EraseFrom_FillsFromPositionWithZeros()
        {
            var oBlock = new Block();
            for (int nStartPos = 0; nStartPos < oBlock.Length; nStartPos += 1024)
            {
                for (int i = 0; i < oBlock.Length; i++)
                    oBlock[i] = 0xFF;

                oBlock.EraseFrom(nStartPos);

                for (int i = 0; i < nStartPos; i++)
                    Assert.AreEqual(0xFF, oBlock[i]);

                for (int i = nStartPos; i < oBlock.Length; i++)
                    Assert.AreEqual(0, oBlock[i]);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests that Block.EraseTo works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void EraseTo_FillsToPositionWithZeros()
        {
            var oBlock = new Block();
            for (int nEndPos = 0; nEndPos < oBlock.Length; nEndPos += 1024)
            {
                for (int i = 0; i < oBlock.Length; i++)
                    oBlock[i] = 0xFF;

                oBlock.EraseTo(nEndPos);

                for (int i = 0; i < nEndPos; i++)
                    Assert.AreEqual(0, oBlock[i]);

                for (int i = nEndPos; i < oBlock.Length; i++)
                    Assert.AreEqual(0xFF, oBlock[i]);
            }
        }
    }
}

