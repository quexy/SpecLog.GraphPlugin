using System;

namespace SpecLog.GraphPlugin.Client
{
    [Serializable]
    public class GraphPluginConfiguration
    {
        public const string CredentialsPasswordKey = "CredentialsPassword";
        public const string GenerateGraphsVerb = "GenerateGraphs";

        public GraphPluginConfiguration()
        {
            GraphRefreshPeriod = 15;
        }

        public int GraphRefreshPeriod { get; set; }

        public bool IsRestricted { get; set; }

        public string UserName { get; set; }
    }
}
