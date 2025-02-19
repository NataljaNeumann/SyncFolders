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
using System.IO;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Provides access to read file system
    /// </summary>
    //*******************************************************************************************************
    public class RealFileSystem : IFileOperations
    {

        //===================================================================================================
        /// <summary>
        /// Opens a file for write
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        public IFile OpenRead(
            string strPathstrPath
            )
        {
            var oStream = File.OpenRead(strPathstrPath);
            return new RealFile(oStream);
        }



        //===================================================================================================
        /// <summary>
        /// Opens a file for read
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        public IFile OpenWrite(
            string strPath
            )
        {
            var oStream = File.OpenWrite(strPath);
            return new RealFile(oStream);
        }


        //===================================================================================================
        /// <summary>
        /// Creates a file and opens it for write
        /// </summary>
        /// <param name="strPath">Path of the file to open</param>
        /// <returns>File access object</returns>
        //===================================================================================================
        public IFile Create(
            string strPath
            )
        {
            var oStream = File.Create(strPath);
            return new RealFile(oStream);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file from source to destination
        /// </summary>
        /// <param name="strSourcePath">Path to copy from</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        public void CopyFile(
            string strSourcePath, 
            string strDestinationPath
            )
        {
            File.Copy(strSourcePath, strDestinationPath);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file from source to destination
        /// </summary>
        /// <param name="strSourcePath">Path to copy from</param>
        /// <param name="strDestinationPath">Path to copy to</param>
        //===================================================================================================
        public void CopyFileFromReal(
            string strSourcePath,
            string strDestinationPath
            )
        {
            File.Copy(strSourcePath, strDestinationPath);
        }


        //===================================================================================================
        /// <summary>
        /// Reads all contents of the file
        /// </summary>
        /// <param name="strPath">Path to read from</param>
        /// <returns>The content of the file</returns>
        //===================================================================================================
        public string ReadFromFile(
            string strPath
            )
        {
            return File.ReadAllText(strPath);
        }

        //===================================================================================================
        /// <summary>
        /// Writes tontent to file
        /// </summary>
        /// <param name="strPath">The path of the file</param>
        /// <param name="strContent">Content to write</param>
        //===================================================================================================
        public void WriteAllText(
            string strPath, 
            string strContent
            )
        {
            File.WriteAllText(strPath, strContent);
        }

        //===================================================================================================
        /// <summary>
        /// Searches file in a directory
        /// </summary>
        /// <param name="strSearchPattern">Directory</param>
        /// <returns>A list of files</returns>
        //===================================================================================================
        public List<string> SearchFiles(
            string strSearchPattern
            )
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), strSearchPattern);
            return new List<string>(files);
        }


        //===================================================================================================
        /// <summary>
        /// Renames a file
        /// </summary>
        /// <param name="strOldPath">Old path of an existing file</param>
        /// <param name="strNewPath">New path for the file</param>
        //===================================================================================================
        public void Move(
            string strOldPath, 
            string strNewPath
            )
        {
            File.Move(strOldPath, strNewPath);
        }

        //===================================================================================================
        /// <summary>
        /// Gets information about a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        public IFileInfo GetFileInfo(
            string strPath
            )
        {
            return new RealFileInfo(new FileInfo(strPath));
        }


        //===================================================================================================
        /// <summary>
        /// Gets information about a directory
        /// </summary>
        /// <param name="strPath">Path of the directory</param>
        /// <returns>Information object</returns>
        //===================================================================================================
        public IDirectoryInfo GetDirectoryInfo(
            string strPath
            )
        {
            return new RealDirectoryInfo(new DirectoryInfo(strPath));
        }

        //===================================================================================================
        /// <summary>
        /// Creates buffered stream object, if in real file, or does nothing for in-memory file
        /// </summary>
        /// <param name="iFile">IFile object</param>
        /// <returns>IFile object</returns>
        //===================================================================================================
        public IFile CreateBufferedStream(
            IFile iFile, 
            int nBufferLength
            )
        {
            Stream s = ((RealFile)iFile).m_oStream;
            ((RealFile)iFile).m_oStream = null;
            return new RealFile(new BufferedStream(s, nBufferLength));
        }


        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        //===================================================================================================
        public IFileInfo CopyTo(
            IFileInfo fi,
            string strDestFileName
            )
        {
            return fi.CopyTo(strDestFileName);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        /// <param name="bOverwrite">Indicates, if the file shall be overwritten, if exists</param>
        //===================================================================================================
        public IFileInfo CopyTo(
            IFileInfo fi,
            string strDestFileName,
            bool bOverwrite
            )
        {
            return fi.CopyTo(strDestFileName, bOverwrite);
        }



        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public IFile Open(
            string strPath,
            FileMode eMode
            )
        {
            return new RealFile(File.Open(strPath, eMode));
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <param name="eAccess">Access</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public IFile Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess
            )
        {
            return  new RealFile(File.Open(strPath, eMode, eAccess));
        }

        //===================================================================================================
        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eMode">Open mode</param>
        /// <param name="eAccess">Access</param>
        /// <param name="eShare">File share</param>
        /// <returns>File stream</returns>
        //===================================================================================================
        public IFile Open(
            string strPath,
            FileMode eMode,
            FileAccess eAccess,
            FileShare eShare
            )
        {
            return new RealFile(File.Open(strPath, eMode, eAccess, eShare));
        }


        //===================================================================================================
        /// <summary>
        /// Physically deletes the file.
        /// </summary>
        /// <param name="strFilePath">Path of the file</param>
        //===================================================================================================
        public void Delete(
            string strFilePath
            )
        {
            File.Delete(strFilePath);
        }

        //===================================================================================================
        /// <summary>
        /// Physically deletes the file.
        /// </summary>
        /// <param name="fi">Path of the file</param>
        //===================================================================================================
        public void Delete(
            IFileInfo fi
            )
        {
            fi.Delete();
        }


        //===================================================================================================
        /// <summary>
        /// Sets last write time
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="dtmLastWriteTimeUtc">Last write time</param>
        //===================================================================================================
        public void SetLastWriteTimeUtc(
            string strPath,
            DateTime dtmLastWriteTimeUtc
            )
        {
            File.SetLastWriteTimeUtc(strPath, dtmLastWriteTimeUtc);
        }


        //===================================================================================================
        /// <summary>
        /// Gets last write time
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        //===================================================================================================
        public DateTime GetLastWriteTimeUtc(
            string strPath
            )
        {
            return File.GetLastWriteTimeUtc(strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Gets information, if file exits in file system
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <returns>true iff file exists</returns>
        //===================================================================================================
        public bool Exists(
            string strPath
            )
        {
            return File.Exists(strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Gets attribues of the file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        //===================================================================================================
        public FileAttributes GetAttributes(
            string strPath
            )
        {
            return File.GetAttributes(strPath);
        }


        //===================================================================================================
        /// <summary>
        /// Gets attribues of the file
        /// </summary>
        /// <param name="strPath">Path of the file</param>
        /// <param name="eNewAttributes">New attributes to set</param>
        //===================================================================================================
        public void SetAttributes(
            string strPath,
            FileAttributes eNewAttributes
            )
        {
            File.SetAttributes(strPath, eNewAttributes);
        }

    }

}
