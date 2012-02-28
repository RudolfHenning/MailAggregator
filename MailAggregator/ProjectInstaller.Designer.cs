namespace MailAggregator
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.MailAggregatorServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.MailAggregatorServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // MailAggregatorServiceProcessInstaller
            // 
            this.MailAggregatorServiceProcessInstaller.Password = null;
            this.MailAggregatorServiceProcessInstaller.Username = null;
            // 
            // MailAggregatorServiceInstaller
            // 
            this.MailAggregatorServiceInstaller.DelayedAutoStart = true;
            this.MailAggregatorServiceInstaller.Description = "Mail Aggregator Service";
            this.MailAggregatorServiceInstaller.DisplayName = "Mail Aggregator Service";
            this.MailAggregatorServiceInstaller.ServiceName = "Mail Aggregator Service";
            this.MailAggregatorServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.MailAggregatorServiceProcessInstaller,
            this.MailAggregatorServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller MailAggregatorServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller MailAggregatorServiceInstaller;
    }
}