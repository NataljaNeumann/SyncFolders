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
    /// This class provides means for choosing steps regarding a pair of files
    /// </summary>
    //*******************************************************************************************************
    public class FilePairStepChooser: IFilePairStepChooser
    {
        //===================================================================================================
        /// <summary>
        /// Decides the step to execute and executes it on IStepsImpl
        /// </summary>
        /// <param name="strFilePath1">Path to first file</param>
        /// <param name="strFilePath2">Path to second file</param>
        /// <param name="iFileSystem">File system for performing operations</param>
        /// <param name="iSettings">Settings for operations</param>
        /// <param name="iLogic">Implementation of steps logic</param>
        /// <param name="iStepsImpl">Implementation of steps</param>
        /// <param name="iLogWriter">Log writer</param>
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
                iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.SkippingFilePairDontDelete,
                    fi1.FullName, fi2.FullName);
                iLogWriter.WriteLog(true, 0, "Skipping file pair ", fi1.FullName,
                    ", ", fi2.FullName,
                    ". Special file prevents usage of delete option at wrong root folder.");
                return;
            }

            if (iSettings.FirstToSecond)
                ProcessFilePair_FirstToSecond(strFilePath1, strFilePath2, fi1, fi2, 
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
            else
                ProcessFilePair_Bidirectionally(strFilePath1, strFilePath2, fi1, fi2
                    , iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
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
            ILogWriter iLogWriter
            )
        {
            if (iSettings.FirstReadOnly)
                ProcessFilePair_FirstToSecond_FirstReadonly(
                    strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
            else
                ProcessFilePair_FirstToSecond_FirstReadWrite(
                    strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
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
            ILogWriter iLogWriter
            )
        {
            // special case: both exist and both zero length
            if (fi1.Exists && fi2.Exists &&
                fi1.Length == 0 && fi2.Length == 0)
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
                        strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter);
                }
                else
                {
                    if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                    {
                        iLogic.ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(
                            strFilePath1, strFilePath2, fi1, fi2,
                            iFileSystem, iSettings, iStepsImpl, iLogWriter);
                    }
                    else
                        ProcessFilePair_FirstToSecond_FirstReadonly_BothExist(
                            strFilePath1, strFilePath2, fi1, fi2,
                            iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
                }
            }
        }




        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only and both files exist
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
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
            ILogWriter iLogWriter
            )
        {
            if (!iSettings.FirstToSecondSyncMode ? (!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
                           ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) || (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
                )
                iLogic.ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
            else
                iLogic.ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
        }



        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to.
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
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
                };

                if ((fi2.Exists && fi2.Length == 0) && Utils.CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    iLogWriter.WriteLogFormattedLocalized(0, Properties.Resources.FileHasZeroLength,
                        strFilePath2);
                    iLogWriter.WriteLog(true, 0, "Warning: file has zero length, " +
                        "indicating a failed copy operation in the past: ", strFilePath2);
                };

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
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
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
            if (!iSettings.FirstToSecondSyncMode ? (!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
                           ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc) || (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.Length != fi2.Length)))
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
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
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
            iLogic.ProcessFilePair_Bidirectionally_BothExist_FirstNewer(strFilePath1, strFilePath2, fi1, fi2,
                iSettings.FirstToSecondSyncMode ? "(file was newer or bigger)" : "(file has a different date or length)",
                iSettings.FirstToSecondSyncMode ? Properties.Resources.FileWasNewer : Properties.Resources.FileHasDifferentTime,
                iFileSystem, iSettings, iStepsImpl, iLogWriter);
        }




        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default)
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
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
            // special case: both exist and both zero length
            if (fi1.Exists && fi2.Exists &&
                fi1.Length == 0 && fi2.Length == 0)
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
                        strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter);
                }
                else
                {
                    if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                    {
                        iLogic.ProcessFilePair_Bidirectionally_SecondExists(
                            strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter);
                    }
                    else
                        ProcessFilePair_Bidirectionally_BothExist(
                            strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iLogic, iStepsImpl, iLogWriter);
                }
            }
        }




        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
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
            ILogWriter iLogWriter
            )
        {
            // bidirectionally path
            if ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) ||
                (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
                iLogic.ProcessFilePair_Bidirectionally_BothExist_FirstNewer(strFilePath1, strFilePath2, fi1, fi2,
                    "(file newer or bigger)", Properties.Resources.FileWasNewer,
                    iFileSystem, iSettings, iStepsImpl, iLogWriter);
            else
            {
                // bidirectionally path
                if ((!Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi2.LastWriteTimeUtc > fi1.LastWriteTimeUtc)) ||
                    (Utils.FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi2.Length > fi1.Length))
                    iLogic.ProcessFilePair_Bidirectionally_BothExist_SecondNewer(strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter);
                else
                    iLogic.ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(strFilePath1, strFilePath2, fi1, fi2,
                        iFileSystem, iSettings, iStepsImpl, iLogWriter);
            }
        }



    }
}
