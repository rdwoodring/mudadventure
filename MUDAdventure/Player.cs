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
    class Player
    {
        private bool connected;
        private string name;
        private Server myServer;
        private TcpClient tcpClient;
        NetworkStream clientStream;
        ASCIIEncoding encoder;

        public Player(TcpClient client, Server server)
        {
            this.tcpClient = client;
            this.myServer = server;
        }

        public void initialize(object e)
        {
            this.clientStream = tcpClient.GetStream();
            this.encoder = new ASCIIEncoding();

            this.myServer.
        }
    }
}
