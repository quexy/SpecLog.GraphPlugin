using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Unity;
using TechTalk.SpecLog.Commands;
using TechTalk.SpecLog.Commands.Synchronization;
using TechTalk.SpecLog.Common.Commands;
using TechTalk.SpecLog.Common.Exceptions;
using TechTalk.SpecLog.DataAccess.Boundaries;
using TechTalk.SpecLog.DataAccess.Commands;
using TechTalk.SpecLog.DataAccess.Repositories;
using TechTalk.SpecLog.Synchronization;

namespace SpecLog.GraphPlugin.Server
{
    public class JustUpdateSyncStrategy : IClientSyncStrategy
    {
        private readonly ISyncStep[] steps;
        public JustUpdateSyncStrategy(IUnityContainer container)
        {
            steps = new ISyncStep[]
            {
                container.Resolve<MyUpdateChangesStep>(),
            };
        }

        public IEnumerable<ISyncStep> GetSyncSteps()
        {
            return steps;
        }
    }

    public class DummyCommandListMerger : ICommandListMerger
    {
        public IEnumerable<ChangeResponse> MergeCommands(IEnumerable<ChangeResponse> changeList)
        {
            return changeList;
        }
    }

    public class JustStoreCommandExecutor : ICommandExecutor
    {
        private readonly IGraphPluginRepositoryAccess repositoryAccess;
        public JustStoreCommandExecutor(IGraphPluginRepositoryAccess repositoryAccess)
        {
            this.repositoryAccess = repositoryAccess;
        }

        public bool Apply(TechTalk.SpecLog.Entities.RepositoryInfo repository, TechTalk.SpecLog.Commands.Command command)
        {
            repositoryAccess.StoreCommand(command);

            if (command.CommandName == TechTalk.SpecLog.Commands.CommandName.RenameRepository)
            {
                var changeArgs = command.CommandArgs as EntityChangeCommandArgs;
                var nameChange = changeArgs.EntityChange.PropertyChanges.Single(pc => pc.Name == "Name");
                repositoryAccess.RenameRepository(nameChange.NewValue);
            }

            return true;
        }

        public void Resolve(TechTalk.SpecLog.Entities.RepositoryInfo repository, TechTalk.SpecLog.Commands.Command command)
        {
            throw new NotSupportedException();
        }

        public void Undo(TechTalk.SpecLog.Entities.RepositoryInfo repositoryInfo, TechTalk.SpecLog.Commands.Command command)
        {
            throw new NotSupportedException();
        }
    }

    public class DummyClientCommandRepository : IClientCommandRepository
    {
        public TechTalk.SpecLog.Entities.ClientCommand CreateClientCommand(IBoundary boundary)
        {
            throw new NotSupportedException();
        }

        public void Delete(IBoundary boundary, TechTalk.SpecLog.Entities.ClientCommand clientCommand)
        {
            /* NOP */
        }

        public TechTalk.SpecLog.Entities.ClientCommand GetById(IBoundary boundary, Guid commandId)
        {
            return null;
        }

        public TechTalk.SpecLog.Entities.ClientCommand[] GetChangesForUndo(IBoundary boundary, IEnumerable<Guid> undoCommandIdList)
        {
            return new TechTalk.SpecLog.Entities.ClientCommand[0];
        }

        public TechTalk.SpecLog.Entities.ClientCommand[] GetEarliestNotSynchronizedChanges(IBoundary boundary, int maxCount)
        {
            return new TechTalk.SpecLog.Entities.ClientCommand[0];
        }

        public TechTalk.SpecLog.Entities.ClientCommand GetFirstNotSynchronizedChange(IBoundary boundary)
        {
            return null;
        }
    }

    public class MyUpdateChangesStep : ISyncStep
    {
        private readonly IBoundaryFactory boundaryFactory;
        private readonly UpdateChangesStep implementation;
        public MyUpdateChangesStep(IBoundaryFactory boundaryFactory, UpdateChangesStep implementation)
        {
            this.implementation = implementation;
            this.boundaryFactory = boundaryFactory;
        }

        public bool DoStep(IBoundary rootBoundary, IRepositoryStorage repositoryStorage, ISynchronizationService syncService)
        {
            try
            {
                return implementation.DoStep(rootBoundary, repositoryStorage, syncService);
            }
            catch (MissingCommandException ex)
            {
                using (var boundary = boundaryFactory.CreateShortRunningWithUpdateBarrier(rootBoundary, "BaseLineMagick"))
                {
                    var repository = repositoryStorage.GetRepository(boundary);
                    repository.BaseLineVersion = ex.FirstCommandVersion - 1;
                    boundary.Complete();
                    return true;
                }
            }
        }
    }
}
