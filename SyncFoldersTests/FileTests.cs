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

using NUnit.Framework;
using System;
using System.IO;
using SyncFoldersApi;

#pragma warning disable NUnit2005

namespace SyncFoldersTests
{



    //***************************************************************************************************
    /// <summary>
    /// Base for file tests
    /// </summary>
    //***************************************************************************************************
    public abstract class FileTestsBase
    {
        //===============================================================================================
        /// <summary>
        /// Represents an abstraction for file system operations.
        /// </summary>
        /// <remarks>This interface is intended to provide a contract for performing file-related
        /// operations,  such as reading, writing, or managing files and directories. Implementations of this 
        /// interface may vary depending on the underlying file system or storage mechanism.</remarks>
        protected IFileOperations m_oFileSystem = new InMemoryFileSystem();

        //===============================================================================================
        /// <summary>
        /// Creates a file, implemented in subclasses
        /// </summary>
        /// <returns>New file</returns>
        //===============================================================================================
        protected abstract IFile CreateFile();

        //===============================================================================================
        /// <summary>
        /// Verifies that a byte written to a file can be read back and matches the original value.
        /// </summary>
        /// <remarks>This test writes a single byte to a file, resets the file's position to the
        /// beginning,  and reads the byte back to ensure the written and read values are identical.</remarks>
        //===============================================================================================
        [Test]
        public void WriteByte_ReadByte_ShouldMatch()
        {
            using (var file = CreateFile())
            {
                byte value = 123;
                file.WriteByte(value);
                file.Position = 0;
                int read = file.ReadByte();
                Assert.AreEqual(value, read);
            }
        }

        //===============================================================================================
        /// <summary>
        /// Verifies that data written to a file can be read back correctly, ensuring the written and read buffers
        /// match.
        /// </summary>
        /// <remarks>This test writes a predefined byte array to a file, resets the file's
        /// position, and reads the data back into a new buffer. It then asserts that the number of bytes read
        /// matches the expected length and that the read buffer matches the original buffer.</remarks>
        //===============================================================================================
        [Test]
        public void WriteBuffer_ReadBuffer_ShouldMatch()
        {
            using (var file = CreateFile())
            {
                byte[] buffer = { 10, 20, 30, 40 };
                file.Write(buffer, 0, buffer.Length);
                file.Position = 0;
                byte[] readBuffer = new byte[4];
                int bytesRead = file.Read(readBuffer, 0, 4);
                Assert.AreEqual(4, bytesRead);
                Assert.AreEqual(buffer, readBuffer);
            }
        }

        //===============================================================================================
        /// <summary>
        /// Tests that the <see cref="IFile.Seek(long, SeekOrigin)"/> method updates the position of the stream
        /// correctly.
        /// </summary>
        /// <remarks>This test verifies that seeking to a specific position in the stream updates
        /// the <see cref="IFile.Position"/> property as expected.</remarks>
        //===============================================================================================
        [Test]
        public void Seek_ShouldUpdatePosition()
        {
            using (var file = CreateFile())
            {
                file.Write(new byte[] { 1, 2, 3 }, 0, 3);
                file.Seek(2, SeekOrigin.Begin);
                Assert.AreEqual(2, file.Position);
            }
        }

        //===============================================================================================
        /// <summary>
        /// Verifies that the <see cref="IFile.Flush"/> method does not throw an exception after writing data to
        /// the stream.
        /// </summary>
        /// <remarks>This test ensures that calling <see cref="IFile.Flush"/> on the created file
        /// stream does not result in any exceptions, confirming that the stream can be flushed successfully after
        /// data is written.</remarks>
        //===============================================================================================
        [Test]
        public void Flush_ShouldNotThrow()
        {
            using (var file = CreateFile())
            {
                file.Write(new byte[] { 1, 2, 3 }, 0, 3);
                Assert.DoesNotThrow(() => file.Flush());
            }
        }

