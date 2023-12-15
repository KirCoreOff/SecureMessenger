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
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;

namespace Messenger.Models
{
    static class Client
    {
        #region Настроки
        static string masterKey = "01234567890123456789012345678901"; // Ключ шифрования
        static byte[] bytesMasterKey = Encoding.UTF8.GetBytes(masterKey);
        const int BUFF_SIZE = 8192;
        // Настройка Сокета для подключения
        static int port = 11000;
        static IPHostEntry ipHost = Dns.GetHostEntry("");
        static IPAddress ipAddr = ipHost.AddressList[10];
        static IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);
        static Socket serverSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        static bool connectedToServer = true;
        static public string clientLogin = "";
        static Kuznechik kuznechik = new Kuznechik(bytesMasterKey);


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
        static public void ReadMessage(MainWindowViewModel mainVM)
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
                    (kuznechik.Decrypt(buff)).Trim('\0').Split('~');

                if (infoPackage[0] == "F") // Метка получения файла
                {
                    string fileName = infoPackage[1];
                    string encryptedFile = $"enc_{fileName}";
                    int bytesReceived;
                    mainVM.Status = "Получение файла";
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
                            mainVM.ProgressBarValue = (double)i / packages;
                        }
                        Console.WriteLine($"Файл {fileName} получен успешно! Вес файлоа: {file.Length} Байт");
                        // Открытие полученного зашифрованного файла
                    }
                    FileInfo fileInfo = new FileInfo(encryptedFile);
                    using (FileStream file = File.OpenRead(encryptedFile))
                    {
                        mainVM.Status = "Файл расшифровывается...";
                        // Создание расшифрованного файла
                        using (FileStream destinationStream = File.Create($"{fileName}"))
                        {
                            int threadCount = 10;
                            int bytesRead = 0;
                            byte[] encBuffer = new byte[BUFF_SIZE * threadCount];
                            byte[] decBuffer = new byte[BUFF_SIZE * threadCount];
                            Thread[] threads = new Thread[threadCount];
                            while ((bytesRead = file.Read(encBuffer, 0, encBuffer.Length)) > 0)
                            {
                                for (int i = 0; i < threadCount; i++)
                                {
                                    threads[i] = new Thread(() =>
                                    {
                                        int indexOfCurrentBlock = int.Parse(Thread.CurrentThread.Name) * BUFF_SIZE;
                                        Array.Copy(kuznechik.Decrypt(encBuffer.Skip(indexOfCurrentBlock).Take(BUFF_SIZE).ToArray()),
                                            0, decBuffer, indexOfCurrentBlock, BUFF_SIZE);
                                    });
                                    threads[i].Name = i.ToString();
                                    threads[i].Start();
                                }
                                for (int i = 0; i < threadCount; i++)
                                {
                                    threads[i].Join();
                                }
                                mainVM.ProgressBarValue = (double)destinationStream.Length / file.Length * 100;
                                destinationStream.Write(decBuffer, 0, bytesRead);
                            }
                        }
                    }
                    mainVM.Status = "Файл расшифровался";
                    mainVM.ProgressBarValue = 0;
                    File.Delete(encryptedFile);
                }
                else if (infoPackage[0] == "M") // Метка получения сообщения
                {
                    Application.Current.Dispatcher.BeginInvoke(
                      DispatcherPriority.Background,
                      new Action(() => mainVM.StoryMessages.Add(
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
            byte[] encryptedBytes = kuznechik.Encript(Encoding.UTF8.GetBytes(
                $"M~{clientLogin}~{msg}~{DateTime.Now.ToShortTimeString()}~"));
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
        static public void SendFile(string filePath, MainWindowViewModel mainVM)
        {
            Console.WriteLine("Sending file...");
            //string filePath = "smile.jpg";
            FileInfo file = new FileInfo(filePath);
            string encriptedFilePath = $"enc_{file.Name}";
            //string decriptedFilePath = $"dec_{file.Name}";
            if (File.Exists(filePath))
            {
                // Открытие файл на чтениен
                using (FileStream sourceStream = File.OpenRead(filePath))
                {
                    mainVM.Status = "Файл шифруется...";
                    // Создание шифрованного файла
                    using (FileStream destinationStream = File.Create(encriptedFilePath))
                    {
                        int threadCount = 10;
                        int bytesRead = 0;
                        byte[] buffer = new byte[BUFF_SIZE * threadCount];
                        byte[] encBuffer = new byte[BUFF_SIZE * threadCount];
                        Thread[] threads = new Thread[threadCount];
                        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            for (int i = 0; i < threadCount; i++)
                            {
                                threads[i] = new Thread(() =>
                                {
                                    int indexOfCurrentBlock = int.Parse(Thread.CurrentThread.Name) * BUFF_SIZE;
                                    Array.Copy(kuznechik.Encript(buffer.Skip(indexOfCurrentBlock).Take(BUFF_SIZE).ToArray()),
                                        0, encBuffer, indexOfCurrentBlock, BUFF_SIZE);
                                });
                                threads[i].Name = i.ToString();
                                threads[i].Start();
                            }
                            for (int i = 0; i < threadCount; i++)
                            {
                                threads[i].Join();
                            }
                            mainVM.ProgressBarValue = (double)destinationStream.Length / sourceStream.Length * 100;
                            destinationStream.Write(encBuffer, 0, bytesRead);
                        }
                    }
                }
                mainVM.ProgressBarValue = 0;
                Console.WriteLine($"Файл {file.Name} зашифрован в файл {encriptedFilePath}");
                byte[] buff = kuznechik.Encript(Encoding.UTF8.GetBytes
                    ($"F~{clientLogin}~{file.Name}~{file.Length}~{DateTime.Now.ToShortTimeString()}~")); // Служебрный пакет
                serverSocket.Send(buff);
                mainVM.Status = "Файл отправляется...";
                serverSocket.SendFile(encriptedFilePath);
                mainVM.Status = "Файл отправлен!";
                File.Delete(encriptedFilePath);
                Console.WriteLine($"Файл отправлен успешно! Вес файла: {file.Length} Байт");
                Application.Current.Dispatcher.BeginInvoke(
                      DispatcherPriority.Background,
                      new Action(() => mainVM.StoryMessages.Add(
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
            byte[] buff = kuznechik.Encript(Encoding.UTF8.GetBytes($"R~{login}~{password}~"));
            serverSocket.Send(buff);
            buff = new byte[BUFF_SIZE];
            // Получение ответа от сервера
            serverSocket.Receive(buff);
            int regInfo = -1;
            string[] infoPackage = Encoding.UTF8.GetString
                    (kuznechik.Decrypt(buff)).Trim('\0').Split('~');
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
            byte[] buff = kuznechik.Encript(Encoding.UTF8.GetBytes($"A~{login}~{password}~"));
            serverSocket.Send(buff);
            buff = new byte[BUFF_SIZE];
            // Получение ответа от сервера
            serverSocket.Receive(buff);
            int authInfo = -1;
            string[] infoPackage = Encoding.UTF8.GetString
                    (kuznechik.Decrypt(buff)).Trim('\0').Split('~');
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
            connectedToServer = false;
            serverSocket.Close();
        }
    }
}
