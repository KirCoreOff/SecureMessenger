using Messanger.Infrastructure.Commands;
using Messanger.ViewModels.Base;
using System.Windows;
using System.Collections.Generic;
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
        #region Загрузка файла
        public ICommand AttachFileCommand { get; }
        private void OnAttachFileCommandExecuted(object p)
        {
            // Suda code
        }
        private bool OnAttachFileCommandExecute(object p) => true;
        #endregion

        #region Название окна
        private string _TitleWindow = "Secure Messanger";
        public string TitleWindow
        {
            get => _TitleWindow;
            set => Set(ref _TitleWindow, value);
        }
        #endregion
        #region История сообщений
        private List<string> _StoryMessages = new List<string>
        {
            "Hello",
            "Salam",
            "alekym asalam"
        };
        public List<string> StoryMessages
        {
            get => _StoryMessages;
            set => Set(ref _StoryMessages, value);
        }
        #endregion
        #region Сообщение текущего пользователя
        private string _CurrentMessage;
        public string CurrentMessage
        {
            get => _CurrentMessage;
            set => Set(ref _CurrentMessage, value);
        }
        #endregion
        #region Статус
        private string _Status;
        public string Status
        {
            get => _Status;
            set => Set(ref _Status, value);
        }
        #endregion


        public MainWindowViewModel()
        {
            #region Commands
            AttachFileCommand = new LambdaCommand(OnAttachFileCommandExecuted, OnAttachFileCommandExecute);
            #endregion
        }
    }
}
