using System;
using Microsoft.Practices.Unity;
using SpecLog.GraphPlugin.Server.HtmlGraphGenerators;
using TechTalk.SpecLog.Common;
using TechTalk.SpecLog.Common.Commands;
using TechTalk.SpecLog.DataAccess;
using TechTalk.SpecLog.DataAccess.Commands;
using TechTalk.SpecLog.Synchronization;

namespace SpecLog.GraphPlugin.Server
{
    public interface IGraphPluginContainerSetup : IPluginContainerSetup
    {
        IUnityContainer ConfigureContainer(GraphPluginConfiguration configuration);
    }

    public class GraphPluginContainerSetup : IGraphPluginContainerSetup
    {
        private IUnityContainer container;
        public void Setup(IUnityContainer container)
        {
            this.container = container;
            container.RegisterInstance<IGraphPluginContainerSetup>(this, new ContainerControlledLifetimeManager());

            container
                .RegisterType<IClientSyncStrategy, JustUpdateSyncStrategy>(new ContainerControlledLifetimeManager())
                .RegisterType<ICommandListMerger, DummyCommandListMerger>(new ContainerControlledLifetimeManager())
                .RegisterType<ICommandExecutor, JustStoreCommandExecutor>(new ContainerControlledLifetimeManager())
                .RegisterType<IClientCommandRepository, DummyClientCommandRepository>(new ContainerControlledLifetimeManager())
                .RegisterType<IGraphPluginRepositoryAccess, GraphPluginRepositoryAccess>(new ContainerControlledLifetimeManager())
                .RegisterType<IGraphDataRepositoryAccess, GraphPluginRepositoryAccess>(new ContainerControlledLifetimeManager())
            ;

            container
                .RegisterType<IHtmlGraphGenerator, PunchcardHtmlGraphGenerator>("PunchcardHtmlGraphGenerator", new ContainerControlledLifetimeManager())
                .RegisterType<IHtmlGraphGenerator, FrequencyHtmlGraphGenerator>("FrequencyHtmlGraphGenerator", new ContainerControlledLifetimeManager())
                //TODO: design, create, and add new graph types:
                //    * register here as "{type}HtmlGraphGenerator" like above;
                //    * at GraphPluginHttpListener.SupportedGraphTypes as seen there
            ;
        }

        public IUnityContainer ConfigureContainer(GraphPluginConfiguration configuration)
        {
            var childContainer = container.CreateChildContainer();

            childContainer.RegisterInstance<IPeriodicActivityConfiguration>(configuration);
            childContainer.RegisterType<IGraphGenerationActivity, GraphGenerationActivity>(new ContainerControlledLifetimeManager());

            childContainer.RegisterInstance<IHttpListenerConfiguration>(configuration);
            childContainer.RegisterType<IGraphPluginHttpListener, GraphPluginHttpListener>(new ContainerControlledLifetimeManager());

            return childContainer;
        }
    }
}
