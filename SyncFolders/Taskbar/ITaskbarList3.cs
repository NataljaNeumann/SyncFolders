using System;
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

