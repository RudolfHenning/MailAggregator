using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MailAggregator
{
    public class MailAggregatedMessage
    {
        public string ToAddress { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public int MsgRepeatCounter { get; set; }
        public int MsgAggrCounter { get; set; }
    }
}
