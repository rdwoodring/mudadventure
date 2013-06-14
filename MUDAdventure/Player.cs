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
        public event EventHandler<PlayerConnectedEventArgs> PlayerConnected;

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
            this.name = null;
        }

        public string getName()
        {
            return this.name;
        }

        public void initialize(object e)
        {
            this.clientStream = tcpClient.GetStream();
            this.encoder = new ASCIIEncoding();            
            
            writeToClient("Welcome to my MUD\r\nWhat is your name, traveller? ");

            string tempName = readFromClient();
            if (tempName != null && tempName != "")
            {
                this.name = tempName;
                this.connected = true;
                this.OnPlayerConnected(new PlayerConnectedEventArgs(this.name));

                this.myServer.players.CollectionChanged += playerListUpdated;
            }

        }

        protected virtual void OnPlayerConnected(PlayerConnectedEventArgs e)
        {
            EventHandler<PlayerConnectedEventArgs> handler = PlayerConnected;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void writeToClient(string message)
        {
            byte[] buffer;

            if (message != null)
            {
                buffer = this.encoder.GetBytes(message);

                this.clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();
            }
        }

        private string readFromClient()
        {
            byte[] message = new byte[4096];
            int bytesRead;
            string finalMessage = "";

            while (!finalMessage.EndsWith("\r\n"))
            {
                bytesRead = 0;

                //try reading from the client stream
                try
                {
                    bytesRead = clientStream.Read(message, 0, 4096);
                    finalMessage += this.encoder.GetString(message, 0, bytesRead);
                }
                catch (Exception e)
                {
                    Console.WriteLine(this.name + e.ToString());
                }
            }

            //Console.WriteLine(finalMessage.TrimEnd('\r','\n') + " has connected.");
            return finalMessage.TrimEnd('\r', '\n');
        }

        /**************************************************************************/
        /*                       EVENT HANDLERS                                   */
        /**************************************************************************/


        private void playerListUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Player player in e.NewItems)
                {
                    player.PlayerConnected += HandlePlayerConnected;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Player player in e.OldItems)
                {
                    writeToClient(player.getName() + " has disconnected.");
                }
            }
        }

        private void HandlePlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            writeToClient(e.Name + " has connected.");
        }
    }
}
