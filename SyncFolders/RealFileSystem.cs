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
        public IFile OpenFileForRead(string path)
        {
            var stream = File.OpenRead(path);
            return new RealFile(stream);
        }

        public IFile OpenFileForWrite(string path)
        {
            var stream = File.OpenWrite(path);
            return new RealFile(stream);
        }


        public void CopyFile(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }

        public string ReadFromFile(string path)
        {
            return File.ReadAllText(path);
        }

        public void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        public List<string> SearchFiles(string searchPattern)
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), searchPattern);
            return new List<string>(files);
        }

        public void Move(string oldPath, string newPath)
        {
            File.Move(oldPath, newPath);
        }

        public IFileInfo GetFileInfo(string path)
        {
            return new RealFileInfo(new FileInfo(path));
        }

        public IDirectoryInfo GetDirectoryInfo(string path)
        {
            return new RealDirectoryInfo(new DirectoryInfo(path));
        }
    }

}
