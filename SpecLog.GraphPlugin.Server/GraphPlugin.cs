using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Practices.Unity;
using TechTalk.SpecLog.Common;
using TechTalk.SpecLog.Server.Services.PluginInfrastructure;

namespace SpecLog.GraphPlugin.Server
{
    [Plugin(GraphPlugin.PluginName, ContainerSetupType = typeof(GraphPluginContainerSetup))]
    public class GraphPlugin : ServerPlugin
    {
        public const string PluginName = "SpecLog.GraphPlugin";

        private readonly IGraphPluginContainerSetup containerSetup;
        private readonly IGraphPluginRepositoryAccess repositoryAccess;
        public GraphPlugin(IGraphPluginContainerSetup containerSetup, IGraphPluginRepositoryAccess repositoryAccess)
        {
            this.containerSetup = containerSetup;
            this.repositoryAccess = repositoryAccess;
        }

        public override IEnumerable<IPeriodicActivity> ActiveSynchronizers
        {
            get { return new IPeriodicActivity[] { generatorActivity }; }
        }

        private IUnityContainer container;
        private IGraphPluginHttpListener httpListener;
        private IGraphGenerationActivity generatorActivity;
        public override void OnStart()
        {
            repositoryAccess.EnsureDatabase();

            var configuration = base.GetConfiguration<GraphPluginConfiguration>();
            configuration.Password = (!configuration.IsRestricted) ? null :
                GetSecuredConfiguration()[GraphPluginConfiguration.CredentialsPasswordKey];
            configuration.RepositoryId = repositoryAccess.GetRepositoryId().ToString();

            container = containerSetup.ConfigureContainer(configuration);
            generatorActivity = container.Resolve<IGraphGenerationActivity>();
            httpListener = container.Resolve<IGraphPluginHttpListener>();

            generatorActivity.Start();
            httpListener.Start();
        }

        public override void OnStop()
        {
            if (httpListener != null)
                httpListener.Stop();
            if (generatorActivity != null)
                generatorActivity.Stop();
            if (container != null)
                container.Dispose();

            httpListener = null;
            generatorActivity = null;
            container = null;
        }

        public override void PerformCommand(string commandVerb, string issuingUser)
        {
            Log(TraceEventType.Information, "Performing received command '{0}'...", commandVerb);
            using (BeginEndDisposable.Create(generatorActivity.Stop, generatorActivity.Start))
            {
                if (commandVerb == GraphPluginConfiguration.GenerateGraphsVerb)
                    generatorActivity.GenerateGraphs();
                else
                    throw new NotSupportedException(string.Format("The command '{0}' is not supported", commandVerb));
            }
            Log(TraceEventType.Information, "Perform command '{0}' finished", commandVerb);
        }

        public override void BeforeApplyCommand(TechTalk.SpecLog.Entities.RepositoryInfo repository, TechTalk.SpecLog.Commands.Command command) { /* NOP */ }

        public override void BeforeUndoCommand(TechTalk.SpecLog.Entities.RepositoryInfo repository, TechTalk.SpecLog.Commands.Command command) { /* NOP */ }

        public override void AfterApplyCommand(TechTalk.SpecLog.Entities.RepositoryInfo repository, TechTalk.SpecLog.Commands.Command command) { /* NOP */ }

        public override void AfterUndoCommand(TechTalk.SpecLog.Entities.RepositoryInfo repository, TechTalk.SpecLog.Commands.Command command) { /* NOP */ }

        public static string GetExportPath(string repositoryId)
        {
            var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return System.IO.Path.Combine(baseFolder, "TechTalk", "SpecLog", "GraphPlugin", repositoryId);
        }
    }
}
