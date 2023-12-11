﻿using Messanger.Infrastructure.Commands;
using Messanger.ViewModels.Base;
using Messenger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Messanger.ViewModels
{
    internal class LoginWindowViewModel : ViewModel
    {
        #region Commands
        #region LoginUser
        public ICommand LoginUserCommand { get; }
        private void OnLoginUserCommandExecuted(object p)
        {
            // Сюда код подключения
            //Client.Auth();
        }
        private bool OnLoginUserCommandExecute(object p) => UserName.Length != 0 && UserPassword?.Length != 0;
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
        private string _ServerResponse = "Все крута!";
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

        private void ChangeViewFromServerResponse(int responseCode)
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
                    }
                    break;
                case 1:
                    {

                    }
                    break;
                case 2:
                    {

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
            #endregion

            //Client.StartClient();
        }
    }
}
