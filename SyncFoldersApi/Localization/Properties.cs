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

namespace SyncFoldersApi.Localization
{
    //*******************************************************************************************************
    /// <summary>
    /// Class for localization of messages in the API
    /// </summary>
    //*******************************************************************************************************
    public class Properties
    {
        //===================================================================================================
        /// <summary>
        /// Resources for similar access
        /// </summary>
        //===================================================================================================
        public static ILocalizedStrings? Resources
        {
            get; set;
        }
    }
}
