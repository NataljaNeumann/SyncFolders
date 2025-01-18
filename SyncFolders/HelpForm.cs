using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SyncFolders
{
    public partial class HelpForm : Form
    {
        public HelpForm(string strStartPoint)
        {
            InitializeComponent();

            m_ctlWebBrowser.Navigate(
                System.IO.Path.Combine(Application.StartupPath, "Readme.html") + strStartPoint);
        }
    }
}
