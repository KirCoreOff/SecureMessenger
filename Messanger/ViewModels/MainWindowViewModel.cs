using Messanger.Infrastructure.Commands;
using Messanger.ViewModels.Base;
using System.Windows;
using System.Windows.Input;

namespace Messanger.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        #region Commands
        #region CloseApplicationCommand
        public ICommand CloseApplicationCommmand { get; }
        private void OnCloseApplicationCommandExecuted(object p)
        {
            Application.Current.Shutdown();
        }
        private bool CanCloseApplicationCommandExecute(object p) => true;
        #endregion
        #endregion

        #region Название окна
        private string _TitleWindow = "Hello World!";
        public string TitleWindow
        {
            get => _TitleWindow;
            set => Set(ref _TitleWindow, value);
        } 
        #endregion
            
        public MainWindowViewModel()
        {
            #region Commands
            CloseApplicationCommmand = new LambdaCommand(OnCloseApplicationCommandExecuted, CanCloseApplicationCommandExecute);
            #endregion
        }
    }
}
