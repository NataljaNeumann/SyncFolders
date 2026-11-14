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

using System.Runtime.InteropServices;

//*******************************************************************************************************
/// <summary>
/// Interface for handling taskbar
/// </summary>
//*******************************************************************************************************
[ComImport]
[Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ITaskbarList3
{

    //===================================================================================================
    /// <summary>
    /// Initialize the taskbar list
    /// </summary>
    //===================================================================================================
    void HrInit();

    void AddTab(
        IntPtr hwnd
        );

    void DeleteTab(
        IntPtr hwnd
        );

    void ActivateTab(
        IntPtr hwnd
        );

    void SetActiveAlt(
        IntPtr hwnd
        );

    void MarkFullscreenWindow(
        IntPtr hwnd,
        bool fFullscreen
        );

    //===================================================================================================
    /// <summary>
    /// Sets current progress state
    /// </summary>
    /// <param name="hWnd">The window handle</param>
    /// <param name="ulCompleted">The current status</param>
    /// <param name="ulTotal">The total amount</param>
    //===================================================================================================
    void SetProgressValue(
        IntPtr hWnd, 
        ulong ulCompleted, 
        ulong ulTotal
        );

    //===================================================================================================
    /// <summary>
    /// Sets status of task bar progress
    /// </summary>
    /// <param name="hWnd">The window handle</param>
    /// <param name="nState">The status, one of TaskbarProgressState values</param>
    /// <seealso cref="TaskbarProgressState"/>
    //===================================================================================================
    void SetProgressState(
        IntPtr hWnd, 
        int nState
        );
}

