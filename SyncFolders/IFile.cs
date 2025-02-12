using System;
using System.Collections.Generic;
using System.Text;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide basic file functionality
    /// </summary>
    //*******************************************************************************************************
    public interface IFile: IDisposable
    {

        //===================================================================================================
        /// <summary>
        /// Gets or sets position inside the file
        /// </summary>
        //===================================================================================================
        long Position
        {
            get;
            set;
        }

        //===================================================================================================
        /// <summary>
        /// Writes to the file
        /// </summary>
        /// <param name="aBuffer">Buffer to write from</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to write</param>
        //===================================================================================================
        void Write(
            byte[] aBuffer, 
            int nOffset, 
            int nCount
            );

        //===================================================================================================
        /// <summary>
        /// Reads from file
        /// </summary>
        /// <param name="aBuffer">Buffer to read to</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to read</param>
        /// <returns>Count of bytes actually read</returns>
        //===================================================================================================
        int Read(
            byte[] aBuffer, 
            int nOffset, 
            int nCount
            );

        //===================================================================================================
        /// <summary>
        /// Seeks a position insider the file
        /// </summary>
        /// <param name="lOffset">Offset, in regards to the origin</param>
        /// <param name="eOrigin">Origin for the seek</param>
        //===================================================================================================
        void Seek(
            long lOffset, 
            System.IO.SeekOrigin eOrigin
            );

        //===================================================================================================
        /// <summary>
        /// Closes the file
        /// </summary>
        //===================================================================================================
        void Close();
    }
}
