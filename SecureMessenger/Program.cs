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
using Org.BouncyCastle.Cms;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Messenger
{
    class Program
    {
        static class Server
        {
            #region Настроки
            static string masterKey = "01234567890123456789012345678901"; // Ключ шифрования
            static byte[] bytesMasterKey = Encoding.UTF8.GetBytes(masterKey);
            static Kuznechik kuznechik = new Kuznechik(bytesMasterKey);
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
            #endregion

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
                string clientLogin = "";
                bool clientIsConnected = true;
                while (clientIsConnected)
                {
                    byte[] buff = new byte[BUFF_SIZE];     // Буфер прочитанных из сокета байтов
                    // Получение сообщения от сервера
                    try
                    {
                        ((Socket)ClientSock).Receive(buff); // Получаем последовательность байтов из сокета в буфер buff
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Клиент {clientLogin} разорвал подключение");
                        SendMessage(kuznechik.Encript(Encoding.UTF8.GetBytes(
                            $"M~Система~{clientLogin} отключился~{DateTime.Now.ToShortTimeString()}~")), (Socket)ClientSock);
                        clientIsConnected = false;
                    }
                    // Расшифровка и разбиение пакета на информациронные части 
                    string[] infoPackage = Encoding.UTF8.GetString(kuznechik.Decrypt(buff)).Trim('\0').Split('~');
                    // Обработка пакетов только авторизаованных клиентов, или клиентов, проходящих регистрацию или аутентификацию
                    if (infoPackage[0] != "A" && infoPackage[0] != "R" && clients[(Socket)ClientSock] == false)
                        continue;
                    if (infoPackage[0] == "F") // Метка получения файла
                    {
                        //Console.WriteLine("Получил сообщение");
                        string fileName = infoPackage[2];
                        SendMessage(kuznechik.Encript(Encoding.UTF8.GetBytes(
                            $"M~{infoPackage[1]}~Отправил файл {infoPackage[2]}~{infoPackage[4]}~")), (Socket)ClientSock);
                        int bytesReceived;
                        // Содание файла для записи передаваемых зашифрованных байтов файла
                        using (FileStream file = File.OpenWrite(fileName))
                        {
                            // Определение количества необходимых пакетов
                            int packages = int.Parse(infoPackage[3]) / BUFF_SIZE;
                            if (long.Parse(infoPackage[3]) % BUFF_SIZE != 0)
                                packages++;
                            // Запись зашифрованных байтов в файл
                            for (int i = 0; i < packages; i++)
                            {
                                bytesReceived = ((Socket)ClientSock).Receive(buff);
                                file.Write(buff, 0, bytesReceived);
                            }
                            Console.WriteLine($"Файл {fileName} Получен успешно! Вес файла: {file.Length} Байт");
                            byte[] encFilePath = kuznechik.Encript(Encoding.UTF8.GetBytes(fileName));
                            string command = $"insert into messages(senderLogin, FilePath, Timestamp)" +
                                $" values(\"{clientLogin}\", @encFilePath, NOW())";
                            using (MySqlCommand cmd = new MySqlCommand(command, sql))
                            {
                                cmd.Parameters.AddWithValue("@senderLogin", clientLogin);
                                cmd.Parameters.AddWithValue("@FilePath", encFilePath);
                                cmd.Parameters.Add("@encFilePath", MySqlDbType.Binary).Value = encFilePath;
                                cmd.ExecuteNonQuery();
                            }
                            Thread.Sleep(500);
                        }
                        SendFile(fileName, (Socket)ClientSock); // Отправка зашифрованного файла всем клиентам
                    }
                    else if (infoPackage[0] == "M") // Метка получения сообщения
                    {
                        Console.WriteLine(infoPackage[2].Trim('\0'));
                        byte[] encMessage = kuznechik.Encript(Encoding.UTF8.GetBytes(infoPackage[2]));
                        string command = $"insert into messages(senderLogin, messageText, Timestamp)" +
                            $" values(\"{clientLogin}\", @encMessage, NOW())";
                        using (MySqlCommand cmd = new MySqlCommand(command, sql))
                        {
                            cmd.Parameters.AddWithValue("@senderLogin", clientLogin);
                            cmd.Parameters.AddWithValue("@messageText", encMessage);
                            cmd.Parameters.Add("@encMessage", MySqlDbType.Binary).Value = encMessage;
                            cmd.ExecuteNonQuery();
                        }
                        SendMessage(buff, (Socket)ClientSock);
                    }
                    else if (infoPackage[0] == "R") // Метка решистрации клиента
                    {
                        string command = $"SELECT * FROM users WHERE login = \"{infoPackage[1]}\"";
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
                                    Console.WriteLine($"Попытка регистрации клиента, логин которого уже есть в системе");
                                    ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes($"S~2~")));
                                    continue; // Выход из процесса регистрации, если пользователь с таким логином уже существует
                                }
                        }
                        // Получение Стрибог-хэша пароля
                        byte[] hashPassword = Streebog.HashFunc(Encoding.UTF8.GetBytes(infoPackage[2]), 0, 0);
                        command = $"Insert into users values(\"{infoPackage[1]}\", @hashPassword)";
                        // Создание записи в бд с данными регистрирующегося клиента
                        using (MySqlCommand cmd = new MySqlCommand(command, sql))
                        {
                            // Выполнение запроса в бд
                            cmd.Parameters.Add("@hashPassword", MySqlDbType.Binary).Value = hashPassword;
                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                Console.WriteLine($"Пользователь {infoPackage[1]} зарегистрировался");
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~0~")));
                            }
                            else
                            {
                                Console.WriteLine("Регистрация завершилась с ошибкой.");
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~1~")));
                            }
                        }
                        //command = "select * from users";
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
                        string command = $"SELECT * FROM users WHERE login = \"{infoPackage[1]}\"";
                        // Посик в бд клиента с указанным логином
                        using (MySqlCommand cmd = new MySqlCommand(command, sql))
                        {
                            // Параметризованный запрос для предотвращения SQL-инъекций
                            cmd.Parameters.AddWithValue("@login", "\"infoPackage[1]\"");
                            // Проверка полученного пароля для аутентификации с хранящимся в бд
                            bool userExist = false;
                            byte[] storedPassword = null;
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    userExist = true;
                                    // Получение шэша паоля из бд для сравнения
                                    storedPassword = new byte[reader.GetBytes(reader.GetOrdinal("password"), 0, null, 0, int.MaxValue)];
                                    reader.GetBytes(reader.GetOrdinal("password"), 0, storedPassword, 0, storedPassword.Length);
                                }
                            }
                            // Получение Стрибог-хэша пароля
                            byte[] hashPassword = Streebog.HashFunc(Encoding.UTF8.GetBytes(infoPackage[2]), 0, 0);
                            // Сравнение хэшей пароля
                            if (storedPassword.SequenceEqual(hashPassword))
                            {
                                clients[(Socket)ClientSock] = true;
                                clientLogin = infoPackage[1];
                                Console.WriteLine($"Пользователь {clientLogin} авторизировался");
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~0~")));
                                SendMessage(kuznechik.Encript(Encoding.UTF8.GetBytes(
                                    $"M~Система~{clientLogin} подклюился!~{DateTime.Now.ToShortTimeString()}~")), (Socket)ClientSock);
                                SendMessageHistory((Socket)ClientSock);
                            }
                            else
                            {
                                Console.WriteLine($"Неудачная попытка входа в аккаунт {infoPackage[1]}");
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~1~")));
                            }
                            if (!userExist)
                            {
                                Console.WriteLine($"Попытка входа в несуществующий аккаунт {infoPackage[1]}");
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~2~")));
                            }
                        }
                    }
                }
            }
            /// <summary>
            /// Предоставленрие пользователю последних 5-ти сообщений
            /// </summary>
            /// <param name="clientSocket">клиентский сокет</param>
            static void SendMessageHistory(Socket clientSocket)
            {
                string command = "(SELECT  messageId, senderLogin, messageText, filePath, DATE_FORMAT(timestamp, '%H:%i') AS timestamp FROM Messages ORDER BY messageId DESC  LIMIT 5) ORDER BY messageId ASC;";
                // Получение всех записей  в бд
                using (MySqlCommand cmd = new MySqlCommand(command, sql))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string senderLogin = reader.GetString("senderLogin");
                            string message = "";
                            string filePath = "";
                            if (!reader.IsDBNull(reader.GetOrdinal("messageText")))
                            {
                                byte[] messageTextBytes = new byte[reader.GetBytes(reader.GetOrdinal("messageText"), 0, null, 0, int.MaxValue)];
                                reader.GetBytes(reader.GetOrdinal("messageText"), 0, messageTextBytes, 0, messageTextBytes.Length);
                                message = Encoding.UTF8.GetString(kuznechik.Decrypt(messageTextBytes)).Trim('\0');
                            }
                            else
                            {
                                byte[] filePathBytes = new byte[reader.GetBytes(reader.GetOrdinal("FilePath"), 0, null, 0, int.MaxValue)];
                                reader.GetBytes(reader.GetOrdinal("FilePath"), 0, filePathBytes, 0, filePathBytes.Length);
                                filePath = Encoding.UTF8.GetString(kuznechik.Decrypt(filePathBytes)).Trim('\0');
                            }    

                            string timestamp = reader.GetString("timestamp");
                            clientSocket.Send(kuznechik.Encript(
                                Encoding.UTF8.GetBytes($"M~{senderLogin}~{message}{filePath}~{timestamp}~")));
                            Thread.Sleep(100);
                        }
                    }
                }
            }
            /// <summary>
            /// Метод отправки полученного файла всем клиентам
            /// </summary>
            /// <param name="fileName">Имя файла</param>
            static void SendFile(string fileName, Socket senderSocket)
            {
                int i = 0;
                Console.WriteLine("Отправка файла клиентам...");
                byte[] buff = new byte[BUFF_SIZE];
                // Получение списка авторизованных клиентов
                var authClient = clients.Where(kv => kv.Value == true).Where(kv => kv.Key != senderSocket).Select(kv => kv.Key).ToList();
                // Отправка файла авторизованным клиентам
                foreach (Socket client in authClient)
                {
                    FileInfo file = new FileInfo(fileName);
                    buff = kuznechik.Encript(Encoding.UTF8.GetBytes($"F~{++i}{file.Name}~{file.Length}~"));
                    client.Send(buff);
                    client.SendFile(fileName);
                    Console.WriteLine($"Файл успешно отправлен! Вес файла: {file.Length} Байт");
                }
            }
            /// <summary>
            /// Метод отправки полученного сообщения всем клиентам
            /// </summary>
            /// <param name="buff">Байты сообщения в кодировке UTF8</param>
            static void SendMessage(byte[] buff, Socket senderSocket)
            {
                // Получение списка авторизованных клиентов
                var authClient = clients.Where(kv => kv.Value == true).Where(kv => kv.Key != senderSocket).Select(kv => kv.Key).ToList();
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
