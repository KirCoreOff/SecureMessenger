using Messenger.Infrastructure.Commands;
using Messenger.ViewModels.Base;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using Messenger;
using System.Threading;
using System.Collections.ObjectModel;
using Messenger.Models;

namespace Messenger.ViewModels
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
            Client.SendMessage(CurrentMessage);
            //StoryMessages.Add(CurrentMessage);
            CurrentMessage = string.Empty;
        }
        private bool OnAttachFileCommandExecute(object p) => true;
        #endregion
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
        static private ObservableCollection<Message> _StoryMessages = new ObservableCollection<Message>
        { new Message("login111", "Ukraine for gays", "12.12.2023")
        };
        static public ObservableCollection<Message> StoryMessages
        {
            get => _StoryMessages;
            //set => Set(ref _StoryMessages, value);
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
            Thread th = new Thread(Client.ReadMessage);
            th.Start(); // Запуск потока на общение с сервером
        }
    }
}
