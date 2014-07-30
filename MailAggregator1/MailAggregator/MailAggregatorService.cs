using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace MailAggregator
{
    public partial class MailAggregatorService : ServiceBase
    {
        private List<MailAggregatorHost> hosts = new List<MailAggregatorHost>();
        private string serviceEventSource = "Mail Aggregator Service";

        public MailAggregatorService()
        {
            InitializeComponent();
        }

        #region Service actions
        protected override void OnStart(string[] args)
        {
            #region Provide way to attach debugger by Waiting for specified time
#if DEBUG
            //The following code is simply to ease attaching the debugger to the service to debug the startup routine
            DateTime startTime = DateTime.Now;
            while ((!Debugger.IsAttached) && ((TimeSpan)DateTime.Now.Subtract(startTime)).TotalSeconds < 20)  // Waiting until debugger is attached
            {
                RequestAdditionalTime(1000);  // Prevents the service from timeout
                Thread.Sleep(1000);           // Gives you time to attach the debugger   
            }
            // increase as needed to prevent timeouts
            RequestAdditionalTime(5000);     // for Debugging the OnStart method,     
#endif
            #endregion

            foreach (var sourceConfig in Properties.Settings.Default.AggrSources)
            {
                try
                {
                    MailAggregatorHost host = new MailAggregatorHost();
                    host.PollingFreq = Properties.Settings.Default.PollingFreqSec * 1000;
                    host.SMTPHost = Properties.Settings.Default.SMTPHost;
                    host.SMTPDomain = Properties.Settings.Default.SMTPDomain;
                    host.SMTPUserName = Properties.Settings.Default.SMTPUserName;
                    host.SMTPUseDefaultCredentials = Properties.Settings.Default.SMTPUseDefaultCredentials;
                    host.FromAddress = Properties.Settings.Default.FromAddress;
                    host.IsBodyHtml = Properties.Settings.Default.IsBodyHtml;
                    host.InitializeHost(sourceConfig);

                    EventLog.WriteEntry(serviceEventSource, "Starting aggregator host: " + host.MAGSourceName, EventLogEntryType.Information, 2);

                    host.AggregatorHostError += new RaiseMessageDelegate(host_AggregatorHostError);
                    hosts.Add(host);
                    host.StartPolling();
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry(serviceEventSource,
                        string.Format("There was a problem initializing an aggregator host\r\nHost config: {0}\r\n{1}", sourceConfig, ex.ToString()),
                        EventLogEntryType.Error, 1);
                }
            }
            EventLog.WriteEntry(serviceEventSource,
                string.Format("{0} (version {1}) started successfully with {2} aggregator host(s)", 
                    serviceEventSource, 
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    hosts.Count), EventLogEntryType.Information, 0);
        }
        protected override void OnStop()
        {
            foreach (MailAggregatorHost host in hosts)
            {
                host.IsRunning = false;
            }
        } 
        #endregion

        #region Events
        private void host_AggregatorHostError(string message)
        {
            EventLog.WriteEntry(serviceEventSource, message, EventLogEntryType.Error, 1);
        } 
        #endregion
    }
}
