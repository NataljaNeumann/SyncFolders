using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using SyncFoldersApi.Localization;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// This class provides methods for choosing and executing the appropriate
    /// synchronization steps for a pair of files.
    /// </summary>
    //*******************************************************************************************************
    public class FilePairStepChooser : IFilePairStepChooser
    {
        //===================================================================================================
        /// <summary>
        /// Decides which step to execute for a given pair of files and performs it
        /// using the provided IStepsImpl implementation.
        /// </summary>
        /// <param name="strFilePath1">Path to the first file</param>
        /// <param name="strFilePath2">Path to the second file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        public void ProcessFilePair(
            string strFilePath1,
            string strFilePath2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter)
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            if (strFilePath1.Equals(strFilePath2, StringComparison.CurrentCultureIgnoreCase) &&
                iSettings.TestFiles && iSettings.RepairFiles && iSettings.FirstToSecond && iSettings.FirstReadOnly)
            {
                // don't repair files, if first is read-only and it is the same file.
                throw new InvalidOperationException("Repair and read-only on a single file");
            }

            // Retrieve file info objects for both files
            IFileInfo fi1 = iFileSystem.GetFileInfo(strFilePath1);
            IFileInfo fi2 = iFileSystem.GetFileInfo(strFilePath2);

            //IFileInfo fi1 = null, fi2 = null;
            //try
            //{
            //  fi1 = iFileSystem.GetFileInfo(strFilePath1);
            //    fi2 = iFileSystem.GetFileInfo(strFilePath2);
            //}
            // this solves the problem in this place, but other problems appear down the code
            //catch (Exception oEx)
            //{
            //    string name1 = strFilePath1.Substring(strFilePath1.LastIndexOf('\\') + 1);
            //    foreach(IFileInfo fitest in m_iFileSystem.GetDirectoryInfo(strFilePath1.Substring(0,strFilePath1.LastIndexOf('\\'))).GetFiles())
            //        if (fitest.Name.Equals(name1, StringComparison.InvariantCultureIgnoreCase))
            //        {
            //            fi1 = fitest;
            //            break;
            //        }
            //
            //    string name2 = strFilePath2.Substring(strFilePath1.LastIndexOf('\\') + 1);
            //    foreach (IFileInfo fitest in m_iFileSystem.GetDirectoryInfo(strFilePath2.Substring(0, strFilePath2.LastIndexOf('\\'))).GetFiles())
            //        if (fitest.Name.Equals(name2, StringComparison.InvariantCultureIgnoreCase))
            //        {
            //            fi2 = fitest;
            //            break;
            //        }
            //    if (fi1 == null || fi2 == null)
            //        throw oEx;
            //}

            // this must be there, surely, don't question that again
            if (fi1.Name.Equals("SyncFolders-Dont-Delete.txt",
                    StringComparison.InvariantCultureIgnoreCase) ||
                fi1.Name.Equals("SyncFolders-Don't-Delete.txt",
                    StringComparison.InvariantCultureIgnoreCase) ||
                fi2.Name.Equals("SyncFolders-Dont-Delete.txt",
                    StringComparison.InvariantCultureIgnoreCase) ||
                fi2.Name.Equals("SyncFolders-Don't-Delete.txt",
                    StringComparison.InvariantCultureIgnoreCase))
            {
                iLogWriter.WriteLogFormattedLocalized(
                    0,
                    Properties.Resources.SkippingFilePairDontDelete,
                    fi1.FullName,
                    fi2.FullName
                );

                iLogWriter.WriteLog(
                    true, 0, "Skipping file pair ",
                    fi1.FullName, ", ", fi2.FullName,
                    ". Special file prevents usage of delete option at wrong root folder."
                );

                return;
            }

            // Determine which synchronization mode to use
            if (iSettings.FirstToSecond)
            {
                ProcessFilePair_FirstToSecond(
                    strFilePath1, strFilePath2,
                    fi1, fi2,
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter
                );
            }
            else
            {
                ProcessFilePair_Bidirectionally(
                    strFilePath1, strFilePath2,
                    fi1, fi2,
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter
                );
            }
        }

        //===================================================================================================
        /// <summary>
        /// Processes a file pair in "first-to-second" folder mode.
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter)
        {
            // Choose behavior depending on whether the first folder is read-only
            if (iSettings.FirstReadOnly)
            {
                ProcessFilePair_FirstToSecond_FirstReadonly(
                    strFilePath1, strFilePath2,
                    fi1, fi2,
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter
                );
            }
            else
            {
                ProcessFilePair_FirstToSecond_FirstReadWrite(
                    strFilePath1, strFilePath2,
                    fi1, fi2,
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter
                );
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadonly(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter)
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // Handle special case: both files exist but have zero length
            if (fi1.Exists && fi2.Exists && fi1.Length == 0 && fi2.Length == 0)
            {
                if (Utils.CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    if (strFilePath1.Equals(strFilePath2, StringComparison.CurrentCultureIgnoreCase))
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength,
                            strFilePath1);
                        iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                            "indicating a failed copy operation in the past: ",
                            strFilePath1);
                    }
                    else
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FilesHaveZeroLength,
                            strFilePath1, strFilePath2);
                        iLogWriter.WriteLog(true, 0, "Warning: both files have zero length, " +
                            "indicating a failed copy operation in the past: ",
                            strFilePath1, ", ", strFilePath2);
                    }
                }
            }
            else
            {
                // Handle if only one file exists or has zero length
                if (fi1.Exists && fi1.Length == 0 && Utils.CheckIfZeroLengthIsInteresting(strFilePath1))
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength,
                        strFilePath1);
                    iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                        "indicating a failed copy operation in the past: ",
                        strFilePath1);

                }

                if (fi2.Exists && fi2.Length == 0 && Utils.CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength,
                        strFilePath2);
                    iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                        "indicating a failed copy operation in the past: ",
                        strFilePath2);

                }


                if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                {
                    iLogic.ProcessFilePair_FirstToSecond_FirstReadonly_SecondExists(
                        strFilePath1, strFilePath2,
                        fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter
                    );
                }
                else if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                {
                    iLogic.ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(
                        strFilePath1, strFilePath2,
                        fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter
                    );
                }
                else
                {
                    ProcessFilePair_FirstToSecond_FirstReadonly_BothExist(
                        strFilePath1, strFilePath2,
                        fi1, fi2,
                        iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter
                    );
                }
            }
        }




        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only and both files exist
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter)
        {
            // Compare modification times and lengths
            if (!iSettings.FirstToSecondSyncMode ? 
                    (!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) 
                    :
                    ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) 
                       || (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
                )
            {
                iLogic.ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(
                    strFilePath1, strFilePath2,
                    fi1, fi2,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
            }
            else
            {
                iLogic.ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(
                    strFilePath1, strFilePath2,
                    fi1, fi2,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
            }
        }



        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to.
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadWrite(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            )
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // special case: both exist and both zero length
            if (fi2.Exists && fi1.Exists && fi1.Length == 0 && fi2.Length == 0)
            {
                if (Utils.CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    if (strFilePath1.Equals(strFilePath2, StringComparison.CurrentCultureIgnoreCase))
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength,
                            strFilePath1);
                        iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                            "indicating a failed copy operation in the past: ", strFilePath1);
                    }
                    else
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FilesHaveZeroLength,
                               strFilePath1, strFilePath2);
                        iLogWriter.WriteLog(true, 0, "Warning: both files have zero length, " +
                            "indicating a failed copy operation in the past: ",
                            strFilePath1, ", ", strFilePath2);
                    }
                }
            }
            else
            {
                if ((fi1.Exists && fi1.Length == 0) && Utils.CheckIfZeroLengthIsInteresting(strFilePath1))
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength,
                        strFilePath1);
                    iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                        "indicating a failed copy operation in the past: ", strFilePath1);
                }

                if ((fi2.Exists && fi2.Length == 0) && Utils.CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength,
                        strFilePath2);
                    iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                        "indicating a failed copy operation in the past: ", strFilePath2);
                }

                if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                {

                    iLogic.ProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists(
                        strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
                }
                else
                {
                    if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                    {
                        iLogic.ProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists(
                            strFilePath1, strFilePath2, fi1, fi2,
                            iFileSystem, iSettings, iStepsImpl, iLogWriter);
                    }
                    else
                        ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist(
                            strFilePath1, strFilePath2, fi1, fi2,
                            iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to and both files exist.
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            )
        {
            // first to second, but first can be written to
            if (!iSettings.FirstToSecondSyncMode ? 
                    (!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) 
                    :
                    ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc) 
                        || (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.Length != fi2.Length)))
               )
                ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
            else
                iLogic.ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to, both files exist and the first needs to be written over
        /// second file.
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NeedToCopy(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter
            )
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            iLogic.ProcessFilePair_Bidirectionally_BothExist_FirstNewer(strFilePath1, strFilePath2, fi1, fi2,
                iSettings.FirstToSecondSyncMode ? "(file was newer or bigger)" : "(file has a different date or length)",
                iSettings.FirstToSecondSyncMode ? Properties.Resources.FileWasNewer : Properties.Resources.FileHasDifferentTime,
                iFileSystem, iSettings, iStepsImpl, iLogWriter);
        }




        //===================================================================================================
        /// <summary>
        /// Processes a file pair bidirectionally (default mode).
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter)
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // Handle missing or zero-length files first
            if (fi1.Exists && fi2.Exists && fi1.Length == 0 && fi2.Length == 0)
            {
                if (Utils.CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    if (strFilePath1.Equals(strFilePath2,
                        StringComparison.CurrentCultureIgnoreCase))
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength, strFilePath1);
                        iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                            "indicating a failed copy operation in the past: ", strFilePath1);
                    }
                    else
                    {
                        iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FilesHaveZeroLength, strFilePath1, strFilePath2);
                        iLogWriter.WriteLog(true, 0, "Warning: both files have zero length, " +
                            "indicating a failed copy operation in the past: ", strFilePath1, ", ", strFilePath2);
                    }
                }
            }
            else
            {
                if (fi1.Exists && fi1.Length == 0 && Utils.CheckIfZeroLengthIsInteresting(strFilePath1))
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength, strFilePath1);
                    iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                        "indicating a failed copy operation in the past: ", strFilePath1);
                }

                if (fi2.Exists && fi2.Length == 0 && Utils.CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength, strFilePath2);
                    iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                        "indicating a failed copy operation in the past: ", strFilePath2);
                }

                if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                {
                    iLogic.ProcessFilePair_Bidirectionally_FirstExists(
                        strFilePath1, strFilePath2,
                        fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter
                    );
                }
                else
                {
                    if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                    {
                        iLogic.ProcessFilePair_Bidirectionally_SecondExists(
                            strFilePath1, strFilePath2,
                            fi1, fi2,
                            iFileSystem, iSettings, iStepsImpl, iLogWriter
                        );
                    }
                    else
                    {
                        ProcessFilePair_Bidirectionally_BothExist(
                            strFilePath1, strFilePath2,
                            fi1, fi2,
                            iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter
                        );
                    }
                }
            }
        }




        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist
        /// </summary>
        /// <param name="strFilePath1">Path of first file</param>
        /// <param name="strFilePath2">Path of second file</param>
        /// <param name="fi1">Information about the first file</param>
        /// <param name="fi2">Information about the first file</param>
        /// <param name="iFileSystem">File system abstraction for performing operations</param>
        /// <param name="iSettings">Settings defining synchronization mode and behavior</param>
        /// <param name="iLogic">Logic implementation determining actions</param>
        /// <param name="iStepsImpl">Implementation of the actual file steps</param>
        /// <param name="iLogWriter">Logger used for outputting messages</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally_BothExist(
            string strFilePath1,
            string strFilePath2,
            IFileInfo fi1,
            IFileInfo fi2,
            IFileOperations iFileSystem,
            IFilePairStepsSettings iSettings,
            IFilePairStepsDirectionLogic iLogic,
            IFilePairSteps iStepsImpl,
            ILogWriter iLogWriter)
        {
            if (Properties.Resources == null)
                throw new ArgumentNullException(nameof(Properties.Resources));

            // bidirectionally path
            if ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) ||
                (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
            {
                iLogic.ProcessFilePair_Bidirectionally_BothExist_FirstNewer(strFilePath1, strFilePath2, fi1, fi2,
                    "(file newer or bigger)", Properties.Resources.FileWasNewer,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
            }
            else
            {
                // bidirectionally path
                if ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi2.LastWriteTimeUtc > fi1.LastWriteTimeUtc)) ||
                    (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi2.Length > fi1.Length))
                {
                    iLogic.ProcessFilePair_Bidirectionally_BothExist_SecondNewer(strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter);
                }
                else
                {
                    iLogic.ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter);
                }
            }
        }





    }
}