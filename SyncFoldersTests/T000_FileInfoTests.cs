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
using SyncFoldersApi.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable NUnit2005


namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// This class provides base for file info tests
    /// </summary>
    //*******************************************************************************************************
    public abstract class FileInfoTestsBase
    {
        //===================================================================================================
        /// <summary>
        /// Represents an abstraction for file system operations.
        /// </summary>
        /// <remarks>This interface is intended to provide a contract for performing file-related
        /// operations,  such as reading, writing, or managing files and directories. Implementations of this 
        /// interface may vary depending on the underlying file system or storage mechanism.</remarks>
        protected IFileOperations m_oFileSystem = new InMemoryFileSystem();

        //===================================================================================================
        /// <summary>
        /// Path of temp directory
        /// </summary>
        private string m_strTempDir;

        //===================================================================================================
        /// <summary>
        /// Patth of temp file
        /// </summary>
        private string m_strFilePath;


        //===================================================================================================
        /// <summary>
        /// Sets up the environment for testing
        /// </summary>
        //===================================================================================================
        [SetUp]
        public virtual void Setup()
        {
            m_strTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            m_oFileSystem.GetDirectoryInfo(m_strTempDir).Create();

            m_strFilePath = Path.Combine(m_strTempDir, "test.txt");
            m_oFileSystem.WriteAllText(m_strFilePath, "Hello World");

            // setup resources
            Properties.Resources = new SyncFoldersApiResources();
        }

        //===================================================================================================
        /// <summary>
        /// Deletes used temporary directories and files
        /// </summary>
        //===================================================================================================
        [TearDown]
        public void Cleanup()
        {
            IDirectoryInfo iDir = m_oFileSystem.GetDirectoryInfo(m_strTempDir);
            if (iDir.Exists)
            {
                iDir.Delete(true);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests if exists works correctly for an existing file
        /// </summary>
        //===================================================================================================
        [Test]
        public void Exists_ShouldBeTrueForExistingFile()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            Assert.IsTrue(file.Exists);
        }

        //===================================================================================================
        /// <summary>
        /// Tests if length works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void Length_ShouldMatchFileSize()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            Assert.AreEqual(11, file.Length); // "Hello World" = 11 bytes
        }


        //===================================================================================================
        /// <summary>
        /// Tests if FullName property works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void FullName_ShouldMatchPath()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            Assert.AreEqual(m_strFilePath, file.FullName);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if Name property works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void Name_ShouldMatchFileName()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            Assert.AreEqual("test.txt", file.Name);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if file extension works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void Extension_ShouldMatchFileExtension()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            Assert.AreEqual(".txt", file.Extension);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if directory name is returned correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void DirectoryName_ShouldMatchParentPath()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            Assert.AreEqual(m_strTempDir, file.DirectoryName);
        }

        //===================================================================================================
        /// <summary>
        /// Tests the Directory property
        /// </summary>
        //===================================================================================================
        [Test]
        public void Directory_ShouldReturnParentDirectory()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            var dir = file.Directory;
            Assert.AreEqual(m_strTempDir, dir.FullName);
        }

        //===================================================================================================
        /// <summary>
        /// Tests file attributes
        /// </summary>
        //===================================================================================================
        [Test]
        public void Attributes_ShouldBeSettable()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            file.Attributes = FileAttributes.Hidden;
            Assert.AreEqual(FileAttributes.Hidden, file.Attributes);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if last write time works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void LastWriteTimeUtc_ShouldBeSettable()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            var newTime = DateTime.UtcNow.AddHours(-1);
            file.LastWriteTimeUtc = newTime;
            Assert.AreEqual(newTime, file.LastWriteTimeUtc);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if Delete works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void Delete_ShouldRemoveFile()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            file.Delete();
            Assert.IsFalse(m_oFileSystem.Exists(m_strFilePath));
            Assert.IsFalse(file.Exists);
            // create the file again, since we don't know order of tests
            m_oFileSystem.WriteAllText(m_strFilePath, "Hello World");
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if moving files works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void MoveTo_ShouldRelocateFile()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            var newPath = Path.Combine(m_strTempDir, "moved.txt");
            file.MoveTo(newPath);
            Assert.IsFalse(m_oFileSystem.Exists(m_strFilePath));
            Assert.IsTrue(m_oFileSystem.Exists(newPath));
            Assert.AreEqual("moved.txt", file.Name);
            // move back, since we don't know the order of tests
            file.MoveTo(m_strFilePath);
            Assert.IsFalse(m_oFileSystem.Exists(newPath));
            Assert.IsTrue(m_oFileSystem.Exists(m_strFilePath));
            Assert.AreEqual("test.txt", file.Name);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if copy works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void CopyTo_ShouldCreateCopy()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            var copyPath = Path.Combine(m_strTempDir, "copy.txt");
            var copy = file.CopyTo(copyPath);
            Assert.IsTrue(m_oFileSystem.Exists(copyPath));
            Assert.AreEqual("copy.txt", copy.Name);
            Assert.AreEqual("Hello World", m_oFileSystem.ReadAllText(copyPath));
        }

        //===================================================================================================
        /// <summary>
        /// Tests if overwriting works
        /// </summary>
        //===================================================================================================
        [Test]
        public void CopyTo_WithOverwrite_ShouldReplaceFile()
        {
            var file = m_oFileSystem.GetFileInfo(m_strFilePath);
            var copyPath = Path.Combine(m_strTempDir, "copy2.txt");
            m_oFileSystem.WriteAllText(copyPath, "Old long content"); // Length != 11
            var copy = file.CopyTo(copyPath, true);
            Assert.AreEqual("copy2.txt", copy.Name);
            Assert.AreEqual(11, copy.Length); // "Hello World" Length == 11
        }
    }



    //*******************************************************************************************************
    /// <summary>
    /// Tests in-memory directory info
    /// </summary>
    //*******************************************************************************************************
    [TestFixture]
    [NonParallelizable]
    public class T000_FileInfoTestInMemory : FileInfoTestsBase
    {
        //===================================================================================================
        /// <summary>
        /// Sets up the fixture
        /// </summary>
        //===================================================================================================
        public override void Setup()
        {
            m_oFileSystem = new InMemoryFileSystem();

            base.Setup();            
        }

    }



    //*******************************************************************************************************
    /// <summary>
    /// Tests real directory info
    /// </summary>
    //*******************************************************************************************************
    [TestFixture]
    [NonParallelizable]
    public class T000_FileInfoTestInReal : FileInfoTestsBase
    {
        //===================================================================================================
        /// <summary>
        /// Sets up the fixture
        /// </summary>
        //===================================================================================================
        public override void Setup()
        {
            m_oFileSystem = new RealFileSystem();

            base.Setup();

            
        }

    }

}
