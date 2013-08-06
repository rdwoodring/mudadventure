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
using System.Linq;

namespace MUDAdventure
{
    class Player
    {
        public event EventHandler<PlayerConnectedEventArgs> PlayerConnected;
        public event EventHandler<PlayerMovedEventArgs> PlayerMoved;
        public event EventHandler<PlayerDisconnectedEventArgs> PlayerDisconnected;
        public event EventHandler<FledEventArgs> PlayerFled;
        public event EventHandler<FleeFailEventArgs> PlayerFleeFail;
        public event EventHandler<AttackedAndHitEventArgs> PlayerAttackedAndHit;

        //TODO: temporary solution, what a headache this gave me.  stupid debug folder making copies of databases YAAAARRRRGHGH!
        private MUDAdventureDataContext db = new MUDAdventureDataContext(@"C:\Users\rswoody\Documents\Visual Studio 2010\Projects\MUDAdventure\MUDAdventure\MUDAdventure.mdf");

        private string name; //the player character's name
        private TcpClient tcpClient;
        NetworkStream clientStream;
        ASCIIEncoding encoder;
        private int x, y, z; //x, y, and z coordinates
        private ObservableCollection<Player> players; //a list of all connected players
        private Dictionary<string, Room> rooms; //a dictionary of all rooms where the key is "x,y,z"
        private List<NPC> npcs; //a list of all npcs
        private List<Item> itemList; //a list of all items
        private List<Item> expirableItemList;
        private Room currentRoom; //which room the player is currently in
        private System.Timers.Timer worldTimer; //the world timer instantiated by the server. used for timed events like attacking and regening moves and health
        private int worldTime; //what time it is according to the world's clock
        private int totalMoves, currentMoves, totalHitpoints, currentHitpoints; //current moves, total moves, current hp, total hp TODO: add MP to this
        private Object combatTarget; //the target of your wrath, if there is one TODO: make this a list in case another NPC/player attacks while already in combat
        private Random rand = new Random(); //a random number... it gets used...
        private Inventory inventory = new Inventory();
        private double maxCarryWeight;

        private int timeCounter = 0; //for regenerating moves and hp

        private static object playerlock = new object();
        private static object npclock = new object();
        private static object itemlock = new object();
        private static object expirableitemlock = new object();
        private Object hplock = new Object();

        /****************************************/
        /*        FINITE STATE MACHINES         */
        /****************************************/
        private bool inCombat = false;
        private bool isNight = false;
        private bool enteringName, enteringPassword, mainGame; //is the player entering their name, entering their password, or in the main game loop
        private bool connected;
        private bool isDead = false; //you don't wanna be this, you're dead HAH!
        private bool isFleeing = false;
        
        public Player(TcpClient client, ref ObservableCollection<Player> playerlist, Dictionary<string, Room> roomlist, ref List<NPC> npcs, System.Timers.Timer timer, int time, ref List<Item> itemlist, ref List<Item> expirableItemList)
        {
            //assign a bunch of stuff that's passed in from the server then the Player is created in memory
            this.tcpClient = client;
            this.name = null;
            this.players = playerlist;

            this.rooms = roomlist;
            this.npcs = npcs;
            this.itemList = itemlist;

            this.worldTimer = timer;

            //assign a handler to the timer's elapsed event so we can have events happen during time.
            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            //setting the time equal to the world time passed in from the server.  
            this.worldTime = time;

            //if the time that's initially passed in is before 6 AM or after 9 PM (21:00), it's night time
            //which means it's dark, so players can't see without a light UNLESS they are in a lighted room
            //TODO: add torches and light sources
            if (this.worldTime < 6 || this.worldTime > 21)
            {
                isNight = true;
            }

            //TODO: peg this value to the strength statistic somehow
            this.maxCarryWeight = 100;

            this.expirableItemList = expirableItemList;
        }

        //TODO: fix this accessor... this is not the way all the other accessors are
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
            
