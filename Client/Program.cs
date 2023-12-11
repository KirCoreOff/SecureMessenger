using System.Numerics;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;
using System.Net.Http;
using System;
using System.Reflection;
using System.Drawing;

namespace Messenger
{
    class Program
    {
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
