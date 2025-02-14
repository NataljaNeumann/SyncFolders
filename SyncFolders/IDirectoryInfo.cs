﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide possibility to get informationn about directories
    /// </summary>
    //*******************************************************************************************************
    public interface IDirectoryInfo
    {
        //===================================================================================================
        /// <summary>
        /// Gets files inside the directory
        /// </summary>
        /// <returns>A list of files</returns>
        //===================================================================================================
        IFileInfo[] GetFiles();

        //===================================================================================================
        /// <summary>
        /// Gets subdirectories
        /// </summary>
        /// <returns>List of subdirectories</returns>
        //===================================================================================================
        IDirectoryInfo[] GetDirectories();

        //===================================================================================================
        /// <summary>
        /// Gets full name of the directory
        /// </summary>
        //===================================================================================================
        string FullName { get; }

        //===================================================================================================
        /// <summary>
        /// Gets name of the directory
        /// </summary>
        //===================================================================================================
        string Name{ get; }

        //===================================================================================================
        /// <summary>
        /// Gets parent directory
        /// </summary>
        //===================================================================================================
        IDirectoryInfo Parent{ get; }


        //===================================================================================================
        /// <summary>
        /// Does the directory exist?
        /// </summary>
        //===================================================================================================
        bool Exists { get; }


        //===================================================================================================
        /// <summary>
        /// Creates the directory
        /// </summary>
        //===================================================================================================
        void Create();

        //===================================================================================================
        /// <summary>
        /// Deletes the directory
        /// </summary>
        /// <param name="bIncludingContents">Indicates that the contents need to be deleted, as well</param>
        //===================================================================================================
        void Delete(
            bool bIncludingContents
            );

        //===================================================================================================
        /// <summary>
        /// Gets or sets directory attributes
        /// </summary>
        //===================================================================================================
        FileAttributes Attributes
        {
            get;
            set;
        }

    }
}
