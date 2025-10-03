using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// resources must be set by user, null-reference ignored.
#pragma warning disable CS8618

namespace SyncFoldersApi.Localization
{
    //*******************************************************************************************************
    /// <summary>
    /// Class for localization of messages in the API
    /// </summary>
    //*******************************************************************************************************
    public class Properties
    {
        //===================================================================================================
        /// <summary>
        /// Resources for similar access
        /// </summary>
        //===================================================================================================
        public static ILocalizedStrings Resources
        {
            get; set;
        }
    }
}
