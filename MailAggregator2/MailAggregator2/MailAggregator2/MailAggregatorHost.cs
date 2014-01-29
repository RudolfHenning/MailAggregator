using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MailAggregator
{
    public class MailAggregatorHost
    {
        private IMailAggregatorSource magsource = null;

        #region Constructor
        public MailAggregatorHost()
        {
            IsRunning = false;
            PollingFreq = 15000;
        }
        #endregion

        #region Events
        public event RaiseMessageDelegate AggregatorHostError;
        private void RaiseAggregatorHostError(string message)
        {
            if (AggregatorHostError != null)
                AggregatorHostError(message);
        }
        private void magsource_AggregatorError(string message)
        {
            RaiseAggregatorHostError(message);
        }
        #endregion

        #region Properties
        public bool IsRunning { get; set; }
        public int PollingFreq { get; set; }
        public string SMTPHost { get; set; }
        public bool SMTPUseDefaultCredentials { get; set; }
        public string SMTPDomain { get; set; }
        public string SMTPUserName { get; set; }
        public string SMTPPassword { get; set; }
        public string FromAddress { get; set; }
        public bool IsBodyHtml { get; set; }
        #endregion

        #region Initialize Host
        /// <summary>
        /// Initialize the aggregator host by loading the appropriate assembly as specified by the config
        /// </summary>
        /// <param name="config">Configuration string</param>
        public void InitializeHost(string config)
        {
        }
        #endregion

        #region Async refreshing
        public void StartPolling()
        {
            IsRunning = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(BackgroundPolling));
        }
        private void BackgroundPolling(object o)
        {
            while (IsRunning && magsource != null)
            {
                try
                {
                    RunAggregator();
                }
                catch (Exception ex)
                {
                    RaiseAggregatorHostError(ex.ToString());
                }
                BackgroundWaitIsPolling(PollingFreq);
            }
        }
        private void RunAggregator()
        {

        }
        private void BackgroundWaitIsPolling(int nextWaitInterval)
        {
            int waitTimeRemaining;
            int decrementBy = 2000;
            if (IsRunning)
            {
                try
                {
                    if ((nextWaitInterval <= decrementBy) && (nextWaitInterval > 0))
                    {
                        Thread.Sleep(nextWaitInterval);
                    }
                    else
                    {
                        waitTimeRemaining = nextWaitInterval;
                        while (IsRunning && (waitTimeRemaining > 0))
                        {
                            if (waitTimeRemaining <= decrementBy)
                            {
                                waitTimeRemaining = 0;
                            }
                            else
                            {
                                waitTimeRemaining -= decrementBy;
                            }
                            if (decrementBy > 0)
                            {
                                Thread.Sleep(decrementBy);
                            }
                        }
                    }
                }
                catch { }
            }
        }
        #endregion
    }
}
