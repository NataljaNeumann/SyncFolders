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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable NUnit2005

namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// Base class for tests of file operations classes
    /// </summary>
    //*******************************************************************************************************
    public abstract class FileOperationsTestsBase
    {

        //===================================================================================================
        /// <summary>
        /// Represents an abstraction for file system operations.
        /// </summary>
        /// <remarks>This interface is intended to provide a contract for performing file-related
        /// operations,  such as reading, writing, or managing files and directories. Implementations of this 
        /// interface may vary depending on the underlying file system or storage mechanism.</remarks>
        protected IFileOperations m_oFileSystem;

        //===================================================================================================
        /// <summary>
        /// Sets up the test environment
        /// </summary>
        //===================================================================================================
        [SetUp]
        public abstract void Setup();

        //===================================================================================================
        /// <summary>
        /// Creates a temp file
        /// </summary>
        /// <param name="strContent">Content of the file</param>
        /// <returns>Path of the file</returns>
        //===================================================================================================
        private string CreateTempFile(string strContent = "Hello World")
        {
            IDirectoryInfo iDir = m_oFileSystem.GetDirectoryInfo(Path.GetTempPath());
            if (!iDir.Exists)
                iDir.Create();

            string strPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            m_oFileSystem.WriteAllText(strPath, strContent);
            return strPath;
        }

        //===================================================================================================
        /// <summary>
        /// Creates a temporary directory
        /// </summary>
        /// <returns>Path to created directory</returns>
        //===================================================================================================
        private string CreateTempDirectory()
        {
            string strPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            m_oFileSystem.GetDirectoryInfo(strPath).Create();
            return strPath;
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if file creation works
        /// </summary>
        //===================================================================================================
        [Test]
        public void Create_ShouldCreateFile()
        {
            string strPath = CreateTempFile("");
            File.Delete(strPath);
            using (var iFile = m_oFileSystem.Create(strPath))
                ;
            Assert.IsTrue(m_oFileSystem.Exists(strPath));
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if writing is possible for files that are opened for writing
        /// </summary>
        //===================================================================================================
        [Test]
        public void OpenWrite_ShouldAllowWriting_Read_Reading()
        {
            string strPath = CreateTempFile("");
            using (var iFile = m_oFileSystem.OpenWrite(strPath))
            {
                iFile.WriteByte(65);
                iFile.Position = 0;
                Assert.AreEqual(0, iFile.Position);
            }

            using (var iFile = m_oFileSystem.OpenRead(strPath))
            {
                Assert.AreEqual(65, iFile.ReadByte());
                iFile.Position = 0;
                Assert.AreEqual(0, iFile.Position);
            }
        }


        //===================================================================================================
        /// <summary>
        /// Tests, if writing is possible for files that are opened for writing
        /// </summary>
        //===================================================================================================
        [Test]
        public void OpenReaddWrite_ShouldAllowReadingWriting()
        {
            string strPath = CreateTempFile("");
            using (var iFile = m_oFileSystem.Open(strPath, FileMode.Open, FileAccess.ReadWrite))
            {
                iFile.WriteByte(65);
                iFile.Position = 0;
                Assert.AreEqual(0, iFile.Position);
                Assert.AreEqual(65, iFile.ReadByte());
                iFile.Position = 0;
                Assert.AreEqual(0, iFile.Position);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if reading of the content is possible
        /// </summary>
        //===================================================================================================
        [Test]
        public void OpenRead_ShouldReadContent()
        {
            string strPath = CreateTempFile("TestContent");
            using (var iFle = m_oFileSystem.OpenRead(strPath))
            {
                byte[] aBuffer = new byte[11];
                int nRead = iFle.Read(aBuffer, 0, aBuffer.Length);
                Assert.AreEqual("TestContent", System.Text.Encoding.UTF8.GetString(aBuffer, 0, nRead));
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests copying the file
        /// </summary>
        //===================================================================================================
        [Test]
        public void CopyFile_ShouldDuplicateFile()
        {
            string strSourcePath = CreateTempFile("CopyMe");
            string strDestPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            m_oFileSystem.CopyFile(strSourcePath, strDestPath);
            Assert.IsTrue(m_oFileSystem.Exists(strDestPath));
            Assert.AreEqual(m_oFileSystem.ReadAllText(strSourcePath), m_oFileSystem.ReadAllText(strDestPath));
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if WriteAllText works in combination with ReadAllText
        /// </summary>
        //===================================================================================================
        [Test]
        public void WriteAllText_ShouldWriteContent()
        {
            string strPath = CreateTempFile("");
            m_oFileSystem.WriteAllText(strPath, "NewContent");
            Assert.AreEqual("NewContent", m_oFileSystem.ReadAllText(strPath));
        }

        //===================================================================================================
        /// <summary>
        /// Another test, if WriteAllText works with ReadAllText
        /// </summary>
        //===================================================================================================
        [Test]
        public void ReadFromFile_ShouldReturnContent()
        {
            string strPath = CreateTempFile("ReadMe");
            string strContent = m_oFileSystem.ReadAllText(strPath);
            Assert.AreEqual("ReadMe", strContent);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if searching for files with pattern works
        /// </summary>
        //===================================================================================================
        [Test]
        public void SearchFiles_ShouldFindMatchingFiles()
        {
            string strDirPath = CreateTempDirectory();
            string strFilePath1 = Path.Combine(strDirPath, "match1.txt");
            string strFilePath2 = Path.Combine(strDirPath, "match2.txt");
            string strFilePath3 = Path.Combine(strDirPath, "nomatch1.dat");
            m_oFileSystem.WriteAllText(strFilePath1, "A");
            m_oFileSystem.WriteAllText(strFilePath2, "B");
            m_oFileSystem.WriteAllText(strFilePath3, "C");
            var aResults = m_oFileSystem.SearchFiles(Path.Combine(strDirPath, "*.txt"));
            Assert.AreEqual(2, aResults.Count);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if renamming works
        /// </summary>
        //===================================================================================================
        [Test]
        public void Move_ShouldRelocateFile()
        {
            string strSourcePath = CreateTempFile("MoveMe");
            string strDestPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            m_oFileSystem.Move(strSourcePath, strDestPath);
            Assert.IsFalse(m_oFileSystem.Exists(strSourcePath));
            Assert.IsTrue(m_oFileSystem.Exists(strDestPath));
            Assert.AreEqual("MoveMe", m_oFileSystem.ReadAllText(strDestPath));
        }

        //===================================================================================================
        /// <summary>
        /// Test deleting a file
        /// </summary>
        //===================================================================================================
        [Test]
        public void Delete_ByPath_ShouldRemoveFile()
        {
            string strFilePath = CreateTempFile("DeleteMe");
            m_oFileSystem.Delete(strFilePath);
            Assert.IsFalse(m_oFileSystem.Exists(strFilePath));
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if Exists returns true for existing file
        /// </summary>
        //===================================================================================================
        [Test]
        public void Exists_ShouldReturnTrueForExistingFile()
        {
            string strFilePath = CreateTempFile();
            Assert.IsTrue(m_oFileSystem.Exists(strFilePath));
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if attributes work for normal files
        /// </summary>
        //===================================================================================================
        [Test]
        public void GetAttributes_ShouldReturnCorrectFlags()
        {
            string strFilePath = CreateTempFile();
            var eAttr = m_oFileSystem.GetAttributes(strFilePath);
            Assert.IsTrue(eAttr.HasFlag(FileAttributes.Archive));
        }

        /*
        //===================================================================================================
        /// <summary>
        /// Tests, if setting attributes works
        /// </summary>
        //===================================================================================================
        [Test]
        public void SetAttributes_ShouldUpdateFlags()
        {
            string strFilePath = CreateTempFile();
            m_oFileSystem.SetAttributes(strFilePath, FileAttributes.Hidden);
            var eAttr = m_oFileSystem.GetAttributes(strFilePath);
            Assert.IsTrue(eAttr.HasFlag(FileAttributes.Hidden));
        }
        */

        //===================================================================================================
        /// <summary>
        /// Tests, if changing last write time works
        /// </summary>
        //===================================================================================================
        [Test]
        public void SetAndGetLastWriteTimeUtc_ShouldMatch()
        {
            string strFilePath = CreateTempFile();
            var dtmNewTime = DateTime.UtcNow.AddHours(-2);
            m_oFileSystem.SetLastWriteTimeUtc(strFilePath, dtmNewTime);
            var dtmActual = m_oFileSystem.GetLastWriteTimeUtc(strFilePath);
            Assert.AreEqual(dtmNewTime, dtmActual);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if file info returns info about the same file
        /// </summary>
        //===================================================================================================
        [Test]
        public void GetFileInfo_ShouldReturnCorrectName()
        {
            string strFilePath = CreateTempFile();
            var oFileInfo = m_oFileSystem.GetFileInfo(strFilePath);
            Assert.AreEqual(Path.GetFileName(strFilePath), oFileInfo.Name);
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if GetDirectoryInfo returns same directory
        /// </summary>
        //===================================================================================================
        [Test]
        public void GetDirectoryInfo_ShouldReturnCorrectPath()
        {
            string strDirPath = CreateTempDirectory();
            var iDirInfo = m_oFileSystem.GetDirectoryInfo(strDirPath);
            Assert.AreEqual(strDirPath, iDirInfo.FullName);
        }
    }

    //*******************************************************************************************************
    /// <summary>
    /// Tests in-memory directory info
    /// </summary>
    //*******************************************************************************************************
    [TestFixture]
    [NonParallelizable]
    public class FileOperationsTestInMemory : FileOperationsTestsBase
    {
        //===================================================================================================
        /// <summary>
        /// Sets up the fixture
        /// </summary>
        //===================================================================================================
        public override void Setup()
        {
            m_oFileSystem = new InMemoryFileSystem();
        }

    }



    //*******************************************************************************************************
    /// <summary>
    /// Tests real directory info
    /// </summary>
    //*******************************************************************************************************
    [TestFixture]
    [NonParallelizable]
    public class FileOperationsTestInReal : FileOperationsTestsBase
    {
        //===================================================================================================
        /// <summary>
        /// Sets up the fixture
        /// </summary>
        //===================================================================================================
        public override void Setup()
        {
            m_oFileSystem = new RealFileSystem();
        }

    }
}
