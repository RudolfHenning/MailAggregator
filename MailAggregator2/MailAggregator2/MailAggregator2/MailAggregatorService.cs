using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Xml;

namespace MailAggregator
{
    public partial class MailAggregatorService : ServiceBase
    {
        private List<MailAggregatorHost> hosts = new List<MailAggregatorHost>();
        private string serviceEventSource = "Mail Aggregator 2 Service";

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
            try
            {
                string aggregatorsFile = Properties.Settings.Default.AggregatorSourceFile;
                if (!aggregatorsFile.Contains("\\"))
                    aggregatorsFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), aggregatorsFile);

                if (System.IO.File.Exists(aggregatorsFile))
                {
                    XmlDocument inputFile = new XmlDocument();
                    inputFile.Load(aggregatorsFile);
                    XmlElement root = inputFile.DocumentElement;
                    foreach (XmlElement entry in root.SelectNodes("aggregator"))
                    {
                        try
                        {
                            string sourceConfig = entry.OuterXml;

                            MailAggregatorHost host = new MailAggregatorHost();
                            host.PollingFreq = Properties.Settings.Default.PollingFreqSec * 1000;
                            host.SMTPHost = Properties.Settings.Default.SMTPHost;
                            host.SMTPDomain = Properties.Settings.Default.SMTPDomain;
                            host.SMTPUserName = Properties.Settings.Default.SMTPUserName;
                            host.SMTPUseDefaultCredentials = Properties.Settings.Default.SMTPUseDefaultCredentials;
                            host.FromAddress = Properties.Settings.Default.FromAddress;
                            host.IsBodyHtml = Properties.Settings.Default.IsBodyHtml;
                            host.InitializeHost(sourceConfig);

                            host.AggregatorHostError += new RaiseMessageDelegate(host_AggregatorHostError);
                            hosts.Add(host);
                            host.StartPolling();
                        }
                        catch (Exception ex)
                        {
                            WriteLog(string.Format("There was a problem initializing an aggregator host\r\nHost config: {0}\r\n{1}", entry.OuterXml, ex.ToString()),
                                EventLogEntryType.Error, 1);
                        }
                    }
                    WriteLog(string.Format("{0} (version {1}) started successfully with {2} aggregator host(s)",
                    serviceEventSource,
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    hosts.Count), EventLogEntryType.Information, 0);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, EventLogEntryType.Error, 0);
            }

            base.OnStart(args);
        }
        protected override void OnStop()
        {
            foreach (MailAggregatorHost host in hosts)
            {
                host.IsRunning = false;
            }
            base.OnStop();
        }
        #endregion

        #region Events
        private void host_AggregatorHostError(string message)
        {
            EventLog.WriteEntry(serviceEventSource, message, EventLogEntryType.Error, 1);
        }
        #endregion

        private void WriteLog(string msg, EventLogEntryType eventType, int eventId)
        {
            EventLog.WriteEntry(serviceEventSource, msg, eventType, eventId);
        }
    }
}
