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
        public event EventHandler<PlayerDisconnectedEventArgs> PlayerDisconnected;

        private bool connected;
        private string name;
        private TcpClient tcpClient;
        NetworkStream clientStream;
        ASCIIEncoding encoder;
        private int x, y, z;
        private ObservableCollection<Player> players;
        private Dictionary<string, Room> rooms;

        private Object playerlock = new Object();

        public Player(TcpClient client, ref ObservableCollection<Player> playerlist, Dictionary<string, Room> roomlist)
        {
            this.tcpClient = client;
            this.name = null;
            this.players = playerlist;

            this.rooms = roomlist;
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
                lock (playerlock)
                {
                    this.players.CollectionChanged += playerListUpdated;
                }
            }

            //TODO: replace with loading player's location from DB
            this.x = 0;
            this.y = 0;
            this.z = 0;

            this.Look();

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

        private void Look()
        {
            Room currentRoom;
            this.rooms.TryGetValue(this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString(), out currentRoom);

            try
            {
                if (currentRoom != null)
                {
                    string exits = "";
                    if (currentRoom.NorthExit)
                    {
                        exits += "N ";
                    }

                    if (currentRoom.EastExit)
                    {
                        exits += "E ";
                    }

                    if (currentRoom.SouthExit)
                    {
                        exits += "S ";
                    }

                    if (currentRoom.WestExit)
                    {
                        exits += "W";
                    }

                    writeToClient(currentRoom.RoomName + "\r\n" + exits + "\r\n" + currentRoom.RoomDescription + "\r\n\r\n");
                }
                else
                {
                    writeToClient("An Empty Void\r\nThis room is a total void.  It is bereft of anything because, in fact, it does not exist.  You've reached the edge of the world... and fallen off.");
                }
            }
            catch (NullReferenceException e)
            {
                writeToClient(e.ToString());
            }
        }

        private void ParseInput(string input)
        {
            if (input.ToLower() == "n" || input.ToLower() == "s" || input.ToLower() == "e" || input.ToLower() == "w")
            {
                this.Move(input.ToLower());
            }
            else if (input.ToLower().Contains("look"))
            {
                this.Look();
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

            //unsubscribe from events
            lock (playerlock)
            {
                this.players.CollectionChanged -= this.playerListUpdated;
                foreach (Player player in this.players)
                {
                    player.PlayerMoved -= this.HandlePlayerMoved;
                    player.PlayerConnected -= this.HandlePlayerConnected;
                }
            }

            this.clientStream.Close();
            this.tcpClient.Close();

            this.OnPlayerDisconnected(new PlayerDisconnectedEventArgs(this.name));
        }

        protected virtual void OnPlayerConnected(PlayerConnectedEventArgs e)
        {
            EventHandler<PlayerConnectedEventArgs> handler = this.PlayerConnected;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnPlayerDisconnected(PlayerDisconnectedEventArgs e)
        {
            EventHandler<PlayerDisconnectedEventArgs> handler = this.PlayerDisconnected;

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

            int oldx, oldy, oldz;
            oldx = this.x;
            oldy = this.y;
            oldz = this.z;

            //temporary holder for current room.
            //TODO: maybe make this a class level variable since it seems to be used repeatedly
            Room currentRoom;

            //getting current room.
            this.rooms.TryGetValue(this.x + "," + this.y + "," + this.z, out currentRoom);

            //make sure the room we are in exists and is not null
            if (currentRoom != null)
            {
                //check the movement direction, and see if an exit is available that way.
                switch (dir)
                {
                    case "n":
                        if (currentRoom.NorthExit)
                        {
                            this.y++;
                        }
                        break;
                    case "s":
                        if (currentRoom.SouthExit)
                        {
                            this.y--;
                        }
                        break;
                    case "e":
                        if (currentRoom.EastExit)
                        {
                            this.x++;
                        }
                        break;
                    case "w":
                        if (currentRoom.WestExit)
                        {
                            this.x--;
                        }
                        break;
                }

                //if we have moved, let's look around the new room automatically and raise the OnPlayerMoved event
                if (this.x != oldx || this.y != oldy || this.z != oldz)
                {
                    this.Look();

                    this.OnPlayerMoved(new PlayerMovedEventArgs(this.x, this.y, oldx, oldy, this.name, dir));
                }
                //if we haven't moved, that means there wasn't an available exit in that direction.
                //let's tell the stupid player with an message
                else
                {
                    writeToClient("You cannot go that direction.");
                }
            }
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

            lock (playerlock)
            {
                //subscribe to all the other player's events the player will need to know about
                if (this.players.Contains((Player)sender))
                {
                    this.players[this.players.IndexOf((Player)sender)].PlayerMoved += this.HandlePlayerMoved;
                    this.players[this.players.IndexOf((Player)sender)].PlayerDisconnected += this.HandlePlayerDisconnected;
                }
            }
        }

        private void HandlePlayerMoved(object sender, PlayerMovedEventArgs e)
        {
            if( e.X == this.x && e.Y == this.y)
            {
                this.writeToClient(e.Name + " enters the room.");
            }

            //TODO: add which direction player left in
            if (e.OldX == this.x && e.OldY == this.y)
            {
                this.writeToClient(e.Name + " heads " + e.Direction + ".");
            }
        }

        private void HandlePlayerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            this.writeToClient(e.Name + " has disconnected.");

            lock (playerlock)
            {
                if (this.players.Contains((Player)sender))
                {
                    this.players[this.players.IndexOf((Player)sender)].PlayerConnected -= this.HandlePlayerConnected;
                    this.players[this.players.IndexOf((Player)sender)].PlayerMoved -= this.HandlePlayerMoved;
                    this.players[this.players.IndexOf((Player)sender)].PlayerDisconnected -= this.HandlePlayerDisconnected;
                }
            }
        }
    }
}
