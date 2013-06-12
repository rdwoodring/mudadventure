using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class Program
    {
        static void Main(string[] args)
        {
            string serverInput = "";
            
            Console.WriteLine("Booting server...");
            Server myServer = new Server();

            Console.WriteLine("Server started. Listening for connections...");

            while (serverInput != "exit")
            {
                serverInput = Console.ReadLine();
            }

            Console.WriteLine("Shutting down server... Press any key to continue");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
