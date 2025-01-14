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
            this.labelFolder1 = new System.Windows.Forms.Label();
            this.textBoxFirstFolder = new System.Windows.Forms.TextBox();
            this.buttonSelectFirstFolder = new System.Windows.Forms.Button();
            this.labelSecondFolder = new System.Windows.Forms.Label();
            this.textBoxSecondFolder = new System.Windows.Forms.TextBox();
            this.buttonSelectSecondFolder = new System.Windows.Forms.Button();
            this.checkBoxCreateRestoreInfo = new System.Windows.Forms.CheckBox();
            this.checkBoxTestAllFiles = new System.Windows.Forms.CheckBox();
            this.checkBoxRepairBlockFailures = new System.Windows.Forms.CheckBox();
            this.buttonSync = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.buttonSelfTest = new System.Windows.Forms.Button();
            this.checkBoxPreferCopies = new System.Windows.Forms.CheckBox();
            this.labelProgress = new System.Windows.Forms.Label();
            this.checkBoxFirstToSecond = new System.Windows.Forms.CheckBox();
            this.checkBoxFirstReadonly = new System.Windows.Forms.CheckBox();
            this.checkBoxDeleteFilesInSecond = new System.Windows.Forms.CheckBox();
            this.linkLabelAbout = new System.Windows.Forms.LinkLabel();
            this.linkLabelLicence = new System.Windows.Forms.LinkLabel();
            this.checkBoxSkipRecentlyTested = new System.Windows.Forms.CheckBox();
            this.checkBoxParallel = new System.Windows.Forms.CheckBox();
            this.checkBoxSyncMode = new System.Windows.Forms.CheckBox();
            this.timerUpdateFileDescription = new System.Windows.Forms.Timer(this.components);
            this.checkBoxIgnoreTime = new System.Windows.Forms.CheckBox();
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
            // labelFolder1
            // 
            this.labelFolder1.AccessibleDescription = null;
            this.labelFolder1.AccessibleName = null;
            resources.ApplyResources(this.labelFolder1, "labelFolder1");
            this.labelFolder1.Font = null;
            this.labelFolder1.Name = "labelFolder1";
            // 
            // textBoxFirstFolder
            // 
            this.textBoxFirstFolder.AccessibleDescription = null;
            this.textBoxFirstFolder.AccessibleName = null;
            resources.ApplyResources(this.textBoxFirstFolder, "textBoxFirstFolder");
            this.textBoxFirstFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.textBoxFirstFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.textBoxFirstFolder.BackgroundImage = null;
            this.textBoxFirstFolder.Font = null;
            this.textBoxFirstFolder.Name = "textBoxFirstFolder";
            this.textBoxFirstFolder.TextChanged += new System.EventHandler(this.textBoxFirstFolder_TextChanged);
            this.textBoxFirstFolder.Enter += new System.EventHandler(this.textBoxFirstFolder_Enter);
            // 
            // buttonSelectFirstFolder
            // 
            this.buttonSelectFirstFolder.AccessibleDescription = null;
            this.buttonSelectFirstFolder.AccessibleName = null;
            resources.ApplyResources(this.buttonSelectFirstFolder, "buttonSelectFirstFolder");
            this.buttonSelectFirstFolder.BackgroundImage = null;
            this.buttonSelectFirstFolder.Font = null;
            this.buttonSelectFirstFolder.Name = "buttonSelectFirstFolder";
            this.buttonSelectFirstFolder.UseVisualStyleBackColor = true;
            this.buttonSelectFirstFolder.Click += new System.EventHandler(this.buttonSelectFirstFolder_Click);
            // 
            // labelSecondFolder
            // 
            this.labelSecondFolder.AccessibleDescription = null;
            this.labelSecondFolder.AccessibleName = null;
            resources.ApplyResources(this.labelSecondFolder, "labelSecondFolder");
            this.labelSecondFolder.Font = null;
            this.labelSecondFolder.Name = "labelSecondFolder";
            // 
            // textBoxSecondFolder
            // 
            this.textBoxSecondFolder.AccessibleDescription = null;
            this.textBoxSecondFolder.AccessibleName = null;
            resources.ApplyResources(this.textBoxSecondFolder, "textBoxSecondFolder");
            this.textBoxSecondFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.textBoxSecondFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.textBoxSecondFolder.BackgroundImage = null;
            this.textBoxSecondFolder.Font = null;
            this.textBoxSecondFolder.Name = "textBoxSecondFolder";
            this.textBoxSecondFolder.TextChanged += new System.EventHandler(this.textBoxSecondFolder_TextChanged);
            // 
            // buttonSelectSecondFolder
            // 
            this.buttonSelectSecondFolder.AccessibleDescription = null;
            this.buttonSelectSecondFolder.AccessibleName = null;
            resources.ApplyResources(this.buttonSelectSecondFolder, "buttonSelectSecondFolder");
            this.buttonSelectSecondFolder.BackgroundImage = null;
            this.buttonSelectSecondFolder.Font = null;
            this.buttonSelectSecondFolder.Name = "buttonSelectSecondFolder";
            this.buttonSelectSecondFolder.UseVisualStyleBackColor = true;
            this.buttonSelectSecondFolder.Click += new System.EventHandler(this.buttonSelectSecondFolder_Click);
            // 
            // checkBoxCreateRestoreInfo
            // 
            this.checkBoxCreateRestoreInfo.AccessibleDescription = null;
            this.checkBoxCreateRestoreInfo.AccessibleName = null;
            resources.ApplyResources(this.checkBoxCreateRestoreInfo, "checkBoxCreateRestoreInfo");
            this.checkBoxCreateRestoreInfo.BackgroundImage = null;
            this.checkBoxCreateRestoreInfo.Checked = true;
            this.checkBoxCreateRestoreInfo.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCreateRestoreInfo.Font = null;
            this.checkBoxCreateRestoreInfo.Name = "checkBoxCreateRestoreInfo";
            this.checkBoxCreateRestoreInfo.UseVisualStyleBackColor = true;
            // 
            // checkBoxTestAllFiles
            // 
            this.checkBoxTestAllFiles.AccessibleDescription = null;
            this.checkBoxTestAllFiles.AccessibleName = null;
            resources.ApplyResources(this.checkBoxTestAllFiles, "checkBoxTestAllFiles");
            this.checkBoxTestAllFiles.BackgroundImage = null;
            this.checkBoxTestAllFiles.Font = null;
            this.checkBoxTestAllFiles.Name = "checkBoxTestAllFiles";
            this.checkBoxTestAllFiles.UseVisualStyleBackColor = true;
            this.checkBoxTestAllFiles.CheckedChanged += new System.EventHandler(this.checkBoxTestAllFiles_CheckedChanged);
            // 
            // checkBoxRepairBlockFailures
            // 
            this.checkBoxRepairBlockFailures.AccessibleDescription = null;
            this.checkBoxRepairBlockFailures.AccessibleName = null;
            resources.ApplyResources(this.checkBoxRepairBlockFailures, "checkBoxRepairBlockFailures");
            this.checkBoxRepairBlockFailures.BackgroundImage = null;
            this.checkBoxRepairBlockFailures.Checked = true;
            this.checkBoxRepairBlockFailures.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRepairBlockFailures.Font = null;
            this.checkBoxRepairBlockFailures.Name = "checkBoxRepairBlockFailures";
            this.checkBoxRepairBlockFailures.UseVisualStyleBackColor = true;
            this.checkBoxRepairBlockFailures.CheckedChanged += new System.EventHandler(this.checkBoxRepairBlockFailures_CheckedChanged);
            // 
            // buttonSync
            // 
            this.buttonSync.AccessibleDescription = null;
            this.buttonSync.AccessibleName = null;
            resources.ApplyResources(this.buttonSync, "buttonSync");
            this.buttonSync.BackgroundImage = null;
            this.buttonSync.Font = null;
            this.buttonSync.Name = "buttonSync";
            this.buttonSync.UseVisualStyleBackColor = true;
            this.buttonSync.Click += new System.EventHandler(this.buttonSync_Click);
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
            // progressBar1
            // 
            this.progressBar1.AccessibleDescription = null;
            this.progressBar1.AccessibleName = null;
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.BackgroundImage = null;
            this.progressBar1.Font = null;
            this.progressBar1.Name = "progressBar1";
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
            // checkBoxPreferCopies
            // 
            this.checkBoxPreferCopies.AccessibleDescription = null;
            this.checkBoxPreferCopies.AccessibleName = null;
            resources.ApplyResources(this.checkBoxPreferCopies, "checkBoxPreferCopies");
            this.checkBoxPreferCopies.BackgroundImage = null;
            this.checkBoxPreferCopies.Font = null;
            this.checkBoxPreferCopies.Name = "checkBoxPreferCopies";
            this.checkBoxPreferCopies.UseVisualStyleBackColor = true;
            // 
            // labelProgress
            // 
            this.labelProgress.AccessibleDescription = null;
            this.labelProgress.AccessibleName = null;
            resources.ApplyResources(this.labelProgress, "labelProgress");
            this.labelProgress.AutoEllipsis = true;
            this.labelProgress.Font = null;
            this.labelProgress.Name = "labelProgress";
            // 
            // checkBoxFirstToSecond
            // 
            this.checkBoxFirstToSecond.AccessibleDescription = null;
            this.checkBoxFirstToSecond.AccessibleName = null;
            resources.ApplyResources(this.checkBoxFirstToSecond, "checkBoxFirstToSecond");
            this.checkBoxFirstToSecond.BackgroundImage = null;
            this.checkBoxFirstToSecond.Font = null;
            this.checkBoxFirstToSecond.Name = "checkBoxFirstToSecond";
            this.checkBoxFirstToSecond.UseVisualStyleBackColor = true;
            this.checkBoxFirstToSecond.CheckedChanged += new System.EventHandler(this.checkBoxFirstToSecond_CheckedChanged);
            // 
            // checkBoxFirstReadonly
            // 
            this.checkBoxFirstReadonly.AccessibleDescription = null;
            this.checkBoxFirstReadonly.AccessibleName = null;
            resources.ApplyResources(this.checkBoxFirstReadonly, "checkBoxFirstReadonly");
            this.checkBoxFirstReadonly.BackgroundImage = null;
            this.checkBoxFirstReadonly.Font = null;
            this.checkBoxFirstReadonly.Name = "checkBoxFirstReadonly";
            this.checkBoxFirstReadonly.UseVisualStyleBackColor = true;
            this.checkBoxFirstReadonly.CheckedChanged += new System.EventHandler(this.checkBoxFirstReadonly_CheckedChanged);
            // 
            // checkBoxDeleteFilesInSecond
            // 
            this.checkBoxDeleteFilesInSecond.AccessibleDescription = null;
            this.checkBoxDeleteFilesInSecond.AccessibleName = null;
            resources.ApplyResources(this.checkBoxDeleteFilesInSecond, "checkBoxDeleteFilesInSecond");
            this.checkBoxDeleteFilesInSecond.BackgroundImage = null;
            this.checkBoxDeleteFilesInSecond.Font = null;
            this.checkBoxDeleteFilesInSecond.Name = "checkBoxDeleteFilesInSecond";
            this.checkBoxDeleteFilesInSecond.UseVisualStyleBackColor = true;
            // 
            // linkLabelAbout
            // 
            this.linkLabelAbout.AccessibleDescription = null;
            this.linkLabelAbout.AccessibleName = null;
            resources.ApplyResources(this.linkLabelAbout, "linkLabelAbout");
            this.linkLabelAbout.Font = null;
            this.linkLabelAbout.Name = "linkLabelAbout";
            this.linkLabelAbout.TabStop = true;
            this.linkLabelAbout.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAbout_LinkClicked);
            // 
            // linkLabelLicence
            // 
            this.linkLabelLicence.AccessibleDescription = null;
            this.linkLabelLicence.AccessibleName = null;
            resources.ApplyResources(this.linkLabelLicence, "linkLabelLicence");
            this.linkLabelLicence.Font = null;
            this.linkLabelLicence.Name = "linkLabelLicence";
            this.linkLabelLicence.TabStop = true;
            this.linkLabelLicence.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelLicence_LinkClicked);
            // 
            // checkBoxSkipRecentlyTested
            // 
            this.checkBoxSkipRecentlyTested.AccessibleDescription = null;
            this.checkBoxSkipRecentlyTested.AccessibleName = null;
            resources.ApplyResources(this.checkBoxSkipRecentlyTested, "checkBoxSkipRecentlyTested");
            this.checkBoxSkipRecentlyTested.BackgroundImage = null;
            this.checkBoxSkipRecentlyTested.Checked = true;
            this.checkBoxSkipRecentlyTested.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSkipRecentlyTested.Font = null;
            this.checkBoxSkipRecentlyTested.Name = "checkBoxSkipRecentlyTested";
            this.checkBoxSkipRecentlyTested.UseVisualStyleBackColor = true;
            // 
            // checkBoxParallel
            // 
            this.checkBoxParallel.AccessibleDescription = null;
            this.checkBoxParallel.AccessibleName = null;
            resources.ApplyResources(this.checkBoxParallel, "checkBoxParallel");
            this.checkBoxParallel.BackgroundImage = null;
            this.checkBoxParallel.Checked = true;
            this.checkBoxParallel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxParallel.Font = null;
            this.checkBoxParallel.Name = "checkBoxParallel";
            this.checkBoxParallel.UseVisualStyleBackColor = true;
            this.checkBoxParallel.CheckedChanged += new System.EventHandler(this.checkBoxParallel_CheckedChanged);
            // 
            // checkBoxSyncMode
            // 
            this.checkBoxSyncMode.AccessibleDescription = null;
            this.checkBoxSyncMode.AccessibleName = null;
            resources.ApplyResources(this.checkBoxSyncMode, "checkBoxSyncMode");
            this.checkBoxSyncMode.BackgroundImage = null;
            this.checkBoxSyncMode.Checked = true;
            this.checkBoxSyncMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSyncMode.Font = null;
            this.checkBoxSyncMode.Name = "checkBoxSyncMode";
            this.checkBoxSyncMode.UseVisualStyleBackColor = true;
            this.checkBoxSyncMode.CheckedChanged += new System.EventHandler(this.checkBoxSyncMode_CheckedChanged);
            // 
            // timerUpdateFileDescription
            // 
            this.timerUpdateFileDescription.Interval = 1000;
            this.timerUpdateFileDescription.Tick += new System.EventHandler(this.timerUpdateFileDescription_Tick);
            // 
            // checkBoxIgnoreTime
            // 
            this.checkBoxIgnoreTime.AccessibleDescription = null;
            this.checkBoxIgnoreTime.AccessibleName = null;
            resources.ApplyResources(this.checkBoxIgnoreTime, "checkBoxIgnoreTime");
            this.checkBoxIgnoreTime.BackgroundImage = null;
            this.checkBoxIgnoreTime.Font = null;
            this.checkBoxIgnoreTime.Name = "checkBoxIgnoreTime";
            this.checkBoxIgnoreTime.UseVisualStyleBackColor = true;
            this.checkBoxIgnoreTime.CheckedChanged += new System.EventHandler(this.checkBoxIgnoreTime_CheckedChanged);
            // 
            // FormSyncFolders
            // 
            this.AccessibleDescription = null;
            this.AccessibleName = null;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = null;
            this.Controls.Add(this.checkBoxIgnoreTime);
            this.Controls.Add(this.checkBoxSyncMode);
            this.Controls.Add(this.checkBoxParallel);
            this.Controls.Add(this.checkBoxSkipRecentlyTested);
            this.Controls.Add(this.linkLabelLicence);
            this.Controls.Add(this.linkLabelAbout);
            this.Controls.Add(this.checkBoxDeleteFilesInSecond);
            this.Controls.Add(this.checkBoxFirstReadonly);
            this.Controls.Add(this.checkBoxFirstToSecond);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.checkBoxPreferCopies);
            this.Controls.Add(this.buttonSelfTest);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSync);
            this.Controls.Add(this.checkBoxRepairBlockFailures);
            this.Controls.Add(this.checkBoxTestAllFiles);
            this.Controls.Add(this.checkBoxCreateRestoreInfo);
            this.Controls.Add(this.buttonSelectSecondFolder);
            this.Controls.Add(this.textBoxSecondFolder);
            this.Controls.Add(this.labelSecondFolder);
            this.Controls.Add(this.buttonSelectFirstFolder);
            this.Controls.Add(this.textBoxFirstFolder);
            this.Controls.Add(this.labelFolder1);
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
        private System.Windows.Forms.Label labelFolder1;
        private System.Windows.Forms.TextBox textBoxFirstFolder;
        private System.Windows.Forms.Button buttonSelectFirstFolder;
        private System.Windows.Forms.Label labelSecondFolder;
        private System.Windows.Forms.TextBox textBoxSecondFolder;
        private System.Windows.Forms.Button buttonSelectSecondFolder;
        private System.Windows.Forms.CheckBox checkBoxCreateRestoreInfo;
        private System.Windows.Forms.CheckBox checkBoxTestAllFiles;
        private System.Windows.Forms.CheckBox checkBoxRepairBlockFailures;
        private System.Windows.Forms.Button buttonSync;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Button buttonSelfTest;
        private System.Windows.Forms.CheckBox checkBoxPreferCopies;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.CheckBox checkBoxFirstToSecond;
        private System.Windows.Forms.CheckBox checkBoxFirstReadonly;
        private System.Windows.Forms.CheckBox checkBoxDeleteFilesInSecond;
        private System.Windows.Forms.LinkLabel linkLabelAbout;
        private System.Windows.Forms.LinkLabel linkLabelLicence;
        private System.Windows.Forms.CheckBox checkBoxSkipRecentlyTested;
        private System.Windows.Forms.CheckBox checkBoxParallel;
        private System.Windows.Forms.CheckBox checkBoxSyncMode;
        private System.Windows.Forms.Timer timerUpdateFileDescription;
        private System.Windows.Forms.CheckBox checkBoxIgnoreTime;
    }
}

