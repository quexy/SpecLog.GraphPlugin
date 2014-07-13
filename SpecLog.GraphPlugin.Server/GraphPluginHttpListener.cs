using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TechTalk.SpecLog.Logging;
using TechTalk.SpecLog.Service.Hosting.WcfHosting;

namespace SpecLog.GraphPlugin.Server
{
    public interface IHttpListenerConfiguration
    {
        string RepositoryId { get; }
        bool IsRestricted { get; }
        string UserName { get; }
        string Password { get; }
    }

    public interface IGraphPluginHttpListener : IDisposable
    {
        void Start();
        void Stop();
    }

    public class GraphPluginHttpListener : IGraphPluginHttpListener
    {
        public static readonly IEnumerable<KeyValuePair<string, string>> SupportedGraphTypes = new Dictionary<string, string>
        {
            { "Punchcard", "GitHub-style punchcard graph of repository activity by time and day" },
            { "Frequency" , "Simple line chart of the last 30 days' repository activity" }
        };

        private readonly ILogger logger;
        private readonly IHttpListenerConfiguration configuration;

        private readonly Regex resourceRegex;
        private readonly HttpListener listener;
        public GraphPluginHttpListener(ILogger logger, IServiceHostConfiguration hostConfiguration, IHttpListenerConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;

            resourceRegex = new Regex(string.Format("^/RepositoryActivity/{0}/(?<resource>.*)$", Regex.Escape(configuration.RepositoryId)),
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var baseUrl = string.Format("{1}://+:{2}/RepositoryActivity/{0}/", configuration.RepositoryId,
                hostConfiguration.IsSecureHost ? "https" : "http", hostConfiguration.CommunicationPort);

            listener = new HttpListener
            {
                Prefixes = { baseUrl },
                AuthenticationSchemes = configuration.IsRestricted
                    ? AuthenticationSchemes.Basic
                    : AuthenticationSchemes.Anonymous
            };
        }

        public void Start()
        {
            listener.Start();
            QueueRequestHandler();
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void Dispose()
        {
            listener.Close();
        }

        void QueueRequestHandler()
        {
            try
            {
                if (!listener.IsListening) return;
                var result = listener.BeginGetContext(ServeRequest, null);
                if (result.IsCompleted) ServeRequest(result);
            }
            catch (Exception ex)
            {
                logger.Log(TraceEventType.Warning, "Failed to queue request handler: {0}", ex);
            }
        }

        void ServeRequest(IAsyncResult result)
        {
            //requeue to keep on listening
            QueueRequestHandler();

            try
            {
                if (!listener.IsListening) return;
                HandleRequest(listener.EndGetContext(result));
            }
            catch (Exception ex)
            {
                logger.Log(TraceEventType.Warning, "Could not handle request: {0}", ex);
            }
        }

        void HandleRequest(HttpListenerContext context)
        {
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "text/html";

            if (!Authenticate(context))
            {
                context.Response.StatusCode = 401;
                GetUnauthorized().CopyTo(context.Response.OutputStream);
            }
            else
            {
                string resource = resourceRegex.Match(context.Request.Url.AbsolutePath).Groups["resource"].Value;
                if (string.IsNullOrWhiteSpace(resource))
                {
                    context.Response.StatusCode = 200;
                    GetIndex(context.Request.Url).CopyTo(context.Response.OutputStream);
                }
                else
                {
                    var recognised = SupportedGraphTypes.Select(e => e.Key).Any(t => string.Equals(resource, t, StringComparison.InvariantCultureIgnoreCase));
                    if (!recognised)
                    {
                        context.Response.StatusCode = 404;
                        GetNotFound(resource).CopyTo(context.Response.OutputStream);
                    }
                    else
                    {
                        try
                        {
                            context.Response.StatusCode = 200;
                            var exportPath = Path.Combine(GraphPlugin.GetExportPath(configuration.RepositoryId), resource + "Graph.html");
                            File.Open(exportPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite).CopyTo(context.Response.OutputStream);
                        }
                        catch (Exception ex)
                        {
                            logger.Log(TraceEventType.Warning, string.Format("Could not read HTML graph: {0}", ex));
                            context.Response.StatusCode = 500;
                            GetUnavailable(resource).CopyTo(context.Response.OutputStream);
                        }
                    }
                }
            }

            context.Response.OutputStream.Close();
            context.Response.Close();
        }

        private bool Authenticate(HttpListenerContext context)
        {
            if (!configuration.IsRestricted) return true;

            if (context.User == null) return false;

            var identity = context.User.Identity as HttpListenerBasicIdentity;
            if (identity == null) return false;

            if (!string.Equals(configuration.UserName, identity.Name, StringComparison.InvariantCultureIgnoreCase))
                return false;
            if (!string.Equals(configuration.Password, identity.Password, StringComparison.InvariantCulture))
                return false;

            return true;
        }

        private Stream GetIndex(Uri requestUri)
        {
            var baseUrl = requestUri.ToString().TrimEnd('/');
            var lineTemplate = "<li style=\"margin-left:2em;\"><a href=\"{base}/{type}\" title=\"{details}\">{type}</a> &mdash; {details}</li>";
            Func<string, string, string> createLine = (type, details) => lineTemplate.Replace("{base}", baseUrl).Replace("{type}", type).Replace("{details}", details);
            var content = "<html><head><title>SpecLog Graphs</title></head><body><div style=\"margin:5em; font-family: Verdana, Arial, sans-serif\">"
                + "<div style=\"font-size:1.5em; margin: 0;\">Available repository usage graphs are:</div><ul style=\"margin-top:1em; line-height:1.5em\">"
                + SupportedGraphTypes.Select(t => createLine(t.Key, t.Value)).Aggregate("", (c, p) => string.Concat(c, p))
                + "</ul></div></body></html>";
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private Stream GetUnauthorized()
        {
            var content = "<html><head><title>Unauthorized</title></head><body><div style=\"margin-top:5em; font-size:3em; text-align:center;\">"
                + "Authentication failed<br/>You are not authorized to access the usege graphs for this project</body</html>";
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        private Stream GetNotFound(string graphType)
        {
            Trace.TraceError(string.Format("Graph '{0}' is not supported", graphType));
            var error = "<html><head><title>Unavailable</title></head><body><div style=\"margin-top:5em; font-size:3em; text-align:center;\">"
                + "The graph type <em>" + WebUtility.HtmlEncode(graphType) + "</em> is not supported by the plugin.</div></body></html>";
            return new MemoryStream(Encoding.UTF8.GetBytes(error));
        }

        private Stream GetUnavailable(string graphType)
        {
            var error = "<html><head><title>Unavailable</title></head><body><div style=\"margin-top:5em; font-size:3em; text-align:center;\">"
                + "The " + graphType + " graph is unavailable at the moment.<br/>Please try again later.</div></body></html>";
            return new MemoryStream(Encoding.UTF8.GetBytes(error));
        }
    }
}
