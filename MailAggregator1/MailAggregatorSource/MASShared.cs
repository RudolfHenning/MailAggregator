using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Xml;

namespace MailAggregator
{
    public class MASShared
    {
        /// <summary>
        /// Get the aggregator type name from the config
        /// </summary>
        /// <param name="config">Configuration string</param>
        /// <returns>Type name of aggregator</returns>
        public string GetAggregatorTypeFromConfig(string config)
        {
            string aggregatorType = "";
            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.LoadXml(config);
            XmlElement root = xmlConfig.DocumentElement;
            XmlNode sourceNode;
            if (root.Name == "source")
                sourceNode = root;
            else
                sourceNode = root.SelectSingleNode("source");
            aggregatorType =  sourceNode.ReadXmlElementAttr("type", "");
            //predefined types
            if (aggregatorType == "FlatFile")
                aggregatorType = "MailAggregatorFFSource";
            else if (aggregatorType == "SqlServer")
                aggregatorType = "MailAggregatorSqlSource";
            return aggregatorType;
        }
        /// <summary>
        /// Look for the aggregator assembly in the local directory
        /// </summary>
        /// <param name="aggregatorName">Name (class) of the aggregator</param>
        /// <returns>An IMailAggregatorSource aggregator instance</returns>
        public IMailAggregatorSource GetMASAssembly(string aggregatorName)
        {
            string agentsAssemblyPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            StringBuilder sbExceptions = new StringBuilder();
            try
            {
                foreach (string assemblyPath in System.IO.Directory.GetFiles(agentsAssemblyPath, "*.dll"))
                {
                    try
                    {
                        foreach (string className in LoadAggregatorClasses(assemblyPath))
                        {
                            string assemblyAggregatorName = className.Replace("MailAggregator.", "");
                            if (assemblyAggregatorName.ToUpper().StartsWith(aggregatorName.ToUpper()))
                            {
                                Assembly aggregatorAssembly = Assembly.LoadFile(assemblyPath);
                                IMailAggregatorSource newaggregator = (IMailAggregatorSource)aggregatorAssembly.CreateInstance(className);
                                return newaggregator;
                            }
                        }
                    }
                    catch (System.Reflection.ReflectionTypeLoadException rex)
                    {
                        foreach (Exception lex in rex.LoaderExceptions)
                        {
                            sbExceptions.AppendLine(string.Format("Error in assembly '{0}' - {1}", assemblyPath, lex.Message));
                        }
                    }
                    catch (Exception innerEx)
                    {
                        throw new Exception(string.Format("Error loading {0}.\r\n{1}", assemblyPath, innerEx.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("An error occured loading aggregators from {0}.\r\n{1}", agentsAssemblyPath, ex.Message));
            }
            if (sbExceptions.Length > 0)
            {
                throw new Exception(string.Format("There were errors loading the assembly(s)\r\n{0}", sbExceptions.ToString()));
            }
            return null;
        }
        /// <summary>
        /// Get all aggregators from assembly (that inherits IMailAggregatorSource)
        /// </summary>
        /// <param name="assemblyFilePath"></param>
        /// <returns></returns>
        private IEnumerable LoadAggregatorClasses(string assemblyFilePath)
        {
            Assembly quickAsshehe = Assembly.LoadFile(assemblyFilePath);
            Type[] types = quickAsshehe.GetTypes();
            foreach (Type type in types)
            {
                if (!type.IsInterface && !type.IsAbstract)
                {
                    foreach (Type interfaceType in type.GetInterfaces())
                    {
                        if (interfaceType.FullName == "MailAggregator.IMailAggregatorSource")
                            yield return type.FullName;
                    }
                }
            }
        } 
    }
}
