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

namespace Messenger
{
    class Program
    {
        static class Server
        {
            static string masterKey = "01234567890123456789012345678901";
            const int BUFF_SIZE = 1024;
            static IPAddress IP;
            static int port;
            static IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            static IPAddress ipAddr = ipHost.AddressList[0];
            static IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11000);
            static Socket sListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            static private List<Thread> threads = new List<Thread>();      // список потоков приложения (кроме родительского

            static List<Socket> clients = new List<Socket>();
            static MySqlConnection sql;
            static public void StartServer()
            {
                try
                {
                    Console.WriteLine("Openning Connection ...");
                    sql = DBConnect();
                    sql.Open();
                    Console.WriteLine("DataBase connected!");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
                IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());    // информация об IP-адресах и имени машины, на которой запущено приложение
                IPAddress IP = hostEntry.AddressList[0];                        // IP-адрес, который будет указан при создании сокета
                int Port = 11000;                                                // порт, который будет указан при создании сокета

                try
                {
                    sListener.Bind(ipEndPoint);
                    sListener.Listen(10);
                    threads.Clear();
                    threads.Add(new Thread(ReceiveMessage));
                    threads[threads.Count - 1].Start();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

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

            static void ReceiveMessage()
            {
                while (true)
                {
                    Console.WriteLine("Ожидаем соединение через порт {0}", ipEndPoint);

                    // Программа приостанавливается, ожидая входящее соединение
                    Socket clientSocket = sListener.Accept();           // получаем ссылку на очередной клиентский сокет
                    if (!clients.Contains(clientSocket))
                        clients.Add(clientSocket);
                    threads.Add(new Thread(ReadMessages));          // создаем и запускаем поток, обслуживающий конкретный клиентский сокет
                    threads[threads.Count - 1].Start(clientSocket);

                }

                static void ReadMessages(object ClientSock)
                {
                    string msg = "";        // полученное сообщение
                    //byte[] decryptedBytes = new byte[1024];
                    // входим в бесконечный цикл для работы с клиентским сокетом
                    while (true)
                    {
                        byte[] buff = new byte[BUFF_SIZE];                           // буфер прочитанных из сокета байтов
                        ((Socket)ClientSock).Receive(buff);                     // получаем последовательность байтов из сокета в буфер buff

                        string[] infoPackage = Encoding.ASCII.GetString(Kuznechik.Decrypt(buff, Encoding.ASCII.GetBytes(masterKey))).Trim('\0').Split('~');

                        if (infoPackage[0] == "F")
                        {
                            string fileName = infoPackage[1];
                            int bytesReceived;
                            using (FileStream file = File.OpenWrite(fileName))
                            {
                                int packages = int.Parse(infoPackage[2]) / BUFF_SIZE;
                                if (long.Parse(infoPackage[2]) % BUFF_SIZE != 0)
                                    packages++;
                                for (int i = 0; i < packages; i++)
                                {
                                    bytesReceived = ((Socket)ClientSock).Receive(buff);
                                    file.Write(buff, 0, bytesReceived);
                                }
                                Console.WriteLine($"File {fileName} recived successful! Length = {file.Length}");

                                Thread.Sleep(1000);
                            }
                            using (FileStream sourceStream = File.OpenRead(fileName))
                            {
                                // Создаем или перезаписываем файл назначения
                                using (FileStream destinationStream = File.Create("dec.jpg"))
                                {
                                    // Создаем буфер для чтения и записи данных
                                    byte[] buffer = new byte[BUFF_SIZE];
                                    int bytesRead;

                                    // Читаем данные из исходного файла и записываем их в файл назначения
                                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        byte[] encryptedBytes = Kuznechik.Decrypt(buffer, Encoding.ASCII.GetBytes(masterKey));
                                        destinationStream.Write(encryptedBytes, 0, bytesRead);
                                    }
                                }
                            }
                            SendFile(fileName);
                        }
                        else if (infoPackage[0] == "M")
                        {
                            Console.WriteLine(infoPackage[1].Trim('\0'));
                            SendMessage(buff);
                        }
                        else if (infoPackage[0] == "R")
                        {
                            string command = $"SELECT * FROM test_users WHERE login = \"{infoPackage[1]}\"";
                            using (MySqlCommand cmd = new MySqlCommand(command, sql))
                            {
                                // Параметризованный запрос для предотвращения SQL-инъекций
                                cmd.Parameters.AddWithValue("@login", "\"infoPackage[1]\"");
                                using (MySqlDataReader reader = cmd.ExecuteReader())
                                    if (reader.Read())
                                    {
                                        Console.WriteLine("Пользователь с таким логином уже существует!");
                                        ((Socket)ClientSock).Send(Kuznechik.Encript(
                                                Encoding.ASCII.GetBytes("~M~Пользователь с таким логином уже существует!~"),
                                                Encoding.ASCII.GetBytes(masterKey)));
                                        break;
                                    }
                            }
                            string hashPassword = Encoding.ASCII.GetString(Streebog.HashFunc(Encoding.ASCII.GetBytes(infoPackage[2]), 0, 0));
                            command = $"Insert into test_users values(\"{infoPackage[1]}\", \"{hashPassword}\")";
                            using (MySqlCommand cmd = new MySqlCommand(command, sql))
                            {
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    Console.WriteLine("Пользователь зарегистрирован.");
                                    ((Socket)ClientSock).Send(Kuznechik.Encript(
                                                Encoding.ASCII.GetBytes("~M~Вы успешно зарегистрировались!~"),
                                                Encoding.ASCII.GetBytes(masterKey)));
                                }
                                else
                                {
                                    Console.WriteLine("Регистрация завершилась с ошибкой.");
                                    ((Socket)ClientSock).Send(Kuznechik.Encript(
                                                Encoding.ASCII.GetBytes("M~Регистрация завершилась с ошибкой.~"),
                                                Encoding.ASCII.GetBytes(masterKey)));
                                }
                            }

                            command = "select * from test_users";
                            using (MySqlCommand selectCmd = new MySqlCommand(command, sql))
                            {
                                using (MySqlDataReader reader = selectCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        // Чтение данных из каждой строки результата запроса
                                        string value1 = reader.GetString("login");
                                        string value2 = reader.GetString("password");

                                        // Используйте полученные значения по вашему усмотрению
                                        Console.WriteLine($"login: {value1}, password: {value2}");
                                    }
                                }
                            }
                        }
                        else if (infoPackage[0] == "A")
                        {
                            string command = $"SELECT * FROM test_users WHERE login = \"{infoPackage[1]}\"";
                            using (MySqlCommand cmd = new MySqlCommand(command, sql))
                            {
                                // Параметризованный запрос для предотвращения SQL-инъекций
                                cmd.Parameters.AddWithValue("@login", "\"infoPackage[1]\"");
                                using (MySqlDataReader reader = cmd.ExecuteReader())
                                {
                                    bool userExist = false;
                                    if (reader.Read())
                                    {
                                        userExist = true;
                                        string storedPassword = reader.GetString("password");
                                        string hashPassword = Encoding.ASCII.GetString(Streebog.HashFunc(Encoding.ASCII.GetBytes(infoPackage[2]), 0, 0));

                                        if (storedPassword == hashPassword)
                                        {
                                            Console.WriteLine("Пользователь авторизировался");
                                            ((Socket)ClientSock).Send(Kuznechik.Encript(
                                                Encoding.ASCII.GetBytes("M~Вы успешно авторизировались~"),
                                                Encoding.ASCII.GetBytes(masterKey)));
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Неудачная попытка входа в аккаунт {infoPackage[1]}");
                                            ((Socket)ClientSock).Send(Kuznechik.Encript(
                                                Encoding.ASCII.GetBytes("M~Неверный пароль~"),
                                                Encoding.ASCII.GetBytes(masterKey)));
                                        }
                                    }
                                    if (!userExist)
                                    {
                                        Console.WriteLine($"Попытка Выхода в несуществующий аккаун {infoPackage[1]}");
                                        ((Socket)ClientSock).Send(Kuznechik.Encript(
                                                Encoding.ASCII.GetBytes("M~Такого аккаунта не существует~"),
                                                Encoding.ASCII.GetBytes(masterKey)));
                                    }
                                }
                            }
                        }
                        Thread.Sleep(500);
                    }
                }

