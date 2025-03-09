using System;

namespace SyncFolders.Taskbar
{
    //*******************************************************************************************************
    /// <summary>
    /// COM contexts
    /// </summary>
    //*******************************************************************************************************
    [Flags]
    public enum CLSCTX
    {
        /// <summary>
        /// CLSCTX_INPROC_SERVER
        /// </summary>
        eInProcServer = 0x1,
        /// <summary>
        /// CLSCTX_INPROC_HANDLER
        /// </summary>
        eInProcHandler = 0x2,
        /// <summary>
        /// CLSCTX_LOCAL_SERVER
        /// </summary>
        eLocalServer = 0x4,
        /// <summary>
        /// CLSCTX_INPROC_SERVER16
        /// </summary>
        eInProcServer16 = 0x8,
        /// <summary>
        /// CLSCTX_REMOTE_SERVER
        /// </summary>
        eRemoteServer = 0x10,
        /// <summary>
        /// CLSCTX_INPROC_HANDLER16
        /// </summary>
        eInProcHandler16 = 0x20,
        /// <summary>
        /// CLSCTX_RESERVED1
        /// </summary>
        eReserved1 = 0x40,
        /// <summary>
        /// CLSCTX_RESERVED2
        /// </summary>
        eReserved2 = 0x80,
        /// <summary>
        /// CLSCTX_RESERVED3
        /// </summary>
        eReserved3 = 0x100,
        /// <summary>
        /// CLSCTX_RESERVED4
        /// </summary>
        eReserved4 = 0x200,
        /// <summary>
        /// CLSCTX_NO_CODE_DOWNLOAD
        /// </summary>
        eNoCodeDownload = 0x400,
        /// <summary>
        /// CLSCTX_RESERVED5
        /// </summary>
        eReserved5 = 0x800,
        /// <summary>
        /// CLSCTX_NO_CUSTOM_MARSHAL
        /// </summary>
        eNoCustomMarshal = 0x1000,
        /// <summary>
        /// CLSCTX_ENABLE_CODE_DOWNLOAD
        /// </summary>
        eEnableCodeDownload = 0x2000,
        /// <summary>
        /// CLSCTX_NO_FAILURE_LOG
        /// </summary>
        eNoFailureLog = 0x4000,
        /// <summary>
        /// CLSCTX_DISABLE_AAA
        /// </summary>
        eDisableAAA = 0x8000,
        /// <summary>
        /// CLSCTX_ENABLE_AAA
        /// </summary>
        eEnableAAA = 0x10000,
        /// <summary>
        ///  CLSCTX_FROM_DEFAULT_CONTEXT
        /// </summary>
        eFromDefaultContext = 0x20000,
        /// <summary>
        /// CLSCTX_INPROC
        /// </summary>
        eInProc = eInProcServer | eInProcHandler,
        /// <summary>
        /// CLSCTX_SERVER
        /// </summary>
        eServer = eInProcServer | eLocalServer | eRemoteServer,
        /// <summary>
        /// CLSCTX_ALL
        /// </summary>
        eAll = eServer | eInProcHandler
    }
}