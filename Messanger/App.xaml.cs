using System;
using System.Windows;
using Messenger.Models;
using Messenger.ViewModels;
using Messenger.Views.Windows;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected void ApplicationStart(object sender, EventArgs e)
        {
            var loginView = new LoginWindow();
            loginView.Show();
            loginView.IsVisibleChanged += (s, ev) =>
            {
                if (loginView.IsVisible == false && loginView.IsLoaded)
                {
                    var mainView = new MainWindow();
                    mainView.Closing += MainWindowViewModel.OnWindowClosing;
                    mainView.Show();
                    loginView.Close();
                }
            };
        }
    }
}
