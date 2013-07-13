using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace MUDAdventure
{
    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private ObservableCollection<Player> players = new ObservableCollection<Player>();
        private static object playerLock = new object();
        private static object threadListLock = new object();
        private Dictionary<string, Room> rooms = new Dictionary<string,Room>();
        private List<NPC> npcs = new List<NPC>();
        private List<Thread> playerThreadList = new List<Thread>();
        private List<Item> itemList = new List<Item>();
        private List<Item> expirableItemList = new List<Item>();
        private System.Timers.Timer worldTimer;
        private int time, hour;

        public Server()
        {
            this.tcpListener = new TcpListener(IPAddress.Parse("0.0.0.0"), 3000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();

            this.worldTimer = new System.Timers.Timer();
            this.worldTimer.Interval = 100;

            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            this.worldTimer.Enabled = true;

            this.time = 0;
            this.hour = 0; 

            Console.WriteLine("Attempting to read Rooms.xml...");
            this.ReadRoomFile();
            if (rooms.Count > 0)
            {
                Console.WriteLine("Success!\r\nRead {0} rooms from the file.", rooms.Count);
            }

            //TODO: add code for loading npcs from DB
            NPC npc = new NPC(0, 0, 0, "An NPC", "An NPC is standing here.  It has no form and nothing on.", new List<string> {"NPC"}, 60000, 10, 0, this.worldTimer, this.players, this.rooms);
            this.npcs.Add( npc);

            Dagger dagger = new Dagger(this.worldTimer, "A dagger", "A very generic, basic dagger", 1, 0, 0, 0, 10000, true, new List<string> { "dagger", "dag" }, ref this.expirableItemList, 10, 10);
            itemList.Add(dagger);
            //Weapon weapon = new Weapon("something", "blah", 3.5, 1, 1, 1, 10, 10);
            
        }

        private void ReadRoomFile()
        {
            //TODO: replace this path with the root path eventually.  For now, it is always looking for this
            //file in the debug folder, so we'll specify elsewhere.
            if (File.Exists(@"..\..\Rooms.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@"..\..\Rooms.xml");

                XmlNodeList xmlrooms = doc.GetElementsByTagName("room");
                for (int i = 0; i < xmlrooms.Count; i++)
                {
                    string name, desc;
                    bool nexit, sexit, wexit, eexit, lighted;
                    int x, y, z;

                    name = xmlrooms[i].ChildNodes[0].InnerText;
                    desc = xmlrooms[i].ChildNodes[1].InnerText;
                    nexit = bool.Parse(xmlrooms[i].ChildNodes[2].InnerText);
                    sexit = bool.Parse(xmlrooms[i].ChildNodes[3].InnerText);
                    eexit = bool.Parse(xmlrooms[i].ChildNodes[4].InnerText);
                    wexit = bool.Parse(xmlrooms[i].ChildNodes[5].InnerText);
                    x = int.Parse(xmlrooms[i].ChildNodes[6].InnerText);
                    y = int.Parse(xmlrooms[i].ChildNodes[7].InnerText);
                    z = int.Parse(xmlrooms[i].ChildNodes[8].InnerText);
                    lighted = bool.Parse(xmlrooms[i].ChildNodes[9].InnerText);

                    Room room = new Room(name, desc, nexit, sexit, eexit, wexit, x, y, z, lighted);
                    rooms.Add(x + "," + y + "," + z, room);
                }                
            }
            else
            {
                Console.WriteLine("Couldn't locate the Rooms.xml file. Make sure that it is named corrected and in the correct directory.");
            }
        }

        private void ListenForClients()
        {
            //start our listener socket
            this.tcpListener.Start();

            //start infinite listening loop
            while (true)
            {
                //loop blocks, waiting to accept client
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //when it does, we'll create a new player instance, passing in some stuff
                Player player = new Player(client, ref players, rooms, ref npcs, this.worldTimer, this.hour, ref this.itemList, ref this.expirableItemList);                      

                //then let's create a new thread and initialize our player instance
                Thread clientThread = new Thread(new ParameterizedThreadStart(player.initialize));
                clientThread.Start();

                //add the thread to the thread list so we can track it if necessary
                lock (playerThreadList)
                {
                    this.playerThreadList.Add(clientThread);
                }

                //let's assign some event handlers for the player connecting and disconnecting
                player.PlayerConnected += HandlePlayerConnected;
                player.PlayerDisconnected += HandlePlayerDisconnected;

                //and add the player to the player list.
                lock (playerLock)
                {
                    this.players.Add(player);
                }                
            }
        }
        
        private void HandlePlayerConnected(object sender, PlayerConnectedEventArgs e)
        {
            //message written to the server console window when the player connects
            Console.WriteLine(e.Name + " has connected.");
        }

        private void HandlePlayerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            //message written to the server console window when the palyer disconnects
            Console.WriteLine(e.Name + " has disconnected.");

            //house cleaning on disconnect!
            lock (playerLock)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].Equals((Player)sender))
                    {
                        //let's remove those pesky event handlers since the player disconnected
                        this.players[i].PlayerConnected -= HandlePlayerConnected;
                        this.players[i].PlayerDisconnected -= HandlePlayerDisconnected;

                        //and remove the player from the list
                        this.players.RemoveAt(i);

                        //and stop his thread
                        //TODO: this is the "ungraceful" way.  figure out how to do this better
                        this.playerThreadList[i].Abort();

                        //then remove the thread from the list
                        this.playerThreadList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            //increment the time var
            this.time++;

            //since the timed events happen every 1/10 of a second, we don't want to base our world time on that.
            //Every minute of real time == 1 hour of game time
            //24 minutes of real time == 1 day of game time
            //if time / 600 yields no remainder, we're at a nice round hour time marker 
            if (this.time % 600 == 0)
            {
                hour = this.time / 600;

                foreach (Player player in players)
                {
                    Monitor.TryEnter(player);

                    try
                    {
                        //so let's pass it out to all the player instances
                        player.ReceiveTime(hour);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                        Console.WriteLine("Trace: {0}", ex.StackTrace);
                    }
                    finally
                    {
                        Monitor.Exit(player);
                    }
                }                
            }            
        }
    }
}
