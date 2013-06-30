﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Timers;

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

            //TODO: add code for loading rooms from DB
            //rooms.Add("123", new Room()); etc., etc.
            //for now let's just create some rooms manually for testing purposes.
            Room room = new Room("A Starting Place", "This is the starting room.  It is completely empty.", true, false, true, false, 0, 0, 0, true);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            room = new Room("North of A Starting Place", "Well, you've moved to a room north of the starting room... but it's still completely empty.", false, true, true, false, 0, 1, 0, false);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            room = new Room("Northeast of A Starting Place", "Woohoo!  Just kidding... this room is still completely empty.", false, true, false, true, 1, 1, 0, false);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            room = new Room("East of A Starting Place", "Nothing here.  Move along, move along.", true, false, false, true, 1, 0, 0, false);
            this.rooms.Add(room.X.ToString() + "," + room.Y.ToString() + "," + room.Z.ToString(), room);

            //TODO: add code for loading npcs from DB
            NPC npc = new NPC(0, 0, 0, "An NPC", "An NPC is standing here.  It has no form and nothing on.", new List<string> {"NPC"}, 60000, 10, 50, this.worldTimer, this.players);
            this.npcs.Add( npc);                                   
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

                        this.playerThreadList[i].IsBackground = true;
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
