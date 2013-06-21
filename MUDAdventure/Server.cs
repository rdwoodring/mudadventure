using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace MUDAdventure
{
    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private ObservableCollection<Player> players = new ObservableCollection<Player>();
        private static  Object playerlock = new Object();
        private Dictionary<string, Room> rooms = new Dictionary<string,Room>();

        public Server()
        {
            this.tcpListener = new TcpListener(IPAddress.Parse("0.0.0.0"), 3000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();

            //TODO: add code for loading rooms from DB
            //rooms.Add("123", new Room()); etc., etc.
            Room room = new Room("A Starting Place", "This is the starting room.  It is completely empty.", true, false, true, false, 0, 0, 0);
            rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                TcpClient client = this.tcpListener.AcceptTcpClient();

                Player player = new Player(client, ref players, rooms);                      

                Thread clientThread = new Thread(new ParameterizedThreadStart(player.initialize));
                clientThread.Start();

                player.PlayerConnected += HandlePlayerConnected;
                player.PlayerDisconnected += HandlePlayerDisconnected;
                lock (playerlock)
                {
                    players.Add(player);
                }
            }
        }

        private void HandlePlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            Console.WriteLine(e.Name + " has connected.");
        }

        private void HandlePlayerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            Console.WriteLine(e.Name + " has disconnected.");
        }
    }
}
