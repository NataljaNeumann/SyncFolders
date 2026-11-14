/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024-2026 NataljaNeumann@gmx.de

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

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide environment and settings for
    /// API functions
    /// </summary>
    //*******************************************************************************************************
    public interface IFilePairStepsSettings: ICancelable, ICancelableWithRepeats
    {
        //===================================================================================================
        /// <summary>
        /// Indicates, if the process shall go from first file/folder to second 
        /// </summary>
        public bool FirstToSecond
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if first file/folder shall be considered read-only
        /// </summary>
        public bool FirstReadOnly
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Indicatess if first-to-second shall be dony in sync mode (in contrast to overwrite mode)
        /// </summary>
        public bool FirstToSecondSyncMode
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if first-to-second shall delete files of second folder that aren't present in first
        /// Plese note that the file that prevents deletion of file is crrently evaluated only by the 
        /// application, not by API
        /// </summary>
        public bool FirstToSecondDeleteInSecond
        {
            get;
        }


        //===================================================================================================
        /// <summary>
        /// Indicates, if readability of files shall be tested
        /// </summary>
        public bool TestFiles
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates if test of files shall randomly skip recently tested files
        /// </summary>
        public bool TestFilesSkipRecentlyTested
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if single block failures shall be repaired
        /// </summary>
        public bool RepairFiles
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if SavedInfo shall be created, if it is missing
        /// </summary>
        public bool CreateInfo
        {
            get;
        }


        //===================================================================================================
        /// <summary>
        /// Indicates if time difference between files and saved info shall be ignored
        /// </summary>
        public bool IgnoreTimeDifferencesBetweenDataAndSaveInfo
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if physical copies shall be preferred over calculated restored info
        /// </summary>
        public bool PreferPhysicalCopies
        {
            get;
        }


    }
}
