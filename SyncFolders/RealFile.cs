using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Access to a real file
    /// </summary>
    //*******************************************************************************************************
    public class RealFile : IFile
    {
        //===================================================================================================
        /// <summary>
        /// Contains the actual file stream
        /// </summary>
        private FileStream m_oStream;

        //===================================================================================================
        /// <summary>
        /// Constructs a new real file object
        /// </summary>
        /// <param name="oStream">The real stream object</param>
        //===================================================================================================
        public RealFile(
            FileStream oStream
            )
        {
            m_oStream = oStream;
        }

        //===================================================================================================
        /// <summary>
        /// Gets or sets the position inside the file
        /// </summary>
        public long Position
        {
            get
            {
                return m_oStream.Position;
            }
            set
            {
                m_oStream.Position = value;
            }
        }

        //===================================================================================================
        /// <summary>
        /// Writes to the file
        /// </summary>
        /// <param name="aBuffer">Buffer to write from</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to write</param>
        //===================================================================================================
        public void Write(
            byte[] aBuffer, 
            int nOffset, 
            int nCount
            )
        {
            m_oStream.Write(aBuffer, nOffset, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Reads from file
        /// </summary>
        /// <param name="aBuffer">Buffer to read to</param>
        /// <param name="nOffset">Offset inside the buffer</param>
        /// <param name="nCount">Count of bytes to read</param>
        /// <returns>Count of bytes actually read</returns>
        //===================================================================================================
        public int Read(
            byte[] aBuffer, 
            int nOffset, 
            int nCount
            )
        {
            return m_oStream.Read(aBuffer, nOffset, nCount);
        }


        //===================================================================================================
        /// <summary>
        /// Seeks a position insider the file
        /// </summary>
        /// <param name="lOffset">Offset, in regards to the origin</param>
        /// <param name="eOrigin">Origin for the seek</param>
        //===================================================================================================
        public void Seek(
            long lOffset, 
            SeekOrigin eOrigin)
        {
            m_oStream.Seek(lOffset, eOrigin);
        }


        //===================================================================================================
        /// <summary>
        /// Closes the file
        /// </summary>
        //===================================================================================================
        public void Close()
        {
            m_oStream.Close();
        }

        //===================================================================================================
        /// <summary>
        /// Closes the file and disposes the object
        /// </summary>
        //===================================================================================================
        public void Dispose()
        {
            m_oStream.Dispose();
        }
    }
}
