using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailAggregator;
using System.Xml;
using System.Data;

namespace MailAggregator
{
    public class MailAggregatorSqlSource : IMailAggregatorSource
    {
        #region Private vars
        private string sqlServerName = "";
        private string database = "";
        private bool integratedSec = true;
        private string userName = "";
        private string password = "";
        private string aggregatedSubject = "";
        private bool aggregateBySubject = false;
        private string defaultToAddress = "";
        private bool useSPs = true;
        private string selectMsgs = "";
        private string markMessageDone = "";
        private int maxMsgSize = 1048576;
        #endregion

        #region Events
        public event RaiseMessageDelegate AggregatorError;
        private void RaiseAggregatorError(string message)
        {
            AggregatorError?.Invoke(message);
        }
        public event RaiseMessageDelegate AggregatorWarning;
        private void RaiseAggregatorWarning(string message)
        {
            AggregatorWarning?.Invoke(message);
        }
        #endregion

        #region Properties
        public string Name { get; set; }
        public string LastError { get; set; }
        public string MessageBodyTemplate { get; set; }
        public string MessageSeparatorTemplate { get; set; } 
        #endregion

        #region IMailAggregatorSource Members
        public string GetConfigIdentifierType()
        {
            return "SqlServer";
        }
        public bool SetConfig(string config)
        {
            bool success = false;
            try
            {
                if (MessageBodyTemplate == null || MessageBodyTemplate.Length == 0)
                {
                    MessageBodyTemplate = Properties.Resources.MessageBodyTemplate;
                }
                if (MessageSeparatorTemplate == null || MessageSeparatorTemplate.Length == 0)
                {
                    MessageSeparatorTemplate = new string('-', 20);
                }
                XmlDocument configXml = new XmlDocument();
                if (config.Length > 0)
                {
                    try
                    {
                        configXml.LoadXml(config);
                    }
                    catch
                    {
                        configXml.LoadXml(Properties.Resources.MailAggregatorSqlDefaultConfig);
                    }
                }
                else
                    configXml.LoadXml(Properties.Resources.MailAggregatorSqlDefaultConfig);
                ReadConfiguration(configXml);
                success = true;
            }
            catch (Exception ex)
            {
                LastError = ex.ToString();
                success = false;
            }
            return success;
        }
        public List<MailAggregatedMessage> GetMessages()
        {
            List<MailAggregatedMessage> msgs = new List<MailAggregatedMessage>();
            try
            {
                System.Data.SqlClient.SqlConnectionStringBuilder scsb = new System.Data.SqlClient.SqlConnectionStringBuilder();
                scsb.DataSource = sqlServerName;
                scsb.InitialCatalog = database;
                scsb.IntegratedSecurity = integratedSec;
                if (!integratedSec)
                {
                    scsb.UserID = userName;
                    scsb.Password = password;
                }

                using (System.Data.SqlClient.SqlConnection conn = new System.Data.SqlClient.SqlConnection(scsb.ConnectionString))
                {
                    conn.Open();
                    using (System.Data.SqlClient.SqlCommand cmnd = new System.Data.SqlClient.SqlCommand(selectMsgs, conn))
                    {
                        cmnd.CommandType = useSPs ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
                        DataSet ds = new DataSet();
                        using (System.Data.SqlClient.SqlDataAdapter da = new System.Data.SqlClient.SqlDataAdapter(cmnd))
                        {
                            da.Fill(ds);
                            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                            {
                                foreach (DataRow r in ds.Tables[0].Rows)
                                {
                                    int msgId = (int)r["Id"];
                                    string toAddress = r["ToAddress"].ToString();
                                    if (toAddress.Length == 0)
                                        toAddress = defaultToAddress;
                                    string subject =  r["Subject"].ToString();
                                    string body =  r["Body"].ToString();
                                    AddOrAppendToMessages(msgs, toAddress, subject, body);

                                    using (System.Data.SqlClient.SqlCommand markCmnd = new System.Data.SqlClient.SqlCommand(markMessageDone, conn))
                                    {
                                        markCmnd.CommandType = useSPs ? System.Data.CommandType.StoredProcedure : System.Data.CommandType.Text;
                                        markCmnd.Parameters.Add(new System.Data.SqlClient.SqlParameter("@Id", msgId));
                                        markCmnd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseAggregatorError(ex.ToString());
            }

            return msgs;
        }
        #endregion

        #region Private methods
        private void AddOrAppendToMessages(List<MailAggregatedMessage> msgs, string toAddress, string subject, string body)
        {
            string currentSubject = subject;
            if (aggregatedSubject.Length > 0 && !aggregateBySubject)
                currentSubject = aggregatedSubject;

            if ((from msg in msgs
                 where msg.ToAddress.ToUpper() == toAddress.ToUpper() &&
                    (!aggregateBySubject || msg.Subject.ToUpper() == subject.ToUpper())
                 select msg).Count() == 0)
            {
                //add new instance
                MailAggregatedMessage newMsg = new MailAggregatedMessage()
                {
                    ToAddress = toAddress,
                    Subject = currentSubject,
                    Body = MessageFromTemplate("1", subject, body),
                    MsgRepeatCounter = 1,
                    MsgAggrCounter = 1
                };
                msgs.Add(newMsg);
            }
            else
            {
                MailAggregatedMessage lastMsg = (from msg in msgs
                                                 where msg.ToAddress.ToUpper() == toAddress.ToUpper() &&
                                             (!aggregateBySubject || msg.Subject.ToUpper() == subject.ToUpper())
                                                 orderby msg.MsgRepeatCounter descending
                                                 select msg).First();
                if ((lastMsg.Body.Length + MessageSeparatorTemplate.Length + "Message X\r\nSubject:".Length + subject.Length + body.Length) < maxMsgSize)
                {
                    lastMsg.MsgAggrCounter++;
                    lastMsg.Body += MessageFromTemplate(lastMsg.MsgAggrCounter.ToString(), subject, body);
                }
                else
                {
                    int msgCounter = lastMsg.MsgRepeatCounter + 1;
                    MailAggregatedMessage newMsg = new MailAggregatedMessage()
                    {
                        ToAddress = toAddress,
                        Subject = currentSubject,
                        Body = MessageFromTemplate("1", subject, body),
                        MsgRepeatCounter = msgCounter,
                        MsgAggrCounter = 1
                    };
                    msgs.Add(newMsg);
                }
            }
        }
        private void ReadConfiguration(XmlDocument config)
        {
            XmlElement root = config.DocumentElement;
            XmlNode sourceNode;
            if (root.Name == "source")
                sourceNode = root;
            else
                sourceNode = root.SelectSingleNode("source");
            Name = sourceNode.ReadXmlElementAttr("name", "Mail aggregator for Sql server");
            sqlServerName = sourceNode.ReadXmlElementAttr("sqlServerName", "");
            database = sourceNode.ReadXmlElementAttr("database", "*.txt");
            integratedSec = bool.Parse(sourceNode.ReadXmlElementAttr("integratedSec", "True"));
            userName = sourceNode.ReadXmlElementAttr("userName", "");
            password = sourceNode.ReadXmlElementAttr("password", "");
            aggregatedSubject = sourceNode.ReadXmlElementAttr("aggregatedSubject", "");
            aggregateBySubject = bool.Parse(sourceNode.ReadXmlElementAttr("aggregateBySubject", "False"));
            defaultToAddress = sourceNode.ReadXmlElementAttr("defaultToAddress", "");
            useSPs = bool.Parse(sourceNode.ReadXmlElementAttr("useSPs", "True"));
            selectMsgs = sourceNode.ReadXmlElementAttr("selectMsgs", "");
            markMessageDone = sourceNode.ReadXmlElementAttr("markMessageDone", "");
            maxMsgSize = int.Parse(sourceNode.ReadXmlElementAttr("maxMsgSize", "1048576"));

            if (sqlServerName.Length == 0)
                throw new Exception("Sql server name not specified!");
            if (database.Length == 0)
                throw new Exception("Database not specified!");
            if (!integratedSec && userName.Length == 0)
                throw new Exception("User name not specified!");
            if (selectMsgs.Length == 0)
                throw new Exception("Select messages query not specified!");
            if (markMessageDone.Length == 0)
                throw new Exception("Mark messages query not specified!");
        }
        private string MessageFromTemplate(string msgNumber, string subject, string body)
        {
            return MessageBodyTemplate.Replace("%MsgNo%", msgNumber).Replace("%Subject%", subject).Replace("%Body%", body) + "\r\n" + MessageSeparatorTemplate + "\r\n";
        } 
        #endregion
    }
}
