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
            static string masterKey = "01234567890123456789012345678901";
            const int BUFF_SIZE = 1024;
            static int port = 11000;
            static IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            static IPAddress ipAddr = ipHost.AddressList[0];
            static IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);

            static Socket serverSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            static public void StartClient()
            {
                serverSocket.Connect(ipEndPoint);
                Thread.Sleep(500);
                Thread th = new Thread(ReadMessage);
                th.Start();
            }

            static void ReadMessage()
            {               
                string msg;
                while (true)
                {
                    byte[] buff = new byte[BUFF_SIZE];
                    serverSocket.Receive(buff);                     // получаем последовательность байтов из сокета в буфер buff
                    string[] infoPackage = Encoding.ASCII.GetString(Kuznechik.Decrypt(buff, Encoding.ASCII.GetBytes(masterKey))).Trim('\0').Split('~');

                    if (infoPackage[0] == "F")
                    {
                        string fileName = infoPackage[1];
                        int bytesReceived;
                        using (FileStream file = File.OpenWrite($"1_{fileName}"))
                        {
                            int packages = int.Parse(infoPackage[2]) / BUFF_SIZE;
                            if (long.Parse(infoPackage[2]) % BUFF_SIZE != 0)
                                packages++;
                            for (int i = 0; i < packages; i++)
                            {
                                bytesReceived = serverSocket.Receive(buff);
                                file.Write(buff, 0, bytesReceived);
                            }
                            Console.WriteLine($"File {fileName} recived successful! Length = {file.Length}");
                            using (FileStream sourceStream = File.OpenRead($"1_{fileName}"))
                            {
                                // Создаем или перезаписываем файл назначения
                                using (FileStream destinationStream = File.Create($"d_{fileName}"))
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
                        }
                    }
                    else if (infoPackage[0] == "M")
                    {
                        Console.WriteLine(infoPackage[1].Trim('\0'));
                    }
                    //msg = Encoding.ASCII.GetString(buff).Trim('\0');     // выполняем преобразование байтов в последовательность символов
                    //byte[] decryptedBytes = Kuznechik.Decrypt(buff, Encoding.ASCII.GetBytes(masterKey));
                    //string decryptedText = Encoding.ASCII.GetString(decryptedBytes).Substring(0, Encoding.ASCII.GetString(decryptedBytes).IndexOf('\0'));

                    //msg = Encoding.ASCII.GetString(buff).Trim('\0');     // выполняем преобразование байтов в последовательность символов
                    //Console.WriteLine($"Полученный сообщение: {msg} длинной: {msg.Length}");
                    //byte[] decryptedBytes = Kuznechik.Decrypt(buff, Encoding.ASCII.GetBytes(masterKey));
                    //string decryptedText = Encoding.ASCII.GetString(decryptedBytes).Substring(0, Encoding.ASCII.GetString(decryptedBytes).IndexOf('\0'));


                    //Показываем данные на консоли
                    //Console.WriteLine($"расшифрованное сообщение: {decryptedText} длинной: {decryptedText.Length}");

                    // Показываем данные на консоли
                    //Console.WriteLine("Полученный текст: " + decryptedText + "\n\n");
                }
            }

            static public void SendMessage(string msg)
            {
                byte[] encryptedBytes = Kuznechik.Encript(Encoding.ASCII.GetBytes($"M~{msg}~"), Encoding.ASCII.GetBytes(masterKey));
                serverSocket.Send(encryptedBytes);
                //Console.WriteLine($"Соббщение: {msg} длинной: {msg.Length} закодировано в: {Encoding.ASCII.GetString(encryptedBytes)} длинной: {Encoding.ASCII.GetString(encryptedBytes).Length}");
                //byte[] decryptedBytes = Kuznechik.Decrypt(encryptedBytes, Encoding.ASCII.GetBytes(masterKey));
                //string decryptedText = Encoding.ASCII.GetString(decryptedBytes);
                //Console.WriteLine($"расшифрованное сообщение: {decryptedText} длинной: {decryptedText.Length}");

                //buff = Encoding.Default.GetBytes(msg);
                //serverSocket.Send(buff);
            }

            static public void SendFile() 
            {
                Console.WriteLine("Sending file...");
                string filePath = "smile.jpg";
                string encriptedFilePath = "encsmile.jpg";
                string decriptedFilePath = "decsmile.jpg";
                FileInfo file = new FileInfo(filePath);
                //string filePath = "test.txt";
                //string encriptedFilePath = "enctest.txt";
                if (File.Exists(filePath))
                {
                    using (FileStream sourceStream = File.OpenRead(filePath))
                    {
                        // Создаем или перезаписываем файл назначения
                        using (FileStream destinationStream = File.Create(encriptedFilePath))
                        {
                            // Создаем буфер для чтения и записи данных
                            byte[] buffer = new byte[BUFF_SIZE];
                            int bytesRead;

                            // Читаем данные из исходного файла и записываем их в файл назначения
                            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                byte[] encryptedBytes = Kuznechik.Encript(buffer, Encoding.ASCII.GetBytes(masterKey));
                                destinationStream.Write(encryptedBytes, 0, bytesRead);
                            }

                        }
                    }
                    Console.WriteLine("File created");

                    using (FileStream sourceStream = File.OpenRead(encriptedFilePath))
                    {
                        // Создаем или перезаписываем файл назначения
                        using (FileStream destinationStream = File.Create(decriptedFilePath))
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
                    Console.WriteLine("File decrypted");

                    //serverSocket.SendFile(filePath);
                    byte[] buff = Encoding.ASCII.GetBytes($"F~{file.Name}~{file.Length}~");
                    serverSocket.Send(buff);
                    serverSocket.SendFile(encriptedFilePath);
                    Console.WriteLine($"File sended succsessful! Length = {file.Length}");
                }
                else Console.WriteLine("File not exist");
            }

            static public void Registration()
            {
                string login = "login11";
                string password = "password11";
                byte[] buff = Kuznechik.Encript(Encoding.ASCII.GetBytes($"R~{login}~{password}~"), Encoding.ASCII.GetBytes(masterKey));
                serverSocket.Send(buff);
            }

            static public void Auth()
            {
                string login = "login11";
                string password = "password11";
                byte[] buff = Kuznechik.Encript(Encoding.ASCII.GetBytes($"A~{login}~{password}~"), Encoding.ASCII.GetBytes(masterKey));
                serverSocket.Send(buff);
            }
        }

        static void Main(string[] args)
        {      
            Client.StartClient();
            //Client.SendFile();
            //Client.Registration();
            //Client.Auth();
            Client.SendMessage("привет");
            //Client.SendMessage("world");

        }
    }
}
