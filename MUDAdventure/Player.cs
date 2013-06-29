using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Diagnostics;

namespace MUDAdventure
{
    class Player
    {
        public event EventHandler<PlayerConnectedEventArgs> PlayerConnected;
        public event EventHandler<PlayerMovedEventArgs> PlayerMoved;
        public event EventHandler<PlayerDisconnectedEventArgs> PlayerDisconnected;
        
        private string name;
        private TcpClient tcpClient;
        NetworkStream clientStream;
        ASCIIEncoding encoder;
        private int x, y, z;
        private ObservableCollection<Player> players;
        private Dictionary<string, Room> rooms;
        private List<NPC> npcs;
        private Room currentRoom;
        private System.Timers.Timer worldTimer;
        private int worldTime;
        private int totalMoves, currentMoves, totalHitpoints, currentHitpoints;

        private int timeCounter = 0; //for regenerating moves and hp

        private static object playerlock = new object();
        private static object npclock = new object();       

        /****************************************/
        /*        FINITE STATE MACHINES         */
        /****************************************/
        private bool inCombat = false;
        private bool isNight = false;
        private bool enteringName, enteringPassword, mainGame;
        private bool connected;
        
        public Player(TcpClient client, ref ObservableCollection<Player> playerlist, Dictionary<string, Room> roomlist, ref List<NPC> npclist, System.Timers.Timer timer, int time)
        {
            this.tcpClient = client;
            this.name = null;
            this.players = playerlist;

            this.rooms = roomlist;

            this.npcs = npclist;

            this.worldTimer = timer;

            //aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            this.worldTime = time;

            if (this.worldTime < 6 || this.worldTime > 21)
            {
                isNight = true;
            }
        }

        public string getName()
        {
            return this.name;
        }