            while (!this.mainGame)
            {
                string tempName = readFromClient();

                if (tempName != null && tempName != "")
                {
                    var playerQuery =
                        (from playercharacter in db.PlayerCharacters
                        where playercharacter.PlayerName.ToString().ToLower() == tempName.ToLower()
                        select playercharacter).ToList();

                    if (playerQuery.Count == 1) //player exists, we should request password
                    {
                        this.enteringName = false;

                        int passwordAttempts = 0;
                        bool successfullogin = false;

                        this.enteringPassword = true;
                        do
                        {
                            this.writeToClient("Please enter your password: ");
                            string tempPass = this.readFromClient();

                            if (playerQuery[0].Password.ToString() == tempPass) //successful login.  player may now enter the main game
                            {
                                successfullogin = true;
                            }
                            else
                            {
                                passwordAttempts++;
                            }
                        }
                        while (passwordAttempts < 4 && successfullogin == false);

                        if (successfullogin)
                        {
                            //TODO: add the rest of the init logic
                            this.enteringPassword = false;

                            this.name = playerQuery[0].PlayerName.ToString();
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

                            //subscribing to events for players that are already logged in
                            foreach (Player player in players)
                            {
                                Monitor.TryEnter(playerlock, 5000);
                                try
                                {
                                    if (player != this) //don't need to subscribe to events about ourselves, do we?
                                    {
                                        player.PlayerConnected += this.HandlePlayerConnected;
                                        player.PlayerMoved += this.HandlePlayerMoved;
                                        player.PlayerDisconnected += this.HandlePlayerDisconnected;
                                        player.PlayerFled += this.HandlePlayerFled;
                                        player.PlayerFleeFail += this.HandlePlayerFleeFail;
                                        player.PlayerAttackedAndHit += this.HandlePlayerAttackedAndHit;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    writeToClient("Error: " + ex.Message);
                                    writeToClient("Trace: " + ex.StackTrace);
                                }
                                finally
                                {
                                    Monitor.Exit(playerlock);
                                }
                            }

                            //subscribing to events for NPCs
                            foreach (NPC npc in npcs)
                            {
                                Monitor.TryEnter(npclock, 5000);
                                try
                                {
                                    npc.NPCFled += this.HandleNPCFled;
                                    npc.NPCMoved += this.HandleNPCMoved;
                                    npc.NPCFleeFail += this.HandleNPCFleeFail;
                                    npc.NPCAttackedAndHit += this.HandleNPCAttackedAndHit;
                                    npc.NPCDied += this.HandleNPCDied;
                                    npc.NPCSpawned += this.HandleNPCSpawned;
                                }
                                catch (Exception ex)
                                {
                                    writeToClient("Error: " + ex.Message);
                                    writeToClient("Trace: " + ex.StackTrace);
                                }
                                finally
                                {
                                    Monitor.Exit(npclock);
                                }
                            }

                            this.mainGame = true;

                            //TODO: replace with loading player's location from DB
                            this.x = playerQuery[0].X;
                            this.y = playerQuery[0].Y;
                            this.z = playerQuery[0].Z;                            
                        }
                        else if (passwordAttempts > 3)
                        {
                            this.writeToClient("Password authentication failed too many times.");
                            this.Disconnect();
                        }
                        else
                        {
                            this.writeToClient("Unspecified login failure.");
                            this.Disconnect();
                        }

                    }
                    else if (playerQuery.Count > 1)
                    {
                        //UHHH, this shouldn't ever happen and is probably a bad thing.
                        this.writeToClient("An error occured from duplicate entries in our character database.\r\nAdministrators have been notified.  Please be patient as we correct this.");
                        string errormessage = "Possible duplicate entries in character database: ";
                        foreach (var player in playerQuery)
                        {
                            errormessage += player.PlayerName.ToString() + "\r\n";
                        }

                        this.Disconnect();
                    }
                    else
                    {
                        tempName = tempName[0].ToString().ToUpper() + tempName.Substring(1).ToLower();
                        //TODO: filter unsuitable names with profanity, numbers, etc.

                        this.writeToClient("This character does not seem to exist.  Would you like to create a character with the name " + tempName + "? (Y/N)");

                        if (this.readFromClient().ToLower() == "y")
                        {
                            //we'll create a new character then
                            PlayerCharacter newPlayer = new PlayerCharacter();
                            newPlayer.PlayerName = tempName;

                            bool match = false;

                            do
                            {
                                this.writeToClient("Please enter your desired password:");
                                string tempPass = this.readFromClient();

                                this.writeToClient("Please re-enter your desired password:");
                                string tempPassConfirm = this.readFromClient();

                                if (tempPass == tempPassConfirm)
                                {
                                    match = true;
                                    newPlayer.Password = tempPass;
                                }
                                else
                                {
                                    this.writeToClient("Your password and password confirmation did not match. Please try again.");
                                }
                            } while (!match);
                           
                            int tempStrength, tempAgility, tempIntelligence, tempLearning, tempConstitution;

                            do
                            {
                                tempStrength = rand.Next(10, 25);
                                tempAgility = rand.Next(10, 25);
                                tempIntelligence = rand.Next(10, 25);
                                tempLearning = rand.Next(10, 25);
                                tempConstitution = rand.Next(10, 25);

                                this.writeToClient("Your stats are STR:" + tempStrength.ToString() + "AGI:" + tempAgility.ToString() + "INT:" + tempIntelligence.ToString() + "LEA:" + tempLearning.ToString() + "CON:" + tempConstitution.ToString());
                                this.writeToClient("Are these stats acceptable? (Y/N)");

                            } while (this.readFromClient().ToLower() != "y");

                            newPlayer.Strength = tempStrength;
                            newPlayer.Agility = tempAgility;
                            newPlayer.Intelligence = tempIntelligence;
                            newPlayer.Learning = tempLearning;
                            newPlayer.Constitution = tempConstitution;

                            newPlayer.X = 0;
                            newPlayer.Y = 0;
                            newPlayer.Z = 0;

                            newPlayer.Level = 1;

                            //TODO: insert real EXP til next value here once i calculate the experience progression
                            newPlayer.ExpUntilNext = 1000;

                            try
                            {
                                this.db.PlayerCharacters.InsertOnSubmit(newPlayer);                                
                                this.db.SubmitChanges();

                                var playerQuery2 =
                                    (from playercharacter in db.PlayerCharacters                                     
                                     select playercharacter).ToList();

                                Debug.Print(playerQuery2.Count.ToString());

                                this.writeToClient("Character created and saved to database.");

                                this.name = tempName;

                                this.connected = true;
                                this.mainGame = true;

                                this.x = 0;
                                this.y = 0;
                                this.z = 0;

                                currentRoom = rooms[this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString()];
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error: " + ex.Message);
                                Console.WriteLine("Trace: " + ex.StackTrace);
                            }
                        }
                        else
                        {
                            this.writeToClient("Very well.  By what name would you like to be known then?");
                        }
                    }
                }
            }

            currentRoom = rooms[this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString()];            

            //TODO: replace with loading player's stats from db
            //stats = stats;
            this.totalMoves = 10;
            this.currentMoves = this.totalMoves;
            this.totalHitpoints = 10;
            this.currentHitpoints = this.totalHitpoints;

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
                    this.isNight = false;
                    break;
                case 12:
                    this.writeToClient("The sun sits directly overhead, at its apex.\r\n");
                    break;
                case 21:
                    this.writeToClient("The sun touches the horizon and slowly slips downwards, leaving the world in darkness.\r\n");
                    this.isNight = true;
                    break;
            }
        }

