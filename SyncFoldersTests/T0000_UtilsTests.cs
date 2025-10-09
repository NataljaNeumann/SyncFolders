/*  SyncFolders aims to help you to synchronize two folders or drives, 
    e.g. keeping one as a backup with your family photos. Optionally, 
    some information for restoring of files can be added
 
    Copyright (C) 2024-2025 NataljaNeumann@gmx.de

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

using SyncFoldersApi;

namespace SyncFoldersTests
{

    //*******************************************************************************************************
    /// <summary>
    /// This class tests utils
    /// </summary>
    //*******************************************************************************************************
    public class T0000_UtilsTests
    {
        [SetUp]
        public void Setup()
        {
        }

        //===================================================================================================
        /// <summary>
        /// Tests that file times comparison works correctly
        /// </summary>
        //===================================================================================================
        [Test]
        public void Test_FileTimesComparison()
        {
            DateTime dtmNow = DateTime.Now;
            DateTime dtmSecond = new System.DateTime(
                dtmNow.Year, dtmNow.Month, dtmNow.Day, dtmNow.Hour, dtmNow.Minute, dtmNow.Second);

            for (int i=-999;i<1000;++i)
            {
                Assert.IsTrue(Utils.FileTimesEqual(dtmSecond, dtmSecond.AddMicroseconds(i*100)));
            }

            for (int i = -999; i < 1000; ++i)
            {
                Assert.IsTrue(Utils.FileTimesEqual(dtmSecond.AddMicroseconds(i*100),dtmSecond));
            }
        }
    }
}