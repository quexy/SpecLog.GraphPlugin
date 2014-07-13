using System;
using System.Xml.Serialization;
using TechTalk.SpecLog.Common;

namespace SpecLog.GraphPlugin.Server
{
    public class GraphPluginConfiguration : IPeriodicActivityConfiguration, IHttpListenerConfiguration
    {
        public const string CredentialsPasswordKey = "CredentialsPassword";
        public const string GenerateGraphsVerb = "GenerateGraphs";

        public int GraphRefreshPeriod { get; set; }

        public bool IsRestricted { get; set; }

        public string UserName { get; set; }

        [XmlIgnore]
        public string Password { get; set; }

        [XmlIgnore]
        public string RepositoryId { get; set; }

        [XmlIgnore]
        public TimeSpan TriggerInterval
        {
            get { return TimeSpan.FromMinutes(GraphRefreshPeriod); }
        }
    }
}
