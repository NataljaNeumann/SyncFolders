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
using System.Windows.Forms;
using System.Threading;
using System.Globalization;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// The program class
    /// </summary>
    //*******************************************************************************************************
    static class Program
    {
        public static readonly bool CreateRelease = System.Environment.CommandLine.EndsWith("-CreateRelease");

        //===================================================================================================
        /// <summary>
        /// The main entry point for the application
        /// </summary>
        //===================================================================================================
        [STAThread]
        static void Main()
        {
            SetCultureForThread(Thread.CurrentThread);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormSyncFolders());
        }

        //===================================================================================================
        /// <summary>
        /// This method can be used for manual settinng of culture for the application and worker threads.
        /// </summary>
        /// <param name="oThread">The thread for setting culture</param>
        //===================================================================================================
        public static void SetCultureForThread(Thread oThread)
        {
#if DEBUG
             //oThread.CurrentCulture = new CultureInfo("ja-JP");
             //oThread.CurrentUICulture = new CultureInfo("ja-JP");
#endif
        }
    }
}
