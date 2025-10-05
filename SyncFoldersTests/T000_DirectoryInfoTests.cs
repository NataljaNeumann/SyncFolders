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
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncFoldersApi;

#pragma warning disable NUnit2005

namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// Base class for directory info tests
    /// </summary>
    //*******************************************************************************************************
    public abstract class DirectoryInfoTestsBase
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
        /// Temporary ddirectory for current test
        /// </summary>
        private string m_strTempPath;

        //===================================================================================================
        /// <summary>
        /// Sets up the test
        /// </summary>
        //===================================================================================================
        [SetUp]
        public virtual void Setup()
        {
            m_strTempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        //===================================================================================================
        /// <summary>
        /// Deletes the used directory after test
        /// </summary>
        //===================================================================================================
        [TearDown]
        public void Cleanup()
        {
            // Delete the directory (kind of silly to use tested class there :-)
            IDirectoryInfo iDir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            if (iDir.Exists)
            {
                iDir.Delete(true);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if the directory is created
        /// </summary>
        //===================================================================================================
        [Test]
        public void Create_ShouldMakeDirectory()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            dir.Create();
            Assert.IsTrue(m_oFileSystem.GetDirectoryInfo(m_strTempPath).Exists);
            Assert.IsTrue(dir.Exists);
        }

        //===================================================================================================
        /// <summary>
        /// Tests if delete removes the directory from file system
        /// </summary>
        //===================================================================================================
        [Test]
        public void Delete_ShouldRemoveDirectory()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            dir.Create();
            Assert.IsTrue(dir.Exists);
            dir.Delete(true);

            Assert.IsFalse(m_oFileSystem.GetDirectoryInfo(m_strTempPath).Exists);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if a file that is created in the directory is returned by directory info
        /// </summary>
        //===================================================================================================
        [Test]
        public void GetFiles_ShouldReturnCreatedFiles()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            dir.Create();
            m_oFileSystem.WriteAllText(Path.Combine(m_strTempPath, "test.txt"), "hello");
            var files = dir.GetFiles();
            Assert.AreEqual(1, files.Length);
            Assert.AreEqual("test.txt", files[0].Name);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if a subdirectory that is created in the directory is returned
        /// </summary>
        //===================================================================================================
        [Test]
        public void GetDirectories_ShouldReturnCreatedSubdirectories()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            dir.Create();
            var subDirPath = Path.Combine(m_strTempPath, "sub");
            var subDir = m_oFileSystem.GetDirectoryInfo(subDirPath);
            subDir.Create();

            var subDirs = dir.GetDirectories();
            Assert.AreEqual(1, subDirs.Length);
            Assert.AreEqual("sub", subDirs[0].Name);
        }

        //===================================================================================================
        /// <summary>
        /// Tests if FullName property is correct
        /// </summary>
        //===================================================================================================
        [Test]
        public void FullName_ShouldMatchPath()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            Assert.AreEqual(m_strTempPath, dir.FullName);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if name of the directory is correct 
        /// </summary>
        //===================================================================================================
        [Test]
        public void Name_ShouldMatchLastSegment()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            Assert.AreEqual(Path.GetFileName(m_strTempPath), dir.Name);
        }

        //===================================================================================================
        /// <summary>
        /// Tests if parent directory is returned correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void Parent_ShouldBeCorrect()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            var parent = dir.Parent;
            Assert.AreEqual(Path.GetDirectoryName(m_strTempPath), parent?.FullName);
        }

        //===================================================================================================
        /// <summary>
        /// Tests if directory attributes are settable
        /// </summary>
        //===================================================================================================
        [Test]
        public void Attributes_ShouldBeSettable()
        {
            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            dir.Create();
            dir.Attributes = FileAttributes.Hidden | FileAttributes.Directory;
            Assert.AreEqual(FileAttributes.Hidden | FileAttributes.Directory, dir.Attributes);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if creation of a directory actually creates one
        /// </summary>
        //===================================================================================================
        [Test]
        public void Exists_ShouldReflectActualState()
        {
            // ensure that the dir is not there
            IDirectoryInfo iDir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            if (iDir.Exists)
            {
                iDir.Delete(true);
            }

            var dir = m_oFileSystem.GetDirectoryInfo(m_strTempPath);
            Assert.IsFalse(dir.Exists);
            dir.Create();
            Assert.IsTrue(dir.Exists);
        }

    }

    //*******************************************************************************************************
    /// <summary>
    /// Tests in-memory directory info
    /// </summary>
    //*******************************************************************************************************
    [TestFixture]
    [NonParallelizable]
    public class T000_DirectoryInfoTestInMemory : DirectoryInfoTestsBase
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
    public class T000_DirectoryInfoTestInReal : DirectoryInfoTestsBase
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
