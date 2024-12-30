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

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that impement this interface provide possibility to write some log messages
    /// </summary>
    //*******************************************************************************************************
    interface ILogWriter
    {
        //===================================================================================================
        /// <summary>
        /// Writes a log message, consisting of one or more parts
        /// </summary>
        /// <param name="bOnlyToFile">Indicates that this message shall be written only to file,
        /// and shall not be shown in the GUI</param>
        /// <param name="nIndent">Intent of currrent message</param>
        /// <param name="aParts">Parts of current message</param>
        //===================================================================================================
        void WriteLog(
            bool bOnlyToFile, 
            int nIndent, 
            params object[] aParts
            );

        //===================================================================================================
        /// <summary>
        /// Writes a log message in a formatted manner
        /// </summary>
        /// <param name="nIndent">The nIndent of the new message</param>
        /// <param name="strFormat">Format for the string</param>
        /// <param name="aParams">Parameters for string format</param>
        //===================================================================================================
        void WriteLogFormatted(
            int nIndent,
            string strFormat,
            params object[] aParams
            );

    }
}