        private void ParseInput(string input)
        {
            input = input.ToLower();

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
            else if (input.StartsWith("eq"))
            {
                this.EquipmentList();
            }
            else if (input.StartsWith("wield"))
            {
                if (input.Length > 5) //has args after it
                {
                    string args = input.Substring(6);
                    this.Wield(args.ToLower());
                }
                else //no args after it
                {
                    this.writeToClient("Wield what?");
                }
            }
            else if (input.StartsWith("hold"))
            {
                if (input.Length > 4) //has args after it
                {
                    string args = input.Substring(5);
                    this.Hold(args.ToLower());
                }
                else //no args
                {
                    this.writeToClient("Hold what?");
                }
            }
            else if (input.StartsWith("kill"))
            {
                if (input.Length > 4)
                {
                    string args = input.Substring(5);
                    this.Kill(args.ToLower());
                }
                else
                {
                    this.writeToClient("Kill who?");
                }
            }
            else if (input.StartsWith("take"))
            {
                if (input.Length > 4)
                {
                    string args = input.Substring(5);
                    this.Take(args.ToLower());
                }
                else
                {
                    this.writeToClient("Take what?");
                }
            }
            else if (input.StartsWith("drop"))
            {
                if (input.Length > 4)
                {
                    string args = input.Substring(5);
                    this.Drop(args.ToLower());
                }
                else
                {
                    this.writeToClient("Drop what?");
                }
            }
            else if (input.StartsWith("inv"))
            {
                this.Inventory();
            }
            else if (input.StartsWith("inf"))
            {
                this.Info();
            }
            else if (input.StartsWith("flee"))
            {
                this.Flee();
            }
            else if (input == "exit")
            {
                //TODO: implement event for disconnect so Server can update player list
                this.Disconnect();
            }
            else
            {
                this.writeToClient("Unrecognized command.\r\n");
            }

            Debug.Print(input);
        }

