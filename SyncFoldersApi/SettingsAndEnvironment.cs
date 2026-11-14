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
    /// Pprovides environment and settings for API functions
    /// </summary>
    //*******************************************************************************************************
    public class SettingsAndEnvironment: IFilePairStepsSettings
    {
        /// <summary>
        /// Constructs a nnw SettingsAndEnvironment object
        /// </summary>
        public SettingsAndEnvironment() 
        {
        }

        //===================================================================================================
        /// <summary>
        /// Constructs a new SettingsAndEnvironment object
        /// </summary>
        /// <param name="bFirstToSecond">
        /// Indicates, if the process shall go from first file/folder to second</param>
        /// <param name="bFirstReadOnly">
        /// Indicates, if first file/folder shall be considered read-only</param>
        /// <param name="bFirstToSecondSyncMode">
        /// Indicatess if first-to-second shall be dony in sync mode (in contrast to overwrite mode)</param>
        /// <param name="bFirstToSecondDeleteInSecond">
        /// Indicates, if first-to-second shall delete files of second folder that aren't present in first
        /// </param>
        /// <param name="bTestFiles">Indicates, if readability of files shall be tested</param>
        /// <param name="bTestFilesSkipRecentlyTested">
        /// Indicates if test of files shall randomly skip recently tested files</param>
        /// <param name="bRepairFiles">Indicates, if single block failures shall be repaired</param>
        /// <param name="bCreateInfo">Indicates, if SavedInfo shall be created, if it is missing</param>
        /// <param name="bIgnoreTimeDifferencesBetweenDataAndSaveInfo">
        /// Indicates if time difference between files and saved info shall be ignored</param>
        /// <param name="bPreferPhysicalCopies">
        /// Indicates, if physical copies shall be preferred over calculated restored info</param>
        //===================================================================================================
        public SettingsAndEnvironment(
            bool bFirstToSecond,
            bool bFirstReadOnly,
            bool bFirstToSecondSyncMode,
            bool bFirstToSecondDeleteInSecond,
            bool bTestFiles,
            bool bTestFilesSkipRecentlyTested,
            bool bRepairFiles,
            bool bCreateInfo,
            bool bIgnoreTimeDifferencesBetweenDataAndSaveInfo,
            bool bPreferPhysicalCopies
            )
        {
            FirstToSecond = bFirstToSecond;
            FirstReadOnly = bFirstReadOnly;
            FirstToSecondSyncMode = bFirstToSecondSyncMode;
            FirstToSecondDeleteInSecond = bFirstToSecondDeleteInSecond;
            TestFiles = bTestFiles;
            TestFilesSkipRecentlyTested = bTestFilesSkipRecentlyTested;
            RepairFiles = bRepairFiles;
            CreateInfo = bCreateInfo;
            IgnoreTimeDifferencesBetweenDataAndSaveInfo =
                bIgnoreTimeDifferencesBetweenDataAndSaveInfo;
            PreferPhysicalCopies = bPreferPhysicalCopies;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if the process shall go from first file/folder to second 
        /// </summary>
        public bool FirstToSecond
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if first file/folder shall be considered read-only
        /// </summary>
        public bool FirstReadOnly
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicatess if first-to-second shall be dony in sync mode (in contrast to overwrite mode)
        /// </summary>
        public bool FirstToSecondSyncMode
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if first-to-second shall delete files of second folder that aren't present in first
        /// Plese note that the file that prevents deletion of file is crrently evaluated only by the 
        /// application, not by API
        /// </summary>
        public bool FirstToSecondDeleteInSecond
        {
            get; set;
        }


        //===================================================================================================
        /// <summary>
        /// Indicates, if readability of files shall be tested
        /// </summary>
        public bool TestFiles
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates if test of files shall randomly skip recently tested files
        /// </summary>
        public bool TestFilesSkipRecentlyTested
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if single block failures shall be repaired
        /// </summary>
        public bool RepairFiles
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if SavedInfo shall be created, if it is missing
        /// </summary>
        public bool CreateInfo
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if canclel has been clicked
        /// </summary>
        public bool CancelClicked
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates if time difference between files and saved info shall be ignored
        /// </summary>
        public bool IgnoreTimeDifferencesBetweenDataAndSaveInfo
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Indicates, if physical copies shall be preferred over calculated restored info
        /// </summary>
        public bool PreferPhysicalCopies
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// The number of retries, in case there are errors. This configuration parameter isn't used by
        /// all methods and can be discarded by the actual code
        /// </summary>
        public int Retries
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// The number of milliseconds to wait before a retry. This configuration parameter isn't used by
        /// all methods and can be discarded by the actual code
        /// </summary>
        public int RetryDelay
        {
            get; set;
        }

        //===================================================================================================
        /// <summary>
        /// Converts to string, showing the configuration
        /// </summary>
        /// <returns>String representation</returns>
        //===================================================================================================
        public override string ToString()
        {
            return string.Join('-',
                    FirstToSecond ? "FirstToSecond" : "",
                    FirstToSecond ? (FirstToSecondSyncMode ? "SyncMode" : "OverwriteMode") : "",
                    FirstReadOnly ? "FirstReadOnly" : "",
                    FirstToSecondDeleteInSecond ? "DeleteInSecond" : "",
                    TestFiles ? "Test" : "",
                    TestFilesSkipRecentlyTested ? "SkipRecentlyTested" : "",
                    RepairFiles ? "Repair" : "",
                    CreateInfo ? "CreateInfo" : "",
                    IgnoreTimeDifferencesBetweenDataAndSaveInfo ? "IgnoreTD" : "",
                    PreferPhysicalCopies ? "PreferPhysical" : "",
                    Retries>0?Retries.ToString()+"Retries"+(RetryDelay%1000==0?RetryDelay.ToString() + "s": RetryDelay.ToString()+"ms"):""
                );
        }
    }
}
