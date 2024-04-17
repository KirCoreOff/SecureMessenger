using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Messenger
{
    class Program
    {
        static class Server
        {
            #region Настроки
            static string masterKey = Environment.GetEnvironmentVariable("$ENC_KEY");
            static byte[] bytesMasterKey = Encoding.UTF8.GetBytes(masterKey);
            static Kuznechik kuznechik = new Kuznechik(bytesMasterKey);
            const int BUFF_SIZE_FILE = 8192;
            const int BUFF_SIZE_MESSAGE = 512;
            // Настройка Сокета для подключения
            //static IPAddress IP;
            //static int port;
            //static IPHostEntry ipHost = Dns.GetHostEntry("");
            //static IPAddress ipAddr = ipHost.AddressList[10];
            //static IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11000);
            static Socket sListener;
            static IPEndPoint ipEndPoint;
            static public List<Thread> threads = new List<Thread>(); // Список потоков для работы с клиентами
            static private Dictionary<Socket, bool> clients = new Dictionary<Socket, bool>(); // Клиентские сокеты с меткой авторизации

            static MySqlConnection sql;
            #endregion

            /// <summary>
            /// Метод запуска приложения сервера
            /// </summary>
            static public void StartServer()
            {
                // Подключение к базе данных
                sql = DBConnect();

                IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName()); // Информация об IP-адресах и имени машины, на которой запущено приложение
                IPAddress IP = hostEntry.AddressList[0]; // IP-адрес, который будет указан при создании сокета
                int Port = 11000;  // Порт, который будет указан при создании сокета
                                   // Настройка сокета сервара

                if (File.Exists("config.bin"))
                {
                    string endPoint = File.ReadAllText("config.bin");
                    if (IPEndPoint.TryParse(endPoint, out ipEndPoint))
                    {
                        sListener = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        try
                        {
                            sListener.Bind(ipEndPoint);
                            sListener.Listen(10);
                            threads.Clear();
                            threads.Add(new Thread(ReceiveMessage)); // Запуск поктока на работу с подключенным клиентом
                            threads[threads.Count - 1].Start();
                        }
                        catch
                        {
                            Console.WriteLine("Выбранный порт занят.");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Некорректный адрес сервера в \"config.bin\".");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Не найден файл \"config.bin\".");
                    return;
                }

            }
            /// <summary>
            /// Метод подключения к базе данных
            /// </summary>
            /// <returns>Коннет с базой данных</returns>
            static MySqlConnection DBConnect()
            {
                try
                {
                    Console.WriteLine("Подключение к базе данных...");
                    string host = "localhost";
                    int port = 3306;
                    string database = "securemessenger";
                    string username = "root";
                    string password = "root";
                    string conectData = $"Server={host};User ID={username};Password={password};";
                    MySqlConnection connection = new MySqlConnection(conectData);
                    connection.Open();
                    Console.WriteLine("База данных подключена!");
                    try
                    {
                        string createDatabaseCommand = $"create database {database};";
                        using (MySqlCommand cmd = new MySqlCommand(createDatabaseCommand, connection))
                            cmd.ExecuteNonQuery();
                    }
                    catch (Exception) { }
                    finally
                    {
                        string selectDatabase = $"use {database};";
                        using (MySqlCommand cmd = new MySqlCommand(selectDatabase, connection))
                            cmd.ExecuteNonQuery();
                    }
                    try
                    {
                        string createUserTableCommand = $"CREATE TABLE `users` (`login` varchar(255) NOT NULL, `password` blob, PRIMARY KEY (`login`));";
                        using (MySqlCommand cmd = new MySqlCommand(createUserTableCommand, connection))
                            cmd.ExecuteNonQuery();

                    }
                    catch (Exception) { }
                    try
                    {
                        string createMessagesTableCommand = $"CREATE TABLE `messages` (`messageId` int NOT NULL AUTO_INCREMENT, `senderLogin` varchar(255) NOT NULL, `messageText` blob, `FilePath` blob, `timestamp` datetime NOT NULL, PRIMARY KEY (`messageId`), KEY `senderLogin` (`senderLogin`), CONSTRAINT `messages_ibfk_1` FOREIGN KEY (`senderLogin`) REFERENCES `users` (`login`));";
                        using (MySqlCommand cmd = new MySqlCommand(createMessagesTableCommand, connection))
                            cmd.ExecuteNonQuery();
                    }
                    catch (Exception) { }

                    return connection;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Ошибка: " + e.Message);
                    return null;
                }

            }
            /// <summary>
            /// Метод генерации аудита
            /// </summary>
            static void Audit(string systemEvent)
            {
                File.AppendAllText("audit.txt", $"{DateTime.Now} {systemEvent}\n");
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
                    byte[] buffFile = new byte[BUFF_SIZE_MESSAGE];     // Буфер прочитанных из сокета байтов
                    byte[] buffMsg = new byte[BUFF_SIZE_MESSAGE];     // Буфер прочитанных из сокета байтов
                    // Получение сообщения от сервера
                    try
                    {
                        ((Socket)ClientSock).Receive(buffMsg); // Получаем последовательность байтов из сокета в буфер buff
                    }
                    catch (Exception)
                    {
                        string systemEvent = $"Клиент {clientLogin} разорвал подключение";
                        Console.WriteLine(systemEvent);
                        Audit(systemEvent);
                        SendMessage(kuznechik.Encript(Encoding.UTF8.GetBytes(
                            $"M~Система~{clientLogin} отключился~{DateTime.Now.ToShortTimeString()}~")), (Socket)ClientSock);
                        clientIsConnected = false;
                    }
                    // Расшифровка и разбиение пакета на информациронные части 
                    string[] infoPackage = Encoding.UTF8.GetString(kuznechik.Decrypt(buffMsg)).Trim('\0').Split('~');
                    // Обработка пакетов только авторизаованных клиентов, или клиентов, проходящих регистрацию или аутентификацию
                    if (infoPackage[0] != "A" && infoPackage[0] != "R" && clients[(Socket)ClientSock] == false)
                        continue;
                    if (infoPackage[0] == "F") // Метка получения файла
                    {
                        //Console.WriteLine("Получил сообщение");
                        string fileName = infoPackage[2];
                        SendMessage(kuznechik.Encript(Encoding.UTF8.GetBytes(
                            $"M~{infoPackage[1]}~Отправил файл {infoPackage[2]}~{infoPackage[4]}~")), (Socket)ClientSock);
                        int bytesReceived = 0;
                        int currentBytesReceived = 0;

                        // Содание файла для записи передаваемых зашифрованных байтов файла
                        using (FileStream file = File.OpenWrite(fileName))
                        {
                            // Запись зашифрованных байтов в файл
                            while (bytesReceived < int.Parse(infoPackage[3]))
                            {
                                currentBytesReceived = ((Socket)ClientSock).Receive(buffFile);
                                file.Write(buffFile, 0, currentBytesReceived);
                                bytesReceived += currentBytesReceived;
                            }

                            byte[] encFilePath = kuznechik.Encript(Encoding.UTF8.GetBytes(fileName));
                            string command = $"insert into messages(senderLogin, FilePath, Timestamp)" +
                                $" values(\"{clientLogin}\", @encFilePath, NOW())";
                            using (MySqlCommand cmd = new MySqlCommand(command, sql))
                            {
                                cmd.Parameters.AddWithValue("@senderLogin", clientLogin);
                                cmd.Parameters.AddWithValue("@FilePath", encFilePath);
                                cmd.Parameters.Add("@encFilePath", MySqlDbType.Binary).Value = encFilePath;
                                int recordID = cmd.ExecuteNonQuery();
                                Audit($"Клиент {clientLogin} отправил файл с ID={cmd.LastInsertedId}");
                            }
                            Thread.Sleep(500);
                        }
                        SendFile(fileName, (Socket)ClientSock); // Отправка зашифрованного файла всем клиентам
                    }
                    else if (infoPackage[0] == "M") // Метка получения сообщения
                    {
                        byte[] encMessage = kuznechik.Encript(Encoding.UTF8.GetBytes(infoPackage[2]));
                        string command = $"insert into messages(senderLogin, messageText, Timestamp)" +
                            $" values(\"{clientLogin}\", @encMessage, NOW())";
                        using (MySqlCommand cmd = new MySqlCommand(command, sql))
                        {
                            cmd.Parameters.AddWithValue("@senderLogin", clientLogin);
                            cmd.Parameters.AddWithValue("@messageText", encMessage);
                            cmd.Parameters.Add("@encMessage", MySqlDbType.Binary).Value = encMessage;
                            cmd.ExecuteNonQuery();
                            Audit($"Клиент {clientLogin} отправил сообщение с ID={cmd.LastInsertedId}");
                        }
                        SendMessage(buffMsg, (Socket)ClientSock);
                    }
                    else if (infoPackage[0] == "R") // Метка регистрации клиента
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
                                    string systemEvent = $"Попытка регистрации клиента {login}. Такой пользователь уже существует";
                                    Console.WriteLine(systemEvent);
                                    Audit(systemEvent);
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
                                string systemEvent = $"Пользователь {infoPackage[1]} зарегистрировался";
                                Console.WriteLine(systemEvent);
                                Audit(systemEvent);

                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~0~")));
                            }
                            else
                            {
                                string systemEvent = "Регистрация завершилась с ошибкой";
                                Console.WriteLine(systemEvent);
                                Audit(systemEvent);
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~1~")));
                            }
                        }
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
                            if (storedPassword != null && storedPassword.SequenceEqual(hashPassword))
                            {
                                clients[(Socket)ClientSock] = true;
                                clientLogin = infoPackage[1];
                                string systemEvent = $"Пользователь {clientLogin} авторизовался";
                                Console.WriteLine(systemEvent);
                                Audit(systemEvent);
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~0~")));
                                SendMessage(kuznechik.Encript(Encoding.UTF8.GetBytes(
                                    $"M~Система~{clientLogin} подключился!~{DateTime.Now.ToShortTimeString()}~")), (Socket)ClientSock);
                                SendMessageHistory((Socket)ClientSock);
                            }
                            else if (!userExist)
                            {
                                string systemEvent = $"Попытка входа в несуществующий аккаунт {infoPackage[1]}";
                                Console.WriteLine(systemEvent);
                                Audit(systemEvent);
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~2~")));
                            }
                            else
                            {
                                string systemEvent = $"Неудачная попытка входа в аккаунт {infoPackage[1]}";
                                Console.WriteLine(systemEvent);
                                Audit(systemEvent);
                                ((Socket)ClientSock).Send(kuznechik.Encript(Encoding.UTF8.GetBytes("S~1~")));
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
                Console.WriteLine("Отправка файла клиентам...");
                byte[] buffMsg = new byte[BUFF_SIZE_MESSAGE];
                // Получение списка авторизованных клиентов
                var authClient = clients.Where(kv => kv.Value == true).Where(kv => kv.Key != senderSocket).Select(kv => kv.Key).ToList();
                // Отправка файла авторизованным клиентам
                foreach (Socket client in authClient)
                {
                    FileInfo file = new FileInfo(fileName);
                    buffMsg = kuznechik.Encript(Encoding.UTF8.GetBytes($"F~{file.Name}~{file.Length}~"));
                    client.Send(buffMsg);
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
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ShutDown);
        }
        static void ShutDown(object sender, EventArgs e)
        {
            Environment.Exit(0);
            //foreach (var thread in Server.threads)
            //{
            //    thread.();
            //}
        }
    }
}
