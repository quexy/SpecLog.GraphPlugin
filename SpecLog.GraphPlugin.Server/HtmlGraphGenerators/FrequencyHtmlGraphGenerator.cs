using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TechTalk.SpecLog.Entities;

namespace SpecLog.GraphPlugin.Server.HtmlGraphGenerators
{
    public class FrequencyHtmlGraphGenerator : IHtmlGraphGenerator
    {
        private readonly string outputFolder;
        private readonly CultureInfo formatter;
        private readonly ITimeService timeService;
        private readonly IGraphDataRepositoryAccess repositoryAccess;
        public FrequencyHtmlGraphGenerator(ITimeService timeService, IGraphDataRepositoryAccess repositoryAccess)
        {
            this.timeService = timeService;
            this.repositoryAccess = repositoryAccess;
            this.formatter = CultureInfo.GetCultureInfo("en-GB");

            outputFolder = GraphPlugin.GetExportPath(repositoryAccess.GetRepositoryId().ToString());
        }

        public void GenerateGraph()
        {
            var startDate = timeService.CurrentTime.Date.AddDays(-29);
            var formattedDate = startDate.ToString("dddd, d MMMM yyyy", formatter);

            string content = ReadResource("frequency.html")
                .Replace("{RepositoryName}", repositoryAccess.GetRepositoryName())
                .Replace("{EarliestDate}", formattedDate)
                .Replace("{LineChartData}", FormatData(startDate));

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
            using (var stream = File.OpenWrite(Path.Combine(outputFolder, "FrequencyGraph.html")))
            {
                stream.SetLength(0);
                stream.Position = 0;
                var data = Encoding.UTF8.GetBytes(content);
                stream.Write(data, 0, data.Length);
            }
        }

        private string FormatData(DateTime startDate)
        {
            // create a complete empty set, and merge partial (count > zero) data into it
            var data = Enumerable.Range(0, 30).Select(n => new FrequencyGraphData { Date = startDate.AddDays(n) })
                .Concat(repositoryAccess.GetFrequencyData(startDate)).GroupBy(d => d.Date)
                .Select(g => g.Skip(1).Aggregate(g.First(), (a, b) => { a.UserCount += b.UserCount; a.CommandCount += b.CommandCount; return a; }))
                .OrderBy(d => d.Date).ToArray(); // finally order it by date (just for making sure)
            // format data lines for each day into a javascript-array
            var lines = data.Select(d => string.Format("['{0:dd-MM-yyyy}',{1},{2}]", d.Date, d.UserCount, d.CommandCount)).ToArray();
            // put the header and data-arrays together, forming a [31][3] matrix
            return string.Format("[{0}]", string.Join(",", new[] { "['Date', 'Users', 'Commands']" }.Concat(lines)));
        }

        private string ReadResource(string name)
        {
            var resource = string.Format("{0}.{1}.{2}", GetType().Namespace, "Resources", name);
            using (var stream = GetType().Assembly.GetManifestResourceStream(resource))
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return Encoding.UTF8.GetString(data);
            }
        }
    }
}
