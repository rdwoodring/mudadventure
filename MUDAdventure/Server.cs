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
        private List<Player> players = new List<Player>();

        public ObservableCollection<string> chatQueue = new ObservableCollection<string>();

        public Server()
        {
            this.tcpListener = new TcpListener(IPAddress.Parse("0.0.0.0"), 3000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                TcpClient client = this.tcpListener.AcceptTcpClient();

                Player player = new Player(client, this);                                

                Thread clientThread = new Thread(new ParameterizedThreadStart(player.initialize));
                clientThread.Start();

                players.Add(player);
            }
        }
    }
}