        #region Player Command Methods

        private void Hold(string args)
        {
            List<Item> items = this.inventory.ListInventory();
            List<Item> itemsQuery =
                (from item in items
                 where item.RefNames.Contains(args)
                 select item).ToList();

            if (itemsQuery.Count >= 1)
            {
                if (itemsQuery[0].GetType().ToString() == "MUDAdventure.Light")
                {
                    StringBuilder message = new StringBuilder();

                    //remove the currently equipped light, if any and return it to general inv
                    if (this.inventory.Light != null)
                    {
                        message.Append("You stop using " + this.inventory.Light.Name + " as a light.\r\n");
                        this.inventory.Light.IsLit = false;
                        this.inventory.AddItem(this.inventory.Light);
                    }

                    //equip the new light and LIGHT ER UP
                    this.inventory.Light = (Light)itemsQuery[0];
                    this.inventory.Light.IsLit = true;

                    //remove new light from general inv
                    this.inventory.RemoveItem(itemsQuery[0]);

                    message.Append("You light " + itemsQuery[0].Name + " and hold it aloft as a light.\r\n");

                    this.inventory.Light.LightExpired += this.HandleLightExpired;
                    this.writeToClient(message.ToString());
                }
                else
                {
                    this.writeToClient("You can't use THAT as a light!\r\n");
                }
            }
            else
            {
                this.writeToClient("You're not carrying any such item.\r\n");
            } 
        }

        private void Wield(string args)
        {
            List<Item> items = this.inventory.ListInventory();
            List<Item> itemsQuery =
                (from item in items
                 where item.RefNames.Contains(args)
                 select item).ToList();

            if (itemsQuery.Count >= 1)
            {
                if (itemsQuery[0].GetType().ToString() == "MUDAdventure.Dagger" /* || sword || spear || etc. */)
                {
                    StringBuilder message = new StringBuilder();

                    //remove current weapon if any and add it back to general inv
                    if (this.inventory.Wielded != null)
                    {
                        message.Append("You stop wielding " + this.inventory.Wielded.Name + ".\r\n");
                        this.inventory.AddItem(this.inventory.Wielded);
                    }

                    //wield the new weapon
                    this.inventory.Wielded = (Weapon)itemsQuery[0];

                    //remove the new weapon from general inv
                    this.inventory.RemoveItem(itemsQuery[0]);

                    message.Append("You grasp " + itemsQuery[0].Name + " in your hand and wield it as a weapon.\r\n");
                    this.writeToClient(message.ToString());
                }
                else
                {
                    this.writeToClient("You can't wield that! Perhaps try a weapon instead?\r\n");
                }
            }
            else
            {
                this.writeToClient("You're not carrying any such item.\r\n");
            }            
        }

        private void EquipmentList()
        {
            StringBuilder equipmentlist = new StringBuilder();

            equipmentlist.AppendLine("Items that are currently equipped:");

            //for weapons
            equipmentlist.Append("[Wielded] \t\t");
            if (this.inventory.Wielded != null)
            {
                equipmentlist.AppendLine(this.inventory.Wielded.Name);
            }
            else
            {
                equipmentlist.AppendLine("Nothing");
            }

            equipmentlist.Append("[Light] \t\t");
            if (this.inventory.Light != null)
            {
                equipmentlist.AppendLine(this.inventory.Light.Name);
            }
            else
            {
                equipmentlist.AppendLine("Nothing");
            }
            //TODO: add for apparel, shields, lights, etc.

            writeToClient(equipmentlist.ToString());
        }

