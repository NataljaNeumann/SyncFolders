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
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SyncFolders.Properties;
using SyncFolders.Taskbar;
using SyncFoldersApi;

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
        /// First folder from GUI textbox
        /// </summary>
        string m_strFolder1 = "";
        /// <summary>
        /// Second folder from GUI textbox
        /// </summary>
        string m_strFolder2 = "";
        /// <summary>
        /// If the application starts from a CD then we fill the first directory and forward
        /// focus to second directory textbox
        /// </summary>
        bool m_bForwardFocusToSecondFolder;

        /// <summary>
        /// Found file pairs for possible synchronization
        /// </summary>
        List<KeyValuePair<string, string>> m_aFilePairs = 
            new List<KeyValuePair<string, string>>();

        /// <summary>
        /// This is the index of currently processed file (the last one)
        /// </summary>
        volatile int m_nCurrentFile;
        /// <summary>
        /// This is the path of currently processed file (the last one)
        /// </summary>
        volatile string m_strCurrentPath = "";

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
        Taskbar.TaskbarProgress? m_oTaskbarProgress;

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
        /// <summary>
        /// Chooser of steps from API
        /// </summary>
        IFilePairStepChooser m_iFileStepChooser = new FilePairStepChooser();
        /// <summary>
        /// The file pair direction logic from API
        /// </summary>
        IFilePairStepsDirectionLogic m_iFileStepLogic = new FilePairStepsDirectionLogic();
        /// <summary>
        /// The test and repair steps implementation from API
        /// </summary>
        IFilePairSteps m_iStepsImpl = new FilePairSteps();
        /// <summary>
        /// Settings and environment of the process
        /// </summary>
        SettingsAndEnvironment m_oSettings = new SettingsAndEnvironment();

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
// disabling warnings. This code can only be executed during builds.
// we expect the developer will fix it, if something happens
#pragma warning disable CS8604, CS8602, CS8600
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
#pragma warning restore CS8604, CS8602, CS8600


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
            // compiler seems to have a problem: doesn' recognize that there can be an exception in Create()...
#pragma warning disable CS0219
            bool bFolderWritable = false;
