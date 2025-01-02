/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024-2025 NataljaNeumann@gmx.de

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
    //*******************************************************************************************************
    /// <summary>
    /// This is the main form of the application. It also implements the logic of checking the files
    /// and decisions, how to proceed with them
    /// </summary>
    //*******************************************************************************************************
    public partial class FormSyncFolders : 
        Form, 
        ILogWriter
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

        Random _randomizeChecked = new Random(DateTime.Now.Millisecond + 1000 
            * (DateTime.Now.Second + 60 * (DateTime.Now.Minute + 60 
            * (DateTime.Now.Hour + 24 * DateTime.Now.DayOfYear))));

        static int _maxParallelCopies = 
            Math.Max(System.Environment.ProcessorCount * 5 / 8, 2);
        static int _maxParallelThreads = 
            System.Environment.ProcessorCount * 3/2;
        System.Threading.Semaphore _copyFiles = 
            new System.Threading.Semaphore(_maxParallelCopies, _maxParallelCopies);
        System.Threading.Semaphore _parallelThreads = 
            new System.Threading.Semaphore(_maxParallelThreads, _maxParallelThreads);
        System.Threading.Semaphore _hugeReads = 
            new System.Threading.Semaphore(1, 1);

        IFileOpenAndCopyAbstraction m_iFileOpenAndCopyAbstraction = new FileOpenAndCopyDirectly();

        //===================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        //===================================================================================================
        public FormSyncFolders()
        {
            InitializeComponent();
#if DEBUG
            buttonSelfTest.Visible = true;
            //checkBoxParallel.Visible = true;
#else
            textBoxSecondFolder.Text = System.IO.Directory.GetParent(Application.StartupPath).FullName;
#endif
        }

        //===================================================================================================
        /// <summary>
        /// This is executed on a help request
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void folderBrowserDialog1_HelpRequest(
            object oSender, 
            EventArgs oEventArgs)
        {

        }

        //===================================================================================================
        /// <summary>
        /// Some filesystems have milliseconds in them, some oOtherSaveInfo don't even
        /// have all seconds on file times.
        /// 
        /// So compare the file times in a smart manner
        /// </summary>
        /// <param name="dtmTime1">Time of a file in one file system</param>
        /// <param name="dtmTime2">Time of a file in a different file system</param>
        /// <returns>true if the file times are considered equal</returns>
        //===================================================================================================
        private bool FileTimesEqual(
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
        /// This is executed when Test all files checkbox is clicked
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void checkBoxTestAllFiles_CheckedChanged(
            object oSender, 
            EventArgs oEventArgs)
        {
            checkBoxRepairBlockFailures.Enabled = checkBoxTestAllFiles.Checked;
            checkBoxPreferCopies.Enabled = checkBoxTestAllFiles.Checked && 
                checkBoxRepairBlockFailures.Checked;
            checkBoxSkipRecentlyTested.Enabled = checkBoxTestAllFiles.Checked;
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when ... at first folder is clicked
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void buttonSelectFirstFolder_Click(
            object oSender, 
            EventArgs oEventArgs)
        {
            if (!string.IsNullOrEmpty(textBoxFirstFolder.Text))
                folderBrowserDialogFolder1.SelectedPath = textBoxFirstFolder.Text;

            if (folderBrowserDialogFolder1.ShowDialog() == DialogResult.OK)
            {
                textBoxFirstFolder.Text = folderBrowserDialogFolder1.SelectedPath;
            }
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when ... at second folder is clicked
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void buttonSelectSecondFolder_Click(
            object oSender, 
            EventArgs oEventArgs)
        {
            if (!string.IsNullOrEmpty(textBoxSecondFolder.Text))
                folderBrowserDialogFolder2.SelectedPath = textBoxSecondFolder.Text;

            if (folderBrowserDialogFolder2.ShowDialog() == DialogResult.OK)
            {
                textBoxSecondFolder.Text = folderBrowserDialogFolder2.SelectedPath;
            }
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks "Sync"
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void buttonSync_Click(
            object oSender, 
            EventArgs oEventArgs)
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
                System.IO.FileInfo fiDontDelete = 
                    new System.IO.FileInfo(System.IO.Path.Combine(
                        _folder2, "SyncFolders-Dont-Delete.txt"));
                if (fiDontDelete.Exists)
                {
                    System.Windows.Forms.MessageBox.Show(this, 
                        "The second folder contains file \"SyncFolders-Dont-Delete.txt\", "+
                        "the selected folder seem to be wrong for delete option", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                fiDontDelete = 
                    new System.IO.FileInfo(System.IO.Path.Combine(
                        _folder2, "SyncFolders-Don't-Delete.txt"));
                if (fiDontDelete.Exists)
                {
                    System.Windows.Forms.MessageBox.Show(this, 
                        "The second folder contains file \"SyncFolders-Don't-Delete.txt\", "+
                        "the selected folder seem to be wrong for delete option", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _logFile = new System.IO.StreamWriter(
                System.IO.Path.Combine(
                System.Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), 
                "SyncFoldersLog.txt"), true, Encoding.UTF8);
            _logFileLocalized = new System.IO.StreamWriter(
                System.IO.Path.Combine(
                System.Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments),
                Resources.LogFileName+".txt"), true, Encoding.UTF8);

            _logFile.WriteLine("\r\n\r\n\r\n\r\n");
            _logFileLocalized.WriteLine("\r\n\r\n\r\n\r\n");

            // left to right or right to left
            if (Resources.RightToLeft.Equals("yes"))
                _logFileLocalized.Write((char)0x200F);
            else
                _logFileLocalized.Write((char)0x200E);

 
            _logFile.WriteLine("     First2Second: " + (_bFirstToSecond ? "yes" : "no"));
            _logFileLocalized.WriteLine(checkBoxFirstToSecond.Text + ": " + (_bFirstToSecond ? Resources.Yes : Resources.No));
            if (_bFirstToSecond)
            {
                _logFile.WriteLine("         SyncMode: " + (_syncMode ? "yes" : "no"));
                _logFile.WriteLine("    FirstReadOnly: " + (_bFirstReadOnly ? "yes" : "no"));
                _logFile.WriteLine("   DeleteInSecond: " + (_bDeleteInSecond ? "yes" : "no"));

                _logFileLocalized.WriteLine(checkBoxFirstToSecond.Text + ":" + (_bFirstToSecond ? Resources.Yes : Resources.No));
                _logFile.WriteLine(checkBoxSyncMode.Text + ": " + (_syncMode ? Resources.Yes : Resources.No));
                _logFile.WriteLine(checkBoxFirstReadonly + ": " + (_bFirstReadOnly ? Resources.Yes : Resources.No));
                _logFile.WriteLine(checkBoxDeleteFilesInSecond.Text + ": " + (_bDeleteInSecond ? Resources.Yes : Resources.No));

            }

            _logFile.WriteLine("CreateRestoreInfo: " + (_bCreateInfo ? "yes" : "no"));
            _logFileLocalized.WriteLine(checkBoxCreateRestoreInfo.Text + ": " + (_bCreateInfo ? Resources.Yes : Resources.No));

            _logFile.WriteLine("        TestFiles: " + 
                (_bTestFiles ? (_bSkipRecentlyTested ? "if not tested recently": "yes" ): "no"));
            _logFileLocalized.WriteLine(checkBoxTestAllFiles.Text + ": " +
                (_bTestFiles ? (_bSkipRecentlyTested ? checkBoxSkipRecentlyTested.Text : Resources.Yes) : Resources.No));

            if (_bTestFiles)
            {
                _logFile.WriteLine("      RepairFiles: " + (_bRepairFiles ? "yes" : "no"));
                _logFileLocalized.WriteLine(checkBoxRepairBlockFailures + ": " + (_bRepairFiles ? Resources.Yes : Resources.No));
                if (_bRepairFiles)
                {
                    _logFile.WriteLine("     PreferCopies: " + (_bPreferPhysicalCopies ? "yes" : "no"));
                    _logFile.WriteLine(checkBoxPreferCopies + ": " + (_bPreferPhysicalCopies ? Resources.Yes : Resources.No));
                }
            }
            _logFile.WriteLine("         Folder 1: " + _folder1);
            _logFileLocalized.WriteLine(labelFolder1.Text + (labelFolder1.Text.Contains(":")?" ":": ") + _folder1);
            _logFile.WriteLine("         Folder 2: " + _folder2);
            _logFileLocalized.WriteLine(labelSecondFolder.Text + (labelSecondFolder.Text.Contains(":") ? " " : ": ") + _folder2);

            // end of the path can be in wrong direction, so set it again for the separator
            if (Resources.RightToLeft.Equals("yes"))
                _logFileLocalized.Write((char)0x200F);
            else
                _logFileLocalized.Write((char)0x200E);

            _logFile.Write("###################################################");
            _logFile.Write("###################################################");
            _logFile.Write("###################################################");
            _logFile.WriteLine("###################################################");
            _logFileLocalized.Write("###################################################");
            _logFileLocalized.Write("###################################################");
            _logFileLocalized.Write("###################################################");
            _logFileLocalized.WriteLine("###################################################");

            _logFile.WriteLine(System.DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss.fff") + "UT\tThread\tMessage from thread");
            _logFileLocalized.WriteLine(System.DateTime.Now.ToString("F") + "\t" + Resources.ProcessNo + "\t" + Resources.Message);

            _logFile.Flush();
            _logFileLocalized.Flush();
            _bWorking = true;
            System.Threading.Thread worker = new System.Threading.Thread(SyncWorker);
            Program.SetCultureForThread(worker);
            worker.Priority = System.Threading.ThreadPriority.BelowNormal;
            worker.Start();
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the "repair" checkbox
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        void checkBoxRepairBlockFailures_CheckedChanged(
            object oSender,
            EventArgs oEventArgs)
        {
            checkBoxPreferCopies.Enabled =
                checkBoxTestAllFiles.Checked &&
                checkBoxRepairBlockFailures.Checked;
            checkBoxIgnoreTime.Enabled =
                checkBoxFirstToSecond.Checked &&
                !checkBoxSyncMode.Checked &&
                checkBoxFirstReadonly.Checked &&
                checkBoxRepairBlockFailures.Checked;
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user edits first folder
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        void textBoxFirstFolder_TextChanged(
            object oSender,
            EventArgs oEventArgs)
        {
            buttonSync.Enabled =
                !string.IsNullOrEmpty(textBoxFirstFolder.Text) &&
                !string.IsNullOrEmpty(textBoxSecondFolder.Text);
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user edits the second folder
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        void textBoxSecondFolder_TextChanged(
            object oSender,
            EventArgs oEventArgs)
        {
            buttonSync.Enabled =
                !string.IsNullOrEmpty(textBoxFirstFolder.Text) &&
                !string.IsNullOrEmpty(textBoxSecondFolder.Text);
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the "first-to-second" checkbox
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        void checkBoxFirstToSecond_CheckedChanged(
            object oSender,
            EventArgs oEventArgs
            )
        {
            checkBoxDeleteFilesInSecond.Enabled =
                checkBoxFirstToSecond.Checked;
            checkBoxFirstReadonly.Enabled =
                checkBoxFirstToSecond.Checked;
            checkBoxSyncMode.Enabled =
                checkBoxFirstToSecond.Checked;
            checkBoxIgnoreTime.Enabled =
                checkBoxFirstToSecond.Checked &&
                !checkBoxSyncMode.Checked &&
                checkBoxFirstReadonly.Checked &&
                checkBoxRepairBlockFailures.Checked;
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the 'about' link
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void linkLabelAbout_LinkClicked(
            object oSender,
            LinkLabelLinkClickedEventArgs oEventArgs
            )
        {
            using (AboutForm form = new AboutForm())
                form.ShowDialog(this);
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the 'licence' link
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void linkLabelLicence_LinkClicked(
            object oSender,
            LinkLabelLinkClickedEventArgs oEventArgs
            )
        {
            System.Diagnostics.Process.Start(
                "https://www.gnu.org/licenses/gpl-2.0.html");
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks (X) in window header
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void FormSyncFolders_FormClosing(
            object oSender,
            FormClosingEventArgs oEventArgs)
        {
            if (_bWorking)
                this.buttonCancel_Click(oSender, EventArgs.Empty);
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the "parallel" checkbox
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void checkBoxParallel_CheckedChanged(
            object oSender,
            EventArgs oEventArgs
            )
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

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the "first folder is read-only" checkbox
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void checkBoxFirstReadonly_CheckedChanged(
            object oSender,
            EventArgs oEventArgs)
        {
            checkBoxIgnoreTime.Enabled =
                checkBoxFirstToSecond.Checked &&
                !checkBoxSyncMode.Checked &&
                checkBoxFirstReadonly.Checked &&
                checkBoxRepairBlockFailures.Checked;

            if (checkBoxFirstReadonly.Checked)
                checkBoxParallel.Checked = false;
        }

        volatile int _currentFile;
        volatile string _currentPath;

        //===================================================================================================
        /// <summary>
        /// This is regularly executed for updating currently processed file
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void timerUpdateFileDescription_Tick(
            object oSender,
            EventArgs oEventArgs
            )
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler(delegate(object sender2, EventArgs args)
                {
                    if (_currentFile > 0)
                        progressBar1.Value = _currentFile;
                    if (_currentPath != null)
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

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the "ignore time difference" checkbox
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void checkBoxIgnoreTime_CheckedChanged(
            object oSender,
            EventArgs oEventArgs
            )
        {

        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the "sync mode" checkbox
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void checkBoxSyncMode_CheckedChanged(
            object oSender,
            EventArgs oEventArgs
            )
        {
            checkBoxIgnoreTime.Enabled =
                checkBoxFirstToSecond.Checked &&
                !checkBoxSyncMode.Checked &&
                checkBoxFirstReadonly.Checked &&
                checkBoxRepairBlockFailures.Checked;
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user clicks the cancel button
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        void buttonCancel_Click(
            object oSender,
            EventArgs oEventArgs)
        {
            if (_bWorking)
            {
                buttonCancel.Enabled = false;
                _cancelClicked = true;
            }
            else
                Close();
        }


        //===================================================================================================
        /// <summary>
        /// This is executed whenn user clicks the self-test button
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        void buttonSelfTest_Click(
             object oSender,
             EventArgs oEventArgs
             )
        {
            // replace file abstraction layer with default, as long as we create all the files
            m_iFileOpenAndCopyAbstraction = new FileOpenAndCopyDirectly();
            // but collect simulated errors for later exchange of the file layer
            Dictionary<string, List<long>> oSimulatedReadErrors = new Dictionary<string, List<long>>();
            DateTime dtmTimeForFile = DateTime.UtcNow;

            if (string.IsNullOrEmpty(textBoxFirstFolder.Text))
                textBoxFirstFolder.Text = Application.StartupPath + "\\TestFolder1";

            if (string.IsNullOrEmpty(textBoxSecondFolder.Text))
                textBoxSecondFolder.Text = Application.StartupPath + "\\TestFolder2";

            System.IO.DirectoryInfo di1 =
                new System.IO.DirectoryInfo(textBoxFirstFolder.Text);
            if (!di1.Exists)
                di1.Create();

            System.IO.DirectoryInfo di2 =
                new System.IO.DirectoryInfo(textBoxSecondFolder.Text);
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

            //*

            //---------------------------------
            using (System.IO.StreamWriter w =
                new System.IO.StreamWriter(System.IO.Path.Combine(
                    textBoxFirstFolder.Text, "copy1-2.txt")))
            {
                w.WriteLine("Copy from 1 to 2");
                w.Close();
            }

            //---------------------------------
            using (System.IO.StreamWriter w =
                new System.IO.StreamWriter(System.IO.Path.Combine(
                    textBoxSecondFolder.Text, "copy2-1.txt")))
            {
                w.WriteLine("Copy from 2 to 1");
                w.Close();
            }


            //---------------------------------
            Block b = Block.GetBlock();
            using (System.IO.FileStream s =
                System.IO.File.Create((System.IO.Path.Combine(
                    textBoxFirstFolder.Text, "restore1.txt"))))
            {
                b[0] = 3;
                b.WriteTo(s, 100);
                s.Close();
            }
            System.IO.FileInfo fi2 =
                new System.IO.FileInfo((System.IO.Path.Combine(
                    textBoxFirstFolder.Text, "restore1.txt")));
            System.IO.DirectoryInfo di4 =
                new System.IO.DirectoryInfo((System.IO.Path.Combine(
                    textBoxFirstFolder.Text, "RestoreInfo")));
            di4.Create();
            SavedInfo si = new SavedInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (System.IO.FileStream s =
                System.IO.File.Create((System.IO.Path.Combine(
                    di4.FullName, "restore1.txt.chk"))))
            {
                b[0] = 1;
                si.AnalyzeForInfoCollection(b, 0);
                si.SaveTo(s);
                s.Close();
            }
            System.IO.FileInfo fi3 =
                new System.IO.FileInfo((System.IO.Path.Combine(
                    di4.FullName, "restore1.txt.chk")));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;

            //---------------------------------
            using (System.IO.FileStream s =
                System.IO.File.Create((System.IO.Path.Combine(
                    textBoxFirstFolder.Text, "restore2.txt"))))
            {
                b[0] = 3;
                b.WriteTo(s, b.Length);
                b.WriteTo(s, b.Length);
                s.Close();
            }
            fi2 = new System.IO.FileInfo((System.IO.Path.Combine(
                textBoxFirstFolder.Text, "restore2.txt")));
            si = new SavedInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (System.IO.FileStream s =
                System.IO.File.Create((System.IO.Path.Combine(
                    di4.FullName, "restore2.txt.chk"))))
            {
                b[0] = 2;
                si.AnalyzeForInfoCollection(b, 0);
                b[0] = 0;
                si.AnalyzeForInfoCollection(b, 1);
                si.SaveTo(s);
                s.Close();
            }
            fi3 = new System.IO.FileInfo(System.IO.Path.Combine(
                di4.FullName, "restore2.txt.chk"));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;

            //---------------------------------
            using (System.IO.FileStream s =
                System.IO.File.Create(System.IO.Path.Combine(
                    textBoxFirstFolder.Text, "restore3.txt")))
            {
                using (System.IO.FileStream s2 =
                    System.IO.File.Create(System.IO.Path.Combine(
                        textBoxSecondFolder.Text, "restore3.txt")))
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

            fi2 = new System.IO.FileInfo(System.IO.Path.Combine(
                textBoxFirstFolder.Text, "restore3.txt"));
            si = new SavedInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (System.IO.FileStream s =
                System.IO.File.Create(System.IO.Path.Combine(
                di4.FullName, "restore3.txt.chk")))
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
            fi3 = new System.IO.FileInfo(System.IO.Path.Combine(
                di4.FullName, "restore3.txt.chk"));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;
            fi3 = new System.IO.FileInfo(System.IO.Path.Combine(
                textBoxSecondFolder.Text, "restore3.txt"));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;


            //---------------------------------
            System.IO.File.Copy(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture1.jpg"));
            CreateSavedInfo(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture1.jpg"),
                System.IO.Path.Combine(textBoxFirstFolder.Text, "RestoreInfo\\TestPicture1.jpg.chk"));
            DateTime dtmOld = System.IO.File.GetLastWriteTimeUtc(
                System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture1.jpg"));
            using (System.IO.FileStream s =
                System.IO.File.OpenWrite(System.IO.Path.Combine(
                    textBoxFirstFolder.Text, "TestPicture1.jpg")))
            {
                s.Seek(163840, System.IO.SeekOrigin.Begin);
                s.Write(b._data, 0, b.Length);
                s.Flush();
                s.Close();
            }
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(
                textBoxFirstFolder.Text, "TestPicture1.jpg"), dtmOld);

            //---------------------------------
            System.IO.File.Copy(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture2.jpg"));
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(
                textBoxFirstFolder.Text, "TestPicture2.jpg"), dtmOld);
            CreateSavedInfo(System.IO.Path.Combine(textBoxFirstFolder.Text, "TestPicture2.jpg"),
                System.IO.Path.Combine(textBoxFirstFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"));
            System.IO.File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(textBoxSecondFolder.Text, "TestPicture2.jpg"));
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(
                textBoxSecondFolder.Text, "TestPicture2.jpg"), dtmOld);
            CreateSavedInfo(System.IO.Path.Combine(textBoxSecondFolder.Text, "TestPicture2.jpg"),
                System.IO.Path.Combine(textBoxSecondFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"));
            using (System.IO.FileStream s =
                System.IO.File.OpenWrite(System.IO.Path.Combine(
                textBoxFirstFolder.Text, "TestPicture2.jpg")))
            {
                s.Seek(81920 + 2048, System.IO.SeekOrigin.Begin);
                s.Write(b._data, 0, b.Length);
                s.Flush();
                s.Close();
            }
            System.IO.File.SetLastWriteTimeUtc(System.IO.Path.Combine(
                textBoxFirstFolder.Text, "TestPicture2.jpg"), dtmOld);

            using (System.IO.FileStream s =
                System.IO.File.OpenWrite(System.IO.Path.Combine(
                textBoxSecondFolder.Text, "TestPicture2.jpg")))
            {
                s.Seek(81920 + 4096 + 2048, System.IO.SeekOrigin.Begin);
                s.Write(b._data, 0, b.Length);
                s.Close();
            }
            System.IO.File.SetLastWriteTimeUtc(
                System.IO.Path.Combine(textBoxSecondFolder.Text,
                "TestPicture2.jpg"), dtmOld);

            //---------------------------------
            // non-restorable test
            string strPathOfTestFile1 = CreateSelfTestFile(textBoxFirstFolder.Text, 
                "NonRestorableBecauseNoSaveInfo.dat", 2, false, 
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile1] = new List<long>(new long[] { 0 });

            //---------------------------------
            // auto-repair test
            string strPathOfTestFile2 = CreateSelfTestFile(textBoxFirstFolder.Text,
                "AutoRepairFromSavedInfo.dat", 2, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile2] = new List<long>(new long[] { 4096 });

            //---------------------------------
            // restore older healthy from backup
            string strPathOfTestFile3 = CreateSelfTestFile(textBoxFirstFolder.Text,
                "RestoreOldFromBackup.dat", 2, false,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile3] = new List<long>(new long[] { 4096 });

            strPathOfTestFile3 = CreateSelfTestFile(textBoxSecondFolder.Text,
                "RestoreOldFromBackup.dat", 2, false,
                dtmTimeForFile.AddDays(-1), dtmTimeForFile.AddDays(-1));


            //---------------------------------
            // restore older repairable from backup
            string strPathOfTestFile4 = CreateSelfTestFile(textBoxFirstFolder.Text,
                "RestoreOldFromBackupWithRepairingBackup.dat", 2, false,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile4] = new List<long>(new long[] { 0 });

            strPathOfTestFile4 = CreateSelfTestFile(textBoxSecondFolder.Text,
                "RestoreOldFromBackupWithRepairingBackup.dat", 2, true,
                dtmTimeForFile.AddDays(-1), dtmTimeForFile.AddDays(-1));

            oSimulatedReadErrors[strPathOfTestFile4] = new List<long>(new long[] { 4096 });
            

            //---------------------------------
            // restore file with one block failure in data file and one block failure in .chk
            string strPathOfTestFile5 = CreateSelfTestFile(textBoxFirstFolder.Text,
                "RestoreUncorrelatedChkFailure.dat", 5, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile5] = new List<long>(new long[] { 0 });
            oSimulatedReadErrors[strPathOfTestFile5.Replace
                ("RestoreUncorrelatedChkFailure.dat","RestoreInfo\\RestoreUncorrelatedChkFailure.dat.chk")] = new List<long>(new long[] { 8192 });

            

            //---------------------------------
            // recreate .chk file
            string strPathOfTestFile6 = CreateSelfTestFile(textBoxFirstFolder.Text,
                "RecreeateChkFile.dat", 5, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for the chk file
            oSimulatedReadErrors[strPathOfTestFile6.Replace
                ("RecreeateChkFile.dat", "RestoreInfo\\RecreeateChkFile.dat.chk")] = new List<long>(new long[] { 8192 });


            //---------------------------------
            // recreate two .chk files
            string strPathOfTestFile7 = CreateSelfTestFile(textBoxFirstFolder.Text,
                "Recreeate2ChkFiles.dat", 5, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for the chk files
            oSimulatedReadErrors[strPathOfTestFile7.Replace
                ("Recreeate2ChkFiles.dat", "RestoreInfo\\Recreeate2ChkFiles.dat.chk")] = new List<long>(new long[] { 8192 });

            strPathOfTestFile7 = CreateSelfTestFile(textBoxSecondFolder.Text,
                            "Recreeate2ChkFiles.dat", 5, true,
                            dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for the chk files
            oSimulatedReadErrors[strPathOfTestFile7.Replace
                ("Recreeate2ChkFiles.dat", "RestoreInfo\\Recreeate2ChkFiles.dat.chk")] = new List<long>(new long[] { 8192 });

            // replace default abstraction with error simulation
            m_iFileOpenAndCopyAbstraction = new FileOpenAndCopyWithSimulatedErrors(oSimulatedReadErrors);

            buttonSync_Click(this, EventArgs.Empty);
        }


        //===================================================================================================
        /// <summary>
        /// Creates a pre-defined test file for self-test
        /// </summary>
        /// <param name="strFolder">Folder of the file</param>
        /// <param name="strFileName">File name</param>
        /// <param name="nBlockCount">Number of blocks</param>
        /// <param name="bCreateSaveInfo">If true, the save info file will be created</param>
        /// <param name="dtmTimeForFile">Date and time for the data file</param>
        /// <param name="dtmTimeForSaveInfo">Date and time for the saveinfo (.chk) file</param>
        /// <returns>The path of created data file</returns>
        //===================================================================================================
        string CreateSelfTestFile(
            string strFolder,
            string strFileName,
            int nBlockCount,
            bool bCreateSaveInfo,
            DateTime dtmTimeForFile,
            DateTime dtmTimeForSaveInfo
            )
        {
            string strDestDataFilePath = System.IO.Path.Combine(strFolder,  strFileName);
            Block oBlock = Block.GetBlock();
            using (System.IO.FileStream s = System.IO.File.OpenWrite(
                strDestDataFilePath))
            {
                for (int i=0; i<nBlockCount; ++i)
                {
                    // setting block number within the block helps
                    // in identification of errors
                    oBlock[3] = (byte)(i >> 24);
                    oBlock[2] = (byte)(i >> 16);
                    oBlock[1] = (byte)(i >> 8);
                    oBlock[0] = (byte)(i);

                    if (i == nBlockCount - 1)
                    {
                        // randomly write either half or complete block as
                        // last block, so we have both variants
                        if (dtmTimeForFile.Second % 2 != 0)
                        {
                            oBlock.WriteTo(s, oBlock.Length / 2);
                        }
                        else
                        {
                            oBlock.WriteTo(s, oBlock.Length);
                        }
                    }
                    else
                    {
                        oBlock.WriteTo(s);
                    }
                }
            }

            Block.ReleaseBlock(oBlock);

            // if need saved info, then create it with the specified date and time
            if (bCreateSaveInfo)
            {
                System.IO.File.SetLastWriteTimeUtc(strDestDataFilePath, dtmTimeForSaveInfo);
                CreateSavedInfo(strDestDataFilePath, 
                    CreatePathOfChkFile(strFolder, "RestoreInfo", strFileName, ".chk"));
            }
            // set date and time to specified value
            System.IO.File.SetLastWriteTimeUtc(strDestDataFilePath, dtmTimeForFile);

            return strDestDataFilePath;
        }


        //===================================================================================================
        /// <summary>
        /// A safe method to copy files doesn't copy to the target path, because there can be a problem
        /// with USB cable. This will cause the incomplete file to look newer than the complete original.
        /// 
        /// This method, as well as all other methods in this app first copy to .tmp file and then replace
        /// the target file by .tmp
        /// </summary>
        /// <param name="fi">File info of the source file</param>
        /// <param name="strTargetPath">target path</param>
        /// <param name="strReason">Reason of the copy for messages</param>
        //===================================================================================================
        void CopyFileSafely(
            System.IO.FileInfo fi, 
            string strTargetPath, 
            string strReasonEn,
            string strReasonTranslated)
        {
            string strTargetPath2 = strTargetPath + ".tmp";
            try
            {
                m_iFileOpenAndCopyAbstraction.CopyTo(fi, strTargetPath2, true);

                System.IO.FileInfo fi2 = new System.IO.FileInfo(strTargetPath);
                if (fi2.Exists)
                    m_iFileOpenAndCopyAbstraction.Delete(fi2);

                System.IO.FileInfo fi2tmp = new System.IO.FileInfo(strTargetPath2);
                fi2tmp.MoveTo(strTargetPath);
                WriteLogFormattedLocalized(0, Resources.FileCopied, fi.FullName, strTargetPath, strReasonTranslated);
                WriteLog(true, 0, "Copied ", fi.FullName, " to ", strTargetPath, " ", strReasonEn);
            } catch
            {
                try
                {
                    System.Threading.Thread.Sleep(5000);
                    System.IO.FileInfo fi2 = new System.IO.FileInfo(strTargetPath2);
                    if (fi2.Exists)
                        m_iFileOpenAndCopyAbstraction.Delete(fi2);
                } catch
                {
                    // ignore additional exceptions
                }
                throw;
            }
        }

        List<KeyValuePair<string, string>> _filePairs;
        //===================================================================================================
        /// <summary>
        /// This is the main method of the background sync thread
        /// </summary>
        //===================================================================================================
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
                WriteLog(false, 0, ex.Message);
                bException = true;
            }

            if (!bException)
            {
                if (_filePairs.Count != 1)
                {
                    WriteLogFormattedLocalized(0, Resources.FoundFilesForSync, _filePairs.Count);
                    WriteLog(true, 0, "Found ", _filePairs.Count, " files for possible synchronisation");
                }
                else
                    if (_filePairs.Count == 1)
                    {
                        WriteLogFormattedLocalized(0, Resources.FoundFileForSync);
                        WriteLog(true, 0, "Found 1 file for possible synchronisation");
                    }

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
                            if (string.Compare(pathPair.Key, pathPair.Value, 
                                StringComparison.InvariantCultureIgnoreCase) < 0)
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
                        Program.SetCultureForThread(worker);
                        worker.Priority = System.Threading.ThreadPriority.Lowest;
                        worker.Start(pathPair);

                        /*/

                        try
                        {

                            ProcessFilePair(pathPair.Key, pathPair.Value);
                        }
                        catch (Exception oEx)
                        {
                            WriteLog(0, "Error while processing file pair \"", pathPair.Key, "\" | \"", pathPair.Value, "\": ", oEx.Message);
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
                {
                    WriteLogFormattedLocalized(0, Resources.OperationCanceled);
                    WriteLog(true, 0, "Operation canceled");
                }
                else
                {
                    WriteLogFormattedLocalized(0, Resources.OperationFinished);
                    WriteLog(true, 0, "Operation finished");
                }
            };

#if !DEBUG
            // restore the default abstraction, so we don't get simulated errors
            // by mistake
            m_iFileOpenAndCopyAbstraction = new FileOpenAndCopyDirectly();
#endif

            _logFile.Close();
            _logFileLocalized.Close();
            _logFile.Dispose();
            _logFileLocalized.Dispose();
            _logFile = null;
            _logFileLocalized = null;

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

        //===================================================================================================
        /// <summary>
        /// Searches directories for files and subdirectories. The collected file pairs are storedd to
        /// _filePairs
        /// </summary>
        /// <param name="strDirPath1">Path in subdir of first folder</param>
        /// <param name="strDirPath2">Path in subdir of second folder</param>
        //===================================================================================================
        void FindFilePairs(
            string strDirPath1, 
            string strDirPath2)
        {
            System.IO.DirectoryInfo di1 = new System.IO.DirectoryInfo(strDirPath1);
            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(strDirPath2);

            // don't sync recycle bin
            if (di1.Name.Equals("$RECYCLE.BIN", 
                StringComparison.InvariantCultureIgnoreCase))
                return;

            // don't sync system volume information
            if (di1.Name.Equals("System Volume Information", 
                StringComparison.InvariantCultureIgnoreCase))
                return;

            // if one of the directories exists while the oOtherSaveInfo doesn't then create the missing one
            // and set its attributes
            if (di1.Exists && !di2.Exists)
            {
                di2.Create();
                di2 = new System.IO.DirectoryInfo(strDirPath2);
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


            if (di1.Name.Equals("RestoreInfo", 
                StringComparison.CurrentCultureIgnoreCase))
                return;
            if (_bCreateInfo)
            {
                System.IO.DirectoryInfo di3;

                if (!_bFirstToSecond || !_bFirstReadOnly)
                {
                    di3 = new System.IO.DirectoryInfo(
                        System.IO.Path.Combine(strDirPath1, "RestoreInfo"));
                    if (!di3.Exists)
                    {
                        di3.Create();
                        di3 = new System.IO.DirectoryInfo(
                            System.IO.Path.Combine(strDirPath1, "RestoreInfo"));
                        di3.Attributes = di3.Attributes | System.IO.FileAttributes.Hidden;
                    }
                }

                di3 = new System.IO.DirectoryInfo(
                    System.IO.Path.Combine(strDirPath2, "RestoreInfo"));
                if (!di3.Exists)
                {
                    di3.Create();
                    di3 = new System.IO.DirectoryInfo(
                        System.IO.Path.Combine(strDirPath2, "RestoreInfo"));
                    di3.Attributes = di3.Attributes | System.IO.FileAttributes.Hidden;
                }

            }

            if (_bFirstToSecond && _bDeleteInSecond)
            {
                System.IO.FileInfo fiDontDelete = 
                    new System.IO.FileInfo(System.IO.Path.Combine(
                        _folder2, "SyncFolders-Dont-Delete.txt"));
                if (!fiDontDelete.Exists)
                    fiDontDelete = new System.IO.FileInfo(System.IO.Path.Combine(
                        _folder2, "SyncFolders-Don't-Delete.txt"));

                if (fiDontDelete.Exists)
                {
                    WriteLogFormattedLocalized(0, Resources.SecondFolderNoDelete, fiDontDelete.Name);
                    WriteLog(true, 0, "Error: The second folder contains file \"", fiDontDelete.Name, "\"," +
                        " the selected folder seem to be wrong for delete option. " +
                        "Skipping processing of the folder and subfolders");
                    return;
                }
            }


            // find files in both directories
            Dictionary<string, bool> oFileNames = new Dictionary<string, bool>();
            if (di1.Exists || !_bFirstToSecond)
            {
                foreach (System.IO.FileInfo fi1 in di1.GetFiles())
                {
                    if (fi1.Name.Length<=4 || !".tmp".Equals(
                        fi1.Name.Substring(fi1.Name.Length - 4), 
                        StringComparison.InvariantCultureIgnoreCase))
                        oFileNames[fi1.Name] = false;
                }

                foreach (System.IO.FileInfo fi2 in di2.GetFiles())
                {
                    if (fi2.Name.Length <= 4 || !".tmp".Equals(
                        fi2.Name.Substring(fi2.Name.Length - 4), 
                        StringComparison.InvariantCultureIgnoreCase))
                        oFileNames[fi2.Name] = false;
                }
            }


            foreach (string strFileName in oFileNames.Keys)
                _filePairs.Add( new KeyValuePair<string,string>(
                    System.IO.Path.Combine(strDirPath1, strFileName), 
                    System.IO.Path.Combine(strDirPath2, strFileName)));


            // find subdirectories in both directories
            Dictionary<string, bool> oDirNames = new Dictionary<string, bool>();

            if (di1.Exists || !_bFirstToSecond)
            {
                foreach (System.IO.DirectoryInfo sub1 in di1.GetDirectories())
                    oDirNames[sub1.Name] = false;

                foreach (System.IO.DirectoryInfo sub2 in di2.GetDirectories())
                    oDirNames[sub2.Name] = false;
            }

            // free the parent directory info objects
            di1 = null;
            di2 = null;

            // continue with the subdirs
            foreach (string strSubDirName in oDirNames.Keys)
            {
                FindFilePairs(System.IO.Path.Combine(strDirPath1, strSubDirName), 
                    System.IO.Path.Combine(strDirPath2, strSubDirName));
                if (_cancelClicked)
                    break;
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method is used for elimination of old .chk und .chked files that don't have original
        /// files for them anymore
        /// </summary>
        /// <param name="iAvailableFiles">Files, available in the original directory</param>
        /// <param name="fi">Fileinfo of the chk file</param>
        /// <param name="oComparer1">Simple file name comparer</param>
        /// <param name="oComparer2">Special file name comparer</param>
        /// <returns>true iff iAvailableFiles contains specified file info</returns>
        //===================================================================================================
        bool CheckIfContains(
            IEnumerable<System.IO.FileInfo> iAvailableFiles, 
            System.IO.FileInfo fi, 
            FileEqualityComparer oComparer1, 
            FileEqualityComparer2 oComparer2
            )
        {
            foreach (System.IO.FileInfo fi2 in iAvailableFiles)
                if (oComparer1.Equals(fi2, fi))
                    return true;

            foreach (System.IO.FileInfo fi2 in iAvailableFiles)
                if (oComparer2.Equals(fi2, fi))
                    return true;

            return false;
        }

        //===================================================================================================
        /// <summary>
        /// This method deletes files and folders in second folder, if there is no corresponding files and
        /// folders in first folder
        /// </summary>
        /// <param name="strFolderPath1">first folder</param>
        /// <param name="strFolderPath2">second folder</param>
        //===================================================================================================
        void RemoveOldFilesAndDirs(
            string strFolderPath1, 
            string strFolderPath2
            )
        {
            System.IO.DirectoryInfo di1 = new System.IO.DirectoryInfo(strFolderPath1);
            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo(strFolderPath2);

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
                    WriteLogFormattedLocalized(0, Resources.DeletedFolder, 
                        di2.FullName, strFolderPath1);
                    WriteLog(true, 0, "Deleted folder ", di2.FullName, 
                        " including contents, because there is no ", 
                        strFolderPath1, " anymore");
                    return;
                }
            };

            System.IO.DirectoryInfo di3;
            // consider contents of the first folder
            if (!_bFirstToSecond || !_bFirstReadOnly)
            {
                di3 = new System.IO.DirectoryInfo(
                    System.IO.Path.Combine(strFolderPath1, "RestoreInfo"));

                if (di3.Exists)
                {
                    List<System.IO.FileInfo> aAvailableFiles = new List<System.IO.FileInfo>();
                    aAvailableFiles.AddRange(di1.GetFiles());

                    FileEqualityComparer feq = new FileEqualityComparer();
                    FileEqualityComparer2 feq2 = new FileEqualityComparer2();

                    foreach (System.IO.FileInfo fi in di3.GetFiles())
                    {
                        try
                        {
                            if (fi.Extension.Equals(".chk", StringComparison.InvariantCultureIgnoreCase) && 
                                !CheckIfContains(aAvailableFiles, new System.IO.FileInfo(
                                    System.IO.Path.Combine(
                                    di3.FullName,fi.Name.Substring(0, fi.Name.Length - 4))), feq, feq2))
                            {
                                m_iFileOpenAndCopyAbstraction.Delete(fi);
                            } else
                            if (fi.Extension.Equals(".chked", StringComparison.InvariantCultureIgnoreCase) && 
                                !CheckIfContains(aAvailableFiles, new System.IO.FileInfo(
                                    System.IO.Path.Combine(
                                    di3.FullName,fi.Name.Substring(0, fi.Name.Length - 6))), feq, feq2))
                            {
                                m_iFileOpenAndCopyAbstraction.Delete(fi);
                            }
                        }
                        catch (Exception oEx)
                        {
                            try
                            {
                                WriteLogFormattedLocalized(0, Resources.ErrorDeleting,
                                    System.IO.Path.Combine(di3.FullName, fi.Name), oEx.Message);
                                WriteLog(true, 0, "Error while deleting ", 
                                    System.IO.Path.Combine(di3.FullName,fi.Name), ": ", oEx.Message);
                            }
                            catch (Exception oEx2)
                            {
                                WriteLog(false, 0, "Error in RemoveOldFilesAndDirs: ", oEx2.Message); 
                            }
                        }
                    }
                }
            }


            // consider contents of the second folder
            di3 = new System.IO.DirectoryInfo(System.IO.Path.Combine(strFolderPath2, "RestoreInfo"));
            if (di3.Exists)
            {
                List<System.IO.FileInfo> aAvailableFiles = new List<System.IO.FileInfo>();
                aAvailableFiles.AddRange(di2.GetFiles());

                FileEqualityComparer feq = new FileEqualityComparer();
                FileEqualityComparer2 feq2 = new FileEqualityComparer2();

                foreach (System.IO.FileInfo fi in di3.GetFiles())
                {
                    try
                    {
                        if (fi.Extension.Equals(".chk", StringComparison.InvariantCultureIgnoreCase) && 
                            !CheckIfContains(aAvailableFiles, new System.IO.FileInfo(
                                System.IO.Path.Combine(
                                di3.FullName, fi.Name.Substring(0, fi.Name.Length - 4))), feq, feq2))
                        {
                            m_iFileOpenAndCopyAbstraction.Delete(fi);
                        }
                        else
                        if (fi.Extension.Equals(".chked", StringComparison.InvariantCultureIgnoreCase) && 
                            !CheckIfContains(aAvailableFiles, new System.IO.FileInfo(
                                System.IO.Path.Combine(
                                di3.FullName, fi.Name.Substring(0, fi.Name.Length - 6))), feq, feq2))
                        {
                            m_iFileOpenAndCopyAbstraction.Delete(fi);
                        }
                    }
                    catch (Exception oEx)
                    {
                        try
                        {
                            WriteLogFormattedLocalized(0, Resources.ErrorDeleting,
                                System.IO.Path.Combine(di3.FullName, fi.Name),
                                oEx.Message);
                            WriteLog(true, 0, "Error while deleting ", 
                                System.IO.Path.Combine(di3.FullName, fi.Name), 
                                ": ", oEx.Message);
                        }
                        catch (Exception oEx2)
                        {
                            WriteLog(false, 0, "Error while deleting files in ", 
                                di3.FullName, ": ", oEx.Message);
                            WriteLog(false, 1, "Error while writing log: ", oEx2.Message);
                        }
                    }
                }
            }


            // find subdirectories in both directories
            Dictionary<string, bool> oDirNames = new Dictionary<string, bool>();

            if (di1.Exists)
                foreach (System.IO.DirectoryInfo diSubDir1 in di1.GetDirectories())
                    oDirNames[diSubDir1.Name] = false;

            if (di2.Exists)
                foreach (System.IO.DirectoryInfo diSubDir2 in di2.GetDirectories())
                    oDirNames[diSubDir2.Name] = false;

            // free the parent directory info objects
            di1 = null;
            di2 = null;
            di3 = null;

            // continue with the subdirs
            foreach (string strSubDirName in oDirNames.Keys)
            {
                RemoveOldFilesAndDirs(System.IO.Path.Combine(strFolderPath1, strSubDirName), 
                    System.IO.Path.Combine(strFolderPath2, strSubDirName));
                if (_cancelClicked)
                    break;
            }

        }

        //***************************************************************************************************
        /// <summary>
        /// Class for comparison of file names.
        /// </summary>
        //***************************************************************************************************
        class FileEqualityComparer : IEqualityComparer<System.IO.FileInfo>
        {
            #region IEqualityComparer<DirectoryInfo> Members

            //===============================================================================================
            /// <summary>
            /// Compares two file names
            /// </summary>
            /// <param name="fi1">First name</param>
            /// <param name="fi2">Second name</param>
            /// <returns>true iff the file names are equal case insensitively</returns>
            //===============================================================================================
            public bool Equals(
                System.IO.FileInfo fi1, 
                System.IO.FileInfo fi2
                )
            {
                if (fi1 == null || fi2 == null)
                    return fi1 == fi2;

                return string.Equals(fi1.Name, fi2.Name, 
                    StringComparison.InvariantCultureIgnoreCase);
            }

            //===================================================================================================
            /// <summary>
            /// Calcs hash code for a file name
            /// </summary>
            /// <param name="obj">File name for calc</param>
            /// <returns>Hash code</returns>
            //===================================================================================================
            public int GetHashCode(
                System.IO.FileInfo obj
                )
            {
                return (obj.Name + obj.Extension).ToUpper().GetHashCode();
            }

            #endregion
        }


        //***************************************************************************************************
        /// <summary>
        /// Class for special comparison of chk file names.
        /// </summary>
        //***************************************************************************************************
        class FileEqualityComparer2 : IEqualityComparer<System.IO.FileInfo>
        {
            #region IEqualityComparer<DirectoryInfo> Members

            //===============================================================================================
            /// <summary>
            /// Compares two file names
            /// </summary>
            /// <param name="fi1">First name</param>
            /// <param name="fi2">Second name</param>
            /// <returns>true iff the file names are considered equal</returns>
            //===============================================================================================
            public bool Equals(
                System.IO.FileInfo fi1, 
                System.IO.FileInfo fi2)
            {
                if (fi1 == null || fi2 == null)
                    return fi1 == fi2;

                return (fi1.Name[0]==fi2.Name[0]) && 
                    string.Equals(fi1.Name.Substring(0,1)+(fi1.Name.GetHashCode()), fi2.Name, 
                    StringComparison.InvariantCultureIgnoreCase);
            }

            //===============================================================================================
            /// <summary>
            /// Calculates the hash code for a name
            /// </summary>
            /// <param name="obj">The name for calc</param>
            /// <returns>Hash code</returns>
            //===================================================================================================
            public int GetHashCode(
                System.IO.FileInfo obj
                )
            {
                return (char.ToUpper(obj.Name[0])).GetHashCode();
            }

            #endregion
        }


        //===================================================================================================
        /// <summary>
        /// This method is the main entry point for processing a file pair. It runs in a separate thread.
        /// Several threads may run in parallel
        /// </summary>
        /// <param name="oFilePair">The file pair to process</param>
        void FilePairWorker(
            object oFilePair
            )
        {
            KeyValuePair<string, string> pathPair = 
                (KeyValuePair<string, string>)oFilePair;
            try
            {
                ProcessFilePair(pathPair.Key, pathPair.Value);
            }
            catch (OperationCanceledException oEx)
            {
                // report only if it is unexpected
                if (!_cancelClicked)
                {
                    WriteLogFormattedLocalized(0, Resources.ErrorProcessinngFilePair,
                        pathPair.Key, pathPair.Value, oEx.Message);
                    WriteLog(true, 0, "Error while processing file pair \"",
                        pathPair.Key, "\" | \"", pathPair.Value, "\": ", oEx.Message);
                }
            }
            catch (Exception oEx2)
            {
                WriteLogFormattedLocalized(0, Resources.ErrorProcessinngFilePair,
                        pathPair.Key, pathPair.Value, oEx2.Message);
                WriteLog(true, 0, "Error while processing file pair \"", 
                    pathPair.Key, "\" | \"", pathPair.Value, "\": ", oEx2.Message);
            }
            finally
            {
                _parallelThreads.Release();
            }
        }

        volatile int _dummy_counter;

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
        string CreatePathOfChkFile(
            string strOriginalDir, 
            string strSubDirForSavedInfo, 
            string strFileName, 
            string strNewExtension
            )
        {
            string str1 = System.IO.Path.Combine(
                System.IO.Path.Combine(strOriginalDir, strSubDirForSavedInfo), strFileName+strNewExtension);
            if (str1.Length >= 258)
            {
                str1 = System.IO.Path.Combine(
                    System.IO.Path.Combine(strOriginalDir, strSubDirForSavedInfo), 
                    strFileName.Substring(0, 1) + strFileName.GetHashCode().ToString() + strNewExtension);
                if (str1.Length >= 258)
                {
                    str1 = System.IO.Path.Combine(
                        System.IO.Path.Combine(strOriginalDir, strSubDirForSavedInfo), 
                        strFileName.Substring(0, 1) + ((++_dummy_counter).ToString()) + strNewExtension);
                }
            }
            return str1;
        }


        static volatile bool _randomOrder;

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        //===================================================================================================
        void ProcessFilePair(
            string strFilePath1, 
            string strFilePath2
            )
        {
            System.IO.FileInfo fi1 = null, fi2 = null;

            //try
            {
                fi1 = new System.IO.FileInfo(strFilePath1);
                fi2 = new System.IO.FileInfo(strFilePath2);
            }
            // this solves the problem in this place, but other problems appear down the code
            //catch (Exception oEx)
            //{
            //    string name1 = strFilePath1.Substring(strFilePath1.LastIndexOf('\\') + 1);
            //    foreach(System.IO.FileInfo fitest in new System.IO.DirectoryInfo(strFilePath1.Substring(0,strFilePath1.LastIndexOf('\\'))).GetFiles())
            //        if (fitest.Name.Equals(name1, StringComparison.InvariantCultureIgnoreCase))
            //        {
            //            fi1 = fitest;
            //            break;
            //        }
            //
            //    string name2 = strFilePath2.Substring(strFilePath1.LastIndexOf('\\') + 1);
            //    foreach (System.IO.FileInfo fitest in new System.IO.DirectoryInfo(strFilePath2.Substring(0, strFilePath2.LastIndexOf('\\'))).GetFiles())
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
                    StringComparison.InvariantCultureIgnoreCase)||
                fi1.Name.Equals("SyncFolders-Don't-Delete.txt", 
                    StringComparison.InvariantCultureIgnoreCase)||
                fi2.Name.Equals("SyncFolders-Dont-Delete.txt", 
                    StringComparison.InvariantCultureIgnoreCase) ||
                fi2.Name.Equals("SyncFolders-Don't-Delete.txt", 
                    StringComparison.InvariantCultureIgnoreCase))
            {
                WriteLogFormattedLocalized(0, Resources.SkippingFilePairDontDelete,
                    fi1.FullName, fi2.FullName);
                WriteLog(true, 0, "Skipping file pair ", fi1.FullName, 
                    ", ", fi2.FullName, 
                    ". Special file prevents usage of delete option at wrong root folder.");
                return;
            }

            if (_bFirstToSecond)
                ProcessFilePair_FirstToSecond(strFilePath1, strFilePath2, fi1, fi2);
            else
                ProcessFilePair_Bidirectionally(strFilePath1, strFilePath2, fi1, fi2);
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
            System.IO.FileInfo fi1,
            System.IO.FileInfo fi2
            )
        {
            if (_bFirstReadOnly)
                ProcessFilePair_FirstToSecond_FirstReadonly(
                    strFilePath1, strFilePath2, fi1, fi2);            
            else
                ProcessFilePair_FirstToSecond_FirstReadWrite(
                    strFilePath1, strFilePath2, fi1, fi2);            
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
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            // special case: both exist and both zero length
            if (fi1.Exists && fi2.Exists && 
                fi1.Length == 0 && fi2.Length == 0)
            {
                if (CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    if (strFilePath1.Equals(strFilePath2, StringComparison.CurrentCultureIgnoreCase))
                    {
                        WriteLogFormattedLocalized(0, Resources.FileHasZeroLength,
                            strFilePath1);
                        WriteLog(true, 0, "Warning: file has zero length, " +
                            "indicating a failed copy operation in the past: ",
                            strFilePath1);
                    }
                    else
                    {
                        WriteLogFormattedLocalized(0, Resources.FilesHaveZeroLength,
                            strFilePath1, strFilePath2);
                        WriteLog(true, 0, "Warning: both files have zero length, " +
                            "indicating a failed copy operation in the past: ",
                            strFilePath1, ", ", strFilePath2);
                    }
                }
            }
            else
            {
                if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                    ProcessFilePair_FirstToSecond_FirstReadonly_SecondExists(
                        strFilePath1, strFilePath2, fi1, fi2);
                else
                {
                    if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                        ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(
                            strFilePath1, strFilePath2, fi1, fi2);
                    else
                        ProcessFilePair_FirstToSecond_FirstReadonly_BothExist(
                            strFilePath1, strFilePath2, fi1, fi2);
                }
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only and only second file exists
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadonly_SecondExists(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            if (_bDeleteInSecond)
            {
                System.IO.FileInfo fiSavedInfo2 = 
                    new System.IO.FileInfo(CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                if (fiSavedInfo2.Exists)
                    m_iFileOpenAndCopyAbstraction.Delete(fiSavedInfo2);
                m_iFileOpenAndCopyAbstraction.Delete(fi2);
                WriteLogFormattedLocalized(0, Resources.DeletedFileNotPresentIn,
                    fi2.FullName,
                    fi1.Directory.FullName);
                WriteLog(true, 0, "Deleted file ", fi2.FullName, 
                    " that is not present in ", fi1.Directory.FullName, " anymore");
            }
            else
            {
                if (fi2.Length == 0 && CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    WriteLogFormattedLocalized(0, Resources.FileHasZeroLength,
                            strFilePath2);
                    WriteLog(true, 0, "Warning: file has zero length, "+
                        "indicating a failed copy operation in the past: ", strFilePath2);
                }

                if (_bTestFiles)
                {
                    System.IO.FileInfo fiSavedInfo2 = 
                        new System.IO.FileInfo(CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    bool bForceCreateInfo = false;
                    if (_bRepairFiles)
                        TestAndRepairSingleFile(fi2.FullName, fiSavedInfo2.FullName, ref bForceCreateInfo, false);
                    else
                        TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);

                    if (_bCreateInfo && (!fiSavedInfo2.Exists || bForceCreateInfo))
                    {
                        CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only and only first file exists
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadonly_FirstExists(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            if (fi1.Length == 0 && CheckIfZeroLengthIsInteresting(strFilePath1))
            {
                WriteLogFormattedLocalized(0, Resources.FileHasZeroLength,
                            strFilePath1);
                WriteLog(true, 0, "Warning: file has zero length, "+
                    "indicating a failed copy operation in the past: ", strFilePath1);
            }

            System.IO.FileInfo fiSavedInfo1 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            System.IO.FileInfo fiSavedInfo2 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreatInfo = false;
            bool bForceCreatInfo2 = false;
            if (_cancelClicked)
                return;
            try
            {
                _copyFiles.WaitOne();

                if (_cancelClicked)
                    return;

                CopyRepairSingleFile(fi2.FullName, fi1.FullName, fiSavedInfo1.FullName, 
                    ref bForceCreatInfo, ref bForceCreatInfo2, "(file was new)", 
                    Resources.FileWasNew, false, false);
            }
            finally
            {
                _copyFiles.Release();
            }

            if (_bCreateInfo || fiSavedInfo1.Exists || fiSavedInfo2.Exists)
            {
                if (fiSavedInfo1.Exists && !bForceCreatInfo && !bForceCreatInfo2)
                {
                    try
                    {
                        _copyFiles.WaitOne();

                        //CopyFileSafely(fiSavedInfo1, fiSavedInfo2.FullName);
                        m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    } catch 
                    {
                        CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }
                }
                else
                    CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
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
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            if (!_syncMode ? (!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
                           ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) || (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
                )
                ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2);            
            else
                ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2);
        }


        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only, both files exist and first needs to be copied over second file
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NeedToCopy(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            System.IO.FileInfo fiSavedInfo1 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            System.IO.FileInfo fiSavedInfo2 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo = false;

            // if the first file is ok
            if (TestSingleFile(strFilePath1, fiSavedInfo1.FullName,
                ref bForceCreateInfo, _bTestFiles, true, false))
            {
                if (_cancelClicked)
                    return;
                // then simply copy it
                try
                {
                    _copyFiles.WaitOne();

                    if (_cancelClicked)
                        return;

                    CopyFileSafely(fi1, strFilePath2, "(file newer or bigger)",
                        Resources.FileWasNewer);
                    //m_iFileOpenAndCopyAbstraction.CopyTo(fi1,strFilePath2, true);
                }
                finally
                {
                    _copyFiles.Release();
                }

                if (_bCreateInfo || fiSavedInfo2.Exists || fiSavedInfo1.Exists)
                {
                    if (bForceCreateInfo)
                        CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                    else
                    {
                        try
                        {
                            _copyFiles.WaitOne();
                            m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo1, CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                        }
                        finally
                        {
                            _copyFiles.Release();
                        }

                    }
                }
            }
            else
            {
                WriteLogFormattedLocalized(0, Resources.FirstFileHasBadBlocks,
                    strFilePath1, strFilePath2);
                WriteLog(true,0, "Warning: First file ", strFilePath1,
                    " has bad blocks, overwriting file ", strFilePath2,
                    " has been skipped, so the it remains as backup");
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder is read-only, both files exist and there is no obvious neeed to copy anything
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadonly_BothExist_NoNeedToCopy(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            System.IO.FileInfo fiSavedInfo2 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            System.IO.FileInfo fiSavedInfo1 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            if (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) &&
                fi1.Length == fi2.Length)
            {
                // we are in first readonly path
                // both files are present and have same modification date and lentgh

                // if the second restoreinfo is missing or has wrong date, 
                // but the other is OK then copy the one to the other
                if (fiSavedInfo1.Exists && fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc && 
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }

                    fiSavedInfo2 = new System.IO.FileInfo(CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                bool bForceCreateInfoBecauseDamaged = false;
                if (_bTestFiles)
                    if (_bRepairFiles)
                        TestAndRepairSecondFile(fi1.FullName, fi2.FullName, 
                            fiSavedInfo1.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfoBecauseDamaged);
                    else
                        TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfoBecauseDamaged, true, 
                            !_bSkipRecentlyTested, true);


                if (_bCreateInfo && 
                    (!fiSavedInfo2.Exists || 
                        fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc || 
                        bForceCreateInfoBecauseDamaged))
                {
                    CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                }

                fiSavedInfo2 = new System.IO.FileInfo(CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                fiSavedInfo1 = new System.IO.FileInfo(CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

                // if one of the files is missing or has wrong date, 
                // but the other is OK then copy the one to the other
                if (fiSavedInfo1.Exists && 
                    fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc && 
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }
                }
            } else
            {
                bool bForceCreateInfoBecauseDamaged = false;
                bool bOK = true;
                if (_bTestFiles)
                {
                    // test first file
                    TestSingleFile(strFilePath1, CreatePathOfChkFile(
                        fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), 
                        ref bForceCreateInfoBecauseDamaged, true, !_bSkipRecentlyTested, true);

                    // test or repair second file, which is different from first
                    if (_bRepairFiles)
                        TestAndRepairSingleFile(strFilePath2, CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), 
                            ref bForceCreateInfoBecauseDamaged, false);
                    else
                        bOK = TestSingleFile(strFilePath2, CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), 
                            ref bForceCreateInfoBecauseDamaged, true, !_bSkipRecentlyTested, true);

                    if (bOK && _bCreateInfo && 
                        (!fiSavedInfo2.Exists || bForceCreateInfoBecauseDamaged))
                    {
                        CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                    }
                }
            }
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
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            // special case: both exist and both zero length
            if (fi2.Exists && fi1.Exists && fi1.Length == 0 && fi2.Length == 0)
            {
                if (CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    if (strFilePath1.Equals(strFilePath2, StringComparison.CurrentCultureIgnoreCase))
                    {
                        WriteLogFormattedLocalized(0, Resources.FileHasZeroLength,
                            strFilePath1);
                        WriteLog(true, 0, "Warning: file has zero length, " +
                            "indicating a failed copy operation in the past: ", strFilePath1);
                    }
                    else
                    {
                        WriteLogFormattedLocalized(0, Resources.FilesHaveZeroLength,
                               strFilePath1, strFilePath2);
                        WriteLog(true, 0, "Warning: both files have zero length, " +
                            "indicating a failed copy operation in the past: ", strFilePath1, ", ", strFilePath2);
                    }
                }
            }
            else
            {
                if (fi2.Exists && (!fi1.Exists || fi1.Length == 0))
                    ProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists(
                        strFilePath1, strFilePath2, fi1, fi2);
                else
                {
                    if (fi1.Exists && (!fi2.Exists || fi2.Length == 0))
                        ProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists(
                            strFilePath1, strFilePath2, fi1, fi2);
                    else
                        ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist(
                            strFilePath1, strFilePath2, fi1, fi2);
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to and only file in second folder exists
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadWrite_SecondExists(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            if (_bDeleteInSecond)
            {
                System.IO.FileInfo fiSavedInfo2 = 
                    new System.IO.FileInfo(CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                if (fiSavedInfo2.Exists)
                    m_iFileOpenAndCopyAbstraction.Delete(fiSavedInfo2);
                m_iFileOpenAndCopyAbstraction.Delete(fi2);
                WriteLogFormattedLocalized(0, Resources.DeletedFileNotPresentIn,
                    fi2.FullName, fi1.Directory.FullName);
                WriteLog(true, 0, "Deleted file ", fi2.FullName, 
                    " that is not present in ", fi1.Directory.FullName, " anymore");
            }
            else
            {
                if (fi2.Length == 0 && CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    WriteLogFormattedLocalized(0, Resources.FileHasZeroLength, strFilePath2);
                    WriteLog(true, 0, "Warning: file has zero length, "+
                        "indicating a failed copy operation in the past: ", strFilePath2);
                }

                if (_bTestFiles)
                {
                    System.IO.FileInfo fiSavedInfo2 = 
                        new System.IO.FileInfo(CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    bool bForceCreateInfo = false;
                    bool bOK = true;

                    if (_bRepairFiles)
                        TestAndRepairSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfo, false);
                    else
                        bOK = TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);

                    if (bOK && _bCreateInfo && 
                        (!fiSavedInfo2.Exists || bForceCreateInfo))
                    {
                        CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to and only file in first folder exists
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadWrite_FirstExists(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            ProcessFilePair_Bidirectionally_FirstExists(strFilePath1, strFilePath2, fi1, fi2);
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
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            // first to second, but first can be written to
            if (!_syncMode ? (!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
                           ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.LastAccessTimeUtc > fi2.LastAccessTimeUtc) || (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.Length != fi2.Length)))
               )
                ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2);            
            else
                ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy(
                    strFilePath1, strFilePath2, fi1, fi2);
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
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            ProcessFilePair_Bidirectionally_BothExist_FirstNewer(strFilePath1, strFilePath2, fi1, fi2, 
                _syncMode?"(file was newer or bigger)":"(file has a different date or length)",
                _syncMode ? Resources.FileWasNewer : Resources.FileHasDifferentTime);
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in first-to-second folder mode, in case user specified
        /// that first folder can be written to, both files exist and there is no obvious reason for
        /// copying anything
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_FirstToSecond_FirstReadWrite_BothExist_NoNeedToCopy(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            // both files are present and have same modification date
            System.IO.FileInfo fiSavedInfo2 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            System.IO.FileInfo fiSavedInfo1 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // first to second, but first can be written to
            if (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) &&
                fi1.Length == fi2.Length)
            {
                ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(
                    strFilePath1, strFilePath2, fi1, fi2);
            }
            else
            {
                bool bForceCreateInfo = false;
                bool bOK = true;
                if (_bTestFiles)
                {
                    bOK = TestSingleFile(strFilePath2, CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), 
                        ref bForceCreateInfo, true, !_bSkipRecentlyTested, true);
                    if (!bOK && _bRepairFiles)
                    {
                        // first try to repair second file internally
                        if (TestSingleFileHealthyOrCanRepair(strFilePath2,
                            CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo",
                            fi2.Name, ".chk"), ref bForceCreateInfo))
                        {
                            bOK = TestAndRepairSingleFile(strFilePath2, CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"),
                                ref bForceCreateInfo, false);
                        }

                        if (bOK && bForceCreateInfo)
                        {
                            CreateSavedInfo(strFilePath2, 
                                CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                        }
                        bForceCreateInfo = false;

                        // if it didn't work, then try to repair using first file
                        if (!bOK)
                        {
                            bOK = TestSingleFile(strFilePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo, true, true, true);
                            if (!bOK)
                                bOK = TestAndRepairSingleFile(strFilePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo, true);

                            if (bOK && bForceCreateInfo)
                            {
                                bOK = CreateSavedInfo(strFilePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                                bForceCreateInfo = false;
                            }

                            if (bOK)
                            {
                                if (fi1.LastWriteTime.Year > 1975)
                                    CopyFileSafely(fi1, strFilePath2,
                                        "(file was healthy, or repaired)",
                                        Resources.FileHealthyOrRepaired);
                                else
                                {
                                    WriteLogFormattedLocalized(0, Resources.CouldntUseOutdatedFileForRestoringOther,
                                        strFilePath1, strFilePath2);
                                    WriteLog(true, 0, "Warning: couldn't use outdated file ",
                                        strFilePath1, " with year 1975 or earlier for restoring ",
                                        strFilePath2, ", signaling this was a last chance restore");
                                }
                            }
                        }
                    }
                    else
                    {
                        // second file was OK, or no repair option, still need to process first file
                        bOK = TestSingleFile(strFilePath1, CreatePathOfChkFile(
                            fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), 
                            ref bForceCreateInfo, true, true, true);

                        if (!bOK && _bRepairFiles)
                        {
                            if (TestAndRepairSingleFile(strFilePath1, CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), ref bForceCreateInfo, true))
                            {
                                bOK = true;
                            }

                            if (bOK && bForceCreateInfo)
                            {
                                CreateSavedInfo(strFilePath1, CreatePathOfChkFile(
                                    fi1.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                            }
                            bForceCreateInfo = false;

                            // if it didn't work, then try to repair using second file
                            if (!bOK)
                            {
                                bOK = TestSingleFile(strFilePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo, true, true, true);
                                if (!bOK)
                                    bOK = TestAndRepairSingleFile(strFilePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), ref bForceCreateInfo, true);

                                if (bOK && bForceCreateInfo)
                                {
                                    bOK = CreateSavedInfo(strFilePath2, CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                                    bForceCreateInfo = false;
                                }

                                if (bOK)
                                {
                                    if (fi2.LastWriteTime.Year > 1975)
                                    {
                                        CopyFileSafely(fi2, strFilePath1, "(file was healthy, or repaired)",
                                            Resources.FileHealthyOrRepaired);
                                    }
                                    else
                                    {
                                        WriteLogFormattedLocalized(0, Resources.CouldntUseOutdatedFileForRestoringOther,
                                            strFilePath2, strFilePath1);
                                        WriteLog(true, 0, "Warning: couldn't use outdated file ", strFilePath2,
                                            " with year 1975 or earlier for restoring ",
                                            strFilePath1, ", signaling this was a last chance restore");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method identifies files that are interesting for messages about zero length and failed
        /// copy
        /// </summary>
        /// <param name="strFilePath">Path of the file for testing, if extension is interesting</param>
        /// <returns>true iff a message about zero-length of the file is desired</returns>
        //===================================================================================================
        bool CheckIfZeroLengthIsInteresting(
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
                   strFilePath.EndsWith(".m2ts", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".heic", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".avi", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".wmv", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".jp2", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".wma", StringComparison.InvariantCultureIgnoreCase) ||
                   strFilePath.EndsWith(".flac", StringComparison.InvariantCultureIgnoreCase);
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
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2)
        {
            // special case: both exist and both zero length
            if (fi1.Exists && fi2.Exists && 
                fi1.Length == 0 && fi2.Length == 0)
            {
                if (CheckIfZeroLengthIsInteresting(strFilePath2))
                {
                    if (strFilePath1.Equals(strFilePath2,
                        StringComparison.CurrentCultureIgnoreCase))
                    {
                        WriteLogFormattedLocalized(0, Resources.FileHasZeroLength, strFilePath1);
                        WriteLog(true, 0, "Warning: file has zero length, " +
                            "indicating a failed copy operation in the past: ", strFilePath1);
                    }
                    else
                    {
                        WriteLogFormattedLocalized(0, Resources.FilesHaveZeroLength, strFilePath1, strFilePath2);
                        WriteLog(true, 0, "Warning: both files have zero length, " +
                            "indicating a failed copy operation in the past: ", strFilePath1, ", ", strFilePath2);
                    }
                }
            }
            else
                if (fi1.Exists && (!fi2.Exists || fi2.Length==0))
                    ProcessFilePair_Bidirectionally_FirstExists(
                        strFilePath1, strFilePath2, fi1, fi2);            
                else
                {
                    if (fi2.Exists && (!fi1.Exists || fi1.Length==0))
                       ProcessFilePair_Bidirectionally_SecondExists(
                           strFilePath1, strFilePath2, fi1, fi2);
                    else
                       ProcessFilePair_Bidirectionally_BothExist(
                           strFilePath1, strFilePath2, fi1, fi2);
                }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case only first of
        /// the two files exists
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally_FirstExists(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            if (fi1.Length == 0 && CheckIfZeroLengthIsInteresting(strFilePath1))
            {
                WriteLogFormattedLocalized(0, Resources.FileHasZeroLength, strFilePath1);
                WriteLog(true, 0, "Warning: file has zero length, " + 
                    "indicating a failed copy operation in the past: ", strFilePath1);
            }

            System.IO.FileInfo fiSavedInfo1 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            bool bForceCreateInfo = false;
            bool bForceCreateInfo2 = false;
            bool bInTheEndOK = true;

            try
            {
                _copyFiles.WaitOne();

                if (_cancelClicked)
                    return;

                if (_bCreateInfo && 
                    (!fiSavedInfo1.Exists || 
                        fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc || 
                        bForceCreateInfo))
                {
                    if (!CreateSavedInfoAndCopy(
                        fi1.FullName, fiSavedInfo1.FullName, 
                        fi2.FullName, "(file was new)",
                        Resources.FileWasNew))
                    {
                        CopyRepairSingleFile(strFilePath2, strFilePath1, 
                            fiSavedInfo1.FullName, ref bForceCreateInfo,
                            ref bForceCreateInfo2, "(file was new)",
                            Resources.FileWasNew, false, false);
                        CreateSavedInfo(strFilePath2, 
                            CreatePathOfChkFile(fi2.DirectoryName, 
                            "RestoreInfo", fi2.Name, ".chk"));
                        return;
                    }

                    fiSavedInfo1 = new System.IO.FileInfo(
                        CreatePathOfChkFile(fi1.DirectoryName, 
                        "RestoreInfo", fi1.Name, ".chk"));
                    bForceCreateInfo = false;
                }
                else
                {
                    try
                    {
                        if (_bTestFiles)
                            CopyRepairSingleFile(strFilePath2, fi1.FullName, 
                                fiSavedInfo1.FullName, ref bForceCreateInfo, ref bForceCreateInfo2, 
                                "(file was new)", Resources.FileWasNew, true, _bRepairFiles);
                        else
                            CopyFileSafely(fi1, strFilePath2, "(file was new)", Resources.FileWasNew);
                    }
                    catch (Exception)
                    {
                        WriteLogFormattedLocalized(0, Resources.EncounteredErrorWhileCopyingTryingToRepair,
                            fi1.FullName);
                        WriteLog(true, 0, "Warning: Encountered error while copying ", 
                            fi1.FullName, ", trying to automatically repair");
                        if (_bTestFiles && _bRepairFiles)
                            TestAndRepairSingleFile(fi1.FullName, 
                                fiSavedInfo1.FullName, ref bForceCreateInfo, false);
                        if (bInTheEndOK)
                            bInTheEndOK = CopyRepairSingleFile(strFilePath2, 
                                fi1.FullName, fiSavedInfo1.FullName, 
                                ref bForceCreateInfo, ref bForceCreateInfo2, 
                                "(file was new)", Resources.FileWasNew, false, _bTestFiles && _bRepairFiles);
                    }
                }


                if (bInTheEndOK)
                {
                    if (_bCreateInfo || fiSavedInfo1.Exists)
                    {
                        if (!fiSavedInfo1.Exists || bForceCreateInfo || 
                            fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc)
                        {
                            CreateSavedInfo(strFilePath1, 
                                CreatePathOfChkFile(fi1.DirectoryName, 
                                "RestoreInfo", fi1.Name, ".chk"));
                            fiSavedInfo1 = new System.IO.FileInfo(
                                CreatePathOfChkFile(fi1.DirectoryName, 
                                "RestoreInfo", fi1.Name, ".chk"));
                        }

                        if (bForceCreateInfo2)
                            CreateSavedInfo(strFilePath2, 
                                CreatePathOfChkFile(fi2.DirectoryName, 
                                "RestoreInfo", fi2.Name, ".chk"));
                        else
                            if (fiSavedInfo1.Exists)
                                m_iFileOpenAndCopyAbstraction.CopyTo(
                                    fiSavedInfo1, CreatePathOfChkFile(
                                    fi2.DirectoryName, "RestoreInfo", 
                                    fi2.Name, ".chk"), true);
                    }
                }
            }
            finally
            {
                _copyFiles.Release();
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case only second of
        /// the two files exists
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally_SecondExists(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            // symmetric situation
            ProcessFilePair_Bidirectionally_FirstExists(
                strFilePath2, strFilePath1, fi2, fi1);
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
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            // bidirectionally path
            if ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc)) || 
                (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.Length > fi2.Length))
                ProcessFilePair_Bidirectionally_BothExist_FirstNewer(strFilePath1, strFilePath2, fi1, fi2, 
                    "(file newer or bigger)", Resources.FileWasNewer);
            else
            {
                // bidirectionally path
                if ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi2.LastWriteTimeUtc > fi1.LastWriteTimeUtc)) || 
                    (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi2.Length > fi1.Length))
                    ProcessFilePair_Bidirectionally_BothExist_SecondNewer(strFilePath1, strFilePath2, fi1, fi2);
                else
                    ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(strFilePath1, strFilePath2, fi1, fi2);
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist and first file has a more recent date
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally_BothExist_FirstNewer(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2,
            string strReasonEn,
            string strReasonTranslated)
        {
            System.IO.FileInfo fiSavedInfo1 = 
                new System.IO.FileInfo(CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            System.IO.FileInfo fiSavedInfo2 = 
                new System.IO.FileInfo(CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

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

                if (_bCreateInfo && 
                    (!fiSavedInfo1.Exists || 
                        fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc || 
                        bForceCreateInfo1))
                {
                    bCopied1To2 = CreateSavedInfoAndCopy(
                        fi1.FullName, fiSavedInfo1.FullName, fi2.FullName, 
                        strReasonEn, strReasonTranslated);
                    fiSavedInfo1 = new System.IO.FileInfo(
                        CreatePathOfChkFile(fi1.DirectoryName, 
                        "RestoreInfo", fi1.Name, ".chk"));

                    if (bCopied1To2)
                        bForceCreateInfo1 = false;
                }
                else
                {
                    try
                    {
                        if (_bTestFiles)
                            CopyRepairSingleFile(strFilePath2, fi1.FullName, fiSavedInfo1.FullName, 
                                ref bForceCreateInfo1, ref bForceCreateInfo2, 
                                "(file was new)", Resources.FileWasNew, true, _bRepairFiles);
                        else
                            CopyFileSafely(fi1, strFilePath2, strReasonEn, strReasonTranslated);
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
                        WriteLogFormattedLocalized(0, Resources.RunningWithoutRepairOptionUndecided,
                            fi1.FullName, fi2.FullName);
                        WriteLog(true, 0, "Running without repair option, "
                        + "so couldn't decide, if the file ",  
                        fi1.FullName, " can be restored using ", fi2.FullName);
                        // first failed,  still need to test the second
                        if (_bTestFiles)
                        {
                            TestSingleFileHealthyOrCanRepair(strFilePath2, fiSavedInfo2.FullName, ref bForceCreateInfo2);
                        }
                        return;
                    }

                    // first try to copy the first/needed file, if it can be restored
                    if (TestSingleFileHealthyOrCanRepair(strFilePath1, fiSavedInfo1.FullName, ref bForceCreateInfo1) &&
                        TestAndRepairSingleFile(strFilePath1, fiSavedInfo1.FullName, ref bForceCreateInfo1, true))
                    {
                        if (bForceCreateInfo1)
                        {
                            bCopied1To2 = CreateSavedInfoAndCopy(
                                fi1.FullName, fiSavedInfo1.FullName, fi2.FullName, strReasonEn, strReasonTranslated);
                            fiSavedInfo1 = new System.IO.FileInfo(
                                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                            bForceCreateInfo1 = false;
                        }
                        else
                        {
                            CopyFileSafely(fi1, strFilePath2, strReasonEn, strReasonTranslated);
                            bCopied1To2 = true;
                        }
                    }

                    if (!bCopied1To2)
                    {
                        // well, then try the second, older file. Let's see if it is OK, or can be restored in place
                        if (TestAndRepairSingleFile(strFilePath2, fiSavedInfo2.FullName, ref bForceCreateInfo2, true)
                             && fi2.LastWriteTime.Year>1975)
                        {
                            WriteLogFormattedLocalized(0, Resources.EncounteredErrorOlderOk,
                                fi1.FullName, strFilePath2);
                            WriteLog(true, 0, "Warning: Encountered I/O error while copying ", 
                                fi1.FullName, ". The older file ", strFilePath2, " seems to be OK");
                            bCopied1To2 = false;
                            bCopy2To1 = true;
                        }
                        else
                        {
                            WriteLogFormattedLocalized(0, Resources.EncounteredErrorOtherBadToo,
                                fi1.FullName, strFilePath2);
                            WriteLog(true, 0, "Warning: Encountered I/O error while copying ", fi1.FullName, 
                                ". Other file has errors as well: ", strFilePath2, 
                                ", or is a product of last chance restore, trying to automatically repair ", 
                                strFilePath1);
                            TestAndRepairSingleFile(fi1.FullName, fiSavedInfo1.FullName, ref bForceCreateInfo1, false);
                            bForceCreateInfo2 = false;

                            CopyRepairSingleFile(strFilePath2, fi1.FullName, fiSavedInfo1.FullName, 
                                ref bForceCreateInfo1, ref bForceCreateInfo2, strReasonEn, strReasonTranslated, false, true);
                            bCopied1To2 = true;
                        }
                    }
                };

                if (bCopied1To2)
                {
                    if ((_bCreateInfo && 
                        (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc)) || 
                        (fiSavedInfo1.Exists && bForceCreateInfo1))
                    {
                        CreateSavedInfo(strFilePath1, 
                            CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                        fiSavedInfo1 = new System.IO.FileInfo(
                            CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    }

                    if (fiSavedInfo1.Exists)
                    {
                        if (bForceCreateInfo2)
                        {
                            if (_bCreateInfo || fiSavedInfo2.Exists)
                                CreateSavedInfo(strFilePath2, CreatePathOfChkFile(
                                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                        }
                        else
                            m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo1, CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                    }

                    return;
                }


                if (!bCopy2To1)
                    return;

                if (!_bTestFiles || !_bRepairFiles)
                {
                    WriteLogFormattedLocalized(0, Resources.RunningWithoutRepairOptionUndecided,
                        fi1.FullName, fi2.FullName);
                    WriteLog(true, 0, "Running without repair option, so couldn't decide, " +
                    "if the file ", fi1.FullName, " can be restored using ", fi2.FullName);
                    return;
                }

                // there we try to restore the older file 2, since it seems to be OK, while newer file 1 failed.
                fiSavedInfo2 = new System.IO.FileInfo(
                    CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                bForceCreateInfo2 = false;

                if (_bCreateInfo && 
                    (!fiSavedInfo2.Exists || 
                        fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc || 
                        bForceCreateInfo2))
                {
                    bCopied2To1 = CreateSavedInfoAndCopy(
                        fi2.FullName, fiSavedInfo2.FullName, strFilePath1, 
                        "(file was healthy)", Resources.FileWasHealthy);
                    fiSavedInfo2 = new System.IO.FileInfo(
                        CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

                    if (bCopied2To1)
                        bForceCreateInfo2 = false;
                    else
                    {
                        // should actually never happen, since we go there only if file 2 could be restored above
                        WriteLogFormattedLocalized(0, Resources.InternalErrorCouldntRestoreAny,
                            fi1.FullName, fi2.FullName);
                        WriteLog(true, 0, "Internal error: Couldn't "+
                            "restore any of the copies of the file ", fi1.FullName, ", ", fi2.FullName);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        CopyRepairSingleFile(strFilePath1, strFilePath2, fiSavedInfo2.FullName,
                            ref bForceCreateInfo2, ref bForceCreateInfo1, 
                            "(file was healthy)", Resources.FileWasHealthy, true, true);
                    }
                    catch (Exception)
                    {
                        // should actually never happen, since we go there only if file 2 could be restored above
                        WriteLogFormattedLocalized(0, Resources.InternalErrorCouldntRestoreAny,
                            fi1.FullName, fi2.FullName);

                        WriteLog(true, 0, "Internal error: Couldn't "+
                        "restore any of the copies of the file ", fi1.FullName, ", ", fi2.FullName);
                        return;
                    }
                }


                if ((_bCreateInfo && 
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc!=fi2.LastWriteTimeUtc)) || 
                    (fiSavedInfo2.Exists && bForceCreateInfo2))
                {
                    CreateSavedInfo(strFilePath2, 
                        CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    fiSavedInfo2 = new System.IO.FileInfo(
                        CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                if (fiSavedInfo2.Exists)
                {
                    if (bForceCreateInfo1)
                    {
                        if (_bCreateInfo || fiSavedInfo1.Exists)
                            CreateSavedInfo(strFilePath1, 
                                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    }
                    else
                        m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo2, 
                            CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), true);
                }
            }
            finally
            {
                _copyFiles.Release();
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist and second file has a more recent date
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally_BothExist_SecondNewer(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2
            )
        {
            ProcessFilePair_Bidirectionally_BothExist_FirstNewer(
                strFilePath2, strFilePath1, fi2, fi1, 
                "(file was newer or bigger)", Resources.FileWasNewer);
        }

        //===================================================================================================
        /// <summary>
        /// This method processes a file pair in bidirectional folder mode (default), in case both files
        /// exist and seem to have same last-write-time and length
        /// </summary>
        /// <param name="strFilePath1">first file</param>
        /// <param name="strFilePath2">second file</param>
        /// <param name="fi1">The file information about first file</param>
        /// <param name="fi2">The file information about second file</param>
        //===================================================================================================
        void ProcessFilePair_Bidirectionally_BothExist_AssumingBothEqual(
            string strFilePath1, 
            string strFilePath2, 
            System.IO.FileInfo fi1, 
            System.IO.FileInfo fi2)
        {
            // bidirectionally path
            // both files are present and have same modification date
            System.IO.FileInfo fiSavedInfo2 = 
                new System.IO.FileInfo(CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));;
            System.IO.FileInfo fiSavedInfo1 = 
                new System.IO.FileInfo(CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the restoreinfo files is missing or has wrong date, 
            // but the oOtherSaveInfo is OK then copy the one to the oOtherSaveInfo
            if (fiSavedInfo1.Exists && 
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc && 
                (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    _copyFiles.WaitOne();
                    m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                }
                finally
                {
                    _copyFiles.Release();
                }

                fiSavedInfo2 = new System.IO.FileInfo(
                    CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            }
            else
                if (fiSavedInfo2.Exists && 
                    fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc && 
                    (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo2, fiSavedInfo1.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }

                    fiSavedInfo1 = new System.IO.FileInfo(
                        CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
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
                        bTotalResultOk = TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, true, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            bTotalResultOk = bTotalResultOk && TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, true, false);
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        bTotalResultOk = TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, true, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            bTotalResultOk = bTotalResultOk && TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, true, false);
                        else
                        {
                            bCreateInfo1 = bCreateInfo2;
                            bCreateInfo2 = false;
                        }
                    }

                    if (!bTotalResultOk)
                    {
                        TestAndRepairTwoFiles(fi1.FullName, fi2.FullName, fiSavedInfo1.FullName, fiSavedInfo2.FullName, ref bCreateInfo);
                        bCreateInfo1 = bCreateInfo;
                        bCreateInfo2 = bCreateInfo;
                    }
                }
                else
                {
                    if (bFirstOrSecond)
                    {
                        TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, false, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, false, false);
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !_bSkipRecentlyTested, true, false, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !_bSkipRecentlyTested, true, false, false);
                        else
                            bCreateInfo1 = false;
                    }

                    //TestSingleFile(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1);
                    //TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2);
                }
            }

            if (_bCreateInfo && (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc || bCreateInfo1))
            {
                CreateSavedInfo(fi1.FullName, fiSavedInfo1.FullName);
                if (fiSavedInfo1.FullName.Equals(fiSavedInfo2.FullName, StringComparison.InvariantCultureIgnoreCase))
                    fiSavedInfo2 = new System.IO.FileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            }

            if (_bCreateInfo && (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc || bCreateInfo2))
            {
                CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
            }


            fiSavedInfo2 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            fiSavedInfo1 = new System.IO.FileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the files is missing or has wrong date, but the oOtherSaveInfo is OK then copy the one to the oOtherSaveInfo
            if (fiSavedInfo1.Exists && 
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc && 
                (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    _copyFiles.WaitOne();
                    m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                }
                finally
                {
                    _copyFiles.Release();
                }
            }
            else
                if (fiSavedInfo2.Exists && 
                    fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc && 
                    (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
                {
                    try
                    {
                        _copyFiles.WaitOne();
                        m_iFileOpenAndCopyAbstraction.CopyTo(fiSavedInfo2, fiSavedInfo1.FullName, true);
                    }
                    finally
                    {
                        _copyFiles.Release();
                    }
                }
        }

        //===================================================================================================
        /// <summary>
        /// This method combines copying of a file with creation of SavedInfo (.chk) file. So there is no
        /// need to read a big data file twice.
        /// </summary>
        /// <param name="strPathFile">The source path for copy</param>
        /// <param name="strTargetPath">The target path for copy</param>
        /// <param name="strPathSavedInfoFile">The target path for saved info</param>
        /// <param name="strReason">The reason of copy for messages</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        bool CreateSavedInfoAndCopy(
            string strPathFile, 
            string strPathSavedInfoFile, 
            string strTargetPath,
            string strReasonEn,
            string strReasonTranslated)
        {
            string pathFileCopy = strTargetPath + ".tmp";


            System.IO.FileInfo finfo = new System.IO.FileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, finfo.LastWriteTimeUtc, false);
            try
            {
                using (System.IO.BufferedStream s = 
                    new System.IO.BufferedStream(m_iFileOpenAndCopyAbstraction.OpenRead(finfo.FullName), 
                        (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    try
                    {
                        using (System.IO.BufferedStream s2 = new
                            System.IO.BufferedStream(System.IO.File.Create(pathFileCopy), 
                            (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
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

                            System.IO.FileInfo fi2 = new System.IO.FileInfo(strTargetPath);
                            if (fi2.Exists)
                                m_iFileOpenAndCopyAbstraction.Delete(fi2);

                            fi2tmp.MoveTo(strTargetPath);

                            WriteLogFormattedLocalized(0, Resources.CopiedFromToReason,
                                strPathFile, strTargetPath, strReasonTranslated);
                            WriteLog(true, 0, "Copied ", strPathFile, " to ", strTargetPath, " ", strReasonEn);
                        }
                    } catch
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(5000);
                            System.IO.FileInfo finfoCopy = new System.IO.FileInfo(pathFileCopy);
                            if (finfoCopy.Exists)
                                m_iFileOpenAndCopyAbstraction.Delete(finfoCopy);
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
                WriteLogFormattedLocalized(0, Resources.WarningIOErrorWhileCopyingToReason,
                    finfo.FullName, strTargetPath, ex.Message);
                WriteLog(true, 0, "Warning: I/O Error while copying file: \"", 
                    finfo.FullName, "\" to \"", strTargetPath, "\": " + ex.Message);
                return false;
            }

            try
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(
                    strPathSavedInfoFile.Substring(0, 
                    strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                if (!di.Exists)
                {
                    di.Create();
                    di = new System.IO.DirectoryInfo(
                        strPathSavedInfoFile.Substring(0, 
                        strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden;
                }
                using (System.IO.FileStream s = System.IO.File.Create(strPathSavedInfoFile))
                {
                    si.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                System.IO.FileInfo fiSavedInfo = new System.IO.FileInfo(strPathSavedInfoFile);
                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

            }
            catch (System.IO.IOException ex)
            {
                WriteLogFormattedLocalized(0, Resources.IOErrorWritingFile, strPathSavedInfoFile, ex.Message);
                WriteLog(true, 0, "I/O Error writing file: \"", strPathSavedInfoFile, "\": " + ex.Message);
                return false;
            }

            // we just created the file, so assume we checked everything, no need to double-check immediately
            CreateOrUpdateFileChecked(strPathSavedInfoFile);

            return true;
        }

        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        bool CreateSavedInfo(
            string strPathFile, 
            string strPathSavedChkInfoFile
            )
        {
            System.IO.FileInfo finfo = new System.IO.FileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, finfo.LastWriteTimeUtc, false);
            try
            {
                using (System.IO.BufferedStream s =
                    new System.IO.BufferedStream(m_iFileOpenAndCopyAbstraction.OpenRead(finfo.FullName), 
                        (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
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
                WriteLogFormattedLocalized(0, Resources.IOErrorReadingFile,
                    finfo.FullName, ex.Message);
                WriteLog(true, 0, "I/O Error reading file: \"", 
                    finfo.FullName, "\": " + ex.Message);
                return false;
            }

            try
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(
                    strPathSavedChkInfoFile.Substring(0, 
                    strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                if (!di.Exists)
                {
                    di.Create();
                    di = new System.IO.DirectoryInfo(
                        strPathSavedChkInfoFile.Substring(0, 
                        strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden;
                }

                System.IO.FileInfo fiSavedInfo = new System.IO.FileInfo(strPathSavedChkInfoFile);
                if (fiSavedInfo.Exists)
                {
                    m_iFileOpenAndCopyAbstraction.Delete(fiSavedInfo);
                }

                using (System.IO.FileStream s = System.IO.File.Create(strPathSavedChkInfoFile, 
                    1024*1024))
                {
                    si.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                fiSavedInfo = new System.IO.FileInfo(strPathSavedChkInfoFile);
                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                CreateOrUpdateFileChecked(strPathSavedChkInfoFile);

            } catch (System.IO.IOException ex)
            {
                WriteLogFormattedLocalized(0, Resources.IOErrorWritingFile,
                    strPathSavedChkInfoFile, ex.Message);
                WriteLog(true, 0, "I/O Error writing file: \"", 
                    strPathSavedChkInfoFile, "\": " + ex.Message);
                return false;
            }
            return true;
        }

        //===================================================================================================
        /// <summary>
        /// This method tests a single file, together with its saved info, if present
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bNeedsMessageAboutOldSavedInfo">Specifies, if method shall add a message
        /// in case saved info is outdated or wrong</param>
        /// <param name="bForcePhysicalTest">If set to false, method will analyze the last date and
        /// time the original file has been tested or copied and skip physical test, if possible</param>
        /// <param name="bCreateConfirmationFile">If test succeeds then information about succeeded
        /// test is saved in file system</param>
        /// <returns>true iff the test succeeded</returns>
        //===================================================================================================
        bool TestSingleFile(
            string strPathFile, 
            string strPathSavedInfoFile, 
            ref bool bForceCreateInfo, 
            bool bNeedsMessageAboutOldSavedInfo, 
            bool bForcePhysicalTest, 
            bool bCreateConfirmationFile)
        {
            return TestSingleFile2(strPathFile, strPathSavedInfoFile, ref bForceCreateInfo, 
                bNeedsMessageAboutOldSavedInfo, bForcePhysicalTest, bCreateConfirmationFile, 
                false, false);
        }

        //===================================================================================================
        /// <summary>
        /// This method tests a single file, together with its saved info, if present
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bNeedsMessageAboutOldSavedInfo">Specifies, if method shall add a message
        /// in case saved info is outdated or wrong</param>
        /// <param name="bForcePhysicalTest">If set to false, method will analyze the last date and
        /// time the original file has been tested or copied and skip physical test, if possible</param>
        /// <param name="bCreateConfirmationFile">If test succeeds then information about succeeded
        /// test is saved in file system</param>
        /// <param name="bFailASAPwoMessage">If set to true method silently exits on first error</param>
        /// <param name="bReturnFalseIfNonRecoverableNotIfDamaged">Usually a test returns true if
        /// the file is healthy, but if this flag is set to true then method will also return true, 
        /// if the file can be completely restored using saved info</param>
        /// <returns>true iff the test succeeded</returns>
        //===================================================================================================
        bool TestSingleFile2(
            string pathFile, 
            string strPathSavedInfoFile, 
            ref bool bForceCreateInfo, 
            bool bNeedsMessageAboutOldSavedInfo, 
            bool bForcePhysicalTest, 
            bool bCreateConfirmationFile,
            bool bFailASAPwoMessage, 
            bool bReturnFalseIfNonRecoverableNotIfDamaged
            )
        {
            System.IO.FileInfo finfo = 
                new System.IO.FileInfo(pathFile);
            System.IO.FileInfo fiSavedInfo = 
                new System.IO.FileInfo(strPathSavedInfoFile);
            bool bSkipBufferedFile = false;

            try
            {
                if (!bForcePhysicalTest)
                {
                    System.IO.FileInfo fichecked = 
                        new System.IO.FileInfo(strPathSavedInfoFile + "ed");
                    // this randomly skips testing of files,
                    // so the user doesn't have to wait long, when performing checks annually:
                    // 100% of files are skipped within first 2 years after last check
                    // 0% after 7 years after last check
                    if (fichecked.Exists && finfo.Exists &&
                        (!fiSavedInfo.Exists || fiSavedInfo.LastWriteTimeUtc == finfo.LastWriteTimeUtc) &&
                        fichecked.LastWriteTimeUtc.CompareTo(finfo.LastWriteTimeUtc) > 0 &&
                        Math.Abs(DateTime.UtcNow.Year * 366 + DateTime.UtcNow.DayOfYear 
                            - fichecked.LastWriteTimeUtc.Year * 366 - fichecked.LastWriteTimeUtc.DayOfYear) 
                        < 366 * 2.2 + 366 * 4.6 * _randomizeChecked.NextDouble())
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLogFormattedLocalized(1, Resources.WarningWhileDiscoveringIfNeedsToBeRechecked,
                    ex.Message, pathFile);
                WriteLog(true, 1, "Warning: ", ex.Message, 
                    " while discovering, if ", pathFile, 
                    " needs to be rechecked.");
            }

        repeat:
            SavedInfo si = new SavedInfo();
            bool bSaveInfoUnreadable = !fiSavedInfo.Exists;
            if (!bSaveInfoUnreadable)
            {
                try
                {
                    using (System.IO.BufferedStream s =
                        new System.IO.BufferedStream(
                            m_iFileOpenAndCopyAbstraction.OpenRead(strPathSavedInfoFile), 
                            (int)Math.Min(fiSavedInfo.Length + 1, 32 * 1024 * 1024)))
                    {
                        si.ReadFrom(s);
                        s.Close();
                    }
                }
                catch // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (System.IO.FileStream s =
                            m_iFileOpenAndCopyAbstraction.OpenRead(strPathSavedInfoFile))
                        {
                            si.ReadFrom(s);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        WriteLogFormattedLocalized(0, Resources.IOErrorReadingFile,
                            strPathSavedInfoFile, ex.Message);
                        WriteLog(true, 0, "I/O Error reading file: \"", 
                            strPathSavedInfoFile, "\": " + ex.Message);
                        bSaveInfoUnreadable = true;
                        bForceCreateInfo = true;
                        bForcePhysicalTest = true;
                    }
                }
            }


            if (bSaveInfoUnreadable || si.Length != finfo.Length || 
                !FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc))
            {
                bool bAllBlocksOK = true;

                bForceCreateInfo = true;
                if (!bSaveInfoUnreadable)
                    if (bNeedsMessageAboutOldSavedInfo)
                    {
                        WriteLogFormattedLocalized(0, Resources.SavedInfoFileCantBeUsedForTesting,
                            strPathSavedInfoFile, pathFile);
                        WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                            "\" can't be used for testing file \"", pathFile,
                            "\": it was created for another version of the file");
                    }

                Block b = Block.GetBlock();
                try
                {
                    using (System.IO.BufferedStream s =
                        new System.IO.BufferedStream(
                            m_iFileOpenAndCopyAbstraction.OpenRead(finfo.FullName), 
                            (int)Math.Min(finfo.Length + 1, 32 * 1024 * 1024)))
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

                    using (System.IO.FileStream s =
                        m_iFileOpenAndCopyAbstraction.OpenRead(finfo.FullName))
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
                                WriteLogFormattedLocalized(0, Resources.IOErrorReadingFileOffset,
                                    finfo.FullName, index * b.Length, ex.Message);
                                WriteLog(true, 0, "I/O Error reading file: \"",
                                    finfo.FullName, "\", offset ", 
                                    index * b.Length, ": " + ex.Message);
                                s.Seek((index + 1) * b.Length, 
                                    System.IO.SeekOrigin.Begin);
                                bAllBlocksOK = false;
                            }
                        }
                        s.Close();
                    }
                }
                Block.ReleaseBlock(b);

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile);
                }

                return bAllBlocksOK;
            }


            try
            {
                long nonRestoredSize = 0;
                bool bAllBlocksOK = true;

                System.IO.Stream s = 
                    m_iFileOpenAndCopyAbstraction.OpenRead(finfo.FullName);
                if (!bSkipBufferedFile)
                    s = new System.IO.BufferedStream(s, 
                        (int)Math.Min(finfo.Length + 1, 8 * 1024 * 1024));

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

                                    WriteLogFormattedLocalized(1,
                                        Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName,
                                        index * b.Length);

                                    WriteLog(true, 1, finfo.FullName, 
                                        ": checksum of block at offset ", 
                                        index * b.Length, " not OK");
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

                                        WriteLogFormattedLocalized(1,
                                            Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName,
                                            index * b.Length);

                                        WriteLog(true, 1, finfo.FullName, 
                                            ": checksum of block at offset ", 
                                            index * b.Length, " not OK");
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

                            bAllBlocksOK = false;

                            WriteLogFormattedLocalized(1, Resources.IOErrorReadingFileOffset,
                                finfo.FullName, ex.Message);
                            WriteLog(true, 1, "I/O Error reading file: \"", 
                                finfo.FullName, "\", offset ", 
                                index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length, 
                                System.IO.SeekOrigin.Begin);
                        }
                    };
                    Block.ReleaseBlock(b);

                    List<RestoreInfo> ri = si.EndRestore(out nonRestoredSize, fiSavedInfo.FullName, this);
                    if (ri.Count > 1)
                    {
                        WriteLogFormattedLocalized(0, Resources.ThereAreBadBlocksNonRestorableOnlyTested,
                            ri.Count, finfo.FullName, nonRestoredSize);
                        WriteLog(true, 0, "There are ", ri.Count, " bad blocks in the file ",
                            finfo.FullName, ", non-restorable parts: ", nonRestoredSize,
                            " bytes, file remains unchanged, it was only tested");
                    }
                    else
                        if (ri.Count > 0)
                        {
                            WriteLogFormattedLocalized(0, Resources.ThereIsOneBadBlockNonRestorableOnlyTested,
                                finfo.FullName, nonRestoredSize);
                            WriteLog(true, 0, "There is one bad block in the file ", finfo.FullName,
                                ", non-restorable parts: ", nonRestoredSize,
                                " bytes, file remains unchanged, it was only tested");
                        }

                    s.Close();
                };

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file 
                    // match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        if (bNeedsMessageAboutOldSavedInfo)
                        {
                            WriteLogFormattedLocalized(0, Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                                strPathSavedInfoFile, pathFile);
                            WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                                "\" has been damaged and needs to be recreated from \"",
                                pathFile, "\"");
                        }
                        bForceCreateInfo = true;
                    }
                }

                if (bAllBlocksOK && bCreateConfirmationFile)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile);
                }

                if (bReturnFalseIfNonRecoverableNotIfDamaged)
                    return nonRestoredSize == 0;
                else
                    return bAllBlocksOK;
            }
            catch (System.IO.IOException ex)
            {
                WriteLogFormattedLocalized(0, Resources.IOErrorReadingFile,
                    finfo.FullName, ex.Message);
                WriteLog(true, 0, "I/O Error reading file: \"", 
                    finfo.FullName, "\": " + ex.Message);
                return false;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Saves information, when the original file has been last read completely
        /// </summary>
        /// <param name="strPathSavedInfoFile">The path of restore info file (not original file)</param>
        //===================================================================================================
        void CreateOrUpdateFileChecked(
            string strPathSavedInfoFile
            )
        {
            string strPath = strPathSavedInfoFile + "ed";

            try
            {
                if (System.IO.File.Exists(strPath))
                {
                    System.IO.File.SetLastWriteTimeUtc(strPath, DateTime.UtcNow);
                }
                else
                {
                    // there we use the simple File.OpenWrite since we need only the date of the file
                    using (System.IO.Stream s = System.IO.File.OpenWrite(strPath))
                    {
                        s.Close();
                    };
                }

                System.IO.File.SetAttributes(
                    strPath, System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System);
            }
            catch (Exception ex)
            {
                WriteLogFormattedLocalized(1, Resources.WarningWhileCreating,
                    ex.Message, strPath);
                WriteLog(true, 1, "Warning: ", ex.Message, 
                    " while creating ", strPath);
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method tests a single file, and repairs it, if there are read or checkum errors
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <returns>true iff the test or restore succeeded</returns>
        //===================================================================================================
        bool TestAndRepairSingleFile(
            string strPathFile, 
            string strPathSavedInfoFile, 
            ref bool bForceCreateInfo,
            bool bOnlyIfCompletelyRecoverable
            )
        {
            System.IO.FileInfo finfo = new System.IO.FileInfo(strPathFile);
            System.IO.FileInfo fiSavedInfo = new System.IO.FileInfo(strPathSavedInfoFile);

            SavedInfo si = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (System.IO.BufferedStream s =
                        new System.IO.BufferedStream(
                            m_iFileOpenAndCopyAbstraction.OpenRead(
                            strPathSavedInfoFile), 
                            (int)Math.Min(fiSavedInfo.Length + 1, 8 * 1024 * 1024)))
                    {
                        si.ReadFrom(s);
                        s.Close();
                    }
                }
                catch // in case of any errors we switch to the unbuffered I/O
                {
                    try
                    {
                        using (System.IO.FileStream s =
                            m_iFileOpenAndCopyAbstraction.OpenRead(strPathSavedInfoFile))
                        {
                            si.ReadFrom(s);
                            s.Close();
                        }
                    }
                    catch (System.IO.IOException ex)
                    {
                        WriteLogFormattedLocalized(0, Resources.IOErrorReadingFile,
                            strPathSavedInfoFile, ex.Message);
                        WriteLog(true, 0, "I/O Error reading file: \"", 
                            strPathSavedInfoFile, "\": " + ex.Message);
                        bNotReadableSi = true;
                    }
                }
            }

            if (bNotReadableSi || 
                si.Length != finfo.Length || 
                !FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc))
            {
                bool bAllBlocksOk = true;
                if (fiSavedInfo.Exists)
                    bForceCreateInfo = true;

                if (!bNotReadableSi)
                {
                    WriteLogFormattedLocalized(0, Resources.SavedInfoFileCantBeUsedForTesting,
                        strPathSavedInfoFile, strPathFile);
                    WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile,
                        "\" can't be used for testing file \"", strPathFile,
                        "\": it was created for another version of the file");
                }

                using (System.IO.FileStream s = m_iFileOpenAndCopyAbstraction.Open(
                    finfo.FullName, System.IO.FileMode.Open,
                    bOnlyIfCompletelyRecoverable ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite,
                    System.IO.FileShare.Read))
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

                            int lengthToWrite =
                                (int)(finfo.Length - index * b.Length > b.Length ?
                                    b.Length :
                                    finfo.Length - index * b.Length);

                            if (bOnlyIfCompletelyRecoverable)
                            {
                                // we can't recover, so put only messages, don't write to file
                                WriteLogFormattedLocalized(1, Resources.IOErrorReadingFileOffset,
                                    finfo.FullName, index * b.Length, ex.Message);
                                WriteLog(true, 1, "I/O error reading file ", finfo.FullName,
                                    " position ", index * b.Length, ": ", ex.Message);
                                s.Seek(index * b.Length + lengthToWrite, System.IO.SeekOrigin.Begin);
                            }
                            else
                            {
                                WriteLogFormattedLocalized(0, Resources.ErrorReadingPositionWillFillWithDummy,
                                    finfo.FullName, index * b.Length, ex.Message);
                                WriteLog(true, 0, "Error while reading file ", finfo.FullName,
                                    " position ", index * b.Length, ": ", ex.Message,
                                    ". Block will be filled with a dummy");
                                s.Seek(index * b.Length, System.IO.SeekOrigin.Begin);
                                if (lengthToWrite > 0)
                                    b.WriteTo(s, lengthToWrite);
                            }
                            bAllBlocksOk = false;
                        }
                    }
                    Block.ReleaseBlock(b);

                    s.Close();
                }

                if (bAllBlocksOk)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfoFile);
                }

                return bAllBlocksOk;
            }

            System.DateTime prevLastWriteTime = finfo.LastWriteTimeUtc;

            Dictionary<long, bool> readableButNotAccepted = 
                new Dictionary<long, bool>();
            try
            {
                bool bAllBlocksOK = true;
                using (System.IO.FileStream s =
                    m_iFileOpenAndCopyAbstraction.OpenRead(finfo.FullName))
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
                                    WriteLogFormattedLocalized(1, Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName, index * b.Length);
                                    WriteLog(true, 1,finfo.FullName, 
                                        ": checksum of block at offset ", 
                                        index * b.Length, " not OK");
                                    readableButNotAccepted[index] = true;
                                }
                            }
                            else
                            {
                                bBlockOk = si.AnalyzeForTestOrRestore(b, index);
                                if (!bBlockOk)
                                {
                                    bAllBlocksOK = false;
                                    WriteLogFormattedLocalized(1,
                                        Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName, index * b.Length);
                                    WriteLog(true, 1, finfo.FullName, 
                                        ": checksum of block at offset ", 
                                        index * b.Length, " not OK");
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
                            WriteLogFormattedLocalized(1, Resources.IOErrorReadingFileOffset,
                                finfo.FullName, index * b.Length, ex.Message);
                            WriteLog(true, 1,"I/O Error reading file: \"", 
                                finfo.FullName, "\", offset ", 
                                index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length, 
                                System.IO.SeekOrigin.Begin);
                        }

                    };

                    Block.ReleaseBlock(b);

                    s.Close();
                };

                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file 
                    // match the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        WriteLogFormattedLocalized(0,
                            Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                            strPathSavedInfoFile, strPathFile);
                        WriteLog(true, 0, "Saved info file \"", strPathSavedInfoFile, 
                            "\" has been damaged and needs to be recreated from \"", 
                            strPathFile, "\"");
                        bForceCreateInfo = true;
                    }
                    else
                    {
                        CreateOrUpdateFileChecked(strPathSavedInfoFile);
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                WriteLogFormattedLocalized(0, Resources.IOErrorReadingFile,
                    finfo.FullName, ex.Message);
                WriteLog(true, 0, "I/O Error reading file: \"", 
                    finfo.FullName, "\": " + ex.Message);
                return false;
            }

            try
            { 
                long nonRestoredSize = 0;
                List<RestoreInfo> rinfos = si.EndRestore(
                    out nonRestoredSize, fiSavedInfo.FullName, this);
                if (nonRestoredSize > 0)
                {
                    bForceCreateInfo = true;
                }

                if (nonRestoredSize == 0 || !bOnlyIfCompletelyRecoverable)
                {
                    using (System.IO.FileStream s =
                        m_iFileOpenAndCopyAbstraction.OpenWrite(finfo.FullName))
                    {
                        foreach (RestoreInfo ri in rinfos)
                        {
                            if (ri.NotRecoverableArea)
                            {
                                if (readableButNotAccepted.ContainsKey(ri.Position / ri.Data.Length))
                                {
                                    WriteLogFormattedLocalized(1,
                                        Resources.KeepingReadableButNotRecoverableBlockAtOffset,
                                        ri.Position);
                                    WriteLog(true, 1, "Keeping readable but not recoverable block at offset ",
                                        ri.Position, ", checksum indicates the block is wrong");
                                }
                                else
                                {
                                    s.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                    WriteLogFormattedLocalized(1, Resources.FillingNotRecoverableAtOffsetWithDummy,
                                        ri.Position);
                                    WriteLog(true, 1, "Filling not recoverable block at offset ",
                                        ri.Position, " with a dummy block");
                                    int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ?
                                        ri.Data.Length :
                                        si.Length - ri.Position);
                                    if (lengthToWrite > 0)
                                        ri.Data.WriteTo(s, lengthToWrite);
                                }
                                bForceCreateInfo = true;
                            }
                            else
                            {
                                s.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                WriteLogFormattedLocalized(1, Resources.RecoveringBlockAtOffsetOfFile,
                                    ri.Position, finfo.FullName);
                                WriteLog(true, 1, "Recovering block at offset ",
                                    ri.Position, " of the file ", finfo.FullName);
                                int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ?
                                    ri.Data.Length :
                                    si.Length - ri.Position);
                                if (lengthToWrite > 0)
                                    ri.Data.WriteTo(s, lengthToWrite);
                            }
                        }

                        s.Close();
                    }
                }

                if (bOnlyIfCompletelyRecoverable && nonRestoredSize != 0)
                {
                    if (rinfos.Count > 1)
                    {
                        WriteLogFormattedLocalized(0, Resources.ThereAreBadBlocksNonRestorableCantBeBackup,
                            rinfos.Count, finfo.FullName, nonRestoredSize);
                        WriteLog(true, 0, "There are ", rinfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", non-restorable parts: ", nonRestoredSize, " bytes, file can't be used as backup");
                    }
                    else
                        if (rinfos.Count > 0)
                        {
                            WriteLogFormattedLocalized(0, Resources.ThereIsBadBlockNonRestorableCantBeBackup,
                                finfo.FullName, nonRestoredSize);
                            WriteLog(true, 0, "There is one bad block in the file ", finfo.FullName,
                                " and it can't be restored: ", nonRestoredSize, " bytes, file can't be used as backup");
                        }

                    finfo.LastWriteTimeUtc = prevLastWriteTime;
                }
                else
                {
                    if (rinfos.Count > 1)
                    {
                        WriteLogFormattedLocalized(0, Resources.ThereWereBadBlocksInFileNotRestoredParts,
                            rinfos.Count, finfo.FullName, nonRestoredSize);
                        WriteLog(true, 0, "There were ", rinfos.Count,
                            " bad blocks in the file ", finfo.FullName,
                            ", not restored parts: ", nonRestoredSize, " bytes");
                    }
                    else
                        if (rinfos.Count > 0)
                        {
                            WriteLogFormattedLocalized(0, Resources.ThereWasBadBlockInFileNotRestoredParts,
                                finfo.FullName, nonRestoredSize);
                            WriteLog(true, 0, "There was one bad block in the file ", finfo.FullName,
                                ", not restored parts: ", nonRestoredSize, " bytes");
                        }

                    if (nonRestoredSize == 0 && rinfos.Count == 0)
                    {
                        CreateOrUpdateFileChecked(strPathSavedInfoFile);
                    }

                    if (nonRestoredSize > 0)
                    {
                        int countErrors = (int)(nonRestoredSize / (Block.GetBlock().Length));
                        finfo.LastWriteTime = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 23 -
                            (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                        bForceCreateInfo = true;
                    }
                    else
                        finfo.LastWriteTimeUtc = prevLastWriteTime;
                }

                return nonRestoredSize == 0;
            }
            catch (System.IO.IOException ex)
            {
                WriteLogFormattedLocalized(0, Resources.IOErrorWritingFile,
                    finfo.FullName, ex.Message);
                WriteLog(true, 0, "I/O Error writing file: \"", finfo.FullName, "\": " + ex.Message);
                finfo.LastWriteTimeUtc = prevLastWriteTime;
                return false;
            }
        }


        //===================================================================================================
        /// <summary>
        /// This method tests a single file, together with its saved info, if the original file is healthy,
        /// or can be restored using the saved info.
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <returns>true iff the file is healthy or can be restored</returns>
        //===================================================================================================
        bool TestSingleFileHealthyOrCanRepair(
            string strPathFile, 
            string strPathSavedInfoFile, 
            ref bool bForceCreateInfo
            )
        {
            return TestSingleFile2(strPathFile, strPathSavedInfoFile, 
                ref bForceCreateInfo, true, true, true, false, true);
        }

        //===================================================================================================
        /// <summary>
        /// This method bidirectionylly tests and/or repairs two original files, together with two saved 
        /// info files
        /// </summary>
        /// <param name="strPathFile1">The path of first original file</param>
        /// <param name="strPathFile2">The path of second original file</param>
        /// <param name="strPathSavedInfo1">The path of saved info for first file</param>
        /// <param name="strPathSavedInfo2">The path of saved info for second file</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        //===================================================================================================
        void TestAndRepairTwoFiles(
            string strPathFile1, 
            string strPathFile2, 
            string strPathSavedInfo1, 
            string strPathSavedInfo2, 
            ref bool bForceCreateInfo
            )
        {
            System.IO.FileInfo fi1 = new System.IO.FileInfo(strPathFile1);
            System.IO.FileInfo fi2 = new System.IO.FileInfo(strPathFile2);
            System.IO.FileInfo fiSavedInfo1 = new System.IO.FileInfo(strPathSavedInfo1);
            System.IO.FileInfo fiSavedInfo2 = new System.IO.FileInfo(strPathSavedInfo2);

            SavedInfo si1 = new SavedInfo();
            SavedInfo si2 = new SavedInfo();

            bool bSaveInfo1Present = false;
            if (fiSavedInfo1.Exists && 
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc)
            {
                using (System.IO.Stream s =
                    m_iFileOpenAndCopyAbstraction.OpenRead(fiSavedInfo1.FullName))
                {
                    si1.ReadFrom(s);
                    bSaveInfo1Present = si1.Length==fi1.Length && 
                        FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc);
                    if (!bSaveInfo1Present)
                    {
                        si1 = new SavedInfo();
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

            if (fiSavedInfo2.Exists && 
                fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc)
            {
                using (System.IO.Stream s =
                    m_iFileOpenAndCopyAbstraction.OpenRead(fiSavedInfo2.FullName))
                {
                    SavedInfo si3 = new SavedInfo();
                    si3.ReadFrom(s);
                    if (si3.Length == fi2.Length && 
                        FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc))
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
                si1.ImproveThisAndOther(si2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> equalBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                using (System.IO.Stream s1 = 
                    m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile1))
                {
                    using (System.IO.Stream s2 = 
                        m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile2))
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
                                    WriteLogFormattedLocalized(2, Resources.ChecksumOfBlockAtOffsetNotOK,
                                        strPathFile1, index * b1.Length);
                                    WriteLog(true, 2, strPathFile1, ": checksum of block at offset ", 
                                        index * b1.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLogFormattedLocalized(2, Resources.IOErrorReadingFile,
                                    strPathFile1, ex.Message);
                                WriteLog(true, 2, "I/O exception while reading file \"", 
                                    strPathFile1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, 
                                    System.IO.SeekOrigin.Begin);
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
                                    WriteLogFormattedLocalized(2, Resources.ChecksumOfBlockAtOffsetNotOK,
                                        strPathFile2, index * b2.Length);
                                    WriteLog(true, 2, strPathFile2, ": checksum of block at offset ", 
                                        index * b2.Length, " not OK");
                                }
                            }
                            catch (System.IO.IOException ex)
                            {
                                WriteLogFormattedLocalized(2, Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);
                                WriteLog(true, 2, "I/O exception while reading file \"", 
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, 
                                    System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                if (si2.AnalyzeForTestOrRestore(b1, index))
                                {
                                    WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, index * b1.Length, fi1.FullName);
                                    WriteLog(true, 1, "Block of ", fi2.FullName, 
                                        " position ", index * b1.Length, 
                                        " will be restored from ", fi1.FullName);
                                    restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                }
                            }
                            else
                                if (b2Present && !b1Present)
                            {
                                if (si1.AnalyzeForTestOrRestore(b2, index))
                                {
                                    WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi1.FullName, index * b1.Length, fi2.FullName);
                                    WriteLog(true, 1, "Block of ", fi1.FullName, 
                                        " position ", index * b1.Length, 
                                        " will be restored from ", fi2.FullName);
                                    restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                }
                            }
                            else
                            {
                                if (b2Present && !b1Ok)
                                {
                                    if (si1.AnalyzeForTestOrRestore(b2, index))
                                    {
                                        WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi1.FullName, index * b1.Length, fi2.FullName);
                                        WriteLog(true, 1, "Block of ", fi1.FullName, 
                                            " position ", index * b1.Length, 
                                            " will be restored from ", fi2.FullName);
                                        restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    }
                                };

                                if (b1Present && !b2Ok)
                                {
                                    if (si2.AnalyzeForTestOrRestore(b1, index))
                                    {
                                        WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, index * b1.Length, fi1.FullName);
                                        WriteLog(true, 1, "Block of ", fi2.FullName, 
                                            " position ", index * b1.Length, 
                                            " will be restored from ", fi1.FullName);
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
                restore1.AddRange(si1.EndRestore(out notRestoredSize1, fiSavedInfo1.FullName, this));
                long notRestoredSize2 = 0;
                restore2.AddRange(si2.EndRestore(out notRestoredSize2, fiSavedInfo2.FullName, this));

                // now we've got the list of improvements for both files
                using (System.IO.Stream s1 = m_iFileOpenAndCopyAbstraction.Open(
                    strPathFile1, System.IO.FileMode.Open, 
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {

                    using (System.IO.Stream s2 = m_iFileOpenAndCopyAbstraction.Open(
                        strPathFile2, System.IO.FileMode.Open, 
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {
                        // let'oInputStream apply improvements of one file 
                        // to the list of the oOtherSaveInfo, whenever possible
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            foreach (RestoreInfo ri2 in restore2)
                            {
                                if (ri2.Position == ri1.Position)
                                    if (ri2.NotRecoverableArea && !ri1.NotRecoverableArea)
                                    {
                                        WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, ri2.Position, fi1.FullName);
                                        WriteLog(true, 1, "Block of ", fi2.FullName, 
                                            " position ", ri2.Position, 
                                            " will be restored from ", fi1.FullName);
                                        ri2.Data = ri1.Data;
                                        ri2.NotRecoverableArea = false;
                                    }
                                    else
                                        if (ri1.NotRecoverableArea && !ri2.NotRecoverableArea)
                                        {
                                            WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                                fi1.FullName, ri1.Position, fi2.FullName);
                                            WriteLog(true, 1, "Block of ", fi1.FullName, 
                                                " position ", ri1.Position, 
                                                " will be restored from ", fi2.FullName);
                                            ri1.Data = ri2.Data;
                                            ri1.NotRecoverableArea = false;
                                        }
                            }
                        }


                        // let'oInputStream apply the definitive improvements
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea || 
                                (_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length)))
                                ;// bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                WriteLogFormattedLocalized(1, Resources.RecoveringBlockAtOffsetOfFile,
                                    ri1.Position, fi1.FullName);
                                WriteLog(true, 1, "Recovering block of ", fi1.FullName, 
                                    " at position ", ri1.Position);
                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ? 
                                    ri1.Data.Length : 
                                    si1.Length - ri1.Position);
                                if (lengthToWrite > 0)
                                    ri1.Data.WriteTo(s1, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks1[ri1.Position / ri1.Data.Length] = true;
                            }
                        };


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea || 
                                (_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
                                ; // bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                WriteLogFormattedLocalized(1, Resources.RecoveringBlockAtOffsetOfFile,
                                    ri2.Position, fi2.FullName);
                                WriteLog(true, 1, "Recovering block of ", fi2.FullName, 
                                    " at position ", ri2.Position);
                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? 
                                    ri2.Data.Length : 
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        };



                        // let'oInputStream try to copy non-recoverable 
                        // blocks from one file to another, whenever possible
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            if (ri1.NotRecoverableArea && !equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length) && 
                                readableBlocks2.ContainsKey(ri1.Position/ri1.Data.Length) && 
                                !readableBlocks1.ContainsKey(ri1.Position/ri1.Data.Length) )
                            {
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi1.FullName, ri1.Position, fi2.FullName);
                                WriteLog(true, 1, "Block of ", fi1.FullName, " position ", 
                                    ri1.Position, " will be copied from ", 
                                    fi2.FullName, " even if checksum indicates the block is wrong");

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
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi2.FullName, ri2.Position, fi1.FullName);
                                WriteLog(true, 1, "Block of ", fi2.FullName, " position ", 
                                    ri2.Position, " will be copied from ", fi1.FullName, 
                                    " even if checksum indicates the block is wrong");

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
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi1.FullName, ri1.Position);
                                WriteLog(true, 1, "Block of ", fi1.FullName, " position ", 
                                    ri1.Position, " is not recoverable and will be filled with a dummy");

                                s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ? 
                                    ri1.Data.Length : 
                                    si1.Length - ri1.Position);
                                if (lengthToWrite > 0)
                                    ri1.Data.WriteTo(s1, lengthToWrite);
                            }
                        };


                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea && 
                                !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, ri2.Position);
                                WriteLog(true, 1, "Block of ", fi2.FullName,
                                    " position ", ri2.Position, 
                                    " is not recoverable and will be filled with a dummy");

                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? 
                                    ri2.Data.Length : 
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                            }
                        };




                        s2.Close();
                    }
                    s1.Close();
                }

                if (restore1.Count > 0)
                {
                    WriteLogFormattedLocalized(0, Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        restore1.Count, fi1.FullName, notRestoredSize1);
                    WriteLog(true, 0, "There were ", restore1.Count,
                        " bad blocks in ", fi1.FullName,
                        " not restored bytes: ", notRestoredSize1);
                }
                if (restore2.Count > 0)
                {
                    WriteLogFormattedLocalized(0, Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        restore2.Count, fi2.FullName, notRestoredSize2);

                    WriteLog(true, 0, "There were ", restore2.Count,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", notRestoredSize2);
                }

                fi1.LastWriteTimeUtc = prevLastWriteTime;
                fi2.LastWriteTimeUtc = prevLastWriteTime;

                if (notRestoredSize1 == 0 && restore1.Count==0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo1);
                }

                if (notRestoredSize2 == 0 && restore2.Count==0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo2);
                }

            }
            else
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // let'oInputStream read both copies of the file 
                // that obviously are both present, without saved info
                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                long notRestoredSize1 = 0;
                long notRestoredSize2 = 0;
                long badBlocks1 = 0;
                long badBlocks2 = 0;
                using (System.IO.Stream s1 = 
                    m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile1))
                {
                    using (System.IO.Stream s2 = 
                        m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile2))
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
                                WriteLogFormattedLocalized(2, Resources.IOErrorWritingFile,
                                    strPathFile1, ex.Message);
                                WriteLog(true, 2, "I/O exception while reading file \"", 
                                    strPathFile1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, 
                                    System.IO.SeekOrigin.Begin);
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
                                WriteLogFormattedLocalized(2, Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);
                                WriteLog(true, 2, "I/O exception while reading file \"", 
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, 
                                    System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, index * b1.Length, fi1.FullName);
                                WriteLog(true, 1, "Block of ", fi2.FullName, 
                                    " position ", index * b1.Length, 
                                    " will be restored from ", fi1.FullName);
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                            }
                            else
                            if (b2Present && !b1Present)
                            {
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi1.FullName, index * b1.Length, fi2.FullName);
                                WriteLog(true, 1, "Block of ", fi1.FullName, 
                                    " position ", index * b1.Length, 
                                    " will be restored from ", fi2.FullName);
                                restore1.Add(new RestoreInfo(index * b2.Length, b2, false));
                            }
                            else
                            if (!b1Present && !b2Present)
                            {
                                Block b = Block.GetBlock();
                                WriteLogFormattedLocalized(1, Resources.BlocksOfAndAtPositionNonRecoverableFillDummy,
                                    fi1.FullName, fi2.FullName, index * b1.Length);
                                WriteLog(true, 1, "Blocks of ", fi1.FullName, 
                                    " and ", fi2.FullName, " at position ", 
                                    index * b1.Length, 
                                    " are not recoverable and will be filled with a dummy block");
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
                using (System.IO.Stream s1 = m_iFileOpenAndCopyAbstraction.Open(
                    strPathFile1, System.IO.FileMode.Open, 
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo ri1 in restore1)
                    {
                        s1.Seek(ri1.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si1.Length - ri1.Position >= ri1.Data.Length ? 
                            ri1.Data.Length : 
                            si1.Length - ri1.Position);
                        if (lengthToWrite > 0)
                            ri1.Data.WriteTo(s1, lengthToWrite);
                    };
                };
                fi1.LastWriteTimeUtc = prevLastWriteTime;

                if (badBlocks1 > 0)
                {
                    WriteLogFormattedLocalized(0, Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        badBlocks1, fi1.FullName, notRestoredSize1);
                    WriteLog(true, 0, "There were ", badBlocks1,
                        " bad blocks in ", fi1.FullName,
                        " not restored bytes: ", notRestoredSize1);
                }


                using (System.IO.Stream s2 = m_iFileOpenAndCopyAbstraction.Open(
                    strPathFile2, System.IO.FileMode.Open, 
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo ri2 in restore2)
                    {
                        s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? 
                            ri2.Data.Length : 
                            si2.Length - ri2.Position);
                        if (lengthToWrite > 0)
                            ri2.Data.WriteTo(s2, lengthToWrite);
                    };
                }
                fi2.LastWriteTimeUtc = prevLastWriteTime;
                if (badBlocks2 > 0)
                {
                    WriteLogFormattedLocalized(0, Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        badBlocks2, fi2.FullName, notRestoredSize2);
                    WriteLog(true, 0, "There were ", badBlocks2,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", notRestoredSize2);
                }

                if (notRestoredSize1 == 0 && restore1.Count==0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo1);
                }

                if (notRestoredSize2 == 0 && restore2.Count==0)
                {
                    CreateOrUpdateFileChecked(strPathSavedInfo2);
                }

            }
        }


        //===================================================================================================
        /// <summary>
        /// This method copies a single file, and repairs it on the way, if it encounters some bad blocks.
        /// </summary>
        /// <param name="strPathFile">The path of original file</param>
        /// <param name="strPathTargetFile">The path of target file for copy</param>
        /// <param name="strPathSavedInfoFile">The path of saved info (.chk)</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        /// <param name="bForceCreateInfoTarget">If saved info of target file needs to be updated then 
        /// method sets given var to true</param>
        /// <param name="strReason">The reason of the copy for log messages</param>
        /// <param name="bApplyRepairsToSrc">If set to true, method will also repair source file,
        /// not only the copy</param>
        /// <param name="bFailOnNonRecoverable">If there are non-recoverable blocks and this flag
        /// is set to true, then method throws an exception, instead of continuing</param>
        /// <returns>true iff copy succeeded</returns>
        //===================================================================================================
        bool CopyRepairSingleFile(
            string strPathTargetFile, 
            string strPathFile, 
            string strPathSavedInfoFile, 
            ref bool bForceCreateInfo, 
            ref bool bForceCreateInfoTarget,
            string strReasonEn,
            string strReasonTranslated, 
            bool bFailOnNonRecoverable, 
            bool bApplyRepairsToSrc)
        {
            // if same file then try to repair in place
            if (string.Equals(strPathTargetFile, strPathFile, 
                StringComparison.InvariantCultureIgnoreCase))
            {
                if (_bTestFiles && _bRepairFiles)
                    return TestAndRepairSingleFile(strPathFile, strPathSavedInfoFile, 
                        ref bForceCreateInfo, false);
                else
                {
                    if (_bTestFiles)
                    {
                        if (TestSingleFile2(strPathFile, strPathSavedInfoFile, 
                            ref bForceCreateInfo, false, true, true, true, false))
                        {
                            return true;
                        }
                        else
                        {
                            string strMessage = string.Format(Resources.ErrorWhileTestingFile, strPathFile);
                            WriteLog(false, 1, strMessage);
                            WriteLog(true, 1, "Error while testing file ", strPathFile);
                            if (bFailOnNonRecoverable)
                                throw new Exception(strMessage);
                            return false;
                        }
                    }
                    else
                        return true;
                }
            }


            System.IO.FileInfo finfo = new System.IO.FileInfo(strPathFile);
            System.IO.FileInfo fiSavedInfo = new System.IO.FileInfo(strPathSavedInfoFile);

            DateTime dtmOriginalTime = finfo.LastWriteTimeUtc;

            SavedInfo si = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (System.IO.FileStream s =
                        m_iFileOpenAndCopyAbstraction.OpenRead(strPathSavedInfoFile))
                    {
                        si.ReadFrom(s);
                        s.Close();
                    }
                }
                catch (System.IO.IOException ex)
                {
                    WriteLogFormattedLocalized(0, Resources.IOErrorReadingFile,
                        strPathSavedInfoFile, ex.Message);
                    WriteLog(true, 0, "I/O Error reading file: \"", 
                        strPathSavedInfoFile, "\": " + ex.Message);
                    bNotReadableSi = true;
                }
            }

            if (bNotReadableSi || si.Length != finfo.Length || 
                !(_bIgnoreTimeDifferences || FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc)))
            {
                bool bAllBlocksOk = true;
                bForceCreateInfo = true;

                if (!bNotReadableSi)
                {
                    WriteLogFormattedLocalized(0, Resources.SavedInfoFileCantBeUsedForTesting,
                        strPathSavedInfoFile, strPathFile);
                    WriteLog(true, 0, "RestoreInfo file \"", strPathSavedInfoFile,
                        "\" can't be used for restoring file \"",
                        strPathFile, "\": it was created for another version of the file");
                }

                using (System.IO.FileStream s = m_iFileOpenAndCopyAbstraction.Open(
                    finfo.FullName, System.IO.FileMode.Open, 
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    try
                    {
                        int countErrors = 0;
                        using (System.IO.FileStream s2 = m_iFileOpenAndCopyAbstraction.Open(
                            strPathTargetFile + ".tmp", System.IO.FileMode.Create, 
                            System.IO.FileAccess.Write, System.IO.FileShare.None))
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

                                    WriteLogFormattedLocalized(1, Resources.IOErrorWhileReadingPositionFillDummyWhileCopy,
                                        finfo.FullName, index * b.Length, ex.Message);
                                    WriteLog(true, 1, "I/O Error while reading file ", 
                                        finfo.FullName, " position ", index * b.Length, ": ", 
                                        ex.Message, ". Block will be replaced with a dummy during copy.");
                                    int lengthToWrite = (int)(finfo.Length - index * b.Length > b.Length ? 
                                        b.Length : 
                                        finfo.Length - index * b.Length);
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
                        System.IO.FileInfo fi2 = new System.IO.FileInfo(strPathTargetFile);

                        if (fi2.Exists)
                            m_iFileOpenAndCopyAbstraction.Delete(fi2);

                        // and replace it with the new one
                        System.IO.FileInfo fi2tmp = new System.IO.FileInfo(strPathTargetFile + ".tmp");
                        if (bAllBlocksOk)
                            // if everything OK set original time
                            fi2tmp.LastWriteTimeUtc = dtmOriginalTime;
                        else
                        {
                            // set the time to very old, so any existing newer or with less errors appears to be better.
                            fi2tmp.LastWriteTime = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 
                                23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                            bForceCreateInfoTarget = true;
                        }
                        //fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                        fi2tmp.MoveTo(strPathTargetFile);

                        if (!bAllBlocksOk)
                        {
                            WriteLogFormattedLocalized(0, Resources.WarningCopiedToWithErrors,
                                strPathFile, strPathTargetFile, strReasonTranslated);
                            WriteLog(true, 0, "Warning: copied ", strPathFile, " to ",
                                strPathTargetFile, " ", strReasonEn, " with errors");
                        }
                        else
                        {
                            WriteLogFormattedLocalized(0, Resources.CopiedFromToReason,
                                strPathFile, strPathTargetFile, strReasonTranslated);
                            WriteLog(true, 0, "Copied ", strPathFile, " to ",
                                strPathTargetFile, " ", strReasonEn);
                        }

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
                using (System.IO.FileStream s = 
                    m_iFileOpenAndCopyAbstraction.OpenRead(finfo.FullName))
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
                                    WriteLogFormattedLocalized(2, Resources.ChecksumOfBlockAtOffsetNotOK,
                                        finfo.FullName, index * b.Length);
                                    WriteLog(true, 2, finfo.FullName, 
                                        ": checksum of block at offset ", 
                                        index * b.Length, " not OK");
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
                                        WriteLogFormattedLocalized(2, Resources.ChecksumOfBlockAtOffsetNotOK,
                                            finfo.FullName, index * b.Length);
                                        WriteLog(true, 2, finfo.FullName, 
                                            ": checksum of block at offset ", 
                                            index * b.Length, " not OK");
                                        readableButNotAccepted[index] = true;
                                    }
                                }
                                break;
                            }
                        }
                        catch (System.IO.IOException ex)
                        {
                            bAllBlocksOK = false;
                            WriteLogFormattedLocalized(2, Resources.IOErrorReadingFileOffset,
                                finfo.FullName, index * b.Length, ex.Message);
                            WriteLog(true, 2, "I/O Error reading file: \"", 
                                finfo.FullName, "\", offset ", 
                                index * b.Length, ": " + ex.Message);
                            s.Seek((index + 1) * b.Length, 
                                System.IO.SeekOrigin.Begin);
                        }

                        if (_cancelClicked)
                            throw new OperationCanceledException();

                    };
                    Block.ReleaseBlock(b);

                    s.Close();
                };


                if (bAllBlocksOK)
                {
                    // check also, if the contents of the checksum file match 
                    // the file itself, or if they have been corrupted somehow
                    if (!si.VerifyIntegrityAfterRestoreTest())
                    {
                        WriteLogFormattedLocalized(0, Resources.SavedInfoHasBeenDamagedNeedsRecreation,
                            strPathSavedInfoFile, strPathFile);
                        WriteLog(true, 0, "Saved info file \"", 
                            strPathSavedInfoFile, 
                            "\" has been damaged and needs to be recreated from \"", 
                            strPathFile, "\"");
                        bForceCreateInfo = true;
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                WriteLogFormattedLocalized(0, Resources.IOErrorReadingFile,
                    finfo.FullName, ex.Message);
                WriteLog(true, 0, "I/O Error reading file: \"", 
                    finfo.FullName, "\": " + ex.Message);

                if (bFailOnNonRecoverable)
                    throw;

                return false;
            }


            try
            {
                long nonRestoredSize = 0;
                List<RestoreInfo> rinfos = si.EndRestore(
                    out nonRestoredSize, strPathSavedInfoFile, this);

                if (nonRestoredSize > 0)
                {
                    if (bFailOnNonRecoverable)
                    {
                        WriteLogFormattedLocalized(1, Resources.ThereAreBadBlocksInNonRestorableMayRetryLater,
                            rinfos.Count, finfo.FullName, nonRestoredSize);
                        WriteLog(true, 1, "There are ", rinfos.Count, 
                            " bad blocks in the file ", finfo.FullName,
                            ", non-restorable parts: ", nonRestoredSize, 
                            " bytes. Can't proceed there because of non-recoverable, may retry later.");
                        throw new Exception("Non-recoverable blocks discovered, failing");
                    }
                    else
                        bForceCreateInfoTarget = true;
                }

                if (rinfos.Count > 1)
                {
                    WriteLogFormattedLocalized(1,
                        Resources.ThereAreBadBlocksInFileNonRestorableParts +
                            (bApplyRepairsToSrc ? "" :
                             Resources.TheFileCantBeModifiedMissingRepairApplyToCopy),
                        rinfos.Count, finfo.FullName, nonRestoredSize);
                    WriteLog(true, 1, "There are ", rinfos.Count,
                        " bad blocks in the file ", finfo.FullName,
                        ", non-restorable parts: ", nonRestoredSize, " bytes. " +
                        (bApplyRepairsToSrc ? "" :
                            "The file can't be modified because of missing repair option, " +
                            "the restore process will be applied to copy."));
                }
                else
                    if (rinfos.Count > 0)
                    {
                        WriteLogFormattedLocalized(1,
                           Resources.ThereIsBadBlockInFileNonRestorableParts +
                               (bApplyRepairsToSrc ? "" :
                               Resources.TheFileCantBeModifiedMissingRepairApplyToCopy),
                           finfo.FullName, nonRestoredSize);
                        WriteLog(true, 1, "There is one bad block in the file ", finfo.FullName,
                           ", non-restorable parts: ", nonRestoredSize, " bytes. " +
                           (bApplyRepairsToSrc ? "" :
                               "The file can't be modified because of missing repair option, " +
                               "the restore process will be applied to copy."));
                    }

                //bool bNonRecoverablePresent = false;
                try
                {
                    using (System.IO.FileStream s2 = 
                        m_iFileOpenAndCopyAbstraction.Open(
                            finfo.FullName, System.IO.FileMode.Open, 
                            bApplyRepairsToSrc && (rinfos.Count > 0) ? 
                                System.IO.FileAccess.ReadWrite : System.IO.FileAccess.Read, 
                            System.IO.FileShare.Read))
                    {
                        using (System.IO.FileStream s = 
                            m_iFileOpenAndCopyAbstraction.Open(
                            strPathTargetFile + ".tmp", System.IO.FileMode.Create, 
                            System.IO.FileAccess.Write, System.IO.FileShare.None))
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
                                                WriteLogFormattedLocalized(1, Resources.KeepingReadableNonRecovBBlockAtAlsoInCopy,
                                                    ri.Position, finfo.FullName, strPathTargetFile);
                                                WriteLog(true, 1, "Keeping readable but not recoverable block at offset ", 
                                                    ri.Position, " of original file ", finfo.FullName, 
                                                    " also in copy ", strPathTargetFile, 
                                                    ", checksum indicates the block is wrong");
                                            }
                                            else
                                            {
                                                s2.Seek(ri.Position + ri.Data.Length, System.IO.SeekOrigin.Begin);

                                                WriteLogFormattedLocalized(1, Resources.FillingNotRecoverableAtOffsetOfCopyWithDummy,
                                                    ri.Position, strPathTargetFile);

                                                WriteLog(true, 1, "Filling not recoverable block at offset ", 
                                                    ri.Position, " of copied file ", strPathTargetFile, " with a dummy");

                                                //bNonRecoverablePresent = true;
                                                int lengthToWrite = (int)(finfo.Length - position > blockSize ? 
                                                    blockSize : 
                                                    finfo.Length - position);
                                                if (lengthToWrite > 0)
                                                    ri.Data.WriteTo(s, lengthToWrite);
                                            }
                                            bForceCreateInfoTarget = true;
                                        }
                                        else
                                        {
                                            WriteLogFormattedLocalized(1, Resources.RecoveringBlockAtOfCopiedFile,
                                                ri.Position, strPathTargetFile);
                                            WriteLog(true, 1, "Recovering block at offset ", 
                                                ri.Position, " of copied file ", strPathTargetFile);
                                            int lengthToWrite = (int)(si.Length - ri.Position >= ri.Data.Length ? 
                                                ri.Data.Length : 
                                                si.Length - ri.Position);
                                            if (lengthToWrite > 0)
                                                ri.Data.WriteTo(s, lengthToWrite);

                                            if (bApplyRepairsToSrc)
                                            {
                                                WriteLogFormattedLocalized(1, Resources.RecoveringBlockAtOffsetOfFile,
                                                    ri.Position, finfo.FullName);
                                                WriteLog(true, 1, "Recovering block at offset ",
                                                    ri.Position, " of file ", finfo.FullName);
                                                s2.Seek(ri.Position, System.IO.SeekOrigin.Begin);
                                                if (lengthToWrite > 0)
                                                    ri.Data.WriteTo(s2, lengthToWrite);
                                            }
                                            else
                                                s2.Seek(ri.Position + lengthToWrite, 
                                                    System.IO.SeekOrigin.Begin);
                                        }
                                        break;
                                    }
                                }

                                if (!bBlockWritten)
                                {
                                    // there we land in case we didn't overwrite the block with restore info,
                                    // so read from source and write to destination
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
                System.IO.FileInfo finfoTmp = new System.IO.FileInfo(strPathTargetFile + ".tmp");
                if (System.IO.File.Exists(strPathTargetFile))
                    m_iFileOpenAndCopyAbstraction.Delete(strPathTargetFile);
                finfoTmp.MoveTo(strPathTargetFile);

                if (si.NeedsRebuild())
                {
                    if ((!_bFirstToSecond || !_bFirstReadOnly) && rinfos.Count==0)
                        bForceCreateInfo = true;

                    bForceCreateInfoTarget = true;
                }
 

                System.IO.FileInfo finfo2 = new System.IO.FileInfo(strPathTargetFile);
                if (rinfos.Count > 1)
                {
                    WriteLogFormattedLocalized(1, Resources.OutOfBadBlocksNotRestoredInCopyBytes,
                        rinfos.Count, finfo2.FullName, nonRestoredSize);
                    WriteLog(true, 1, "Out of ", rinfos.Count,
                        " bad blocks in the original file not restored parts in the copy ",
                        finfo2.FullName, ": ", nonRestoredSize, " bytes.");
                }
                else
                    if (rinfos.Count > 0)
                    {
                        WriteLogFormattedLocalized(1, Resources.ThereWasBadBlockNotRestoredInCopyBytes,
                            finfo2.FullName, nonRestoredSize);
                        WriteLog(true, 1, "There was one bad block in the original file, " +
                            "not restored parts in the copy ", finfo2.FullName, ": ",
                            nonRestoredSize, " bytes.");
                    }

                if (nonRestoredSize > 0)
                {
                    int countErrors = (int)(nonRestoredSize / (Block.GetBlock().Length));
                    finfo2.LastWriteTime = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 
                        23 - (countErrors / 60) % 24, 59 - countErrors % 60, 0);
                    bForceCreateInfoTarget = true;
                }
                else
                    finfo2.LastWriteTimeUtc = dtmOriginalTime;

                if (nonRestoredSize != 0)
                {
                    WriteLogFormattedLocalized(0, Resources.WarningCopiedToWithErrors,
                        strPathFile, strPathTargetFile, strReasonTranslated);
                    WriteLog(true, 0, "Warning: copied ", strPathFile, " to ",
                        strPathTargetFile, " ", strReasonEn, " with errors");
                }
                else
                {
                    WriteLogFormattedLocalized(0, Resources.CopiedFromToReason,
                        strPathFile, strPathTargetFile, strReasonTranslated);
                    WriteLog(true, 0, "Copied ", strPathFile, " to ",
                        strPathTargetFile, " ", strReasonEn);
                }

                //finfo2.LastWriteTimeUtc = prevLastWriteTime;

                return nonRestoredSize == 0;
            }
            catch (System.IO.IOException ex)
            {
                WriteLogFormattedLocalized(0, Resources.IOErrorDuringRepairCopyOf,
                    strPathTargetFile, ex.Message);
                WriteLog(true, 0, "I/O Error during repair copy to file: \"", 
                    strPathTargetFile, "\": " + ex.Message);
                return false;
            }
        }

        //===================================================================================================
        /// <summary>
        /// This method tests and repairs the second file with all available means
        /// </summary>
        /// <param name="strPathFile1">Path of first file</param>
        /// <param name="strPathFile2">Path of second file to be tested and repaired</param>
        /// <param name="strPathSavedInfo1">Saved info of the first file</param>
        /// <param name="strPathSavedInfo2">Saved info of the second file</param>
        /// <param name="bForceCreateInfo">If saved info needs to be updated then method sets given 
        /// var to true</param>
        //===================================================================================================
        void TestAndRepairSecondFile(
            string strPathFile1, 
            string strPathFile2, 
            string strPathSavedInfo1, 
            string strPathSavedInfo2, 
            ref bool bForceCreateInfo
            )
        {
            // if we can skip repairs, then try to test first and repair only in case there are some errors.
            if (_bSkipRecentlyTested)
                if (TestSingleFile(strPathFile2, strPathSavedInfo2, ref bForceCreateInfo, false, false, true))
                    return;

            System.IO.FileInfo fi1 = new System.IO.FileInfo(strPathFile1);
            System.IO.FileInfo fi2 = new System.IO.FileInfo(strPathFile2);
            System.IO.FileInfo fiSavedInfo1 = new System.IO.FileInfo(strPathSavedInfo1);
            System.IO.FileInfo fiSavedInfo2 = new System.IO.FileInfo(strPathSavedInfo2);

            SavedInfo si1 = new SavedInfo();
            SavedInfo si2 = new SavedInfo();

            bool bSaveInfo1Present = false;
            if (fiSavedInfo1.Exists && 
                (_bIgnoreTimeDifferences || fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc) )
            {
                using (System.IO.Stream s = m_iFileOpenAndCopyAbstraction.OpenRead(fiSavedInfo1.FullName))
                {
                    si1.ReadFrom(s);
                    bSaveInfo1Present = si1.Length == fi1.Length && 
                        (_bIgnoreTimeDifferences || FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc) );
                    if (!bSaveInfo1Present)
                    {
                        si1 = new SavedInfo();
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

            if (fiSavedInfo2.Exists && 
                (_bIgnoreTimeDifferences || fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc) )
            {
                using (System.IO.Stream s = m_iFileOpenAndCopyAbstraction.OpenRead(fiSavedInfo2.FullName))
                {
                    SavedInfo si3 = new SavedInfo();
                    si3.ReadFrom(s);
                    if (si3.Length == fi2.Length && 
                        (_bIgnoreTimeDifferences || FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc) ))
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
                si1.ImproveThisAndOther(si2);

                // the list of equal blocks, so we don't overwrite obviously correct blocks
                Dictionary<long, bool> equalBlocks = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks1 = new Dictionary<long, bool>();
                Dictionary<long, bool> readableBlocks2 = new Dictionary<long, bool>();

                List<RestoreInfo> restore1 = new List<RestoreInfo>();
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                using (System.IO.Stream s1 = 
                    m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile1))
                {
                    using (System.IO.Stream s2 = 
                        m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile2))
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
                                WriteLogFormattedLocalized(2, Resources.IOErrorReadingFile,
                                    strPathFile1, ex.Message);
                                WriteLog(true, 2, "I/O error while reading file \"", strPathFile1, "\": ", ex.Message);
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
                                WriteLogFormattedLocalized(2, Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);
                                WriteLog(true, 2, "I/O error while reading file \"", 
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                if (si2.AnalyzeForTestOrRestore(b1, index))
                                {
                                    WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, index * b1.Length, fi1.FullName);
                                    WriteLog(true, 1, "Block of ", fi2.FullName, " position ", 
                                        index * b1.Length, " will be restored from ", fi1.FullName);
                                    restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                                }
                            }
                            else
                                if (b2Present && !b1Present)
                            {
                                if (si1.AnalyzeForTestOrRestore(b2, index))
                                {
                                    restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi1.FullName, index * b1.Length, fi2.FullName);
                                    WriteLog(true, 1, "Block of ", fi1.FullName, " position ", index * 
                                        b1.Length, " could be restored from ", fi2.FullName, 
                                        " but it is not possible to write to the first folder");
                                }
                            }
                            else
                            {
                                if (b2Present && !b1Ok)
                                {
                                    if (si1.AnalyzeForTestOrRestore(b2, index))
                                    {
                                        WriteLogFormattedLocalized(1,
                                            Resources.BlockOfAtPositionCanBeRestoredFromNoWriteFirst,
                                            fi1.FullName, index * b1.Length, fi2.FullName);

                                        WriteLog(true, 1, "Block of ", fi1.FullName, " at position ", 
                                            index * b1.Length, " can be restored from ", fi2.FullName, 
                                            " but it is not possible to write to the first folder");
                                        restore1.Add(new RestoreInfo(index * b1.Length, b2, false));
                                    }
                                };

                                if (b1Present && !b2Ok)
                                {
                                    if (si2.AnalyzeForTestOrRestore(b1, index))
                                    {
                                        WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                            fi2.FullName, index * b1.Length, fi1.FullName);
                                        WriteLog(true, 1, "Block of ", fi2.FullName, " at position ", 
                                            index * b1.Length, " will be restored from ", fi1.FullName);
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
                restore1.AddRange(si1.EndRestore(
                    out notRestoredSize1, fiSavedInfo1.FullName, this));
                notRestoredSize1 = 0;

                long notRestoredSize2 = 0;
                restore2.AddRange(si2.EndRestore(
                    out notRestoredSize2, fiSavedInfo2.FullName, this));
                notRestoredSize2 = 0;

                // now we've got the list of improvements for both files
                using (System.IO.Stream s1 = m_iFileOpenAndCopyAbstraction.Open(
                    strPathFile1, System.IO.FileMode.Open, 
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {

                    using (System.IO.Stream s2 = m_iFileOpenAndCopyAbstraction.Open(
                        strPathFile2, System.IO.FileMode.Open, 
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {
                        // let'oInputStream apply improvements of one file to the list 
                        // of the oOtherSaveInfo, whenever possible (we are in first folder readonly case)
                        foreach (RestoreInfo ri1 in restore1)
                        {
                            foreach (RestoreInfo ri2 in restore2)
                            {
                                if (ri2.Position == ri1.Position && 
                                    ri2.NotRecoverableArea && 
                                    !ri1.NotRecoverableArea)
                                {
                                    WriteLogFormattedLocalized(1,
                                        Resources.BlockOfAtPositionWillBeRestoredFrom,
                                        fi2.FullName, ri2.Position, fi1.FullName);

                                    WriteLog(true, 1, "Block of ", fi2.FullName, 
                                        " position ", ri2.Position, 
                                        " will be restored from ", fi1.FullName);
                                    ri2.Data = ri1.Data;
                                    ri2.NotRecoverableArea = false;
                                }
                            }
                        }

                        // let'oInputStream apply the definitive improvements
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea || 
                                (_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
                                ; // bForceCreateInfoBecauseDamaged = true;
                            else
                            {
                                WriteLogFormattedLocalized(1, Resources.RecoveringBlockAtOffsetOfFile,
                                    ri2.Position, fi2.FullName);
                                WriteLog(true, 1, "Recovering block of ", 
                                    fi2.FullName, " at position ", ri2.Position);
                                s1.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? 
                                    ri2.Data.Length : 
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                // we assume the block is readbable now
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
                            }
                        };



                        // let'oInputStream try to copy non-recoverable blocks from one file to another, whenever possible
                        foreach (RestoreInfo ri2 in restore2)
                        {
                            if (ri2.NotRecoverableArea && 
                                !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                readableBlocks1.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                WriteLogFormattedLocalized(1,
                                    Resources.BlockOfAtPositionWillBeCopiedFromNoMatterChecksum,
                                    fi2.FullName, ri2.Position, fi1.FullName);
                                WriteLog(true, 1, "Block of ", fi2.FullName, " at position ", 
                                    ri2.Position, " will be copied from ", 
                                    fi1.FullName, " even if checksum indicates the block is wrong");
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
                            if (ri2.NotRecoverableArea && 
                                !equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length) &&
                                !readableBlocks2.ContainsKey(ri2.Position / ri2.Data.Length))
                            {
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, ri2.Position);
                                WriteLog(true, 1, "Block of ", fi2.FullName, " position ", 
                                    ri2.Position, " is not recoverable and will be filled with a dummy");

                                s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                                int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? 
                                    ri2.Data.Length : 
                                    si2.Length - ri2.Position);
                                if (lengthToWrite > 0)
                                    ri2.Data.WriteTo(s2, lengthToWrite);
                                notRestoredSize2 += lengthToWrite;
                            }
                        };

                        s2.Close();
                    }
                    s1.Close();
                }

                if (restore2.Count > 0)
                {
                    WriteLogFormattedLocalized(0,
                        Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        restore2.Count, fi2.FullName, notRestoredSize2);

                    WriteLog(true, 0, "There were ", restore2.Count,
                        " bad blocks in ", fi2.FullName,
                        " not restored bytes: ", notRestoredSize2);
                }
                if (restore1.Count > 0)
                {
                    WriteLogFormattedLocalized(0,
                        Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        restore1.Count, fi1.FullName);
                    WriteLog(true, 0, "There remain ", restore1.Count,
                        " bad blocks in ", fi1.FullName,
                        ", because it can't be modified ");
                }
                fi2.LastWriteTimeUtc = prevLastWriteTime;

            }
            else
            {
                System.DateTime prevLastWriteTime = fi1.LastWriteTimeUtc;

                // let'oInputStream read both copies of the 
                // file that obviously are both present, without saved info
                List<RestoreInfo> restore2 = new List<RestoreInfo>();

                // now let'oInputStream try to read the files and compare 
                long notRestoredSize2 = 0;
                long badBlocks2 = 0;
                long badBlocks1 = 0;
                using (System.IO.Stream s1 = 
                    m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile1))
                {
                    using (System.IO.Stream s2 = 
                        m_iFileOpenAndCopyAbstraction.OpenRead(strPathFile2))
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
                                WriteLogFormattedLocalized(2, Resources.IOErrorReadingFile,
                                    strPathFile1, ex.Message, ex.Message);
                                WriteLog(true, 2, "I/O error while reading file \"", 
                                    strPathFile1, "\": ", ex.Message);
                                s1.Seek((index + 1) * b1.Length, 
                                    System.IO.SeekOrigin.Begin);
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
                                WriteLogFormattedLocalized(2, Resources.IOErrorReadingFile,
                                    strPathFile2, ex.Message);

                                WriteLog(true, 2, "I/O error while reading file \"", 
                                    strPathFile2, "\": ", ex.Message);
                                s2.Seek((index + 1) * b2.Length, 
                                    System.IO.SeekOrigin.Begin);
                            }

                            if (b1Present && !b2Present)
                            {
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionWillBeRestoredFrom,
                                    fi2.FullName, index * b1.Length, fi1.FullName);
                                WriteLog(true, 1, "Block of ", fi2.FullName, " position ", 
                                    index * b1.Length, " will be restored from ", fi1.FullName);
                                restore2.Add(new RestoreInfo(index * b1.Length, b1, false));
                            }
                            else
                            if (!b1Present && !b2Present)
                            {
                                WriteLogFormattedLocalized(1, Resources.BlockOfAtPositionNotRecoverableFillDumy,
                                    fi2.FullName, index * b1.Length);
                                WriteLog(true, 1, "Block of ", fi2.FullName, " at position ", 
                                    index * b1.Length, " is not recoverable and will be filled with a dummy block");
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


                using (System.IO.Stream s2 = m_iFileOpenAndCopyAbstraction.Open(
                    strPathFile2, System.IO.FileMode.Open, 
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {
                    foreach (RestoreInfo ri2 in restore2)
                    {
                        s2.Seek(ri2.Position, System.IO.SeekOrigin.Begin);

                        int lengthToWrite = (int)(si2.Length - ri2.Position >= ri2.Data.Length ? 
                            ri2.Data.Length : 
                            si2.Length - ri2.Position);
                        if (lengthToWrite > 0)
                            ri2.Data.WriteTo(s2, lengthToWrite);
                    };
                }

                if (badBlocks2 > 0)
                {
                    WriteLogFormattedLocalized(0,
                        Resources.ThereWereBadBlocksInFileNotRestoredParts,
                        badBlocks2, fi2.FullName, notRestoredSize2);
                    WriteLog(true, 0, "There were ", badBlocks2, " bad blocks in ",
                        fi2.FullName, " not restored bytes: ", notRestoredSize2);
                }
                if (badBlocks1 > 0)
                {
                    WriteLogFormattedLocalized(0,
                        Resources.ThereRemainBadBlocksInBecauseReadOnly,
                        badBlocks1, fi1.FullName);
                    WriteLog(true, 0, "There remain ", badBlocks1, " bad blocks in ",
                        fi1.FullName, ", because it can't be modified ");
                }

                fi2.LastWriteTimeUtc = prevLastWriteTime;

            }
        }


        System.Text.StringBuilder _log;
        System.IO.TextWriter _logFileLocalized;
        System.IO.TextWriter _logFile;


        //===================================================================================================
        /// <summary>
        /// Writes a log message
        /// </summary>
        /// <param name="bOnlyToFile">Indicates that there is already a message in user language
        /// and we need to write this only to log file for later debugging/bug reports</param>
        /// <param name="nIndent">The nIndent of the new message</param>
        /// <param name="aParts">Message aParts</param>
        //===================================================================================================
        public void WriteLog(
            bool bOnlyToNonlocalizedLog,
            int nIndent, 
            params object [] aParts
            )
        {
            if (_logFile != null)
            {
                if (bOnlyToNonlocalizedLog &&
                    (Resources.DefaultCulture.Equals("yes")))
                {
                    // don't write same twice to the log file
                    return;
                }

                System.DateTime utc = System.DateTime.UtcNow;
                System.DateTime now = utc.ToLocalTime();
                lock (_log)
                {
                    _logFile.Write("{0}UT\t={1}=\t", utc.ToString("yyyy-MM-dd hh:mm:ss.fff"),
                        System.Threading.Thread.CurrentThread.ManagedThreadId);
                    if (!bOnlyToNonlocalizedLog)
                    {
                        // switch back to LTR for this message in the localized log
                        if (Resources.RightToLeft.Equals("yes"))
                            _logFileLocalized.Write((char)0x200E);

                        _logFileLocalized.Write("{0}\t={1}=\t", now.ToString("F"),
                            System.Threading.Thread.CurrentThread.ManagedThreadId);
                    }

                    while (nIndent-- > 0)
                    {
                        _logFile.Write("\t");
                        if (!bOnlyToNonlocalizedLog)
                             _logFileLocalized.Write("\t");
                        if (!bOnlyToNonlocalizedLog) 
                            _log.Append("        ");
                    }

                    foreach (object part in aParts)
                    {
                        string s = part.ToString().Replace(Environment.NewLine,"");
                        if (!bOnlyToNonlocalizedLog)
                        {
                            _log.Append(s);
                            _logFileLocalized.Write(s);
                        }
                        _logFile.Write(s);
                    }


                    if (!bOnlyToNonlocalizedLog)
                    {
                        _log.Append(Environment.NewLine);

                        // continue with rtl
                        if (Resources.RightToLeft.Equals("yes"))
                            _logFileLocalized.Write((char)0x200F);

                        _logFileLocalized.WriteLine();
                        _logFileLocalized.Flush();
                    }
                    _logFile.WriteLine();
                    _logFile.Flush();
                }
            }
            else
            {
                if (!bOnlyToNonlocalizedLog)
                {
                    lock (_log)
                    {
                        while (nIndent-- > 0)
                        {
                            _log.Append("        ");
                        }

                        foreach (object part in aParts)
                        {
                            _log.Append(part.ToString());
                        }

                        _log.Append("\r\n");
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// Writes a log message in a formatted/localized manner
        /// </summary>
        /// <param name="nIndent">The nIndent of the new message</param>
        /// <param name="strFormat">Format for the string</param>
        /// <param name="aParams">Parameters for string format</param>
        //===================================================================================================
        public void WriteLogFormattedLocalized(
            int nIndent,
            string strFormat,
            params object[] aParams
            )
        {
            if (_logFileLocalized != null)
            {
                System.DateTime utc = System.DateTime.UtcNow;
                System.DateTime now = utc.ToLocalTime();
                lock (_log)
                {
                    _logFileLocalized.Write("{0}\t={1}=\t", now.ToString("F"),
                        System.Threading.Thread.CurrentThread.ManagedThreadId);

                    while (nIndent-- > 0)
                    {
                        _logFileLocalized.Write("\t");
                        _log.Append("        ");
                    }

                    string s = string.Format(strFormat, aParams);
                    _log.Append(s);
                    _logFileLocalized.Write(s);

                    _log.Append(Environment.NewLine);
                    _logFileLocalized.Write(Environment.NewLine);
                    _logFileLocalized.Flush();
                }
            }
            else
            {
                lock (_log)
                {
                    while (nIndent-- > 0)
                    {
                        _log.Append("        ");
                    }

                    string s = string.Format(strFormat, aParams);
                    _log.Append(s);

                    _log.Append("\r\n");
                }
            }
        }


        private void labelProgress_Click(object sender, EventArgs e)
        {

        }


    }
}
