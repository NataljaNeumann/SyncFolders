/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024 NataljaNeumann@gmx.de

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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace SyncFolders
{
    public partial class FormSyncFolders : Form, ILogWriter
    {
        bool _cancelClicked;
        string _folder1;
        string _folder2;
        bool _bCreateInfo;
        bool _bTestFiles;
        bool _bRepairFiles;
        bool _bPreferPhysicalCopies;
        bool _bWorking;

        bool _bFirstToSecond;
        bool _bFirstReadOnly;
        bool _syncMode;
        bool _bDeleteInSecond;
        bool _bSkipRecentlyTested = true;
        bool _bIgnoreTimeDifferences;

        Random _randomizeChecked = new Random(DateTime.Now.Millisecond + 1000 * (DateTime.Now.Second + 60 * (DateTime.Now.Minute + 60 * (DateTime.Now.Hour + 24 * DateTime.Now.DayOfYear))));

        static int _maxParallelCopies = Math.Max(System.Environment.ProcessorCount * 5 / 8, 2);
        static int _maxParallelThreads = System.Environment.ProcessorCount * 3/2;
        System.Threading.Semaphore _copyFiles = new System.Threading.Semaphore(_maxParallelCopies, _maxParallelCopies);
        System.Threading.Semaphore _parallelThreads = new System.Threading.Semaphore(_maxParallelThreads, _maxParallelThreads);
        System.Threading.Semaphore _hugeReads = new System.Threading.Semaphore(1, 1);

        public FormSyncFolders()
        {
            InitializeComponent();
#if DEBUG
            buttonSelfTest.Visible = true;
            //checkBoxParallel.Visible = true;
#else
            textBoxSecondFolder.Text = Application.StartupPath;
#endif
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Some filesystems have milliseconds in them, some oOtherSaveInfo don't even have all seconds on file times
        /// So compare the file times in a smart manner
        /// </summary>
        /// <param name="t1">Time of a file in one file system</param>
        /// <param name="t2">Time of a file in a different file system</param>
        /// <returns></returns>
        private bool FileTimesEqual(DateTime t1, DateTime t2)
        {
            // if one time is with milliseconds and the oOtherSaveInfo without
            if ((t1.Millisecond == 0) != (t2.Millisecond == 0))
            {
                // then the difference should be within two seconds
                TimeSpan span = new TimeSpan(Math.Abs(t1.Ticks - t2.Ticks));
                bool bResult = span.TotalSeconds < 2;
                return bResult;
            }
            else
                // if both times are with milliseconds or both are without then simply compare the times
                return t1 == t2;
        }

        private void checkBoxTestAllFiles_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxRepairBlockFailures.Enabled = checkBoxTestAllFiles.Checked;
            checkBoxPreferCopies.Enabled = checkBoxTestAllFiles.Checked && checkBoxRepairBlockFailures.Checked;
            checkBoxSkipRecentlyTested.Enabled = checkBoxTestAllFiles.Checked;
        }

        private void buttonSelectFirstFolder_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxFirstFolder.Text))
                folderBrowserDialogFolder1.SelectedPath = textBoxFirstFolder.Text;

            if (folderBrowserDialogFolder1.ShowDialog() == DialogResult.OK)
            {
                textBoxFirstFolder.Text = folderBrowserDialogFolder1.SelectedPath;
            }
        }

        private void buttonSelectSecondFolder_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxSecondFolder.Text))
                folderBrowserDialogFolder2.SelectedPath = textBoxSecondFolder.Text;

            if (folderBrowserDialogFolder2.ShowDialog() == DialogResult.OK)
            {
                textBoxSecondFolder.Text = folderBrowserDialogFolder2.SelectedPath;
            }
        }

        private void buttonSync_Click(object sender, EventArgs e)
        {

            _folder1 = textBoxFirstFolder.Text;
            _folder2 = textBoxSecondFolder.Text;
            _bCreateInfo = checkBoxCreateRestoreInfo.Checked;
            _bTestFiles = checkBoxTestAllFiles.Checked;
            _bRepairFiles = checkBoxRepairBlockFailures.Checked;
            _bPreferPhysicalCopies = checkBoxPreferCopies.Checked;
            _bFirstToSecond = checkBoxFirstToSecond.Checked;
            _bFirstReadOnly = checkBoxFirstReadonly.Checked;
            _bDeleteInSecond = checkBoxDeleteFilesInSecond.Checked;
            _bSkipRecentlyTested = !_bTestFiles || checkBoxSkipRecentlyTested.Checked;
            _bIgnoreTimeDifferences = checkBoxIgnoreTime.Checked;
            _syncMode = checkBoxSyncMode.Checked;

            if (_bFirstToSecond && _bDeleteInSecond)
            {
                System.IO.FileInfo fiDontDelete = new System.IO.FileInfo(System.IO.Path.Combine(_folder2, "SyncFolders-Dont-Delete.txt"));
                if (fiDontDelete.Exists)
                {
                    System.Windows.Forms.MessageBox.Show(this, "The second folder contains file \"SyncFolders-Dont-Delete.txt\", the selected folder seem to be wrong for delete option", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                fiDontDelete = new System.IO.FileInfo(System.IO.Path.Combine(_folder2, "SyncFolders-Don't-Delete.txt"));
                if (fiDontDelete.Exists)
                {
                    System.Windows.Forms.MessageBox.Show(this, "The second folder contains file \"SyncFolders-Don't-Delete.txt\", the selected folder seem to be wrong for delete option", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            buttonSync.Visible = false;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            progressBar1.Visible = true;
            labelFolder1.Enabled = false;
            labelSecondFolder.Enabled = false;
            linkLabelAbout.Visible = false;
            linkLabelLicence.Visible = false;
            textBoxFirstFolder.Enabled = false;
            textBoxSecondFolder.Enabled = false;
            buttonSelectFirstFolder.Enabled = false;
            buttonSelectSecondFolder.Enabled = false;
            checkBoxCreateRestoreInfo.Enabled = false;
            checkBoxTestAllFiles.Enabled = false;
            checkBoxRepairBlockFailures.Enabled = false;
            checkBoxPreferCopies.Visible = false;
            checkBoxFirstToSecond.Enabled = false;
            checkBoxFirstReadonly.Enabled = false;
            checkBoxIgnoreTime.Enabled = false;
            checkBoxDeleteFilesInSecond.Enabled = false;
            checkBoxSkipRecentlyTested.Enabled = false;
            checkBoxSyncMode.Enabled = false;
            labelProgress.Visible = true;
            checkBoxParallel.Enabled = false;

            _currentFile = 0;
            _currentPath = null;
            timerUpdateFileDescription.Start();




            _cancelClicked = false;
            _log = new StringBuilder();
            _logFile = new System.IO.StreamWriter(System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SyncFolders.log"), true, Encoding.UTF8);
            _logFile.WriteLine("\r\n\r\n\r\n\r\n");
            _logFile.WriteLine("     First2Second: " + (_bFirstToSecond ? "yes" : "no"));
            if (_bFirstToSecond)
            {
                _logFile.WriteLine("         SyncMode: " + (_syncMode ? "yes" : "no"));
                _logFile.WriteLine("    FirstReadOnly: " + (_bFirstReadOnly ? "yes" : "no"));
                _logFile.WriteLine("   DeleteInSecond: " + (_bDeleteInSecond ? "yes" : "no"));
            }
            _logFile.WriteLine("CreateRestoreInfo: " + (_bCreateInfo ? "yes" : "no"));
            _logFile.WriteLine("        TestFiles: " + (_bTestFiles ? (_bSkipRecentlyTested ? "if not tested recently": "yes" ): "no"));
            if (_bTestFiles)
            {
                _logFile.WriteLine("      RepairFiles: " + (_bRepairFiles ? "yes" : "no"));
                if (_bRepairFiles)
                    _logFile.WriteLine("     PreferCopies: " + (_bPreferPhysicalCopies ? "yes" : "no"));
            }
            _logFile.WriteLine("         Folder 1: " + _folder1);
            _logFile.WriteLine("         Folder 2: " + _folder2);
            _logFile.Write("#####################################################################################################");
            _logFile.WriteLine("#####################################################################################################");
            _logFile.Flush();
            _bWorking = true;
            System.Threading.Thread worker = new System.Threading.Thread(SyncWorker);
            worker.Priority = System.Threading.ThreadPriority.BelowNormal;
            worker.Start();
        }


        void CopyFileSafely(System.IO.FileInfo fi, string targetPath, string strReason)
        {
            string targetPath2 = targetPath + ".tmp";
            try
            {
                fi.CopyTo(targetPath2,true);

                System.IO.FileInfo fi2 = new System.IO.FileInfo(targetPath);
                if (fi2.Exists)
                    fi2.Delete();

                System.IO.FileInfo fi2tmp = new System.IO.FileInfo(targetPath2);
                fi2tmp.MoveTo(targetPath);
                WriteLog(0, "Copied ", fi.FullName, " to ", targetPath, " ", strReason);                
            } catch
            {
                try
                {
                    System.Threading.Thread.Sleep(5000);
                    System.IO.FileInfo fi2 = new System.IO.FileInfo(targetPath2);
                    if (fi2.Exists)
                        fi2.Delete();
                } catch
                {
                    // ignore additional exceptions
                }
                throw;
            }
        }

        List<KeyValuePair<string, string>> _filePairs;
        void SyncWorker()
        {
            // first of all search file pairs for synching
            _filePairs = new List<KeyValuePair<string, string>>();
            bool bException = false;
            try
            {
                FindFilePairs(_folder1, _folder2);
            }
            catch (Exception ex)
            {
                WriteLog(0, ex.Message);
                bException = true;
            }

            if (!bException)
            {
                if (_filePairs.Count != 1)
                    WriteLog(0, "Found ", _filePairs.Count, " files for possible synchronisation");
                else
                    if (_filePairs.Count == 1)
                        WriteLog(0, "Found 1 file for possible synchronisation");

                if (InvokeRequired)
                {
                    Invoke(new EventHandler(delegate(object sender, EventArgs args)
                    {
                        progressBar1.Minimum = 0;
                        progressBar1.Maximum = _filePairs.Count;
                        progressBar1.Value = 0;
                    }));
                }
                else
                {
                    progressBar1.Minimum = 0;
                    progressBar1.Maximum = _filePairs.Count;
                    progressBar1.Value = 0;
                }

                // if user still has not clicked cancel
                if (!_cancelClicked)
                {
                    int currentFile = 0;


                    // sort the list, so it is in a defined order
                    SortedDictionary<string, string> sorted = new SortedDictionary<string, string>();
                    foreach (KeyValuePair<string, string> pathPair in _filePairs)
                    {
                        if (!_bFirstToSecond)
                        {
                            if (string.Compare(pathPair.Key, pathPair.Value, StringComparison.InvariantCultureIgnoreCase) < 0)
                                sorted[pathPair.Key] = pathPair.Value;
                            else
                                sorted[pathPair.Value] = pathPair.Key;
                        }
                        else
                            sorted[pathPair.Key] = pathPair.Value;
                    }

                    // start processing file pairs, one by one
                    foreach (KeyValuePair<string, string> pathPair in sorted)
                    {

                        //*
                        _parallelThreads.WaitOne();

                        if (_cancelClicked)
                        {
                            _parallelThreads.Release();
                            break;
                        }

                        System.Threading.Thread worker = new System.Threading.Thread(FilePairWorker);
                        worker.Priority = System.Threading.ThreadPriority.Lowest;
                        worker.Start(pathPair);

                        /*/

                        try
                        {

                            ProcessFilePair(pathPair.Key, pathPair.Value);
                        }
                        catch (Exception ex)
                        {
                            WriteLog(0, "Error while processing file pair \"", pathPair.Key, "\" | \"", pathPair.Value, "\": ", ex.Message);
                        };

                        //*/

                        _currentFile = currentFile;
                        _currentPath = pathPair.Key;

                        if ((++currentFile) % 10 == 0)
                        {
                            if (InvokeRequired)
                            {
                                Invoke(new EventHandler(delegate(object sender, EventArgs args)
                                {
                                    progressBar1.Value = currentFile;
                                    labelProgress.Text = pathPair.Key;
                                }));
                            }
                            else
                            {
                                progressBar1.Value = currentFile;
                                labelProgress.Text = pathPair.Key;
                            }
                        };

                        if (_cancelClicked)
                            break;
                    }
                }


                // wait for all parallel threads to finnish
                for (int i = 0; i < _maxParallelThreads; ++i)
                    _parallelThreads.WaitOne();

                // free the parallel threads back again
                for (int i = 0; i < _maxParallelThreads; ++i)
                    _parallelThreads.Release();


                if (!_cancelClicked)
                    RemoveOldFilesAndDirs(_folder1, _folder2);

                if (_cancelClicked)
                    WriteLog(0, "Operation canceled");
                else
                    WriteLog(0, "Operation finished");
            };

            _logFile.Close();
            _logFile.Dispose();
            _logFile = null;

            if (InvokeRequired)
            {
                Invoke(new EventHandler(delegate (object sender, EventArgs args)
                {
                    buttonSync.Visible = true;
                    progressBar1.Visible = false;
                    labelFolder1.Enabled = true;
                    labelSecondFolder.Enabled = true;
                    linkLabelAbout.Visible = true;
                    linkLabelLicence.Visible = true;
                    textBoxFirstFolder.Enabled = true;
                    textBoxSecondFolder.Enabled = true;
                    buttonSelectFirstFolder.Enabled = true;
                    buttonSelectSecondFolder.Enabled = true;
                    checkBoxCreateRestoreInfo.Enabled = true;
                    checkBoxTestAllFiles.Enabled = true;
                    checkBoxRepairBlockFailures.Enabled = checkBoxTestAllFiles.Checked;
                    checkBoxPreferCopies.Visible = true;
                    checkBoxFirstToSecond.Enabled = true;
                    checkBoxIgnoreTime.Enabled = true;
                    checkBoxFirstReadonly.Enabled = checkBoxFirstToSecond.Checked;
                    checkBoxDeleteFilesInSecond.Enabled = checkBoxFirstToSecond.Checked;
                    checkBoxSkipRecentlyTested.Enabled = checkBoxTestAllFiles.Checked;
                    checkBoxSyncMode.Enabled = checkBoxFirstToSecond.Checked;
                    labelProgress.Visible = false;
                    checkBoxParallel.Enabled = true;

                    _bWorking = false;
                    buttonCancel.Enabled = true;
                    timerUpdateFileDescription.Stop();

                    using (LogDisplayingForm form = new LogDisplayingForm())
                    {
                        form.textBoxLog.Text = _log.ToString();
                        form.ShowDialog(this);
                    }

                    GC.Collect();
                }));
            } else
            {
                buttonSync.Visible = true;
                progressBar1.Visible = false;
                labelFolder1.Enabled = true;
                labelSecondFolder.Enabled = true;
                linkLabelAbout.Visible = true;
                linkLabelLicence.Visible = true;
                textBoxFirstFolder.Enabled = true;
                textBoxSecondFolder.Enabled = true;
                buttonSelectFirstFolder.Enabled = true;
                buttonSelectSecondFolder.Enabled = true;
                checkBoxCreateRestoreInfo.Enabled = true;
                checkBoxTestAllFiles.Enabled = true;
                checkBoxRepairBlockFailures.Enabled = checkBoxTestAllFiles.Checked;
                checkBoxPreferCopies.Visible = true;
                checkBoxFirstToSecond.Enabled = true;
                checkBoxIgnoreTime.Enabled = true;
                checkBoxFirstReadonly.Enabled = checkBoxFirstToSecond.Checked;
                checkBoxDeleteFilesInSecond.Enabled = checkBoxFirstToSecond.Checked;
                checkBoxSkipRecentlyTested.Enabled = checkBoxTestAllFiles.Checked;
                checkBoxSyncMode.Enabled = checkBoxFirstToSecond.Checked;
                checkBoxParallel.Enabled = true;

                labelProgress.Visible = false;
                _bWorking = false;
                buttonCancel.Enabled = true;

                timerUpdateFileDescription.Stop();


                using (LogDisplayingForm form = new LogDisplayingForm())
                {
                    form.textBoxLog.Text = _log.ToString();
                    form.ShowDialog(this);
                }


                GC.Collect();

            }
        }

        void FindFilePairs(string path1, string path2)
        {
            System.IO.DirectoryInfo di1 = new System.IO.DirectoryInfo(path1);
            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(path2);

            // don't sync recycle bin
            if (di1.Name.Equals("$RECYCLE.BIN", StringComparison.InvariantCultureIgnoreCase))
                return;

            // don't sync system volume information
            if (di1.Name.Equals("System Volume Information", StringComparison.InvariantCultureIgnoreCase))
                return;

            // if one of the directories exists while the oOtherSaveInfo doesn't then create the missing one
            // and set its attributes
            if (di1.Exists && !di2.Exists)
            {
                di2.Create();
                di2 = new System.IO.DirectoryInfo(path2);
                di2.Attributes = di1.Attributes;
            } else
            if (di2.Exists && !di1.Exists)
            {
                if (!_bFirstToSecond)
                {
                    di1.Create();
                    di1.Attributes = di2.Attributes;
                }
            };


            if (di1.Name.Equals("RestoreInfo", StringComparison.CurrentCultureIgnoreCase))
                return;
            if (_bCreateInfo)
            {
                System.IO.DirectoryInfo di3;

                if (!_bFirstToSecond || !_bFirstReadOnly)
                {
                    di3 = new System.IO.DirectoryInfo(System.IO.Path.Combine(path1, "RestoreInfo"));
                    if (!di3.Exists)
                    {
                        di3.Create();
                        di3 = new System.IO.DirectoryInfo(System.IO.Path.Combine(path1, "RestoreInfo"));
                        di3.Attributes = di3.Attributes | System.IO.FileAttributes.Hidden;
                    }
                }

                di3 = new System.IO.DirectoryInfo(System.IO.Path.Combine(path2, "RestoreInfo"));
                if (!di3.Exists)
                {
                    di3.Create();
                    di3 = new System.IO.DirectoryInfo(System.IO.Path.Combine(path2, "RestoreInfo"));
                    di3.Attributes = di3.Attributes | System.IO.FileAttributes.Hidden;
                }

            }

            if (_bFirstToSecond && _bDeleteInSecond)
            {
                System.IO.FileInfo fiDontDelete = new System.IO.FileInfo(System.IO.Path.Combine(_folder2, "SyncFolders-Dont-Delete.txt"));
                if (!fiDontDelete.Exists)
                    fiDontDelete = new System.IO.FileInfo(System.IO.Path.Combine(_folder2, "SyncFolders-Don't-Delete.txt"));

                if (fiDontDelete.Exists)
                {
                    WriteLog(0, "Error: The second folder contains file \"SyncFolders-Dont-Delete.txt\", the selected folder seem to be wrong for delete option. Skipping processing of the folder and subfolders");
                    return;
                }
            }


            // find files in both directories
            Dictionary<string, bool> fileNames = new Dictionary<string, bool>();
            if (di1.Exists || !_bFirstToSecond)
            {
                foreach (System.IO.FileInfo fi1 in di1.GetFiles())
                {
                    if (fi1.Name.Length<=4 || !".tmp".Equals(fi1.Name.Substring(fi1.Name.Length - 4), StringComparison.InvariantCultureIgnoreCase))
                        fileNames[fi1.Name] = false;
                }

                foreach (System.IO.FileInfo fi2 in di2.GetFiles())
                {
                    if (fi2.Name.Length <= 4 || !".tmp".Equals(fi2.Name.Substring(fi2.Name.Length - 4), StringComparison.InvariantCultureIgnoreCase))
                        fileNames[fi2.Name] = false;
                }
            }


            foreach (string fileName in fileNames.Keys)
                _filePairs.Add( new KeyValuePair<string,string>(System.IO.Path.Combine(path1, fileName), System.IO.Path.Combine(path2, fileName)));


            // find subdirectories in both directories
            Dictionary<string, bool> dirNames = new Dictionary<string, bool>();

            if (di1.Exists || !_bFirstToSecond)
            {
                foreach (System.IO.DirectoryInfo sub1 in di1.GetDirectories())
                    dirNames[sub1.Name] = false;

                foreach (System.IO.DirectoryInfo sub2 in di2.GetDirectories())
                    dirNames[sub2.Name] = false;
            }

            // free the parent directory info objects
            di1 = null;
            di2 = null;

            // continue with the subdirs
            foreach (string subDirName in dirNames.Keys)
            {
                FindFilePairs(System.IO.Path.Combine(path1, subDirName), System.IO.Path.Combine(path2, subDirName));
                if (_cancelClicked)
                    break;
            }
        }

        bool CheckIfContains(IEnumerable<System.IO.FileInfo> availableFiles, System.IO.FileInfo fi, FileEqualityComparer cmp, FileEqualityComparer2 cmp2)
        {
            foreach (System.IO.FileInfo fi2 in availableFiles)
                if (cmp.Equals(fi2, fi))
                    return true;

            foreach (System.IO.FileInfo fi2 in availableFiles)
                if (cmp2.Equals(fi2, fi))
                    return true;

            return false;
        }

        void RemoveOldFilesAndDirs(string folderPath1, string folderPath2)
        {
            System.IO.DirectoryInfo di1 = new System.IO.DirectoryInfo(folderPath1);
            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(folderPath2);

            // don't sync recycle bin
            if (di1.Name.Equals("$RECYCLE.BIN", StringComparison.InvariantCultureIgnoreCase))
                return;

            // don't sync system volume information
            if (di1.Name.Equals("System Volume Information", StringComparison.InvariantCultureIgnoreCase))
                return;


            // the contents of the RestoreInfo folders is considered at their parent folders
            if (di1.Name == "RestoreInfo")
                return;

            // if one of the directories exists while the oOtherSaveInfo doesn't then create the missing one
            // and set its attributes
            if (di2.Exists && !di1.Exists)
            {
                if (_bFirstToSecond && _bDeleteInSecond)
                {
                    di2.Delete(true);
                    WriteLog(0, "Deleted folder ", di2.FullName, " including contents, because there is no ", folderPath1, " anymore");
                    return;
                }
            };

            System.IO.DirectoryInfo di3;
            // consider contents of the first folder
            if (!_bFirstToSecond || !_bFirstReadOnly)
            {
                di3 = new System.IO.DirectoryInfo(System.IO.Path.Combine(folderPath1, "RestoreInfo"));

                if (di3.Exists)
                {
                    List<System.IO.FileInfo> availableFiles = new List<System.IO.FileInfo>();
                    availableFiles.AddRange(di1.GetFiles());

                    FileEqualityComparer feq = new FileEqualityComparer();
                    FileEqualityComparer2 feq2 = new FileEqualityComparer2();

                    foreach (System.IO.FileInfo fi in di3.GetFiles())
                    {
                        try
                        {
                            if (fi.Extension.Equals(".chk", StringComparison.InvariantCultureIgnoreCase) && !CheckIfContains(availableFiles, new System.IO.FileInfo(System.IO.Path.Combine(di3.FullName,fi.Name.Substring(0, fi.Name.Length - 4))), feq, feq2))
                            {
                                fi.Delete();
                            } else
                            if (fi.Extension.Equals(".chked", StringComparison.InvariantCultureIgnoreCase) && !CheckIfContains(availableFiles, new System.IO.FileInfo(System.IO.Path.Combine(di3.FullName,fi.Name.Substring(0, fi.Name.Length - 6))), feq, feq2))
                            {
                                fi.Delete();
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                WriteLog(0, "Error while deleting ", System.IO.Path.Combine(di3.FullName,fi.Name), ": ", ex.Message);
                            }
                            catch (Exception ex2)
                            {
                                WriteLog(0, "Error in RemoveOldFilesAndDirs: ", ex2.Message); 
                            }
                        }
                    }
                }
            }


            // consider contents of the second folder
            di3 = new System.IO.DirectoryInfo(System.IO.Path.Combine(folderPath2, "RestoreInfo"));
            if (di3.Exists)
            {
                List<System.IO.FileInfo> availableFiles = new List<System.IO.FileInfo>();
                availableFiles.AddRange(di2.GetFiles());

                FileEqualityComparer feq = new FileEqualityComparer();
                FileEqualityComparer2 feq2 = new FileEqualityComparer2();

                foreach (System.IO.FileInfo fi in di3.GetFiles())
                {
                    try
                    {
                        if (fi.Extension.Equals(".chk", StringComparison.InvariantCultureIgnoreCase) && !CheckIfContains(availableFiles, new System.IO.FileInfo(System.IO.Path.Combine(di3.FullName, fi.Name.Substring(0, fi.Name.Length - 4))), feq, feq2))
                        {
                            fi.Delete();
                        }
                        else
                        if (fi.Extension.Equals(".chked", StringComparison.InvariantCultureIgnoreCase) && !CheckIfContains(availableFiles, new System.IO.FileInfo(System.IO.Path.Combine(di3.FullName, fi.Name.Substring(0, fi.Name.Length - 6))), feq, feq2))
                        {
                            fi.Delete();
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            WriteLog(0, "Error while deleting ", System.IO.Path.Combine(di3.FullName, fi.Name), ": ", ex.Message);
                        }
                        catch (Exception ex2)
                        {
                            WriteLog(0, "Error while deleting files in ", di3.FullName, ": ", ex.Message);
                            WriteLog(1, "Error while writing log: ", ex2.Message);
                        }
                    }
                }
            }


            // find subdirectories in both directories
            Dictionary<string, bool> dirNames = new Dictionary<string, bool>();

            if (di1.Exists)
                foreach (System.IO.DirectoryInfo sub1 in di1.GetDirectories())
                    dirNames[sub1.Name] = false;

            if (di2.Exists)
                foreach (System.IO.DirectoryInfo sub2 in di2.GetDirectories())
                    dirNames[sub2.Name] = false;

            // free the parent directory info objects
            di1 = null;
            di2 = null;
            di3 = null;

            // continue with the subdirs
            foreach (string subDirName in dirNames.Keys)
            {
                RemoveOldFilesAndDirs(System.IO.Path.Combine(folderPath1, subDirName), System.IO.Path.Combine(folderPath2, subDirName));
                if (_cancelClicked)
                    break;
            }

        }

        class FileEqualityComparer : IEqualityComparer<System.IO.FileInfo>
        {
            #region IEqualityComparer<DirectoryInfo> Members

            public bool Equals(System.IO.FileInfo x, System.IO.FileInfo y)
            {
                if (x == null || y == null)
                    return x == y;

                return string.Equals(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase);
                //return string.Equals(x.Name + x.Extension, y.Name + y.Extension, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(System.IO.FileInfo obj)
            {
                return (obj.Name + obj.Extension).ToUpper().GetHashCode();
            }

            #endregion
        }


        class FileEqualityComparer2 : IEqualityComparer<System.IO.FileInfo>
        {
            #region IEqualityComparer<DirectoryInfo> Members

            public bool Equals(System.IO.FileInfo x, System.IO.FileInfo y)
            {
                if (x == null || y == null)
                    return x == y;

                return (x.Name[0]==y.Name[0]) && string.Equals(x.Name.Substring(0,1)+(x.Name.GetHashCode()), y.Name, StringComparison.InvariantCultureIgnoreCase);
                //return string.Equals(x.Name + x.Extension, y.Name + y.Extension, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(System.IO.FileInfo obj)
            {
                return (obj.Name + obj.Extension).ToUpper().GetHashCode();
            }

            #endregion
        }

        void FilePairWorker (object filePair)
        {
            KeyValuePair<string, string> pathPair = (KeyValuePair<string, string>)filePair;
            try
            {
                ProcessFilePair(pathPair.Key, pathPair.Value);
            }
            catch (OperationCanceledException ex)
            {
                // report only if it is unexpected
                if (!_cancelClicked)
                    WriteLog(0, "Error while processing file pair \"", pathPair.Key, "\" | \"", pathPair.Value, "\": ", ex.Message);
            }
            catch (Exception ex)
            {
                WriteLog(0, "Error while processing file pair \"", pathPair.Key, "\" | \"", pathPair.Value, "\": ", ex.Message);
            }
            finally
            {
                _parallelThreads.Release();
            }
        }

        volatile int _dummy_counter;
        string CreatePathOfChkFile(string originaldir, string diraddon, string filename, string newext)
        {
            string str1 = System.IO.Path.Combine(System.IO.Path.Combine(originaldir, diraddon), filename+newext);
            if (str1.Length >= 258)
            {
                str1 = System.IO.Path.Combine(System.IO.Path.Combine(originaldir, diraddon), filename.Substring(0, 1) + filename.GetHashCode().ToString() + newext);
                if (str1.Length >= 258)
                {
                    str1 = System.IO.Path.Combine(System.IO.Path.Combine(originaldir, diraddon), filename.Substring(0, 1) + ((++_dummy_counter).ToString()) + newext);
                }
            }
            return str1;
        }


        static volatile bool _randomOrder;
        void ProcessFilePair(string filePath1, string filePath2)
        {
            System.IO.FileInfo fi1 = null, fi2 = null;

            //try
            {
                fi1 = new System.IO.FileInfo(filePath1);
                fi2 = new System.IO.FileInfo(filePath2);
            }
            // this solves the problem in this place, but oOtherSaveInfo problems appear down the code
            //catch (Exception ex)
            //{
            //    string name1 = filePath1.Substring(filePath1.LastIndexOf('\\') + 1);
            //    foreach(System.IO.FileInfo fitest in new System.IO.DirectoryInfo(filePath1.Substring(0,filePath1.LastIndexOf('\\'))).GetFiles())
            //        if (fitest.Name.Equals(name1, StringComparison.InvariantCultureIgnoreCase))
            //        {
            //            fi1 = fitest;
            //            break;
            //        }
            //
            //    string name2 = filePath2.Substring(filePath1.LastIndexOf('\\') + 1);
            //    foreach (System.IO.FileInfo fitest in new System.IO.DirectoryInfo(filePath2.Substring(0, filePath2.LastIndexOf('\\'))).GetFiles())
            //        if (fitest.Name.Equals(name2, StringComparison.InvariantCultureIgnoreCase))
            //        {
            //            fi2 = fitest;
            //            break;
            //        }
            //    if (fi1 == null || fi2 == null)
            //        throw ex;
            //}

            // this must be there, surely, don't question that again
            if (fi1.Name.Equals("SyncFolders-Dont-Delete.txt", StringComparison.InvariantCultureIgnoreCase)||
                fi1.Name.Equals("SyncFolders-Don't-Delete.txt", StringComparison.InvariantCultureIgnoreCase)||
                fi2.Name.Equals("SyncFolders-Dont-Delete.txt", StringComparison.InvariantCultureIgnoreCase) ||
                fi2.Name.Equals("SyncFolders-Don't-Delete.txt", StringComparison.InvariantCultureIgnoreCase))
            {
                WriteLog(0, "Skipping file pair ", fi1.FullName, ", ", fi2.FullName, ". Special file prevents usage of delete option at wrong root folder.");
                return;
            }

            if (_bFirstToSecond)
                ProcessFilePair_FirstToSecond(filePath1, filePath2, fi1, fi2);
            else
                ProcessFilePair_Bidirectionally(filePath1, filePath2, fi1, fi2);
        }

        void ProcessFilePair_FirstToSecond(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            if (_bFirstReadOnly)
                ProcessFilePair_FirstToSecond_FirstReadonly(filePath1, filePath2, fi1, fi2);            
            else
                ProcessFilePair_FirstToSecond_FirstReadWrite(filePath1, filePath2, fi1, fi2);            
        }

        void ProcessFilePair_FirstToSecond_FirstReadonly(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // special case: both exist and both zero length
            if (fi1.Exists && fi2.Exists && fi1.Length == 0 && fi2.Length == 0)
            {
                if (CheckIfZeroLengthIsInteresting(filePath2))
                {
                    if (filePath1.Equals(filePath2, StringComparison.CurrentCultureIgnoreCase))
                        WriteLog(0, "Warning: file has zero length, indicating a failed copy operation in the past: ", filePath1);
                    else
                        WriteLog(0, "Warning: both files have zero length, indicating a failed copy operation in the past: ", filePath1, ", ", filePath2);
                }
            }
            else
            {
                if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                    ProcessFilePair_FirstToSecond_FirstReadonly_SecondExists(filePath1, filePath2, fi1, fi2);
                else
                {
                    if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                        ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(filePath1, filePath2, fi1, fi2);
                    else
                        ProcessFilePair_FirstToSecond_FirstReadonly_BothExist(filePath1, filePath2, fi1, fi2);
                }
            }
        }


        void ProcessFilePair_FirstToSecond_FirstReadonly_SecondExists(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            if (_bDeleteInSecond)
            {
                System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                if (fi2ri.Exists)
                    fi2ri.Delete();
                fi2.Delete();
                WriteLog(0, "Deleted file ", fi2.FullName, " that is not present in ", fi1.Directory.FullName, " anymore");
            }
            else
            {
                if (_bTestFiles)
                {
                    System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    bool bForceCreateInfo = false;
                    if (_bRepairFiles)
                        TestAndRepairSingleFile(fi2.FullName, fi2ri.FullName, ref bForceCreateInfo);
                    else
                        TestSingleFile(fi2.FullName, fi2ri.FullName, ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);

                    if (_bCreateInfo && (!fi2ri.Exists || bForceCreateInfo))
                    {
                        CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
                    }
                }
            }
        }

        void ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreatInfo = false;
            bool bForceCreatInfo2 = false;
            if (_cancelClicked)
                return;
            try
            {
                _copyFiles.WaitOne();

                if (_cancelClicked)
                    return;

                CopyRepairSingleFile(fi2.FullName, fi1.FullName, fi1ri.FullName, ref bForceCreatInfo, ref bForceCreatInfo2, "(file was new)", false, false);
            }
            finally
            {
                _copyFiles.Release();
            }

            if (_bCreateInfo || fi1ri.Exists || fi2ri.Exists)
            {
                if (fi1ri.Exists && !bForceCreatInfo && !bForceCreatInfo2)
                {
                    try
                    {
                        _copyFiles.WaitOne();

                        //CopyFileSafely(fi1ri, fi2ri.FullName);
                        fi1ri.CopyTo(fi2ri.FullName, true);
                    } catch 
                    {
                        CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }
                }
                else
                    CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
            }
        }

        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            if (!_syncMode ? (!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
                           ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) || (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
                )
                ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(filePath1, filePath2, fi1, fi2);            
            else
                ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(filePath1, filePath2, fi1, fi2);
        }


        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo = false;

            // if the first file is ok
            if (TestSingleFile(filePath1, fi1ri.FullName, ref bForceCreateInfo, _bTestFiles, true, false))
            {
                if (_cancelClicked)
                    return;
                // then simply copy it
                try
                {
                    _copyFiles.WaitOne();

                    if (_cancelClicked)
                        return;

                    CopyFileSafely(fi1, filePath2, "(file newer or bigger)");
                    //fi1.CopyTo(filePath2, true);
                }
                finally
                {
                    _copyFiles.Release();
                }

                if (_bCreateInfo || fi2ri.Exists || fi1ri.Exists)
                {
                    if (bForceCreateInfo)
                        CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
                    else
                    {
                        try
                        {
                            _copyFiles.WaitOne();
                            fi1ri.CopyTo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                        }
                        finally
                        {
                            _copyFiles.Release();
                        }

                    }
                }
            }
            else
                WriteLog(0, "Warning: First file ", filePath1, " has bad blocks, overwriting file ", filePath2, " has been skipped, so the it remains as backup");
        }

        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            if (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length == fi2.Length)
            {
                // we are in first readonly path
                // both files are present and have same modification date and lentgh

                // if the second restoreinfo is missing or has wrong date, but the oOtherSaveInfo is OK then copy the one to the oOtherSaveInfo
                if (fi1ri.Exists && fi1ri.LastWriteTimeUtc == fi1.LastWriteTimeUtc && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        fi1ri.CopyTo(fi2ri.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }

                    fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                bool bCreateInfo = false;
                if (_bTestFiles)
                    if (_bRepairFiles)
                        TestAndRepairSecondFile(fi1.FullName, fi2.FullName, fi1ri.FullName, fi2ri.FullName, ref bCreateInfo);
                    else
                        TestSingleFile(fi2.FullName, fi2ri.FullName, ref bCreateInfo, true, !_bSkipRecentlyTested, true);


                if (_bCreateInfo && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc != fi2.LastWriteTimeUtc || bCreateInfo))
                {
                    CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
                }

                fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

                // if one of the files is missing or has wrong date, but the oOtherSaveInfo is OK then copy the one to the oOtherSaveInfo
                if (fi1ri.Exists && fi1ri.LastWriteTimeUtc == fi1.LastWriteTimeUtc && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        fi1ri.CopyTo(fi2ri.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }
                }
            } else
            {
                bool bForceCreateInfo = false;
                bool bOK = true;
                if (_bTestFiles)
                {
                    // test first file
                    TestSingleFile(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);

                    // test or repair second file, which is different from first
                    if (_bRepairFiles)
                        TestAndRepairSingleFile(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo);
                    else
                        bOK = TestSingleFile(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);

                    if (bOK && _bCreateInfo && (!fi2ri.Exists || bForceCreateInfo))
                    {
                        CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
                    }
                }
            }
        }

        void ProcessFilePair_FirstToSecond_FirstReadWrite(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // special case: both exist and both zero length
            if (fi2.Exists && fi1.Exists && fi1.Length == 0 && fi2.Length == 0)
            {
                if (CheckIfZeroLengthIsInteresting(filePath2))
                {
                    if (filePath1.Equals(filePath2, StringComparison.CurrentCultureIgnoreCase))
                        WriteLog(0, "Warning: file has zero length, indicating a failed copy operation in the past: ", filePath1);
                    else
                        WriteLog(0, "Warning: both files have zero length, indicating a failed copy operation in the past: ", filePath1, ", ", filePath2);
                }
            }
            else
            {
                if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                    ProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists(filePath1, filePath2, fi1, fi2);
                else
                {
                    if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                        ProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists(filePath1, filePath2, fi1, fi2);
                    else
                        ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist(filePath1, filePath2, fi1, fi2);
                }
            }
        }

        void ProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            if (_bDeleteInSecond)
            {
                System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                if (fi2ri.Exists)
                    fi2ri.Delete();
                fi2.Delete();
                WriteLog(0, "Deleted file ", fi2.FullName, " that is not present in ", fi1.Directory.FullName, " anymore");
            }
            else
            {
                if (_bTestFiles)
                {
                    System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    bool bForceCreateInfo = false;
                    bool bOK = true;

                    if (_bRepairFiles)
                        TestAndRepairSingleFile(fi2.FullName, fi2ri.FullName, ref bForceCreateInfo);
                    else
                        bOK = TestSingleFile(fi2.FullName, fi2ri.FullName, ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);

                    if (bOK && _bCreateInfo && (!fi2ri.Exists || bForceCreateInfo))
                    {
                        CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
                    }
                }
            }
        }

        void ProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            ProcessFilePair_Bidirectionally_FirstExists(filePath1, filePath2, fi1, fi2);
        }

        void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // first to second, but first can be written to
            if (!_syncMode ? (!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
                           ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.LastAccessTimeUtc > fi2.LastAccessTimeUtc) || (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.Length != fi2.Length)))
               )
                ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NeedToCopy(filePath1, filePath2, fi1, fi2);            
            else
                ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy(filePath1, filePath2, fi1, fi2);
        }


        void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NeedToCopy(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            ProcessFilePair_Bidirectionally_BothExist_FirstNewer(filePath1, filePath2, fi1, fi2, 
                _syncMode?"(file was newer or bigger)":"(file has a different date or length)");
        }

        void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // both files are present and have same modification date
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // first to second, but first can be written to
            if (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length == fi2.Length)
            {
                ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(filePath1, filePath2, fi1, fi2);
            }
            else
            {
                bool bForceCreateInfo = false;
                bool bOK = true;
                if (_bTestFiles)
                {
                    bOK = TestSingleFile(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);
                    if (!bOK && _bRepairFiles)
                    {
                        // first try to repair second file internally
                        if (TestSingleFileHealthyOrCanRepair(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo))
                            bOK = TestAndRepairSingleFile(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo);

                        if (bOK && bForceCreateInfo)
                        {
                            CreateRestoreInfo(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                        }
                        bForceCreateInfo = false;

                        // if it didn't work, then try to repair using first file
                        if (!bOK)
                        {
                            bOK = TestSingleFile(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo, true, true, true);
                            if (!bOK && TestSingleFileHealthyOrCanRepair(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo))
                                bOK = TestAndRepairSingleFile(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo);

                            if (bOK && bForceCreateInfo)
                            {
                                bOK = CreateRestoreInfo(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                                bForceCreateInfo = false;
                            }

                            if (bOK)
                            {
                                if (fi1.LastWriteTime.Year>1975)
                                    CopyFileSafely(fi1, filePath2, "(file was healthy, or repaired)");
                                else
                                    WriteLog(0, "Warning: couldn't use outdated file ", filePath1," with year 1975 or earlier for restoring ",
                                        filePath2, ", signaling this was a last chance restore");
                            }
                        }
                    }
                    else
                    {
                        // second file was OK, or no repair option, still need to process first file
                        bOK = TestSingleFile(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo, true, true, true);
                        if (!bOK && _bRepairFiles)
                        {
                            if (TestSingleFileHealthyOrCanRepair(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo))
                                bOK = TestAndRepairSingleFile(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo);

                            if (bOK && bForceCreateInfo)
                            {
                                CreateRestoreInfo(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                            }
                            bForceCreateInfo = false;

                            // if it didn't work, then try to repair using second file
                            if (!bOK)
                            {
                                bOK = TestSingleFile(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo, true, true, true);
                                if (!bOK && TestSingleFileHealthyOrCanRepair(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo))
                                    bOK = TestAndRepairSingleFile(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo);

                                if (bOK && bForceCreateInfo)
                                {
                                    bOK = CreateRestoreInfo(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                                    bForceCreateInfo = false;
                                }

                                if (bOK)
                                {
                                    if (fi2.LastWriteTime.Year > 1975)
                                        CopyFileSafely(fi2, filePath1, "(file was healthy, or repaired)");
                                    else
                                        WriteLog(0, "Warning: couldn't use outdated file ", filePath2, " with year 1975 or earlier for restoring ",
                                            filePath1, ", signaling this was a last chance restore");
                                }
                            }
                        }
                    }
                }
            }
        }

        bool CheckIfZeroLengthIsInteresting(string filePath)
        {
            return filePath.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".cr2", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".raf", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".mov", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".mpeg4", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".aac", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".avc", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".m2ts", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".heic", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".avi", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".wmv", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".jp2", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".wma", StringComparison.InvariantCultureIgnoreCase) ||
                   filePath.EndsWith(".flac", StringComparison.InvariantCultureIgnoreCase);
        }

        void ProcessFilePair_Bidirectionally(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // special case: both exist and both zero length
            if (fi1.Exists && fi2.Exists && fi1.Length == 0 && fi2.Length == 0)
            {
                if (CheckIfZeroLengthIsInteresting(filePath2))
                {
                    if (filePath1.Equals(filePath2, StringComparison.CurrentCultureIgnoreCase))
                        WriteLog(0, "Warning: file has zero length, indicating a failed copy operation in the past: ", filePath1);
                    else
                        WriteLog(0, "Warning: both files have zero length, indicating a failed copy operation in the past: ", filePath1, ", ", filePath2);
                }
            }
            else
                if (fi1.Exists && (!fi2.Exists || fi2.Length==0))
                    ProcessFilePair_Bidirectionally_FirstExists(filePath1, filePath2, fi1, fi2);            
                else
                {
                    if (fi2.Exists && (!fi1.Exists || fi1.Length==0))
                       ProcessFilePair_Bidirectionally_SecondExists(filePath1, filePath2, fi1, fi2);
                    else
                       ProcessFilePair_Bidirectionally_BothExist(filePath1, filePath2, fi1, fi2);
                }
        }

        void ProcessFilePair_Bidirectionally_FirstExists(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            bool bForceCreateInfo = false;
            bool bForceCreateInfo2 = false;
            bool bInTheEndOK = true;

            try
            {
                _copyFiles.WaitOne();

                if (_cancelClicked)
                    return;

                if (_bCreateInfo && (!fi1ri.Exists || fi1ri.LastWriteTimeUtc != fi1.LastWriteTimeUtc || bForceCreateInfo))
                {
                    if (!CreateRestoreInfoAndCopy(fi1.FullName, fi1ri.FullName, fi2.FullName, "(file was new)"))
                    {
                        CopyRepairSingleFile(filePath2, filePath1, fi1ri.FullName, ref bForceCreateInfo, ref bForceCreateInfo2, "(file was new)", false, false);
                        CreateRestoreInfo(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                        return;
                    }

                    fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    bForceCreateInfo = false;
                }
                else
                {
                    try
                    {
                        if (_bTestFiles)
                            CopyRepairSingleFile(filePath2, fi1.FullName, fi1ri.FullName, ref bForceCreateInfo, ref bForceCreateInfo2, "(file was new)", true, _bRepairFiles);
                        else
                            CopyFileSafely(fi1, filePath2, "(file was new)");
                    }
                    catch (Exception)
                    {
                        WriteLog(0, "Warning: Encountered error while copying ", fi1.FullName, ", trying to automatically repair");
                        if (_bTestFiles && _bRepairFiles)
                            TestAndRepairSingleFile(fi1.FullName, fi1ri.FullName, ref bForceCreateInfo);
                        if (bInTheEndOK)
                            bInTheEndOK = CopyRepairSingleFile(filePath2, fi1.FullName, fi1ri.FullName, ref bForceCreateInfo, ref bForceCreateInfo2, "(file was new)", false, _bTestFiles && _bRepairFiles);
                    }
                }


                if (bInTheEndOK)
                {
                    if (_bCreateInfo || fi1ri.Exists)
                    {
                        if (!fi1ri.Exists || bForceCreateInfo || fi1ri.LastWriteTimeUtc != fi1.LastWriteTimeUtc)
                        {
                            CreateRestoreInfo(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                            fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                        }

                        if (bForceCreateInfo2)
                            CreateRestoreInfo(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                        else
                            if (fi1ri.Exists)
                                fi1ri.CopyTo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                    }
                }
            }
            finally
            {
                _copyFiles.Release();
            }
        }

        void ProcessFilePair_Bidirectionally_SecondExists(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // symmetric situation
            ProcessFilePair_Bidirectionally_FirstExists(filePath2, filePath1, fi2, fi1);
        }

        void ProcessFilePair_Bidirectionally_BothExist(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // bidirectionally path
            if ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) || 
                (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
                ProcessFilePair_Bidirectionally_BothExist_FirstNewer(filePath1, filePath2, fi1, fi2, "(file newer or bigger)");
            else
            {
                // bidirectionally path
                if ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi2.LastWriteTimeUtc > fi1.LastWriteTimeUtc)) || 
                    (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi2.Length > fi1.Length))
                    ProcessFilePair_Bidirectionally_BothExist_SecondNewer(filePath1, filePath2, fi1, fi2);
                else
                    ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(filePath1, filePath2, fi1, fi2);
            }
        }

        void ProcessFilePair_Bidirectionally_BothExist_FirstNewer(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2, string strReason)
        {
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo1 = false;
            bool bForceCreateInfo2 = false;
            bool bCopied2To1 = false;
            bool bCopy2To1 = false;
            bool bCopied1To2 = true;

            try
            {
                _copyFiles.WaitOne();

                if (_cancelClicked)
                    return;

                if (_bCreateInfo && (!fi1ri.Exists || fi1ri.LastWriteTimeUtc != fi1.LastWriteTimeUtc || bForceCreateInfo1))
                {
                    bCopied1To2 = CreateRestoreInfoAndCopy(fi1.FullName, fi1ri.FullName, fi2.FullName, strReason);
                    fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

                    if (bCopied1To2)
                        bForceCreateInfo1 = false;
                }
                else
                {
                    try
                    {
                        if (_bTestFiles)
                            CopyRepairSingleFile(filePath2, fi1.FullName, fi1ri.FullName, ref bForceCreateInfo1, ref bForceCreateInfo2, "(file was new)", true, _bRepairFiles);
                        else
                            CopyFileSafely(fi1, filePath2, strReason);
                    }
                    catch (Exception)
                    {
                        bCopied1To2 = false;
                    }
                }

                if (!bCopied1To2)
                {
                    if (!_bTestFiles || !_bRepairFiles)
                    {
                        WriteLog(0, "Running without repair option, so couldn't decide, if the file can be restored ", fi1.FullName, ", ", fi2.FullName);
                        // first failed,  still need to test the second
                        if (_bTestFiles)
                        {
                            TestSingleFile(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo2, true
                              , !_bSkipRecentlyTested, true);
                        }
                        return;
                    }

                    // first try to copy the first/needed file
                    if (TestSingleFileHealthyOrCanRepair(filePath1, fi1ri.FullName, ref bForceCreateInfo1) &&
                        TestAndRepairSingleFile(filePath1, fi1ri.FullName, ref bForceCreateInfo1))
                    {
                        if (bForceCreateInfo1)
                        {
                            bCopied1To2 = CreateRestoreInfoAndCopy(fi1.FullName, fi1ri.FullName, fi2.FullName, strReason);
                            fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                            bForceCreateInfo1 = false;
                        }
                        else
                        {
                            CopyFileSafely(fi1, filePath2, strReason);
                            bCopied1To2 = true;
                        }
                    }

                    if (!bCopied1To2)
                    {
                        // well, then try the second, older file.
                        if ((TestSingleFile(filePath2, fi2ri.FullName, ref bForceCreateInfo2, true, true, true) ||
                                (TestSingleFileHealthyOrCanRepair(filePath2, fi2ri.FullName, ref bForceCreateInfo2) &&
                                 TestAndRepairSingleFile(filePath2, fi2ri.FullName, ref bForceCreateInfo2)))
                             && fi2.LastWriteTime.Year>1975)
                        {
                            WriteLog(0, "Warning: Encountered I/O error while copying ", fi1.FullName, ". The older file ", filePath2, " seems to be OK");
                            bCopied1To2 = false;
                            bCopy2To1 = true;
                        }
                        else
                        {
                            WriteLog(0, "Warning: Encountered I/O error while copying ", fi1.FullName, ". Other file has errors as well: ", filePath2, ", or is a product of last chance restore, trying to automatically repair ", filePath1);
                            TestAndRepairSingleFile(fi1.FullName, fi1ri.FullName, ref bForceCreateInfo1);
                            bForceCreateInfo2 = false;

                            CopyRepairSingleFile(filePath2, fi1.FullName, fi1ri.FullName, ref bForceCreateInfo1, ref bForceCreateInfo2, strReason, false, true);
                            bCopied1To2 = true;
                        }
                    }
                };

                if (bCopied1To2)
                {
                    if ((_bCreateInfo && (!fi1ri.Exists || fi1ri.LastWriteTimeUtc != fi1.LastWriteTimeUtc)) || (fi1ri.Exists && bForceCreateInfo1))
                    {
                        CreateRestoreInfo(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                        fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    }

                    if (fi1ri.Exists)
                    {
                        if (bForceCreateInfo2)
                        {
                            if (_bCreateInfo || fi2ri.Exists)
                                CreateRestoreInfo(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                        }
                        else
                            fi1ri.CopyTo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                    }

                    return;
                }


                if (!bCopy2To1)
                    return;

                if (!_bTestFiles || !_bRepairFiles)
                {
                    WriteLog(0, "Running without repair option, so couldn't decide, if the file can be restored ", fi1.FullName, ", ", fi2.FullName);
                    return;
                }

                // there we try to restore the older file 2, since it seems to be OK, while newer file 1 failed.
                fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                bForceCreateInfo2 = false;

                if (_bCreateInfo && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc != fi2.LastWriteTimeUtc || bForceCreateInfo2))
                {
                    bCopied2To1 = CreateRestoreInfoAndCopy(fi2.FullName, fi2ri.FullName, filePath1, "(file was healthy)");
                    fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

                    if (bCopied2To1)
                        bForceCreateInfo2 = false;
                    else
                    {
                        // should actually never happen, since we go there only if file 2 could be restored above
                        WriteLog(0, "Internal error: Couldn't restore any of the copies of the file ", fi1.FullName, ", ", fi2.FullName);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        CopyRepairSingleFile(filePath1, filePath2,fi2ri.FullName,ref bForceCreateInfo2, ref bForceCreateInfo1, "(file was healthy)", true, true);
                    }
                    catch (Exception)
                    {
                        // should actually never happen, since we go there only if file 2 could be restored above
                        WriteLog(0, "Internal error: Couldn't restore any of the copies of the file ", fi1.FullName, ", ", fi2.FullName);
                        return;
                    }
                }


                if ((_bCreateInfo && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc!=fi2ri.LastWriteTimeUtc)) || (fi2ri.Exists && bForceCreateInfo2))
                {
                    CreateRestoreInfo(filePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                if (fi2ri.Exists)
                {
                    if (bForceCreateInfo1)
                    {
                        if (_bCreateInfo || fi1ri.Exists)
                            CreateRestoreInfo(filePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    }
                    else
                        fi2ri.CopyTo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), true);
                }
            }
            finally
            {
                _copyFiles.Release();
            }
        }

        void ProcessFilePair_Bidirectionally_BothExist_SecondNewer(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            ProcessFilePair_Bidirectionally_BothExist_FirstNewer(filePath2, filePath1, fi2, fi1, "(file was newer or bigger)");
        }


        void ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(string filePath1, string filePath2, System.IO.FileInfo fi1, System.IO.FileInfo fi2)
        {
            // bidirectionally path
            // both files are present and have same modification date
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            //System.IO.FileInfo fi2ri = new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Path.Combine(fi2.DirectoryName, "RestoreInfo"), fi2.Name + ".chk"));
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the restoreinfo files is missing or has wrong date, but the oOtherSaveInfo is OK then copy the one to the oOtherSaveInfo
            if (fi1ri.Exists && fi1ri.LastWriteTimeUtc == fi1.LastWriteTimeUtc && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    _copyFiles.WaitOne();
                    fi1ri.CopyTo(fi2ri.FullName, true);
                }
                finally
                {
                    _copyFiles.Release();
                }

                fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            }
            else
                if (fi2ri.Exists && fi2ri.LastWriteTimeUtc == fi2.LastWriteTimeUtc && (!fi1ri.Exists || fi1ri.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        fi2ri.CopyTo(fi1ri.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }

                    fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                }


            bool bCreateInfo = false;
            bool bCreateInfo1 = false;
            bool bCreateInfo2 = false;
            if (_bTestFiles)
            {
                bool bFirstOrSecond;
                lock (this)
                {
                    bFirstOrSecond = _randomOrder = !_randomOrder;
                }

                if (_bRepairFiles)
                {
                    bool bTotalResultOk = true;
                    if (bFirstOrSecond)
                    {
                        bTotalResultOk = TestSingleFile2(fi1.FullName, fi1ri.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, true, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            bTotalResultOk = bTotalResultOk && TestSingleFile2(fi2.FullName, fi2ri.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, true, false);
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        bTotalResultOk = TestSingleFile2(fi2.FullName, fi2ri.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, true, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            bTotalResultOk = bTotalResultOk && TestSingleFile2(fi1.FullName, fi1ri.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, true, false);
                        else
                        {
                            bCreateInfo1 = bCreateInfo2;
                            bCreateInfo2 = false;
                        }
                    }

                    if (!bTotalResultOk)
                    {
                        TestAndRepairTwoFiles(fi1.FullName, fi2.FullName, fi1ri.FullName, fi2ri.FullName, ref bCreateInfo);
                        bCreateInfo1 = bCreateInfo;
                        bCreateInfo2 = bCreateInfo;
                    }
                }
                else
                {
                    if (bFirstOrSecond)
                    {
                        TestSingleFile2(fi1.FullName, fi1ri.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, false, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            TestSingleFile2(fi2.FullName, fi2ri.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, false, false);
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        TestSingleFile2(fi2.FullName, fi2ri.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, false, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            TestSingleFile2(fi1.FullName, fi1ri.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, false, false);
                        else
                            bCreateInfo1 = false;
                    }

                    //TestSingleFile(fi1.FullName, fi1ri.FullName, ref bCreateInfo1);
                    //TestSingleFile(fi2.FullName, fi2ri.FullName, ref bCreateInfo2);
                }
            }

            if (_bCreateInfo && (!fi1ri.Exists || fi1ri.LastWriteTimeUtc != fi1.LastWriteTimeUtc || bCreateInfo1))
            {
                CreateRestoreInfo(fi1.FullName, fi1ri.FullName);
                if (fi1ri.FullName.Equals(fi2ri.FullName, StringComparison.InvariantCultureIgnoreCase))
                    fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            }

            if (_bCreateInfo && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc != fi2.LastWriteTimeUtc || bCreateInfo2))
            {
                CreateRestoreInfo(fi2.FullName, fi2ri.FullName);
            }


            fi2ri = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            fi1ri = new System.IO.FileInfo(CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the files is missing or has wrong date, but the oOtherSaveInfo is OK then copy the one to the oOtherSaveInfo
            if (fi1ri.Exists && fi1ri.LastWriteTimeUtc == fi1.LastWriteTimeUtc && (!fi2ri.Exists || fi2ri.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    _copyFiles.WaitOne();
                    fi1ri.CopyTo(fi2ri.FullName, true);
                }
                finally
                {
                    _copyFiles.Release();
                }
            }
            else
                if (fi2ri.Exists && fi2ri.LastWriteTimeUtc == fi2.LastWriteTimeUtc && (!fi1ri.Exists || fi1ri.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        fi2ri.CopyTo(fi1ri.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }
                }
        }

        bool CreateRestoreInfoAndCopy(string pathFile, string pathRestoreInfoFile, string targetPath, string strReason)
        {
            string pathFileCopy = targetPath + ".tmp";


            System.IO.FileInfo finfo = new System.IO.FileInfo(pathFile);
            SaveInfo si = new SaveInfo(finfo.Length, finfo.LastWriteTimeUtc, false);
            try
            {
                using (System.IO.BufferedStream s = new System.IO.BufferedStream(System.IO.File.OpenRead(finfo.FullName), (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    try
                    {
                        using (System.IO.BufferedStream s2 = new System.IO.BufferedStream(System.IO.File.Create(pathFileCopy), (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                        {
                            Block b = Block.GetBlock();

                            for (int index = 0; ; index++)
                            {
                                for (int i = b.Length - 1; i >= 0; --i)
                                    b[i] = 0;

                                int readCount = 0;
                                if ((readCount = b.ReadFrom(s)) == b.Length)
                                {
                                    b.WriteTo(s2);
                                    si.AnalyzeForInfoCollection(b, index);
                                }
                                else
                                {
                                    b.WriteTo(s2, readCount);
                                    si.AnalyzeForInfoCollection(b, index);
                                    break;
                                }
                            };

                            Block.ReleaseBlock(b);

                            s2.Close();
                            System.IO.FileInfo fi2tmp = new System.IO.FileInfo(pathFileCopy);
                            fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                            System.IO.FileInfo fi2 = new System.IO.FileInfo(targetPath);
                            if (fi2.Exists)
                                fi2.Delete();

                            fi2tmp.MoveTo(targetPath);

                            WriteLog(0, "Copied ", pathFile, " to ", targetPath, " ", strReason);
                        }
                    } catch
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(5000);
                            System.IO.FileInfo finfoCopy = new System.IO.FileInfo(pathFileCopy);
                            if (finfoCopy.Exists)
                                finfoCopy.Delete();
                        }
                        catch
                        {
                            // ignore
                        }
                        throw;
                    }

                    s.Close();
                };
            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error while processing file: \"", finfo.FullName, "\": " + ex.Message);
                return false;
            }

            try
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(pathRestoreInfoFile.Substring(0, pathRestoreInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                if (!di.Exists)
                {
                    di.Create();
                    di = new System.IO.DirectoryInfo(pathRestoreInfoFile.Substring(0, pathRestoreInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden;
                }
                using (System.IO.FileStream s = System.IO.File.Create(pathRestoreInfoFile))
                {
                    si.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                System.IO.FileInfo firi = new System.IO.FileInfo(pathRestoreInfoFile);
                firi.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error writing file: \"", pathRestoreInfoFile, "\": " + ex.Message);
                return false;
            }

            // we just created the file, so assume we checked everything, no need to double-check immediately
            CreateOrUpdateFileChecked(pathRestoreInfoFile);

            return true;
        }

        bool CreateRestoreInfo(string pathFile, string pathRestoreInfoFile)
        {
            System.IO.FileInfo finfo = new System.IO.FileInfo(pathFile);
            SaveInfo si = new SaveInfo(finfo.Length, finfo.LastWriteTimeUtc, false);
            try
            {
                using (System.IO.BufferedStream s = new System.IO.BufferedStream(System.IO.File.OpenRead(finfo.FullName), (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    Block b = Block.GetBlock();

                    for (int index = 0; ; index++)
                    {
                        for (int i = b.Length - 1; i >= 0; --i)
                            b[i] = 0;

                        if (b.ReadFrom(s) == b.Length)
                        {
                            si.AnalyzeForInfoCollection(b, index);
                        }
                        else
                        {
                            si.AnalyzeForInfoCollection(b, index);
                            break;
                        }
                        if (_cancelClicked)
                            throw new OperationCanceledException();
 
                    };

                    Block.ReleaseBlock(b);

                    s.Close();
                };
            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error reading file: \"", finfo.FullName, "\": " + ex.Message);
                return false;
            }

            try
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(pathRestoreInfoFile.Substring(0, pathRestoreInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                if (!di.Exists)
                {
                    di.Create();
                    di = new System.IO.DirectoryInfo(pathRestoreInfoFile.Substring(0, pathRestoreInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden;
                }

                System.IO.FileInfo firi = new System.IO.FileInfo(pathRestoreInfoFile);
                if (firi.Exists)
                {
                    firi.Delete();
                }

                using (System.IO.FileStream s = System.IO.File.Create(pathRestoreInfoFile, 1024*1024))
                {
                    si.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                firi = new System.IO.FileInfo(pathRestoreInfoFile);
                firi.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                CreateOrUpdateFileChecked(pathRestoreInfoFile);

            } catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error writing file: \"", pathRestoreInfoFile, "\": " + ex.Message);
                return false;
            }
            return true;
        }

        bool TestSingleFile(string pathFile, string pathRestoreInfoFile, ref bool bForceCreateInfo, bool needsMessageAboutOldRestoreInfo, bool bForcePhysicalTest, bool bCreateConfirmationFile)
        {
            return TestSingleFile2(pathFile, pathRestoreInfoFile, ref bForceCreateInfo, needsMessageAboutOldRestoreInfo, bForcePhysicalTest, bCreateConfirmationFile, false, false);
        }

        bool TestSingleFile2(string pathFile, string pathRestoreInfoFile, ref bool bForceCreateInfo, 
            bool needsMessageAboutOldRestoreInfo, bool bForcePhysicalTest, bool bCreateConfirmationFile,
            bool bFailASAPwoMessage, bool bReturnFalseIfNonRecoverableNotIfDamaged)
        {
            System.IO.FileInfo finfo = new System.IO.FileInfo(pathFile);
            System.IO.FileInfo firi = new System.IO.FileInfo(pathRestoreInfoFile);
            bool bSkipBufferedFile = false;

            try
            {
                if (!bForcePhysicalTest)
                {
                    System.IO.FileInfo fichecked = new System.IO.FileInfo(pathRestoreInfoFile + "ed");
                    // this randomly skips testing of files,
                    // so the user doesn't have to wait long, when performing checks annually:
                    // 100% of files are skipped within first 2 years after last check
                    // 0% after 7 years after last check
                    if (fichecked.Exists && finfo.Exists &&
                        (!firi.Exists || firi.LastWriteTimeUtc == finfo.LastWriteTimeUtc) &&
                        fichecked.LastWriteTimeUtc.CompareTo(finfo.LastWriteTimeUtc) > 0 &&
                        Math.Abs(DateTime.UtcNow.Year * 366 + DateTime.UtcNow.DayOfYear - fichecked.LastWriteTimeUtc.Year * 366 - fichecked.LastWriteTimeUtc.DayOfYear) < 366 * 2.2 + 366 * 4.6 * _randomizeChecked.NextDouble())
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(1, "Warning: ", ex.Message, " while discovering, if ", pathFile, " needs to be rechecked.");
            }

        repeat:
            SaveInfo si = new SaveInfo();
            bool bSaveInfoUnreadable = !firi.Exists;
            if (!bSaveInfoUnreadable)
            {
                try
                {
                    using (System.IO.BufferedStream s = new System.IO.BufferedStream(System.IO.File.OpenRead(pathRestoreInfoFile), (int)Math.Min(firi.Length + 1, 32 * 1024 * 1024)))
                    {
                        si.ReadFrom(s);
                        s.Close();
                    }
                }
                catch // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (System.IO.FileStream s = System.IO.File.OpenRead(pathRestoreInfoFile))
                        {
                            si.ReadFrom(s);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        WriteLog(0, "I/O Error reading file: \"", pathRestoreInfoFile, "\": " + ex.Message);
                        bSaveInfoUnreadable = true;
                        bForceCreateInfo = true;
                        bForcePhysicalTest = true;
                    }
                }
            }

            /*
            if (!bForcePhysicalTest)
            {
                if (!bSkipBufferedFile && fichecked.Exists && finfo.Exists &&
                    (!firi.Exists || firi.LastWriteTimeUtc == finfo.LastWriteTimeUtc) &&
                    fichecked.LastWriteTimeUtc.CompareTo(finfo.LastWriteTimeUtc) > 0 &&
                    Math.Abs(DateTime.UtcNow.Year * 366 + DateTime.UtcNow.DayOfYear - fichecked.LastWriteTimeUtc.Year * 366 - fichecked.LastWriteTimeUtc.DayOfYear) < 366 * 2.2 + 366 * 4.6 * _randomizeChecked.NextDouble())
                {
                    return true;
                }
            }
             */

            if (bSaveInfoUnreadable || si.Length != finfo.Length || !FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc) /*si.TimeStamp != finfo.LastWriteTimeUtc*/)
            {
                bool bAllBlocksOK = true;

                bForceCreateInfo = true;
                if (!bSaveInfoUnreadable)
                    if (needsMessageAboutOldRestoreInfo)
                        WriteLog(0, "RestoreInfo file \"", pathRestoreInfoFile, "\" can't be used for testing file \"", pathFile, "\": it was created for another version of the file");

                Block b = Block.GetBlock();
                try
                {
                    using (System.IO.BufferedStream s = new System.IO.BufferedStream(System.IO.File.OpenRead(finfo.FullName), (int)Math.Min(finfo.Length + 1, 32 * 1024 * 1024)))
                    {
                        for (int index = 0; ; index++)
                        {
                            if (b.ReadFrom(s) != b.Length)
                                break;
                        }
                        s.Close();
                    }
                }
                catch // in case there are any errors simply switch to unbuffered, so we have authentic results
                {
                    if (bFailASAPwoMessage)
                        return false;

                    if (bReturnFalseIfNonRecoverableNotIfDamaged)
                        return false;

                    using (System.IO.FileStream s = System.IO.File.OpenRead(finfo.FullName))
                    {
                        for (int index = 0; ; index++)
                        {
                            try
                            {
                                if (b.ReadFrom(s) != b.Length)
                                    break;
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLog(0, "I/O Error reading file: \"", finfo.FullName, "\", offset ", index * b.Length, ": " + ex.Message);
                                s.Seek((index + 1) * b.Length, System.IO.SeekOrigin.Begin);
                                bAllBlocksOK = false;
                            }
                        }
                        s.Close();
                    }
                }
                Block.ReleaseBlock(b);

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    CreateOrUpdateFileChecked(pathRestoreInfoFile);
                }

                return bAllBlocksOK;
            }


            try
            {
                long nonRestoredSize = 0;
                bool bAllBlocksOK = true;

                System.IO.Stream s = System.IO.File.OpenRead(finfo.FullName);
                if (!bSkipBufferedFile)
                    s = new System.IO.BufferedStream(s, (int)Math.Min(finfo.Length + 1, 8 * 1024 * 1024));

                using (s)
                {
                    si.StartRestore();
                    Block b = Block.GetBlock();
                    for (int index = 0; ; index++)
                    {


                        try
                        {
                            bool bBlockOk = true;
                            int nRead = 0;
                            if ( (nRead = b.ReadFrom(s)) == b.Length)
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    if (bFailASAPwoMessage)
                                        return false;

                                    WriteLog(1, finfo.FullName, ": checksum of block at offset ", index * b.Length, " not OK");
                                    bAllBlocksOK = false;
                                }
                            }
                            else
                            {
                                if (nRead > 0)
                                {
                                    while (nRead < b.Length)
                                        b[nRead++] = 0;

                                    bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                    if (!bBlockOk)
                                    {
                                        if (bFailASAPwoMessage)
                                            return false;

                                        WriteLog(1, finfo.FullName, ": checksum of block at offset ", index * b.Length, " not OK");
                                        bAllBlocksOK = false;
                                    }
                                }
                                break;
                            }

                            if (_cancelClicked)
                                throw new OperationCanceledException();
                        }
                        catch (System.IO.IOException ex)
                        {
                            if (bFailASAPwoMessage)
                                return false;

                            if (!bSkipBufferedFile)
                            {
                                // we need to re-read saveinfo
                                bSkipBufferedFile = true;
                                if (!_cancelClicked)
                                    goto repeat;
                                else
                                    throw;
                            }

                            WriteLog(1, "I/O Error reading file: \"", finfo.FullName, "\", offset ", index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length, System.IO.SeekOrigin.Begin);
                        }
                    };
                    Block.ReleaseBlock(b);

                    List<RestoreInfo> ri = si.EndRestore(out nonRestoredSize, firi.FullName, this);
                    if (ri.Count > 1)
                        WriteLog(0, "There are ", ri.Count, " bad blocks in the file ", finfo.FullName, ", non-restorable parts: ", nonRestoredSize, " bytes, file remains unchanged, it was only tested");
                    else
                        if (ri.Count > 0)
                        WriteLog(0, "There is one bad block in the file ", finfo.FullName, ", non-restorable parts: ", nonRestoredSize, " bytes, file remains unchanged, it was only tested");

                    s.Close();
                };

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        if (needsMessageAboutOldRestoreInfo)
                            WriteLog(0, "RestoreInfo file \"", pathRestoreInfoFile, "\" has been damaged and needs to be recreated from \"", pathFile,"\"");
                        bForceCreateInfo = true;
                    }
                }

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    CreateOrUpdateFileChecked(pathRestoreInfoFile);
                }

                if (bReturnFalseIfNonRecoverableNotIfDamaged)
                    return nonRestoredSize == 0;
                else
                    return bAllBlocksOK;
            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error reading file: \"", finfo.FullName, "\": " + ex.Message);
                return false;
            }
        }

        void CreateOrUpdateFileChecked(string pathRestoreInfoFile)
        {
            string strPath = pathRestoreInfoFile + "ed";

            try
            {

                if (System.IO.File.Exists(strPath))
                    System.IO.File.SetLastWriteTimeUtc(strPath, DateTime.UtcNow);
                else
                    using (System.IO.Stream s = System.IO.File.OpenWrite(strPath)) { s.Close(); };

                System.IO.File.SetAttributes(strPath, System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System);
            }
            catch (Exception ex)
            {
                WriteLog(1, "Warning: ", ex.Message, " while creating ", strPath);
            }
        }

        bool TestAndRepairSingleFile(string pathFile, string pathRestoreInfoFile, ref bool bForceCreateInfo)
        {
            System.IO.FileInfo finfo = new System.IO.FileInfo(pathFile);
            System.IO.FileInfo firi = new System.IO.FileInfo(pathRestoreInfoFile);

            SaveInfo si = new SaveInfo();
            bool bNotReadableSi = !firi.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (System.IO.BufferedStream s = new System.IO.BufferedStream(System.IO.File.OpenRead(pathRestoreInfoFile), (int)Math.Min(firi.Length + 1, 8 * 1024 * 1024)))
                    {
                        si.ReadFrom(s);
                        s.Close();
                    }
                }
                catch // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (System.IO.FileStream s = System.IO.File.OpenRead(pathRestoreInfoFile))
                        {
                            si.ReadFrom(s);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        WriteLog(0, "I/O Error reading file: \"", pathRestoreInfoFile, "\": " + ex.Message);
                        bNotReadableSi = true;
                    }
                }
            }

            if (bNotReadableSi || si.Length != finfo.Length || !FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc) /* si.TimeStamp != finfo.LastWriteTimeUtc*/)
            {
                bool bAllBlocksOk = true;
                if (firi.Exists)
                    bForceCreateInfo = true;

                if (!bNotReadableSi)
                    WriteLog(0, "RestoreInfo file \"", pathRestoreInfoFile, "\" can't be used for testing file \"", pathFile, "\": it was created for another version of the file");

                using (System.IO.FileStream s = System.IO.File.Open(finfo.FullName, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {
                    Block b = Block.GetBlock();
                    for (long index = 0; ; index++)
                    {
                        try
                        {
                            // we simply read to end, no need in content
                            if (b.ReadFrom(s) != b.Length)
                                break;
                        }
                        catch (System.IO.IOException ex)
                        {
                            // fill bad block with zeros
                            for (int i = b.Length - 1; i >= 0; --i)
                                b[i] = 0;

                            WriteLog(0, "Error while reading file ", finfo.FullName, " position ", index * b.Length, ": ", ex.Message, ". Block will be filled with a dummy");
                            s.Seek(index * b.Length, System.IO.SeekOrigin.Begin);
                            int lengthToWrite = (int)(finfo.Length - index * b.Length > b.Length ? b.Length : finfo.Length - index * b.Length);
                            if (lengthToWrite>0)
                                b.WriteTo(s, lengthToWrite);
                            bAllBlocksOk = false;
                        }
                    }
                    Block.ReleaseBlock(b);

                    s.Close();
                }

                if (bAllBlocksOk)
                {
                    CreateOrUpdateFileChecked(pathRestoreInfoFile);
                }

                return bAllBlocksOk;
            }

            System.DateTime prevLastWriteTime = finfo.LastWriteTimeUtc;

            Dictionary<long, bool> readableButNotAccepted = new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;
                using (System.IO.FileStream s = System.IO.File.OpenRead(finfo.FullName))
                {
                    si.StartRestore();
                    Block b = Block.GetBlock();
                    for (long index = 0; ; index++)
                    {
                        for (int i = b.Length-1; i >= 0; --i)
                            b[i] = 0;

                        try
                        {
                            bool bBlockOk = true;
                            if (b.ReadFrom(s) == b.Length)
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    bAllBlocksOK = false;
                                    WriteLog(1,finfo.FullName, ": checksum of block at offset ", index * b.Length, " not OK");
                                    readableButNotAccepted[index] = true;
                                }
                            }
                            else
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    bAllBlocksOK = false;
                                    WriteLog(1,finfo.FullName, ": checksum of block at offset ", index * b.Length, " not OK");
                                    readableButNotAccepted[index] = true;
                                }
                                break;
                            }

                            if (_cancelClicked)
                                throw new OperationCanceledException();

                        }
                        catch (System.IO.IOException ex)
                        {
                            bAllBlocksOK = false;
                            WriteLog(1,"I/O Error reading file: \"", finfo.FullName, "\", offset ", index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length, System.IO.SeekOrigin.Begin);
                        }

                    };

                    Block.ReleaseBlock(b);

                    s.Close();
                };

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        WriteLog(0, "RestoreInfo file \"", pathRestoreInfoFile, "\" has been damaged and needs to be recreated from \"", pathFile, "\"");
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        CreateOrUpdateFileChecked(pathRestoreInfoFile);
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0,"I/O Error reading file: \"", finfo.FullName, "\": " + ex.Message);
                return false;
            }

            try
            { 
                long nonRestoredSize = 0;
                List<RestoreInfo> rinfos = si.EndRestore(out nonRestoredSize, firi.FullName, this);
                if (nonRestoredSize > 0)
                    bForceCreateInfo = true;

                using (System.IO.FileStream s = System.IO.File.OpenWrite(finfo.FullName))
                {
                    foreach (RestoreInfo ri in rinfos)
                    {
                        if (ri.NotRecoverableArea)
                        {
                            if (readableButNotAccepted.ContainsKey(ri.Position / ri.Data.Length))
                                WriteLog(1, "Keeping readable but not recoverable block at offset ", ri.Position, ", checksum indicates the block is wrong");
                            else
                            {
                                s.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                WriteLog(1, "Filling not recoverable block at offset ", ri.Position, " with a dummy block");
                                int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ? ri.Data.Length : si.Length - ri.Position);
                                if (lengthToWrite > 0)
                                    ri.Data.WriteTo(s,lengthToWrite);
                            }
                            bForceCreateInfo = true;
                        }
                        else
                        {
                            s.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                            WriteLog(1, "Recovering block at offset ", ri.Position, " of the file ", finfo.FullName);
                            int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ? ri.Data.Length : si.Length - ri.Position);
                            if (lengthToWrite > 0)
                                ri.Data.WriteTo(s, lengthToWrite);
                        }
                    }

                    s.Close();
                }

                if (rinfos.Count > 1)
                    WriteLog(0, "There were ", rinfos.Count, " bad blocks in the file ", finfo.FullName, ", not restored parts: ", nonRestoredSize, " bytes");
                else
                    if (rinfos.Count > 0)
                    WriteLog(0, "There was one bad block in the file ", finfo.FullName, ", not restored parts: ", nonRestoredSize, " bytes");

                if (nonRestoredSize == 0 && rinfos.Count == 0)
                {
                    CreateOrUpdateFileChecked(pathRestoreInfoFile);
                }

                if (nonRestoredSize > 0)
                {
                    int countErrors = (int)(nonRestoredSize / (Block.GetBlock().Length));
                    finfo.LastWriteTime = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                    bForceCreateInfo = true;
                } else
                    finfo.LastWriteTimeUtc = prevLastWriteTime;

                return nonRestoredSize == 0;
            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error writing file: \"", finfo.FullName, "\": " + ex.Message);
                finfo.LastWriteTimeUtc = prevLastWriteTime;
                return false;
            }
        }


        bool TestSingleFileHealthyOrCanRepair(string pathFile, string pathRestoreInfoFile, ref bool bForceCreateInfo)
        {
            return TestSingleFile2(pathFile, pathRestoreInfoFile, ref bForceCreateInfo, true, true, true, false, true);
        }

        void TestAndRepairTwoFiles(string path1, string path2, string path1ri, string path2ri, ref bool bForceCreateInfo)
        {
            System.IO.FileInfo fi1 = new System.IO.FileInfo(path1);
            System.IO.FileInfo fi2 = new System.IO.FileInfo(path2);
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(path1ri);
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(path2ri);
            System.IO.FileInfo fi1checked = new System.IO.FileInfo(path1ri + "ed");
            System.IO.FileInfo fi2checked = new System.IO.FileInfo(path2ri + "ed");

            SaveInfo si1 = new SaveInfo();
            SaveInfo si2 = new SaveInfo();

            bool bSaveInfo1Present = false;
            if (fi1ri.Exists && fi1ri.LastWriteTimeUtc == fi1.LastWriteTimeUtc)
            {
                using (System.IO.Stream s = System.IO.File.OpenRead(fi1ri.FullName))
                {
                    si1.ReadFrom(s);
                    bSaveInfo1Present = si1.Length==fi1.Length && FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc) /*si1.TimeStamp==fi1.LastWriteTimeUtc*/;
                    if (!bSaveInfo1Present)
                    {
                        si1 = new SaveInfo();
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        s.Seek(0, System.IO.SeekOrigin.Begin);
                        si2.ReadFrom(s);
                    }
                    s.Close();
                }
            }

            if (fi2ri.Exists && fi2ri.LastWriteTimeUtc == fi2.LastWriteTimeUtc)
            {
                using (System.IO.Stream s = System.IO.File.OpenRead(fi2ri.FullName))
                {
                    SaveInfo si3 = new SaveInfo();
                    si3.ReadFrom(s);
                    if (si3.Length == fi2.Length && FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc) /*si3.TimeStamp == fi2.LastWriteTimeUtc*/)
                    {
                        si2 = si3;
                        if (!bSaveInfo1Present)
                        {
                            s.Seek(0, System.IO.SeekOrigin.Begin);
                            si1.ReadFrom(s);
                            bSaveInfo1Present = true;
                        }
                    }
                    else
                        bForceCreateInfo = true;
                    s.Close();
                }
            }


            if (bSaveInfo1Present)
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // improve the available saved infos, if needed 
                si1.ImproveWith(si2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> equalBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                using (System.IO.Stream s1 = System.IO.File.OpenRead(path1))
                {
                    using (System.IO.Stream s2 = System.IO.File.OpenRead(path2))
                    {
                        si1.StartRestore();
                        si2.StartRestore();

                        Block b1 = Block.GetBlock();
                        Block b2 = Block.GetBlock();

                        for (int index = 0; ; ++index)
                        {
                            for (int i=b1.Length-1;i>=0;--i)
                            {
                                b1[i] = 0;
                                b2[i] = 0;
                            }

                            bool b1Present = false;
                            bool b1Ok = false;
                            bool s1Continue = false;
                            try
                            {
                                int nRead = 0;
                                if ((nRead = b1.ReadFrom(s1)) == b1.Length)
                                {
                                    b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                    s1Continue = true;
                                    readableBlocks1[index] = true;
                                    b1Present = true;
                                }
                                else
                                {
                                    if (nRead > 0)
                                    {
                                        // fill the rest with zeros
                                        while (nRead < b1.Length)
                                            b1[nRead++] = 0;

                                        b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                        readableBlocks1[index] = true;
                                        b1Present = true;
                                    }
                                }

                                if (!b1Ok)
                                {
                                    WriteLog(2, path1, ": checksum of block at offset ", index * b1.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLog(2, "I/O exception while reading file \"", path1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, System.IO.SeekOrigin.Begin);
                            }

                            bool b2Present = false;
                            bool b2Ok = false;
                            bool s2Continue = false;
                            try
                            {
                                int nRead = 0;
                                if ((nRead = b2.ReadFrom(s2)) == b2.Length)
                                {
                                    b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                    s2Continue = true;
                                    readableBlocks2[index] = true;
                                    b2Present = true;
                                }
                                else
                                {
                                    if (nRead > 0)
                                    {
                                        // fill the rest with zeros
                                        while (nRead < b2.Length)
                                            b2[nRead++] = 0;
                                        b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                        readableBlocks2[index] = true;
                                        b2Present = true;
                                    }
                                }

                                if (!b2Ok)
                                {
                                    WriteLog(2, path2, ": checksum of block at offset ", index * b2.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLog(2, "I/O exception while reading file \"", path2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                if (si2.AnalyzeForTestOrRestore(b1, index))
                                {
                                    WriteLog(1, "Block of ", fi2.FullName, " position ", index * b1.Length, " will be restored from ", fi1.FullName);
                                    restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                }
                            }
                            else
                                if (b2Present && !b1Present)
                            {
                                if (si1.AnalyzeForTestOrRestore(b2, index))
                                {
                                    WriteLog(1, "Block of ", fi1.FullName, " position ", index * b1.Length, " will be restored from ", fi2.FullName);
                                    restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                }
                            }
                            else
                            {
                                if (b2Present && !b1Ok)
                                {
                                    if (si1.AnalyzeForTestOrRestore(b2, index))
                                    {
                                        WriteLog(1, "Block of ", fi1.FullName, " position ", index * b1.Length, " will be restored from ", fi2.FullName);
                                        restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    }
                                };

                                if (b1Present && !b2Ok)
                                {
                                    if (si2.AnalyzeForTestOrRestore(b1, index))
                                    {
                                        WriteLog(1, "Block of ", fi2.FullName, " position ", index * b1.Length, " will be restored from ", fi1.FullName);
                                        restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                    }
                                }
                            }

                            if (b1Present && b2Present)
                            {
                                // if both blocks are present we'll compare their contents
                                // equal blocks have higher priority compared to their checksums and saved infos
                                bool bDifferent = false;
                                for (int i=b1.Length-1;i>=0;--i)
                                    if (b1[i]!=b2[i])
                                    {
                                        bDifferent = true;
                                        break;
                                    }

                                if (!bDifferent)
                                {
                                    equalBlocks[index] = true;
                                }
                            }

                            if (!s1Continue && !s2Continue)
                                break;

                            if (_cancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();

                        Block.ReleaseBlock(b1);
                        Block.ReleaseBlock(b2);
                    }
                    s1.Close();


                }

                long notRestoredSize1 = 0;
                restore1.AddRange(si1.EndRestore(out notRestoredSize1, fi1ri.FullName, this));
                long notRestoredSize2 = 0;
                restore2.AddRange(si2.EndRestore(out notRestoredSize2, fi2ri.FullName, this));

                // now we've got the list of improvements for both files
                using (System.IO.Stream s1 = System.IO.File.Open(path1, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {

                    using (System.IO.Stream s2 = System.IO.File.Open(path2, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {
                        // let'oInputStream apply improvements of one file to the list of the oOtherSaveInfo, whenever possible
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            foreach (RestoreInfo ri2 in restore2)
                            {
                                if (ri2.Position == ri1.Position)
                                    if (ri2.NotRecoverableArea && !ri1.NotRecoverableArea)
                                    {
                                        WriteLog(1, "Block of ", fi2.FullName, " position ", ri2.Position, " will be restored from ", fi1.FullName);
                                        ri2.Data = ri1.Data;
                                        ri2.NotRecoverableArea = false;
                                    }
                                    else
                                        if (ri1.NotRecoverableArea && !ri2.NotRecoverableArea)
                                    {
                                        WriteLog(1, "Block of ", fi1.FullName, " position ", ri1.Position, " will be restored from ", fi2.FullName);
                                        ri1.Data = ri2.Data;
                                        ri1.NotRecoverableArea = false;
                                    }
                            }
                        }


                        // let'oInputStream apply the definitive improvements
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea || (_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length)))
                                ;// bForceCreateInfo = true;
                            else
                            {
                                WriteLog(1, "Recovering block of ", fi1.FullName, " at position ", ri1.Position);
                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ? ri1.Data.Length : si1.Length - ri1.Position);
                                if (lengthToWrite > 0)
                                    ri1.Data.WriteTo(s1, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks1[ri1.Position / ri1.Data.Length] = true;
                            }
                        };


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea || (_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
                                ; // bForceCreateInfo = true;
                            else
                            {
                                WriteLog(1, "Recovering block of ", fi2.FullName, " at position ", ri2.Position);
                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? ri2.Data.Length : si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        };



                        // let'oInputStream try to copy non-recoverable blocks from one file to another, whenever possible
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea && !equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length) && 
                                readableBlocks2.ContainsKey(ri1.Position/ri1.Data.Length) && 
                                !readableBlocks1.ContainsKey(ri1.Position/ri1.Data.Length) )
                            {
                                WriteLog(1, "Block of ", fi1.FullName, " position ", ri1.Position, " will be copied from ", fi2.FullName, " even if checksum indicates the block is wrong");

                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                Block temp = Block.GetBlock();
                                int length = temp.ReadFrom(s2);
                                temp.WriteTo(s1, length);
                                Block.ReleaseBlock(temp);
                                readableBlocks1[ri1.Position / ri1.Data.Length] = true;
                            }
                        };


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea && !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                               readableBlocks1.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length) )
 
                            {
                                WriteLog(1, "Block of ", fi2.FullName, " position ", ri2.Position, " will be copied from ", fi1.FullName, " even if checksum indicates the block is wrong");

                                s1.Seek(ri2.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                Block temp = Block.GetBlock();
                                int length = temp.ReadFrom(s1);
                                temp.WriteTo(s2, length);
                                Block.ReleaseBlock(temp);
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        };

                        // after all fill non-readable blocks with zeroes
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea && !equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length) &&
                                !readableBlocks1.ContainsKey(ri1.Position / ri1.Data.Length))
                            {
                                WriteLog(1, "Block of ", fi1.FullName, " position ", ri1.Position, " is not recoverable and will be filled with a dummy");

                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ? ri1.Data.Length : si1.Length - ri1.Position);
                                if (lengthToWrite > 0)
                                    ri1.Data.WriteTo(s1, lengthToWrite);
                            }
                        };


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea && !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                WriteLog(1, "Block of ", fi2.FullName, " position ", ri2.Position, " is not recoverable and will be filled with a dummy");

                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? ri2.Data.Length : si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                            }
                        };




                        s2.Close();
                    }
                    s1.Close();
                }

                if (restore1.Count>0)
                    WriteLog(0, "There were ", restore1.Count, " bad blocks in ", fi1.FullName, " not restored bytes: ", notRestoredSize1);
                if (restore2.Count>0)
                    WriteLog(0, "There were ", restore2.Count, " bad blocks in ", fi2.FullName, " not restored bytes: ", notRestoredSize2);

                fi1.LastWriteTimeUtc = prevLastWriteTime;
                fi2.LastWriteTimeUtc = prevLastWriteTime;

                if (notRestoredSize1 == 0 && restore1.Count==0)
                {
                    if (fi1checked.Exists)
                        fi1checked.LastWriteTimeUtc = DateTime.UtcNow;
                    else
                        using (System.IO.Stream s = System.IO.File.OpenWrite(fi1checked.FullName)) { s.Close(); };
                }

                if (notRestoredSize2 == 0 && restore2.Count==0)
                {
                    if (fi2checked.Exists)
                        fi2checked.LastWriteTimeUtc = DateTime.UtcNow;
                    else
                        using (System.IO.Stream s = System.IO.File.OpenWrite(fi2checked.FullName)) { s.Close(); };
                }

            }
            else
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // let'oInputStream read both copies of the file that obviously are both present, without saved info
                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                long notRestoredSize1 = 0;
                long notRestoredSize2 = 0;
                long badBlocks1 = 0;
                long badBlocks2 = 0;
                using (System.IO.Stream s1 = System.IO.File.OpenRead(path1))
                {
                    using (System.IO.Stream s2 = System.IO.File.OpenRead(path2))
                    {
                        for (int index = 0; ; ++index)
                        {
                            Block b1 = Block.GetBlock();
                            Block b2 = Block.GetBlock();

                            bool b1Present = false;
                            bool s1Continue = false;
                            try
                            {
                                if (b1.ReadFrom(s1) == b1.Length)
                                    s1Continue = true;
                                b1Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLog(2, "I/O exception while reading file \"", path1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, System.IO.SeekOrigin.Begin);
                                ++badBlocks1;
                            }

                            bool b2Present = false;
                            bool s2Continue = false;
                            try
                            {
                                if (b2.ReadFrom(s2) == b2.Length)
                                    s2Continue = true;
                                b2Present = true;
                                ++badBlocks2;
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLog(2, "I/O exception while reading file \"", path2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                WriteLog(1, "Block of ", fi2.FullName, " position ", index * b1.Length, " will be restored from ", fi1.FullName);
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                            }
                            else
                                if (b2Present && !b1Present)
                            {
                                WriteLog(1, "Block of ", fi1.FullName, " position ", index * b1.Length, " will be restored from ", fi2.FullName);
                                restore1.Add(new RestoreInfo(index * b2.Length, b2, false));
                            }
                            else
                                if (!b1Present && !b2Present)
                            {
                                Block b = Block.GetBlock();
                                WriteLog(1, "Blocks of ", fi1.FullName, " and ", fi2.FullName, " at position ", index * b1.Length, " are not recoverable and will be filled with a dummy block");
                                restore1.Add(new RestoreInfo(index * b1.Length, b1, true));
                                restore2.Add(new RestoreInfo(index * b2.Length, b2, true));
                                notRestoredSize1 += b1.Length;
                                notRestoredSize2 += b2.Length;
                                Block.ReleaseBlock(b);
                            }

                            if (!s1Continue && !s2Continue)
                                break;

                            if (_cancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }
                    s1.Close();
                }


                // now we've got the list of improvements for both files
                using (System.IO.Stream s1 = System.IO.File.Open(path1, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo ri1 in restore1)
                    {
                        s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ? ri1.Data.Length : si1.Length - ri1.Position);
                        if (lengthToWrite > 0)
                            ri1.Data.WriteTo(s1, lengthToWrite);
                    };
                };
                fi1.LastWriteTimeUtc = prevLastWriteTime;

                if (badBlocks1>0)
                    WriteLog(0, "There were ", badBlocks1, " bad blocks in ", fi1.FullName, " not restored bytes: ", notRestoredSize1);


                using (System.IO.Stream s2 = System.IO.File.Open(path2, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo ri2 in restore2)
                    {
                        s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? ri2.Data.Length : si2.Length - ri2.Position);
                        if (lengthToWrite > 0)
                            ri2.Data.WriteTo(s2, lengthToWrite);
                    };
                }
                fi2.LastWriteTimeUtc = prevLastWriteTime;
                if (badBlocks2>0)
                    WriteLog(0, "There were ", badBlocks2, " bad blocks in ", fi2.FullName, " not restored bytes: ", notRestoredSize2);

                if (notRestoredSize1 == 0 && restore1.Count==0)
                {
                    if (fi1checked.Exists)
                        fi1checked.LastWriteTimeUtc = DateTime.UtcNow;
                    else
                        using (System.IO.Stream s = System.IO.File.OpenWrite(fi1checked.FullName)) { s.Close(); };
                }

                if (notRestoredSize2 == 0 && restore2.Count==0)
                {
                    if (fi2checked.Exists)
                        fi2checked.LastWriteTimeUtc = DateTime.UtcNow;
                    else
                        using (System.IO.Stream s = System.IO.File.OpenWrite(fi2checked.FullName)) { s.Close(); };
                }

            }
        }


        bool CopyRepairSingleFile(string pathTargetFile, string pathFile, string pathRestoreInfoFile, ref bool bForceCreateInfo, ref bool bForceCreateInfoTarget, string strReason, bool bFailOnNonRecoverable, bool bApplyRepairsToSrc)
        {
            // if same file then try to repair in place
            if (string.Equals(pathTargetFile, pathFile, StringComparison.InvariantCultureIgnoreCase))
                if (_bRepairFiles)
                    return TestAndRepairSingleFile(pathFile, pathRestoreInfoFile, ref bForceCreateInfo);
                else
                {
                    if (_bTestFiles)
                    {
                        if (TestSingleFile2(pathFile, pathRestoreInfoFile, ref bForceCreateInfo, false, true, true, true, false))
                        {
                            return true;
                        }
                        else
                        {
                            WriteLog(1, "Error while testing file ", pathFile);
                            if (bFailOnNonRecoverable)
                                throw new Exception("Error while testing file " + pathFile);
                            return false;
                        }
                    }
                    else
                        return true;
                }


            System.IO.FileInfo finfo = new System.IO.FileInfo(pathFile);
            System.IO.FileInfo firi = new System.IO.FileInfo(pathRestoreInfoFile);

            DateTime dtmOriginalTime = finfo.LastWriteTimeUtc;

            SaveInfo si = new SaveInfo();
            bool bNotReadableSi = !firi.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (System.IO.FileStream s = System.IO.File.OpenRead(pathRestoreInfoFile))
                    {
                        si.ReadFrom(s);
                        s.Close();
                    }
                }
                catch (System.IO.IOException ex)
                {
                    WriteLog(0, "I/O Error reading file: \"", pathRestoreInfoFile, "\": " + ex.Message);
                    bNotReadableSi = true;
                }
            }

            if (bNotReadableSi || si.Length != finfo.Length || !(_bIgnoreTimeDifferences || FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc)) /*si.TimeStamp != finfo.LastWriteTimeUtc*/)
            {
                bool bAllBlocksOk = true;
                bForceCreateInfo = true;

                if (!bNotReadableSi)
                    WriteLog(0, "RestoreInfo file \"", pathRestoreInfoFile, "\" can't be used for restoring file \"", pathFile, "\": it was created for another version of the file");

                using (System.IO.FileStream s = System.IO.File.Open(finfo.FullName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    try
                    {
                        int countErrors = 0;
                        using (System.IO.FileStream s2 = System.IO.File.Open(pathTargetFile + ".tmp", System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                        {
                            for (long index = 0; ; index++)
                            {
                                Block b = Block.GetBlock();
                                try
                                {
                                    int lengthToWrite = b.ReadFrom(s);
                                    if (lengthToWrite > 0)
                                        b.WriteTo(s2, lengthToWrite);
                                    if (lengthToWrite != b.Length)
                                        break;
                                }
                                catch (System.IO.IOException ex)
                                {
                                    if (bFailOnNonRecoverable)
                                        throw;

                                    WriteLog(0, "I/O Error while reading file ", finfo.FullName, " position ", index * b.Length, ": ", ex.Message, ". Block will be replaced with a dummy during copy.");
                                    int lengthToWrite = (int)(finfo.Length - index * b.Length > b.Length ? b.Length : finfo.Length - index * b.Length);
                                    if (lengthToWrite > 0)
                                        b.WriteTo(s2, lengthToWrite);
                                    bAllBlocksOk = false;
                                    ++countErrors;
                                    s.Seek(index * b.Length + lengthToWrite, System.IO.SeekOrigin.Begin);
                                }

                                Block.ReleaseBlock(b);
                            };

                            s2.Close();
                        }

                        // after the file has been copied to a ".tmp" delete old one
                        System.IO.FileInfo fi2 = new System.IO.FileInfo(pathTargetFile);

                        if (fi2.Exists)
                            fi2.Delete();

                        // and replace it with the new one
                        System.IO.FileInfo fi2tmp = new System.IO.FileInfo(pathTargetFile + ".tmp");
                        if (bAllBlocksOk)
                            // if everything OK set original time
                            fi2tmp.LastWriteTimeUtc = dtmOriginalTime;
                        else
                        {
                            // set the time to very old, so any existing newer or with less errors appears to be better.
                            fi2tmp.LastWriteTime = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                            bForceCreateInfoTarget = true;
                        }
                        //fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                        fi2tmp.MoveTo(pathTargetFile);

                        if (!bAllBlocksOk)
                            WriteLog(0, "Warning: copied ", pathFile, " to ", pathTargetFile, " ", strReason, " with errors");
                        else
                            WriteLog(0, "Copied ", pathFile, " to ", pathTargetFile, " ", strReason);

                    } catch
                    {
                        System.Threading.Thread.Sleep(5000);

                        throw;
                    }
                    s.Close();
                }
               
                return bAllBlocksOk;
            }

            Dictionary<long, bool> readableButNotAccepted = new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;
                using (System.IO.FileStream s = System.IO.File.OpenRead(finfo.FullName))
                {
                    si.StartRestore();
                    Block b = Block.GetBlock();
                    for (long index = 0; ; index++)
                    {
                        try
                        {
                            bool bBlockOk = true;
                            int nRead = 0;
                            if ( (nRead = b.ReadFrom(s)) == b.Length)
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    bAllBlocksOK = false;
                                    WriteLog(2, finfo.FullName, ": checksum of block at offset ", index * b.Length, " not OK");
                                    readableButNotAccepted[index] = true;
                                }
                            }
                            else 
                            {
                                if (nRead > 0)
                                {
                                    //  fill the rest with zeros
                                    while (nRead < b.Length)
                                        b[nRead++] = 0;

                                    bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                    if (!bBlockOk)
                                    {
                                        bAllBlocksOK = false;
                                        WriteLog(2, finfo.FullName, ": checksum of block at offset ", index * b.Length, " not OK");
                                        readableButNotAccepted[index] = true;
                                    }
                                }
                                break;
                            }
                        }
                        catch (System.IO.IOException ex)
                        {
                            bAllBlocksOK = false;
                            WriteLog(2, "I/O Error reading file: \"", finfo.FullName, "\", offset ", index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length, System.IO.SeekOrigin.Begin);
                        }

                        if (_cancelClicked)
                            throw new OperationCanceledException();

                    };
                    Block.ReleaseBlock(b);

                    s.Close();
                };


                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        WriteLog(0, "RestoreInfo file \"", pathRestoreInfoFile, "\" has been damaged and needs to be recreated from \"", pathFile, "\"");
                        bForceCreateInfo = true;
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error reading file: \"", finfo.FullName, "\": " + ex.Message);

                if (bFailOnNonRecoverable)
                    throw;

                return false;
            }


            try
            {
                long nonRestoredSize = 0;
                List<RestoreInfo> rinfos = si.EndRestore(out nonRestoredSize, pathRestoreInfoFile, this);

                if (nonRestoredSize > 0)
                {
                    if (bFailOnNonRecoverable)
                    {
                        WriteLog(1, "There are ", rinfos.Count, " bad blocks in the file ", finfo.FullName, ", non-restorable parts: ", nonRestoredSize, " bytes. Can't proceed there because of non-recoverable, may retry later.");
                        throw new Exception("Non-recoverable blocks discovered, failing");
                    }
                    else
                        bForceCreateInfoTarget = true;
                }

                if (rinfos.Count > 1)
                    WriteLog(1, "There are ", rinfos.Count, " bad blocks in the file ", finfo.FullName, ", non-restorable parts: ", nonRestoredSize, " bytes. "+ (bApplyRepairsToSrc?"":"The file can't be modified because of missing repair option, the restore process will be applied to copy."));
                else
                    if (rinfos.Count > 0)
                        WriteLog(1, "There is one bad block in the file ", finfo.FullName, ", non-restorable parts: ", nonRestoredSize, " bytes." + (bApplyRepairsToSrc ? "" : " The file can't be modified because of missing repair option, the restore process will be applied to copy."));

                //bool bNonRecoverablePresent = false;
                try
                {
                    using (System.IO.FileStream s2 = System.IO.File.Open(finfo.FullName, System.IO.FileMode.Open, bApplyRepairsToSrc && (rinfos.Count > 0) ? System.IO.FileAccess.ReadWrite : System.IO.FileAccess.Read, System.IO.FileShare.Read))
                    {
                        using (System.IO.FileStream s = System.IO.File.Open(pathTargetFile + ".tmp", System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                        {
                            long blockSize = Block.GetBlock().Length;
                            for (long position = 0; position < finfo.Length; position += blockSize)
                            {
                                bool bBlockWritten = false;
                                foreach (RestoreInfo ri in rinfos)
                                {
                                    if (ri.Position == position)
                                    {
                                        bBlockWritten = true;
                                        if (ri.NotRecoverableArea)
                                        {
                                            if (readableButNotAccepted.ContainsKey(ri.Position / blockSize))
                                            {
                                                WriteLog(1, "Keeping readable but not recoverable block at offset ", ri.Position, ", of original file ", finfo.FullName, " also in copy ", pathTargetFile, ", checksum indicates the block is wrong");
                                            }
                                            else
                                            {
                                                s2.Seek(ri.Position + ri.Data.Length, System.IO.SeekOrigin.Begin);

                                                WriteLog(1, "Filling not recoverable block at offset ", ri.Position, " of copied file ", pathTargetFile, " with a dummy");

                                                //bNonRecoverablePresent = true;
                                                int lengthToWrite = (int)(finfo.Length - position > blockSize ? blockSize : finfo.Length - position);
                                                if (lengthToWrite > 0)
                                                    ri.Data.WriteTo(s, lengthToWrite);
                                            }
                                            bForceCreateInfo = true;
                                        }
                                        else
                                        {
                                            WriteLog(1, "Recovering block at offset ", ri.Position, " of copied file ", pathTargetFile);
                                            int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ? ri.Data.Length : si.Length - ri.Position);
                                            if (lengthToWrite > 0)
                                                ri.Data.WriteTo(s, lengthToWrite);

                                            if (bApplyRepairsToSrc)
                                            {
                                                WriteLog(1, "Recovering block at offset ", ri.Position, " of file ", finfo.FullName);
                                                s2.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                                if (lengthToWrite > 0)
                                                    ri.Data.WriteTo(s2, lengthToWrite);
                                            }
                                            else
                                                s2.Seek(ri.Position + lengthToWrite, System.IO.SeekOrigin.Begin);
                                        }
                                        break;
                                    }
                                }

                                if (!bBlockWritten)
                                {
                                    Block b = Block.GetBlock();
                                    int lengthToWrite = b.ReadFrom(s2);
                                    b.WriteTo(s, lengthToWrite);
                                    Block.ReleaseBlock(b);
                                }
                            }

                            s.Close();
                        }
                        s2.Close();
                    }
                }
                finally
                {
                    // if we applied some repairs to the source, then restore its timestamp
                    if (bApplyRepairsToSrc && (rinfos.Count > 0))
                        finfo.LastWriteTimeUtc = dtmOriginalTime;
                }
                System.IO.FileInfo finfoTmp = new System.IO.FileInfo(pathTargetFile + ".tmp");
                if (System.IO.File.Exists(pathTargetFile))
                    System.IO.File.Delete(pathTargetFile);
                finfoTmp.MoveTo(pathTargetFile);


                System.IO.FileInfo finfo2 = new System.IO.FileInfo(pathTargetFile);
                if (rinfos.Count > 1)
                    WriteLog(1, "Out of ", rinfos.Count, " bad blocks in the original file not restored parts in the copy ", finfo2.FullName, ": ", nonRestoredSize, " bytes.");
                else
                    if (rinfos.Count > 0)
                    WriteLog(1, "There was one bad block in the original file, not restored parts in the copy ", finfo2.FullName, ": ", nonRestoredSize, " bytes.");

                if (nonRestoredSize > 0)
                {
                    int countErrors = (int)(nonRestoredSize / (Block.GetBlock().Length));
                    finfo2.LastWriteTime = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                    bForceCreateInfoTarget = true;
                }
                else
                    finfo2.LastWriteTimeUtc = dtmOriginalTime;

                if (nonRestoredSize != 0)
                    WriteLog(0, "Warning: copied ", pathFile, " to ", pathTargetFile, " ", strReason, " with errors");
                else
                    WriteLog(0, "Copied ", pathFile, " to ", pathTargetFile, " ", strReason);

                //finfo2.LastWriteTimeUtc = prevLastWriteTime;

                return nonRestoredSize == 0;
            }
            catch (System.IO.IOException ex)
            {
                WriteLog(0, "I/O Error during repair copy to file: \"", pathTargetFile, "\": " + ex.Message);
                return false;
            }
        }

        void TestAndRepairSecondFile(string path1, string path2, string path1ri, string path2ri, ref bool bForceCreateInfo)
        {
            // if we can skip repairs, then try to test first and repair only in case there are some errors.
            if (_bSkipRecentlyTested)
                if (TestSingleFile(path2, path2ri, ref bForceCreateInfo, false, false, true))
                    return;

            System.IO.FileInfo fi1 = new System.IO.FileInfo(path1);
            System.IO.FileInfo fi2 = new System.IO.FileInfo(path2);
            System.IO.FileInfo fi1ri = new System.IO.FileInfo(path1ri);
            System.IO.FileInfo fi2ri = new System.IO.FileInfo(path2ri);

            SaveInfo si1 = new SaveInfo();
            SaveInfo si2 = new SaveInfo();

            bool bSaveInfo1Present = false;
            if (fi1ri.Exists && (_bIgnoreTimeDifferences || fi1ri.LastWriteTimeUtc == fi1.LastWriteTimeUtc) )
            {
                using (System.IO.Stream s = System.IO.File.OpenRead(fi1ri.FullName))
                {
                    si1.ReadFrom(s);
                    bSaveInfo1Present = si1.Length == fi1.Length && (_bIgnoreTimeDifferences || FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc) )/*si1.TimeStamp == fi1.LastWriteTimeUtc*/;
                    if (!bSaveInfo1Present)
                    {
                        si1 = new SaveInfo();
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        s.Seek(0, System.IO.SeekOrigin.Begin);
                        si2.ReadFrom(s);
                    }
                    s.Close();
                }
            }

            if (fi2ri.Exists && (_bIgnoreTimeDifferences || fi2ri.LastWriteTimeUtc == fi2.LastWriteTimeUtc) )
            {
                using (System.IO.Stream s = System.IO.File.OpenRead(fi2ri.FullName))
                {
                    SaveInfo si3 = new SaveInfo();
                    si3.ReadFrom(s);
                    if (si3.Length == fi2.Length && (_bIgnoreTimeDifferences || FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc) )/*si3.TimeStamp == fi2.LastWriteTimeUtc*/)
                    {
                        si2 = si3;
                        if (!bSaveInfo1Present)
                        {
                            s.Seek(0, System.IO.SeekOrigin.Begin);
                            si1.ReadFrom(s);
                            bSaveInfo1Present = true;
                        }
                    }
                    else
                        bForceCreateInfo = true;
                    s.Close();
                }
            }


            if (bSaveInfo1Present)
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // improve the available saved infos, if needed 
                si1.ImproveWith(si2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> equalBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                using (System.IO.Stream s1 = System.IO.File.OpenRead(path1))
                {
                    using (System.IO.Stream s2 = System.IO.File.OpenRead(path2))
                    {
                        si1.StartRestore();
                        si2.StartRestore();

                        for (int index = 0; ; ++index)
                        {
                            Block b1 = Block.GetBlock();
                            Block b2 = Block.GetBlock();

                            bool b1Present = false;
                            bool b1Ok = false;
                            bool s1Continue = false;
                            try
                            {
                                if (b1.ReadFrom(s1) == b1.Length)
                                {
                                    b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                    s1Continue = true;
                                }
                                else
                                {
                                    b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                }
                                readableBlocks1[index] = true;
                                b1Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLog(2, "I/O exception while reading file \"", path1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, System.IO.SeekOrigin.Begin);
                            }

                            bool b2Present = false;
                            bool b2Ok = false;
                            bool s2Continue = false;
                            try
                            {
                                if (b2.ReadFrom(s2) == b2.Length)
                                {
                                    b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                    s2Continue = true;
                                }
                                else
                                {
                                    b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                }
                                readableBlocks2[index] = true;
                                b2Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLog(2, "I/O exception while reading file \"", path2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                if (si2.AnalyzeForTestOrRestore(b1, index))
                                {
                                    WriteLog(1, "Block of ", fi2.FullName, " position ", index * b1.Length, " will be restored from ", fi1.FullName);
                                    restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                }
                            }
                            else
                                if (b2Present && !b1Present)
                            {
                                if (si1.AnalyzeForTestOrRestore(b2, index))
                                {
                                    restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    WriteLog(1, "Block of ", fi1.FullName, " position ", index * b1.Length, " could be restored from ", fi2.FullName, " but it is not possible to write to the first folder");
                                }
                            }
                            else
                            {
                                if (b2Present && !b1Ok)
                                {
                                    if (si1.AnalyzeForTestOrRestore(b2, index))
                                    {
                                        WriteLog(1, "Block of ", fi1.FullName, " position ", index * b1.Length, " could be restored from ", fi2.FullName, " but it is not possible to write to the first folder");
                                        restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    }
                                };

                                if (b1Present && !b2Ok)
                                {
                                    if (si2.AnalyzeForTestOrRestore(b1, index))
                                    {
                                        WriteLog(1, "Block of ", fi2.FullName, " position ", index * b1.Length, " will be restored from ", fi1.FullName);
                                        restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                    }
                                }
                            }

                            if (b1Present && b2Present)
                            {
                                // if both blocks are present we'll compare their contents
                                // equal blocks could have higher priority compared to their checksums and saved infos
                                bool bDifferent = false;
                                for (int i = b1.Length - 1; i >= 0; --i)
                                    if (b1[i] != b2[i])
                                    {
                                        bDifferent = true;
                                        break;
                                    }

                                if (!bDifferent)
                                {
                                    equalBlocks[index] = true;
                                }
                            }

                            if (!s1Continue && !s2Continue)
                                break;

                            if (_cancelClicked)
                                throw new OperationCanceledException();

                            Block.ReleaseBlock(b1);
                            Block.ReleaseBlock(b2);
                        }

                        s2.Close();
                    }
                    s1.Close();
                }

                long notRestoredSize1 = 0;
                restore1.AddRange(si1.EndRestore(out notRestoredSize1, fi1ri.FullName, this));
                notRestoredSize1 = 0;

                long notRestoredSize2 = 0;
                restore2.AddRange(si2.EndRestore(out notRestoredSize2, fi2ri.FullName, this));
                notRestoredSize2 = 0;

                // now we've got the list of improvements for both files
                using (System.IO.Stream s1 = System.IO.File.Open(path1, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {

                    using (System.IO.Stream s2 = System.IO.File.Open(path2, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {
                        // let'oInputStream apply improvements of one file to the list of the oOtherSaveInfo, whenever possible (we are in first folder readonly case)
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            foreach (RestoreInfo ri2 in restore2)
                            {
                                if (ri2.Position == ri1.Position && ri2.NotRecoverableArea && !ri1.NotRecoverableArea)
                                {
                                    WriteLog(1, "Block of ", fi2.FullName, " position ", ri2.Position, " will be restored from ", fi1.FullName);
                                    ri2.Data = ri1.Data;
                                    ri2.NotRecoverableArea = false;
                                }
                            }
                        }

                        // let'oInputStream apply the definitive improvements
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea || (_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
                                ; // bForceCreateInfo = true;
                            else
                            {
                                WriteLog(1, "Recovering block of ", fi2.FullName, " at position ", ri2.Position);
                                s1.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? ri2.Data.Length : si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        };



                        // let'oInputStream try to copy non-recoverable blocks from one file to another, whenever possible
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea && !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                               readableBlocks1.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                WriteLog(1, "Block of ", fi2.FullName, " position ", ri2.Position, " will be copied from ", fi1.FullName, " even if checksum indicates the block is wrong");
                                s1.Seek(ri2.Position, System.IO.SeekOrigin.Begin);
                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                Block temp = Block.GetBlock();
                                int length = temp.ReadFrom(s1);
                                temp.WriteTo(s2, length);
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                                Block.ReleaseBlock(temp);
                            }
                        };

                        // after all fill non-readable blocks with zeroes
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea && !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                WriteLog(1, "Block of ", fi2.FullName, " position ", ri2.Position, " is not recoverable and will be filled with a dummy");

                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? ri2.Data.Length : si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                notRestoredSize2 += lengthToWrite;
                            }
                        };

                        s2.Close();
                    }
                    s1.Close();
                }

                if (restore2.Count>0)
                    WriteLog(0, "There were ", restore2.Count, " bad blocks in ", fi2.FullName, " not restored bytes: ", notRestoredSize2);
                if (restore1.Count>0)
                    WriteLog(0, "There remain ", restore1.Count, " bad blocks in ", fi1.FullName, ", because it can't be modified ");
                fi2.LastWriteTimeUtc = prevLastWriteTime;

            }
            else
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // let'oInputStream read both copies of the file that obviously are both present, without saved info
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                long notRestoredSize2 = 0;
                long badBlocks2 = 0;
                long badBlocks1 = 0;
                using (System.IO.Stream s1 = System.IO.File.OpenRead(path1))
                {
                    using (System.IO.Stream s2 = System.IO.File.OpenRead(path2))
                    {
                        for (int index = 0; ; ++index)
                        {
                            Block b1 = Block.GetBlock();
                            Block b2 = Block.GetBlock();

                            bool b1Present = false;
                            bool s1Continue = false;
                            try
                            {
                                if (b1.ReadFrom(s1) == b1.Length)
                                    s1Continue = true;
                                b1Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                ++badBlocks1;
                                WriteLog(2, "I/O exception while reading file \"", path1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, System.IO.SeekOrigin.Begin);
                            }

                            bool b2Present = false;
                            bool s2Continue = false;
                            try
                            {
                                if (b2.ReadFrom(s2) == b2.Length)
                                    s2Continue = true;
                                b2Present = true;
                            }
                            catch (System.IO.IOException ex)
                            {
                                ++badBlocks2;
                                WriteLog(2, "I/O exception while reading file \"", path2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                WriteLog(1, "Block of ", fi2.FullName, " position ", index * b1.Length, " will be restored from ", fi1.FullName);
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                            }
                            else
                                if (!b1Present && !b2Present)
                            {
                                WriteLog(1, "Block of ", fi2.FullName, " at position ", index * b1.Length, " is not recoverable and will be filled with a dummy block");
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, true));
                            }


                            if (!s1Continue && !s2Continue)
                                break;

                            if (_cancelClicked)
                                throw new OperationCanceledException();

                            Block.ReleaseBlock(b1);
                            Block.ReleaseBlock(b2);
                        }

                        s2.Close();
                    }
                    s1.Close();
                }


                using (System.IO.Stream s2 = System.IO.File.Open(path2, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo ri2 in restore2)
                    {
                        s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? ri2.Data.Length : si2.Length - ri2.Position);
                        if (lengthToWrite > 0)
                            ri2.Data.WriteTo(s2, lengthToWrite);
                    };
                }

                if (badBlocks2>0)
                    WriteLog(0, "There were ", badBlocks2, " bad blocks in ", fi2.FullName, " not restored bytes: ", notRestoredSize2);
                if (badBlocks1>0)
                    WriteLog(0, "There remain ", badBlocks1, " bad blocks in ", fi1.FullName, ", because it can't be modified ");

                fi2.LastWriteTimeUtc = prevLastWriteTime;

            }
        }


        System.Text.StringBuilder _log;
        System.IO.TextWriter _logFile;

        public void WriteLog(int indent, params object [] parts)
        {
            if (_logFile != null)
            {
                System.DateTime utc = System.DateTime.UtcNow;
                System.DateTime now = utc.ToLocalTime();
                lock (_logFile)
                {
                    _logFile.Write("{0}UT\t{1}\t", utc, now);

                    while (indent-- > 0)
                    {
                        _logFile.Write("\t");
                        _log.Append("        ");
                    }

                    foreach (object part in parts)
                    {
                        string s = part.ToString().Replace(Environment.NewLine,"");
                        _log.Append(s);
                        _logFile.Write(s);
                    }

                    _log.Append(Environment.NewLine);
                    _logFile.Write(Environment.NewLine);
                    _logFile.Flush();
                }
            }
            else
            {
                lock (_log)
                {
                    while (indent-- > 0)
                    {
                        _log.Append("        ");
                    }

                    foreach (object part in parts)
                    {
                        _log.Append(part.ToString());
                    }

                    _log.Append("\r\n");
                }
            }
        }

        void buttonCancel_Click(object sender, EventArgs e)
        {
            if (_bWorking)
            {
                buttonCancel.Enabled = false;
                _cancelClicked = true;
            }
            else
                Close();
        }

         void buttonSelfTest_Click(object sender, EventArgs e)
         {
            if (string.IsNullOrEmpty(textBoxFirstFolder.Text))
                textBoxFirstFolder.Text = Application.StartupPath+"\\TestFolder1";

            if (string.IsNullOrEmpty(textBoxSecondFolder.Text))
                textBoxSecondFolder.Text = Application.StartupPath+"\\TestFolder2";

            System.IO.DirectoryInfo di1 = new System.IO.DirectoryInfo(textBoxFirstFolder.Text);
            if (!di1.Exists)
                di1.Create();

            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(textBoxSecondFolder.Text);
            if (!di2.Exists)
                di2.Create();


            // clear previous selftests
            foreach (System.IO.FileInfo fi in di1.GetFiles())
                fi.Delete();

            foreach (System.IO.FileInfo fi in di2.GetFiles())
                fi.Delete();

            foreach (System.IO.DirectoryInfo di3 in di1.GetDirectories())
                di3.Delete(true);

            foreach (System.IO.DirectoryInfo di3 in di2.GetDirectories())
                di3.Delete(true);

            using (System.IO.StreamWriter w = new System.IO.StreamWriter(System.IO.Path.Combine(textBoxFirstFolder.Text, "copy1-2.txt")))
            {
                w.WriteLine("Copy from 1 to 2");
                w.Close();
            }

            using (System.IO.StreamWriter w = new System.IO.StreamWriter(System.IO.Path.Combine(textBoxSecondFolder.Text, "copy2-1.txt")))
            {
                w.WriteLine("Copy from 2 to 1");
                w.Close();
            }


            Block b = Block.GetBlock();
            using (System.IO.FileStream s = System.IO.File.Create((System.IO.Path.Combine(textBoxFirstFolder.Text, "restore1.txt"))))
            {
                b[0] = 3;
                b.WriteTo(s, 100);
                s.Close();
            }
            System.IO.FileInfo fi2 = new System.IO.FileInfo((System.IO.Path.Combine(textBoxFirstFolder.Text, "restore1.txt")));
            System.IO.DirectoryInfo di4 = new System.IO.DirectoryInfo((System.IO.Path.Combine(textBoxFirstFolder.Text, "RestoreInfo")));
            di4.Create();
            SaveInfo si = new SaveInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (System.IO.FileStream s = System.IO.File.Create((System.IO.Path.Combine(di4.FullName, "restore1.txt.chk"))))
            {
                b[0] = 1;
                si.AnalyzeForInfoCollection(b, 0);
                si.SaveTo(s);
                s.Close();
            }
            System.IO.FileInfo fi3 = new System.IO.FileInfo((System.IO.Path.Combine(di4.FullName, "restore1.txt.chk")));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;

            using (System.IO.FileStream s = System.IO.File.Create((System.IO.Path.Combine(textBoxFirstFolder.Text, "restore2.txt"))))
            {
                b[0] = 3;
                b.WriteTo(s, b.Length);
                b.WriteTo(s, b.Length);
                s.Close();
            }
            fi2 = new System.IO.FileInfo((System.IO.Path.Combine(textBoxFirstFolder.Text, "restore2.txt")));
            si = new SaveInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (System.IO.FileStream s = System.IO.File.Create((System.IO.Path.Combine(di4.FullName, "restore2.txt.chk"))))
            {
                b[0] = 2;
                si.AnalyzeForInfoCollection(b, 0);
                b[0] = 0;
                si.AnalyzeForInfoCollection(b, 1);
                si.SaveTo(s);
                s.Close();
            }
            fi3 = new System.IO.FileInfo((System.IO.Path.Combine(di4.FullName, "restore2.txt.chk")));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;

            using (System.IO.FileStream s = System.IO.File.Create((System.IO.Path.Combine(textBoxFirstFolder.Text, "restore3.txt"))))
            {
                using (System.IO.FileStream s2 = System.IO.File.Create((System.IO.Path.Combine(textBoxSecondFolder.Text, "restore3.txt"))))
                {
                    // first block of both files: equal, but the checksum will differ
                    b[0] = 3;
                    b.WriteTo(s, b.Length);
                    b.WriteTo(s2, b.Length);

                    // second block needs to be copied from first to second 
                    b.WriteTo(s, b.Length);
                    b[0] = 255;
                    b.WriteTo(s2, b.Length);

                    // third block needs to be copied from second to first
                    b.WriteTo(s, b.Length);
                    b[0] = 3;
                    b.WriteTo(s2, b.Length);

                    // fourth block: both files are different, and the checksum is different
                    b[0] = 255;
                    b.WriteTo(s, b.Length);
                    b[0] = 254;
                    b.WriteTo(s2, b.Length);

                    s2.Close();
                }
                s.Close();
            }

            fi2 = new System.IO.FileInfo((System.IO.Path.Combine(textBoxFirstFolder.Text, "restore3.txt")));
            si = new SaveInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (System.IO.FileStream s = System.IO.File.Create((System.IO.Path.Combine(di4.FullName, "restore3.txt.chk"))))
            {
                b[0] = 255;
                si.AnalyzeForInfoCollection(b, 0);
                b[0] = 3;
                si.AnalyzeForInfoCollection(b, 1);
                si.AnalyzeForInfoCollection(b, 2);
                si.AnalyzeForInfoCollection(b, 3);
                si.SaveTo(s);
                s.Close();
            }
            fi3 = new System.IO.FileInfo((System.IO.Path.Combine(di4.FullName, "restore3.txt.chk")));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;
            fi3 = new System.IO.FileInfo((System.IO.Path.Combine(textBoxSecondFolder.Text, "restore3.txt")));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;

            
            System.IO.File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(textBoxFirstFolder.Text,"TestPicture1.jpg"));
            CreateRestoreInfo(System.IO.Path.Combine(textBoxFirstFolder.Text,"TestPicture1.jpg"),System.IO.Path.Combine(textBoxFirstFolder.Text,"RestoreInfo\\TestPicture1.jpg.chk"));
            DateTime dtmOld = System.IO.File.GetLastWriteTimeUtc(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture1.jpg"));
            using (System.IO.FileStream s = System.IO.File.OpenWrite(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture1.jpg")))
            {
                s.Seek(163840, System.IO.SeekOrigin.Begin);
                s.Write(b._data, 0, b.Length);
                s.Flush();
                s.Close();
            }
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture1.jpg"), dtmOld);

            System.IO.File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture2.jpg"));
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture2.jpg"), dtmOld);
            CreateRestoreInfo(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture2.jpg"), System.IO.Path.Combine(textBoxFirstFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"));
            System.IO.File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(textBoxSecondFolder.Text, "TestPicture2.jpg"));
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(textBoxSecondFolder.Text, "TestPicture2.jpg"), dtmOld);
            CreateRestoreInfo(System.IO.Path.Combine(textBoxSecondFolder.Text, "TestPicture2.jpg"), System.IO.Path.Combine(textBoxSecondFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"));
            using (System.IO.FileStream s = System.IO.File.OpenWrite(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture2.jpg")))
            {
                s.Seek(81920 + 2048, System.IO.SeekOrigin.Begin);
                s.Write(b._data,0,b.Length);
                s.Flush();
                s.Close();
            }
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture2.jpg"), dtmOld);

            using (System.IO.FileStream s = System.IO.File.OpenWrite(System.IO.Path.Combine(textBoxSecondFolder.Text, "TestPicture2.jpg")))
            {
                s.Seek(81920 + 4096 + 2048, System.IO.SeekOrigin.Begin);
                s.Write(b._data, 0, b.Length);
                s.Close();
            }
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(textBoxSecondFolder.Text, "TestPicture2.jpg"), dtmOld);
            
            buttonSync_Click(this, EventArgs.Empty);
        }

        void checkBoxRepairBlockFailures_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxPreferCopies.Enabled = checkBoxTestAllFiles.Checked && checkBoxRepairBlockFailures.Checked;
            checkBoxIgnoreTime.Enabled = checkBoxFirstToSecond.Checked && !checkBoxSyncMode.Checked && checkBoxFirstReadonly.Checked && checkBoxRepairBlockFailures.Checked;
        }

        void textBoxFirstFolder_TextChanged(object sender, EventArgs e)
        {
            buttonSync.Enabled = !string.IsNullOrEmpty(textBoxFirstFolder.Text) && !string.IsNullOrEmpty(textBoxSecondFolder.Text);
        }

        void textBoxSecondFolder_TextChanged(object sender, EventArgs e)
        {
            buttonSync.Enabled = !string.IsNullOrEmpty(textBoxFirstFolder.Text) && !string.IsNullOrEmpty(textBoxSecondFolder.Text);
        }

        void checkBoxFirstToSecond_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxDeleteFilesInSecond.Enabled = checkBoxFirstToSecond.Checked;
            checkBoxFirstReadonly.Enabled = checkBoxFirstToSecond.Checked;
            checkBoxSyncMode.Enabled = checkBoxFirstToSecond.Checked;
            checkBoxIgnoreTime.Enabled = checkBoxFirstToSecond.Checked && !checkBoxSyncMode.Checked && checkBoxFirstReadonly.Checked && checkBoxRepairBlockFailures.Checked;
        }

        private void linkLabelAbout_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (AboutForm form = new AboutForm())
                form.ShowDialog(this);
        }

        private void linkLabelLicence_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.gnu.org/licenses/gpl-2.0.html");
        }

        private void FormSyncFolders_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_bWorking)
               this.buttonCancel_Click(sender, EventArgs.Empty);
        }

        private void checkBoxParallel_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxParallel.Checked)
            {
                _maxParallelCopies = Math.Max(System.Environment.ProcessorCount * 5 / 8, 2);
                _maxParallelThreads = System.Environment.ProcessorCount * 3 / 2;
                _copyFiles = new System.Threading.Semaphore(_maxParallelCopies, _maxParallelCopies);
                _parallelThreads = new System.Threading.Semaphore(_maxParallelThreads, _maxParallelThreads);
                _hugeReads = new System.Threading.Semaphore(1, 1);
            }
            else
            {
                _maxParallelCopies = 1;
                _maxParallelThreads = 1;
                _copyFiles = new System.Threading.Semaphore(_maxParallelCopies, _maxParallelCopies);
                _parallelThreads = new System.Threading.Semaphore(_maxParallelThreads, _maxParallelThreads);
                _hugeReads = new System.Threading.Semaphore(1, 1);
            }
        }

        private void checkBoxFirstReadonly_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxIgnoreTime.Enabled = checkBoxFirstToSecond.Checked && !checkBoxSyncMode.Checked && checkBoxFirstReadonly.Checked && checkBoxRepairBlockFailures.Checked;

            if (checkBoxFirstReadonly.Checked)
                checkBoxParallel.Checked = false;
        }

        volatile int _currentFile;
        volatile string _currentPath;

        private void timerUpdateFileDescription_Tick(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(delegate(object sender2, EventArgs args)
                {
                    if (_currentFile>0)
                        progressBar1.Value = _currentFile;
                    if (_currentPath!=null)
                        labelProgress.Text = _currentPath;
                }));
            }
            else
            {
                if (_currentFile > 0)
                    progressBar1.Value = _currentFile;
                if (_currentPath != null)
                    labelProgress.Text = _currentPath;
            }
        }

        private void checkBoxIgnoreTime_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxSyncMode_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxIgnoreTime.Enabled = checkBoxFirstToSecond.Checked && !checkBoxSyncMode.Checked && checkBoxFirstReadonly.Checked && checkBoxRepairBlockFailures.Checked;
        }
    }
}
