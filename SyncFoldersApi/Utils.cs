using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Utils for the API
    /// </summary>
    //*******************************************************************************************************
    public class Utils
    {
        //===================================================================================================
        /// <summary>
        /// This is used for randomization of recently tested files
        /// </summary>
        private static Random s_oRandomForRecentlyChecked = new Random(unchecked(DateTime.Now.Millisecond + 1000
            * (DateTime.Now.Second + 60 * (DateTime.Now.Minute + 60
            * (DateTime.Now.Hour + 24 * DateTime.Now.DayOfYear)))));


        //===================================================================================================
        /// <summary>
        /// This is used for randomization of recently tested files
        /// </summary>
        public static Random RandomForRecentlyChecked
        {
            get => s_oRandomForRecentlyChecked;                
        }

        //===================================================================================================
        /// <summary>
        /// Some filesystems have milliseconds in them, some other don't even
        /// have all seconds on file times.
        /// 
        /// So compare the file times in a smart manner
        /// </summary>
        /// <param name="dtmTime1">Time of a file in one file system</param>
        /// <param name="dtmTime2">Time of a file in a different file system</param>
        /// <returns>true if the file times are considered equal</returns>
        //===================================================================================================
        public static bool FileTimesEqual(
            DateTime dtmTime1,
            DateTime dtmTime2)
        {
            // if one time is with milliseconds and the other without
            if ((dtmTime1.Millisecond == 0) != (dtmTime2.Millisecond == 0))
            {
                // then the difference should be within five seconds
                TimeSpan oTimeSpanDifference =
                    new TimeSpan(Math.Abs(dtmTime1.Ticks - dtmTime2.Ticks));
                bool bResult = oTimeSpanDifference.TotalSeconds < 5;
                return bResult;
            }
            else
                // if both times are with milliseconds or both are without 
                // then simply compare the times
                return dtmTime1 == dtmTime2;
        }


        //===================================================================================================
        /// <summary>
        /// This method identifies files that are interesting for messages about zero length and failed
        /// copy
        /// </summary>
        /// <param name="strFilePath">Path of the file for testing, if extension is interesting</param>
        /// <returns>true iff a message about zero-length of the file is desired</returns>
        //===================================================================================================
        public static bool CheckIfZeroLengthIsInteresting(
            string strFilePath
            )
        {
            return strFilePath.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".cr2", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".raf", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".mov", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".mpeg4", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".aac", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".avc", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".mts", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".m2ts", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".heic", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".avi", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".wmv", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".jp2", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".wma", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".flac", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".doc", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".docx", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".docm", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".xls", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".xlsx", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".clsm", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".ppt", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".pptx", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".pptm", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase);
        }

        //===================================================================================================
        /// <summary>
        /// This method calcs a possibly valid path for the .chk file
        /// </summary>
        /// <param name="strOriginalDir">The location of original file</param>
        /// <param name="strSubDirForSavedInfo">Subdir name</param>
        /// <param name="strFileName">Original file name</param>
        /// <param name="strNewExtension">New extension</param>
        /// <returns>The combined information, or a special path, if in other case the path would be
        /// too long</returns>
        //===================================================================================================
        public static string CreatePathOfChkFile(
            string strOriginalDir,
            string strSubDirForSavedInfo,
            string strFileName,
            string strNewExtension
            )
        {
            string str1 = System.IO.Path.Combine(
                System.IO.Path.Combine(strOriginalDir, strSubDirForSavedInfo), strFileName + strNewExtension);
            if (str1.Length >= 258)
            {
                str1 = System.IO.Path.Combine(
                    System.IO.Path.Combine(strOriginalDir, strSubDirForSavedInfo),
                    strFileName.Substring(0, 1) + strFileName.GetHashCode().ToString() + strNewExtension);
                if (str1.Length >= 258)
                {
                    // still too big? then try a smaller version, consisting of three chars
                    str1 = System.IO.Path.Combine(
                        System.IO.Path.Combine(strOriginalDir, strSubDirForSavedInfo),
                        strFileName.Substring(0, 1) + ((strFileName.GetHashCode() % 100).ToString()) + strNewExtension);
                }
            }
            return str1;
        }

    }
}