        //===============================================================================================
        /// <summary>
        /// Verifies that closing the file does not throw an exception and prevents further access to the file.
        /// </summary>
        /// <remarks>This test ensures that the <see cref="System.IO.Stream.Close"/> method can be
        /// called without throwing an exception  and that subsequent attempts to write to the file after it is
        /// closed result in an <see cref="ObjectDisposedException"/>.</remarks>
        //===============================================================================================
        [Test]
        public void Close_ShouldNotThrowAndPreventFurtherAccess()
        {
            using (var file = CreateFile())
            {
                file.WriteByte(99);
                Assert.DoesNotThrow(() => file.Close());

                // Verify behavior after close
                Assert.Throws<ObjectDisposedException>(() => file.WriteByte(100));
            }
        }

        //===============================================================================================
        /// <summary>
        /// Verifies that the <see cref="Length"/> property of the file reflects the amount of data written to it.
        /// </summary>
        /// <remarks>This test writes a predefined byte array to the file and asserts that the
        /// <see cref="Length"/> property matches the number of bytes written. The test ensures that the file's
        /// length is updated correctly after a write operation.</remarks>
        //===============================================================================================
        [Test]
        public void Length_ShouldReflectWrittenData()
        {
            using (var file = CreateFile())
            {
                byte[] data = { 1, 2, 3, 4, 5 };
                file.Write(data, 0, data.Length);
                Assert.AreEqual(5, file.Length);
            }
        }
    }

    //===================================================================================================
    /// <summary>
    /// Provides a set of non-parallelizable unit tests for file operations using an in-memory file implementation.
    /// </summary>
    /// <remarks>This class is a test fixture that derives from <see cref="FileTestsBase"/> and
    /// overrides the file creation logic to use an in-memory file implementation. It is intended for testing
    /// file-related functionality without relying on physical file system access.</remarks>
    //===================================================================================================
    [TestFixture]
    [NonParallelizable]
    public class FileTestsInMemory : FileTestsBase
    {
        //===============================================================================================
        /// <summary>
        /// Current file number
        /// </summary>
        private int m_nCurrentFile;


        //===============================================================================================
        /// <summary>
        /// Sets up the test fixture
        /// </summary>
        //===============================================================================================
        [SetUp]
        public void Setup()
        {
            m_nCurrentFile = 0;
            m_oFileSystem = new InMemoryFileSystem();
        }

        //===============================================================================================
        /// <summary>
        /// Creates an in-memory file for testing
        /// </summary>
        /// <returns>the newly created file</returns>
        //===============================================================================================
        protected override IFile CreateFile()
        {
            return m_oFileSystem.Create("\\\\simulated\\" + (++m_nCurrentFile).ToString() + ".dat");
        }
    }



    //===================================================================================================
    /// <summary>
    /// Provides a set of non-parallelizable unit tests for file operations using an in-memory file implementation.
    /// </summary>
    /// <remarks>This class is a test fixture that derives from <see cref="FileTestsBase"/> and
    /// overrides the file creation logic to use an in-memory file implementation. It is intended for testing
    /// file-related functionality without relying on physical file system access.</remarks>
    //===================================================================================================
    [TestFixture]
    [NonParallelizable]
    public class FileTestsRealFileSystem : FileTestsBase
    {
        //===============================================================================================
        /// <summary>
        /// Sets up the test fixture
        /// </summary>
        //===============================================================================================
        [SetUp]
        public void Setup()
        {
            m_oFileSystem = new RealFileSystem();
        }

        //===============================================================================================
        /// <summary>
        /// Creates an in-memory file for testing
        /// </summary>
        /// <returns>the newly created file</returns>
        //===============================================================================================
        protected override IFile CreateFile()
        {
            string strTempFilePath = Path.GetTempFileName();
            return m_oFileSystem.Create(strTempFilePath + ".dat");
        }
    }

}
