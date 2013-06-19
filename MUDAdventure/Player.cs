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
        public event EventHandler<PlayerMovedEventArgs> PlayerMoved;

        private bool connected;
        private string name;
        private Server myServer;
        private TcpClient tcpClient;
        NetworkStream clientStream;
        ASCIIEncoding encoder;
        private int x, y;

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
            
            this.writeToClient("Welcome to my MUD\r\nWhat is your name, traveller? ");

            string tempName = readFromClient();
            if (tempName != null && tempName != "")
            {
                this.name = tempName;
                this.connected = true;
                this.OnPlayerConnected(new PlayerConnectedEventArgs(this.name));

                this.myServer.players.CollectionChanged += playerListUpdated;
            }

            //TODO: replace with loading player's location from DB
            this.x = 0;
            this.y = 0;

            this.InputLoop();
        }

        private void InputLoop()
        {
            string input;

            while (connected)
            {
                input = this.readFromClient();

                this.ParseInput(input);
            }
        }

        private void ParseInput(string input)
        {
            if (input.ToLower() == "n" || input.ToLower() == "s" || input.ToLower() == "e" || input.ToLower() == "w")
            {
                this.Move(input.ToLower());
            }
            else if (input.ToLower() == "exit")
            {
                //TODO: implement event for disconnect so Server can update player list
                this.Disconnect();
            }
            else
            {
                this.writeToClient("Unrecognized command.");
            }
        }

        private void Disconnect()
        {
            this.writeToClient("Disconnecting...");

            this.connected = false;

            this.clientStream.Close();
            this.tcpClient.Close();
        }

        protected virtual void OnPlayerConnected(PlayerConnectedEventArgs e)
        {
            EventHandler<PlayerConnectedEventArgs> handler = this.PlayerConnected;

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
                buffer = this.encoder.GetBytes(message + "\r\n");

                this.clientStream.Write(buffer, 0, buffer.Length);
                this.clientStream.Flush();
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
                    bytesRead = this.clientStream.Read(message, 0, 4096);
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

        private void Move(string dir)
        {
            //TODO: add movement logic here

            int oldx, oldy;
            oldx = this.x;
            oldy = this.y;

            switch (dir)
            {
                case "n":
                    y++;
                    break;
                case "s":
                    y--;
                    break;
                case "e":
                    x++;
                    break;
                case "w":
                    x--;
                    break;
            }

            this.OnPlayerMoved(new PlayerMovedEventArgs(this.x, this.y, oldx, oldy, this.name, dir));
        }

        protected virtual void OnPlayerMoved(PlayerMovedEventArgs e)
        {
            EventHandler<PlayerMovedEventArgs> handler = this.PlayerMoved;

            if (handler != null)
            {
                handler(this, e);
            }
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
                    player.PlayerConnected += this.HandlePlayerConnected;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Player player in e.OldItems)
                {
                    this.writeToClient(player.getName() + " has disconnected.");
                }
            }
        }

        private void HandlePlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            //write to the client to let them know someone else has connected
            this.writeToClient(e.Name + " has connected.");

            //subscribe to all the other player's events the player will need to know about
            foreach (Player player in this.myServer.players)
            {
                if (player.getName() == e.Name)
                {
                    player.PlayerMoved += HandlePlayerMoved;
                }
            }
        }

        private void HandlePlayerMoved(object sender, PlayerMovedEventArgs e)
        {
            if( e.X == this.x && e.Y == this.y)
            {
                writeToClient(e.Name + " has entered the room.");
            }

            if (e.OldX == this.x && e.OldY == this.y)
            {
                writeToClient(e.Name + " has left the room.");
            }
        }
    }
}
