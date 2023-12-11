using Messanger.Infrastructure.Commands;
using Messanger.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
        private SecureString _UserPassword ;
        public SecureString UserPassword
        {
            get => _UserPassword;
            set => Set(ref _UserPassword, value);
        }
        #endregion

        public LoginWindowViewModel()
        {
            #region Commands
            LoginUserCommand = new LambdaCommand(OnLoginUserCommandExecuted, OnLoginUserCommandExecute);
            #endregion
        }
    }
}
