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
            this.connected = true;

            this.myServer.chatQueue.CollectionChanged += new NotifyCollectionChangedEventHandler(chatQueueUpdated);
            this.myServer.players.CollectionChanged += new NotifyCollectionChangedEventHandler(playerListUpdated);

            writeToClient("Welcome to my MUD\r\nWhat is your name, traveller? ");

            readFromClient();
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

        private void readFromClient()
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

            Console.WriteLine(finalMessage.TrimEnd('\r','\n') + " has connected.");
        }

        /**************************************************************************/
        /*                       EVENT HANDLERS                                   */
        /**************************************************************************/


        private void chatQueueUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            //TODO: add code to handle an update to the chat queue
        }

        private void playerListUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                //List<Player> players = new List<Player>();

                foreach (Player player in e.NewItems)
                {
                    writeToClient(player.getName() + " has connected.");
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (Player player in e.NewItems)
                {
                    writeToClient(player.getName() + " has connected.");
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
    }
}
