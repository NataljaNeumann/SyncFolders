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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSyncFolders));
            this.folderBrowserDialogFolder1 = new System.Windows.Forms.FolderBrowserDialog();
            this.folderBrowserDialogFolder2 = new System.Windows.Forms.FolderBrowserDialog();
            this.m_lblFolder1 = new System.Windows.Forms.Label();
            this.m_tbxFirstFolder = new System.Windows.Forms.TextBox();
            this.m_btnSelectFirstFolder = new System.Windows.Forms.Button();
            this.m_lblSecondFolder = new System.Windows.Forms.Label();
            this.m_tbxSecondFolder = new System.Windows.Forms.TextBox();
            this.m_btnSelectSecondFolder = new System.Windows.Forms.Button();
            this.m_cbCreateRestoreInfo = new System.Windows.Forms.CheckBox();
            this.m_cbTestAllFiles = new System.Windows.Forms.CheckBox();
            this.m_cbRepairBlockFailures = new System.Windows.Forms.CheckBox();
            this.m_btnSync = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.m_ctlProgressBar = new System.Windows.Forms.ProgressBar();
            this.buttonSelfTest = new System.Windows.Forms.Button();
            this.m_cbPreferCopies = new System.Windows.Forms.CheckBox();
            this.m_lblProgress = new System.Windows.Forms.Label();
            this.m_cbFirstToSecond = new System.Windows.Forms.CheckBox();
            this.m_cbFirstReadonly = new System.Windows.Forms.CheckBox();
            this.m_cbDeleteFilesInSecond = new System.Windows.Forms.CheckBox();
            this.m_lblAbout = new System.Windows.Forms.LinkLabel();
            this.m_lblLicence = new System.Windows.Forms.LinkLabel();
            this.m_cbSkipRecentlyTested = new System.Windows.Forms.CheckBox();
            this.m_cbParallel = new System.Windows.Forms.CheckBox();
            this.m_cbSyncMode = new System.Windows.Forms.CheckBox();
            this.m_oTimerUpdateFileDescription = new System.Windows.Forms.Timer(this.components);
            this.m_cbIgnoreTime = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // folderBrowserDialogFolder1
            // 
            resources.ApplyResources(this.folderBrowserDialogFolder1, "folderBrowserDialogFolder1");
            this.folderBrowserDialogFolder1.HelpRequest += new System.EventHandler(this.folderBrowserDialog1_HelpRequest);
            // 
            // folderBrowserDialogFolder2
            // 
            resources.ApplyResources(this.folderBrowserDialogFolder2, "folderBrowserDialogFolder2");
            // 
            // m_lblFolder1
            // 
            this.m_lblFolder1.AccessibleDescription = null;
            this.m_lblFolder1.AccessibleName = null;
            resources.ApplyResources(this.m_lblFolder1, "labelFolder1");
            this.m_lblFolder1.Font = null;
            this.m_lblFolder1.Name = "labelFolder1";
            // 
            // m_tbxFirstFolder
            // 
            this.m_tbxFirstFolder.AccessibleDescription = null;
            this.m_tbxFirstFolder.AccessibleName = null;
            resources.ApplyResources(this.m_tbxFirstFolder, "textBoxFirstFolder");
            this.m_tbxFirstFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.m_tbxFirstFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.m_tbxFirstFolder.BackgroundImage = null;
            this.m_tbxFirstFolder.Font = null;
            this.m_tbxFirstFolder.Name = "textBoxFirstFolder";
            this.m_tbxFirstFolder.TextChanged += new System.EventHandler(this.textBoxFirstFolder_TextChanged);
            this.m_tbxFirstFolder.Enter += new System.EventHandler(this.textBoxFirstFolder_Enter);
            // 
            // m_btnSelectFirstFolder
            // 
            this.m_btnSelectFirstFolder.AccessibleDescription = null;
            this.m_btnSelectFirstFolder.AccessibleName = null;
            resources.ApplyResources(this.m_btnSelectFirstFolder, "buttonSelectFirstFolder");
            this.m_btnSelectFirstFolder.BackgroundImage = null;
            this.m_btnSelectFirstFolder.Font = null;
            this.m_btnSelectFirstFolder.Name = "buttonSelectFirstFolder";
            this.m_btnSelectFirstFolder.UseVisualStyleBackColor = true;
            this.m_btnSelectFirstFolder.Click += new System.EventHandler(this.buttonSelectFirstFolder_Click);
            // 
            // m_lblSecondFolder
            // 
            this.m_lblSecondFolder.AccessibleDescription = null;
            this.m_lblSecondFolder.AccessibleName = null;
            resources.ApplyResources(this.m_lblSecondFolder, "labelSecondFolder");
            this.m_lblSecondFolder.Font = null;
            this.m_lblSecondFolder.Name = "labelSecondFolder";
            // 
            // m_tbxSecondFolder
            // 
            this.m_tbxSecondFolder.AccessibleDescription = null;
            this.m_tbxSecondFolder.AccessibleName = null;
            resources.ApplyResources(this.m_tbxSecondFolder, "textBoxSecondFolder");
            this.m_tbxSecondFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.m_tbxSecondFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.m_tbxSecondFolder.BackgroundImage = null;
            this.m_tbxSecondFolder.Font = null;
            this.m_tbxSecondFolder.Name = "textBoxSecondFolder";
            this.m_tbxSecondFolder.TextChanged += new System.EventHandler(this.textBoxSecondFolder_TextChanged);
            // 
            // m_btnSelectSecondFolder
            // 
            this.m_btnSelectSecondFolder.AccessibleDescription = null;
            this.m_btnSelectSecondFolder.AccessibleName = null;
            resources.ApplyResources(this.m_btnSelectSecondFolder, "buttonSelectSecondFolder");
            this.m_btnSelectSecondFolder.BackgroundImage = null;
            this.m_btnSelectSecondFolder.Font = null;
            this.m_btnSelectSecondFolder.Name = "buttonSelectSecondFolder";
            this.m_btnSelectSecondFolder.UseVisualStyleBackColor = true;
            this.m_btnSelectSecondFolder.Click += new System.EventHandler(this.buttonSelectSecondFolder_Click);
            // 
            // m_cbCreateRestoreInfo
            // 
            this.m_cbCreateRestoreInfo.AccessibleDescription = null;
            this.m_cbCreateRestoreInfo.AccessibleName = null;
            resources.ApplyResources(this.m_cbCreateRestoreInfo, "checkBoxCreateRestoreInfo");
            this.m_cbCreateRestoreInfo.BackgroundImage = null;
            this.m_cbCreateRestoreInfo.Checked = true;
            this.m_cbCreateRestoreInfo.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_cbCreateRestoreInfo.Font = null;
            this.m_cbCreateRestoreInfo.Name = "checkBoxCreateRestoreInfo";
            this.m_cbCreateRestoreInfo.UseVisualStyleBackColor = true;
            // 
            // m_cbTestAllFiles
            // 
            this.m_cbTestAllFiles.AccessibleDescription = null;
            this.m_cbTestAllFiles.AccessibleName = null;
            resources.ApplyResources(this.m_cbTestAllFiles, "checkBoxTestAllFiles");
            this.m_cbTestAllFiles.BackgroundImage = null;
            this.m_cbTestAllFiles.Font = null;
            this.m_cbTestAllFiles.Name = "checkBoxTestAllFiles";
            this.m_cbTestAllFiles.UseVisualStyleBackColor = true;
            this.m_cbTestAllFiles.CheckedChanged += new System.EventHandler(this.checkBoxTestAllFiles_CheckedChanged);
            // 
            // m_cbRepairBlockFailures
            // 
            this.m_cbRepairBlockFailures.AccessibleDescription = null;
            this.m_cbRepairBlockFailures.AccessibleName = null;
            resources.ApplyResources(this.m_cbRepairBlockFailures, "checkBoxRepairBlockFailures");
            this.m_cbRepairBlockFailures.BackgroundImage = null;
            this.m_cbRepairBlockFailures.Checked = true;
            this.m_cbRepairBlockFailures.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_cbRepairBlockFailures.Font = null;
            this.m_cbRepairBlockFailures.Name = "checkBoxRepairBlockFailures";
            this.m_cbRepairBlockFailures.UseVisualStyleBackColor = true;
            this.m_cbRepairBlockFailures.CheckedChanged += new System.EventHandler(this.checkBoxRepairBlockFailures_CheckedChanged);
            // 
            // m_btnSync
            // 
            this.m_btnSync.AccessibleDescription = null;
            this.m_btnSync.AccessibleName = null;
            resources.ApplyResources(this.m_btnSync, "buttonSync");
            this.m_btnSync.BackgroundImage = null;
            this.m_btnSync.Font = null;
            this.m_btnSync.Name = "buttonSync";
            this.m_btnSync.UseVisualStyleBackColor = true;
            this.m_btnSync.Click += new System.EventHandler(this.buttonSync_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.AccessibleDescription = null;
            this.buttonCancel.AccessibleName = null;
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.BackgroundImage = null;
            this.buttonCancel.Font = null;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // m_ctlProgressBar
            // 
            this.m_ctlProgressBar.AccessibleDescription = null;
            this.m_ctlProgressBar.AccessibleName = null;
            resources.ApplyResources(this.m_ctlProgressBar, "progressBar1");
            this.m_ctlProgressBar.BackgroundImage = null;
            this.m_ctlProgressBar.Font = null;
            this.m_ctlProgressBar.Name = "progressBar1";
            // 
            // buttonSelfTest
            // 
            this.buttonSelfTest.AccessibleDescription = null;
            this.buttonSelfTest.AccessibleName = null;
            resources.ApplyResources(this.buttonSelfTest, "buttonSelfTest");
            this.buttonSelfTest.BackgroundImage = null;
            this.buttonSelfTest.Font = null;
            this.buttonSelfTest.Name = "buttonSelfTest";
            this.buttonSelfTest.UseVisualStyleBackColor = true;
            this.buttonSelfTest.Click += new System.EventHandler(this.buttonSelfTest_Click);
            // 
            // m_cbPreferCopies
            // 
            this.m_cbPreferCopies.AccessibleDescription = null;
            this.m_cbPreferCopies.AccessibleName = null;
            resources.ApplyResources(this.m_cbPreferCopies, "checkBoxPreferCopies");
            this.m_cbPreferCopies.BackgroundImage = null;
            this.m_cbPreferCopies.Font = null;
            this.m_cbPreferCopies.Name = "checkBoxPreferCopies";
            this.m_cbPreferCopies.UseVisualStyleBackColor = true;
            // 
            // m_lblProgress
            // 
            this.m_lblProgress.AccessibleDescription = null;
            this.m_lblProgress.AccessibleName = null;
            resources.ApplyResources(this.m_lblProgress, "labelProgress");
            this.m_lblProgress.AutoEllipsis = true;
            this.m_lblProgress.Font = null;
            this.m_lblProgress.Name = "labelProgress";
            // 
            // m_cbFirstToSecond
            // 
            this.m_cbFirstToSecond.AccessibleDescription = null;
            this.m_cbFirstToSecond.AccessibleName = null;
            resources.ApplyResources(this.m_cbFirstToSecond, "checkBoxFirstToSecond");
            this.m_cbFirstToSecond.BackgroundImage = null;
            this.m_cbFirstToSecond.Font = null;
            this.m_cbFirstToSecond.Name = "checkBoxFirstToSecond";
            this.m_cbFirstToSecond.UseVisualStyleBackColor = true;
            this.m_cbFirstToSecond.CheckedChanged += new System.EventHandler(this.checkBoxFirstToSecond_CheckedChanged);
            // 
            // m_cbFirstReadonly
            // 
            this.m_cbFirstReadonly.AccessibleDescription = null;
            this.m_cbFirstReadonly.AccessibleName = null;
            resources.ApplyResources(this.m_cbFirstReadonly, "checkBoxFirstReadonly");
            this.m_cbFirstReadonly.BackgroundImage = null;
            this.m_cbFirstReadonly.Font = null;
            this.m_cbFirstReadonly.Name = "checkBoxFirstReadonly";
            this.m_cbFirstReadonly.UseVisualStyleBackColor = true;
            this.m_cbFirstReadonly.CheckedChanged += new System.EventHandler(this.checkBoxFirstReadonly_CheckedChanged);
            // 
            // m_cbDeleteFilesInSecond
            // 
            this.m_cbDeleteFilesInSecond.AccessibleDescription = null;
            this.m_cbDeleteFilesInSecond.AccessibleName = null;
            resources.ApplyResources(this.m_cbDeleteFilesInSecond, "checkBoxDeleteFilesInSecond");
            this.m_cbDeleteFilesInSecond.BackgroundImage = null;
            this.m_cbDeleteFilesInSecond.Font = null;
            this.m_cbDeleteFilesInSecond.Name = "checkBoxDeleteFilesInSecond";
            this.m_cbDeleteFilesInSecond.UseVisualStyleBackColor = true;
            // 
            // m_lblAbout
            // 
            this.m_lblAbout.AccessibleDescription = null;
            this.m_lblAbout.AccessibleName = null;
            resources.ApplyResources(this.m_lblAbout, "linkLabelAbout");
            this.m_lblAbout.Font = null;
            this.m_lblAbout.Name = "linkLabelAbout";
            this.m_lblAbout.TabStop = true;
            this.m_lblAbout.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAbout_LinkClicked);
            // 
            // m_lblLicence
            // 
            this.m_lblLicence.AccessibleDescription = null;
            this.m_lblLicence.AccessibleName = null;
            resources.ApplyResources(this.m_lblLicence, "linkLabelLicence");
            this.m_lblLicence.Font = null;
            this.m_lblLicence.Name = "linkLabelLicence";
            this.m_lblLicence.TabStop = true;
            this.m_lblLicence.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelLicence_LinkClicked);
            // 
            // m_cbSkipRecentlyTested
            // 
            this.m_cbSkipRecentlyTested.AccessibleDescription = null;
            this.m_cbSkipRecentlyTested.AccessibleName = null;
            resources.ApplyResources(this.m_cbSkipRecentlyTested, "checkBoxSkipRecentlyTested");
            this.m_cbSkipRecentlyTested.BackgroundImage = null;
            this.m_cbSkipRecentlyTested.Checked = true;
            this.m_cbSkipRecentlyTested.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_cbSkipRecentlyTested.Font = null;
            this.m_cbSkipRecentlyTested.Name = "checkBoxSkipRecentlyTested";
            this.m_cbSkipRecentlyTested.UseVisualStyleBackColor = true;
            // 
            // m_cbParallel
            // 
            this.m_cbParallel.AccessibleDescription = null;
            this.m_cbParallel.AccessibleName = null;
            resources.ApplyResources(this.m_cbParallel, "checkBoxParallel");
            this.m_cbParallel.BackgroundImage = null;
            this.m_cbParallel.Checked = true;
            this.m_cbParallel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_cbParallel.Font = null;
            this.m_cbParallel.Name = "checkBoxParallel";
            this.m_cbParallel.UseVisualStyleBackColor = true;
            this.m_cbParallel.CheckedChanged += new System.EventHandler(this.checkBoxParallel_CheckedChanged);
            // 
            // m_cbSyncMode
            // 
            this.m_cbSyncMode.AccessibleDescription = null;
            this.m_cbSyncMode.AccessibleName = null;
            resources.ApplyResources(this.m_cbSyncMode, "checkBoxSyncMode");
            this.m_cbSyncMode.BackgroundImage = null;
            this.m_cbSyncMode.Checked = true;
            this.m_cbSyncMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.m_cbSyncMode.Font = null;
            this.m_cbSyncMode.Name = "checkBoxSyncMode";
            this.m_cbSyncMode.UseVisualStyleBackColor = true;
            this.m_cbSyncMode.CheckedChanged += new System.EventHandler(this.checkBoxSyncMode_CheckedChanged);
            // 
            // m_oTimerUpdateFileDescription
            // 
            this.m_oTimerUpdateFileDescription.Interval = 1000;
            this.m_oTimerUpdateFileDescription.Tick += new System.EventHandler(this.timerUpdateFileDescription_Tick);
            // 
            // m_cbIgnoreTime
            // 
            this.m_cbIgnoreTime.AccessibleDescription = null;
            this.m_cbIgnoreTime.AccessibleName = null;
            resources.ApplyResources(this.m_cbIgnoreTime, "checkBoxIgnoreTime");
            this.m_cbIgnoreTime.BackgroundImage = null;
            this.m_cbIgnoreTime.Font = null;
            this.m_cbIgnoreTime.Name = "checkBoxIgnoreTime";
            this.m_cbIgnoreTime.UseVisualStyleBackColor = true;
            this.m_cbIgnoreTime.CheckedChanged += new System.EventHandler(this.checkBoxIgnoreTime_CheckedChanged);
            // 
            // FormSyncFolders
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.m_cbIgnoreTime);
            this.Controls.Add(this.m_cbSyncMode);
            this.Controls.Add(this.m_cbParallel);
            this.Controls.Add(this.m_cbSkipRecentlyTested);
            this.Controls.Add(this.m_lblLicence);
            this.Controls.Add(this.m_lblAbout);
            this.Controls.Add(this.m_cbDeleteFilesInSecond);
            this.Controls.Add(this.m_cbFirstReadonly);
            this.Controls.Add(this.m_cbFirstToSecond);
            this.Controls.Add(this.m_lblProgress);
            this.Controls.Add(this.m_cbPreferCopies);
            this.Controls.Add(this.buttonSelfTest);
            this.Controls.Add(this.m_ctlProgressBar);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.m_btnSync);
            this.Controls.Add(this.m_cbRepairBlockFailures);
            this.Controls.Add(this.m_cbTestAllFiles);
            this.Controls.Add(this.m_cbCreateRestoreInfo);
            this.Controls.Add(this.m_btnSelectSecondFolder);
            this.Controls.Add(this.m_tbxSecondFolder);
            this.Controls.Add(this.m_lblSecondFolder);
            this.Controls.Add(this.m_btnSelectFirstFolder);
            this.Controls.Add(this.m_tbxFirstFolder);
            this.Controls.Add(this.m_lblFolder1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FormSyncFolders";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormSyncFolders_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ProgressBar m_ctlProgressBar;
        private System.Windows.Forms.Button buttonSelfTest;
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

