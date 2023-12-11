using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http;
using System;
using System.Reflection;
using System.Drawing;

namespace Messenger
{
    class Program
    {
        static class Client
        {
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
            /// <summary>
            /// Метод запуска клиентского приложения
            /// </summary>
            static public void StartClient()
            {
                serverSocket.Connect(ipEndPoint); // Подключение к серверу
                Thread.Sleep(500);
                Thread th = new Thread(ReadMessage);
                th.Start(); // Запуск потока на общение с сервером
            }
            /// <summary>
            /// Метод общения с сервером
            /// </summary>
            static void ReadMessage()
            {             
                while (connectedToServer)
                {
                    byte[] buff = new byte[BUFF_SIZE];
                    try
                    {
                        serverSocket.Receive(buff);    // Получаем последовательность байтов из сокета в буфер buff
                    }
                    catch (Exception)
                    {

                    }
                    string[] infoPackage = Encoding.UTF8.GetString
                        (Kuznechik.Decrypt(buff, bytesMasterKey)).Trim('\0').Split('~'); // Расшифровка и разбиение пакета на информациронные части 

                    if (infoPackage[0] == "F") // Метка получения файла
                    {
                        string fileName = infoPackage[1];
                        int bytesReceived;
                        // Содание файла для записи передаваемых зашифрованных байтов файла
                        using (FileStream file = File.OpenWrite($"1_{fileName}"))
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
                            using (FileStream sourceStream = File.OpenRead($"1_{fileName}"))
                            {
                                // Создание расшифрованного файла
                                using (FileStream destinationStream = File.Create($"d_{fileName}"))
                                {
                                    // Создаем буфер для чтения и записи данных
                                    byte[] buffer = new byte[BUFF_SIZE];
                                    int bytesRead;
                                    // Расшифровка байтов из полученного файла и запись их в созданный 
                                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        byte[] encryptedBytes = Kuznechik.Decrypt(buffer, bytesMasterKey);
                                        destinationStream.Write(encryptedBytes, 0, bytesRead);
                                    }

                                }
                            }
                        }
                    }
                    else if (infoPackage[0] == "M") // Метка получения сообщения
                    {
                        Console.WriteLine(infoPackage[1].Trim('\0'));
                    }
                }
            }
            /// <summary>
            /// Метод отправки сообщения серверу
            /// </summary>
            /// <param name="msg">Сообщение</param>
            static public void SendMessage(string msg)
            {
                byte[] encryptedBytes = Kuznechik.Encript(Encoding.UTF8.GetBytes($"M~{msg}~"), bytesMasterKey);
                try
                {
                    serverSocket.Send(encryptedBytes);
                } catch 
                {
                    Console.WriteLine("Соединение с сервером прервано.");
                }
                
            }
            /// <summary>
            /// Метод отправки файла серверу
            /// </summary>
            static public void SendFile() 
            {
                Console.WriteLine("Sending file...");
                string filePath = "smile.jpg";
                string encriptedFilePath = "encsmile.jpg";
                string decriptedFilePath = "decsmile.jpg";
                FileInfo file = new FileInfo(encriptedFilePath);
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
                    Console.WriteLine($"Файл {filePath} зашифрован в файл {encriptedFilePath}");
                     
                    //using (FileStream sourceStream = File.OpenRead(encriptedFilePath))
                    //{
                    //    // Создаем или перезаписываем файл назначения
                    //    using (FileStream destinationStream = File.Create(decriptedFilePath))
                    //    {
                    //        // Создаем буфер для чтения и записи данных
                    //        byte[] buffer = new byte[BUFF_SIZE];
                    //        int bytesRead;

                    //        // Читаем данные из исходного файла и записываем их в файл назначения
                    //        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    //        {
                    //            byte[] encryptedBytes = Kuznechik.Decrypt(buffer, bytesMasterKey);
                    //            destinationStream.Write(encryptedBytes, 0, bytesRead);
                    //        }
                    //    }
                    //}
                    //Console.WriteLine("File decrypted");
                    //serverSocket.SendFile(filePath);


                    byte[] buff = Encoding.UTF8.GetBytes($"F~{file.Name}~{file.Length}~"); // Служебрный пакет
                    serverSocket.Send(buff);
                    serverSocket.SendFile(encriptedFilePath);
                    Console.WriteLine($"Файл отправлен успешно! Вес файла: {file.Length} Байт");
                }
                else Console.WriteLine("Файл не найден");
            }

            /// <summary>
            /// Метод регистрации клиента в системе
            /// </summary>
            static public void Registration()
            {
                string login = "login122";
                string password = "password122";
                byte[] buff = Kuznechik.Encript(Encoding.UTF8.GetBytes($"R~{login}~{password}~"), bytesMasterKey);
                serverSocket.Send(buff);
            }
            /// <summary>
            /// Метод аутентификации клиента в системе
            /// </summary>
            static public void Auth()
            {
                string login = "login122";
                string password = "password122";
                byte[] buff = Kuznechik.Encript(Encoding.UTF8.GetBytes($"A~{login}~{password}~"), bytesMasterKey);
                serverSocket.Send(buff);
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

        static void Main(string[] args)
        {      
            Client.StartClient();
            //Client.SendFile();
            Client.Registration();
            Client.Auth();
            //Thread.Sleep(300);
            //Client.SendMessage("привет");
            //Thread.Sleep(300);
            //Client.SendMessage("мир");
            //Thread.Sleep(300);
            //Client.SendMessage("Как ваши дела?");
            //Thread.Sleep(300);
            //Client.SendMessage("world");
            //Thread.Sleep(300);
            //Client.SendMessage("qwerqwe");
            //Client.DisconnectFromServer();
            //Thread.Sleep(300);
            //Client.SendMessage("я отключился");


        }
    }
}
