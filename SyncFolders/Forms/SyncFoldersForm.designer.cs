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

namespace SyncFolders
{
    partial class FormSyncFolders
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSyncFolders));
            folderBrowserDialogFolder1 = new FolderBrowserDialog();
            folderBrowserDialogFolder2 = new FolderBrowserDialog();
            m_lblFolder1 = new Label();
            m_tbxFirstFolder = new TextBox();
            m_btnSelectFirstFolder = new Button();
            m_lblSecondFolder = new Label();
            m_tbxSecondFolder = new TextBox();
            m_btnSelectSecondFolder = new Button();
            m_cbCreateRestoreInfo = new CheckBox();
            m_cbTestAllFiles = new CheckBox();
            m_cbRepairBlockFailures = new CheckBox();
            m_btnSync = new Button();
            m_btnCancel = new Button();
            m_ctlProgressBar = new ProgressBar();
            m_btnSelfTest = new Button();
            m_cbPreferCopies = new CheckBox();
            m_lblProgress = new Label();
            m_cbFirstToSecond = new CheckBox();
            m_cbFirstReadonly = new CheckBox();
            m_cbDeleteFilesInSecond = new CheckBox();
            m_lblAbout = new LinkLabel();
            m_lblLicence = new LinkLabel();
            m_cbSkipRecentlyTested = new CheckBox();
            m_cbParallel = new CheckBox();
            m_cbSyncMode = new CheckBox();
            m_oTimerUpdateFileDescription = new System.Windows.Forms.Timer(components);
            m_cbIgnoreTime = new CheckBox();
            SuspendLayout();
            // 
            // folderBrowserDialogFolder1
            // 
            resources.ApplyResources(folderBrowserDialogFolder1, "folderBrowserDialogFolder1");
            folderBrowserDialogFolder1.HelpRequest += folderBrowserDialog1_HelpRequest;
            // 
            // folderBrowserDialogFolder2
            // 
            resources.ApplyResources(folderBrowserDialogFolder2, "folderBrowserDialogFolder2");
            // 
            // m_lblFolder1
            // 
            resources.ApplyResources(m_lblFolder1, "m_lblFolder1");
            m_lblFolder1.Name = "m_lblFolder1";
            // 
            // m_tbxFirstFolder
            // 
            resources.ApplyResources(m_tbxFirstFolder, "m_tbxFirstFolder");
            m_tbxFirstFolder.AutoCompleteMode = AutoCompleteMode.Suggest;
            m_tbxFirstFolder.AutoCompleteSource = AutoCompleteSource.FileSystemDirectories;
            m_tbxFirstFolder.Name = "m_tbxFirstFolder";
            m_tbxFirstFolder.TextChanged += textBoxFirstFolder_TextChanged;
            m_tbxFirstFolder.Enter += textBoxFirstFolder_Enter;
            // 
            // m_btnSelectFirstFolder
            // 
            resources.ApplyResources(m_btnSelectFirstFolder, "m_btnSelectFirstFolder");
            m_btnSelectFirstFolder.Name = "m_btnSelectFirstFolder";
            m_btnSelectFirstFolder.UseVisualStyleBackColor = true;
            m_btnSelectFirstFolder.Click += buttonSelectFirstFolder_Click;
            // 
            // m_lblSecondFolder
            // 
            resources.ApplyResources(m_lblSecondFolder, "m_lblSecondFolder");
            m_lblSecondFolder.Name = "m_lblSecondFolder";
            // 
            // m_tbxSecondFolder
            // 
            resources.ApplyResources(m_tbxSecondFolder, "m_tbxSecondFolder");
            m_tbxSecondFolder.AutoCompleteMode = AutoCompleteMode.Suggest;
            m_tbxSecondFolder.AutoCompleteSource = AutoCompleteSource.FileSystemDirectories;
            m_tbxSecondFolder.Name = "m_tbxSecondFolder";
            m_tbxSecondFolder.TextChanged += textBoxSecondFolder_TextChanged;
            // 
            // m_btnSelectSecondFolder
            // 
            resources.ApplyResources(m_btnSelectSecondFolder, "m_btnSelectSecondFolder");
            m_btnSelectSecondFolder.Name = "m_btnSelectSecondFolder";
            m_btnSelectSecondFolder.UseVisualStyleBackColor = true;
            m_btnSelectSecondFolder.Click += buttonSelectSecondFolder_Click;
            // 
            // m_cbCreateRestoreInfo
            // 
            resources.ApplyResources(m_cbCreateRestoreInfo, "m_cbCreateRestoreInfo");
            m_cbCreateRestoreInfo.Checked = true;
            m_cbCreateRestoreInfo.CheckState = CheckState.Checked;
            m_cbCreateRestoreInfo.Name = "m_cbCreateRestoreInfo";
            m_cbCreateRestoreInfo.UseVisualStyleBackColor = true;
            // 
            // m_cbTestAllFiles
            // 
            resources.ApplyResources(m_cbTestAllFiles, "m_cbTestAllFiles");
            m_cbTestAllFiles.Name = "m_cbTestAllFiles";
            m_cbTestAllFiles.UseVisualStyleBackColor = true;
            m_cbTestAllFiles.CheckedChanged += checkBoxTestAllFiles_CheckedChanged;
            // 
            // m_cbRepairBlockFailures
            // 
            resources.ApplyResources(m_cbRepairBlockFailures, "m_cbRepairBlockFailures");
            m_cbRepairBlockFailures.Checked = true;
            m_cbRepairBlockFailures.CheckState = CheckState.Checked;
            m_cbRepairBlockFailures.Name = "m_cbRepairBlockFailures";
            m_cbRepairBlockFailures.UseVisualStyleBackColor = true;
            m_cbRepairBlockFailures.CheckedChanged += checkBoxRepairBlockFailures_CheckedChanged;
            // 
            // m_btnSync
            // 
            resources.ApplyResources(m_btnSync, "m_btnSync");
            m_btnSync.Name = "m_btnSync";
            m_btnSync.UseVisualStyleBackColor = true;
            m_btnSync.Click += buttonSync_Click;
            // 
            // m_btnCancel
            // 
            resources.ApplyResources(m_btnCancel, "m_btnCancel");
            m_btnCancel.Name = "m_btnCancel";
            m_btnCancel.UseVisualStyleBackColor = true;
            m_btnCancel.Click += buttonCancel_Click;
            // 
            // m_ctlProgressBar
            // 
            resources.ApplyResources(m_ctlProgressBar, "m_ctlProgressBar");
            m_ctlProgressBar.Name = "m_ctlProgressBar";
            // 
            // m_btnSelfTest
            // 
            resources.ApplyResources(m_btnSelfTest, "m_btnSelfTest");
            m_btnSelfTest.Name = "m_btnSelfTest";
            m_btnSelfTest.UseVisualStyleBackColor = true;
            m_btnSelfTest.Click += buttonSelfTest_Click;
            // 
            // m_cbPreferCopies
            // 
            resources.ApplyResources(m_cbPreferCopies, "m_cbPreferCopies");
            m_cbPreferCopies.Name = "m_cbPreferCopies";
            m_cbPreferCopies.UseVisualStyleBackColor = true;
            // 
            // m_lblProgress
            // 
            resources.ApplyResources(m_lblProgress, "m_lblProgress");
            m_lblProgress.AutoEllipsis = true;
            m_lblProgress.Name = "m_lblProgress";
            // 
            // m_cbFirstToSecond
            // 
            resources.ApplyResources(m_cbFirstToSecond, "m_cbFirstToSecond");
            m_cbFirstToSecond.Name = "m_cbFirstToSecond";
            m_cbFirstToSecond.UseVisualStyleBackColor = true;
            m_cbFirstToSecond.CheckedChanged += checkBoxFirstToSecond_CheckedChanged;
            // 
            // m_cbFirstReadonly
            // 
            resources.ApplyResources(m_cbFirstReadonly, "m_cbFirstReadonly");
            m_cbFirstReadonly.Name = "m_cbFirstReadonly";
            m_cbFirstReadonly.UseVisualStyleBackColor = true;
            m_cbFirstReadonly.CheckedChanged += checkBoxFirstReadonly_CheckedChanged;
            // 
            // m_cbDeleteFilesInSecond
            // 
            resources.ApplyResources(m_cbDeleteFilesInSecond, "m_cbDeleteFilesInSecond");
            m_cbDeleteFilesInSecond.Name = "m_cbDeleteFilesInSecond";
            m_cbDeleteFilesInSecond.UseVisualStyleBackColor = true;
            // 
            // m_lblAbout
            // 
            resources.ApplyResources(m_lblAbout, "m_lblAbout");
            m_lblAbout.Name = "m_lblAbout";
            m_lblAbout.TabStop = true;
            m_lblAbout.LinkClicked += linkLabelAbout_LinkClicked;
            // 
            // m_lblLicence
            // 
            resources.ApplyResources(m_lblLicence, "m_lblLicence");
            m_lblLicence.Name = "m_lblLicence";
            m_lblLicence.TabStop = true;
            m_lblLicence.LinkClicked += linkLabelLicence_LinkClicked;
            // 
            // m_cbSkipRecentlyTested
            // 
            resources.ApplyResources(m_cbSkipRecentlyTested, "m_cbSkipRecentlyTested");
            m_cbSkipRecentlyTested.Checked = true;
            m_cbSkipRecentlyTested.CheckState = CheckState.Checked;
            m_cbSkipRecentlyTested.Name = "m_cbSkipRecentlyTested";
            m_cbSkipRecentlyTested.UseVisualStyleBackColor = true;
            // 
            // m_cbParallel
            // 
            resources.ApplyResources(m_cbParallel, "m_cbParallel");
            m_cbParallel.Checked = true;
            m_cbParallel.CheckState = CheckState.Checked;
            m_cbParallel.Name = "m_cbParallel";
            m_cbParallel.UseVisualStyleBackColor = true;
            m_cbParallel.CheckedChanged += checkBoxParallel_CheckedChanged;
            // 
            // m_cbSyncMode
            // 
            resources.ApplyResources(m_cbSyncMode, "m_cbSyncMode");
            m_cbSyncMode.Checked = true;
            m_cbSyncMode.CheckState = CheckState.Checked;
            m_cbSyncMode.Name = "m_cbSyncMode";
            m_cbSyncMode.UseVisualStyleBackColor = true;
            m_cbSyncMode.CheckedChanged += checkBoxSyncMode_CheckedChanged;
            // 
            // m_oTimerUpdateFileDescription
            // 
            m_oTimerUpdateFileDescription.Interval = 1000;
            m_oTimerUpdateFileDescription.Tick += timerUpdateFileDescription_Tick;
            // 
            // m_cbIgnoreTime
            // 
            resources.ApplyResources(m_cbIgnoreTime, "m_cbIgnoreTime");
            m_cbIgnoreTime.Name = "m_cbIgnoreTime";
            m_cbIgnoreTime.UseVisualStyleBackColor = true;
            m_cbIgnoreTime.CheckedChanged += checkBoxIgnoreTime_CheckedChanged;
            // 
            // FormSyncFolders
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(m_cbSyncMode);
            Controls.Add(m_cbParallel);
            Controls.Add(m_lblLicence);
            Controls.Add(m_lblAbout);
            Controls.Add(m_cbFirstToSecond);
            Controls.Add(m_lblProgress);
            Controls.Add(m_btnSelfTest);
            Controls.Add(m_ctlProgressBar);
            Controls.Add(m_btnCancel);
            Controls.Add(m_btnSync);
            Controls.Add(m_btnSelectSecondFolder);
            Controls.Add(m_tbxSecondFolder);
            Controls.Add(m_btnSelectFirstFolder);
            Controls.Add(m_tbxFirstFolder);
            Controls.Add(m_lblFolder1);
            Controls.Add(m_lblSecondFolder);
            Controls.Add(m_cbFirstReadonly);
            Controls.Add(m_cbIgnoreTime);
            Controls.Add(m_cbDeleteFilesInSecond);
            Controls.Add(m_cbCreateRestoreInfo);
            Controls.Add(m_cbTestAllFiles);
            Controls.Add(m_cbSkipRecentlyTested);
            Controls.Add(m_cbRepairBlockFailures);
            Controls.Add(m_cbPreferCopies);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "FormSyncFolders";
            FormClosing += FormSyncFolders_FormClosing;
            HelpRequested += FormSyncFolders_HelpRequested;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogFolder1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogFolder2;
        private System.Windows.Forms.Label m_lblFolder1;
        private System.Windows.Forms.TextBox m_tbxFirstFolder;
        private System.Windows.Forms.Button m_btnSelectFirstFolder;
        private System.Windows.Forms.Label m_lblSecondFolder;
        private System.Windows.Forms.TextBox m_tbxSecondFolder;
        private System.Windows.Forms.Button m_btnSelectSecondFolder;
        private System.Windows.Forms.CheckBox m_cbCreateRestoreInfo;
        private System.Windows.Forms.CheckBox m_cbTestAllFiles;
        private System.Windows.Forms.CheckBox m_cbRepairBlockFailures;
        private System.Windows.Forms.Button m_btnSync;
        private System.Windows.Forms.Button m_btnCancel;
        private System.Windows.Forms.ProgressBar m_ctlProgressBar;
        private System.Windows.Forms.Button m_btnSelfTest;
        private System.Windows.Forms.CheckBox m_cbPreferCopies;
        private System.Windows.Forms.Label m_lblProgress;
        private System.Windows.Forms.CheckBox m_cbFirstToSecond;
        private System.Windows.Forms.CheckBox m_cbFirstReadonly;
        private System.Windows.Forms.CheckBox m_cbDeleteFilesInSecond;
        private System.Windows.Forms.LinkLabel m_lblAbout;
        private System.Windows.Forms.LinkLabel m_lblLicence;
        private System.Windows.Forms.CheckBox m_cbSkipRecentlyTested;
        private System.Windows.Forms.CheckBox m_cbParallel;
        private System.Windows.Forms.CheckBox m_cbSyncMode;
        private System.Windows.Forms.Timer m_oTimerUpdateFileDescription;
        private System.Windows.Forms.CheckBox m_cbIgnoreTime;
    }
}

