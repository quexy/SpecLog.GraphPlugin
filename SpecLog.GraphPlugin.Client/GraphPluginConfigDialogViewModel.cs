using System;
using System.Windows;
using System.Windows.Input;
using TechTalk.SpecLog.Application.Common;
using TechTalk.SpecLog.Application.Common.Dialogs;
using TechTalk.SpecLog.Application.Common.PluginsInfrastructure;
using TechTalk.SpecLog.Entities;

namespace SpecLog.GraphPlugin.Client
{
    public class GraphPluginConfigDialogViewModel : PluginConfigurationDialogViewModel<GraphPluginConfiguration>
    {
        private readonly IRepositoryInfo repository;
        private readonly IDialogService dialogService;
        public GraphPluginConfigDialogViewModel(IDialogService dialogService, IRepositoryInfo repository, string config, bool enabled)
            : base(config, enabled)
        {
            this.repository = repository;
            this.dialogService = dialogService;
            CopyPublishUrlCommand = new DelegateCommand(CopyPublishUrl);
            ChangeCredentialsCommand = new DelegateCommand(ChangeViewerCredentials);
            ClearCredentialsCommand = new DelegateCommand(ClearViewerCredentials);
        }

        public override string Caption
        {
            get { return "Graph Plugin Configuration"; }
        }

        public ICommand CopyPublishUrlCommand { get; private set; }
        public ICommand ChangeCredentialsCommand { get; private set; }
        public ICommand ClearCredentialsCommand { get; private set; }

        public Visibility ClearButtonVisibility
        {
            get { return string.IsNullOrEmpty(configuration.UserName) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public int GraphRefreshPeriod
        {
            get { return configuration.GraphRefreshPeriod; }
            set
            {
                configuration.GraphRefreshPeriod = value;
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsRestricted
        {
            get { return configuration.IsRestricted; }
            set
            {
                configuration.IsRestricted = value;
                NotifyPropertyChanged("IsRestricted");
                NotifyPropertyChanged("PublishUrl");
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public string UserName
        {
            get
            {
                if (string.IsNullOrEmpty(configuration.UserName))
                    return "not set";
                return configuration.UserName;
            }
        }

        public string PublishUrl
        {
            get
            {
                if (repository == null) return string.Empty;
                var uriBuilder = new UriBuilder(repository.ServerUrl);
                uriBuilder.Path = "RepositoryActivity/" + repository.Id.ToString();
                return uriBuilder.ToString();
            }
        }

        public void CopyPublishUrl()
        {
            Clipboard.SetText(PublishUrl);
        }

        public void ChangeViewerCredentials()
        {
            var result = dialogService.ShowDialog(new ChangeUserDialogViewModel(configuration.UserName));
            var changeUserResult = result as ChangeUserDialogResult;
            if (changeUserResult != null)
            {
                configuration.UserName = changeUserResult.UserName;
                sensitiveConfig[GraphPluginConfiguration.CredentialsPasswordKey] = changeUserResult.Password;
            }
            NotifyPropertyChanged("UserName");
            NotifyPropertyChanged("ClearButtonVisibility");
            SaveCommand.RaiseCanExecuteChanged();
        }

        public void ClearViewerCredentials()
        {
            configuration.UserName = null;
            sensitiveConfig.Remove(GraphPluginConfiguration.CredentialsPasswordKey);
            NotifyPropertyChanged("UserName");
            NotifyPropertyChanged("ClearButtonVisibility");
            SaveCommand.RaiseCanExecuteChanged();
        }

        public override bool CanSave()
        {
            if (!IsEnabled) return true;
            return configuration.GraphRefreshPeriod > 0
                && (!IsRestricted || !string.IsNullOrEmpty(configuration.UserName))
            ;
        }
    }
}