#pragma warning disable CS0219
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
        int CountPreviousSiblings(System.Xml.XmlNode? oXmlNode)
        {
            int nResult = 0;
            if (oXmlNode!=null)
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
            m_oSettings.CreateInfo = m_cbCreateRestoreInfo.Checked;
            m_oSettings.TestFiles = m_cbTestAllFiles.Checked;
            m_oSettings.RepairFiles = m_cbRepairBlockFailures.Checked;
            m_oSettings.PreferPhysicalCopies = m_cbPreferCopies.Checked;
            m_oSettings.FirstToSecond = m_cbFirstToSecond.Checked;
            m_oSettings.FirstReadOnly = m_cbFirstReadonly.Checked;
            m_oSettings.FirstToSecondDeleteInSecond = m_cbDeleteFilesInSecond.Checked;
            m_oSettings.TestFilesSkipRecentlyTested = !m_oSettings.TestFiles || m_cbSkipRecentlyTested.Checked;
            m_oSettings.IgnoreTimeDifferencesBetweenDataAndSaveInfo = m_cbIgnoreTime.Checked;
            m_oSettings.FirstToSecondSyncMode = m_cbSyncMode.Checked;

            if (m_oSettings.FirstToSecond && m_oSettings.FirstToSecondDeleteInSecond)
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
            m_strCurrentPath = "";
            m_lblProgress.Text = Properties. Resources.ScanningFolders;
            m_oTimerUpdateFileDescription.Start();




            m_oSettings.CancelClicked = false;
            m_oLogToShow = new StringBuilder();
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

 
            m_oLogFile.WriteLine("     First2Second: " + (m_oSettings.FirstToSecond ? "yes" : "no"));
            m_oLogFileLocalized.WriteLine(m_cbFirstToSecond.Text + ": " + (m_oSettings.FirstToSecond ? Resources.Yes : Resources.No));
            if (m_oSettings.FirstToSecond)
            {
                m_oLogFile.WriteLine("         SyncMode: " + (m_oSettings.FirstToSecondSyncMode ? "yes" : "no"));
                m_oLogFile.WriteLine("    FirstReadOnly: " + (m_oSettings.FirstReadOnly ? "yes" : "no"));
                m_oLogFile.WriteLine("   DeleteInSecond: " + (m_oSettings.FirstToSecondDeleteInSecond ? "yes" : "no"));

                m_oLogFileLocalized.WriteLine(m_cbFirstToSecond.Text + ":" + (m_oSettings.FirstToSecond ? Resources.Yes : Resources.No));
                m_oLogFile.WriteLine(m_cbSyncMode.Text + ": " + (m_oSettings.FirstToSecondSyncMode ? Resources.Yes : Resources.No));
                m_oLogFile.WriteLine(m_cbFirstReadonly + ": " + (m_oSettings.FirstReadOnly ? Resources.Yes : Resources.No));
                m_oLogFile.WriteLine(m_cbDeleteFilesInSecond.Text + ": " + (m_oSettings.FirstToSecondDeleteInSecond ? Resources.Yes : Resources.No));

            }

            m_oLogFile.WriteLine("CreateRestoreInfo: " + (m_oSettings.CreateInfo ? "yes" : "no"));
            m_oLogFileLocalized.WriteLine(m_cbCreateRestoreInfo.Text + ": " + (m_oSettings.CreateInfo ? Resources.Yes : Resources.No));

            m_oLogFile.WriteLine("        TestFiles: " + 
                (m_oSettings.TestFiles ? (m_oSettings.TestFilesSkipRecentlyTested ? "if not tested recently": "yes" ): "no"));
            m_oLogFileLocalized.WriteLine(m_cbTestAllFiles.Text + ": " +
                (m_oSettings.TestFiles ? (m_oSettings.TestFilesSkipRecentlyTested ? m_cbSkipRecentlyTested.Text : Resources.Yes) : Resources.No));

            if (m_oSettings.TestFiles)
            {
                m_oLogFile.WriteLine("      RepairFiles: " + (m_oSettings.RepairFiles ? "yes" : "no"));
                m_oLogFileLocalized.WriteLine(m_cbRepairBlockFailures + ": " + (m_oSettings.RepairFiles ? Resources.Yes : Resources.No));
                if (m_oSettings.RepairFiles)
                {
                    m_oLogFile.WriteLine("     PreferCopies: " + (m_oSettings.PreferPhysicalCopies ? "yes" : "no"));
                    m_oLogFile.WriteLine(m_cbPreferCopies + ": " + (m_oSettings.PreferPhysicalCopies ? Resources.Yes : Resources.No));
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
            string strUrl = "https://www.gnu.org/licenses/gpl-2.0.html";
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(strUrl) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", strUrl);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", strUrl);
                }
            }
            catch (Exception oEx)
            {
                MessageBox.Show("Could not open browser: " + oEx.Message);
            }
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
                Invoke(new EventHandler(delegate(object? sender2, EventArgs args)
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
                m_oSettings.CancelClicked = true;
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

            IFilePairSteps iStepsImpl = new SyncFoldersApi.FilePairSteps();

            InMemoryFileSystem oInMemoryFileSystem = new InMemoryFileSystem();
            m_iFileSystem = oInMemoryFileSystem;

            m_cbParallel.Checked = false;
            System.Windows.Forms.MessageBox.Show(this, "The self test doesn't test easy conditions. " +
                "It simulates E/A errors, bad checksums and other problems, so a log full of error messages "
                +"is expected. You need to interpret the messages", "About self test", MessageBoxButtons.OK, 
                MessageBoxIcon.Information);

            DateTime dtmTimeForFile = DateTime.UtcNow;

            if (string.IsNullOrEmpty(m_tbxFirstFolder.Text))
                m_tbxFirstFolder.Text = Path.Combine(Application.StartupPath, "TestFolder1");

            if (string.IsNullOrEmpty(m_tbxSecondFolder.Text))
                m_tbxSecondFolder.Text = Path.Combine(Application.StartupPath, "TestFolder2");

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
            iStepsImpl.CreateSavedInfo(System.IO.Path.Combine(m_tbxFirstFolder.Text, "TestPicture1.jpg"),
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "RestoreInfo\\TestPicture1.jpg.chk"),
                m_iFileSystem, m_oSettings, this);
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
            iStepsImpl.CreateSavedInfo(System.IO.Path.Combine(m_tbxFirstFolder.Text, "TestPicture2.jpg"),
                System.IO.Path.Combine(m_tbxFirstFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"),
                m_iFileSystem, m_oSettings, this);
            m_iFileSystem.CopyFileFromReal(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Coolpix_2010-08-01_23-57-56.JPG"),
                System.IO.Path.Combine(m_tbxSecondFolder.Text, "TestPicture2.jpg"));
            m_iFileSystem.SetLastWriteTimeUtc(System.IO.Path.Combine(
                m_tbxSecondFolder.Text, "TestPicture2.jpg"), dtmOld);
            iStepsImpl.CreateSavedInfo(System.IO.Path.Combine(m_tbxSecondFolder.Text, "TestPicture2.jpg"),
                System.IO.Path.Combine(m_tbxSecondFolder.Text, "RestoreInfo\\TestPicture2.jpg.chk"),
                m_iFileSystem, m_oSettings, this);
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
            iStepsImpl.CreateSavedInfo(strPathOfTestFile9,
                    strPathOfTestFile9.Replace
                ("RestorableSavedInfoVersion0.dat",
                "RestoreInfo\\RestorableSavedInfoVersion0.dat.chk"),
                0, false,
                m_iFileSystem, m_oSettings, this);

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
                m_iStepsImpl.CreateSavedInfo(strDestDataFilePath, 
                    Utils.CreatePathOfChkFile(strFolder, "RestoreInfo", strFileName, ".chk"),
                    m_iFileSystem, m_oSettings, this);
            }
            // set date and time to specified value
            m_iFileSystem.SetLastWriteTimeUtc(strDestDataFilePath, dtmTimeForFile);

            return strDestDataFilePath;
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
                    Invoke(new EventHandler(delegate(object? sender, EventArgs args)
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
                    Invoke(new EventHandler(delegate(object? sender, EventArgs args)
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
                if (!m_oSettings.CancelClicked)
                {
                    int currentFile = 0;


                    // sort the list, so it is in a defined order
                    SortedDictionary<string, string> oSorted = new SortedDictionary<string, string>();
                    foreach (KeyValuePair<string, string> pathPair in m_aFilePairs)
                    {
                        if (!m_oSettings.FirstToSecond)
                        {
                            if (string.Compare(pathPair.Key, pathPair.Value,
                                StringComparison.InvariantCultureIgnoreCase) < 0)
                            {
                                oSorted[pathPair.Key] = pathPair.Value;
                            }
                            else
                            {
                                oSorted[pathPair.Value] = pathPair.Key;
                            }
                        }
                        else
                        {
                            oSorted[pathPair.Key] = pathPair.Value;
                        }
                    }


                    // start processing file pairs, one by one
                    foreach (KeyValuePair<string, string> oPathPair in oSorted)
                    {

                        //*
                        m_oSemaphoreParallelThreads.WaitOne();

                        if (m_oSettings.CancelClicked)
                        {
                            m_oSemaphoreParallelThreads.Release();
                            break;
                        }

                        System.Threading.Thread oWorker = new System.Threading.Thread(FilePairWorker);
                        Program.SetCultureForThread(oWorker);
                        oWorker.Priority = System.Threading.ThreadPriority.Lowest;
                        oWorker.Start(oPathPair);

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
                        m_strCurrentPath = oPathPair.Key;


                        // update progress bar
                        if ((++currentFile) % 10 == 0)
                        {
                            // GUI
                            if (InvokeRequired)
                            {
                                Invoke(new EventHandler(delegate(object? sender, EventArgs args)
                                {
                                    m_ctlProgressBar.Value = currentFile;
                                    m_lblProgress.Text = oPathPair.Key;

                                    // task bar
                                    if (m_oTaskbarProgress != null)
                                        m_oTaskbarProgress.SetProgress(this.Handle, (ulong)currentFile, (ulong)m_aFilePairs.Count);
                                }));
                            }
                            else
                            {
                                m_ctlProgressBar.Value = currentFile;
                                m_lblProgress.Text = oPathPair.Key;

                                // task bar
                                if (m_oTaskbarProgress != null)
                                    m_oTaskbarProgress.SetProgress(this.Handle, (ulong)currentFile, (ulong)m_aFilePairs.Count);
                            }
                        };

                        if (m_oSettings.CancelClicked)
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
                    Invoke(new EventHandler(delegate(object? sender, EventArgs args)
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

                if (!m_oSettings.CancelClicked)
                {
                    m_strCurrentPath = Resources.DeletingObsoleteSavedInfos;
                    RemoveOldFilesAndDirs(m_strFolder1, m_strFolder2);
                }

                if (m_oSettings.CancelClicked)
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

            m_oLogFile?.Close();
            m_oLogFileLocalized?.Close();
            m_oLogFile?.Dispose();
            m_oLogFileLocalized?.Dispose();
            m_oLogFile = null;
            m_oLogFileLocalized = null;


            // Free pool of blocks, used during sync
            Block.FreeMemory();

            if (InvokeRequired)
            {
                Invoke(new EventHandler(delegate (object? sender, EventArgs args)
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
                            form.textBoxLog.Text = m_oLogToShow.ToString();
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
                        form.textBoxLog.Text = m_oLogToShow.ToString();
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
            // find subdirectories in both directories
            Dictionary<string, bool> oDirNames = new Dictionary<string, bool>();

            // the block is for releasing the di objects
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
                }
                else
                if (di2.Exists && !di1.Exists)
                {
                    if (!m_oSettings.FirstToSecond)
                    {
                        di1.Create();
                        di1.Attributes = di2.Attributes;
                    }
                }
                ;


                if (di1.Name.Equals("RestoreInfo",
                    StringComparison.CurrentCultureIgnoreCase))
                    return;
                if (m_oSettings.CreateInfo)
                {
                    IDirectoryInfo di3;

                    if (!m_oSettings.FirstToSecond || !m_oSettings.FirstReadOnly)
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

                if (m_oSettings.FirstToSecond && m_oSettings.FirstToSecondDeleteInSecond)
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
                if (di1.Exists || !m_oSettings.FirstToSecond)
                {
                    foreach (IFileInfo fi1 in di1.GetFiles())
                    {
                        if (fi1.Name.Length <= 4 || !".tmp".Equals(
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
                    m_aFilePairs.Add(new KeyValuePair<string, string>(
                        System.IO.Path.Combine(strDirPath1, strFileName),
                        System.IO.Path.Combine(strDirPath2, strFileName)));

                if (di1.Exists || !m_oSettings.FirstToSecond)
                {
                    foreach (IDirectoryInfo sub1 in di1.GetDirectories())
                        oDirNames[sub1.Name] = false;

                    foreach (IDirectoryInfo sub2 in di2.GetDirectories())
                        oDirNames[sub2.Name] = false;
                }
            }


            // continue with the subdirs
            foreach (string strSubDirName in oDirNames.Keys)
            {
                FindFilePairs(System.IO.Path.Combine(strDirPath1, strSubDirName), 
                    System.IO.Path.Combine(strDirPath2, strSubDirName));
                if (m_oSettings.CancelClicked)
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
            // find subdirectories in both directories
            Dictionary<string, bool> oDirNames = new Dictionary<string, bool>();

            // block is for freeing the di objects
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
                    if (m_oSettings.FirstToSecond && m_oSettings.FirstToSecondDeleteInSecond)
                    {
                        di2.Delete(true);
                        WriteLogFormattedLocalized(0, Resources.DeletedFolder,
                            di2.FullName, strFolderPath1);
                        WriteLog(true, 0, "Deleted folder ", di2.FullName,
                            " including contents, because there is no ",
                            strFolderPath1, " anymore");
                        return;
                    }
                }
                ;

                IDirectoryInfo di3;
                // consider contents of the first folder
                if (!m_oSettings.FirstToSecond || !m_oSettings.FirstReadOnly)
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
                                        System.IO.Path.Combine(di3.FullName, fi.Name), oEx.Message);
                                    WriteLog(true, 0, "Error while deleting ",
                                        System.IO.Path.Combine(di3.FullName, fi.Name), ": ", oEx.Message);
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



                if (di1.Exists)
                    foreach (IDirectoryInfo diSubDir1 in di1.GetDirectories())
                        oDirNames[diSubDir1.Name] = false;

                if (di2.Exists)
                    foreach (IDirectoryInfo diSubDir2 in di2.GetDirectories())
                        oDirNames[diSubDir2.Name] = false;

            }

            // continue with the subdirs
            foreach (string strSubDirName in oDirNames.Keys)
            {
                RemoveOldFilesAndDirs(System.IO.Path.Combine(strFolderPath1, strSubDirName), 
                    System.IO.Path.Combine(strFolderPath2, strSubDirName));
                if (m_oSettings.CancelClicked)
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
                IFileInfo? fi1, 
                IFileInfo? fi2
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
                IFileInfo? fi1, 
                IFileInfo? fi2)
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
            object? oFilePair
            )
        {
            if (oFilePair == null)
                throw new ArgumentNullException(nameof(oFilePair));

            KeyValuePair<string, string> pathPair = 
                (KeyValuePair<string, string>)oFilePair;
            try
            {
                m_iFileStepChooser.ProcessFilePair(
                    pathPair.Key, pathPair.Value, m_iFileSystem,
                    m_oSettings, m_iFileStepLogic, m_iStepsImpl, this);
            }
            catch (OperationCanceledException oEx)
            {
                // report only if it is unexpected
                if (!m_oSettings.CancelClicked)
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




        System.Text.StringBuilder m_oLogToShow = new StringBuilder();
        System.IO.TextWriter? m_oLogFileLocalized;
        System.IO.TextWriter? m_oLogFile;


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
            params object? [] aParts
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
                lock (m_oLogToShow)
                {
                    m_oLogFile.Write("{0}UT\t={1}=\t", utc.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        System.Threading.Thread.CurrentThread.ManagedThreadId);
                    if (!bOnlyToNonlocalizedLog && m_oLogFileLocalized!=null)
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
                        if (!bOnlyToNonlocalizedLog && m_oLogFileLocalized != null)
                             m_oLogFileLocalized.Write("\t");
                        if (!bOnlyToNonlocalizedLog) 
                            m_oLogToShow.Append("        ");
                    }

                    foreach (object? oPart in aParts)
                    {
                        if (oPart != null)
                        {
                            string? strPart = oPart.ToString();
                            if (strPart != null)
                            {
                                strPart = strPart.Replace(Environment.NewLine, "");
                                if (!bOnlyToNonlocalizedLog)
                                {
                                    m_oLogToShow.Append(strPart);
                                    m_oLogFileLocalized?.Write(strPart);
                                }
                                m_oLogFile.Write(strPart);
                            }
                        }
                    }


                    if (!bOnlyToNonlocalizedLog)
                    {
                        m_oLogToShow.Append(Environment.NewLine);

                        if (m_oLogFileLocalized != null)
                        {
                            // continue with rtl
                            if (Resources.RightToLeft.Equals("yes"))
                                m_oLogFileLocalized.Write((char)0x200F);

                            m_oLogFileLocalized.WriteLine();
                            m_oLogFileLocalized.Flush();
                        }
                    }
                    m_oLogFile.WriteLine();
                    m_oLogFile.Flush();
                }
            }
            else
            {
                if (!bOnlyToNonlocalizedLog)
                {
                    lock (m_oLogToShow)
                    {
                        while (nIndent-- > 0)
                        {
                            m_oLogToShow.Append("        ");
                        }

                        foreach (object? oPart in aParts)
                        {
                            if (oPart != null)
                            {
                                string? strPart = oPart.ToString();
                                if (strPart != null)
                                    m_oLogToShow.Append(strPart);
                            }
                        }

                        m_oLogToShow.Append("\r\n");
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
            params object?[] aParams
            )
        {
            if (m_oLogFileLocalized != null)
            {
                System.DateTime utc = System.DateTime.UtcNow;
                System.DateTime now = utc.ToLocalTime();
                lock (m_oLogToShow)
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
                        m_oLogToShow.Append("        ");
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
                    m_oLogToShow.Append(s);
                    m_oLogFileLocalized.Write(s);

                    m_oLogToShow.Append(Environment.NewLine);
                    m_oLogFileLocalized.Write(Environment.NewLine);
                    m_oLogFileLocalized.Flush();
                }
            }
            else
            {
                lock (m_oLogToShow)
                {
                    while (nIndent-- > 0)
                    {
                        m_oLogToShow.Append("        ");
                    }

                    string s = string.Format(strFormat, aParams);
                    m_oLogToShow.Append(s);

                    m_oLogToShow.Append("\r\n");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oNumber"></param>
        /// <returns></returns>
        public static object FormatNumber(object? oNumber)
        {
            if (oNumber == null)
                return "";

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
        public static string FormatNumberTibet(object? oNumber)
        {
            if (oNumber == null)
                return "";

            string? strNumber = oNumber.ToString();
            if (strNumber == null)
                return "";

            const string c_strTibetDigits = "༠༡༢༣༤༥༦༧༨༩";
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
            if (oNumber == null)
                return "";

            string? strNumber = oNumber.ToString();
            if (strNumber == null)
                return "";

            string[] c_strLatinHundredThousands = new string[] { "I̅", "V̅", "X̅", "L̅", "C̅", "D̅", "M̅", "D̿" };
            string[] c_strLatinDigits = new string[] { "I", "V", "X", "L", "C", "D", "M", "V̅", "X̅", "L̅", "C̅", "D̅", "M̅" };


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

                if (strNumber == null)
                    return oResult.ToString();

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
            if (oNumber == null)
                return "";

            string? strNumber = oNumber.ToString();
            if (strNumber == null)
                return "";

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
        private PictureBox? m_ctlPictureBox;
        //===================================================================================================
        /// <summary>
        /// Image
        /// </summary>
        private System.Drawing.Image? m_oLoadedImage;
        //===================================================================================================
        /// <summary>
        /// A dictionary with positions of other elements
        /// </summary>
        private Dictionary<Control, int>? m_oOriginalPositions;

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
        private void ResizeImageAlongWithForm(object? oSender, EventArgs oEventArgs)
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
            if (m_oLoadedImage != null && m_ctlPictureBox != null)
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
            if (m_oOriginalPositions != null)
            {
                foreach (Control ctl in m_oOriginalPositions.Keys)
                {
                    if ((ctl.Anchor & AnchorStyles.Bottom) == AnchorStyles.None)
                        ctl.Top = m_oOriginalPositions[ctl] + nNewPictureHeight;
                }
            }
        }
        #endregion


    }
}
