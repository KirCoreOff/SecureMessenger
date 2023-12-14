using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System;
using System.IO;
using System.Security;
using System.Security.Cryptography.Xml;
using Messenger.ViewModels;
using System.Windows.Threading;
using System.Windows;
using Messenger.Models;

namespace Messenger.Models
{
    static class Client
    {
        #region Настроки
        static string masterKey = "01234567890123456789012345678901"; // Ключ шифрования
        static byte[] bytesMasterKey = Encoding.UTF8.GetBytes(masterKey);
        const int BUFF_SIZE = 1024;
        // Настройка Сокета для подключения
        static int port = 11000;
        static IPHostEntry ipHost = Dns.GetHostEntry("localhost");
        static IPAddress ipAddr = ipHost.AddressList[0];
        static IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);
        static Socket serverSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        static bool connectedToServer = true;
        static public string clientLogin = "";
        #endregion

        /// <summary>
        /// Метод запуска клиентского приложения
        /// </summary>
        static public void StartClient()
        {
            serverSocket.Connect(ipEndPoint); // Подключение к серверу
            Thread.Sleep(500);
        }
        /// <summary>
        /// Метод общения с сервером
        /// </summary>
        static public void ReadMessage()
        {
            while (connectedToServer)
            {
                byte[] buff = new byte[BUFF_SIZE];
                // Получение сообщения от клиента
                try
                {
                    serverSocket.Receive(buff);    // Получаем последовательность байтов из сокета в буфер buff
                }
                catch (Exception)
                {

                }
                // Расшифровка и разбиение пакета на информациронные части 
                string[] infoPackage = Encoding.UTF8.GetString
                    (Kuznechik.Decrypt(buff, bytesMasterKey)).Trim('\0').Split('~');

                if (infoPackage[0] == "F") // Метка получения файла
                {
                    string fileName = infoPackage[1];
                    string encryptedFile = $"enc_{fileName}";
                    int bytesReceived;
                    // Содание файла для записи передаваемых зашифрованных байтов файла
                    using (FileStream file = File.OpenWrite(encryptedFile))
                    {
                        // Определение количества необходимых пакетов
                        int packages = int.Parse(infoPackage[2]) / BUFF_SIZE;
                        if (long.Parse(infoPackage[2]) % BUFF_SIZE != 0)
                            packages++;
                        // Запись зашифрованных байтов в файл
                        for (int i = 0; i < packages; i++)
                        {
                            bytesReceived = serverSocket.Receive(buff);
                            file.Write(buff, 0, bytesReceived);
                        }
                        Console.WriteLine($"Файл {fileName} получен успешно! Вес файлоа: {file.Length} Байт");
                        // Открытие полученного зашифрованного файла
                    }
                    using (FileStream file = File.OpenRead(encryptedFile))
                    {
                        // Создание расшифрованного файла
                        using (FileStream destinationStream = File.Create($"{fileName}"))
                        {
                            // Создаем буфер для чтения и записи данных
                            byte[] buffer = new byte[BUFF_SIZE];
                            int bytesRead;
                            // Расшифровка байтов из полученного файла и запись их в созданный 
                            while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                byte[] encryptedBytes = Kuznechik.Decrypt(buffer, bytesMasterKey);
                                destinationStream.Write(encryptedBytes, 0, bytesRead);
                            }
                        }
                    }
                    File.Delete(encryptedFile);
                }
                else if (infoPackage[0] == "M") // Метка получения сообщения
                {
                    Application.Current.Dispatcher.BeginInvoke(
                      DispatcherPriority.Background,
                      new Action(() => MainWindowViewModel.StoryMessages.Add(
                          new Message(infoPackage[1], infoPackage[2], infoPackage[3]))));

                }
            }
        }
        /// <summary>
        /// Метод отправки сообщения серверу
        /// </summary>
        /// <param name="msg">Сообщение</param>
        static public void SendMessage(string msg)
        {
            byte[] encryptedBytes = Kuznechik.Encript(Encoding.UTF8.GetBytes(
                $"M~{clientLogin}~{msg}~{DateTime.Now.ToShortTimeString()}~"), bytesMasterKey);
            try
            {
                serverSocket.Send(encryptedBytes);
            }
            catch
            {
                Console.WriteLine("Соединение с сервером прервано.");
            }

        }
        /// <summary>
        /// Метод отправки файла серверу
        /// </summary>
        static public void SendFile(string filePath)
        {
            Console.WriteLine("Sending file...");
            //string filePath = "smile.jpg";
            FileInfo file = new FileInfo(filePath);
            string encriptedFilePath = $"enc_{file.Name}";
            string decriptedFilePath = $"dec_{file}";
            if (File.Exists(filePath))
            {
                // Открытие файл на чтениен
                using (FileStream sourceStream = File.OpenRead(filePath))
                {
                    // Создание шифрованного файла
                    using (FileStream destinationStream = File.Create(encriptedFilePath))
                    {
                        // Создаем буфер для чтения и записи данных
                        byte[] buffer = new byte[BUFF_SIZE];
                        int bytesRead;
                        // Процесс шифрования байтов файла
                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            byte[] encryptedBytes = Kuznechik.Encript(buffer, bytesMasterKey);
                            destinationStream.Write(encryptedBytes, 0, bytesRead);
                        }
                    }
                }
                Console.WriteLine($"Файл {file.Name} зашифрован в файл {encriptedFilePath}");
                byte[] buff = Kuznechik.Encript(Encoding.UTF8.GetBytes
                    ($"F~{clientLogin}~{file.Name}~{file.Length}~{DateTime.Now}~"), bytesMasterKey); // Служебрный пакет
                serverSocket.Send(buff);
                serverSocket.SendFile(encriptedFilePath);
                File.Delete(encriptedFilePath);
                Console.WriteLine($"Файл отправлен успешно! Вес файла: {file.Length} Байт");
                Application.Current.Dispatcher.BeginInvoke(
                      DispatcherPriority.Background,
                      new Action(() => MainWindowViewModel.StoryMessages.Add(
                          new Message(clientLogin, $"Вы отправили файл {file.Name}", DateTime.Now.ToShortTimeString()))));
            }
            else Console.WriteLine("Файл не найден");
        }
        /// <summary>
        /// Метод регистрации клиента в системе
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>Результат регистрации: 0 - Регистрация успешна, 
        /// 1 - Регистрация завершилась с ошибкой,
        /// 2 - Пользователь с таким логином уже существует</returns>
        static public int Registration(string login, string password)
        {
            // Отправка логина и пароля в зашифрованном виде на сервер для регистрации
            byte[] buff = Kuznechik.Encript(Encoding.UTF8.GetBytes($"R~{login}~{password}~"), bytesMasterKey);
            serverSocket.Send(buff);
            buff = new byte[BUFF_SIZE];
            // Получение ответа от сервера
            serverSocket.Receive(buff);
            int regInfo = -1;
            string[] infoPackage = Encoding.UTF8.GetString
                    (Kuznechik.Decrypt(buff, bytesMasterKey)).Trim('\0').Split('~');
            if (infoPackage[0] == "S")
                regInfo = int.Parse(infoPackage[1]);
            return regInfo;
        }
        /// <summary>
        /// Метод аутентификации клиента в системе
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>Результат аутентификации: 0 - Аутентификация успешна, 
        /// 1 - Введен неверный пароль,
        /// 2 - Пользователя с таким логином не существует</returns>
        static public int Auth(string login, string password)
        {
            //string login = "login122";
            //string password = "password122";
            // Отправка логина и пароля в зашифрованном виде на сервер для аутентификации
            byte[] buff = Kuznechik.Encript(Encoding.UTF8.GetBytes($"A~{login}~{password}~"), bytesMasterKey);
            serverSocket.Send(buff);
            buff = new byte[BUFF_SIZE];
            // Получение ответа от сервера
            serverSocket.Receive(buff);
            int authInfo = -1;
            string[] infoPackage = Encoding.UTF8.GetString
                    (Kuznechik.Decrypt(buff, bytesMasterKey)).Trim('\0').Split('~');
            clientLogin = login;
            if (infoPackage[0] == "S")
                authInfo = int.Parse(infoPackage[1]);
            return authInfo;
        }
        /// <summary>
        /// Метод отключения от сервера
        /// </summary>
        static public void DisconnectFromServer()
        {
            byte[] encryptedBytes = Kuznechik.Encript(Encoding.UTF8.GetBytes($"Q~"), bytesMasterKey);
            serverSocket.Send(encryptedBytes);
            connectedToServer = false;
            serverSocket.Close();
            Console.WriteLine("Вы отключились от сервера");
        }
    }
}
