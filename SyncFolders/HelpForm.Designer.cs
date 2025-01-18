namespace SyncFolders
{
    partial class HelpForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpForm));
            this.m_ctlWebBrowser = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // m_ctlWebBrowser
            // 
            resources.ApplyResources(this.m_ctlWebBrowser, "m_ctlWebBrowser");
            this.m_ctlWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.m_ctlWebBrowser.Name = "m_ctlWebBrowser";
            // 
            // HelpForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.m_ctlWebBrowser);
            this.Name = "HelpForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser m_ctlWebBrowser;
    }
}