﻿/*  SyncFolders aims to help you to synchronize two folders or drives, 
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
using SyncFolders.Properties;
using SyncFolders.Taskbar;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// This is the main oForm of the application. It also implements the logic of checking the files
    /// and decisions, how to proceed with them
    /// </summary>
    //*******************************************************************************************************
    public partial class FormSyncFolders : 
        Form, 
        ILogWriter
    {
        //===================================================================================================
        // Static variables
        //===================================================================================================
        static int s_nMaxParallelCopies =
            Math.Max(System.Environment.ProcessorCount * 5 / 8, 2);
        static int s_nMaxParallelThreads =
            System.Environment.ProcessorCount * 3 / 2;

        //===================================================================================================
        // Member variables
        //===================================================================================================
        /// <summary>
        /// True iff the synchronization process is running
        /// </summary>
        bool m_bWorking;
        /// <summary>
        /// Indicates that user cliecked the cancel button
        /// </summary>
        bool m_bCancelClicked;
        /// <summary>
        /// First folder from GUI textbox
        /// </summary>
        string m_strFolder1;
        /// <summary>
        /// Second folder from GUI textbox
        /// </summary>
        string m_strFolder2;
        /// <summary>
        /// Create saved info from GUI checkbox
        /// </summary>
        bool m_bCreateInfo;
        /// <summary>
        /// Test readability of all files from GUI checkbox
        /// </summary>
        bool m_bTestFiles;
        /// <summary>
        /// Repair single block failures from GUI checkbox
        /// </summary>
        bool m_bRepairFiles;
        /// <summary>
        /// Prefer physical copies in case of error, from GUI checkbox
        /// </summary>
        bool m_bPreferPhysicalCopies;
        /// <summary>
        /// Unidirectionally, from first to second, from GUI checkbox
        /// </summary>
        bool m_bFirstToSecond;
        /// <summary>
        /// First folder can't be written to, e.g CD. From GUI checkbox
        /// </summary>
        bool m_bFirstReadOnly;
        /// <summary>
        /// Sync mode from GUI checkbox. Makes only sence, if first to second checked, too
        /// </summary>
        bool m_bFirstToSecondSyncMode;
        /// <summary>
        /// Delete files in second that aren't present in first.
        /// Makes only sence, if first to second checked, too.
        /// </summary>
        bool m_bFirstToSecondDeleteInSecond;
        /// <summary>
        /// Skip test of recently tested files.
        /// Makes only sence, if test files has been checked, too.
        /// </summary>
        bool m_bTestFilesSkipRecentlyTested = true;
        /// <summary>
        /// Ignore time difference between file and checksum.
        /// May be useful in case there are problems with times in filesystem.
        /// </summary>
        bool m_bIgnoreTimeDifferencesBetweenDataAndSaveInfo;
        /// <summary>
        /// If the application starts from a CD then we fill the first directory and forward
        /// focus to second directory textbox
        /// </summary>
        bool m_bForwardFocusToSecondFolder;

        /// <summary>
        /// Found file pairs for possible synchronization
        /// </summary>
        List<KeyValuePair<string, string>> m_aFilePairs;
        /// <summary>
        /// This is used for randomization of recently tested files
        /// </summary>
        Random m_oRandomForRecentlyChecked = new Random(DateTime.Now.Millisecond + 1000 
            * (DateTime.Now.Second + 60 * (DateTime.Now.Minute + 60 
            * (DateTime.Now.Hour + 24 * DateTime.Now.DayOfYear))));
        /// <summary>
        /// This is the index of currently processed file (the last one)
        /// </summary>
        volatile int m_nCurrentFile;
        /// <summary>
        /// This is the path of currently processed file (the last one)
        /// </summary>
        volatile string m_strCurrentPath;
        /// <summary>
        /// This is used for changing order of tests in different threads.
        /// One tests first file first, the next tests the second file first,
        /// so we reduce the competition on drives and use both drives more efficiently
        /// </summary>
        volatile bool m_bRandomOrder;



        /// <summary>
        /// Semaphore for copy file operations
        /// </summary>
        System.Threading.Semaphore m_oSemaphoreCopyFiles = 
            new System.Threading.Semaphore(s_nMaxParallelCopies, s_nMaxParallelCopies);
        /// <summary>
        /// Semaphore for parallel threads
        /// </summary>
        System.Threading.Semaphore m_oSemaphoreParallelThreads = 
            new System.Threading.Semaphore(s_nMaxParallelThreads, s_nMaxParallelThreads);
        /// <summary>
        /// Semaphore for huge reads
        /// </summary>
        System.Threading.Semaphore m_oSemaphoreHugeReads = 
            new System.Threading.Semaphore(1, 1);

        /// <summary>
        /// An object for displaying progress in task bar
        /// </summary>
        Taskbar.TaskbarProgress m_oTaskbarProgress;

        /*
        /// <summary>
        /// This is used to simulate read errors in self-test
        /// </summary>
        IFileOpenAndCopyAbstraction m_iFileOpenAndCopyAbstraction = new FileOpenAndCopyDirectly();
         */

        /// <summary>
        /// This is used to encapsulate file system, so tests can be done
        /// </summary>
        IFileOperations m_iFileSystem = new RealFileSystem();

        //===================================================================================================
        /// <summary>
        /// Constructor
        /// </summary>
        //===================================================================================================
        public FormSyncFolders()
        {
            InitializeComponent();

            // hiding desktop.ini and the icon that could have been extracted from ZIP
            HideIconAndDesktopIni();

            // create .md file and .txt file from .html, if the application is executed in
            // release process for automatic creation of readme.md and readme.txt files
            if (Program.CreateRelease)
                CreateRelease();

            // test if the startup folder is writale, change settings
            TestIfRunningFromReadonnly();

            // Add header image
            ReadyToUseImageInjection("SyncFoldersHeader.jpg");


            // Init progress in task bar, if at least windows 7
            if (IsWindows7OrLater())
            {
                m_oTaskbarProgress = new TaskbarProgress();
                m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eNoProgress);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Tests, if the application is running at least in Windows 7
        /// </summary>
        /// <returns>true iff the OS is at least Windows 7</returns>
        //===================================================================================================
        public static bool IsWindows7OrLater()
        {
            Version windows7 = new Version(6, 1); // Windows 7 version
            return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                   Environment.OSVersion.Version >= windows7;
        }

        //===================================================================================================
        /// <summary>
        /// hiding desktop.ini and the icon that could have been extracted from ZIP
        /// </summary>
        //===================================================================================================
        private void HideIconAndDesktopIni()
        {
            // some code for hiding desktop.ini and the icon that could have been extracted from ZIP
            try
            {
                string strDesktopIni = System.IO.Path.Combine(Application.StartupPath, "desktop.ini");

                if (m_iFileSystem.Exists(strDesktopIni))
                {
                    System.IO.FileAttributes attr = m_iFileSystem.GetAttributes(strDesktopIni);

                    m_iFileSystem.SetAttributes(strDesktopIni,
                        System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System |
                        attr
                        );
                }

                strDesktopIni = System.IO.Path.Combine(Application.StartupPath, "SyncFolders.ico");

                if (m_iFileSystem.Exists(strDesktopIni))
                {
                    System.IO.FileAttributes attr = m_iFileSystem.GetAttributes(strDesktopIni);

                    m_iFileSystem.SetAttributes(strDesktopIni,
                        System.IO.FileAttributes.Hidden | attr
                        );
                }
            }
            catch
            {
                // ignore
            }
        }


        //===================================================================================================
        /// <summary>
        /// create .md file and .txt file from .html, if the application is executed in
        /// release process for automatic creation of readme.md and readme.txt files
        /// </summary>
        //===================================================================================================
        private void CreateRelease()
        {
            if (Program.CreateRelease)
            {
                try
                {
                    StringBuilder strMd = new StringBuilder();
                    StringBuilder strTxt = new StringBuilder();

                    strMd.Append(Environment.NewLine);

                    // for this: 1) load the readme html
                    System.Xml.XmlDocument oHtmlReadme = new System.Xml.XmlDocument();
                    oHtmlReadme.Load(System.IO.Path.Combine(Application.StartupPath, "Readme.html"));

                    // put the first node to stack
                    Stack<System.Xml.XmlNode> oXmlStack = new Stack<System.Xml.XmlNode>();
                    oXmlStack.Push(oHtmlReadme.SelectSingleNode("/html/body"));
                    bool bDescended = true;

                    // and continue traversing the document, as long as there are nodes
                    while (oXmlStack.Count > 0)
                    {
                        System.Xml.XmlNode oXmlNode = oXmlStack.Peek();
                        if (bDescended)
                        {
                            if (oXmlNode is System.Xml.XmlElement)
                            {
                                if (oXmlNode.Name.Equals("img"))
                                {
                                    strTxt.Append("> " + oXmlNode.Attributes.GetNamedItem("src").Value);
                                    strMd.Append(string.Format("![{0}]({1})",
                                        oXmlNode.Attributes.GetNamedItem("alt").Value,
                                        oXmlNode.Attributes.GetNamedItem("src").Value));
                                }
                                if (oXmlNode.Name.Equals("br"))
                                {
                                    strTxt.Append(Environment.NewLine);
                                    strMd.Append("  " + Environment.NewLine);
                                }
                                if (oXmlNode.Name.Equals("p"))
                                {
                                    strTxt.Append(Environment.NewLine);
                                    strMd.Append(Environment.NewLine);

                                    if (oXmlNode.Attributes != null && oXmlNode.Attributes.GetNamedItem("style") != null)
                                    {
                                        strMd.AppendFormat("> [!{0}]\r\n> ",
                                            oXmlNode.Attributes.GetNamedItem("style").Value.ToUpper());
                                        strTxt.Append("> ");
                                    }

                                    if (oXmlNode.Attributes != null && oXmlNode.Attributes.GetNamedItem("dir") != null)
                                    {
                                        if (oXmlNode.Attributes.GetNamedItem("dir").Value.Equals("rtl"))
                                        {
                                            strTxt.Append((char)0x200F);
                                            strMd.Append((char)0x200F);
                                        }
                                        else
                                        {
                                            strTxt.Append((char)0x200E);
                                            strMd.Append((char)0x200E);
                                        }
                                    }
                                }

                                if (oXmlNode.Name.Equals("cite"))
                                {
                                    if (oXmlNode.Attributes != null && oXmlNode.Attributes.GetNamedItem("dir") != null)
                                    {
                                        if (oXmlNode.Attributes.GetNamedItem("dir").Value.Equals("rtl"))
                                        {
                                            strTxt.Append(" ");
                                            strMd.Append(" ");
                                            strTxt.Append((char)0x200F);
                                            strMd.Append((char)0x200F);
                                        }
                                        else
                                        if (oXmlNode.Attributes.GetNamedItem("dir").Value.Equals("ltr"))
                                        {
                                            strTxt.Append(" ");
                                            strMd.Append(" ");
                                            strTxt.Append((char)0x200E);
                                            strMd.Append((char)0x200E);
                                        }
                                    }
                                }
                                if (oXmlNode.Name.Equals("a"))
                                {
                                    if (oXmlNode.Attributes.GetNamedItem("href") != null)
                                    {
                                        if (!oXmlNode.Attributes.GetNamedItem("href").Value.Equals("#fn1"))
                                        {
                                            strMd.Append("[");
                                        }
                                    }
                                }
                                if (oXmlNode.Name.Equals("b"))
                                {
                                    strMd.Append("**");
                                }
                                if (oXmlNode.Name.Equals("i"))
                                {
                                    strMd.Append("*");
                                }
                                if (oXmlNode.Name.Equals("h1"))
                                {
                                    if (oXmlNode.Attributes != null && oXmlNode.Attributes.GetNamedItem("dir") != null)
                                    {
                                        if (oXmlNode.Attributes.GetNamedItem("dir").Value.Equals("rtl"))
                                        {
                                            strTxt.Append((char)0x200F);
                                            strMd.Append((char)0x200F);
                                        }
                                        else
                                        {
                                            strTxt.Append((char)0x200E);
                                            strMd.Append((char)0x200E);
                                        }
                                        strTxt.Append(Environment.NewLine);
                                        strMd.Append(Environment.NewLine);
                                    }

                                    strMd.Append("# ");
                                    strTxt.Append(Environment.NewLine);
                                    strTxt.Append(Environment.NewLine);
                                    strTxt.Append(Environment.NewLine);
                                    strTxt.Append(Environment.NewLine);
                                }
                                if (oXmlNode.Name.Equals("h2"))
                                {
                                    strMd.Append("## ");
                                }
                                if (oXmlNode.Name.Equals("li") && oXmlNode.ParentNode.Name.Equals("ul"))
                                {
                                    strTxt.Append("- ");
                                    strMd.Append("- ");
                                }
                                if (oXmlNode.Name.Equals("li") && oXmlNode.ParentNode.Name.Equals("ol"))
                                {
                                    int nPos = CountPreviousSiblings(oXmlNode) + 1;
                                    strTxt.Append(nPos.ToString() + ". ");
                                    strMd.Append(nPos.ToString() + ". ");
                                }

                                // continue descent
                                oXmlNode = oXmlNode.FirstChild;
                                if (oXmlNode!=null)
                                {
                                    oXmlStack.Push(oXmlNode);
                                    bDescended = true;
                                }
                                else
                                {
                                    bDescended = false;
                                }
                            }
                            else
                            {

                                string strText = oXmlNode.InnerText;
                                string strTextForMd = strText.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"); ;

                                while (strText.IndexOf(Environment.NewLine + " ") >= 0)
                                    strText = strText.Replace(Environment.NewLine + " ", Environment.NewLine);

                                if (oXmlNode.ParentNode.Attributes != null && oXmlNode.ParentNode.Attributes.GetNamedItem("style") != null)
                                {
                                    strText = strText.Replace(Environment.NewLine,
                                        Environment.NewLine+"> ");
                                    strTextForMd = strTextForMd.Replace(Environment.NewLine,
                                        Environment.NewLine + "> ");
                                }

                                if (oXmlNode.ParentNode.Attributes != null && oXmlNode.ParentNode.Attributes.GetNamedItem("dir") != null)
                                {
                                    if (oXmlNode.ParentNode.Attributes.GetNamedItem("dir").Value.Equals("rtl"))
                                    {
                                        strText = strText.Replace(".NET-Framework", (char)0x200E + ".NET-Framework" + (char)0x200F);
                                        strTextForMd = strTextForMd.Replace(".NET-Framework", (char)0x200E + ".NET-Framework" + (char)0x200F);
                                    }
                                }

                                strTxt.Append(strText);

                                if (strText.Equals("[1]"))
                                {
                                    strText = "[^1]";
                                    strTextForMd = strText;
                                }
                                if (strText.Equals("[1]:"))
                                {
                                    strText = "[^1]:";
                                    strTextForMd = strText;
                                }

                                strMd.Append(strTextForMd);

                                // then try to descend into next sibling
                                if (oXmlNode.NextSibling != null)
                                {
                                    oXmlStack.Push(oXmlStack.Pop().NextSibling);
                                    bDescended = true;
                                }
                                else
                                {
                                    // no more siblings? then return to parent
                                    bDescended = false;
                                    oXmlStack.Pop();
                                }
                            }
                        }
                        else
                        {
                            // we leave current node anyway 
                            if (oXmlNode.Name.Equals("p") ||
                                oXmlNode.Name.Equals("h1") ||
                                oXmlNode.Name.Equals("h2") ||
                                oXmlNode.Name.Equals("li"))
                            {
                                strTxt.Append(Environment.NewLine);
                                strMd.Append(Environment.NewLine);
                                if (oXmlNode.Name.Equals("h1"))
                                {
                                    strTxt.Append("___________________________________________________________");
                                    strTxt.Append("___________________________________________________________");
                                    strTxt.Append(Environment.NewLine);
                                }

                            }
                            if (oXmlNode.Name.Equals("b"))
                            {
                                strMd.Append("**");
                            }
                            if (oXmlNode.Name.Equals("i"))
                            {
                                strMd.Append("*");
                            }
                            if (oXmlNode.Name.Equals("cite"))
                            {
                                if (oXmlNode.ParentNode.Attributes != null && oXmlNode.ParentNode.Attributes.GetNamedItem("dir") != null)
                                {
                                    if (oXmlNode.ParentNode.Attributes.GetNamedItem("dir").Value.Equals("rtl"))
                                    {
                                        strTxt.Append((char)0x200F);
                                        strMd.Append((char)0x200F);
                                    }

                                    if (oXmlNode.ParentNode.Attributes.GetNamedItem("dir").Value.Equals("ltr"))
                                    {
                                        strTxt.Append((char)0x200E);
                                        strMd.Append((char)0x200E);
                                    }

                                }
                            }
                            if (oXmlNode.Name.Equals("a"))
                            {

                                System.Xml.XmlNode oXmlAttr =
                                    oXmlNode.Attributes.GetNamedItem("href");

                                if (oXmlAttr != null)
                                {
                                    if (!oXmlAttr.Value.Equals("#fn1"))
                                    {
                                        strMd.AppendFormat("]({0})", oXmlAttr.Value);
                                        if (oXmlAttr.Value.StartsWith("#"))
                                        {
                                            strTxt.Append("..");
                                        }
                                        else
                                        {
                                            strTxt.Append(": " + oXmlAttr.Value);
                                        }
                                    }
                                }
                                else
                                {
                                    oXmlAttr =
                                    oXmlNode.Attributes.GetNamedItem("name");

                                    if (!oXmlAttr.Value.Equals("fn1"))
                                    {
                                        strMd.AppendFormat("<a name=\"{0}\"></a>",
                                            oXmlNode.Attributes.GetNamedItem("name").Value);
                                    }

                                }
                            }

                            // then try to descend into next sibling
                            if (oXmlNode.NextSibling != null)
                            {
                                oXmlStack.Push(oXmlStack.Pop().NextSibling);
                                bDescended = true;
                            }
                            else
                            {
                                // no more siblings? then return to parent
                                bDescended = false;
                                oXmlStack.Pop();
                            }
                        }
                    }


                    int nPrevLength = 0;
                    while (strTxt.Length != nPrevLength)
                    {
                        nPrevLength = strTxt.Length;
                        strTxt = strTxt.Replace(((char)0x200E) + " ", (char)0x200E + "");
                        strTxt = strTxt.Replace(((char)0x200F) + " ", (char)0x200F + "");
                        strTxt = strTxt.Replace("\n ", "\n");
                        strTxt = strTxt.Replace("\n>  ", "\n> ");
                    }


                    nPrevLength = 0;
                    while (strMd.Length != nPrevLength)
                    {
                        nPrevLength = strMd.Length;
                        strMd = strMd.Replace(((char)0x200E) + " ", (char)0x200E + "");
                        strMd = strMd.Replace(((char)0x200F) + " ", (char)0x200F + "");
                        strMd = strMd.Replace("\n ", "\n");
                        strMd = strMd.Replace("\n>  ", "\n> ");
                    }

                    using (System.IO.StreamWriter oTxtWriter = new System.IO.StreamWriter(
                        System.IO.Path.Combine(Application.StartupPath, "Readme.txt"), 
                        false, Encoding.UTF8))
                    {
                        oTxtWriter.WriteLine(strTxt.ToString());
                        oTxtWriter.Flush();
                    }


                    using (System.IO.StreamWriter oMdWriter = new System.IO.StreamWriter(
                        System.IO.Path.Combine(Application.StartupPath, "..\\..\\..\\Readme.md"), 
                        false, Encoding.UTF8))
                    {
                        oMdWriter.WriteLine(strMd.ToString());
                        oMdWriter.Flush();
                    }

                } catch (Exception oEx)
                {
                    MessageBox.Show(oEx.Message, "Error in readme.html", MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
            }

            // some settings for creation of a release
            if (Program.CreateRelease)
            {
                m_tbxFirstFolder.Text = m_tbxSecondFolder.Text = Application.StartupPath;
                m_cbFirstToSecond.Checked = false;
                buttonSync_Click(this, EventArgs.Empty);
                return;
            }

        }

        //===================================================================================================
        /// <summary>
        /// Tests, if the startup folder is writable, changes checked state of checkboxes, etc.
        /// </summary>
        //===================================================================================================
        private void TestIfRunningFromReadonnly()
        {
            // try to create a file in the location of exe file
            string strTempFileName = Application.StartupPath + "test.tmp";
            bool bProgramFiles = strTempFileName.StartsWith("C:\\progra", StringComparison.InvariantCultureIgnoreCase);
            bool bFolderWritable = false;
            if (!bProgramFiles)
            {
                try
                {
                    using (IFile s = m_iFileSystem.Create(strTempFileName))
                    {
                        s.Close();
                    }

                    bFolderWritable = true;

                    m_iFileSystem.Delete(strTempFileName);
                }
                catch
                {
                    // ignore
                }
            }

            // decide, if we need to init with parent folder
            int nFoundLocalizationSubdirs = 0;
            try
            {
                Dictionary<string, bool> oLocalizationSubdirs = new Dictionary<string, bool>();
                // Afrikaans (south africa)
                oLocalizationSubdirs["af"] = true;
                // Arab
                oLocalizationSubdirs["ar"] = true;
                // Azerbaijan
                oLocalizationSubdirs["az"] = true;
                // Belarussian
                oLocalizationSubdirs["be-BY"] = true;
                // Bulgarian
                oLocalizationSubdirs["bg"] = true;
                // Tibetan
                oLocalizationSubdirs["bo-CN"] = true;
                // Bulgarian
                oLocalizationSubdirs["bg"] = true;
                // Bosnian
                oLocalizationSubdirs["bs-Latn-BA"] = true;
                // Catalun
                oLocalizationSubdirs["ca"] = true;
                // Chech
                oLocalizationSubdirs["cs"] = true;
                // dannish
                oLocalizationSubdirs["da"] = true;
                // german
                oLocalizationSubdirs["de"] = true;
                // greek
                oLocalizationSubdirs["el"] = true;
                // spanish
                oLocalizationSubdirs["es"] = true;
                // Estonian
                oLocalizationSubdirs["et"] = true;
                // farsi (persian)
                oLocalizationSubdirs["fa"] = true;
                // finnish
                oLocalizationSubdirs["fi"] = true;
                // french
                oLocalizationSubdirs["fr"] = true;
                // hebrew
                oLocalizationSubdirs["he"] = true;
                // hindi
                oLocalizationSubdirs["hi"] = true;
                // hungarian
                oLocalizationSubdirs["hu"] = true;
                // armenian
                oLocalizationSubdirs["hy"] = true;
                // indonesian
                oLocalizationSubdirs["id"] = true;
                // icelandic
                oLocalizationSubdirs["is"] = true;
                // italian
                oLocalizationSubdirs["it"] = true;
                // japanese
                oLocalizationSubdirs["ja"] = true;
                // georgian
                oLocalizationSubdirs["ka"] = true;
                // Kazakh
                oLocalizationSubdirs["kk"] = true;
                // Khmer (Kambodscha)
                oLocalizationSubdirs["km-KH"] = true;
                // Korean
                oLocalizationSubdirs["ko"] = true;
                // Kyrgis
                oLocalizationSubdirs["ky-KG"] = true;
                // Latin (Vatikan)
                oLocalizationSubdirs["la-001"] = true;
                // Lithuanian
                oLocalizationSubdirs["lt"] = true;
                // Latvian
                oLocalizationSubdirs["lv"] = true;
                // Macedonian
                oLocalizationSubdirs["mk"] = true;
                // Mongolian
                oLocalizationSubdirs["mn-MN"] = true;
                // Malaisian
                oLocalizationSubdirs["ms"] = true;
                // Dutch (Netherlands)
                oLocalizationSubdirs["nl"] = true;
                // Norsk
                oLocalizationSubdirs["no"] = true;
                // Punjabi (Pakistan)
                oLocalizationSubdirs["pa-Arab-PK"] = true;
                // Punjabi (India)
                oLocalizationSubdirs["pa-IN"] = true;
                // Polish
                oLocalizationSubdirs["pl"] = true;
                // Pashtu (Afganistan)
                oLocalizationSubdirs["ps-AF"] = true;
                // Portuguese
                oLocalizationSubdirs["pt"] = true;
                // Romanian
                oLocalizationSubdirs["ro"] = true;
                // Russian
                oLocalizationSubdirs["ru"] = true;
                // Sanskrit (India)
                oLocalizationSubdirs["sa"] = true;
                // Slovakian
                oLocalizationSubdirs["sk"] = true;
                // Slovenian
                oLocalizationSubdirs["sl"] = true;
                // Serbian
                oLocalizationSubdirs["sr"] = true;
                // Svedish
                oLocalizationSubdirs["sv"] = true;
                // Tadjikistan
                oLocalizationSubdirs["tg-Cyrl-TJ"] = true;
                // Thailand
                oLocalizationSubdirs["th"] = true;
                // Turkmenistan
                oLocalizationSubdirs["tk-TM"] = true;
                // Turkish
                oLocalizationSubdirs["tr"] = true;
                // Ukrainian
                oLocalizationSubdirs["uk"] = true;
                // Uzbekistan
                oLocalizationSubdirs["uz"] = true;
                // Vietnam
                oLocalizationSubdirs["vi"] = true;
                // Chinese simplified (Peoples republic of China)
                oLocalizationSubdirs["zh-CHS"] = true;
                // Chinese traditional (Taiwan)
                oLocalizationSubdirs["zh-CHT"] = true;

                foreach (string strSubdir in System.IO.Directory.GetDirectories(Application.StartupPath))
                {
                    if (oLocalizationSubdirs.ContainsKey(strSubdir.Substring(strSubdir.LastIndexOf('\\') + 1)))
                        ++nFoundLocalizationSubdirs;
                }
            }
            catch
            {
                // ignore
            }

#if DEBUG
            m_btnSelfTest.Visible = true;
            //checkBoxParallel.Visible = true;
#else
            if (!bProgramFiles)
            {
                if (bFolderWritable)
                {
                    m_tbxSecondFolder.Text = 
                        nFoundLocalizationSubdirs>2?
                            System.IO.Directory.GetParent(Application.StartupPath).FullName:
                            Application.StartupPath;
                }
                else
                {
                    m_tbxFirstFolder.Text = 
                        nFoundLocalizationSubdirs>2?
                            System.IO.Directory.GetParent(Application.StartupPath).FullName:
                            Application.StartupPath;

                    m_cbFirstToSecond.Checked = true;
                    m_cbFirstReadonly.Checked = true;
                    m_cbParallel.Checked = false;
                    m_bForwardFocusToSecondFolder = true;
                }
            }
#endif
        }

        //===================================================================================================
        /// <summary>
        /// Counts previous siblings of the XML node
        /// </summary>
        /// <param name="oXmlNode">Node to start with</param>
        /// <returns>Number of previous siblings. 0 if none</returns>
        //===================================================================================================
        int CountPreviousSiblings(System.Xml.XmlNode oXmlNode)
        {
            int nResult = 0;
            while ((oXmlNode = oXmlNode.PreviousSibling) != null)
                ++nResult;

            return nResult;
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when first folder text box gets focus
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">Event args</param>
        //===================================================================================================
        private void textBoxFirstFolder_Enter(object sender, EventArgs e)
        {
            if (m_bForwardFocusToSecondFolder)
            {
                m_bForwardFocusToSecondFolder = false;
                m_tbxSecondFolder.Focus();
            }
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
        /// Some filesystems have milliseconds in them, some other don't even
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
            m_cbRepairBlockFailures.Enabled = m_cbTestAllFiles.Checked;
            m_cbPreferCopies.Enabled = m_cbTestAllFiles.Checked && 
                m_cbRepairBlockFailures.Checked;
            m_cbSkipRecentlyTested.Enabled = m_cbTestAllFiles.Checked;
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
            if (!string.IsNullOrEmpty(m_tbxFirstFolder.Text))
                folderBrowserDialogFolder1.SelectedPath = m_tbxFirstFolder.Text;

            if (folderBrowserDialogFolder1.ShowDialog() == DialogResult.OK)
            {
                m_tbxFirstFolder.Text = folderBrowserDialogFolder1.SelectedPath;
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
            if (!string.IsNullOrEmpty(m_tbxSecondFolder.Text))
                folderBrowserDialogFolder2.SelectedPath = m_tbxSecondFolder.Text;

            if (folderBrowserDialogFolder2.ShowDialog() == DialogResult.OK)
            {
                m_tbxSecondFolder.Text = folderBrowserDialogFolder2.SelectedPath;
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

            m_strFolder1 = m_tbxFirstFolder.Text;
            m_strFolder2 = m_tbxSecondFolder.Text;
            m_bCreateInfo = m_cbCreateRestoreInfo.Checked;
            m_bTestFiles = m_cbTestAllFiles.Checked;
            m_bRepairFiles = m_cbRepairBlockFailures.Checked;
            m_bPreferPhysicalCopies = m_cbPreferCopies.Checked;
            m_bFirstToSecond = m_cbFirstToSecond.Checked;
            m_bFirstReadOnly = m_cbFirstReadonly.Checked;
            m_bFirstToSecondDeleteInSecond = m_cbDeleteFilesInSecond.Checked;
            m_bTestFilesSkipRecentlyTested = !m_bTestFiles || m_cbSkipRecentlyTested.Checked;
            m_bIgnoreTimeDifferencesBetweenDataAndSaveInfo = m_cbIgnoreTime.Checked;
            m_bFirstToSecondSyncMode = m_cbSyncMode.Checked;

            if (m_bFirstToSecond && m_bFirstToSecondDeleteInSecond)
            {
                IFileInfo fiDontDelete = 
                    m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                        m_strFolder2, "SyncFolders-Dont-Delete.txt"));
                if (fiDontDelete.Exists)
                {
                    System.Windows.Forms.MessageBox.Show(this, 
                        "The second folder contains file \"SyncFolders-Dont-Delete.txt\", "+
                        "the selected folder seem to be wrong for delete option", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                fiDontDelete = 
                    m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                        m_strFolder2, "SyncFolders-Don't-Delete.txt"));
                if (fiDontDelete.Exists)
                {
                    System.Windows.Forms.MessageBox.Show(this, 
                        "The second folder contains file \"SyncFolders-Don't-Delete.txt\", "+
                        "the selected folder seem to be wrong for delete option", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            m_btnSync.Visible = false;
            m_ctlProgressBar.Minimum = 0;
            m_ctlProgressBar.Maximum = 100;
            m_ctlProgressBar.Value = 0;
            m_ctlProgressBar.Visible = true;
            m_lblFolder1.Enabled = false;
            m_lblSecondFolder.Enabled = false;
            m_lblAbout.Visible = false;
            m_lblLicence.Visible = false;
            m_tbxFirstFolder.Enabled = false;
            m_tbxSecondFolder.Enabled = false;
            m_btnSelectFirstFolder.Enabled = false;
            m_btnSelectSecondFolder.Enabled = false;
            m_cbCreateRestoreInfo.Enabled = false;
            m_cbTestAllFiles.Enabled = false;
            m_cbRepairBlockFailures.Enabled = false;
            m_cbPreferCopies.Visible = false;
            m_cbFirstToSecond.Enabled = false;
            m_cbFirstReadonly.Enabled = false;
            m_cbIgnoreTime.Enabled = false;
            m_cbDeleteFilesInSecond.Enabled = false;
            m_cbSkipRecentlyTested.Enabled = false;
            m_cbSyncMode.Enabled = false;
            m_lblProgress.Visible = true;
            m_cbParallel.Enabled = false;
            m_btnSelfTest.Enabled = false;

            m_nCurrentFile = 0;
            m_strCurrentPath = null;
            m_lblProgress.Text = Properties. Resources.ScanningFolders;
            m_oTimerUpdateFileDescription.Start();




            m_bCancelClicked = false;
            m_strLogToShow = new StringBuilder();
            m_oLogFile = new System.IO.StreamWriter(
                System.IO.Path.Combine(
                System.Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), 
                "SyncFoldersLog.txt"), true, Encoding.UTF8);
            m_oLogFileLocalized = new System.IO.StreamWriter(
                System.IO.Path.Combine(
                System.Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments),
                Resources.LogFileName+".txt"), true, Encoding.UTF8);

            m_oLogFile.WriteLine("\r\n\r\n\r\n\r\n");
            m_oLogFileLocalized.WriteLine("\r\n\r\n\r\n\r\n");

            // left to right or right to left
            if (Resources.RightToLeft.Equals("yes"))
                m_oLogFileLocalized.Write((char)0x200F);
            else
                m_oLogFileLocalized.Write((char)0x200E);

 
            m_oLogFile.WriteLine("     First2Second: " + (m_bFirstToSecond ? "yes" : "no"));
            m_oLogFileLocalized.WriteLine(m_cbFirstToSecond.Text + ": " + (m_bFirstToSecond ? Resources.Yes : Resources.No));
            if (m_bFirstToSecond)
            {
                m_oLogFile.WriteLine("         SyncMode: " + (m_bFirstToSecondSyncMode ? "yes" : "no"));
                m_oLogFile.WriteLine("    FirstReadOnly: " + (m_bFirstReadOnly ? "yes" : "no"));
                m_oLogFile.WriteLine("   DeleteInSecond: " + (m_bFirstToSecondDeleteInSecond ? "yes" : "no"));

                m_oLogFileLocalized.WriteLine(m_cbFirstToSecond.Text + ":" + (m_bFirstToSecond ? Resources.Yes : Resources.No));
                m_oLogFile.WriteLine(m_cbSyncMode.Text + ": " + (m_bFirstToSecondSyncMode ? Resources.Yes : Resources.No));
                m_oLogFile.WriteLine(m_cbFirstReadonly + ": " + (m_bFirstReadOnly ? Resources.Yes : Resources.No));
                m_oLogFile.WriteLine(m_cbDeleteFilesInSecond.Text + ": " + (m_bFirstToSecondDeleteInSecond ? Resources.Yes : Resources.No));

            }

            m_oLogFile.WriteLine("CreateRestoreInfo: " + (m_bCreateInfo ? "yes" : "no"));
            m_oLogFileLocalized.WriteLine(m_cbCreateRestoreInfo.Text + ": " + (m_bCreateInfo ? Resources.Yes : Resources.No));

            m_oLogFile.WriteLine("        TestFiles: " + 
                (m_bTestFiles ? (m_bTestFilesSkipRecentlyTested ? "if not tested recently": "yes" ): "no"));
            m_oLogFileLocalized.WriteLine(m_cbTestAllFiles.Text + ": " +
                (m_bTestFiles ? (m_bTestFilesSkipRecentlyTested ? m_cbSkipRecentlyTested.Text : Resources.Yes) : Resources.No));

            if (m_bTestFiles)
            {
                m_oLogFile.WriteLine("      RepairFiles: " + (m_bRepairFiles ? "yes" : "no"));
                m_oLogFileLocalized.WriteLine(m_cbRepairBlockFailures + ": " + (m_bRepairFiles ? Resources.Yes : Resources.No));
                if (m_bRepairFiles)
                {
                    m_oLogFile.WriteLine("     PreferCopies: " + (m_bPreferPhysicalCopies ? "yes" : "no"));
                    m_oLogFile.WriteLine(m_cbPreferCopies + ": " + (m_bPreferPhysicalCopies ? Resources.Yes : Resources.No));
                }
            }
            m_oLogFile.WriteLine("         Folder 1: " + m_strFolder1);
            m_oLogFileLocalized.WriteLine(m_lblFolder1.Text + (m_lblFolder1.Text.Contains(":")?" ":": ") + m_strFolder1);
            m_oLogFile.WriteLine("         Folder 2: " + m_strFolder2);
            m_oLogFileLocalized.WriteLine(m_lblSecondFolder.Text + (m_lblSecondFolder.Text.Contains(":") ? " " : ": ") + m_strFolder2);

            // end of the path can be in wrong direction, so set it again for the separator
            if (Resources.RightToLeft.Equals("yes"))
                m_oLogFileLocalized.Write((char)0x200F);
            else
                m_oLogFileLocalized.Write((char)0x200E);

            string strQuarterOfASeparator = "###################################################";
            m_oLogFile.Write(strQuarterOfASeparator);
            m_oLogFile.Write(strQuarterOfASeparator);
            m_oLogFile.Write(strQuarterOfASeparator);
            m_oLogFile.WriteLine(strQuarterOfASeparator);
            m_oLogFileLocalized.Write(strQuarterOfASeparator);
            m_oLogFileLocalized.Write(strQuarterOfASeparator);
            m_oLogFileLocalized.Write(strQuarterOfASeparator);
            m_oLogFileLocalized.WriteLine(strQuarterOfASeparator);

            m_oLogFile.WriteLine(System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + "UT\tThread\tMessage from thread");

            DateTime dtmNow = DateTime.Now;
            string strNowF = dtmNow.ToString("F");
            string[] astrNowF = strNowF.Split(' ');
            string strDateTimeFormatted = string.Format(Resources.DateFormat, strNowF,
                FormatNumber(dtmNow.Year), FormatNumber(dtmNow.Month), FormatNumber(dtmNow.Day),
                astrNowF.Length >= 2 ? astrNowF[1] : "", FormatNumber(dtmNow.Hour), FormatNumber(dtmNow.Minute), FormatNumber(dtmNow.Second),
                astrNowF[0], astrNowF.Length >= 3 ? astrNowF[2] : "", astrNowF.Length >= 4 ? astrNowF[3] : "",
                astrNowF.Length >= 5 ? astrNowF[4] : "");
            m_oLogFileLocalized.WriteLine(strDateTimeFormatted + "\t" + "=" + Resources.ProcessNo + "=" + "\t" + Resources.Message);


            m_oLogFile.Flush();
            m_oLogFileLocalized.Flush();

            m_bWorking = true;

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
            m_cbPreferCopies.Enabled =
                m_cbTestAllFiles.Checked &&
                m_cbRepairBlockFailures.Checked;
            m_cbIgnoreTime.Enabled =
                m_cbFirstToSecond.Checked &&
                !m_cbSyncMode.Checked &&
                m_cbFirstReadonly.Checked &&
                m_cbRepairBlockFailures.Checked;
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
            m_btnSync.Enabled =
                !string.IsNullOrEmpty(m_tbxFirstFolder.Text) &&
                !string.IsNullOrEmpty(m_tbxSecondFolder.Text);
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
            m_btnSync.Enabled =
                !string.IsNullOrEmpty(m_tbxFirstFolder.Text) &&
                !string.IsNullOrEmpty(m_tbxSecondFolder.Text);
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
            m_cbDeleteFilesInSecond.Enabled =
                m_cbFirstToSecond.Checked;
            m_cbFirstReadonly.Enabled =
                m_cbFirstToSecond.Checked;
            m_cbSyncMode.Enabled =
                m_cbFirstToSecond.Checked;
            m_cbIgnoreTime.Enabled =
                m_cbFirstToSecond.Checked &&
                !m_cbSyncMode.Checked &&
                m_cbFirstReadonly.Checked &&
                m_cbRepairBlockFailures.Checked;
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
            using (AboutForm oForm = new AboutForm())
                oForm.ShowDialog(this);
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
            if (m_bWorking)
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
            if (m_cbParallel.Checked)
            {
                s_nMaxParallelCopies = Math.Max(System.Environment.ProcessorCount * 5 / 8, 2);
                s_nMaxParallelThreads = System.Environment.ProcessorCount * 3 / 2;
                m_oSemaphoreCopyFiles = new System.Threading.Semaphore(s_nMaxParallelCopies, s_nMaxParallelCopies);
                m_oSemaphoreParallelThreads = new System.Threading.Semaphore(s_nMaxParallelThreads, s_nMaxParallelThreads);
                m_oSemaphoreHugeReads = new System.Threading.Semaphore(1, 1);
            }
            else
            {
                s_nMaxParallelCopies = 1;
                s_nMaxParallelThreads = 1;
                m_oSemaphoreCopyFiles = new System.Threading.Semaphore(s_nMaxParallelCopies, s_nMaxParallelCopies);
                m_oSemaphoreParallelThreads = new System.Threading.Semaphore(s_nMaxParallelThreads, s_nMaxParallelThreads);
                m_oSemaphoreHugeReads = new System.Threading.Semaphore(1, 1);
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
            m_cbIgnoreTime.Enabled =
                m_cbFirstToSecond.Checked &&
                !m_cbSyncMode.Checked &&
                m_cbFirstReadonly.Checked &&
                m_cbRepairBlockFailures.Checked;

            if (m_cbFirstReadonly.Checked)
                m_cbParallel.Checked = false;
        }


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
                    if (m_nCurrentFile > 0)
                        m_ctlProgressBar.Value = m_nCurrentFile;
                    if (m_strCurrentPath != null)
                        m_lblProgress.Text = m_strCurrentPath;
                }));
            }
            else
            {
                if (m_nCurrentFile > 0)
                    m_ctlProgressBar.Value = m_nCurrentFile;
                if (m_strCurrentPath != null)
                    m_lblProgress.Text = m_strCurrentPath;
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
            m_cbIgnoreTime.Enabled =
                m_cbFirstToSecond.Checked &&
                !m_cbSyncMode.Checked &&
                m_cbFirstReadonly.Checked &&
                m_cbRepairBlockFailures.Checked;
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
            if (m_bWorking)
            {
                m_btnCancel.Enabled = false;
                m_bCancelClicked = true;
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
            // but collect simulated errors for later exchange of the file layer
            Dictionary<string, List<long>> oSimulatedReadErrors = new Dictionary<string, List<long>>();

            InMemoryFileSystem oInMemoryFileSystem = new InMemoryFileSystem();
            m_iFileSystem = oInMemoryFileSystem;

            m_cbParallel.Checked = false;
            System.Windows.Forms.MessageBox.Show(this, "The self test doesn't test easy conditions. " +
                "It simulates E/A errors, bad checksums and other problems, so a log full of error messages "
                +"is expected. You need to interpret the messages", "About self test", MessageBoxButtons.OK, 
                MessageBoxIcon.Information);

            DateTime dtmTimeForFile = DateTime.UtcNow;

            if (string.IsNullOrEmpty(m_tbxFirstFolder.Text))
                m_tbxFirstFolder.Text = Application.StartupPath + "\\TestFolder1";

            if (string.IsNullOrEmpty(m_tbxSecondFolder.Text))
                m_tbxSecondFolder.Text = Application.StartupPath + "\\TestFolder2";

            IDirectoryInfo di1 =
                m_iFileSystem.GetDirectoryInfo(m_tbxFirstFolder.Text);
            if (!di1.Exists)
                di1.Create();

            IDirectoryInfo di2 =
                m_iFileSystem.GetDirectoryInfo(m_tbxSecondFolder.Text);
            if (!di2.Exists)
                di2.Create();


            // clear previous selftests
            foreach (IFileInfo fi in di1.GetFiles())
                fi.Delete();

            foreach (IFileInfo fi in di2.GetFiles())
                fi.Delete();

            foreach (IDirectoryInfo di3 in di1.GetDirectories())
                di3.Delete(true);

            foreach (IDirectoryInfo di3 in di2.GetDirectories())
                di3.Delete(true);

            //*

            //---------------------------------
            m_iFileSystem.WriteAllText(System.IO.Path.Combine(
                    m_tbxFirstFolder.Text, "copy1-2.txt"), "Copy from 1 to 2\r\n");

            //---------------------------------
            m_iFileSystem.WriteAllText(System.IO.Path.Combine(
                    m_tbxSecondFolder.Text, "copy2-1.txt"), "Copy from 2 to 1\r\n");


            //---------------------------------
            Block b = new Block();
            using (IFile s = m_iFileSystem.OpenWrite(
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "restore1.txt")))
            {
                b[0] = 3;
                b.WriteTo(s, 100);
                s.Close();
            }

            IFileInfo fi2 =
                m_iFileSystem.GetFileInfo((System.IO.Path.Combine(
                    m_tbxFirstFolder.Text, "restore1.txt")));
            IDirectoryInfo di4 =
                m_iFileSystem.GetDirectoryInfo((System.IO.Path.Combine(
                    m_tbxFirstFolder.Text, "RestoreInfo")));
            di4.Create();
            SavedInfo si = new SavedInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (IFile s =
                m_iFileSystem.Create((System.IO.Path.Combine(
                    di4.FullName, "restore1.txt.chk"))))
            {
                b[0] = 1;
                si.AnalyzeForInfoCollection(b, 0);
                si.SaveTo(s);
                s.Close();
            }
            IFileInfo fi3 =
                m_iFileSystem.GetFileInfo((System.IO.Path.Combine(
                    di4.FullName, "restore1.txt.chk")));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;

            //---------------------------------
            using (IFile s =
                m_iFileSystem.Create((System.IO.Path.Combine(
                    m_tbxFirstFolder.Text, "restore2.txt"))))
            {
                b[0] = 3;
                b.WriteTo(s, b.Length);
                b.WriteTo(s, b.Length);
                s.Close();
            }
            fi2 = m_iFileSystem.GetFileInfo((System.IO.Path.Combine(
                m_tbxFirstFolder.Text, "restore2.txt")));
            si = new SavedInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (IFile s =
                m_iFileSystem.Create((System.IO.Path.Combine(
                    di4.FullName, "restore2.txt.chk"))))
            {
                b[0] = 2;
                si.AnalyzeForInfoCollection(b, 0);
                b[0] = 0;
                si.AnalyzeForInfoCollection(b, 1);
                si.SaveTo(s);
                s.Close();
            }
            fi3 = m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                di4.FullName, "restore2.txt.chk"));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;

            //---------------------------------
            using (IFile s =
                m_iFileSystem.Create(System.IO.Path.Combine(
                    m_tbxFirstFolder.Text, "restore3.txt")))
            {
                using (IFile s2 =
                    m_iFileSystem.Create(System.IO.Path.Combine(
                        m_tbxSecondFolder.Text, "restore3.txt")))
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

            fi2 = m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                m_tbxFirstFolder.Text, "restore3.txt"));
            si = new SavedInfo(fi2.Length, fi2.LastWriteTimeUtc, false);
            using (IFile s =
                m_iFileSystem.Create(System.IO.Path.Combine(
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
            fi3 = m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                di4.FullName, "restore3.txt.chk"));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;
            fi3 = m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                m_tbxSecondFolder.Text, "restore3.txt"));
            fi3.LastWriteTimeUtc = fi2.LastWriteTimeUtc;


            //---------------------------------
            m_iFileSystem.CopyFileFromReal(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "TestPicture1.jpg"));
            CreateSavedInfo(System.IO.Path.Combine(m_tbxFirstFolder.Text, "TestPicture1.jpg"),
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "RestoreInfo\\TestPicture1.jpg.chk"));
            DateTime dtmOld = m_iFileSystem.GetLastWriteTimeUtc(
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "TestPicture1.jpg"));
            using (IFile s =
                m_iFileSystem.OpenWrite(System.IO.Path.Combine(
                    m_tbxFirstFolder.Text, "TestPicture1.jpg")))
            {
                s.Seek(163840, System.IO.SeekOrigin.Begin);
                s.Write(b.m_aData, 0, b.Length);
                s.Flush();
                s.Close();
            }
            m_iFileSystem.SetLastWriteTimeUtc(System.IO.Path.Combine(
                m_tbxFirstFolder.Text, "TestPicture1.jpg"), dtmOld);

            //---------------------------------
            m_iFileSystem.CopyFileFromReal(System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "TestPicture2.jpg"));
            m_iFileSystem.SetLastWriteTimeUtc(System.IO.Path.Combine(
                m_tbxFirstFolder.Text, "TestPicture2.jpg"), dtmOld);
            CreateSavedInfo(System.IO.Path.Combine(m_tbxFirstFolder.Text, "TestPicture2.jpg"),
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"));
            m_iFileSystem.CopyFileFromReal(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(m_tbxSecondFolder.Text, "TestPicture2.jpg"));
            m_iFileSystem.SetLastWriteTimeUtc(System.IO.Path.Combine(
                m_tbxSecondFolder.Text, "TestPicture2.jpg"), dtmOld);
            CreateSavedInfo(System.IO.Path.Combine(m_tbxSecondFolder.Text, "TestPicture2.jpg"),
                System.IO.Path.Combine(m_tbxSecondFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"));
            using (IFile s =
                m_iFileSystem.OpenWrite(System.IO.Path.Combine(
                m_tbxFirstFolder.Text, "TestPicture2.jpg")))
            {
                s.Seek(81920 + 2048, System.IO.SeekOrigin.Begin);
                s.Write(b.m_aData, 0, b.Length);
                s.Flush();
                s.Close();
            }
            m_iFileSystem.SetLastWriteTimeUtc(System.IO.Path.Combine(
                m_tbxFirstFolder.Text, "TestPicture2.jpg"), dtmOld);

            using (IFile s =
                m_iFileSystem.OpenWrite(System.IO.Path.Combine(
                m_tbxSecondFolder.Text, "TestPicture2.jpg")))
            {
                s.Seek(81920 + 4096 + 2048, System.IO.SeekOrigin.Begin);
                s.Write(b.m_aData, 0, b.Length);
                s.Close();
            }
            m_iFileSystem.SetLastWriteTimeUtc(
                System.IO.Path.Combine(m_tbxSecondFolder.Text,
                "TestPicture2.jpg"), dtmOld);

            //---------------------------------
            // non-restorable test
            string strPathOfTestFile1 = CreateSelfTestFile(m_tbxFirstFolder.Text, 
                "NonRestorableBecauseNoSaveInfo.dat", 2, false, 
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile1] = new List<long>(new long[] { 0 });

            //---------------------------------
            // auto-repair test
            string strPathOfTestFile2 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "AutoRepairFromSavedInfo.dat", 2, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile2] = new List<long>(new long[] { 4096 });

            //---------------------------------
            // restore older healthy from backup
            string strPathOfTestFile3 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "RestoreOldFromBackup.dat", 2, false,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile3] = new List<long>(new long[] { 4096 });

            strPathOfTestFile3 = CreateSelfTestFile(m_tbxSecondFolder.Text,
                "RestoreOldFromBackup.dat", 2, false,
                dtmTimeForFile.AddDays(-1), dtmTimeForFile.AddDays(-1));


            //---------------------------------
            // restore older repairable from backup
            string strPathOfTestFile4 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "RestoreOldFromBackupWithRepairingBackup.dat", 2, false,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile4] = new List<long>(new long[] { 0 });

            strPathOfTestFile4 = CreateSelfTestFile(m_tbxSecondFolder.Text,
                "RestoreOldFromBackupWithRepairingBackup.dat", 2, true,
                dtmTimeForFile.AddDays(-1), dtmTimeForFile.AddDays(-1));

            oSimulatedReadErrors[strPathOfTestFile4] = new List<long>(new long[] { 4096 });
            

            //---------------------------------
            // restore file with one block failure in data file and one block failure in .chk
            string strPathOfTestFile5 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "RestoreUncorrelatedChkFailure.dat", 10, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for this file
            oSimulatedReadErrors[strPathOfTestFile5] = new List<long>(new long[] { 0 });
            oSimulatedReadErrors[strPathOfTestFile5.Replace
                ("RestoreUncorrelatedChkFailure.dat","RestoreInfo\\RestoreUncorrelatedChkFailure.dat.chk")] = new List<long>(new long[] { 8192 });

            

            //---------------------------------
            // recreate .chk file
            string strPathOfTestFile6 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "RecreeateChkFile.dat", 10, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for the chk file
            oSimulatedReadErrors[strPathOfTestFile6.Replace
                ("RecreeateChkFile.dat", "RestoreInfo\\RecreeateChkFile.dat.chk")] = new List<long>(new long[] { 8192 });


            //---------------------------------
            // recreate two .chk files
            string strPathOfTestFile7 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "Recreeate2ChkFiles.dat", 10, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for the chk files
            oSimulatedReadErrors[strPathOfTestFile7.Replace
                ("Recreeate2ChkFiles.dat", "RestoreInfo\\Recreeate2ChkFiles.dat.chk")] = new List<long>(new long[] { 8192 });

            strPathOfTestFile7 = CreateSelfTestFile(m_tbxSecondFolder.Text,
                            "Recreeate2ChkFiles.dat", 10, true,
                            dtmTimeForFile, dtmTimeForFile);

            // add simulated read errors for the chk files
            oSimulatedReadErrors[strPathOfTestFile7.Replace
                ("Recreeate2ChkFiles.dat", "RestoreInfo\\Recreeate2ChkFiles.dat.chk")] = new List<long>(new long[] { 8192 });

            
            //---------------------------------
            // this file is non-restorable with old saved info, because saved info has a read failure at position 0
            // it is restorable with new saved info
            string strPathOfTestFile8 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "NonRestorableWithOldSavedInfoBecauseOfFailureAtPos0.dat", 5, true,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read error for the data file
            oSimulatedReadErrors[strPathOfTestFile8] = new List<long>(new long[] { 4096 });
            // add simulated read error for the chk file
            oSimulatedReadErrors[strPathOfTestFile8.Replace
                ("NonRestorableWithOldSavedInfoBecauseOfFailureAtPos0.dat",
                "RestoreInfo\\NonRestorableWithOldSavedInfoBecauseOfFailureAtPos0.dat.chk")] = new List<long>(new long[] { 0 });

            //---------------------------------
            // This file has restorable old saved info
            // this file is non-restorable with old saved info, because saved info has a read failure at position 0
            string strPathOfTestFile9 = CreateSelfTestFile(m_tbxFirstFolder.Text,
                "RestorableSavedInfoVersion0.dat", 10, false,
                dtmTimeForFile, dtmTimeForFile);

            // add simulated read error for the data file
            oSimulatedReadErrors[strPathOfTestFile9] = new List<long>(new long[] { 4096 });

            // create saved info version 0
            CreateSavedInfo(strPathOfTestFile9,
                    strPathOfTestFile9.Replace
                ("RestorableSavedInfoVersion0.dat",
                "RestoreInfo\\RestorableSavedInfoVersion0.dat.chk"),
                0, false);

            oInMemoryFileSystem.SetSimulatedReadErrors(oSimulatedReadErrors);

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
            Block oBlock = new Block();
            using (IFile s = m_iFileSystem.OpenWrite(
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


            // if need saved info, then create it with the specified date and time
            if (bCreateSaveInfo)
            {
                m_iFileSystem.SetLastWriteTimeUtc(strDestDataFilePath, dtmTimeForSaveInfo);
                CreateSavedInfo(strDestDataFilePath, 
                    CreatePathOfChkFile(strFolder, "RestoreInfo", strFileName, ".chk"));
            }
            // set date and time to specified value
            m_iFileSystem.SetLastWriteTimeUtc(strDestDataFilePath, dtmTimeForFile);

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
            IFileInfo fi, 
            string strTargetPath, 
            string strReasonEn,
            string strReasonTranslated)
        {
            string strTargetPath2 = strTargetPath + ".tmp";
            try
            {
                m_iFileSystem.CopyTo(fi, strTargetPath2, true);

                IFileInfo fi2 = m_iFileSystem.GetFileInfo(strTargetPath);
                if (fi2.Exists)
                    m_iFileSystem.Delete(fi2);

                IFileInfo fi2tmp = m_iFileSystem.GetFileInfo(strTargetPath2);
                fi2tmp.MoveTo(strTargetPath);
                WriteLogFormattedLocalized(0, Resources.FileCopied, fi.FullName, 
                    strTargetPath, strReasonTranslated);
                WriteLog(true, 0, "Copied ", fi.FullName, " to ", 
                    strTargetPath, " ", strReasonEn);
            } catch
            {
                try
                {
                    System.Threading.Thread.Sleep(5000);
                    IFileInfo fi2 = m_iFileSystem.GetFileInfo(strTargetPath2);
                    if (fi2.Exists)
                        m_iFileSystem.Delete(fi2);
                } catch
                {
                    // ignore additional exceptions
                }
                throw;
            }
        }

        //===================================================================================================
        /// <summary>
        /// This is the main method of the background sync thread
        /// </summary>
        //===================================================================================================
        void SyncWorker()
        {
            // first of all search file pairs for synching
            m_aFilePairs = new List<KeyValuePair<string, string>>();
            bool bException = false;
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new EventHandler(delegate(object sender, EventArgs args)
                    {
                        m_ctlProgressBar.Style = ProgressBarStyle.Marquee;
                        if (m_oTaskbarProgress != null)
                            m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eIndeterminate);
                    }));
                }
                else
                {
                    m_ctlProgressBar.Style = ProgressBarStyle.Marquee;
                    if (m_oTaskbarProgress != null)
                        m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eIndeterminate);
                }

                FindFilePairs(m_strFolder1, m_strFolder2);
            }
            catch (Exception ex)
            {
                WriteLog(false, 0, ex.Message);
                bException = true;
            }

            if (!bException)
            {
                if (m_aFilePairs.Count != 1)
                {
                    WriteLogFormattedLocalized(0, Resources.FoundFilesForSync, m_aFilePairs.Count);
                    WriteLog(true, 0, "Found ", m_aFilePairs.Count, " files for possible synchronisation");
                }
                else
                    if (m_aFilePairs.Count == 1)
                    {
                        WriteLogFormattedLocalized(0, Resources.FoundFileForSync);
                        WriteLog(true, 0, "Found 1 file for possible synchronisation");
                    }


                // show progresss in the GUI
                if (InvokeRequired)
                {
                    Invoke(new EventHandler(delegate(object sender, EventArgs args)
                    {

                        m_ctlProgressBar.Style = ProgressBarStyle.Continuous;
                        m_ctlProgressBar.Minimum = 0;
                        m_ctlProgressBar.Maximum = m_aFilePairs.Count;
                        m_ctlProgressBar.Value = 0;

                        // show progress in task bar
                        if (m_oTaskbarProgress != null)
                        {
                            m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eNormal);
                            m_oTaskbarProgress.SetProgress(this.Handle, 0ul, (ulong)m_aFilePairs.Count);
                        }
                    }));
                }
                else
                {
                    m_ctlProgressBar.Style = ProgressBarStyle.Continuous;
                    m_ctlProgressBar.Minimum = 0;
                    m_ctlProgressBar.Maximum = m_aFilePairs.Count;
                    m_ctlProgressBar.Value = 0;

                    // show progress in task bar
                    if (m_oTaskbarProgress != null)
                    {
                        m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eNormal);
                        m_oTaskbarProgress.SetProgress(this.Handle, 0ul, (ulong)m_aFilePairs.Count);
                    }
                }

                // if user still has not clicked cancel
                if (!m_bCancelClicked)
                {
                    int currentFile = 0;


                    // sort the list, so it is in a defined order
                    SortedDictionary<string, string> sorted = new SortedDictionary<string, string>();
                    foreach (KeyValuePair<string, string> pathPair in m_aFilePairs)
                    {
                        if (!m_bFirstToSecond)
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
                        m_oSemaphoreParallelThreads.WaitOne();

                        if (m_bCancelClicked)
                        {
                            m_oSemaphoreParallelThreads.Release();
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

                        m_nCurrentFile = currentFile;
                        m_strCurrentPath = pathPair.Key;


                        // update progress bar
                        if ((++currentFile) % 10 == 0)
                        {
                            // GUI
                            if (InvokeRequired)
                            {
                                Invoke(new EventHandler(delegate(object sender, EventArgs args)
                                {
                                    m_ctlProgressBar.Value = currentFile;
                                    m_lblProgress.Text = pathPair.Key;

                                    // task bar
                                    if (m_oTaskbarProgress != null)
                                        m_oTaskbarProgress.SetProgress(this.Handle, (ulong)currentFile, (ulong)m_aFilePairs.Count);
                                }));
                            }
                            else
                            {
                                m_ctlProgressBar.Value = currentFile;
                                m_lblProgress.Text = pathPair.Key;

                                // task bar
                                if (m_oTaskbarProgress != null)
                                    m_oTaskbarProgress.SetProgress(this.Handle, (ulong)currentFile, (ulong)m_aFilePairs.Count);
                            }
                        };

                        if (m_bCancelClicked)
                            break;
                    }
                }


                // wait for all parallel threads to finnish
                for (int i = 0; i < s_nMaxParallelThreads; ++i)
                    m_oSemaphoreParallelThreads.WaitOne();

                // free the parallel threads back again
                for (int i = 0; i < s_nMaxParallelThreads; ++i)
                    m_oSemaphoreParallelThreads.Release();


                if (InvokeRequired)
                {
                    Invoke(new EventHandler(delegate(object sender, EventArgs args)
                    {
                        // change progress in task bar
                        if (m_oTaskbarProgress != null)
                            m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eIndeterminate);
                        // and in the GUI
                        m_ctlProgressBar.Style = ProgressBarStyle.Marquee;
                    }));
                } else
                {
                        // change progress in task bar
                        if (m_oTaskbarProgress != null)
                            m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eIndeterminate);
                        // and in the GUI
                        m_ctlProgressBar.Style = ProgressBarStyle.Marquee;
                }

                if (!m_bCancelClicked)
                {
                    m_strCurrentPath = Resources.DeletingObsoleteSavedInfos;
                    RemoveOldFilesAndDirs(m_strFolder1, m_strFolder2);
                }

                if (m_bCancelClicked)
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
            // m_iFileOpenAndCopyAbstraction = new FileOpenAndCopyDirectly();
            m_iFileSystem = new RealFileSystem();
