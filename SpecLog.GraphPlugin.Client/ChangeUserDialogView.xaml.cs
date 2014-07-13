using System;
using System.Windows;
using System.Windows.Controls;

namespace SpecLog.GraphPlugin.Client
{
    /// <summary>
    /// Interaction logic for ChangeUserDialogView.xaml
    /// </summary>
    public partial class ChangeUserDialogView : UserControl
    {
        public ChangeUserDialogView()
        {
            InitializeComponent();
        }

        private void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ChangeUserDialogViewModel;
            if (viewModel == null) return;

            viewModel.Password = PasswordBox.Password;
            viewModel.ConfirmPwd = ConfirmPwdBox.Password;
        }
    }
}
