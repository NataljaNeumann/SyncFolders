using SyncFoldersApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFoldersTests
{
    //*******************************************************************************************************
    /// <summary>
    /// This class provides means for testing log messages
    /// </summary>
    //*******************************************************************************************************
    internal class HashSetLog : ILogWriter
    {

        //===================================================================================================
        /// <summary>
        /// Non-Localized log
        /// </summary>
        public HashSet<string> Log { get; set; } = new HashSet<string>();

        //===================================================================================================
        /// <summary>
        /// Localized log
        /// </summary>
        public HashSet<string> LocalizedLog { get; set;  } = new HashSet<string>();


        //===================================================================================================
        /// <summary>
        /// Collects a message into the non-localized log
        /// </summary>
        /// <param name="bOnlyToFile"></param>
        /// <param name="nIndent"></param>
        /// <param name="aParts"></param>
        //===================================================================================================
        public void WriteLog(bool bOnlyToFile, int nIndent, params object?[] aParts)
        {
            string strMessage = string.Join("", aParts);
            if (!Log.Contains(strMessage)) 
                Log.Add(strMessage);
        }

        //===================================================================================================
        /// <summary>
        /// Collects given message innto the localized log
        /// </summary>
        /// <param name="nIndent">This parameter is ignored</param>
        /// <param name="strFormat">Format for the message</param>
        /// <param name="aParams">Parameters for the message</param>
        //===================================================================================================
        public void WriteLogFormattedLocalized(int nIndent, string strFormat, params object?[] aParams)
        {
            string strMessage = string.Format(strFormat, aParams);
            if (!Log.Contains(strMessage))
                LocalizedLog.Add(strMessage);
        }
    }
}
