﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.239
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MailAggregator.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"<?xml version=""1.0"" encoding=""utf-16""?>
<ArrayOfString xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <string>&lt;source type=""FlatFile"" name=""Test files"" inputFilePath=""C:\temp"" inputFileMask=""*.txt"" maxMsgSize=""1048576"" aggregatedSubject=""Aggregated test message"" aggregateBySubject=""false"" appendDoneFiles=""True"" defaultToAddress=""someaddress@somedomain.com"" appendDoneFileMaxSizeKB=""10""/&gt;</string>
</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection AggrSources {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["AggrSources"]));
            }
            set {
                this["AggrSources"] = value;
            }
        }
    }
}
