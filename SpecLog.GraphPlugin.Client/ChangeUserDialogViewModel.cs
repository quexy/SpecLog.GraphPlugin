using System;
using TechTalk.SpecLog.Application.Common;
using TechTalk.SpecLog.Application.Common.Dialogs;
using TechTalk.SpecLog.Common;

namespace SpecLog.GraphPlugin.Client
{
    public class ChangeUserDialogViewModel : IDialogViewModel
    {
        public event EventHandler<EventArgs<bool?>> Close = delegate { };
        public ChangeUserDialogViewModel(string userName)
        {
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            CancelCommand = new DelegateCommand(Cancel);
            UserName = userName;
        }

        public string Caption
        {
            get { return "Change user"; }
        }

        private string userName;
        public string UserName
        {
            get { return userName; }
            set { userName = value; ConfirmCommand.RaiseCanExecuteChanged(); }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { password = value; ConfirmCommand.RaiseCanExecuteChanged(); }
        }

        private string confirmPwd;
        public string ConfirmPwd
        {
            get { return confirmPwd; }
            set { confirmPwd = value; ConfirmCommand.RaiseCanExecuteChanged(); }
        }


        public IDialogResult GetDialogResultData()
        {
            return new ChangeUserDialogResult(UserName, Password);
        }

        public DelegateCommand ConfirmCommand { get; private set; }
        public void Confirm()
        {
            Close(this, new EventArgs<bool?>(true));
        }
        public bool CanConfirm()
        {
            return !string.IsNullOrWhiteSpace(UserName)
                && !string.IsNullOrWhiteSpace(Password)
                && string.Equals(Password, ConfirmPwd);
        }

        public DelegateCommand CancelCommand { get; private set; }
        public void Cancel()
        {
            Close(this, new EventArgs<bool?>(false));
        }
    }

    public class ChangeUserDialogResult : IDialogResult
    {
        public ChangeUserDialogResult(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