        public void initialize(object e)
        {
            this.clientStream = tcpClient.GetStream();
            this.encoder = new ASCIIEncoding();

            this.enteringName = true;
            this.writeToClient("Welcome to my MUD\r\nWhat is your name, traveller? ");
            string tempName = readFromClient();
            if (tempName != null && tempName != "")
            {
                this.name = tempName;
                this.connected = true;
                this.OnPlayerConnected(new PlayerConnectedEventArgs(this.name));

                Monitor.TryEnter(playerlock, 3000);
                try
                {
                    this.players.CollectionChanged += playerListUpdated;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(this.name);
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine("Trace: " + ex.StackTrace);
                }
                finally
                {
                    Monitor.Exit(playerlock);
                }
            }
            this.enteringName = false;

            //TODO: check for player in database
            //TODO: if player exists, ask for password
            this.enteringPassword = true;
            this.writeToClient("Please enter your password: ");
            this.readFromClient();
            //TODO: if passwords match, allow entrance
            this.enteringPassword = false;

            this.mainGame = true;            

            //TODO: replace with loading player's location from DB
            this.x = 0;
            this.y = 0;
            this.z = 0;

            //TODO: replace with loading player's stats from db
            //stats = stats;
            this.totalMoves = 10;
            this.currentMoves = this.totalMoves;
            this.totalHitpoints = 10;
            this.currentHitpoints = this.totalHitpoints;

            currentRoom = rooms[this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString()];

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

        public void ReceiveTime(int hour)
        {
            switch (hour)
            {
                case 6:
                    this.writeToClient("The sun crests the horizon and begins its daily ascent.\r\n");
                    break;
                case 12:
                    this.writeToClient("The sun sits directly overhead, at its apex.\r\n");
                    break;
                case 21:
                    this.writeToClient("The sun touches the horizon and slowly slips downwards, leaving the world in darkness.\r\n");
                    break;
            }
        }

        private void Look()
        {
            string message = String.Empty;

            this.rooms.TryGetValue(this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString(), out currentRoom);

            if (!isNight || currentRoom.LightedRoom)
            {                
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

                        message += "\r\n" + currentRoom.RoomName + "\r\n" + exits + "\r\n" + currentRoom.RoomDescription;

                        //check to see if any NPCs are here
                        Monitor.TryEnter(npclock, 3000);
                        try
                        {
                            foreach (NPC npc in npcs)
                            {
                                if (npc.X == this.x && npc.Y == this.y && npc.Z == this.z)
                                {
                                    message += "\r\n" + npc.Name + " is here.";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        finally
                        {
                            Monitor.Exit(npclock);
                        }

                        writeToClient(message);
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
            else
            {
                writeToClient("\r\nIt's pitch black... Do you have a light, perhaps?");
            }
        }

        private void Look(string args)
        {
            //TODO: implement logic for looking at objects,  npcs, and other players
            bool found = false;

            if (!isNight || currentRoom.LightedRoom)
            {

                Monitor.TryEnter(npclock, 3000);
                try
                {
                    foreach (NPC npc in npcs)
                    {
                        if (npc.X == this.x && npc.Y == this.y && npc.Z == this.z)
                        {
                            foreach (string refname in npc.RefNames)
                            {
                                if (refname.ToLower() == args)
                                {
                                    writeToClient(npc.Description);
                                    found = true;
                                }
                            }
                        }

                        if (!found)
                        {
                            writeToClient("That person or thing is not here.");
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    Monitor.Exit(npclock);
                }
            }
            else
            {
                writeToClient("It's too dark to see that person or thing.");
            }
        }

        private void ParseInput(string input)
        {
            input = input.ToLower();

            //TODO: fix errors when input entered is too short. substring is causing problems.
            if (input == "n" || input == "s" || input == "e" || input == "w")
            {
                this.Move(input);
            }
            else if (input == String.Empty)
            {
                writeToClient(String.Empty);
            }            
            else if (input.StartsWith("look"))
            {                
                if (input.Length > 4) //has args after it
                {
                    string args = input.Substring(5);
                    this.Look(args.ToLower());
                }
                else //no args after it
                {
                    this.Look();
                }
            }
            else if (input.StartsWith("kill"))
            {
                string args = input.Substring(5);
                this.Kill(args.ToLower());
            }
            else if (input == "exit")
            {
                //TODO: implement event for disconnect so Server can update player list
                this.Disconnect();
            }
            else
            {
                this.writeToClient("Unrecognized command.");
            }
        }

        private void Kill(string args)
        {
            //TODO: fill method with logic for attacking
        }

        private void Attack(NPC npc)
        {
            
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
                if (enteringName || enteringPassword)
                {
                    buffer = this.encoder.GetBytes(message + "\r\n");
                }
                else if (mainGame)
                {
                    string health = String.Empty;
                    string moves = String.Empty;
                    string append = String.Empty;

                    if (((double)this.currentHitpoints / (double)this.totalHitpoints) <= .1)
                    {
                        health += "HP: Awful";
                    }
                    else if (((double)this.currentHitpoints / (double)this.totalHitpoints) <= .25)
                    {
                        health += "HP: Bloodied";
                    }
                    else if (((double)this.currentHitpoints / (double)this.totalHitpoints) <= .4)
                    {
                        health += "HP: Wounded";
                    }
                    else if (((double)this.currentHitpoints / (double)this.totalHitpoints) <= .6)
                    {
                        health += "HP: Hurt";
                    }
                    else if ((double)(this.currentHitpoints / this.totalHitpoints) <= .75)
                    {
                        health += "HP: Bruised";
                    }
                    else if ((double)(this.currentHitpoints / this.totalHitpoints) < 1)
                    {
                        health += "HP: Scratched";
                    }

                    Debug.Print((((double)this.currentMoves / (double)this.totalMoves)).ToString());

                    if (((double)this.currentMoves / (double)this.totalMoves) <= .1)
                    {
                        moves += "MV: Spent";
                    }
                    else if (((double)this.currentMoves / (double)this.totalMoves) <= .25)
                    {
                        moves += "MV: Exhausted";
                    }
                    else if (((double)this.currentMoves / (double)this.totalMoves) <= .4)
                    {
                        moves += "MV: Tired";
                    }
                    else if (((double)this.currentMoves / (double)this.totalMoves) <= .6)
                    {
                        moves += "MV: Weary";
                    }

                    if (health != String.Empty)
                    {
                        append = health + "; " + moves;
                    }
                    else
                    {
                        append = health + moves;
                    }

                    buffer = this.encoder.GetBytes(message + "\r\n" + append + ">");
                }
                else
                {
                    buffer = this.encoder.GetBytes(message + "\r\n");
                }

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

            return finalMessage.TrimEnd('\r', '\n');
        }

        private void Move(string dir)
        {
            //TODO: add movement logic here

            int oldx, oldy, oldz;
            oldx = this.x;
            oldy = this.y;
            oldz = this.z;

            //getting current room.
            this.rooms.TryGetValue(this.x + "," + this.y + "," + this.z, out currentRoom);

            //make sure the room we are in exists and is not null
            if (currentRoom != null)
            {
                if (currentMoves > 0)
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
                        this.currentMoves--;
                    }
                    //if we haven't moved, that means there wasn't an available exit in that direction.
                    //let's tell the stupid player with an message
                    else
                    {
                        writeToClient("You cannot go that direction.");
                    }
                }
                else
                {
                    writeToClient("You are too tired to move.");
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

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            this.timeCounter++;

            //TODO: HP regen depending on constitution
            //this.writeToClient("hp regenerating");
            
            //TODO: MOVE regen
            if (this.currentMoves < this.totalMoves)
            {
                if (this.timeCounter % 50 == 0)
                {
                    this.currentMoves++;
                }
            }

            if (inCombat)
            {
                //call attack method depending upon speed.
            }
        }
    }
}