                static void SendFile(string fileName)
                {
                    Console.WriteLine("Sending file...");
                    byte[] buff = new byte[BUFF_SIZE];

                    foreach (Socket client in clients)
                    {
                        FileInfo file = new FileInfo(fileName);
                        buff = Encoding.ASCII.GetBytes($"F~{file.Name}~{file.Length}");
                        client.Send(buff);
                        client.SendFile(fileName);
                        Console.WriteLine($"File sended succsessful! Length = {file.Length}");
                    }

                }

                static void SendMessage(byte[] buff)
                {
                    //byte[] buff = Encoding.ASCII.GetBytes(msg);
                    foreach (Socket client in clients)
                    {
                        client.Send(buff);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            Server.StartServer();


            //Console.Write("Введите текст для зашифрования: ");
            //string textToEncrypt = Console.ReadLine();
            //string masterKey = "01234567890123456789012345678901";  //Пароль должен быть 256 бит (32 символа)
            //byte[] encryptedBytes = Kuznechik.Encript(Encoding.Default.GetBytes(textToEncrypt), Encoding.Default.GetBytes(masterKey)); //Получение массива байт зашифрованного файла
            //string encryptedText = Encoding.Default.GetString(encryptedBytes);
            //Console.WriteLine("Зашифрованный текст: " + encryptedText);


            //byte[] decryptedBytes = Kuznechik.Decrypt(encryptedBytes, Encoding.Default.GetBytes(masterKey)); //Получение массива байт расшифрованного файла
            //string decryptedText = Encoding.Default.GetString(decryptedBytes);
            //Console.WriteLine("Расшифрованный текст: " + decryptedText);

        }

    }
}
