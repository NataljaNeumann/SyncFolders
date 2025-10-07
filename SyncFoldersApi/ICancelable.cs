using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFoldersApi
{
    //*******************************************************************************************************
    /// <summary>
    /// Objects that implement this interface provide cancelation environment for API functions
    /// </summary>
    //*******************************************************************************************************
    public interface ICancelable
    {
        //===================================================================================================
        /// <summary>
        /// Indicates, if canclel has been clicked
        /// </summary>
        public bool CancelClicked
        {
            get; set;
        }
    }
}
