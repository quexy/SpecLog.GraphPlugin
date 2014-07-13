using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpecLog.GraphPlugin.Server.HtmlGraphGenerators
{
    public class PunchcardHtmlGraphGenerator : IHtmlGraphGenerator 
    {
        private readonly string outputFolder;
        private readonly CultureInfo formatter;
        private readonly IGraphDataRepositoryAccess repositoryAccess;
        public PunchcardHtmlGraphGenerator(IGraphDataRepositoryAccess repositoryAccess)
        {
            this.repositoryAccess = repositoryAccess;
            this.formatter = CultureInfo.GetCultureInfo("en-GB");

            outputFolder = GraphPlugin.GetExportPath(repositoryAccess.GetRepositoryId().ToString());
        }

        public void GenerateGraph()
        {
            var startDate = repositoryAccess.GetStartDate();
            var formattedDate = startDate.HasValue
                ? startDate.Value.ToString("dddd, d MMMM yyyy", formatter)
                : "&lt;no data yet&gt;";

            string content = ReadResource("punchcard.html")
                .Replace("{RepositoryName}", repositoryAccess.GetRepositoryName())
                .Replace("{EarliestDate}", formattedDate)
                .Replace("{PunchcardData}", FormatData())
                .Replace("{PunchcardScript}", FormatJavascript());

            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
            using (var stream = File.OpenWrite(Path.Combine(outputFolder, "PunchcardGraph.html")))
            {
                stream.SetLength(0);
                stream.Position = 0;
                var data = Encoding.UTF8.GetBytes(content);
                stream.Write(data, 0, data.Length);
            }
        }

        private string FormatData()
        {
            // create a complete empty set, and merge partial (count > zero) data into it, finally group by day
            var data = Enumerable.Range(0, 7).SelectMany(d => Enumerable.Range(0, 24).Select(h => Tuple.Create(d, h)))
                .Select(t => new PunchcardGraphData { Day = (DayOfWeek)t.Item1, Hour = t.Item2, Count = 0 })
                .Concat(repositoryAccess.GetPunchcardData()).GroupBy(d => Tuple.Create(d.Day, d.Hour), d => d.Count)
                .Select(g => new PunchcardGraphData { Day = g.Key.Item1, Hour = g.Key.Item2, Count = g.Sum() })
                .GroupBy(d => d.Day).OrderBy(g => g.Key);
            // format data for each day (order by hour) into a javascript-array
            var groups = data.Select(g => string.Join(",", g.OrderBy(d => d.Hour).Select(d => d.Count))).ToArray();
            // original order is Sunday first; move that to the end
            groups = groups.Skip(1).Concat(new[] { groups.First() }).ToArray();
            // put the (by day) sub-arrays together, forming a [7][24] matrix
            return string.Format("[[{0}]]", string.Join("],[", groups));
        }

        private string FormatJavascript()
        {
            var script = ReadResource("d3.punchcard.js");
    
            // remove multiline comments:
            script = Regex.Replace(script, "/\\*.*?\\*/", " ");
            // remove line comments:
            script = Regex.Replace(script, "//[^\r\n]*[\r\n]+", " ");
            // remove multiple whitespaces:
            script = Regex.Replace(script, "\\s+", " ");
            
            return script;
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
