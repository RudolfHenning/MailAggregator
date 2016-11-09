using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MailAggregator
{
    public class MailAggregatorFFSource : IMailAggregatorSource
    {
        #region Private vars
        private string inputFilePath = "";
        private string inputFileMask = "*.txt";
        private int maxMsgSize = 1048576;
        private string aggregatedSubject = "No subject";
        private bool aggregateBySubject = false;
        private bool removeFilesOnDone = false;
        private bool appendDoneFiles = true;
        private string defaultToAddress = "";
        private int appendDoneFileMaxSizeKB = 1024;
        #endregion

        #region Events
        public event RaiseMessageDelegate AggregatorError;
        private void RaiseAggregatorError(string message)
        {
            if (AggregatorError != null)
                AggregatorError(message);
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
            return "FlatFile";
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
                        configXml.LoadXml(Properties.Resources.MailAggregatorFlatFileDefaultConfig);
                    }
                }
                else
                    configXml.LoadXml(Properties.Resources.MailAggregatorFlatFileDefaultConfig);
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
                if (inputFilePath.Length > 0 && System.IO.Directory.Exists(inputFilePath))
                {
                    foreach (string fileName in System.IO.Directory.GetFiles(inputFilePath, inputFileMask))
                    {
                        string[] lines = "".Split();
                        try
                        {
                            lines = System.IO.File.ReadAllLines(fileName);
                        }
                        catch (Exception linesEx)
                        {
                            if (linesEx.Message.Contains("The process cannot access the file"))
                            {
                                lines = "".Split();
                            }
                            else
                                throw;
                        }
                        if (lines.Length == 0)
                            RaiseAggregatorError(string.Format("The file {0} is empty!", fileName));
                        else
                        {
                            string msgToAddress = GetMsgToAddressFromFile(lines);
                            string subject = GetMsgSubjectFromFile(lines);
                            string currentSubject = subject;
                            string body = GetMsgBodyFromFile(lines);

                            if (aggregatedSubject.Length > 0 && !aggregateBySubject)
                                currentSubject = aggregatedSubject;

                            if ((from msg in msgs
                                 where msg.ToAddress.ToUpper() == msgToAddress.ToUpper() &&
                                    (!aggregateBySubject || msg.Subject.ToUpper() == subject.ToUpper())
                                 select msg).Count() == 0)
                            {
                                //add new instance
                                MailAggregatedMessage newMsg = new MailAggregatedMessage()
                                {
                                    ToAddress = msgToAddress,
                                    Subject = currentSubject,
                                    Body = MessageFromTemplate("1", subject, body), // "Message 1\r\nSubject:" + subject + "\r\n" + body,
                                    MsgRepeatCounter = 1,
                                    MsgAggrCounter = 1
                                };
                                msgs.Add(newMsg);
                            }
                            else
                            {
                                MailAggregatedMessage lastMsg = (from msg in msgs
                                                                 where msg.ToAddress.ToUpper() == msgToAddress.ToUpper() &&
                                                             (!aggregateBySubject || msg.Subject.ToUpper() == subject.ToUpper())
                                                                 orderby msg.MsgRepeatCounter descending
                                                                 select msg).First();
                                if ((lastMsg.Body.Length + MessageSeparatorTemplate.Length + "Message X\r\nSubject:".Length + subject.Length + body.Length) < maxMsgSize)
                                {
                                    lastMsg.MsgAggrCounter++;
                                    lastMsg.Body += MessageFromTemplate(lastMsg.MsgAggrCounter.ToString(), subject, body);
                                    //    "\r\nMessage " + lastMsg.MsgAggrCounter.ToString() + "\r\nSubject:" + subject + "\r\n" + body;
                                }
                                else
                                {
                                    int msgCounter = lastMsg.MsgRepeatCounter + 1;
                                    MailAggregatedMessage newMsg = new MailAggregatedMessage()
                                    {
                                        ToAddress = msgToAddress,
                                        Subject = currentSubject,
                                        Body = MessageFromTemplate("1", subject, body), // "Message 1\r\nSubject:" + subject + "\r\n" + body,
                                        MsgRepeatCounter = msgCounter,
                                        MsgAggrCounter = 1
                                    };
                                    msgs.Add(newMsg);
                                }                                
                            }                            
                        }
                        try
                        {
                            if (removeFilesOnDone)
                                System.IO.File.Delete(fileName);
                            else if (appendDoneFiles)
                            {
                                if (!System.IO.File.Exists(fileName + ".done")) //Does not exist yet
                                    System.IO.File.Move(fileName, fileName + ".done");
                                else
                                {
                                    if (appendDoneFileMaxSizeKB > 0) //Max size for append file specified
                                    {
                                        System.IO.FileInfo fi = new System.IO.FileInfo(fileName + ".done");
                                        if ((fi.Length / 1024) > appendDoneFileMaxSizeKB) //newest append file over size limit
                                        {
                                            RenameExistingAppendFiles(fileName, 0); //increase counter and rename all previous append files
                                            System.IO.File.Move(fileName, fileName + ".done");
                                        }
                                        else //newest append file still under limit
                                            System.IO.File.AppendAllText(fileName + ".done", "\r\n" + System.IO.File.ReadAllText(fileName));
                                    }
                                    else //No max size for append file specified
                                    {
                                        System.IO.File.AppendAllText(fileName + ".done", "\r\n" + System.IO.File.ReadAllText(fileName));
                                    }
                                    System.IO.File.Delete(fileName);
                                }
                            }
                            else //try to rename
                            {
                                if (System.IO.File.Exists(fileName + ".done")) //try to remove it
                                    System.IO.File.Delete(fileName + ".done");
                                System.IO.File.Move(fileName, fileName + ".done");
                            }
                        }
                        catch (Exception renex)
                        {
                            RaiseAggregatorError(renex.ToString());
                        }
                    }
                }
                else
                    throw new Exception(string.Format("Input directory not found or invalid! ({0})", inputFilePath));
            }
            catch (Exception ex)
            {
                RaiseAggregatorError(ex.ToString());
            }

            return msgs;
        }
        #endregion

        #region Private methods
        private void ReadConfiguration(XmlDocument config)
        {
            XmlElement root = config.DocumentElement;
            XmlNode sourceNode;
            if (root.Name == "source")
                sourceNode = root;
            else
                sourceNode = root.SelectSingleNode("source");
            Name = sourceNode.ReadXmlElementAttr("name", "Mail aggregator for Flat files");
            inputFilePath = sourceNode.ReadXmlElementAttr("inputFilePath", "");
            inputFileMask = sourceNode.ReadXmlElementAttr("inputFileMask", "*.txt");
            maxMsgSize = int.Parse(sourceNode.ReadXmlElementAttr("maxMsgSize", "1048576"));
            aggregatedSubject = sourceNode.ReadXmlElementAttr("aggregatedSubject", "");
            aggregateBySubject = bool.Parse(sourceNode.ReadXmlElementAttr("aggregateBySubject", "False"));
            removeFilesOnDone = bool.Parse(sourceNode.ReadXmlElementAttr("removeFilesOnDone", "False"));
            defaultToAddress = sourceNode.ReadXmlElementAttr("defaultToAddress", "");
            appendDoneFileMaxSizeKB = int.Parse(sourceNode.ReadXmlElementAttr("appendDoneFileMaxSizeKB", "1024"));
        }
        private string GetMsgToAddressFromFile(string[] lines)
        {           
            foreach (string line in lines)
            {
                if (line.ToUpper().StartsWith("TO:"))
                {
                    return line.Substring(3);
                }
            }
            if (defaultToAddress.Length > 0)
                return defaultToAddress;
            else
                return lines[0];
        }
        private string GetMsgSubjectFromFile(string[] lines)
        {
            foreach (string line in lines)
            {
                if (line.ToUpper().StartsWith("SUBJECT:"))
                {
                    return line.Substring(8);
                }
            }
            return aggregatedSubject.Length == 0 ? "Aggregated message" : aggregatedSubject;
        }
        private string GetMsgBodyFromFile(string[] lines)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string line in lines)
            {
                if (!line.ToUpper().StartsWith("TO:") &&
                    !line.ToUpper().StartsWith("SUBJECT:"))
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }
        private string MessageFromTemplate(string msgNumber, string subject, string body)
        {
            return MessageBodyTemplate.Replace("%MsgNo%", msgNumber).Replace("%Subject%", subject).Replace("%Body%", body) + "\r\n" + MessageSeparatorTemplate + "\r\n";
        }
        private void RenameExistingAppendFiles(string fileName, int startCounter)
        {
            if (System.IO.File.Exists(fileName + (startCounter + 1).ToString() + ".done"))
                RenameExistingAppendFiles(fileName, startCounter + 1);

            if (startCounter > 0)
                System.IO.File.Move(fileName + startCounter.ToString() + ".done", fileName + (startCounter + 1).ToString() + ".done");
            else
                System.IO.File.Move(fileName + ".done", fileName + (startCounter + 1).ToString() + ".done");
        }
        #endregion
        
    }
}
