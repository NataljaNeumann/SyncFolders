using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Converts to string, showing the configuration
        /// </summary>
        /// <returns>String representation</returns>
        //===================================================================================================
        public override string ToString()
        {
            return string.Join('-',
                    FirstToSecond ? "FirstToSecond" : "",
                    FirstReadOnly ? "FirstReadOnly" : "",
                    FirstToSecondDeleteInSecond ? "DeleteInSecond" : "",
                    TestFiles ? "Test" : "",
                    TestFilesSkipRecentlyTested ? "SkipRecentlyTested" : "",
                    RepairFiles ? "Repair" : "",
                    CreateInfo ? "CreateInfo" : "",
                    IgnoreTimeDifferencesBetweenDataAndSaveInfo ? "IgnoreTD" : "",
                    PreferPhysicalCopies ? "PreferPhysical" : ""
                );
        }
    }
}
