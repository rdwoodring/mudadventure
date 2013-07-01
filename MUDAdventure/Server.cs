﻿using System;
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

            //TODO: add code for loading rooms from DB or XML file
            //rooms.Add("123", new Room()); etc., etc.
            //for now let's just create some rooms manually for testing purposes.
            /*
            Room room = new Room("A Starting Place", "This is the starting room.  It is completely empty.", true, false, true, false, 0, 0, 0, true);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            room = new Room("North of A Starting Place", "Well, you've moved to a room north of the starting room... but it's still completely empty.", false, true, true, false, 0, 1, 0, false);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            room = new Room("Northeast of A Starting Place", "Woohoo!  Just kidding... this room is still completely empty.", false, true, false, true, 1, 1, 0, false);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            room = new Room("East of A Starting Place", "Nothing here.  Move along, move along.", true, false, false, true, 1, 0, 0, false);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);
             * */
            Console.WriteLine("Attempting to read Rooms.xml...");
            this.ReadRoomFile();
            if (rooms.Count > 0)
            {
                Console.WriteLine("Success!\r\nRead {0} rooms from the file.", rooms.Count);
            }

            //TODO: add code for loading npcs from DB
            NPC npc = new NPC(0, 0, 0, "An NPC", "An NPC is standing here.  It has no form and nothing on.", new List<string> {"NPC"}, 60000, 10, 50, this.worldTimer, this.players, this.rooms);
            this.npcs.Add( npc);                                   
        }

        private void ReadRoomFile()
        {
            //TODO: replace this path with the root path eventually.  For now, it is always looking for this
            //file in the debug folder, so we'll specify elsewhere.
            if (File.Exists(@"..\..\Rooms.xml"))
            {
                /*using (StreamReader sr = new StreamReader(@"..\..\Rooms.xml"))
                {
                    //string contents = sr.ReadToEnd();
                    //return contents;
                }*/

                XmlDocument doc = new XmlDocument();
                doc.Load(@"..\..\Rooms.xml");

                XmlNodeList xmlrooms = doc.GetElementsByTagName("room");
                for (int i = 0; i < xmlrooms.Count; i++)
                {
                    string name, desc;
                    bool nexit, sexit, wexit, eexit, lighted;
                    int x, y, z;

                    //Debug.Print(xmlrooms[i].HasChildNodes.ToString());
                    //Debug.Print(xmlrooms[i].ChildNodes.Count.ToString());
                    //Debug.Print(xmlrooms[i].FirstChild.InnerText);
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
                //return null;
            }
        }

        private void ParseRoomsXml(string roomsXml)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(roomsXml)))
            {
                
                while (!reader.EOF)
                {
                    string name, desc;
                    bool nexit, sexit, wexit, eexit, lighted;
                    int x, y, z;
                
                    reader.ReadToFollowing("name");
                    name = reader.ReadElementContentAsString();

                    reader.ReadToFollowing("description");
                    desc = reader.ReadElementContentAsString();

                    reader.ReadToFollowing("nexit");
                    nexit = reader.ReadElementContentAsBoolean();

                    reader.ReadToFollowing("sexit");
                    sexit = reader.ReadElementContentAsBoolean();

                    reader.ReadToFollowing("eexit");
                    eexit = reader.ReadElementContentAsBoolean();

                    reader.ReadToFollowing("wexit");
                    wexit = reader.ReadElementContentAsBoolean();
                    
                    reader.ReadToFollowing("x");
                    x = reader.ReadElementContentAsInt();

                    reader.ReadToFollowing("y");
                    y = reader.ReadElementContentAsInt();

                    reader.ReadToFollowing("z");
                    z = reader.ReadElementContentAsInt();
                    
                    reader.ReadToFollowing("lighted");
                    lighted = reader.ReadElementContentAsBoolean();

                    Room room = new Room(name, desc, nexit, sexit, eexit, wexit, x, y, z, lighted);
                    this.rooms.Add(x + "," + y + "," + z, room);
                }
            }
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                TcpClient client = this.tcpListener.AcceptTcpClient();

                Player player = new Player(client, ref players, rooms, ref npcs, this.worldTimer, this.hour);                      

                Thread clientThread = new Thread(new ParameterizedThreadStart(player.initialize));
                clientThread.Start();

                lock (playerThreadList)
                {
                    this.playerThreadList.Add(clientThread);
                }

                player.PlayerConnected += HandlePlayerConnected;
                player.PlayerDisconnected += HandlePlayerDisconnected;
                lock (playerLock)
                {
                    this.players.Add(player);
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

            lock (playerLock)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].Equals((Player)sender))
                    {
                        this.players[i].PlayerConnected -= HandlePlayerConnected;
                        this.players[i].PlayerDisconnected -= HandlePlayerDisconnected;
                        this.players.RemoveAt(i);
                        //this.playerThreadList[i].Abort();
                        
                        //this.playerThreadList[i].Interrupt();
                        //this.playerThreadList[i].Join();                        

                        //this.playerThreadList[i].IsBackground = true;
                        this.playerThreadList[i].Abort();

                        //this.playerThreadList[i].Join();

                        //Console.WriteLine(this.playerThreadList[i].Name + " terminating.");

                        this.playerThreadList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            this.time++;
            //Console.WriteLine(this.time);

            if (this.time % 600 == 0)
            {
                hour = this.time / 600;
                //Console.WriteLine(hour);

                foreach (Player player in players)
                {
                    Monitor.TryEnter(player);

                    try
                    {
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
