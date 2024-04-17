using Messenger.Infrastructure.Commands;
using Messenger.ViewModels.Base;
using Messenger;
using System;
using System.Net;
using System.Security;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using Messenger.Models;

namespace Messenger.ViewModels
{
    internal class LoginWindowViewModel : ViewModel
    {
        #region Commands
        #region LoginUser
        public ICommand LoginUserCommand { get; }
        private void OnLoginUserCommandExecuted(object p)
        {
            // Сюда код подключения
            int authInfo = Client.Auth(UserName, new NetworkCredential("", UserPassword).Password);
            ChangeViewFromServerResponseAuth(authInfo);
        }
        private bool OnLoginUserCommandExecute(object p) => UserName.Length != 0 && UserPassword?.Length != 0;
        #endregion
        #region Регистрация
        public ICommand RegisterUserCommand { get; }
        private void OnRegisterUserCommandExecuted(object p)
        {
            int regInfo = Client.Registration(UserName, new NetworkCredential("", UserPassword).Password);
            ChangeViewFromServerResponseRegistration(regInfo);
        }
        private bool OnRegisterUserCommandExecute(object p) => UserName.Length != 0 && UserPassword?.Length >= 6;
        #endregion
        #endregion
        #region Имя пользователя
        private string _UserName = String.Empty;
        public string UserName
        {
            get => _UserName;
            set => Set(ref _UserName, value);
        }
        #endregion
        #region Пароль пользователя
        private SecureString _UserPassword;
        public SecureString UserPassword
        {
            get => _UserPassword;
            set => Set(ref _UserPassword, value);
        }
        #endregion
        #region Ответ сервера
        private string _ServerResponse = String.Empty;
        public string ServerResponse
        {
            get => _ServerResponse;
            set => Set(ref _ServerResponse, value);
        }
        #endregion
        #region Цвет ответа сервера
        private Brush _ColorServerResponse = Brushes.Black;
        public Brush ColorServerResponse
        {
            get => _ColorServerResponse;
            set => Set(ref _ColorServerResponse, value);
        }
        #endregion
        #region Скрытие окна (не трогать)
        private bool _IsViewVisible = true;
        public bool IsViewVisible
        {
            get => _IsViewVisible;
            set => Set(ref _IsViewVisible, value);
        }
        #endregion

        private void ChangeViewFromServerResponseAuth(int responseCode)
        {
            switch (responseCode)
            {
                case 0:
                    {
                        /* Значения для покраски
                        ColorServerResponse = Brushes.Red;
                        ColorServerResponse = Brushes.<Какой-нибудь цвет>;
                        ColorServerResponse = Brushes.Azure;                          
                          */
                        ServerResponse = "Вы успешно авторизировались!";
                        ColorServerResponse = Brushes.Black;

                        IsViewVisible = false;
                    }
                    break;
                case 1:
                    {
                        ServerResponse = "Вы ввели неверный пароль";
                    }
                    break;
                case 2:
                    {
                        ServerResponse = "Такого пользователя не существует";
                    }
                    break;
                default:
                    break;
            }
        }

        private void ChangeViewFromServerResponseRegistration(int responseCode)
        {
            switch (responseCode)
            {
                case 0:
                    {
                        /* Значения для покраски
                        ColorServerResponse = Brushes.Red;
                        ColorServerResponse = Brushes.<Какой-нибудь цвет>;
                        ColorServerResponse = Brushes.Azure;                          
                          */
                        ServerResponse = "Вы успешно зарегистрировались!";
                        ColorServerResponse = Brushes.Black;
                    }
                    break;
                case 1:
                    {
                        ServerResponse = "Регистрация завершилась с ошибкой";
                    }
                    break;
                case 2:
                    {
                        ServerResponse = "Пользователь с таким логином уже существует";
                    }
                    break;
                default:
                    break;
            }
        }

        public LoginWindowViewModel()
        {
            #region Commands
            LoginUserCommand = new LambdaCommand(OnLoginUserCommandExecuted, OnLoginUserCommandExecute);
            RegisterUserCommand = new LambdaCommand(OnRegisterUserCommandExecuted, OnRegisterUserCommandExecute);
            #endregion

            Client.StartClient();
        }
    }
}
