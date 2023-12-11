using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Linq;

namespace Messenger
{
    class Program
    {
        static class Server
        {
            static string masterKey = "01234567890123456789012345678901"; // Ключ шифрования
            static byte[] bytesMasterKey = Encoding.UTF8.GetBytes(masterKey);
            const int BUFF_SIZE = 1024;
            // Настройка Сокета для подключения
            static IPAddress IP;
            static int port;
            static IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            static IPAddress ipAddr = ipHost.AddressList[0];
            static IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11000);
            static Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            static private List<Thread> threads = new List<Thread>(); // Список потоков для работы с клиентами
            static private Dictionary<Socket, bool> clients = new Dictionary<Socket, bool>(); // Клиентские сокеты с меткой авторизации

            static MySqlConnection sql;
            /// <summary>
            /// Метод запуска приложения сервера
            /// </summary>
            static public void StartServer()
            {
                // Подключение к базе данных
                try
                {
                    Console.WriteLine("Подключение к базе данных...");
                    sql = DBConnect();
                    sql.Open();
                    Console.WriteLine("База данных подключена!");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Ошибка: " + e.Message);
                }
                IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName()); // Информация об IP-адресах и имени машины, на которой запущено приложение
                IPAddress IP = hostEntry.AddressList[0]; // IP-адрес, который будет указан при создании сокета
                int Port = 11000;  // Порт, который будет указан при создании сокета
                // Настройка сокета сервара
                try
                {
                    sListener.Bind(ipEndPoint);
                    sListener.Listen(10);
                    threads.Clear();
                    threads.Add(new Thread(ReceiveMessage)); // Запуск поктока на работу с подключенным клиентом
                    threads[threads.Count - 1].Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            /// <summary>
            /// Метод подключения к базе данных
            /// </summary>
            /// <returns>Коннет с базой данных</returns>
            static MySqlConnection DBConnect()
            {
                string host = "localhost";
                int port = 3306;
                string database = "securemessenger";
                string username = "root";
                string password = "root";
                string conectData = $"Server={host};Database={database};User ID={username};Password={password};";
                return new MySqlConnection(conectData);
            }
            /// <summary>
            /// Метод работы с клиентами
            /// </summary>
            static void ReceiveMessage()
            {
                while (true)
                {
                    Console.WriteLine("Ожидаем соединение через порт {0}", ipEndPoint);
                    Socket clientSocket = sListener.Accept(); // Получение ссылки на клиентский сокет
                    if (!clients.ContainsKey(clientSocket))
                        clients.Add(clientSocket, false);
                    threads.Add(new Thread(ReadMessages)); // Создание и запуск потока, обслуживающий конкретный клиентский сокет
                    threads[threads.Count - 1].Start(clientSocket);
                }
            }
            /// <summary>
            /// Метод получения и обработки пакетов от клиента
            /// </summary>
            /// <param name="ClientSock">Сокет конкретного клиента</param>
            static void ReadMessages(object ClientSock)
            {
                bool clientIsConnected = true;
                while (clientIsConnected)
                {
                    byte[] buff = new byte[BUFF_SIZE];     // Буфер прочитанных из сокета байтов
                    ((Socket)ClientSock).Receive(buff);    // Получаем последовательность байтов из сокета в буфер buff
                    string[] infoPackage = Encoding.UTF8.GetString(Kuznechik.Decrypt(buff, bytesMasterKey)).Trim('\0').Split('~');
                    // Обработка пакетов только авторизаованных клиентов, или клиентов, проходящих регистрацию или аутентификацию
                    if (infoPackage[0] != "A" && infoPackage[0] != "R" && clients[(Socket)ClientSock] == false)
                        continue;
                    if (infoPackage[0] == "F") // Метка получения файла
                    {
                        string fileName = infoPackage[1];
                        int bytesReceived;
                        // Содание файла для записи передаваемых зашифрованных байтов файла
                        using (FileStream file = File.OpenWrite(fileName))
                        {
                            // Определение количества необходимых пакетов
                            int packages = int.Parse(infoPackage[2]) / BUFF_SIZE;
                            if (long.Parse(infoPackage[2]) % BUFF_SIZE != 0)
                                packages++;
                            // Запись зашифрованных байтов в файл
                            for (int i = 0; i < packages; i++)
                            {
                                bytesReceived = ((Socket)ClientSock).Receive(buff);
                                file.Write(buff, 0, bytesReceived);
                            }
                            Console.WriteLine($"Файл {fileName} Получен успешно! Вес файла: {file.Length} Байт");
                            Thread.Sleep(500);
                        }
                        //// Открытие полученного зашифрованного файла
                        //using (FileStream sourceStream = File.OpenRead(fileName))
                        //{
                        //    // Создание расшифрованного файла
                        //    using (FileStream destinationStream = File.Create("dec.jpg"))
                        //    {
                        //        // Создаем буфер для чтения и записи данных
                        //        byte[] buffer = new byte[BUFF_SIZE];
                        //        int bytesRead;
                        //        // Расшифровка байтов из полученного файла и запись их в созданный
                        //        while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                        //        {
                        //            byte[] encryptedBytes = Kuznechik.Decrypt(buffer, bytesMasterKey);
                        //            destinationStream.Write(encryptedBytes, 0, bytesRead);
                        //        }
                        //    }
                        //}
                        SendFile(fileName); // Отправка зашифрованного файла всем клиентам
                    }
                    else if (infoPackage[0] == "M") // Метка получения сообщения
                    {
                        Console.WriteLine(infoPackage[1].Trim('\0'));
                        SendMessage(buff);
                    }
                    else if (infoPackage[0] == "R") // Метка решистрации клиента
                    {
                        string command = $"SELECT * FROM test_users WHERE login = \"{infoPackage[1]}\"";
                        // Поиск в бд клиента с указанным логином
                        using (MySqlCommand cmd = new MySqlCommand(command, sql))
                        {
                            // Параметризованный запрос для предотвращения SQL-инъекций
                            cmd.Parameters.AddWithValue("@login", "\"infoPackage[1]\"");
                            // Получение такеого клиента
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                                if (reader.Read())
                                {
                                    string login = reader.GetString("login");
                                    Console.WriteLine($"Пользователь {login} уже существует!");
                                    ((Socket)ClientSock).Send(Kuznechik.Encript(
                                            Encoding.UTF8.GetBytes($"M~Пользователь {login} уже существует!~"),
                                            bytesMasterKey));
                                    continue; // Выход из процесса регистрации, если пользователь с таким логином уже существует
                                }
                        }
                        // Получение Стрибог-хэша пароля
                        string hashPassword = Encoding.UTF8.GetString(
                            Streebog.HashFunc(Encoding.UTF8.GetBytes(infoPackage[2]), 0, 0));
                        command = $"Insert into test_users values(\"{infoPackage[1]}\", \"{hashPassword}\")";
                        // Создание записи в бд с данными регистрирующегося клиента
                        using (MySqlCommand cmd = new MySqlCommand(command, sql))
                        {
                            // Выполнение запроса в бд
                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                Console.WriteLine("Пользователь зарегистрирован.");
                                ((Socket)ClientSock).Send(Kuznechik.Encript(
                                            Encoding.UTF8.GetBytes("M~Вы успешно зарегистрировались!~"),
                                            bytesMasterKey));
                            }
                            else
                            {
                                Console.WriteLine("Регистрация завершилась с ошибкой.");
                                ((Socket)ClientSock).Send(Kuznechik.Encript(
                                            Encoding.UTF8.GetBytes("M~Регистрация завершилась с ошибкой.~"),
                                            bytesMasterKey));
                            }
                        }
                        //command = "select * from test_users";
                        //// Получение всех записей  в бд
                        //using (MySqlCommand selectCmd = new MySqlCommand(command, sql))
                        //{
                        //    using (MySqlDataReader reader = selectCmd.ExecuteReader())
                        //    {
                        //        while (reader.Read())
                        //        {
                        //            string login = reader.GetString("login");
                        //            string password = reader.GetString("password");
                        //            Console.WriteLine($"login: {login}, password: {password}");
                        //        }
                        //    }
                        //}
                    }
                    else if (infoPackage[0] == "A") // Метка аутентификации клиента
                    {
                        string command = $"SELECT * FROM test_users WHERE login = \"{infoPackage[1]}\"";
                        // Посик в бд клиента с указанным логином
                        using (MySqlCommand cmd = new MySqlCommand(command, sql))
                        {
                            // Параметризованный запрос для предотвращения SQL-инъекций
                            cmd.Parameters.AddWithValue("@login", "\"infoPackage[1]\"");
                            // Проверка полученного пароля для аутентификации с хранящимся в бд
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                bool userExist = false;
                                if (reader.Read())
                                {
                                    userExist = true;
                                    // Получение шэша паоля из бд для сравнения
                                    string storedPassword = reader.GetString("password");
                                    // Получение Стрибог-хэша пароля
                                    string hashPassword = Encoding.UTF8.GetString(
                                        Streebog.HashFunc(Encoding.UTF8.GetBytes(infoPackage[2]), 0, 0));
                                    // Сравнение хэшей пароля
                                    if (storedPassword == hashPassword)
                                    {
                                        clients[(Socket)ClientSock] = true;
                                        Console.WriteLine("Пользователь авторизировался");
                                        ((Socket)ClientSock).Send(Kuznechik.Encript(
                                            Encoding.UTF8.GetBytes("M~Вы успешно авторизировались~"),
                                            bytesMasterKey));
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Неудачная попытка входа в аккаунт {infoPackage[1]}");
                                        ((Socket)ClientSock).Send(Kuznechik.Encript(
                                            Encoding.UTF8.GetBytes("M~Неверный пароль~"),
                                            bytesMasterKey));
                                    }
                                }
                                if (!userExist)
                                {
                                    Console.WriteLine($"Попытка входа в несуществующий аккаунт {infoPackage[1]}");
                                    ((Socket)ClientSock).Send(Kuznechik.Encript(
                                            Encoding.UTF8.GetBytes("M~Такого аккаунта не существует~"),
                                            bytesMasterKey));
                                }
                            }
                        }
                    }
                    else if (infoPackage[0] == "Q")
                    {
                        Console.WriteLine($"Клиент {(Socket)ClientSock} отключился");
                        ((Socket)ClientSock).Send(Kuznechik.Encript(
                                            Encoding.UTF8.GetBytes("M~До новых встреч!~"),
                                            bytesMasterKey));
                        clientIsConnected = false;
                    }
                    Thread.Sleep(500);
                }
            }
            /// <summary>
            /// Метод отправки полученного файла всем клиентам
            /// </summary>
            /// <param name="fileName">Имя файла</param>
            static void SendFile(string fileName)
            {
                Console.WriteLine("Отправка файла клиентам...");
                byte[] buff = new byte[BUFF_SIZE];
                // Получение списка авторизованных клиентов
                var authClient = clients.Where(kv => kv.Value == true).Select(kv => kv.Key).ToList();
                // Отправка файла авторизованным клиентам
                foreach (Socket client in authClient)
                {
                    FileInfo file = new FileInfo(fileName);
                    buff = Encoding.UTF8.GetBytes($"F~{file.Name}~{file.Length}~");
                    client.Send(buff);
                    client.SendFile(fileName);
                    Console.WriteLine($"Файл успешно отправлен! Вес файла: {file.Length} Байт");
                }
            }
            /// <summary>
            /// Метод отправки полученного сообщения всем клиентам
            /// </summary>
            /// <param name="buff">Байты сообщения в кодировке UTF8</param>
            static void SendMessage(byte[] buff)
            {
                // Получение списка авторизованных клиентов
                var authClient = clients.Where(kv => kv.Value == true).Select(kv => kv.Key).ToList();
                // Отправка сообщения авторизованным клиентам
                foreach (Socket client in authClient)
                {
                    client.Send(buff);
                }
            }
        }

        static void Main(string[] args)
        {
            Server.StartServer(); // Запуск сервера
        }
    }
}
