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
            this.folderBrowserDialogFolder1.HelpRequest += new System.EventHandler(this.folderBrowserDialog1_HelpRequest);
            // 
            // labelFolder1
            // 
            this.labelFolder1.AutoSize = true;
            this.labelFolder1.Location = new System.Drawing.Point(17, 16);
            this.labelFolder1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelFolder1.Name = "labelFolder1";
            this.labelFolder1.Size = new System.Drawing.Size(78, 16);
            this.labelFolder1.TabIndex = 0;
            this.labelFolder1.Text = "First Folder:";
            // 
            // textBoxFirstFolder
            // 
            this.textBoxFirstFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.textBoxFirstFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.textBoxFirstFolder.Location = new System.Drawing.Point(20, 36);
            this.textBoxFirstFolder.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxFirstFolder.Name = "textBoxFirstFolder";
            this.textBoxFirstFolder.Size = new System.Drawing.Size(489, 22);
            this.textBoxFirstFolder.TabIndex = 1;
            this.textBoxFirstFolder.TextChanged += new System.EventHandler(this.textBoxFirstFolder_TextChanged);
            // 
            // buttonSelectFirstFolder
            // 
            this.buttonSelectFirstFolder.Location = new System.Drawing.Point(520, 33);
            this.buttonSelectFirstFolder.Margin = new System.Windows.Forms.Padding(4);
            this.buttonSelectFirstFolder.Name = "buttonSelectFirstFolder";
            this.buttonSelectFirstFolder.Size = new System.Drawing.Size(44, 28);
            this.buttonSelectFirstFolder.TabIndex = 2;
            this.buttonSelectFirstFolder.Text = "...";
            this.buttonSelectFirstFolder.UseVisualStyleBackColor = true;
            this.buttonSelectFirstFolder.Click += new System.EventHandler(this.buttonSelectFirstFolder_Click);
            // 
            // labelSecondFolder
            // 
            this.labelSecondFolder.AutoSize = true;
            this.labelSecondFolder.Location = new System.Drawing.Point(17, 72);
            this.labelSecondFolder.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelSecondFolder.Name = "labelSecondFolder";
            this.labelSecondFolder.Size = new System.Drawing.Size(100, 16);
            this.labelSecondFolder.TabIndex = 3;
            this.labelSecondFolder.Text = "Second Folder:";
            // 
            // textBoxSecondFolder
            // 
            this.textBoxSecondFolder.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.textBoxSecondFolder.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.textBoxSecondFolder.Location = new System.Drawing.Point(20, 92);
            this.textBoxSecondFolder.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxSecondFolder.Name = "textBoxSecondFolder";
            this.textBoxSecondFolder.Size = new System.Drawing.Size(489, 22);
            this.textBoxSecondFolder.TabIndex = 4;
            this.textBoxSecondFolder.TextChanged += new System.EventHandler(this.textBoxSecondFolder_TextChanged);
            // 
            // buttonSelectSecondFolder
            // 
            this.buttonSelectSecondFolder.Location = new System.Drawing.Point(520, 92);
            this.buttonSelectSecondFolder.Margin = new System.Windows.Forms.Padding(4);
            this.buttonSelectSecondFolder.Name = "buttonSelectSecondFolder";
            this.buttonSelectSecondFolder.Size = new System.Drawing.Size(43, 28);
            this.buttonSelectSecondFolder.TabIndex = 5;
            this.buttonSelectSecondFolder.Text = "...";
            this.buttonSelectSecondFolder.UseVisualStyleBackColor = true;
            this.buttonSelectSecondFolder.Click += new System.EventHandler(this.buttonSelectSecondFolder_Click);
            // 
            // checkBoxCreateRestoreInfo
            // 
            this.checkBoxCreateRestoreInfo.AutoSize = true;
            this.checkBoxCreateRestoreInfo.Checked = true;
            this.checkBoxCreateRestoreInfo.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCreateRestoreInfo.Location = new System.Drawing.Point(20, 220);
            this.checkBoxCreateRestoreInfo.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxCreateRestoreInfo.Name = "checkBoxCreateRestoreInfo";
            this.checkBoxCreateRestoreInfo.Size = new System.Drawing.Size(336, 20);
            this.checkBoxCreateRestoreInfo.TabIndex = 6;
            this.checkBoxCreateRestoreInfo.Text = "Save some info for restoring single blocks, if missing";
            this.checkBoxCreateRestoreInfo.UseVisualStyleBackColor = true;
            // 
            // checkBoxTestAllFiles
            // 
            this.checkBoxTestAllFiles.AutoSize = true;
            this.checkBoxTestAllFiles.Location = new System.Drawing.Point(20, 248);
            this.checkBoxTestAllFiles.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxTestAllFiles.Name = "checkBoxTestAllFiles";
            this.checkBoxTestAllFiles.Size = new System.Drawing.Size(178, 20);
            this.checkBoxTestAllFiles.TabIndex = 7;
            this.checkBoxTestAllFiles.Text = "Test readability of all files";
            this.checkBoxTestAllFiles.UseVisualStyleBackColor = true;
            this.checkBoxTestAllFiles.CheckedChanged += new System.EventHandler(this.checkBoxTestAllFiles_CheckedChanged);
            // 
            // checkBoxRepairBlockFailures
            // 
            this.checkBoxRepairBlockFailures.AutoSize = true;
            this.checkBoxRepairBlockFailures.Checked = true;
            this.checkBoxRepairBlockFailures.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRepairBlockFailures.Enabled = false;
            this.checkBoxRepairBlockFailures.Location = new System.Drawing.Point(37, 276);
            this.checkBoxRepairBlockFailures.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxRepairBlockFailures.Name = "checkBoxRepairBlockFailures";
            this.checkBoxRepairBlockFailures.Size = new System.Drawing.Size(189, 20);
            this.checkBoxRepairBlockFailures.TabIndex = 8;
            this.checkBoxRepairBlockFailures.Text = "Repair single block failures";
            this.checkBoxRepairBlockFailures.UseVisualStyleBackColor = true;
            this.checkBoxRepairBlockFailures.CheckedChanged += new System.EventHandler(this.checkBoxRepairBlockFailures_CheckedChanged);
            // 
            // buttonSync
            // 
            this.buttonSync.Enabled = false;
            this.buttonSync.Location = new System.Drawing.Point(356, 358);
            this.buttonSync.Margin = new System.Windows.Forms.Padding(4);
            this.buttonSync.Name = "buttonSync";
            this.buttonSync.Size = new System.Drawing.Size(100, 28);
            this.buttonSync.TabIndex = 9;
            this.buttonSync.Text = "Sync";
            this.buttonSync.UseVisualStyleBackColor = true;
            this.buttonSync.Click += new System.EventHandler(this.buttonSync_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(464, 358);
            this.buttonCancel.Margin = new System.Windows.Forms.Padding(4);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(100, 28);
            this.buttonCancel.TabIndex = 10;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(24, 358);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(4);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(435, 28);
            this.progressBar1.TabIndex = 11;
            this.progressBar1.Visible = false;
            // 
            // buttonSelfTest
            // 
            this.buttonSelfTest.Location = new System.Drawing.Point(467, 128);
            this.buttonSelfTest.Margin = new System.Windows.Forms.Padding(4);
            this.buttonSelfTest.Name = "buttonSelfTest";
            this.buttonSelfTest.Size = new System.Drawing.Size(100, 28);
            this.buttonSelfTest.TabIndex = 12;
            this.buttonSelfTest.Text = "SelfTest";
            this.buttonSelfTest.UseVisualStyleBackColor = true;
            this.buttonSelfTest.Visible = false;
            this.buttonSelfTest.Click += new System.EventHandler(this.buttonSelfTest_Click);
            // 
            // checkBoxPreferCopies
            // 
            this.checkBoxPreferCopies.AutoSize = true;
            this.checkBoxPreferCopies.Enabled = false;
            this.checkBoxPreferCopies.Location = new System.Drawing.Point(57, 304);
            this.checkBoxPreferCopies.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxPreferCopies.Name = "checkBoxPreferCopies";
            this.checkBoxPreferCopies.Size = new System.Drawing.Size(469, 20);
            this.checkBoxPreferCopies.TabIndex = 13;
            this.checkBoxPreferCopies.Text = "Prefer physical copies in case of conflicts during repair (not recommended)";
            this.checkBoxPreferCopies.UseVisualStyleBackColor = true;
            // 
            // labelProgress
            // 
            this.labelProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProgress.AutoEllipsis = true;
            this.labelProgress.Location = new System.Drawing.Point(21, 300);
            this.labelProgress.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(543, 53);
            this.labelProgress.TabIndex = 14;
            this.labelProgress.Text = "...";
            this.labelProgress.Visible = false;
            // 
            // checkBoxFirstToSecond
            // 
            this.checkBoxFirstToSecond.AutoSize = true;
            this.checkBoxFirstToSecond.Location = new System.Drawing.Point(20, 137);
            this.checkBoxFirstToSecond.Name = "checkBoxFirstToSecond";
            this.checkBoxFirstToSecond.Size = new System.Drawing.Size(257, 20);
            this.checkBoxFirstToSecond.TabIndex = 15;
            this.checkBoxFirstToSecond.Text = "Unidirictionally from 1st to 2nd (e.g. CD)";
            this.checkBoxFirstToSecond.UseVisualStyleBackColor = true;
            this.checkBoxFirstToSecond.CheckedChanged += new System.EventHandler(this.checkBoxFirstToSecond_CheckedChanged);
            // 
            // checkBoxFirstReadonly
            // 
            this.checkBoxFirstReadonly.AutoSize = true;
            this.checkBoxFirstReadonly.Enabled = false;
            this.checkBoxFirstReadonly.Location = new System.Drawing.Point(37, 164);
            this.checkBoxFirstReadonly.Name = "checkBoxFirstReadonly";
            this.checkBoxFirstReadonly.Size = new System.Drawing.Size(198, 20);
            this.checkBoxFirstReadonly.TabIndex = 16;
            this.checkBoxFirstReadonly.Text = "First folder is not changeable";
            this.checkBoxFirstReadonly.UseVisualStyleBackColor = true;
            this.checkBoxFirstReadonly.CheckedChanged += new System.EventHandler(this.checkBoxFirstReadonly_CheckedChanged);
            // 
            // checkBoxDeleteFilesInSecond
            // 
            this.checkBoxDeleteFilesInSecond.AutoSize = true;
            this.checkBoxDeleteFilesInSecond.Enabled = false;
            this.checkBoxDeleteFilesInSecond.Location = new System.Drawing.Point(37, 191);
            this.checkBoxDeleteFilesInSecond.Name = "checkBoxDeleteFilesInSecond";
            this.checkBoxDeleteFilesInSecond.Size = new System.Drawing.Size(321, 20);
            this.checkBoxDeleteFilesInSecond.TabIndex = 17;
            this.checkBoxDeleteFilesInSecond.Text = "Delete files in 2nd folder that aren\'t in first anymore";
            this.checkBoxDeleteFilesInSecond.UseVisualStyleBackColor = true;
            // 
            // linkLabelAbout
            // 
            this.linkLabelAbout.AutoSize = true;
            this.linkLabelAbout.Location = new System.Drawing.Point(26, 364);
            this.linkLabelAbout.Name = "linkLabelAbout";
            this.linkLabelAbout.Size = new System.Drawing.Size(125, 16);
            this.linkLabelAbout.TabIndex = 18;
            this.linkLabelAbout.TabStop = true;
            this.linkLabelAbout.Text = "About Sync Folders";
            this.linkLabelAbout.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelAbout_LinkClicked);
            // 
            // linkLabelLicence
            // 
            this.linkLabelLicence.AutoSize = true;
            this.linkLabelLicence.Location = new System.Drawing.Point(192, 364);
            this.linkLabelLicence.Name = "linkLabelLicence";
            this.linkLabelLicence.Size = new System.Drawing.Size(109, 16);
            this.linkLabelLicence.TabIndex = 19;
            this.linkLabelLicence.TabStop = true;
            this.linkLabelLicence.Text = "Licence (GPL v2)";
            this.linkLabelLicence.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelLicence_LinkClicked);
            // 
            // checkBoxSkipRecentlyTested
            // 
            this.checkBoxSkipRecentlyTested.AutoSize = true;
            this.checkBoxSkipRecentlyTested.Checked = true;
            this.checkBoxSkipRecentlyTested.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSkipRecentlyTested.Enabled = false;
            this.checkBoxSkipRecentlyTested.Location = new System.Drawing.Point(195, 248);
            this.checkBoxSkipRecentlyTested.Name = "checkBoxSkipRecentlyTested";
            this.checkBoxSkipRecentlyTested.Size = new System.Drawing.Size(144, 20);
            this.checkBoxSkipRecentlyTested.TabIndex = 20;
            this.checkBoxSkipRecentlyTested.Text = "if not tested recently";
            this.checkBoxSkipRecentlyTested.UseVisualStyleBackColor = true;
            // 
            // checkBoxParallel
            // 
            this.checkBoxParallel.AutoSize = true;
            this.checkBoxParallel.Checked = true;
            this.checkBoxParallel.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxParallel.Location = new System.Drawing.Point(386, 136);
            this.checkBoxParallel.Name = "checkBoxParallel";
            this.checkBoxParallel.Size = new System.Drawing.Size(73, 20);
            this.checkBoxParallel.TabIndex = 21;
            this.checkBoxParallel.Text = "Parallel";
            this.checkBoxParallel.UseVisualStyleBackColor = true;
            this.checkBoxParallel.CheckedChanged += new System.EventHandler(this.checkBoxParallel_CheckedChanged);
            // 
            // checkBoxSyncMode
            // 
            this.checkBoxSyncMode.AutoSize = true;
            this.checkBoxSyncMode.Checked = true;
            this.checkBoxSyncMode.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxSyncMode.Enabled = false;
            this.checkBoxSyncMode.Location = new System.Drawing.Point(276, 137);
            this.checkBoxSyncMode.Name = "checkBoxSyncMode";
            this.checkBoxSyncMode.Size = new System.Drawing.Size(92, 20);
            this.checkBoxSyncMode.TabIndex = 22;
            this.checkBoxSyncMode.Text = "SyncMode";
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
            this.checkBoxIgnoreTime.AutoSize = true;
            this.checkBoxIgnoreTime.Enabled = false;
            this.checkBoxIgnoreTime.Location = new System.Drawing.Point(242, 164);
            this.checkBoxIgnoreTime.Name = "checkBoxIgnoreTime";
            this.checkBoxIgnoreTime.Size = new System.Drawing.Size(326, 20);
            this.checkBoxIgnoreTime.TabIndex = 23;
            this.checkBoxIgnoreTime.Text = "Ignore Time Difference betwen File and Checksum";
            this.checkBoxIgnoreTime.UseVisualStyleBackColor = true;
            this.checkBoxIgnoreTime.CheckedChanged += new System.EventHandler(this.checkBoxIgnoreTime_CheckedChanged);
            // 
            // FormSyncFolders
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(580, 405);
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
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "FormSyncFolders";
            this.ShowIcon = false;
            this.Text = "Sync Folders v1.4";
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

