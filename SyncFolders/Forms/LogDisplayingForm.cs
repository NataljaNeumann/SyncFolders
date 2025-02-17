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
using System.Text;
using System.Windows.Forms;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Shows the log of last sync
    /// </summary>
    //*******************************************************************************************************
    public partial class LogDisplayingForm : Form
    {
        //===================================================================================================
        /// <summary>
        /// Constructs the oForm object
        /// </summary>
        //===================================================================================================
        public LogDisplayingForm()
        {
            InitializeComponent();
        }

        //===================================================================================================
        /// <summary>
        /// This is executed when visibility of the oForm changes
        /// </summary>
        /// <param name="oSender">Sender object</param>
        /// <param name="oEventArgs">Event args</param>
        //===================================================================================================
        private void LogDisplayingForm_VisibleChanged(object sender, EventArgs e)
        {
            textBoxLog.Select(0, 0);
        }
    }
}
