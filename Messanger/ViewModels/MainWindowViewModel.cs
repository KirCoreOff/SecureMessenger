using Messenger.Infrastructure.Commands;
using Messenger.ViewModels.Base;
using System.Windows.Input;
using Messenger;
using System.Threading;
using System.Collections.ObjectModel;
using Messenger.Models;
using System;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;
using System.Threading.Tasks;

namespace Messenger.ViewModels
{
    internal class MainWindowViewModel : ViewModel
    {
        #region Завершение приложения (Не трогать)
        static public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            Client.DisconnectFromServer();
            Application.Current.Shutdown();
        }
        #endregion
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
                if (filePath != null)
                    Task.Run(() => Client.SendFile(filePath, this));
            }
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
        private ObservableCollection<Message> _StoryMessages = new ObservableCollection<Message>();
        public ObservableCollection<Message> StoryMessages
        {
            get => _StoryMessages;
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
        private string _Status = String.Empty;
        public string Status
        {
            get => _Status;
            set => Set(ref _Status, value);
        }
        #endregion
        #region Прогресс бар
        private double _ProgressBarValue = 0;
        public double ProgressBarValue
        {
            get => _ProgressBarValue;
            set => Set(ref _ProgressBarValue, value);
        }
        #endregion


        public MainWindowViewModel()
        {
            #region Commands
            AttachFileCommand = new LambdaCommand(OnAttachFileCommandExecuted, OnAttachFileCommandExecute);
            SendMessageCommand = new LambdaCommand(OnSendMessageCommandExecuted, OnSendMessageCommandExecute);
            #endregion
            Thread th = new Thread(() => Client.ReadMessage(this));
            th.Start(); // Запуск потока на общение с сервером
        }
    }
}
