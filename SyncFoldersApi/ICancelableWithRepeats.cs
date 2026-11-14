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

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide cancelation environment for API functions as well as
    /// retry/repeat settings
    /// </summary>
    //*******************************************************************************************************
    public interface ICancelableWithRepeats: ICancelable
    {
        //===================================================================================================
        /// <summary>
        /// The number of retries, in case there are errors. This configuration parameter isn't used by
        /// all methods and can be ignored by the actual implementation
        /// </summary>
        public int Retries
        {
            get;
        }

        //===================================================================================================
        /// <summary>
        /// The number of milliseconds to wait before a retry. This configuration parameter isn't used by
        /// all methods and can be ignored by the actual implementation
        /// </summary>
        public int RetryDelay
        {
            get;
        }
    }
}
