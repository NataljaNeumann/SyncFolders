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

namespace SyncFolders.Taskbar
{
    //*******************************************************************************************************
    /// <summary>
    /// Task bar progress states
    /// </summary>
    //*******************************************************************************************************
    public enum TaskbarProgressState
    {
        //===================================================================================================
        /// <summary>
        /// TBPF_NOPROGRESS: No progress displayed.
        /// </summary>
        eNoProgress = 0,

        //===================================================================================================
        /// <summary>
        /// TBPF_INDETERMINATE: Marquee-style progress.
        /// </summary>
        eIndeterminate = 1,

        //===================================================================================================
        /// <summary>
        /// TBPF_NORMAL: Normal progress.
        /// </summary>
        eNormal = 2,

        //===================================================================================================
        /// <summary>
        /// TBPF_ERROR: Error state.
        /// </summary>
        eError = 4,

        //===================================================================================================
        /// <summary>
        /// TBPF_PAUSED: Paused state.
        /// </summary>
        ePaused = 8
    }
}