#endif

            m_oLogFile.Close();
            m_oLogFileLocalized.Close();
            m_oLogFile.Dispose();
            m_oLogFileLocalized.Dispose();
            m_oLogFile = null;
            m_oLogFileLocalized = null;


            // Free pool of blocks, used during sync
            Block.FreeMemory();

            if (InvokeRequired)
            {
                Invoke(new EventHandler(delegate (object sender, EventArgs args)
                {
                    if (m_oTaskbarProgress != null)
                        m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eNoProgress);

                    m_btnSync.Visible = true;
                    m_ctlProgressBar.Visible = false;
                    m_lblFolder1.Enabled = true;
                    m_lblSecondFolder.Enabled = true;
                    m_lblAbout.Visible = true;
                    m_lblLicence.Visible = true;
                    m_tbxFirstFolder.Enabled = true;
                    m_tbxSecondFolder.Enabled = true;
                    m_btnSelectFirstFolder.Enabled = true;
                    m_btnSelectSecondFolder.Enabled = true;
                    m_cbCreateRestoreInfo.Enabled = true;
                    m_cbTestAllFiles.Enabled = true;
                    m_cbRepairBlockFailures.Enabled = m_cbTestAllFiles.Checked;
                    m_cbPreferCopies.Visible = true;
                    m_cbFirstToSecond.Enabled = true;
                    m_cbIgnoreTime.Enabled = true;
                    m_cbFirstReadonly.Enabled = m_cbFirstToSecond.Checked;
                    m_cbDeleteFilesInSecond.Enabled = m_cbFirstToSecond.Checked;
                    m_cbSkipRecentlyTested.Enabled = m_cbTestAllFiles.Checked;
                    m_cbSyncMode.Enabled = m_cbFirstToSecond.Checked;
                    m_lblProgress.Visible = false;
                    m_cbParallel.Enabled = true;
                    m_btnSelfTest.Enabled = true;

                    m_bWorking = false;
                    m_btnCancel.Enabled = true;
                    m_oTimerUpdateFileDescription.Stop();


                    if (Program.CreateRelease)
                    {
                        Close();
                    }
                    else
                    {
                        using (LogDisplayingForm form = new LogDisplayingForm())
                        {
                            form.textBoxLog.Text = m_strLogToShow.ToString();
                            form.ShowDialog(this);
                        }
                    }

                    GC.Collect();
                }));
            } else
            {
                if (m_oTaskbarProgress != null)
                    m_oTaskbarProgress.SetState(this.Handle, SyncFolders.Taskbar.TaskbarProgressState.eNoProgress);

                m_btnSync.Visible = true;
                m_ctlProgressBar.Visible = false;
                m_lblFolder1.Enabled = true;
                m_lblSecondFolder.Enabled = true;
                m_lblAbout.Visible = true;
                m_lblLicence.Visible = true;
                m_tbxFirstFolder.Enabled = true;
                m_tbxSecondFolder.Enabled = true;
                m_btnSelectFirstFolder.Enabled = true;
                m_btnSelectSecondFolder.Enabled = true;
                m_cbCreateRestoreInfo.Enabled = true;
                m_cbTestAllFiles.Enabled = true;
                m_cbRepairBlockFailures.Enabled = m_cbTestAllFiles.Checked;
                m_cbPreferCopies.Visible = true;
                m_cbFirstToSecond.Enabled = true;
                m_cbIgnoreTime.Enabled = true;
                m_cbFirstReadonly.Enabled = m_cbFirstToSecond.Checked;
                m_cbDeleteFilesInSecond.Enabled = m_cbFirstToSecond.Checked;
                m_cbSkipRecentlyTested.Enabled = m_cbTestAllFiles.Checked;
                m_cbSyncMode.Enabled = m_cbFirstToSecond.Checked;
                m_cbParallel.Enabled = true;
                m_btnSelfTest.Enabled = true;

                m_lblProgress.Visible = false;
                m_bWorking = false;
                m_btnCancel.Enabled = true;

                m_oTimerUpdateFileDescription.Stop();


                if (Program.CreateRelease)
                {
                    Close();
                }
                else
                {
                    using (LogDisplayingForm form = new LogDisplayingForm())
                    {
                        form.textBoxLog.Text = m_strLogToShow.ToString();
                        form.ShowDialog(this);
                    }
                }

                GC.Collect();
            }
        }

        //===================================================================================================
        /// <summary>
        /// Searches directories for files and subdirectories. The collected file pairs are storedd to
        /// m_aFilePairs
        /// </summary>
        /// <param name="strDirPath1">Path in subdir of first folder</param>
        /// <param name="strDirPath2">Path in subdir of second folder</param>
        //===================================================================================================
        void FindFilePairs(
            string strDirPath1, 
            string strDirPath2)
        {
            IDirectoryInfo di1 = m_iFileSystem.GetDirectoryInfo(strDirPath1);
            IDirectoryInfo di2 = m_iFileSystem.GetDirectoryInfo(strDirPath2);

            // don't sync recycle bin
            if (di1.Name.Equals("$RECYCLE.BIN", 
                StringComparison.InvariantCultureIgnoreCase))
                return;

            // don't sync system volume information
            if (di1.Name.Equals("System Volume Information", 
                StringComparison.InvariantCultureIgnoreCase))
                return;

            // if one of the directories exists while the other doesn't then create the missing one
            // and set its attributes
            if (di1.Exists && !di2.Exists)
            {
                di2.Create();
                di2 = m_iFileSystem.GetDirectoryInfo(strDirPath2);
                di2.Attributes = di1.Attributes;
            } else
            if (di2.Exists && !di1.Exists)
            {
                if (!m_bFirstToSecond)
                {
                    di1.Create();
                    di1.Attributes = di2.Attributes;
                }
            };


            if (di1.Name.Equals("RestoreInfo", 
                StringComparison.CurrentCultureIgnoreCase))
                return;
            if (m_bCreateInfo)
            {
                IDirectoryInfo di3;

                if (!m_bFirstToSecond || !m_bFirstReadOnly)
                {
                    di3 = m_iFileSystem.GetDirectoryInfo(
                        System.IO.Path.Combine(strDirPath1, "RestoreInfo"));
                    if (!di3.Exists)
                    {
                        di3.Create();
                        di3 = m_iFileSystem.GetDirectoryInfo(
                            System.IO.Path.Combine(strDirPath1, "RestoreInfo"));
                        di3.Attributes = di3.Attributes | System.IO.FileAttributes.Hidden 
                            | System.IO.FileAttributes.System;
                    }
                }

                di3 = m_iFileSystem.GetDirectoryInfo(
                    System.IO.Path.Combine(strDirPath2, "RestoreInfo"));
                if (!di3.Exists)
                {
                    di3.Create();
                    di3 = m_iFileSystem.GetDirectoryInfo(
                        System.IO.Path.Combine(strDirPath2, "RestoreInfo"));
                    di3.Attributes = di3.Attributes 
                        | System.IO.FileAttributes.Hidden | System.IO.FileAttributes.System;
                }

            }

            if (m_bFirstToSecond && m_bFirstToSecondDeleteInSecond)
            {
                IFileInfo fiDontDelete = 
                    m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                        m_strFolder2, "SyncFolders-Dont-Delete.txt"));
                if (!fiDontDelete.Exists)
                    fiDontDelete = m_iFileSystem.GetFileInfo(System.IO.Path.Combine(
                        m_strFolder2, "SyncFolders-Don't-Delete.txt"));

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
            if (di1.Exists || !m_bFirstToSecond)
            {
                foreach (IFileInfo fi1 in di1.GetFiles())
                {
                    if (fi1.Name.Length<=4 || !".tmp".Equals(
                        fi1.Name.Substring(fi1.Name.Length - 4), 
                        StringComparison.InvariantCultureIgnoreCase))
                        oFileNames[fi1.Name] = false;
                }

                foreach (IFileInfo fi2 in di2.GetFiles())
                {
                    if (fi2.Name.Length <= 4 || !".tmp".Equals(
                        fi2.Name.Substring(fi2.Name.Length - 4), 
                        StringComparison.InvariantCultureIgnoreCase))
                        oFileNames[fi2.Name] = false;
                }
            }


            foreach (string strFileName in oFileNames.Keys)
                m_aFilePairs.Add( new KeyValuePair<string,string>(
                    System.IO.Path.Combine(strDirPath1, strFileName), 
                    System.IO.Path.Combine(strDirPath2, strFileName)));


            // find subdirectories in both directories
            Dictionary<string, bool> oDirNames = new Dictionary<string, bool>();

            if (di1.Exists || !m_bFirstToSecond)
            {
                foreach (IDirectoryInfo sub1 in di1.GetDirectories())
                    oDirNames[sub1.Name] = false;

                foreach (IDirectoryInfo sub2 in di2.GetDirectories())
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
                if (m_bCancelClicked)
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
            IEnumerable<IFileInfo> iAvailableFiles, 
            IFileInfo fi, 
            FileEqualityComparer oComparer1, 
            FileEqualityComparer2 oComparer2
            )
        {
            foreach (IFileInfo fi2 in iAvailableFiles)
                if (oComparer1.Equals(fi2, fi))
                    return true;

            foreach (IFileInfo fi2 in iAvailableFiles)
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
            IDirectoryInfo di1 = m_iFileSystem.GetDirectoryInfo(strFolderPath1);
            IDirectoryInfo di2 = m_iFileSystem.GetDirectoryInfo(strFolderPath2);

            // don't sync recycle bin
            if (di1.Name.Equals("$RECYCLE.BIN", StringComparison.InvariantCultureIgnoreCase))
                return;

            // don't sync system volume information
            if (di1.Name.Equals("System Volume Information", StringComparison.InvariantCultureIgnoreCase))
                return;


            // the contents of the RestoreInfo folders is considered at their parent folders
            if (di1.Name == "RestoreInfo")
                return;

            // if one of the directories exists while the other doesn't then create the missing one
            // and set its attributes
            if (di2.Exists && !di1.Exists)
            {
                if (m_bFirstToSecond && m_bFirstToSecondDeleteInSecond)
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

            IDirectoryInfo di3;
            // consider contents of the first folder
            if (!m_bFirstToSecond || !m_bFirstReadOnly)
            {
                di3 = m_iFileSystem.GetDirectoryInfo(
                    System.IO.Path.Combine(strFolderPath1, "RestoreInfo"));

                if (di3.Exists)
                {
                    List<IFileInfo> aAvailableFiles = new List<IFileInfo>();
                    aAvailableFiles.AddRange(di1.GetFiles());

                    FileEqualityComparer feq = new FileEqualityComparer();
                    FileEqualityComparer2 feq2 = new FileEqualityComparer2();

                    foreach (IFileInfo fi in di3.GetFiles())
                    {
                        try
                        {
                            if (fi.Extension.Equals(".chk", StringComparison.InvariantCultureIgnoreCase) && 
                                !CheckIfContains(aAvailableFiles, m_iFileSystem.GetFileInfo(
                                    System.IO.Path.Combine(
                                    di3.FullName,fi.Name.Substring(0, fi.Name.Length - 4))), feq, feq2))
                            {
                                m_iFileSystem.Delete(fi);
                            } else
                            if (fi.Extension.Equals(".chked", StringComparison.InvariantCultureIgnoreCase) && 
                                !CheckIfContains(aAvailableFiles, m_iFileSystem.GetFileInfo(
                                    System.IO.Path.Combine(
                                    di3.FullName,fi.Name.Substring(0, fi.Name.Length - 6))), feq, feq2))
                            {
                                m_iFileSystem.Delete(fi);
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
            di3 = m_iFileSystem.GetDirectoryInfo(System.IO.Path.Combine(strFolderPath2, "RestoreInfo"));
            if (di3.Exists)
            {
                List<IFileInfo> aAvailableFiles = new List<IFileInfo>();
                aAvailableFiles.AddRange(di2.GetFiles());

                FileEqualityComparer feq = new FileEqualityComparer();
                FileEqualityComparer2 feq2 = new FileEqualityComparer2();

                foreach (IFileInfo fi in di3.GetFiles())
                {
                    try
                    {
                        if (fi.Extension.Equals(".chk", StringComparison.InvariantCultureIgnoreCase) && 
                            !CheckIfContains(aAvailableFiles, m_iFileSystem.GetFileInfo(
                                System.IO.Path.Combine(
                                di3.FullName, fi.Name.Substring(0, fi.Name.Length - 4))), feq, feq2))
                        {
                            m_iFileSystem.Delete(fi);
                        }
                        else
                        if (fi.Extension.Equals(".chked", StringComparison.InvariantCultureIgnoreCase) && 
                            !CheckIfContains(aAvailableFiles, m_iFileSystem.GetFileInfo(
                                System.IO.Path.Combine(
                                di3.FullName, fi.Name.Substring(0, fi.Name.Length - 6))), feq, feq2))
                        {
                            m_iFileSystem.Delete(fi);
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
                foreach (IDirectoryInfo diSubDir1 in di1.GetDirectories())
                    oDirNames[diSubDir1.Name] = false;

            if (di2.Exists)
                foreach (IDirectoryInfo diSubDir2 in di2.GetDirectories())
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
                if (m_bCancelClicked)
                    break;
            }

        }

        //***************************************************************************************************
        /// <summary>
        /// Class for comparison of file names.
        /// </summary>
        //***************************************************************************************************
        class FileEqualityComparer : IEqualityComparer<IFileInfo>
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
                IFileInfo fi1, 
                IFileInfo fi2
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
                IFileInfo obj
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
        class FileEqualityComparer2 : IEqualityComparer<IFileInfo>
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
                IFileInfo fi1, 
                IFileInfo fi2)
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
                IFileInfo obj
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
                if (!m_bCancelClicked)
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
                m_oSemaphoreParallelThreads.Release();
            }
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
                    // still too big? then try a smaller version, consisting of three chars
                    str1 = System.IO.Path.Combine(
                        System.IO.Path.Combine(strOriginalDir, strSubDirForSavedInfo),
                        strFileName.Substring(0, 1) + ((strFileName.GetHashCode()%100).ToString()) + strNewExtension);
                }
            }
            return str1;
        }



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
            IFileInfo fi1 = null, fi2 = null;

            //try
            {
                fi1 = m_iFileSystem.GetFileInfo(strFilePath1);
                fi2 = m_iFileSystem.GetFileInfo(strFilePath2);
            }
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

            if (m_bFirstToSecond)
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
            IFileInfo fi1,
            IFileInfo fi2
            )
        {
            if (m_bFirstReadOnly)
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
            IFileInfo fi1, 
            IFileInfo fi2
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            if (m_bFirstToSecondDeleteInSecond)
            {
                IFileInfo fiSavedInfo2 = 
                    m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                if (fiSavedInfo2.Exists)
                    m_iFileSystem.Delete(fiSavedInfo2);
                m_iFileSystem.Delete(fi2);
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

                if (m_bTestFiles)
                {
                    IFileInfo fiSavedInfo2 = 
                        m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    bool bForceCreateInfo = false;
                    if (m_bRepairFiles)
                        TestAndRepairSingleFile(fi2.FullName, fiSavedInfo2.FullName, ref bForceCreateInfo, false);
                    else
                        TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfo, true, !m_bTestFilesSkipRecentlyTested, true);

                    if (m_bCreateInfo && (!fiSavedInfo2.Exists || bForceCreateInfo))
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            if (fi1.Length == 0 && CheckIfZeroLengthIsInteresting(strFilePath1))
            {
                WriteLogFormattedLocalized(0, Resources.FileHasZeroLength,
                            strFilePath1);
                WriteLog(true, 0, "Warning: file has zero length, "+
                    "indicating a failed copy operation in the past: ", strFilePath1);
            }

            IFileInfo fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            IFileInfo fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreatInfo = false;
            bool bForceCreatInfo2 = false;
            if (m_bCancelClicked)
                return;
            try
            {
                m_oSemaphoreCopyFiles.WaitOne();

                if (m_bCancelClicked)
                    return;

                CopyRepairSingleFile(fi2.FullName, fi1.FullName, fiSavedInfo1.FullName, 
                    ref bForceCreatInfo, ref bForceCreatInfo2, "(file was new)", 
                    Resources.FileWasNew, false, false);
            }
            finally
            {
                m_oSemaphoreCopyFiles.Release();
            }

            if (m_bCreateInfo || fiSavedInfo1.Exists || fiSavedInfo2.Exists)
            {
                if (fiSavedInfo1.Exists && !bForceCreatInfo && !bForceCreatInfo2)
                {
                    try
                    {
                        m_oSemaphoreCopyFiles.WaitOne();

                        //CopyFileSafely(fiSavedInfo1, fiSavedInfo2.FullName);
                        m_iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    } catch 
                    {
                        CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                    }
                    finally
                    {
                        m_oSemaphoreCopyFiles.Release();
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            if (!m_bFirstToSecondSyncMode ? (!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            IFileInfo fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            IFileInfo fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo = false;

            // if the first file is ok
            if (TestSingleFile(strFilePath1, fiSavedInfo1.FullName,
                ref bForceCreateInfo, m_bTestFiles, true, false))
            {
                if (m_bCancelClicked)
                    return;
                // then simply copy it
                try
                {
                    m_oSemaphoreCopyFiles.WaitOne();

                    if (m_bCancelClicked)
                        return;

                    CopyFileSafely(fi1, strFilePath2, "(file newer or bigger)",
                        Resources.FileWasNewer);
                    //m_iFileSystem.CopyTo(fi1,strFilePath2, true);
                }
                finally
                {
                    m_oSemaphoreCopyFiles.Release();
                }

                if (m_bCreateInfo || fiSavedInfo2.Exists || fiSavedInfo1.Exists)
                {
                    if (bForceCreateInfo)
                        CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                    else
                    {
                        try
                        {
                            m_oSemaphoreCopyFiles.WaitOne();
                            m_iFileSystem.CopyTo(fiSavedInfo1, CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                        }
                        finally
                        {
                            m_oSemaphoreCopyFiles.Release();
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            IFileInfo fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            IFileInfo fiSavedInfo1 = m_iFileSystem.GetFileInfo(
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
                        m_oSemaphoreCopyFiles.WaitOne();
                        m_iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    }
                    finally
                    {
                        m_oSemaphoreCopyFiles.Release();
                    }

                    fiSavedInfo2 = m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                bool bForceCreateInfoBecauseDamaged = false;
                if (m_bTestFiles)
                    if (m_bRepairFiles)
                        TestAndRepairSecondFile(fi1.FullName, fi2.FullName, 
                            fiSavedInfo1.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfoBecauseDamaged);
                    else
                        TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfoBecauseDamaged, true, 
                            !m_bTestFilesSkipRecentlyTested, true);


                if (m_bCreateInfo && 
                    (!fiSavedInfo2.Exists || 
                        fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc || 
                        bForceCreateInfoBecauseDamaged))
                {
                    CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
                }

                fiSavedInfo2 = m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                fiSavedInfo1 = m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

                // if one of the files is missing or has wrong date, 
                // but the other is OK then copy the one to the other
                if (fiSavedInfo1.Exists && 
                    fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc && 
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
                {
                    try
                    {
                        m_oSemaphoreCopyFiles.WaitOne();
                        m_iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                    }
                    finally
                    {
                        m_oSemaphoreCopyFiles.Release();
                    }
                }
            } else
            {
                bool bForceCreateInfoBecauseDamaged = false;
                bool bOK = true;
                if (m_bTestFiles)
                {
                    // test first file
                    TestSingleFile(strFilePath1, CreatePathOfChkFile(
                        fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), 
                        ref bForceCreateInfoBecauseDamaged, true, !m_bTestFilesSkipRecentlyTested, true);

                    // test or repair second file, which is different from first
                    if (m_bRepairFiles)
                        TestAndRepairSingleFile(strFilePath2, CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), 
                            ref bForceCreateInfoBecauseDamaged, false);
                    else
                        bOK = TestSingleFile(strFilePath2, CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), 
                            ref bForceCreateInfoBecauseDamaged, true, !m_bTestFilesSkipRecentlyTested, true);

                    if (bOK && m_bCreateInfo && 
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
            IFileInfo fi1, 
            IFileInfo fi2
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
                            "indicating a failed copy operation in the past: ", 
                            strFilePath1, ", ", strFilePath2);
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            if (m_bFirstToSecondDeleteInSecond)
            {
                IFileInfo fiSavedInfo2 = 
                    m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                if (fiSavedInfo2.Exists)
                    m_iFileSystem.Delete(fiSavedInfo2);
                m_iFileSystem.Delete(fi2);
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

                if (m_bTestFiles)
                {
                    IFileInfo fiSavedInfo2 = 
                        m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                            fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    bool bForceCreateInfo = false;
                    bool bOK = true;

                    if (m_bRepairFiles)
                        TestAndRepairSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfo, false);
                    else
                        bOK = TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, 
                            ref bForceCreateInfo, true, !m_bTestFilesSkipRecentlyTested, true);

                    if (bOK && m_bCreateInfo && 
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
            IFileInfo fi1, 
            IFileInfo fi2
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            // first to second, but first can be written to
            if (!m_bFirstToSecondSyncMode ? (!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) || (fi1.Length != fi2.Length)) :
                           ((!FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && fi1.LastWriteTimeUtc > fi2.LastWriteTimeUtc) || (FileTimesEqual(fi1.LastWriteTimeUtc, fi2.LastWriteTimeUtc) && (fi1.Length != fi2.Length)))
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            ProcessFilePair_Bidirectionally_BothExist_FirstNewer(strFilePath1, strFilePath2, fi1, fi2, 
                m_bFirstToSecondSyncMode?"(file was newer or bigger)":"(file has a different date or length)",
                m_bFirstToSecondSyncMode ? Resources.FileWasNewer : Resources.FileHasDifferentTime);
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            // both files are present and have same modification date
            IFileInfo fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            IFileInfo fiSavedInfo1 = m_iFileSystem.GetFileInfo(
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
                if (m_bTestFiles)
                {
                    bOK = TestSingleFile(strFilePath2, CreatePathOfChkFile(
                        fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), 
                        ref bForceCreateInfo, true, !m_bTestFilesSkipRecentlyTested, true);
                    if (!bOK && m_bRepairFiles)
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
                                if (fi1.LastWriteTimeUtc.Year > 1975)
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

                        if (!bOK && m_bRepairFiles)
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
                                    if (fi2.LastWriteTimeUtc.Year > 1975)
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
            IFileInfo fi1, 
            IFileInfo fi2)
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
            IFileInfo fi1, 
            IFileInfo fi2
            )
        {
            if (fi1.Length == 0 && CheckIfZeroLengthIsInteresting(strFilePath1))
            {
                WriteLogFormattedLocalized(0, Resources.FileHasZeroLength, strFilePath1);
                WriteLog(true, 0, "Warning: file has zero length, " + 
                    "indicating a failed copy operation in the past: ", strFilePath1);
            }

            IFileInfo fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            bool bForceCreateInfo = false;
            bool bForceCreateInfo2 = false;
            bool bInTheEndOK = true;

            try
            {
                m_oSemaphoreCopyFiles.WaitOne();

                if (m_bCancelClicked)
                    return;

                if (m_bCreateInfo && 
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

                    fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                        CreatePathOfChkFile(fi1.DirectoryName, 
                        "RestoreInfo", fi1.Name, ".chk"));
                    bForceCreateInfo = false;
                }
                else
                {
                    try
                    {
                        if (m_bTestFiles)
                            CopyRepairSingleFile(strFilePath2, fi1.FullName, 
                                fiSavedInfo1.FullName, ref bForceCreateInfo, ref bForceCreateInfo2, 
                                "(file was new)", Resources.FileWasNew, true, m_bRepairFiles);
                        else
                            CopyFileSafely(fi1, strFilePath2, "(file was new)", Resources.FileWasNew);
                    }
                    catch (Exception)
                    {
                        WriteLogFormattedLocalized(0, Resources.EncounteredErrorWhileCopyingTryingToRepair,
                            fi1.FullName);
                        WriteLog(true, 0, "Warning: Encountered error while copying ", 
                            fi1.FullName, ", trying to automatically repair");
                        if (m_bTestFiles && m_bRepairFiles)
                            TestAndRepairSingleFile(fi1.FullName, 
                                fiSavedInfo1.FullName, ref bForceCreateInfo, false);
                        if (bInTheEndOK)
                            bInTheEndOK = CopyRepairSingleFile(strFilePath2, 
                                fi1.FullName, fiSavedInfo1.FullName, 
                                ref bForceCreateInfo, ref bForceCreateInfo2, 
                                "(file was new)", Resources.FileWasNew, false, m_bTestFiles && m_bRepairFiles);
                    }
                }


                if (bInTheEndOK)
                {
                    if (m_bCreateInfo || fiSavedInfo1.Exists)
                    {
                        if (!fiSavedInfo1.Exists || bForceCreateInfo || 
                            fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc)
                        {
                            CreateSavedInfo(strFilePath1, 
                                CreatePathOfChkFile(fi1.DirectoryName, 
                                "RestoreInfo", fi1.Name, ".chk"));
                            fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                                CreatePathOfChkFile(fi1.DirectoryName, 
                                "RestoreInfo", fi1.Name, ".chk"));
                        }

                        if (bForceCreateInfo2)
                            CreateSavedInfo(strFilePath2, 
                                CreatePathOfChkFile(fi2.DirectoryName, 
                                "RestoreInfo", fi2.Name, ".chk"));
                        else
                            if (fiSavedInfo1.Exists)
                            {
                                try
                                {
                                    m_iFileSystem.CopyTo(
                                        fiSavedInfo1, CreatePathOfChkFile(
                                        fi2.DirectoryName, "RestoreInfo",
                                        fi2.Name, ".chk"), true);
                                }
                                catch (System.IO.IOException)
                                {
                                    CreateSavedInfo(strFilePath1,
                                        CreatePathOfChkFile(
                                        fi1.DirectoryName, "RestoreInfo",
                                        fi1.Name, ".chk"));
                                    CreateSavedInfo(strFilePath2,
                                        CreatePathOfChkFile(
                                        fi2.DirectoryName, "RestoreInfo",
                                        fi2.Name, ".chk"));
                                }
                            }
                    }
                }
            }
            finally
            {
                m_oSemaphoreCopyFiles.Release();
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
            IFileInfo fi1, 
            IFileInfo fi2
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
            IFileInfo fi1, 
            IFileInfo fi2
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
            IFileInfo fi1, 
            IFileInfo fi2,
            string strReasonEn,
            string strReasonTranslated)
        {
            IFileInfo fiSavedInfo1 = 
                m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
            IFileInfo fiSavedInfo2 = 
                m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));

            bool bForceCreateInfo1 = false;
            bool bForceCreateInfo2 = false;
            bool bCopied2To1 = false;
            bool bCopy2To1 = false;
            bool bCopied1To2 = true;

            try
            {
                m_oSemaphoreCopyFiles.WaitOne();

                if (m_bCancelClicked)
                    return;

                if (m_bCreateInfo && 
                    (!fiSavedInfo1.Exists || 
                        fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc || 
                        bForceCreateInfo1))
                {
                    bCopied1To2 = CreateSavedInfoAndCopy(
                        fi1.FullName, fiSavedInfo1.FullName, fi2.FullName, 
                        strReasonEn, strReasonTranslated);
                    fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                        CreatePathOfChkFile(fi1.DirectoryName, 
                        "RestoreInfo", fi1.Name, ".chk"));

                    if (bCopied1To2)
                        bForceCreateInfo1 = false;
                }
                else
                {
                    try
                    {
                        if (m_bTestFiles)
                            CopyRepairSingleFile(strFilePath2, fi1.FullName, fiSavedInfo1.FullName, 
                                ref bForceCreateInfo1, ref bForceCreateInfo2, 
                                "(file was new)", Resources.FileWasNew, true, m_bRepairFiles);
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
                    if (!m_bTestFiles || !m_bRepairFiles)
                    {
                        WriteLogFormattedLocalized(0, Resources.RunningWithoutRepairOptionUndecided,
                            fi1.FullName, fi2.FullName);
                        WriteLog(true, 0, "Running without repair option, "
                        + "so couldn't decide, if the file ",  
                        fi1.FullName, " can be restored using ", fi2.FullName);
                        // first failed,  still need to test the second
                        if (m_bTestFiles)
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
                            fiSavedInfo1 = m_iFileSystem.GetFileInfo(
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
                             && fi2.LastWriteTimeUtc.Year>1975)
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
                    if ((m_bCreateInfo && 
                        (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc)) || 
                        (fiSavedInfo1.Exists && bForceCreateInfo1))
                    {
                        CreateSavedInfo(strFilePath1, 
                            CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                        fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                            CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    }

                    if (fiSavedInfo1.Exists)
                    {
                        if (bForceCreateInfo2)
                        {
                            if (m_bCreateInfo || fiSavedInfo2.Exists)
                                CreateSavedInfo(strFilePath2, CreatePathOfChkFile(
                                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                        }
                        else
                            m_iFileSystem.CopyTo(fiSavedInfo1, CreatePathOfChkFile(
                                fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"), true);
                    }

                    return;
                }


                if (!bCopy2To1)
                    return;

                if (!m_bTestFiles || !m_bRepairFiles)
                {
                    WriteLogFormattedLocalized(0, Resources.RunningWithoutRepairOptionUndecided,
                        fi1.FullName, fi2.FullName);
                    WriteLog(true, 0, "Running without repair option, so couldn't decide, " +
                    "if the file ", fi1.FullName, " can be restored using ", fi2.FullName);
                    return;
                }

                // there we try to restore the older file 2, since it seems to be OK, while newer file 1 failed.
                fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                    CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                bForceCreateInfo2 = false;

                if (m_bCreateInfo && 
                    (!fiSavedInfo2.Exists || 
                        fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc || 
                        bForceCreateInfo2))
                {
                    bCopied2To1 = CreateSavedInfoAndCopy(
                        fi2.FullName, fiSavedInfo2.FullName, strFilePath1, 
                        "(file was healthy)", Resources.FileWasHealthy);
                    fiSavedInfo2 = m_iFileSystem.GetFileInfo(
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
                            "(file was healthy or repaired)", Resources.FileHealthyOrRepaired, true, true);
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


                if ((m_bCreateInfo && 
                    (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc!=fi2.LastWriteTimeUtc)) || 
                    (fiSavedInfo2.Exists && bForceCreateInfo2))
                {
                    CreateSavedInfo(strFilePath2, 
                        CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                    fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                        CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
                }

                if (fiSavedInfo2.Exists)
                {
                    if (bForceCreateInfo1)
                    {
                        if (m_bCreateInfo || fiSavedInfo1.Exists)
                            CreateSavedInfo(strFilePath1, 
                                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                    }
                    else
                        m_iFileSystem.CopyTo(fiSavedInfo2, 
                            CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"), true);
                }
            }
            finally
            {
                m_oSemaphoreCopyFiles.Release();
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
            IFileInfo fi1, 
            IFileInfo fi2
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
            IFileInfo fi1, 
            IFileInfo fi2)
        {
            // bidirectionally path
            // both files are present and have same modification date
            IFileInfo fiSavedInfo2 = 
                m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                    fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));;
            IFileInfo fiSavedInfo1 = 
                m_iFileSystem.GetFileInfo(CreatePathOfChkFile(
                    fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the restoreinfo files is missing or has wrong date, 
            // but the other is OK then copy the one to the other
            if (fiSavedInfo1.Exists && 
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc && 
                (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    m_oSemaphoreCopyFiles.WaitOne();
                    m_iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                }
                finally
                {
                    m_oSemaphoreCopyFiles.Release();
                }

                fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                    CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            }
            else
                if (fiSavedInfo2.Exists && 
                    fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc && 
                    (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
                {
                    try
                    {
                        m_oSemaphoreCopyFiles.WaitOne();
                        m_iFileSystem.CopyTo(fiSavedInfo2, fiSavedInfo1.FullName, true);
                    }
                    finally
                    {
                        m_oSemaphoreCopyFiles.Release();
                    }

                    fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                        CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));
                }


            bool bCreateInfo = false;
            bool bCreateInfo1 = false;
            bool bCreateInfo2 = false;
            if (m_bTestFiles)
            {
                bool bFirstOrSecond;
                lock (this)
                {
                    bFirstOrSecond = m_bRandomOrder = !m_bRandomOrder;
                }

                if (m_bRepairFiles)
                {
                    bool bTotalResultOk = true;
                    if (bFirstOrSecond)
                    {
                        bTotalResultOk = TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !m_bTestFilesSkipRecentlyTested, true, true, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            bTotalResultOk = bTotalResultOk && TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !m_bTestFilesSkipRecentlyTested, true, true, false);
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        bTotalResultOk = TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !m_bTestFilesSkipRecentlyTested, true, true, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            bTotalResultOk = bTotalResultOk && TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !m_bTestFilesSkipRecentlyTested, true, true, false);
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
                        TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !m_bTestFilesSkipRecentlyTested, true, false, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !m_bTestFilesSkipRecentlyTested, true, false, false);
                        else
                            bCreateInfo2 = false;
                    }
                    else
                    {
                        TestSingleFile2(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2, true, !m_bTestFilesSkipRecentlyTested, true, false, false);
                        if (!string.Equals(fi1.FullName, fi2.FullName, StringComparison.CurrentCultureIgnoreCase))
                            TestSingleFile2(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1, true, !m_bTestFilesSkipRecentlyTested, true, false, false);
                        else
                            bCreateInfo1 = false;
                    }

                    //TestSingleFile(fi1.FullName, fiSavedInfo1.FullName, ref bCreateInfo1);
                    //TestSingleFile(fi2.FullName, fiSavedInfo2.FullName, ref bCreateInfo2);
                }
            }

            if (m_bCreateInfo && (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc || bCreateInfo1))
            {
                CreateSavedInfo(fi1.FullName, fiSavedInfo1.FullName);
                if (fiSavedInfo1.FullName.Equals(fiSavedInfo2.FullName, StringComparison.InvariantCultureIgnoreCase))
                    fiSavedInfo2 = m_iFileSystem.GetFileInfo(CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            }

            if (m_bCreateInfo && (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc || bCreateInfo2))
            {
                CreateSavedInfo(fi2.FullName, fiSavedInfo2.FullName);
            }


            fiSavedInfo2 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi2.DirectoryName, "RestoreInfo", fi2.Name, ".chk"));
            fiSavedInfo1 = m_iFileSystem.GetFileInfo(
                CreatePathOfChkFile(fi1.DirectoryName, "RestoreInfo", fi1.Name, ".chk"));

            // if one of the files is missing or has wrong date, but the 
            // other is OK then copy the one to the other
            if (fiSavedInfo1.Exists && 
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc && 
                (!fiSavedInfo2.Exists || fiSavedInfo2.LastWriteTimeUtc != fi2.LastWriteTimeUtc))
            {
                try
                {
                    m_oSemaphoreCopyFiles.WaitOne();
                    m_iFileSystem.CopyTo(fiSavedInfo1, fiSavedInfo2.FullName, true);
                }
                finally
                {
                    m_oSemaphoreCopyFiles.Release();
                }
            }
            else
                if (fiSavedInfo2.Exists && 
                    fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc && 
                    (!fiSavedInfo1.Exists || fiSavedInfo1.LastWriteTimeUtc != fi1.LastWriteTimeUtc))
                {
                    try
                    {
                        m_oSemaphoreCopyFiles.WaitOne();
                        m_iFileSystem.CopyTo(fiSavedInfo2, fiSavedInfo1.FullName, true);
                    }
                    finally
                    {
                        m_oSemaphoreCopyFiles.Release();
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


            IFileInfo finfo = m_iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, finfo.LastWriteTimeUtc, false);
            try
            {
                using (IFile s =
                    m_iFileSystem.CreateBufferedStream(m_iFileSystem.OpenRead(finfo.FullName), 
                        (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    try
                    {
                        using (IFile s2 = 
                            m_iFileSystem.CreateBufferedStream(m_iFileSystem.Create(pathFileCopy), 
                            (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                        {
                            Block b = new Block();

                            for (int index = 0; ; index++)
                            {
                                int readCount = 0;
                                if ((readCount = b.ReadFrom(s)) == b.Length)
                                {
                                    b.WriteTo(s2);
                                    si.AnalyzeForInfoCollection(b, index);
                                }
                                else
                                {
                                    if (readCount > 0)
                                    {
                                        for (int i = b.Length - 1; i >= readCount; --i)
                                            b[i] = 0;
                                        b.WriteTo(s2, readCount);
                                        si.AnalyzeForInfoCollection(b, index);
                                    }
                                    break;
                                }
                            };


                            s2.Close();
                            IFileInfo fi2tmp = m_iFileSystem.GetFileInfo(pathFileCopy);
                            fi2tmp.LastWriteTimeUtc = finfo.LastWriteTimeUtc;

                            IFileInfo fi2 = m_iFileSystem.GetFileInfo(strTargetPath);
                            if (fi2.Exists)
                                m_iFileSystem.Delete(fi2);

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
                            IFileInfo finfoCopy = m_iFileSystem.GetFileInfo(pathFileCopy);
                            if (finfoCopy.Exists)
                                m_iFileSystem.Delete(finfoCopy);
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
                IDirectoryInfo di = m_iFileSystem.GetDirectoryInfo(
                    strPathSavedInfoFile.Substring(0, 
                    strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                if (!di.Exists)
                {
                    di.Create();
                    di = m_iFileSystem.GetDirectoryInfo(
                        strPathSavedInfoFile.Substring(0, 
                        strPathSavedInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden 
                        | System.IO.FileAttributes.System;
                }
                using (IFile s = m_iFileSystem.Create(strPathSavedInfoFile))
                {
                    si.SaveTo(s);
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                IFileInfo fiSavedInfo = m_iFileSystem.GetFileInfo(strPathSavedInfoFile);
                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                fiSavedInfo.Attributes = fiSavedInfo.Attributes | System.IO.FileAttributes.Hidden |
                    System.IO.FileAttributes.System;

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
            return CreateSavedInfo(strPathFile, strPathSavedChkInfoFile, 1, false);
        }


        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <param name="bForceSecondBlocks">Indicates that a second row of blocks must be created</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            bool bForceSecondBlocks
            )
        {
            return CreateSavedInfo(strPathFile, strPathSavedChkInfoFile, 1, bForceSecondBlocks);
        }

        //===================================================================================================
        /// <summary>
        /// This method reads a file and creates saved info for it.
        /// </summary>
        /// <param name="strPathFile">Path of the original file</param>
        /// <param name="strPathSavedChkInfoFile">The target path for .CHK file</param>
        /// <param name="nVersion">The version to save supported values: 0, 1</param>
        /// <param name="bForceSecondBlocks">Indicates that a second row of blocks must be created</param>
        /// <returns>true iff the operation succeeded</returns>
        //===================================================================================================
        bool CreateSavedInfo(
            string strPathFile,
            string strPathSavedChkInfoFile,
            int nVersion,
            bool bForceSecondBlocks
            )
        {
            IFileInfo finfo = m_iFileSystem.GetFileInfo(strPathFile);
            SavedInfo si = new SavedInfo(finfo.Length, finfo.LastWriteTimeUtc, bForceSecondBlocks);
            try
            {
                using (IFile s =
                    m_iFileSystem.CreateBufferedStream(m_iFileSystem.OpenRead(finfo.FullName), 
                        (int)Math.Min(finfo.Length + 1, 64 * 1024 * 1024)))
                {
                    Block b = new Block();

                    for (int index = 0; ; index++)
                    {
                        int nReadCount = 0;
                        if ((nReadCount=b.ReadFrom(s)) == b.Length)
                        {
                            si.AnalyzeForInfoCollection(b, index);
                        }
                        else
                        {
                            if (nReadCount > 0)
                            {
                                // fill remaining part with zeros
                                for (int i = b.Length - 1; i >= nReadCount; --i)
                                    b[i] = 0;

                                si.AnalyzeForInfoCollection(b, index);
                            }
                            break;
                        }
                        if (m_bCancelClicked)
                            throw new OperationCanceledException();
 
                    };


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
                IDirectoryInfo di = m_iFileSystem.GetDirectoryInfo(
                    strPathSavedChkInfoFile.Substring(0, 
                    strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                if (!di.Exists)
                {
                    di.Create();
                    di = m_iFileSystem.GetDirectoryInfo(
                        strPathSavedChkInfoFile.Substring(0, 
                        strPathSavedChkInfoFile.LastIndexOfAny(new char[] { '\\', '/' })));
                    di.Attributes = di.Attributes | System.IO.FileAttributes.Hidden |
                        System.IO.FileAttributes.System;
                }

                IFileInfo fiSavedInfo = m_iFileSystem.GetFileInfo(strPathSavedChkInfoFile);
                if (fiSavedInfo.Exists)
                {
                    m_iFileSystem.Delete(fiSavedInfo);
                }

                using (IFile s = m_iFileSystem.CreateBufferedStream(m_iFileSystem.Create(strPathSavedChkInfoFile), 
                    1024*1024))
                {
                    if (nVersion == 0)
                    {
                        si.SaveTo_v0(s);
                    }
                    else
                    {
                        si.SaveTo(s);
                    }
                    s.Close();
                }

                // save last write time also at the time of the .chk file
                fiSavedInfo = m_iFileSystem.GetFileInfo(strPathSavedChkInfoFile);
                fiSavedInfo.LastWriteTimeUtc = finfo.LastWriteTimeUtc;
                fiSavedInfo.Attributes = fiSavedInfo.Attributes | System.IO.FileAttributes.Hidden
                    | System.IO.FileAttributes.System;

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
            IFileInfo finfo = 
                m_iFileSystem.GetFileInfo(pathFile);
            IFileInfo fiSavedInfo = 
                m_iFileSystem.GetFileInfo(strPathSavedInfoFile);
            bool bSkipBufferedFile = false;

            try
            {
                if (!bForcePhysicalTest)
                {
                    IFileInfo fichecked = 
                        m_iFileSystem.GetFileInfo(strPathSavedInfoFile + "ed");
                    // this randomly skips testing of files,
                    // so the user doesn't have to wait long, when performing checks annually:
                    // 100% of files are skipped within first 2 years after last check
                    // 0% after 7 years after last check
                    if (fichecked.Exists && finfo.Exists &&
                        (!fiSavedInfo.Exists || fiSavedInfo.LastWriteTimeUtc == finfo.LastWriteTimeUtc) &&
                        fichecked.LastWriteTimeUtc.CompareTo(finfo.LastWriteTimeUtc) > 0 &&
                        Math.Abs(DateTime.UtcNow.Year * 366 + DateTime.UtcNow.DayOfYear 
                            - fichecked.LastWriteTimeUtc.Year * 366 - fichecked.LastWriteTimeUtc.DayOfYear) 
                        < 366 * 2.2 + 366 * 4.6 * m_oRandomForRecentlyChecked.NextDouble())
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
                    using (IFile s =
                        m_iFileSystem.CreateBufferedStream(
                            m_iFileSystem.OpenRead(strPathSavedInfoFile), 
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
                        using (IFile s =
                            m_iFileSystem.OpenRead(strPathSavedInfoFile))
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

                Block b = new Block();
                try
                {
                    using (IFile s =
                        m_iFileSystem.CreateBufferedStream(
                            m_iFileSystem.OpenRead(finfo.FullName), 
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

                    using (IFile s =
                        m_iFileSystem.OpenRead(finfo.FullName))
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

                IFile s = 
                    m_iFileSystem.OpenRead(finfo.FullName);
                if (!bSkipBufferedFile)
                    s = m_iFileSystem.CreateBufferedStream(s, 
                        (int)Math.Min(finfo.Length + 1, 8 * 1024 * 1024));

                using (s)
                {
                    si.StartRestore();
                    Block b = new Block();
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

                            if (m_bCancelClicked)
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
                                if (!m_bCancelClicked)
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
            // no need in ".chked" files, if we are creating a release
            if (Program.CreateRelease)
                return;

            string strPath = strPathSavedInfoFile + "ed";

            try
            {
                if (m_iFileSystem.Exists(strPath))
                {
                    m_iFileSystem.SetLastWriteTimeUtc(strPath, DateTime.UtcNow);
                }
                else
                {
                    // there we use the simple File.OpenWrite since we need only the date of the file
                    using (IFile s = m_iFileSystem.OpenWrite(strPath))
                    {
                        s.Close();
                    };
                }

                m_iFileSystem.SetAttributes(
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
            IFileInfo finfo = m_iFileSystem.GetFileInfo(strPathFile);
            IFileInfo fiSavedInfo = m_iFileSystem.GetFileInfo(strPathSavedInfoFile);

            SavedInfo si = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (IFile s =
                        m_iFileSystem.CreateBufferedStream(
                            m_iFileSystem.OpenRead(
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
                        using (IFile s =
                            m_iFileSystem.OpenRead(strPathSavedInfoFile))
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

                using (IFile s = m_iFileSystem.Open(
                    finfo.FullName, System.IO.FileMode.Open,
                    bOnlyIfCompletelyRecoverable ? System.IO.FileAccess.Read : System.IO.FileAccess.ReadWrite,
                    System.IO.FileShare.Read))
                {
                    Block b = new Block();
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
                using (IFile s =
                    m_iFileSystem.OpenRead(finfo.FullName))
                {
                    si.StartRestore();
                    Block b = new Block();
                    for (long index = 0; ; index++)
                    {

                        try
                        {
                            bool bBlockOk = true;
                            int nReadCount = 0;
                            if ((nReadCount = b.ReadFrom(s)) == b.Length)
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
                                if (nReadCount > 0)
                                {
                                    for (int i = b.Length - 1; i >= nReadCount; --i)
                                        b[i] = 0;

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
                                }
                                break;
                            }

                            if (m_bCancelClicked)
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
                    using (IFile s =
                        m_iFileSystem.OpenWrite(finfo.FullName))
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
                        int countErrors = (int)(nonRestoredSize / (new Block().Length));
                        finfo.LastWriteTimeUtc = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 23 -
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
            IFileInfo fi1 = m_iFileSystem.GetFileInfo(strPathFile1);
            IFileInfo fi2 = m_iFileSystem.GetFileInfo(strPathFile2);
            IFileInfo fiSavedInfo1 = m_iFileSystem.GetFileInfo(strPathSavedInfo1);
            IFileInfo fiSavedInfo2 = m_iFileSystem.GetFileInfo(strPathSavedInfo2);

            SavedInfo si1 = new SavedInfo();
            SavedInfo si2 = new SavedInfo();

            bool bSaveInfo1Present = false;
            if (fiSavedInfo1.Exists && 
                fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc)
            {
                using (IFile s =
                    m_iFileSystem.OpenRead(fiSavedInfo1.FullName))
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
                using (IFile s =
                    m_iFileSystem.OpenRead(fiSavedInfo2.FullName))
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
                using (IFile s1 = 
                    m_iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 = 
                        m_iFileSystem.OpenRead(strPathFile2))
                    {
                        si1.StartRestore();
                        si2.StartRestore();

                        Block b1 = new Block();
                        Block b2 = new Block();

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

                            if (m_bCancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();

                    }
                    s1.Close();


                }

                long notRestoredSize1 = 0;
                restore1.AddRange(si1.EndRestore(out notRestoredSize1, fiSavedInfo1.FullName, this));
                long notRestoredSize2 = 0;
                restore2.AddRange(si2.EndRestore(out notRestoredSize2, fiSavedInfo2.FullName, this));

                // now we've got the list of improvements for both files
                using (IFile s1 = m_iFileSystem.Open(
                    strPathFile1, System.IO.FileMode.Open, 
                    System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                {

                    using (IFile s2 = m_iFileSystem.Open(
                        strPathFile2, System.IO.FileMode.Open, 
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {
                        // let's apply improvements of one file 
                        // to the list of the other, whenever possible
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
                                (m_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri1.Position / ri1.Data.Length)))
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
                                (m_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
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

                                Block temp = new Block();
                                int length = temp.ReadFrom(s2);
                                temp.WriteTo(s1, length);
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

                                Block temp = new Block();
                                int length = temp.ReadFrom(s1);
                                temp.WriteTo(s2, length);
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
                using (IFile s1 = 
                    m_iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 = 
                        m_iFileSystem.OpenRead(strPathFile2))
                    {
                        for (int index = 0; ; ++index)
                        {
                            Block b1 = new Block();
                            Block b2 = new Block();

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
                                Block b = new Block();
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
                            }

                            if (!s1Continue && !s2Continue)
                                break;

                            if (m_bCancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }
                    s1.Close();
                }


                // now we've got the list of improvements for both files
                using (IFile s1 = m_iFileSystem.Open(
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


                using (IFile s2 = m_iFileSystem.Open(
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
                if (m_bTestFiles && m_bRepairFiles)
                    return TestAndRepairSingleFile(strPathFile, strPathSavedInfoFile, 
                        ref bForceCreateInfo, false);
                else
                {
                    if (m_bTestFiles)
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


            IFileInfo finfo = m_iFileSystem.GetFileInfo(strPathFile);
            IFileInfo fiSavedInfo = m_iFileSystem.GetFileInfo(strPathSavedInfoFile);

            DateTime dtmOriginalTime = finfo.LastWriteTimeUtc;

            SavedInfo si = new SavedInfo();
            bool bNotReadableSi = !fiSavedInfo.Exists;

            if (!bNotReadableSi)
            {
                try
                {
                    using (IFile s =
                        m_iFileSystem.OpenRead(strPathSavedInfoFile))
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
                !(m_bIgnoreTimeDifferencesBetweenDataAndSaveInfo || FileTimesEqual(si.TimeStamp, finfo.LastWriteTimeUtc)))
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

                using (IFile s = m_iFileSystem.Open(
                    finfo.FullName, System.IO.FileMode.Open, 
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    try
                    {
                        int countErrors = 0;
                        using (IFile s2 = m_iFileSystem.Open(
                            strPathTargetFile + ".tmp", System.IO.FileMode.Create, 
                            System.IO.FileAccess.Write, System.IO.FileShare.None))
                        {
                            for (long index = 0; ; index++)
                            {
                                Block b = new Block();
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
                            };

                            s2.Close();
                        }

                        // after the file has been copied to a ".tmp" delete old one
                        IFileInfo fi2 = m_iFileSystem.GetFileInfo(strPathTargetFile);

                        if (fi2.Exists)
                            m_iFileSystem.Delete(fi2);

                        // and replace it with the new one
                        IFileInfo fi2tmp = m_iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");
                        if (bAllBlocksOk)
                            // if everything OK set original time
                            fi2tmp.LastWriteTimeUtc = dtmOriginalTime;
                        else
                        {
                            // set the time to very old, so any existing newer or with less errors appears to be better.
                            fi2tmp.LastWriteTimeUtc = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 
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
                using (IFile s = 
                    m_iFileSystem.OpenRead(finfo.FullName))
                {
                    si.StartRestore();
                    Block b = new Block();
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

                        if (m_bCancelClicked)
                            throw new OperationCanceledException();

                    };

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
                    using (IFile s2 = 
                        m_iFileSystem.Open(
                            finfo.FullName, System.IO.FileMode.Open, 
                            bApplyRepairsToSrc && (rinfos.Count > 0) ? 
                                System.IO.FileAccess.ReadWrite : System.IO.FileAccess.Read, 
                            System.IO.FileShare.Read))
                    {
                        using (IFile s = 
                            m_iFileSystem.Open(
                            strPathTargetFile + ".tmp", System.IO.FileMode.Create, 
                            System.IO.FileAccess.Write, System.IO.FileShare.None))
                        {
                            long blockSize = new Block().Length;
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
                                    Block b = new Block();
                                    int lengthToWrite = b.ReadFrom(s2);
                                    b.WriteTo(s, lengthToWrite);
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
                IFileInfo finfoTmp = m_iFileSystem.GetFileInfo(strPathTargetFile + ".tmp");
                if (m_iFileSystem.Exists(strPathTargetFile))
                    m_iFileSystem.Delete(strPathTargetFile);
                finfoTmp.MoveTo(strPathTargetFile);

                if (si.NeedsRebuild())
                {
                    if ((!m_bFirstToSecond || !m_bFirstReadOnly) && rinfos.Count==0)
                        bForceCreateInfo = true;

                    bForceCreateInfoTarget = true;
                }
 

                IFileInfo finfo2 = m_iFileSystem.GetFileInfo(strPathTargetFile);
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
                    int countErrors = (int)(nonRestoredSize / (new Block().Length));
                    finfo2.LastWriteTimeUtc = new DateTime(1975, 9, 24 - countErrors / 60 / 24, 
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
            if (m_bTestFilesSkipRecentlyTested)
                if (TestSingleFile(strPathFile2, strPathSavedInfo2, ref bForceCreateInfo, false, false, true))
                    return;

            IFileInfo fi1 = m_iFileSystem.GetFileInfo(strPathFile1);
            IFileInfo fi2 = m_iFileSystem.GetFileInfo(strPathFile2);
            IFileInfo fiSavedInfo1 = m_iFileSystem.GetFileInfo(strPathSavedInfo1);
            IFileInfo fiSavedInfo2 = m_iFileSystem.GetFileInfo(strPathSavedInfo2);

            SavedInfo si1 = new SavedInfo();
            SavedInfo si2 = new SavedInfo();

            bool bSaveInfo1Present = false;
            if (fiSavedInfo1.Exists && 
                (m_bIgnoreTimeDifferencesBetweenDataAndSaveInfo || fiSavedInfo1.LastWriteTimeUtc == fi1.LastWriteTimeUtc) )
            {
                using (IFile s = m_iFileSystem.OpenRead(fiSavedInfo1.FullName))
                {
                    si1.ReadFrom(s);
                    bSaveInfo1Present = si1.Length == fi1.Length && 
                        (m_bIgnoreTimeDifferencesBetweenDataAndSaveInfo || FileTimesEqual(si1.TimeStamp, fi1.LastWriteTimeUtc) );
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
                (m_bIgnoreTimeDifferencesBetweenDataAndSaveInfo || fiSavedInfo2.LastWriteTimeUtc == fi2.LastWriteTimeUtc) )
            {
                using (IFile s = m_iFileSystem.OpenRead(fiSavedInfo2.FullName))
                {
                    SavedInfo si3 = new SavedInfo();
                    si3.ReadFrom(s);
                    if (si3.Length == fi2.Length && 
                        (m_bIgnoreTimeDifferencesBetweenDataAndSaveInfo || FileTimesEqual(si3.TimeStamp, fi2.LastWriteTimeUtc) ))
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
                using (IFile s1 = 
                    m_iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 = 
                        m_iFileSystem.OpenRead(strPathFile2))
                    {
                        si1.StartRestore();
                        si2.StartRestore();

                        for (int index = 0; ; ++index)
                        {
                            Block b1 = new Block();
                            Block b2 = new Block();

                            bool b1Present = false;
                            bool b1Ok = false;
                            bool s1Continue = false;
                            try
                            {
                                int nReadCount = 0;
                                if ((nReadCount = b1.ReadFrom(s1)) == b1.Length)
                                {
                                    b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                    s1Continue = true;
                                }
                                else
                                {
                                    if (nReadCount > 0)
                                    {
                                        for (int i = b1.Length - 1; i >= nReadCount; --i)
                                            b1[i] = 0;
                                        b1Ok = si1.AnalyzeForTestOrRestore(b1, index);
                                    }
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
                                int nReadCount = 0;
                                if ((nReadCount = b2.ReadFrom(s2)) == b2.Length)
                                {
                                    b2Ok = si2.AnalyzeForTestOrRestore(b2, index);
                                    s2Continue = true;
                                }
                                else
                                {
                                    for (int i = b2.Length - 1; i >= nReadCount; --i)
                                        b2[i] = 0;
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

                            if (m_bCancelClicked)
                                throw new OperationCanceledException();

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
                using (IFile s1 = m_iFileSystem.Open(
                    strPathFile1, System.IO.FileMode.Open, 
                    System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {

                    using (IFile s2 = m_iFileSystem.Open(
                        strPathFile2, System.IO.FileMode.Open, 
                        System.IO.FileAccess.ReadWrite, System.IO.FileShare.Read))
                    {
                        // let's apply improvements of one file to the list 
                        // of the other, whenever possible (we are in first folder readonly case)
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
                                (m_bPreferPhysicalCopies && equalBlocks.ContainsKey(ri2.Position / ri2.Data.Length)))
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

                                Block temp = new Block();
                                int length = temp.ReadFrom(s1);
                                temp.WriteTo(s2, length);
                                readableBlocks2[ri2.Position / ri2.Data.Length] = true;
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
                using (IFile s1 = 
                    m_iFileSystem.OpenRead(strPathFile1))
                {
                    using (IFile s2 = 
                        m_iFileSystem.OpenRead(strPathFile2))
                    {
                        for (int index = 0; ; ++index)
                        {
                            Block b1 = new Block();
                            Block b2 = new Block();

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

                            if (m_bCancelClicked)
                                throw new OperationCanceledException();

                        }

                        s2.Close();
                    }
                    s1.Close();
                }


                using (IFile s2 = m_iFileSystem.Open(
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


        System.Text.StringBuilder m_strLogToShow;
        System.IO.TextWriter m_oLogFileLocalized;
        System.IO.TextWriter m_oLogFile;


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
            if (m_oLogFile != null)
            {
                if (bOnlyToNonlocalizedLog &&
                    (Resources.DefaultCulture.Equals("yes")))
                {
                    // don't write same twice to the log file
                    return;
                }

                System.DateTime utc = System.DateTime.UtcNow;
                System.DateTime now = utc.ToLocalTime();
                lock (m_strLogToShow)
                {
                    m_oLogFile.Write("{0}UT\t={1}=\t", utc.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        System.Threading.Thread.CurrentThread.ManagedThreadId);
                    if (!bOnlyToNonlocalizedLog)
                    {
                        // switch back to LTR for this message in the localized log
                        if (Resources.RightToLeft.Equals("yes"))
                            m_oLogFileLocalized.Write((char)0x200E);

                        m_oLogFileLocalized.Write("{0}\t={1}=\t", now.ToString("F"),
                            FormatNumber(System.Threading.Thread.CurrentThread.ManagedThreadId));
                    }

                    while (nIndent-- > 0)
                    {
                        m_oLogFile.Write("\t");
                        if (!bOnlyToNonlocalizedLog)
                             m_oLogFileLocalized.Write("\t");
                        if (!bOnlyToNonlocalizedLog) 
                            m_strLogToShow.Append("        ");
                    }

                    foreach (object part in aParts)
                    {
                        string s = part.ToString().Replace(Environment.NewLine,"");
                        if (!bOnlyToNonlocalizedLog)
                        {
                            m_strLogToShow.Append(s);
                            m_oLogFileLocalized.Write(s);
                        }
                        m_oLogFile.Write(s);
                    }


                    if (!bOnlyToNonlocalizedLog)
                    {
                        m_strLogToShow.Append(Environment.NewLine);

                        // continue with rtl
                        if (Resources.RightToLeft.Equals("yes"))
                            m_oLogFileLocalized.Write((char)0x200F);

                        m_oLogFileLocalized.WriteLine();
                        m_oLogFileLocalized.Flush();
                    }
                    m_oLogFile.WriteLine();
                    m_oLogFile.Flush();
                }
            }
            else
            {
                if (!bOnlyToNonlocalizedLog)
                {
                    lock (m_strLogToShow)
                    {
                        while (nIndent-- > 0)
                        {
                            m_strLogToShow.Append("        ");
                        }

                        foreach (object part in aParts)
                        {
                            m_strLogToShow.Append(part.ToString());
                        }

                        m_strLogToShow.Append("\r\n");
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
            if (m_oLogFileLocalized != null)
            {
                System.DateTime utc = System.DateTime.UtcNow;
                System.DateTime now = utc.ToLocalTime();
                lock (m_strLogToShow)
                {
                    DateTime dtmNow = now;
                    string strNowF = dtmNow.ToString("F");
                    string[] astrNowF = strNowF.Split(' ');
                    string strDateTimeFormatted = strNowF;
                    try
                    {
                        strDateTimeFormatted = string.Format(Resources.DateFormat, strNowF,
                        FormatNumber(dtmNow.Year), FormatNumber(dtmNow.Month), FormatNumber(dtmNow.Day),
                        astrNowF.Length >= 2 ? astrNowF[1] : "", 
                        FormatNumber(dtmNow.Hour), FormatNumber(dtmNow.Minute), FormatNumber(dtmNow.Second),
                        astrNowF[0], astrNowF.Length >= 3 ? astrNowF[2] : "", 
                        astrNowF.Length >= 4 ? astrNowF[3] : "",
                        astrNowF.Length >= 5 ? astrNowF[4] : "");
                        m_oLogFileLocalized.Write("{0}\t={1}=\t", strDateTimeFormatted,
                            FormatNumber(System.Threading.Thread.CurrentThread.ManagedThreadId));
                    } catch (Exception oEx)
                    {
                        // if there are any errors - write to nonlocalized log
                        WriteLog(true, 0, "Error while formatting time for localized log: " + 
                            oEx.Message + " F:" + strNowF + " DateFormat:" + Resources.DateFormat);
                    }

                    while (nIndent-- > 0)
                    {
                        m_oLogFileLocalized.Write("\t");
                        m_strLogToShow.Append("        ");
                    }


                    for (int i = aParams.Length - 1; i >= 0; --i)
                    {
                        if (aParams[i] is Int32 || aParams[i] is Int64 || 
                            aParams[i] is UInt32 || aParams[i] is UInt64)
                        {
                            aParams[i] = FormatNumber(aParams[i]);
                        }
                    }

                    string s = string.Format(strFormat, aParams);
                    m_strLogToShow.Append(s);
                    m_oLogFileLocalized.Write(s);

                    m_strLogToShow.Append(Environment.NewLine);
                    m_oLogFileLocalized.Write(Environment.NewLine);
                    m_oLogFileLocalized.Flush();
                }
            }
            else
            {
                lock (m_strLogToShow)
                {
                    while (nIndent-- > 0)
                    {
                        m_strLogToShow.Append("        ");
                    }

                    string s = string.Format(strFormat, aParams);
                    m_strLogToShow.Append(s);

                    m_strLogToShow.Append("\r\n");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oNumber"></param>
        /// <returns></returns>
        public static object FormatNumber(object oNumber)
        {
            if (Resources.Digits.Equals("CHS"))
                return FormatNumberChineseJapaneseKorean(oNumber, "一十二亿三千四百五十六万七千八百九十一", "〇");
            else
                if (Resources.Digits.Equals("CHT"))
                    return FormatNumberChineseJapaneseKorean(oNumber, "一十二億三千四百五十六萬七千八百九十一", "零");
                else
                    if (Resources.Digits.Equals("Korea"))
                        return FormatNumberChineseJapaneseKorean(oNumber, "일십이억삼천사백오십육만칠천팔백구십일", "영");
                    else
                        if (Resources.Digits.Equals("Japan"))
                            return FormatNumberChineseJapaneseKorean(oNumber, "一十二億三千四百五十六万七千八百九十一", "零");
                        else
                            if (Resources.Digits.Equals("Tibet"))
                                return FormatNumberTibet(oNumber);
                            else
                                if (Resources.Digits.Equals("Latin"))
                                    return FormatNumberLatin(oNumber);
                                else
                                    return oNumber;
        }

        //===================================================================================================
        /// <summary>
        /// Formats number in tibet digits
        /// </summary>
        /// <param name="nNumber">The number to format</param>
        /// <returns>Formatted number</returns>
        //===================================================================================================
        public static string FormatNumberTibet(object oNumber)
        {
            const string c_strTibetDigits = "༠༡༢༣༤༥༦༧༨༩";
            string strNumber = oNumber.ToString();
            StringBuilder oResult = new StringBuilder(strNumber.Length);
            foreach (char c in strNumber)
            {
                if (c >= '0' && c <= '9')
                    oResult.Append(c_strTibetDigits[c - '0']);
                else
                    return strNumber;
            }

            return oResult.ToString();
        }


        //===================================================================================================
        /// <summary>
        /// Formats number in latin digits
        /// </summary>
        /// <param name="nNumber">The number to format</param>
        /// <returns>Formatted number</returns>
        //===================================================================================================
        public static string FormatNumberLatin(object oNumber)
        {
            string[] c_strLatinHundredThousands = new string[] { "I̅", "V̅", "X̅", "L̅", "C̅", "D̅", "M̅", "D̿" };
            string[] c_strLatinDigits = new string[] { "I", "V", "X", "L", "C", "D", "M", "V̅", "X̅", "L̅", "C̅", "D̅", "M̅" };

            string strNumber = oNumber.ToString(); 

            if (strNumber.Equals("0"))
                return "nulla";

            StringBuilder oResult = new StringBuilder(strNumber.Length);
            if (strNumber.Length>7 || (strNumber.Length==7 && strNumber[0]>'3'))
            {
                strNumber = strNumber.Substring(0, strNumber.Length - 5);
                // first put hundred thousands. There visual studio doesn't display next line...
                oResult.Append("⎹");

                try
                {
                    for (int i = 0, j = strNumber.Length * 2 - 2; i < strNumber.Length; ++i, j -= 2)
                    {
                        char c = strNumber[i];
                        if (c >= '0' && c <= '9')
                        {
                            switch (c)
                            {
                                case '9':
                                    oResult.Append(c_strLatinHundredThousands[j]);
                                    oResult.Append(c_strLatinHundredThousands[j + 2]);
                                    break;
                                case '8':
                                    oResult.Append(c_strLatinHundredThousands[j + 1]);
                                    goto case '3';
                                case '7':
                                    oResult.Append(c_strLatinHundredThousands[j + 1]);
                                    goto case '2';
                                case '6':
                                    oResult.Append(c_strLatinHundredThousands[j + 1]);
                                    goto case '1';
                                case '4':
                                    oResult.Append(c_strLatinHundredThousands[j]);
                                    goto case '5';
                                case '5':
                                    oResult.Append(c_strLatinHundredThousands[j + 1]);
                                    break;
                                case '3':
                                    oResult.Append(c_strLatinHundredThousands[j]);
                                    goto case '2';
                                case '2':
                                    oResult.Append(c_strLatinHundredThousands[j]);
                                    goto case '1';
                                case '1':
                                    oResult.Append(c_strLatinHundredThousands[j]);
                                    break;
                            }
                        }
                        else
                            return strNumber;
                    }
                }
                catch
                {
                    return strNumber;
                }


                // There visual studio doesn't display next line...
                oResult.Append("⎸");
                // get the number again, but now the rest of it.
                strNumber = oNumber.ToString();
                strNumber = strNumber.Substring(strNumber.Length-5);
            }


           
            try
            {
                for (int i = 0, j = strNumber.Length * 2 - 2; i < strNumber.Length; ++i, j -= 2)
                {
                    char c = strNumber[i];
                    if (c >= '0' && c <= '9')
                    {
                        switch (c)
                        {
                            case '9':
                                oResult.Append(c_strLatinDigits[j]);
                                oResult.Append(c_strLatinDigits[j + 2]);
                                break;
                            case '8':
                                oResult.Append(c_strLatinDigits[j + 1]);
                                goto case '3';
                            case '7':
                                oResult.Append(c_strLatinDigits[j + 1]);
                                goto case '2';
                            case '6':
                                oResult.Append(c_strLatinDigits[j + 1]);
                                goto case '1';
                            case '4':
                                oResult.Append(c_strLatinDigits[j]);
                                goto case '5';
                            case '5':
                                oResult.Append(c_strLatinDigits[j + 1]);
                                break;
                            case '3':
                                oResult.Append(c_strLatinDigits[j]);
                                goto case '2';
                            case '2':
                                oResult.Append(c_strLatinDigits[j]);
                                goto case '1';
                            case '1':
                                oResult.Append(c_strLatinDigits[j]);
                                break;
                        }
                    }
                    else
                        return strNumber;
                }
                return oResult.ToString();
            }
            catch
            {
                return strNumber;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Formats number in chinese, japanese or korean digits
        /// </summary>
        /// <param name="oNumber">The number to format</param>
        /// <param name="strDigits">Digits</param>
        /// <param name="strZero">String for zero</param>
        /// <returns>Formatted number</returns>
        //===================================================================================================
        static string FormatNumberChineseJapaneseKorean(object oNumber, string strDigits, string strZero)
        {
            string strNumber = oNumber.ToString();

            if (strNumber.Equals("0"))
                return strZero;

            StringBuilder oResult = new StringBuilder(strNumber.Length);
            try
            {
                bool bSomeDigitsPresent = false;
                for (int i = 0, j = strNumber.Length; i < strNumber.Length; ++i, --j)
                {
                    char c = strNumber[i];

                    // skip zeros, except the whole number is zero, which is handled above
                    if (c == '0')
                    {
                        if (j == 5 && bSomeDigitsPresent)
                            oResult.Append(strDigits[strDigits.Length - 10]);
                        else
                            if (j == 9)
                            {
                                bSomeDigitsPresent = false;
                                oResult.Append(strDigits[strDigits.Length - 18]);
                            }

                        continue;
                    }

                    if (c >= '1' && c <= '9')
                    {
                        bSomeDigitsPresent = true;

                        // "one hundred" etc. is simply "hundred", so skip ones, except if at last position
                        if (c != '1' || i == strNumber.Length - 1)
                        {
                            // append digit
                            oResult.Append(strDigits[(c - '1') * 2]);
                        }
                        // append exponent
                        if (i != strNumber.Length - 1)
                        {
                            oResult.Append(strDigits[strDigits.Length - 2 * j + 2]);
                        }
                    }
                    else
                        return strNumber;
                }

                return oResult.ToString();
            }
            catch
            {
                return strNumber;
            }
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when user presses F1 key
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Even arguments</param>
        //===================================================================================================
        private void FormSyncFolders_HelpRequested(object oSender, HelpEventArgs oEventArgs)
        {
            // open in browser in case we don't have any special jump point in help.
            // maybe browser can translate somethig.
            if (Resources.ReadmeHtmlHelpJumpPoint.Equals("#en"))
            {
                System.Diagnostics.Process.Start(
                    System.IO.Path.Combine(Application.StartupPath, "Readme.html"));
            }
            else
            {
                using (HelpForm oForm = new HelpForm(Resources.ReadmeHtmlHelpJumpPoint))
                {
                    oForm.ShowDialog();
                }
            }
        }



        #region image injection part
        //===================================================================================================
        /// <summary>
        /// Picture box control
        /// </summary>
        private PictureBox m_ctlPictureBox;
        //===================================================================================================
        /// <summary>
        /// Image
        /// </summary>
        private System.Drawing.Image m_oLoadedImage;
        //===================================================================================================
        /// <summary>
        /// A dictionary with positions of other elements
        /// </summary>
        private Dictionary<Control, int> m_oOriginalPositions;

        //===================================================================================================
        /// <summary>
        /// Loads an image from application startup path and shows it at the top of the window
        /// </summary>
        /// <param name="strName">Name of the image, without directory specifications</param>
        //===================================================================================================
        private void ReadyToUseImageInjection(string strImageName)
        {
            string strImagePath = System.IO.Path.Combine(Application.StartupPath, strImageName);
            if (System.IO.File.Exists(strImagePath))
            {
                m_oOriginalPositions = new Dictionary<Control, int>();
                foreach (Control ctl in Controls)
                {
                    m_oOriginalPositions[ctl] = ctl.Top;
                }

                m_ctlPictureBox = new PictureBox();
                m_ctlPictureBox.Location = this.ClientRectangle.Location;
                m_ctlPictureBox.Size = new Size(0, 0);
                Controls.Add(m_ctlPictureBox);

                LoadAndResizeImage(strImagePath);

                this.Resize += new EventHandler(ResizeImageAlongWithForm);
            }
        }

        //===================================================================================================
        /// <summary>
        /// Resizes image along with the form
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void ResizeImageAlongWithForm(object oSender, EventArgs oEventArgs)
        {
            ResizeImageAndShiftElements();
        }

        //===================================================================================================
        /// <summary>
        /// Loads an image and resizes it to the width of client area
        /// </summary>
        /// <param name="strImagePath"></param>
        //===================================================================================================
        private void LoadAndResizeImage(string strImagePath)
        {
            m_oLoadedImage = Image.FromFile(strImagePath);
            ResizeImageAndShiftElements();
        }

        //===================================================================================================
        /// <summary>
        /// Resizes image and shifts other elements
        /// </summary>
        //===================================================================================================
        private void ResizeImageAndShiftElements()
        {
            if (m_oLoadedImage != null)
            {
                if (WindowState != FormWindowState.Minimized)
                {
                    float fAspectRatio = (float)m_oLoadedImage.Width / (float)m_oLoadedImage.Height;

                    int nNewWidth = this.ClientSize.Width;
                    if (nNewWidth != 0)
                    {
                        int nNewHeight = (int)(nNewWidth / fAspectRatio);

                        int nHeightChange = nNewHeight - m_ctlPictureBox.Height;

                        this.m_ctlPictureBox.Image = new Bitmap(m_oLoadedImage, nNewWidth, nNewHeight);
                        this.m_ctlPictureBox.Size = new Size(nNewWidth, nNewHeight);

                        this.Height += nHeightChange;
                        ShiftOtherElementsUpOrDown(nNewHeight);
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// Shifts elements, apart from the image box up or down
        /// </summary>
        /// <param name="nNewPictureHeight">New height of the picture</param>
        //===================================================================================================
        private void ShiftOtherElementsUpOrDown(int nNewPictureHeight)
        {
            foreach (Control ctl in m_oOriginalPositions.Keys)
            {
                if ((ctl.Anchor & AnchorStyles.Bottom) == AnchorStyles.None)
                    ctl.Top = m_oOriginalPositions[ctl] + nNewPictureHeight;
            }
        }
        #endregion


    }
}
