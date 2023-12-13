using Messenger.Infrastructure.Commands;
using Messenger.ViewModels.Base;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using Messenger;
using System.Threading;
using System.Collections.ObjectModel;
using Messenger.Models;
using System;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Linq;

namespace Messenger.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        #region Commands
        #region Загрузка файла
        public ICommand AttachFileCommand { get; }
        private void OnAttachFileCommandExecuted(object p)
        {
            string filePath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
            }
            Client.SendFile(filePath);
        }
        private bool OnAttachFileCommandExecute(object p) => true;
        #endregion
        #region Отправка сообщения
        public ICommand SendMessageCommand { get; }
        private void OnSendMessageCommandExecuted(object p)
        {
            Client.SendMessage(CurrentMessage);
            StoryMessages.Add(new Message(Client.clientLogin, CurrentMessage, DateTime.Now.ToShortTimeString()));
            CurrentMessage = string.Empty;
        }
        private bool OnSendMessageCommandExecute(object p) => !CurrentMessage.Equals(String.Empty);
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
        { new Message("login111", "Ukraine for gays, Ukraine for gays", "23:15")
        };
        static public ObservableCollection<Message> StoryMessages
        {
            get => _StoryMessages;
            //set => Set(ref _StoryMessages, value);
        }
        #endregion
        #region Сообщение текущего пользователя
        private string _CurrentMessage = String.Empty;
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
            SendMessageCommand = new LambdaCommand(OnSendMessageCommandExecuted, OnSendMessageCommandExecute);
            #endregion
            Thread th = new Thread(Client.ReadMessage);
            th.Start(); // Запуск потока на общение с сервером
        }
    }
}
