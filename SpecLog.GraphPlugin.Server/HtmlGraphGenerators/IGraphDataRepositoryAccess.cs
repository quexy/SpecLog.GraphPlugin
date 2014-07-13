using System;
using System.Collections.Generic;

namespace SpecLog.GraphPlugin.Server.HtmlGraphGenerators
{
    public interface IGraphDataRepositoryAccess
    {
        DateTime? GetStartDate();
        Guid GetRepositoryId();
        string GetRepositoryName();
        
        IEnumerable<PunchcardGraphData> GetPunchcardData();
        IEnumerable<FrequencyGraphData> GetFrequencyData(DateTime since);
    }

    public class PunchcardGraphData
    {
        public DayOfWeek Day { get; set; }
        public int Hour { get; set; }
        public int Count { get; set; }
    }

    public class FrequencyGraphData
    {
        public DateTime Date { get; set; }
        public int UserCount { get; set; }
        public int CommandCount { get; set; }
    }
}
