using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SyncFolders
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects of this class introduce artifical errors when reading and copying files
    /// </summary>
    //*******************************************************************************************************
    class FileOpenAndCopyWithSimulatedErrors : IFileOpenAndCopyAbstraction
    {
        /// <summary>
        /// Contains list of errors to simulate
        /// </summary>
        private Dictionary<string, List<long>> m_oSimulatedReadErrors = new Dictionary<string,List<long>>();


        //===================================================================================================
        /// <summary>
        /// Constructs a new simulator with error list
        /// </summary>
        /// <param name="oSimulatedReadErrors">List of simulated errors for each file</param>
        //===================================================================================================
        public FileOpenAndCopyWithSimulatedErrors(
            Dictionary<string, List<long>> oSimulatedReadErrors
            )
        {
            // copy file names in upper case
            foreach (string strFilePath in oSimulatedReadErrors.Keys)
            {
                m_oSimulatedReadErrors[strFilePath.ToUpper()] =
                    oSimulatedReadErrors[strFilePath];
            }
        }


        //===================================================================================================
        /// <summary>
        /// If the file is in the list and one of specified positions is in the list then throws an
        /// intentional I/O exception
        /// </summary>
        /// <param name="strFilePath">Path of the file</param>
        /// <param name="lStartPosition">Start position of read</param>
        /// <param name="lLength">Intended read length</param>
        //===================================================================================================
        private void ThrowSimulatedReadErrorIfNeeded(
            string strFilePath,
            long lStartPosition,
            long lLength
            )
        {
            // we compare in upper case
            string strFilePathUpper = strFilePath.ToUpper();
            if (m_oSimulatedReadErrors.ContainsKey(strFilePathUpper))
            {
                // there is such a file, let's see if this read hits one of the mines
                long lEndPosition = lStartPosition + lLength;
                foreach (long lPosition in m_oSimulatedReadErrors[strFilePathUpper])
                {
                    if (lPosition >= lStartPosition && lPosition < lEndPosition)
                    {
                        throw new IOException("This is a simulated I/O error for testing");
                    }
                }
            }
        }

        //===================================================================================================
        /// <summary>
        /// If the file is in the list and one of specified positions is in the list then throws an
        /// intentional I/O exception
        /// </summary>
        /// <param name="fi">Path of the file</param>
        /// <param name="lStartPosition">Start position of read</param>
        /// <param name="lLength">Intended read length</param>
        //===================================================================================================
        private void ThrowSimulatedReadErrorIfNeeded(
            FileInfo fi,
            long lStartPosition,
            long lLength
            )
        {
            ThrowSimulatedReadErrorIfNeeded(fi.FullName, lStartPosition, lLength);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        //===================================================================================================
        public void CopyTo(
            FileInfo fi, 
            string strDestFileName
            )
        {
            ThrowSimulatedReadErrorIfNeeded(fi, 0, fi.Exists ? fi.Length : 0);
            fi.CopyTo(strDestFileName);
        }

        //===================================================================================================
        /// <summary>
        /// Copies a file to another destination
        /// </summary>
        /// <param name="fi">fileinfo of source</param>
        /// <param name="strDestFileName">destinationn file name</param>
        /// <param name="bOverwrite">Indicates, if the file shall be overwritten, if exists</param>
        //===================================================================================================
        public void CopyTo(
            FileInfo fi, 
            string strDestFileName, 
            bool bOverwrite
            )
        {
            ThrowSimulatedReadErrorIfNeeded(fi, 0, fi.Exists ? fi.Length : 0);
            fi.CopyTo(strDestFileName, bOverwrite);
        }


    }
}
