using System;
using System.Runtime.InteropServices;

namespace SyncFolders.Taskbar
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects of this class show progress in the task bar
    /// </summary>
    //*******************************************************************************************************
    class TaskbarProgress : IDisposable
    {
        //===================================================================================================
        /// <summary>
        /// Taskbar object
        /// </summary>
        private ITaskbarList3 m_oTaskbar;


        //===================================================================================================
        /// <summary>
        /// Creates an instance of a COM object
        /// </summary>
        /// <param name="guidClsid">Class guid of the needed class</param>
        /// <param name="nContext">COM Context</param>
        /// <param name="guidUuid">Guid of interface</param>
        /// <param name="outiTaskbarList">The variable for return</param>
        /// <returns>hresult</returns>
        //===================================================================================================
        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(
            ref Guid guidClsid,
            IntPtr pInner,
            int nContext,
            ref Guid guidUuid,
            out ITaskbarList3 outiTaskbarList
            );

        //===================================================================================================
        /// <summary>
        /// Constructs a new TaskbarProgress object
        /// </summary>
        //===================================================================================================
        public TaskbarProgress()
        {
            Guid CLSID_TaskbarList = new Guid("56FDF344-FD6D-11d0-958A-006097C9A090");
            Guid IID_ITaskbarList3 = typeof(ITaskbarList3).GUID;

            try
            {
                object oTaskbar = null;
                // Create the COM object
                int hresult = CoCreateInstance(
                    ref CLSID_TaskbarList,
                    IntPtr.Zero,
                    (int)(CLSCTX.eInProcServer),
                    ref IID_ITaskbarList3,
                    out m_oTaskbar);

                Marshal.ThrowExceptionForHR(hresult);

                // init the taskbar com object
                m_oTaskbar.HrInit();
            }
            catch
            {
                m_oTaskbar = null;
            }
        }


        //===================================================================================================
        /// <summary>
        /// Sets current progress state
        /// </summary>
        /// <param name="hWnd">The window handle</param>
        /// <param name="ulCompleted">The current status</param>
        /// <param name="ulTotal">The total amount</param>
        //===================================================================================================
        public void SetProgress(
            IntPtr hWnd,
            ulong ulCompleted,
            ulong ulTotal
            )
        {
            if (m_oTaskbar != null)
                m_oTaskbar.SetProgressValue(hWnd, ulCompleted, ulTotal);
        }


        //===================================================================================================
        /// <summary>
        /// Sets status of task bar progress
        /// </summary>
        /// <param name="hWnd">The window handle</param>
        /// <param name="eState">The overall mode of the progress</param>
        //===================================================================================================
        public void SetState(
            IntPtr hWnd,
            TaskbarProgressState eState
            )
        {
            if (m_oTaskbar != null)
                m_oTaskbar.SetProgressState(hWnd, (int)eState);
        }


        //===================================================================================================
        /// <summary>
        /// Disposes the object
        /// </summary>
        //===================================================================================================
        public void Dispose()
        {
            // Release the COM object explicitly
            if (m_oTaskbar != null)
            {
                Marshal.ReleaseComObject(m_oTaskbar);
                m_oTaskbar = null;
            }
        }
    }

}
