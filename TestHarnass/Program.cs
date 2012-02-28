using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MailAggregator
{
    class Program
    {
        static void Main(string[] args)
        {
            MASShared masShared = new MASShared();
            foreach (var sourceConfig in Properties.Settings.Default.AggrSources)
            {
                string aggregatorType = masShared.GetAggregatorTypeFromConfig(sourceConfig);

                if (aggregatorType.Length > 0)
                {
                    IMailAggregatorSource magsource = masShared.GetMASAssembly(aggregatorType);
                    if (magsource == null)
                    {
                        Console.WriteLine("Mail aggregator with type '{0}' could not be found!", aggregatorType);
                    }
                    else
                    {
                        magsource.AggregatorError += new RaiseMessageDelegate(magsource_AggregatorError);
                        magsource.SetConfig(sourceConfig);
                        foreach (MailAggregatedMessage msg in magsource.GetMessages())
                        {
                            Console.WriteLine("To:" + msg.ToAddress);
                            Console.WriteLine("Subject:" + msg.Subject + (msg.MsgRepeatCounter > 1 ? " (" + msg.MsgRepeatCounter.ToString() + ")" : ""));
                            Console.WriteLine(msg.Body.Length);
                            Console.WriteLine(">---------------");
                        }
                    }
                }
            }

            //foreach (var sourceConfig in Properties.Settings.Default.AggrSources)
            //{
            //    IMailAggregatorSource magsource = null;
            //    if (sourceConfig.Contains("FlatFile"))
            //    {
            //        magsource = new MailAggregatorFFSource();
            //        magsource.AggregatorError += new RaiseMessageDelegate(magsource_AggregatorError);
            //    }

            //    if (magsource != null)
            //    {
            //        magsource.SetConfig(sourceConfig);
            //        foreach (MailAggregatedMessage msg in magsource.GetMessages())
            //        {
            //            Console.WriteLine("To:" + msg.ToAddress);
            //            Console.WriteLine("Subject:" + msg.Subject + (msg.MsgRepeatCounter > 1 ? " (" + msg.MsgRepeatCounter.ToString() + ")" : ""));
            //            Console.WriteLine(msg.Body.Length);
            //            Console.WriteLine(">---------------");
            //        }
            //    }
            //}
            Console.WriteLine("Done");
            Console.ReadKey();
        }

        static void magsource_AggregatorError(string message)
        {
            Console.WriteLine("Err:" + message);
        }
    }
}
