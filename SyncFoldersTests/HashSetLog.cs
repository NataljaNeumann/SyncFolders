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

using SyncFoldersApi;

namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// This class provides means for testing log messages
    /// </summary>
    //*******************************************************************************************************
    internal class HashSetLog : ILogWriter
    {

        //===================================================================================================
        /// <summary>
        /// Non-Localized log
        /// </summary>
        public HashSet<string> Log { get; set; } = new HashSet<string>();

        //===================================================================================================
        /// <summary>
        /// Localized log
        /// </summary>
        public HashSet<string> LocalizedLog { get; set;  } = new HashSet<string>();


        //===================================================================================================
        /// <summary>
        /// Collects a message into the non-localized log
        /// </summary>
        /// <param name="bOnlyToFile"></param>
        /// <param name="nIndent"></param>
        /// <param name="aParts"></param>
        //===================================================================================================
        public void WriteLog(bool bOnlyToFile, int nIndent, params object?[] aParts)
        {
            string strMessage = string.Join("", aParts);
            if (!Log.Contains(strMessage)) 
                Log.Add(strMessage);
        }

        //===================================================================================================
        /// <summary>
        /// Collects given message innto the localized log
        /// </summary>
        /// <param name="nIndent">This parameter is ignored</param>
        /// <param name="strFormat">Format for the message</param>
        /// <param name="aParams">Parameters for the message</param>
        //===================================================================================================
        public void WriteLogFormattedLocalized(int nIndent, string strFormat, params object?[] aParams)
        {
            string strMessage = string.Format(strFormat, aParams);
            if (!Log.Contains(strMessage))
                LocalizedLog.Add(strMessage);
        }
    }
}
