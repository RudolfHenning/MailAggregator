using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MailAggregator
{
    public delegate void RaiseMessageDelegate(string message);

    public interface IMailAggregatorSource
    {
        event RaiseMessageDelegate AggregatorError;
        event RaiseMessageDelegate AggregatorWarning;

        string Name { get; set; }
        string LastError { get; set; }
        string MessageBodyTemplate { get; set; }
        string MessageSeparatorTemplate { get; set; }

        string GetConfigIdentifierType();
        bool SetConfig(string config);
        List<MailAggregatedMessage> GetMessages();
    }
}
