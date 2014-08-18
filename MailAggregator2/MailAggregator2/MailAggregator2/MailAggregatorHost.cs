using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace MailAggregator
{
    public class MailAggregatorHost
    {
        private IMailAggregatorSource magsource = null;
        private Mutex mailAggregatorSMTPWriteMutex = new Mutex();

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
            string lastStep = "";
            if (magsource != null)
            {
                try
                {
                    mailAggregatorSMTPWriteMutex.WaitOne();
                    lastStep = "Reading messages";
                    List<MailAggregatedMessage> msgs = magsource.GetMessages();
                    if (msgs.Count > 0)
                    {
                        lastStep = "Initializing SMTP client";
                        using (SmtpClient smtpClient = new SmtpClient())
                        {
                            smtpClient.Host = SMTPHost;
                            smtpClient.UseDefaultCredentials = SMTPUseDefaultCredentials;
                            if (!SMTPUseDefaultCredentials)
                            {
                                lastStep = "Setting up non default credentials";
                                System.Net.NetworkCredential cr = new System.Net.NetworkCredential();
                                cr.Domain = SMTPDomain;
                                cr.UserName = SMTPUserName;
                                cr.Password = SMTPPassword;
                                smtpClient.Credentials = cr;
                            }
                            lastStep = "Looping through messages";
                            foreach (MailAggregatedMessage msg in msgs)
                            {
                                try
                                {
                                    MailMessage mailMessage = new MailMessage(FromAddress, msg.ToAddress);

                                    mailMessage.Body = msg.Body;
                                    mailMessage.IsBodyHtml = IsBodyHtml;
                                    mailMessage.Subject = msg.Subject;
                                    lastStep = "Sending SMTP mail";
                                    smtpClient.Send(mailMessage);
                                }
                                catch (Exception ex)
                                {
                                    RaiseAggregatorHostError(string.Format("An error occured trying to process the message '{0}'\r\n{1}", msg.Subject, ex.ToString()));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RaiseAggregatorHostError(string.Format("An error occured while sending mail.\r\n{0}\r\n", lastStep, ex.ToString()));
                }
                finally
                {
                    mailAggregatorSMTPWriteMutex.ReleaseMutex();
                }
            }
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
