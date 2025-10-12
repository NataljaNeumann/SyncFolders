using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide environment and settings for
    /// API functions
    /// </summary>
    //*******************************************************************************************************
    public interface IFilePairStepsSettings: ICancelable
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