        private void Move(string dir)
        {
            if (!this.inCombat || this.isFleeing)
            {
                int oldx, oldy, oldz;
                oldx = this.x;
                oldy = this.y;
                oldz = this.z;

                //make sure the room we are in exists and is not null
                if (currentRoom != null)
                {
                    if (this.isFleeing)
                    {
                        if (this.currentMoves > 5)
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
                        }
                        else
                        {
                            this.OnPlayerFleeFail(new FleeFailEventArgs(this.x, this.y, this.z, this.name));
                            this.currentMoves -= 3;
                        }
                    }
                    else if (!this.isFleeing)
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
                        }

                        //if we have moved, let's look around the new room automatically and raise the OnPlayerMoved event
                        if (this.x != oldx || this.y != oldy || this.z != oldz)
                        {
                            if (this.isFleeing)
                            {
                                rooms.TryGetValue(this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString(), out this.currentRoom);
                                this.Look();
                                this.OnPlayerFled(new FledEventArgs(this.x, this.y, this.z, oldx, oldy, oldz, this.name, dir));
                                currentMoves -= 5;
                            }
                            this.Look();

                            this.OnPlayerMoved(new PlayerMovedEventArgs(this.x, this.y, this.z, oldx, oldy, oldz, this.name, dir));
                            this.currentMoves--;

                            //getting current room.
                            this.rooms.TryGetValue(this.x + "," + this.y + "," + this.z, out this.currentRoom);
                        }
                        //if we haven't moved, that means there wasn't an available exit in that direction.
                        //let's tell the stupid player with an message
                        else
                        {
                            writeToClient("You cannot go that direction.\r\n");
                        }
                    }
                    else
                    {
                        writeToClient("You are too tired to move.\r\n");
                    }
                }
            }
            else
            {
                writeToClient("No way! You're in the middle of a fight!\r\n");
            }
        }

        private void Look()
        {
            string message = String.Empty;

            this.rooms.TryGetValue(this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString(), out currentRoom);

            if (!this.isNight || this.currentRoom.LightedRoom || this.inventory.Light != null)
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
                            List<NPC> npcQuery =
                                (from npc in this.npcs
                                 where npc.X == this.x && npc.Y == this.y && npc.Z == this.z
                                 select npc).ToList();

                            foreach (NPC npc in npcQuery)
                            {
                                message += "\r\n" + npc.Name + " is here.";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                            Debug.Print(ex.StackTrace);
                        }
                        finally
                        {
                            Monitor.Exit(npclock);
                        }

                        //check to see if any items are here
                        Monitor.TryEnter(itemlock, 3000);
                        try
                        {
                            List<Item> itemQuery =
                                (from item in this.itemList
                                 where item.X == this.x && item.Y == this.y && item.Z == this.z
                                 select item).ToList();

                            foreach (Item item in itemQuery)
                            {
                                message += "\r\n" + item.Name + " is lying here.";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                            Debug.Print(ex.StackTrace);
                        }
                        finally
                        {
                            Monitor.Exit(itemlock);
                        }

                        Monitor.TryEnter(expirableitemlock, 3000);
                        try
                        {
                            List<Item> expItemQuery =
                                (from expitem in this.expirableItemList
                                 where expitem.X == this.x && expitem.Y == this.y && expitem.Z == this.z
                                 select expitem).ToList();
                            foreach (Item item in expItemQuery)
                            {
                                message += "\r\n" + item.Name + " is lying here.";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                            Debug.Print(ex.StackTrace);
                        }
                        finally
                        {
                            Monitor.Exit(expirableitemlock);
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

            if (!this.isNight || this.currentRoom.LightedRoom || this.inventory.Light != null)
            {
                //TODO: fix this using LINQ instead of this clumsier way
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
                            writeToClient("That person or thing is not here.\r\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine("Trace: " + ex.StackTrace);
                }
                finally
                {
                    Monitor.Exit(npclock);
                }
            }
            else
            {
                writeToClient("It's too dark to see that person or thing.\r\n");
            }
        }

        private void Drop(string args)
        {
            bool found = false;
            List<Item> items = this.inventory.ListInventory();

            if (items.Count > 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].RefNames.Contains(args))
                    {
                        switch (items[i].GetType().ToString())
                        {
                            case "MUDAdventure.Dagger":
                                Dagger tempitem = new Dagger((Dagger)items[i]);
                                tempitem.X = this.x;
                                tempitem.Y = this.y;
                                tempitem.Z = this.z;
                                tempitem.Spawnable = false;
                                tempitem.Expirable = true;
                                tempitem.ExpireCounter = 0;
                                tempitem.InInventory = false;
                                this.expirableItemList.Add(tempitem);
                                break;
                            default:
                                break;
                        }

                        //TODO: raise dropped item event so other users can see it
                        writeToClient("You drop " + items[i].Name);
                        this.inventory.RemoveItem(i);


                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                writeToClient("You're not carrying any such item.\r\n");
            }
        }

        private void Inventory()
        {
            List<Item> items = this.inventory.ListInventory();
            StringBuilder invlist = new StringBuilder();                        

            for (int i =0; i<items.Count; i++)
            {
                Item tempitem = items[i];                

                invlist.AppendLine((i+1).ToString() + ". " + tempitem.Name);
            }

            if (invlist.ToString() == String.Empty)
            {
                invlist.AppendLine("Nothing");
            }

            invlist.Insert(0, "You are carrying:\r\n");
            
            writeToClient(invlist.ToString());
        }

        private void Take(string args)
        {
            if (!this.isNight || this.currentRoom.LightedRoom || this.inventory.Light != null)
            {
                bool found = false;

                foreach (Item item in this.itemList)
                {
                    if (item.RefNames.Contains(args) && item.X == this.x && item.Y == this.y && item.Z == this.z)
                    {
                        if ((item.Weight + this.inventory.Weight) <= this.maxCarryWeight)
                        {
                            this.writeToClient("You pick up " + item.Name + ".\r\n");
                            switch (item.GetType().ToString())
                            {
                                case "MUDAdventure.Dagger":
                                    //Dagger tempitem = new Dagger(item.WorldTimer, item.Name, item.Description, item.Weight, item.SpawnX, item.SpawnY, item.SpawnZ, item.SpawnTime, item.Spawnable, item.RefNames, ref expirableItemList, item.Damage, item.Speed);
                                    Dagger tempdag = new Dagger((Dagger)item);
                                    tempdag.Spawnable = false;
                                    tempdag.SpawnTime = 0;
                                    tempdag.Expirable = true;
                                    tempdag.ExpireCounter = 0;
                                    tempdag.InInventory = true;
                                    this.inventory.AddItem(tempdag);
                                    break;
                                case "MUDAdventure.Light":
                                    Light templight = new Light((Light)item);
                                    templight.Spawnable = false;
                                    templight.SpawnTime = 0;
                                    templight.Expirable = true;
                                    templight.ExpireCounter = 0;
                                    templight.InInventory = true;
                                    this.inventory.AddItem(templight);
                                    break;
                            }

                            //TODO: raise item picked up event so other users can see
                            item.PickedUp();                            
                        }
                        else
                        {
                            writeToClient(item.Name + " is too heavy for you to carry.\r\n");
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    foreach (Item item in this.expirableItemList)
                    {
                        if (item.RefNames.Contains(args) && item.X == this.x && item.Y == this.y && item.Z == this.z)
                        {
                            if ((item.Weight + this.inventory.Weight) <= this.maxCarryWeight)
                            {
                                this.writeToClient("You pick up " + item.Name + ".\r\n");
                                switch (item.GetType().ToString())
                                {
                                    case "MUDAdventure.Dagger":
                                        //Dagger tempitem = new Dagger(item.WorldTimer, item.Name, item.Description, item.Weight, item.SpawnX, item.SpawnY, item.SpawnZ, item.SpawnTime, item.Spawnable, item.RefNames, ref expirableItemList, item.Damage, item.Speed);
                                        Dagger tempitem = new Dagger((Dagger)item);
                                        tempitem.Spawnable = false;
                                        tempitem.SpawnTime = 0;
                                        tempitem.Expirable = true;
                                        tempitem.ExpireCounter = 0;
                                        tempitem.InInventory = true;
                                        this.inventory.AddItem(tempitem);
                                        this.expirableItemList.Remove(item);
                                        break;
                                }

                                //TODO: raise item picked up event so other users can see
                                item.PickedUp();
                            }
                            else
                            {
                                writeToClient(item.Name + " is too heavy for you to carry.\r\n");
                            }

                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    writeToClient("That item isn't here.\r\n");
                }
            }            
            else
            {
                this.writeToClient("It's too dark to see that item.\r\n");
            }
        }

        private void Flee()
        {
            int flee = rand.Next(1, 2);
            if (flee == 1) //the flee was successful
            {
                int direction = rand.Next(1, 4);
                switch (direction)
                {
                    case 1:
                        this.isFleeing = true;
                        this.Move("n");
                        break;
                    case 2:
                        this.isFleeing = true;
                        this.Move("e");
                        break;
                    case 3:
                        this.isFleeing = true;
                        this.Move("s");
                        break;
                    case 4:
                        this.isFleeing = true;
                        this.Move("w");
                        break;
                }
            }
            else
            {
                this.OnPlayerFleeFail(new FleeFailEventArgs(this.x, this.y, this.z, this.name));
            }
        }

        private void Info()
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine("You are " + this.name + ".");
            message.AppendLine("HP: " + this.currentHitpoints + "/" + this.totalHitpoints + "; MV: " + this.currentMoves + "/" + this.totalMoves);
            message.AppendLine("You are carrying " + this.inventory.Weight + "/" + this.maxCarryWeight + " pounds.");
            message.AppendLine("The time is " + this.worldTime);

            this.writeToClient(message.ToString());
        }

        private void Kill(string args)
        {
            if (!this.inCombat)
            {
                if (!this.isNight || this.currentRoom.LightedRoom || this.inventory.Light != null)
                {
                    foreach (NPC npc in npcs)
                    {
                        if (npc.RefNames.Contains(args) && npc.X == this.x && npc.Y == this.y && npc.Z == this.z)
                        {
                            this.combatTarget = npc;

                            npc.CombatTarget = this;

                            this.inCombat = true;
                        }
                        else
                        {
                            this.writeToClient("That person isn't here.\r\n");
                        }
                    }
                }
                else
                {
                    this.writeToClient("It's too dark to see that person.\r\n");
                }
            }
            else
            {
                this.writeToClient("You're already in a fight!\r\n");
            }
        }

        #endregion

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
                    player.PlayerDisconnected -= this.HandlePlayerDisconnected;
                }
            }

            this.clientStream.Close();
            this.tcpClient.Close();

            this.OnPlayerDisconnected(new PlayerDisconnectedEventArgs(this.name));
        }

        protected virtual void OnPlayerAttackedAndHit(AttackedAndHitEventArgs e)
        {
            EventHandler<AttackedAndHitEventArgs> handler = this.PlayerAttackedAndHit;

            if (handler != null)
            {
                handler(this, e);
            }
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
                    if (!this.inCombat)
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

                        if (health != String.Empty && moves != String.Empty)
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
                        else
                        {
                            health += "HP: Healthy";
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

                        if (health != String.Empty && moves != String.Empty)
                        {
                            append = health + "; " + moves;
                        }
                        else
                        {
                            append = health + moves;
                        }

                        buffer = this.encoder.GetBytes(message + "\r\n" + append + ">");
                    }
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

            if (finalMessage.Contains("\b"))
            {                
                do
                {
                    if (finalMessage.IndexOf("\b") == 0)
                    {
                        finalMessage = finalMessage.Remove(finalMessage.IndexOf("\b"), 1);
                    }
                    else
                    {
                        finalMessage = finalMessage.Remove(finalMessage.IndexOf("\b") - 1, 2);
                    }
                } while (finalMessage.Contains("\b"));
                
            }

            Debug.Print(finalMessage);
            return finalMessage.TrimEnd('\r', '\n');
        }

        

        protected virtual void OnPlayerMoved(PlayerMovedEventArgs e)
        {
            EventHandler<PlayerMovedEventArgs> handler = this.PlayerMoved;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnPlayerFled(FledEventArgs e)
        {
            EventHandler<FledEventArgs> handler = this.PlayerFled;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnPlayerFleeFail(FleeFailEventArgs e)
        {
            EventHandler<FleeFailEventArgs> handler = this.PlayerFleeFail;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void ReceiveAttack(int potentialdamage, string attackername)
        {
            //TODO: implement dodge, parry, armor damage reduction or prevention
            this.inCombat = true;

            Monitor.TryEnter(this.hplock);
            try
            {
                this.currentHitpoints -= potentialdamage;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                Console.WriteLine("Trace: {0}", ex.StackTrace);
            }
            finally
            {
                Monitor.Exit(this.hplock);
            }

            if (this.currentHitpoints <= 0)
            {
                this.Die();
                writeToClient(attackername + " hits you, doing some damage.\r\nYou are dead.\r\n");
            }
            else
            {
                writeToClient(attackername + " hits you, doing some damage.\r\n");
                this.OnPlayerAttackedAndHit(new AttackedAndHitEventArgs(attackername, this.name, this.x, this.y, this.z));
            }
        }

        private void Die()
        {
            this.inCombat = false;
            this.combatTarget = null;
            this.isDead = true;
        }

        /**************************************************************************/
        /*                        ACCESSORS                                       */
        /**************************************************************************/

        public bool IsDead
        {
            get { return this.isDead; }
            set { this.isDead = value; }
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
            this.writeToClient(e.Name + " has connected.\r\n");

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
            if( e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                this.writeToClient(e.Name + " enters the room.\r\n");
            }

            //TODO: add which direction player left in
            if (e.OldX == this.x && e.OldY == this.y && e.OldZ == this.z)
            {
                this.writeToClient(e.Name + " heads " + e.Direction + ".\r\n");
            }
        }

        private void HandlePlayerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            this.writeToClient(e.Name + " has disconnected.\r\n");

            lock (playerlock)
            {
                if (this.players.Contains((Player)sender))
                {
                    this.players[this.players.IndexOf((Player)sender)].PlayerConnected -= this.HandlePlayerConnected;
                    this.players[this.players.IndexOf((Player)sender)].PlayerMoved -= this.HandlePlayerMoved;
                    this.players[this.players.IndexOf((Player)sender)].PlayerDisconnected -= this.HandlePlayerDisconnected;
                    this.players[this.players.IndexOf((Player)sender)].PlayerFled -= this.HandlePlayerFled;
                    this.players[this.players.IndexOf((Player)sender)].PlayerFleeFail -= this.HandlePlayerFleeFail;
                }
            }
        }

        private void HandleNPCMoved(object sender, PlayerMovedEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                this.writeToClient(e.Name + " enters the room.");
            }

            //TODO: add which direction player left in
            if (e.OldX == this.x && e.OldY == this.y && e.OldZ == this.z)
            {
                this.writeToClient(e.Name + " heads " + e.Direction + ".");
            }
        }

        private void HandleNPCFled(object sender, FledEventArgs e)
        {
            if (e.OldX == this.x && e.OldY == this.y && e.OldZ == this.z)
            {
                this.writeToClient(e.Name + " panics and flees " + e.Direction + ".\r\n");
                this.combatTarget = null;
                this.inCombat = false;
            }
        }

        private void HandleNPCFleeFail(object sender, FleeFailEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                this.writeToClient(e.Name + " panics and tries to flee, but can't escape.\r\n");
            }
        }

        private void HandlePlayerFled(object sender, FledEventArgs e)
        {
            if (e.OldX == this.x && e.OldY == this.y && e.OldZ == this.z)
            {
                this.writeToClient(e.Name + " panics and flees " + e.Direction + ".\r\n");
            }
        }

        private void HandlePlayerFleeFail(object sender, FleeFailEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                this.writeToClient(e.Name + " panics and tries to flee, but can't escape.\r\n");
            }
        }

        private void HandleNPCAttackedAndHit(object sender, AttackedAndHitEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                if (e.AttackerName == this.name)
                {
                    this.writeToClient("You hit " + e.DefenderName + ", doing some damage.\r\n");
                }
                else
                {
                    this.writeToClient(e.AttackerName + " hits " + e.DefenderName + ", doing some damage.\r\n");
                }
            }

        }

        private void HandlePlayerAttackedAndHit(object sender, AttackedAndHitEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                if (e.AttackerName == this.name)
                {
                    this.writeToClient("You hit " + e.DefenderName + ", doing some damage.\r\n\r\n");
                }
                else
                {
                    this.writeToClient(e.AttackerName + " hits " + e.DefenderName + ", doing some damage.\r\n");
                }
            }
        }

        private void HandleNPCDied(object sender, DiedEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                if (sender == this.combatTarget)
                {
                    this.combatTarget = null;
                    this.inCombat = false;
                    this.writeToClient(e.DefenderName + " collapsed... DEAD!\r\n");
                }
            }
        }

        private void HandleNPCSpawned(object sender, SpawnedEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                this.writeToClient(e.Name + " arrives.\r\n");
            }
        }

        private void HandleLightExpired(object sender, LightExpiredEventArgs e)
        {
            if (this.currentRoom.LightedRoom || !this.isNight)
            {
                this.writeToClient(e.Name + " goes out.\r\n");
                this.inventory.Light = null;
            }
            else
            {
                this.writeToClient(e.Name + " goes out, leaving you in darkness.\r\n");
                this.inventory.Light = null;
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

            if (this.inCombat)
            {
                if (this.combatTarget != null)
                {
                    if (!this.npcs[npcs.IndexOf((NPC)combatTarget)].IsDead)
                    {
                        this.npcs[npcs.IndexOf((NPC)combatTarget)].ReceiveAttack(2, this.name);
                    }
                    else if (this.npcs[npcs.IndexOf((NPC)combatTarget)].IsDead)
                    {
                        this.combatTarget = null;
                        this.inCombat = false;
                    }
                }
            }
        }
    }
}
