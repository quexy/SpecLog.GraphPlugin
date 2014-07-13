using System;
using System.Collections.Generic;
using TechTalk.SpecLog.Application.Common.Dialogs;
using TechTalk.SpecLog.Application.Common.PluginsInfrastructure;
using TechTalk.SpecLog.Common;
using TechTalk.SpecLog.Entities;

namespace SpecLog.GraphPlugin.Client
{
    [Plugin(GraphPlugin.PluginName)]
    public class GraphPlugin : IClientPlugin, IRepositoryAwareConfiguration
    {
        public const string PluginName = "SpecLog.GraphPlugin";

        private readonly IDialogService dialogService;
        public GraphPlugin(IDialogService dialogService)
        {
            this.dialogService = dialogService;
        }

        public string Name
        {
            get { return PluginName; }
        }

        public string DisplayName
        {
            get { return "Graph Plugin"; }
        }

        public string Description
        {
            get { return "Periodically generates various HTML graphs and makes them available through a simple web server."; }
        }

        public string LearnMoreLink
        {
            get { return "http://github.com/quexy/SpecLog.GraphPlugin/"; }
        }

        public string LearnMoreLinkText
        {
            get { return "Learn more..."; }
        }

        public bool IsConfigurable(RepositoryMode repositoryMode)
        {
            return repositoryMode == RepositoryMode.ClientServer;
        }

        public IDialogViewModel GetConfigDialog(RepositoryMode repositoryMode, bool isEnabled, string configuration)
        {
            return GetConfigDialog(null, isEnabled, configuration);
        }
        public IDialogViewModel GetConfigDialog(IRepositoryInfo repository, bool isEnabled, string configuration)
        {
            return new GraphPluginConfigDialogViewModel(dialogService, repository, configuration, isEnabled);
        }

        public bool IsGherkinLinkProvider(RepositoryMode repositoryMode)
        {
            return false;
        }

        public IGherkinLinkProviderViewModel GetGherkinLinkViewModel(RepositoryMode repositoryMode)
        {
            return null;
        }

        public bool IsGherkinStatsProvider(RepositoryMode repositoryMode)
        {
            return false;
        }

        public IGherkinStatsProvider CreateStatsProvider(RepositoryMode repositoryMode, string configuration, IGherkinStatsRepository statsRepository)
        {
            return null;
        }

        public bool IsWorkItemSynchronizer(RepositoryMode repositorMode)
        {
            return false;
        }

        public IEnumerable<PluginCommand> GetSupportedCommands(RepositoryMode repositoryMode)
        {
            return new[] { new PluginCommand { CommandVerb = GraphPluginConfiguration.GenerateGraphsVerb, DisplayText = "Generate Graphs" } };
        }
    }
}
