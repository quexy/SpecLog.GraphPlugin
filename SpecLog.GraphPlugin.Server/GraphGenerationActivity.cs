using System;
using TechTalk.SpecLog.Common;
using TechTalk.SpecLog.DataAccess.Boundaries;
using TechTalk.SpecLog.Entities;
using TechTalk.SpecLog.Logging;

namespace SpecLog.GraphPlugin.Server
{
    public interface IHtmlGraphGenerator
    {
        void GenerateGraph();
    }

    public interface IGraphGenerationActivity : IPeriodicActivity
    {
        void GenerateGraphs();
    }

    public class GraphGenerationActivity : PeriodicActivity, IGraphGenerationActivity
    {
        private readonly ILogger logger;
        private readonly IUpdateBarrier updateBarrier;
        private readonly IHtmlGraphGenerator[] generators;
        public GraphGenerationActivity
        (
            ILogger logger,
            ITimeService timeService,
            IPeriodicActivityConfiguration configuration,
            IUpdateBarrier updateBarrier,
            IHtmlGraphGenerator[] generators
        )
            : base(timeService, configuration, true)
        {
            this.logger = logger;
            this.updateBarrier = updateBarrier;
            this.generators = generators;
        }

        public void GenerateGraphs()
        {
            TriggerAction();
        }

        protected override bool PerformAction()
        {
            var success = true;
            foreach (var generator in generators)
            {
                var name = generator.GetType().Name;
                using (updateBarrier.BeginUpdate("GenerateGraph/" + name))
                {
                    try
                    {
                        generator.GenerateGraph();
                    }
                    catch (Exception ex)
                    {
                        success = false;
                        logger.Log(System.Diagnostics.TraceEventType.Warning,
                            "Graph generator '{0}' failed: {1}", name, ex);
                    }
                    finally
                    {
                        ReportProgress();
                    }
                }
            }
            return success;
        }
    }
}
