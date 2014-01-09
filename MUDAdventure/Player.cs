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
using System.Reflection;

using MUDAdventure.Items;
using MUDAdventure.Skills;

namespace MUDAdventure
{ 
    /// <summary>
    /// The main class for all "player" objects
    /// </summary>
    class Player : Character
    {
        public event EventHandler<PlayerConnectedEventArgs> PlayerConnected;
        public event EventHandler<PlayerMovedEventArgs> PlayerMoved;
        public event EventHandler<PlayerDisconnectedEventArgs> PlayerDisconnected;
        public event EventHandler<FledEventArgs> PlayerFled;
        public event EventHandler<FleeFailEventArgs> PlayerFleeFail;
        //public event EventHandler<AttackedAndHitEventArgs> PlayerAttackedAndHit;

        private static Colorizer colorizer = new Colorizer();
        private static ExperienceChart expChart = new ExperienceChart();       

        //TODO: temporary solution, what a headache this gave me.  stupid debug folder making copies of databases YAAAARRRRGHGH!
        private MUDAdventureDataContext db = new MUDAdventureDataContext(@"C:\Users\rswoody\Documents\Visual Studio 2010\Projects\MUDAdventure\MUDAdventure\MUDAdventure.mdf");

        private TcpClient tcpClient;
        NetworkStream clientStream;
        ASCIIEncoding encoder;

        private SkillTree universalSkillTree = new SkillTree();
        private ObservableCollection<Player> players; //a list of all connected players
        private Dictionary<string, Room> rooms; //a dictionary of all rooms where the key is "x,y,z"
        private List<NPC> npcs; //a list of all npcs
        private List<Item> itemList; //a list of all items
        private List<Item> expirableItemList;   
        private int worldTime; //what time it is according to the world's clock                
        private double maxCarryWeight;
        private int currentLevelExp, totalExperience;

        /****************************************/
        /*           TIMERS                     */
        /****************************************/

        private System.Timers.Timer savePlayerTimer;        

        private static object playerlock = new object();
        private static object npclock = new object();
        private static object itemlock = new object();
        private static object expirableitemlock = new object();
        private static object skilltreelock = new object();

        /****************************************/
        /*        FINITE STATE MACHINES         */
        /****************************************/
        //private bool inCombat = false;
        private bool isNight = false;
        private bool enteringName, enteringPassword, mainGame; //is the player entering their name, entering their password, or in the main game loop
        private bool connected;
        //private bool isDead = false; //you don't wanna be this, you're dead HAH!
        //private bool isFleeing = false;
        
        public Player(TcpClient client, ref ObservableCollection<Player> playerlist, Dictionary<string, Room> roomlist, ref List<NPC> npcs, System.Timers.Timer timer, int time, ref List<Item> itemlist, ref List<Item> expirableItemList, SkillTree _universalSkillTree)
        {
            //assign a bunch of stuff that's passed in from the server then the Player is created in memory
            this.tcpClient = client;
            this.name = null;
            this.players = playerlist;

            this.rooms = roomlist;
            this.npcs = npcs;
            this.itemList = itemlist;

            //the universal skill tree that lists all skills that can be learned along with each skill's effects etc.
            this.universalSkillTree = _universalSkillTree;

            //timer stuff for saving player data at set intervals
            this.savePlayerTimer = new System.Timers.Timer();
            this.savePlayerTimer.Interval = 300000;                  

            //assign handler to savePlayerTimer so player data will be saved every xx minutes
            this.savePlayerTimer.Elapsed += new ElapsedEventHandler(savePlayerTimer_Elapsed);

            this.savePlayerTimer.Enabled = true;
            this.savePlayerTimer.Start();            

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

        public void initialize(object e)
        {            
            this.clientStream = tcpClient.GetStream();
            this.encoder = new ASCIIEncoding();

            this.enteringName = true;
            this.writeToClient(colorizer.Colorize("Welcome to my MUD\r\nWhat is your name, traveller? ", "yellow"));
            
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
                        //make sure the player isn't already connected
                        bool alreadyConnected = false;
                        Monitor.TryEnter(playerlock, 3000);
                        try
                        {
                            var playerAlreadyConnectedQuery = (from player in players
                                                              where player.name.ToLower() == tempName.ToLower()
                                                              select player).FirstOrDefault();
                            if (playerAlreadyConnectedQuery != null)
                            {
                                alreadyConnected = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                            Debug.Print(ex.StackTrace);
                        }
                        finally
                        {
                            Monitor.Exit(playerlock);
                        }

                        if (alreadyConnected)
                        {
                            this.writeToClient(colorizer.Colorize("Sorry, this character is already connected to the game.\r\n", "red") + colorizer.Colorize("If you own this character and believe that it shouldn't be connected or may have been stolen, please contact game administrators.\r\n", "reset"));
                            this.Disconnect();
                        }
                        else
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
                                    this.writeToClient(colorizer.Colorize("INCORRECT PASSWORD", "red") + colorizer.Colorize("Please re-enter your password", "reset"));
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
                                            player.AttackedAndHit += this.HandleAttackedAndHit;
                                            player.AttackedAndDodge += this.HandleAttackedAndDodge;
                                            player.Died += this.HandlePlayerDied;
                                            player.Attack += this.HandlePlayerAttack;
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
                                        npc.AttackedAndHit += this.HandleAttackedAndHit;
                                        npc.Died += this.HandleNPCDied;
                                        npc.NPCSpawned += this.HandleNPCSpawned;
                                        npc.AttackedAndDodge += this.HandleAttackedAndDodge;
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

                                this.x = playerQuery[0].X;
                                this.y = playerQuery[0].Y;
                                this.z = playerQuery[0].Z;

                                this.level = playerQuery[0].Level;

                                this.strength = playerQuery[0].Strength;
                                this.agility = playerQuery[0].Agility;
                                this.constitution = playerQuery[0].Constitution;
                                this.intelligence = playerQuery[0].Intelligence;
                                this.learning = playerQuery[0].Learning;

                                this.totalExperience = playerQuery[0].TotalExperience;
                                this.currentLevelExp = playerQuery[0].ExpThisLevel;

                                var itemsQuery = (from item in db.InventoryItems
                                                  where item.PlayerName.ToLower() == this.name.ToLower()
                                                  select item).ToList();

                                foreach (InventoryItem item in itemsQuery)
                                {
                                    dynamic tempitem = null;

                                    //adding all the general inventory items from the database back into a player's general inventory
                                    switch (item.InventoryItemStatus.InventoryItemStatusName)
                                    {
                                        //the case for items that are coded for "general inventory".  This must contain **ALL** created item types
                                        case "generalinventory":
                                            switch (item.ItemType)
                                            {
                                                case "MUDAdventure.Items.Dagger":
                                                    //Dagger tempdag = new Dagger(item.ItemName, item.ItemDescription, item.ItemWeight, 0, 0, 0, 0, false, new List<string>(item.ItemRefNames.Split(',')), this.expirableItemList, item.ItemDamage, item.ItemSpeed);
                                                    tempitem = new Dagger();

                                                    if (item.ItemDamage.HasValue)
                                                    {
                                                        tempitem.Damage = Convert.ToInt32(item.ItemDamage);
                                                    }

                                                    if (item.ItemSpeed.HasValue)
                                                    {
                                                        tempitem.Speed = Convert.ToInt32(item.ItemSpeed);
                                                    }
                                                    break;
                                                case "MUDAdventure.Items.Light":
                                                    tempitem = new Light();

                                                    if (item.ItemCurrentFuel.HasValue)
                                                    {
                                                        tempitem.CurrentFuel = Convert.ToInt32(item.ItemCurrentFuel);
                                                    }

                                                    if (item.ItemTotalFuel.HasValue)
                                                    {
                                                        tempitem.TotalFuel = Convert.ToInt32(item.ItemTotalFuel);
                                                    }
                                                    break;
                                                case "MUDAdventure.Items.Headwear":
                                                    tempitem = new Headwear();

                                                    if (item.ItemArmorValue.HasValue)
                                                    {
                                                        tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                                    }
                                                    break;
                                                case "MUDAdventure.Items.Shirt":
                                                    tempitem = new Shirt();

                                                    if (item.ItemArmorValue.HasValue)
                                                    {
                                                        tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                                    }
                                                    break;
                                                case "MUDAdventure.Items.Gloves":
                                                    tempitem = new Gloves();

                                                    if (item.ItemArmorValue.HasValue)
                                                    {
                                                        tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                                    }
                                                    break;
                                                case "MUDAdventure.Items.Pants":
                                                    tempitem = new Pants();

                                                    if (item.ItemArmorValue.HasValue)
                                                    {
                                                        tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                                    }
                                                    break;
                                                case "MUDAdventure.Items.Boots":
                                                    tempitem = new Boots();

                                                    if (item.ItemArmorValue.HasValue)
                                                    {
                                                        tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                                    }
                                                    break;
                                            }

                                            tempitem.Name = item.ItemName;
                                            tempitem.Description = item.ItemDescription;
                                            tempitem.Weight = item.ItemWeight;
                                            tempitem.Expirable = true;
                                            tempitem.RefNames = new List<string>(item.ItemRefNames.Split(','));

                                            this.inventory.AddItem(tempitem);

                                            break;

                                        //case for items that are coded "wielded".  This only needs to contain logic for descendants of class Weapon
                                        case "wielded":
                                            switch (item.ItemType)
                                            {
                                                case "MUDAdventure.Items.Dagger":
                                                    tempitem = new Dagger();

                                                    if (item.ItemDamage.HasValue)
                                                    {
                                                        tempitem.Damage = Convert.ToInt32(item.ItemDamage);
                                                    }

                                                    if (item.ItemSpeed.HasValue)
                                                    {
                                                        tempitem.Speed = Convert.ToInt32(item.ItemSpeed);
                                                    }

                                                    break;
                                                case "MUDAdventure.Items.Sword":
                                                    tempitem = new Sword();

                                                    if (item.ItemDamage.HasValue)
                                                    {
                                                        tempitem.Damage = Convert.ToInt32(item.ItemDamage);
                                                    }

                                                    if (item.ItemSpeed.HasValue)
                                                    {
                                                        tempitem.Speed = Convert.ToInt32(item.ItemSpeed);
                                                    }

                                                    break;
                                                case "MUDAdventure.Items.Axe":
                                                    tempitem = new Axe();

                                                    if (item.ItemDamage.HasValue)
                                                    {
                                                        tempitem.Damage = Convert.ToInt32(item.ItemDamage);
                                                    }

                                                    if (item.ItemSpeed.HasValue)
                                                    {
                                                        tempitem.Speed = Convert.ToInt32(item.ItemSpeed);
                                                    }

                                                    break;

                                            }

                                            tempitem.Name = item.ItemName;
                                            tempitem.Description = item.ItemDescription;
                                            tempitem.Weight = item.ItemWeight;
                                            tempitem.Expirable = true;
                                            tempitem.RefNames = new List<string>(item.ItemRefNames.Split(','));

                                            this.inventory.Wielded = tempitem;

                                            break;
                                        case "light":
                                            Light templight = new Light();
                                            templight.Name = item.ItemName;
                                            templight.Description = item.ItemDescription;
                                            templight.Weight = item.ItemWeight;
                                            templight.Expirable = true;
                                            templight.RefNames = new List<string>(item.ItemRefNames.Split(','));
                                            if (item.ItemTotalFuel.HasValue)
                                            {
                                                templight.TotalFuel = Convert.ToInt32(item.ItemTotalFuel);
                                            }

                                            if (item.ItemCurrentFuel.HasValue)
                                            {
                                                templight.CurrentFuel = Convert.ToInt32(item.ItemCurrentFuel);
                                            }

                                            this.inventory.Light = templight;
                                            break;
                                        case "head":
                                            tempitem = new Headwear();
                                            tempitem.Name = item.ItemName;
                                            tempitem.Description = item.ItemDescription;
                                            tempitem.Weight = item.ItemWeight;
                                            tempitem.Expirable = true;
                                            tempitem.RefNames = new List<string>(item.ItemRefNames.Split(','));
                                            if (item.ItemArmorValue.HasValue)
                                            {
                                                tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                            }

                                            this.inventory.Head = tempitem;

                                            break;
                                        case "torso":
                                            tempitem = new Shirt();
                                            tempitem.Name = item.ItemName;
                                            tempitem.Description = item.ItemDescription;
                                            tempitem.Weight = item.ItemWeight;
                                            tempitem.Expirable = true;
                                            tempitem.RefNames = new List<string>(item.ItemRefNames.Split(','));
                                            if (item.ItemArmorValue.HasValue)
                                            {
                                                tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                            }

                                            this.inventory.Shirt = tempitem;

                                            break;
                                        case "hands":
                                            tempitem = new Gloves();
                                            tempitem.Name = item.ItemName;
                                            tempitem.Description = item.ItemDescription;
                                            tempitem.Weight = item.ItemWeight;
                                            tempitem.Expirable = true;
                                            tempitem.RefNames = new List<string>(item.ItemRefNames.Split(','));
                                            if (item.ItemArmorValue.HasValue)
                                            {
                                                tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                            }

                                            this.inventory.Gloves = tempitem;

                                            break;
                                        case "legs":
                                            tempitem = new Pants();
                                            tempitem.Name = item.ItemName;
                                            tempitem.Description = item.ItemDescription;
                                            tempitem.Weight = item.ItemWeight;
                                            tempitem.Expirable = true;
                                            tempitem.RefNames = new List<string>(item.ItemRefNames.Split(','));
                                            if (item.ItemArmorValue.HasValue)
                                            {
                                                tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                            }

                                            this.inventory.Pants = tempitem;

                                            break;
                                        case "feet":
                                            tempitem = new Boots();
                                            tempitem.Name = item.ItemName;
                                            tempitem.Description = item.ItemDescription;
                                            tempitem.Weight = item.ItemWeight;
                                            tempitem.Expirable = true;
                                            tempitem.RefNames = new List<string>(item.ItemRefNames.Split(','));
                                            if (item.ItemArmorValue.HasValue)
                                            {
                                                tempitem.ArmorValue = Convert.ToInt32(item.ItemArmorValue);
                                            }

                                            this.inventory.Boots = tempitem;

                                            break;

                                    }
                                }

                                //timer stuff for regen-ing player health based on 
                                this.healthRegen = new System.Timers.Timer();
                                this.healthRegen.Elapsed += new ElapsedEventHandler(healthRegen_Elapsed);
                                this.healthRegen.Interval = Math.Round(60000.0 / (double)this.constitution);
                                this.healthRegen.Enabled = true;
                                this.healthRegen.Start();


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
                            newPlayer.ExpThisLevel = 0;
                            newPlayer.TotalExperience = 0;

                            try
                            {
                                this.db.PlayerCharacters.InsertOnSubmit(newPlayer);                                
                                this.db.SubmitChanges();

                                this.writeToClient("Character created and saved to database.");

                                this.name = tempName;

                                this.connected = true;
                                this.mainGame = true;

                                this.x = newPlayer.X;
                                this.y = newPlayer.Y;
                                this.z = newPlayer.Z;

                                this.level = newPlayer.Level;
                                //this.expUntilNext = newPlayer.ExpUntilNext;

                                this.strength = newPlayer.Strength;
                                this.agility = newPlayer.Agility;
                                this.constitution = newPlayer.Constitution;
                                this.intelligence = newPlayer.Intelligence;
                                this.learning = newPlayer.Learning;

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
            else if (input.StartsWith("wear"))
            {
                if (input.Length > 4) //has no args after it
                {
                    string args = input.Substring(5);
                    this.Wear(args.ToLower());
                }
                else // no args
                {
                    this.writeToClient("Wear what?");
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
            else if (input.StartsWith("con"))
            {
                string[] temp = input.Split(' ');
                string args = temp[1];
                this.Consider(args);
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
            else if (input.StartsWith("who"))
            {
                this.Who();
            }
            else if (input.StartsWith("flee"))
            {
                this.Flee();
            }
            else if (input.StartsWith("skills"))
            {
                //TODO: list player's specific skills and proficiencies
            }
            else if (input.StartsWith("skill tree"))
            {
                //list the game's specific skill tree
                this.DisplaySkillTree();
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

        private void DisplaySkillTree()
        {
            StringBuilder sb = new StringBuilder();
            SkillTree tempSkillTree = new SkillTree();

            sb.AppendLine("\r\nSkill Tree\r\nFor more information type \"help <skillname>\"");
            sb.AppendLine("Name\tLevel Req");

            Monitor.TryEnter(skilltreelock, 3000);
            try
            {
                tempSkillTree = this.universalSkillTree;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
            finally
            {
                Monitor.Exit(skilltreelock);
            }

            foreach (Skill s in tempSkillTree.Skills)
            {
                sb.AppendLine(s.SkillName + "\tLvl " + s.LevelRequired);
            }

            this.writeToClient(sb.ToString());
        }

        private void Consider(string args)
        {
            List<NPC> npcQuery = new List<NPC>();
            List<Player> playerQuery = new List<Player>(); ;
            List<Character> considerTargets = new List<Character>();

            Monitor.TryEnter(npclock, 3000);
            try
            {
                npcQuery = (from character in this.npcs
                            where character.X == this.x && character.Y == this.y && character.Z == this.z && character.RefNames.Contains(args.ToLower())
                            select character).ToList();
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

            Monitor.TryEnter(playerlock, 3000);
            try
            {
                playerQuery = (from player in this.players
                               where player.X == this.x && player.Y == this.y && player.Z == this.z && player.Name.ToLower() == args.ToLower()
                               select player).ToList();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
            finally
            {
                Monitor.Exit(playerlock);
            }

            considerTargets.AddRange(npcQuery.ToArray());
            considerTargets.AddRange(playerQuery.ToArray());

            if (considerTargets.Count > 0)
            {
                int levelDifference = considerTargets.First().Level - this.level;

                if (levelDifference == 0)
                {
                    this.writeToClient(considerTargets.First().Name + " seems like the perfect match.\r\n");
                }
                else if (levelDifference == -1)
                {
                    this.writeToClient(considerTargets.First().Name + " seems a little bit weaker than you.\r\n");
                }
                else if (levelDifference == -2)
                {
                    this.writeToClient(considerTargets.First().Name + " seems a lot weaker than you.\r\n");
                }
                else if (levelDifference == -3)
                {
                    this.writeToClient(considerTargets.First().Name + " seems pretty pathetic compared to you.\r\n");
                }
                else if (levelDifference <= -4)
                {
                    this.writeToClient(considerTargets.First().Name + " cowers in fear when you glance at it.\r\n");
                }
                else if (levelDifference == 1)
                {
                    this.writeToClient(considerTargets.First().Name + " seems a little bit stronger than you.\r\n");
                }
                else if (levelDifference == 2)
                {
                    this.writeToClient(considerTargets.First().Name + " seems a lot stronger than you.\r\n");
                }
                else if (levelDifference == 3)
                {
                    this.writeToClient(considerTargets.First().Name + " seems VERY dangerous.\r\n");
                }
                else if (levelDifference >= 4)
                {
                    this.writeToClient("Your knees start to knock together when you glance at "+ considerTargets.First().Name +".\r\n");
                }
            }
            else
            {
                this.writeToClient("Consider who??\r\n");
            }
        }

        private void Wear(string args)
        {
            List<Item> items = this.inventory.ListInventory();
            List<Item> itemsQuery = (from item in items
                                     where item.RefNames.Contains(args)
                                     select item).ToList();

            if (itemsQuery.Count >= 1)
            {
                if (itemsQuery.First().GetType().BaseType.ToString() == "MUDAdventure.Items.Apparel")
                {
                    StringBuilder message = new StringBuilder();

                    switch (itemsQuery.First().GetType().ToString())
                    {
                        case "MUDAdventure.Items.Headwear":
                            //remove the currently equipped bit of headwear (if any) and return it to general inventory
                            if (this.inventory.Head != null)
                            {
                                message.Append("You stop wearing " + this.inventory.Head.Name + " on your head.\r\n");
                                this.inventory.AddItem(this.inventory.Head);
                            }

                            //now wear the new one!
                            this.inventory.Head = itemsQuery.First().ToHeadwear();
                            message.Append("You start wearing " + this.inventory.Head.Name + " on your head.\r\n");

                            //and remove it from general inventory
                            this.inventory.RemoveItem(itemsQuery.First());

                            this.writeToClient(message.ToString());
                            break;

                        case "MUDAdventure.Items.Shirt":
                            //remove the currently equipped bit of headwear (if any) and return it to general inventory
                            if (this.inventory.Shirt != null)
                            {
                                message.Append("You stop wearing " + this.inventory.Shirt.Name + " as a shirt.\r\n");
                                this.inventory.AddItem(this.inventory.Shirt);
                            }

                            //now wear the new one!
                            this.inventory.Shirt = itemsQuery.First().ToShirt();
                            message.Append("You start wearing " + this.inventory.Shirt.Name + " as a shirt.\r\n");

                            //and remove it from general inventory
                            this.inventory.RemoveItem(itemsQuery.First());

                            this.writeToClient(message.ToString());
                            break;

                        case "MUDAdventure.Items.Gloves":
                            //remove the currently equipped bit of headwear (if any) and return it to general inventory
                            if (this.inventory.Gloves != null)
                            {
                                message.Append("You stop wearing " + this.inventory.Gloves.Name + " on your hands.\r\n");
                                this.inventory.AddItem(this.inventory.Gloves);
                            }

                            //now wear the new one!
                            this.inventory.Gloves = itemsQuery.First().ToGloves();
                            message.Append("You start wearing " + this.inventory.Gloves.Name + " on your hands.\r\n");

                            //and remove it from general inventory
                            this.inventory.RemoveItem(itemsQuery.First());

                            this.writeToClient(message.ToString());
                            break;

                        case "MUDAdventure.Items.Pants":
                            //remove the currently equipped bit of Pantswear (if any) and return it to general inventory
                            if (this.inventory.Pants != null)
                            {
                                message.Append("You stop wearing " + this.inventory.Pants.Name + " as pants.\r\n");
                                this.inventory.AddItem(this.inventory.Pants);
                            }

                            //now wear the new one!
                            this.inventory.Pants = itemsQuery.First().ToPants();
                            message.Append("You start wearing " + this.inventory.Pants.Name + " as pants.\r\n");

                            //and remove it from general inventory
                            this.inventory.RemoveItem(itemsQuery.First());

                            this.writeToClient(message.ToString());
                            break;

                        case "MUDAdventure.Items.Boots":
                            //remove the currently equipped bit of Bootswear (if any) and return it to general inventory
                            if (this.inventory.Boots != null)
                            {
                                message.Append("You stop wearing " + this.inventory.Boots.Name + " on your feet.\r\n");
                                this.inventory.AddItem(this.inventory.Boots);
                            }

                            //now wear the new one!
                            this.inventory.Boots = itemsQuery.First().ToBoots();
                            message.Append("You start wearing " + this.inventory.Boots.Name + " on your feet.\r\n");

                            //and remove it from general inventory
                            this.inventory.RemoveItem(itemsQuery.First());

                            this.writeToClient(message.ToString());
                            break;
                    }
                }
                else
                {
                    this.writeToClient("What?! Don't be preposterous; you can't wear THAT!\r\n");
                }
            }
            else
            {
                this.writeToClient("You're not carrying any such item.\r\n");
            }
        }

        private void Who()
        {
            StringBuilder message = new StringBuilder();
            List<Player> tempplayers = new List<Player>();

            Monitor.TryEnter(playerlock, 3000);
            try
            {
                tempplayers = this.players.ToList<Player>();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
            finally
            {
                Monitor.Exit(playerlock);
            }

            if (tempplayers != null)
            {
                message.AppendLine("\r\nPlayers:");
                foreach (Player player in tempplayers)
                {
                    message.AppendLine("[Lvl " + player.Level + "] " + player.Name);
                }

                this.writeToClient(message.ToString() + "\r\n");

            }
        }

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
                    //this.inventory.Light.IsLit = true;
                    this.inventory.Light.Ignite();

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
                if (itemsQuery[0].GetType().ToString().EndsWith("Dagger") /* || sword || spear || etc. */)
                {
                    StringBuilder message = new StringBuilder();

                    //remove current weapon if any and add it back to general inv
                    if (this.inventory.Wielded != null)
                    {
                        message.Append("You stop wielding " + this.inventory.Wielded.Name + ".\r\n");
                        this.inventory.AddItem((Dagger)this.inventory.Wielded);
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

            equipmentlist.Append("[Head] \t\t\t");
            if (this.inventory.Head != null)
            {
                equipmentlist.AppendLine(this.inventory.Head.Name);
            }
            else
            {
                equipmentlist.AppendLine("Nothing");
            }

            equipmentlist.Append("[Shirt] \t\t");
            if (this.inventory.Shirt != null)
            {
                equipmentlist.AppendLine(this.inventory.Shirt.Name);
            }
            else
            {
                equipmentlist.AppendLine("Nothing");
            }

            equipmentlist.Append("[Hands] \t\t");
            if (this.inventory.Gloves != null)
            {
                equipmentlist.AppendLine(this.inventory.Gloves.Name);
            }
            else
            {
                equipmentlist.AppendLine("Nothing");
            }

            equipmentlist.Append("[Pants] \t\t");
            if (this.inventory.Pants != null)
            {
                equipmentlist.AppendLine(this.inventory.Pants.Name);
            }
            else
            {
                equipmentlist.AppendLine("Nothing");
            }

            equipmentlist.Append("[Feet] \t\t\t");
            if (this.inventory.Boots != null)
            {
                equipmentlist.AppendLine(this.inventory.Boots.Name);
            }
            else
            {
                equipmentlist.AppendLine("Nothing");
            }
            //TODO: add for shield

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

                        //check to see if any players are here
                        Monitor.TryEnter(playerlock, 3000);
                        try
                        {
                            List<Player> playerQuery = (from player in this.players
                                                        where player.X == this.x && player.Y == this.y && player.Z == this.z && player != this
                                                        select player).ToList();
                            foreach (Player player in playerQuery)
                            {
                                message += "\r\n" + player.Name + " is here.";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                            Debug.Print(ex.StackTrace);
                        }
                        finally
                        {
                            Monitor.Exit(playerlock);
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
            var itemQuery = (from item in this.inventory.ListInventory()
                             where item.RefNames.Contains(args)
                             select item).ToList();         

            if (itemQuery.Any())
            {
                dynamic tempitem = null;

                switch (itemQuery.First().GetType().ToString())
                {
                    case "MUDAdventure.Items.Dagger":
                        tempitem = new Dagger(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToDagger().Damage, itemQuery.First().ToDagger().Speed);                        
                        break;
                    case "MUDAdventure.Items.Light":
                        tempitem = new Light(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToLight().CurrentFuel, itemQuery.First().ToLight().TotalFuel);
                        break;
                    case "MUDAdventure.Items.Headwear":
                        tempitem = new Headwear(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToHeadwear().ArmorValue);
                        break;
                    case "MUDAdventure.Items.Shirt":
                        tempitem = new Shirt(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToShirt().ArmorValue);
                        break;
                    case "MUDAdventure.Items.Gloves":
                        tempitem = new Gloves(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToGloves().ArmorValue);
                        break;
                    case "MUDAdventure.Items.Pants":
                        tempitem = new Pants(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToPants().ArmorValue);
                        break;
                    case "MUDAdventure.Items.Boots":
                        tempitem = new Boots(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToBoots().ArmorValue);
                        break;
                    default:
                        break;
                }

                Monitor.TryEnter(expirableitemlock, 3000);
                try
                {
                    this.expirableItemList.Add(tempitem);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                    Debug.Print(ex.StackTrace);
                }

                writeToClient("You drop " + itemQuery.First().Name);
                this.inventory.RemoveItem(itemQuery.First());
            }
            else
            {
                this.writeToClient("You're not carrying any such item.\r\n");
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
                var itemQuery = (from item in this.itemList
                                 //from expitem in this.expirableItemList
                                 where item.RefNames.Contains(args) && item.X == this.x && item.Y == this.y && item.Z == this.z //|| (expitem.RefNames.Contains(args) && expitem.X == this.x && expitem.Y == this.y && expitem.Z == this.z)
                                 select item).ToList();
                itemQuery.AddRange((from expitem in this.expirableItemList
                              where expitem.RefNames.Contains(args) && expitem.X == this.x && expitem.Y == this.y && expitem.Z == this.z
                              select expitem).ToList());

                if (itemQuery.Any())
                {
                    dynamic tempitem = null;

                    if ((itemQuery.First().Weight + this.inventory.Weight) <= this.maxCarryWeight)
                    {
                        switch (itemQuery.First().GetType().ToString())
                        {
                            case "MUDAdventure.Items.Dagger":
                                tempitem = new Dagger(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToDagger().Damage, itemQuery.First().ToDagger().Speed);
                                break;
                            case "MUDAdventure.Items.Light":
                                tempitem = new Light(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToLight().CurrentFuel, itemQuery.First().ToLight().TotalFuel);
                                break;
                            case "MUDAdventure.Items.Headwear":
                                tempitem = new Headwear(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToHeadwear().ArmorValue);
                                break;
                            case "MUDAdventure.Items.Shirt":
                                tempitem = new Shirt(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToShirt().ArmorValue);
                                break;
                            case "MUDAdventure.Items.Gloves":
                                tempitem = new Gloves(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToGloves().ArmorValue);
                                break;
                            case "MUDAdventure.Items.Pants":
                                tempitem = new Pants(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToPants().ArmorValue);
                                break;
                            case "MUDAdventure.Items.Boots":
                                tempitem = new Boots(itemQuery.First().Name, itemQuery.First().Description, itemQuery.First().Weight, this.x, this.y, this.z, true, itemQuery.First().RefNames, ref this.expirableItemList, itemQuery.First().ToBoots().ArmorValue);
                                break;
                        }

                        this.inventory.AddItem(tempitem);

                        if (itemQuery.First().Expirable)
                        {
                            expirableItemList.Remove(itemQuery.First());
                        }

                        this.writeToClient("You pick up " + itemQuery.First().Name + ".\r\n");
                        itemQuery.First().PickedUp();
                    }
                    else
                    {
                        this.writeToClient("That item is too heavy for you to carry.\r\n");
                    }
                }
                else
                {
                    this.writeToClient("That item isn't here.\r\n");
                }
            }
            else
            {
                this.writeToClient("It's too dark to see.\r\n");
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
            message.AppendLine("You are " + this.name + ". You are currently level " + this.level + ".");
            message.AppendLine("You've earned " + this.totalExperience + " total experience points.");
            if (this.level < 100)
            {
                message.AppendLine("You've earned " + this.currentLevelExp + " experience points this level and need " + (expChart.ExpChart[this.level + 1] - this.currentLevelExp) + " more to advance.");
            }
            else
            {
                message.AppendLine("You've maxed out at level 100. Try the PRESTIGE command to reset, with a stat boost, of course.");
                //TODO: create prestige command and counter
            }
            message.AppendLine("HP: " + this.currentHitpoints + "/" + this.totalHitpoints + "; MV: " + this.currentMoves + "/" + this.totalMoves);
            message.AppendLine("You are carrying " + this.inventory.Weight + "/" + this.maxCarryWeight + " pounds.");
            message.AppendLine("STR: " + this.strength + ", AGI: " + this.agility + ", CON: " + this.constitution + ", INT: " + this.intelligence + ", LEA: " + this.learning);
            message.AppendLine("The time is " + this.worldTime);

            this.writeToClient(message.ToString());
        }

        private void Kill(string args)
        {
            if (!this.inCombat)
            {
                if (!this.isNight || this.currentRoom.LightedRoom || this.inventory.Light != null)
                {
                    List<Character> targets = new List<Character>();
                    
                    //check for the potential target in the NPC list
                    Monitor.TryEnter(npclock, 3000);
                    try
                    {
                        var target = (from npc in this.npcs
                                      where npc.X == this.x && npc.Y == this.y && npc.Z == this.z && npc.RefNames.Contains(args)
                                      select npc).ToList();
                        targets.AddRange(target);
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

                    //now check for the potential target in the Player list
                    Monitor.TryEnter(playerlock, 3000);
                    try
                    {
                        var target = (from player in this.players
                                      where player.X == this.x && player.Y == this.y && player.Z == this.Z && player.Name.ToLower() == args.ToLower() && player != this
                                      select player).ToList();

                        targets.AddRange(target);
                    }
                    catch (Exception ex)
                    {
                        Debug.Print(ex.Message);
                        Debug.Print(ex.StackTrace);
                    }
                    finally
                    {
                        Monitor.Exit(playerlock);
                    }

                    if (targets.Count > 0)
                    {
                        this.combatTarget = targets.First();
                        this.combatTarget.CombatTarget = this;
                    }

                    if (this.combatTarget != null)
                    {
                        this.OnAttack(new AttackEventArgs(this.combatTarget, this.x, this.y, this.z));
                        this.attackTimer = new System.Timers.Timer();
                        this.attackTimer.Elapsed += new ElapsedEventHandler(attackTimer_Elapsed);
                        this.attackTimer.Interval = Math.Ceiling((double)(60000 / this.agility));
                        this.attackTimer.Start();
                        this.writeToClient("You ATTACK " + this.combatTarget.Name + "!\r\n");
                    }
                    else
                    {
                        this.writeToClient("That person isn't here.\r\n");
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
            
            this.Save();

            //disconnecting
            this.writeToClient("Disconnecting...");

            if (this.connected)
            {
                this.connected = false;

                //unsubscribe from events
                lock (playerlock)
                {
                    this.players.CollectionChanged -= this.playerListUpdated;

                    //TODO: unsubscribe all timer event handlers and stop all running timers
                    this.healthRegen.Stop();
                    this.healthRegen.Elapsed -= this.healthRegen_Elapsed;
                    this.healthRegen.Dispose();

                    this.savePlayerTimer.Stop();
                    this.savePlayerTimer.Elapsed -= this.savePlayerTimer_Elapsed;
                    this.savePlayerTimer.Dispose();

                    foreach (Player player in this.players)
                    {
                        player.PlayerConnected -= this.HandlePlayerConnected;
                        player.PlayerMoved -= this.HandlePlayerMoved;
                        player.PlayerDisconnected -= this.HandlePlayerDisconnected;
                        player.PlayerFled -= this.HandlePlayerFled;
                        player.PlayerFleeFail -= this.HandlePlayerFleeFail;
                        player.AttackedAndHit -= this.HandleAttackedAndHit;
                        player.Died -= this.HandlePlayerDied;
                    }

                    foreach (NPC npc in this.npcs)
                    {
                        npc.NPCFled -= this.HandleNPCFled;
                        npc.NPCMoved -= this.HandleNPCMoved;
                        npc.NPCFleeFail -= this.HandleNPCFleeFail;
                        npc.AttackedAndHit -= this.HandleAttackedAndHit;
                        npc.Died -= this.HandleNPCDied;
                        npc.NPCSpawned -= this.HandleNPCSpawned;
                        npc.AttackedAndDodge -= this.HandleAttackedAndDodge;
                    }
                }
            }

            this.clientStream.Close();
            this.tcpClient.Close();

            this.OnPlayerDisconnected(new PlayerDisconnectedEventArgs(this.name));
        }

        //public override void ReceiveAttack(int potentialdamage, string attackerName)
        //{
        //    //base(potentialdamage, attackerName);
        //}

        private void Save()
        {
            if (this.connected)
            {
                //saving player
                var playerQuery =
                    (from playercharacter in db.PlayerCharacters
                     where playercharacter.PlayerName.ToString().ToLower() == this.name.ToLower()
                     select playercharacter).First();

                playerQuery.X = this.x;
                playerQuery.Y = this.y;
                playerQuery.Z = this.z;

                playerQuery.TotalExperience = this.totalExperience;
                playerQuery.ExpThisLevel = this.currentLevelExp;

                playerQuery.Level = this.level;

                //now to save items in db

                //first let's get a list of all items currently saved for this player in the db
                //we're going to save the new items to the db, then delete all of these items, instead of trying to figure out
                //what the exact changes are.
                var dbItems = from item in db.InventoryItems
                              where item.PlayerName.ToLower() == this.name.ToLower()
                              select item;

                //let's start saving items to the db
                List<Item> items = this.inventory.ListInventory();
                foreach (Item item in items)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = item.Name;
                    invItem.ItemDescription = item.Description;
                    invItem.ItemWeight = item.Weight;
                    invItem.ItemRefNames = String.Join(",", item.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 10;
                    invItem.ItemType = item.GetType().ToString();

                    switch (item.GetType().ToString())
                    {
                        case "MUDAdventure.Items.Dagger":
                            invItem.ItemDamage = item.ToDagger().Damage;
                            invItem.ItemSpeed = item.ToDagger().Speed;
                            break;
                        case "MUDAdventure.Items.Sword":
                            invItem.ItemDamage = item.ToSword().Damage;
                            invItem.ItemSpeed = item.ToSword().Speed;
                            break;
                        case "MUDAdventure.Items.Axe":
                            invItem.ItemDamage = item.ToAxe().Damage;
                            invItem.ItemSpeed = item.ToAxe().Speed;
                            break;
                        case "MUDAdventure.Items.Light":
                            invItem.ItemCurrentFuel = item.ToLight().CurrentFuel;
                            invItem.ItemTotalFuel = item.ToLight().TotalFuel;
                            break;
                        case "MUDAdventure.Items.Headwear":
                            invItem.ItemDamage = item.ToHeadwear().ArmorValue;
                            break;
                        case "MUDAdventure.Items.Shirt":
                            invItem.ItemDamage = item.ToShirt().ArmorValue;
                            break;
                        case "MUDAdventure.Items.Gloves":
                            invItem.ItemDamage = item.ToGloves().ArmorValue;
                            break;
                        case "MUDAdventure.Items.Pants":
                            invItem.ItemDamage = item.ToPants().ArmorValue;
                            break;
                        case "MUDAdventure.Items.Boots":
                            invItem.ItemDamage = item.ToBoots().ArmorValue;
                            break;
                    }

                    playerQuery.InventoryItems.Add(invItem);
                }

                //TODO: save items that are wielded/worn/held etc.
                if (this.inventory.Wielded != null)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = this.inventory.Wielded.Name;
                    invItem.ItemDescription = this.inventory.Wielded.Description;
                    invItem.ItemWeight = this.inventory.Wielded.Weight;
                    invItem.ItemRefNames = String.Join(",", this.inventory.Wielded.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 1;
                    invItem.ItemType = this.inventory.Wielded.GetType().ToString();

                    switch (this.inventory.Wielded.GetType().ToString())
                    {
                        case "MUDAdventure.Items.Dagger":
                            invItem.ItemDamage = this.inventory.Wielded.ToDagger().Damage;
                            invItem.ItemSpeed = this.inventory.Wielded.ToDagger().Speed;
                            break;
                        case "MUDAdventure.Items.Sword":
                            invItem.ItemDamage = this.inventory.Wielded.ToSword().Damage;
                            invItem.ItemSpeed = this.inventory.Wielded.ToSword().Speed;
                            break;
                        case "MUDAdventure.Items.Axe":
                            invItem.ItemDamage = this.inventory.Wielded.ToAxe().Damage;
                            invItem.ItemSpeed = this.inventory.Wielded.ToAxe().Speed;
                            break;
                        //case: MUDAdventure.Sword, spear, etc, etc
                    }

                    playerQuery.InventoryItems.Add(invItem);
                }

                if (this.inventory.Light != null)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = this.inventory.Light.Name;
                    invItem.ItemDescription = this.inventory.Light.Description;
                    invItem.ItemWeight = this.inventory.Light.Weight;
                    invItem.ItemRefNames = String.Join(",", this.inventory.Light.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 2;
                    invItem.ItemType = this.inventory.Light.GetType().ToString();

                    invItem.ItemCurrentFuel = this.inventory.Light.ToLight().CurrentFuel;
                    invItem.ItemTotalFuel = this.inventory.Light.ToLight().TotalFuel;

                    playerQuery.InventoryItems.Add(invItem);
                }

                if (this.inventory.Head != null)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = this.inventory.Head.Name;
                    invItem.ItemDescription = this.inventory.Head.Description;
                    invItem.ItemWeight = this.inventory.Head.Weight;
                    invItem.ItemRefNames = String.Join(",", this.inventory.Head.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 3;
                    invItem.ItemType = this.inventory.Head.GetType().ToString();

                    invItem.ItemArmorValue = this.inventory.Head.ArmorValue;

                    playerQuery.InventoryItems.Add(invItem);
                }

                if (this.inventory.Shirt != null)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = this.inventory.Shirt.Name;
                    invItem.ItemDescription = this.inventory.Shirt.Description;
                    invItem.ItemWeight = this.inventory.Shirt.Weight;
                    invItem.ItemRefNames = String.Join(",", this.inventory.Shirt.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 4;
                    invItem.ItemType = this.inventory.Shirt.GetType().ToString();

                    invItem.ItemArmorValue = this.inventory.Shirt.ArmorValue;

                    playerQuery.InventoryItems.Add(invItem);
                }

                if (this.inventory.Gloves != null)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = this.inventory.Gloves.Name;
                    invItem.ItemDescription = this.inventory.Gloves.Description;
                    invItem.ItemWeight = this.inventory.Gloves.Weight;
                    invItem.ItemRefNames = String.Join(",", this.inventory.Gloves.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 6;
                    invItem.ItemType = this.inventory.Gloves.GetType().ToString();

                    invItem.ItemArmorValue = this.inventory.Gloves.ArmorValue;

                    playerQuery.InventoryItems.Add(invItem);
                }

                if (this.inventory.Pants != null)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = this.inventory.Pants.Name;
                    invItem.ItemDescription = this.inventory.Pants.Description;
                    invItem.ItemWeight = this.inventory.Pants.Weight;
                    invItem.ItemRefNames = String.Join(",", this.inventory.Pants.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 7;
                    invItem.ItemType = this.inventory.Pants.GetType().ToString();

                    invItem.ItemArmorValue = this.inventory.Pants.ArmorValue;

                    playerQuery.InventoryItems.Add(invItem);
                }

                if (this.inventory.Boots != null)
                {
                    InventoryItem invItem = new InventoryItem();
                    invItem.PlayerName = this.name;
                    invItem.ItemName = this.inventory.Boots.Name;
                    invItem.ItemDescription = this.inventory.Boots.Description;
                    invItem.ItemWeight = this.inventory.Boots.Weight;
                    invItem.ItemRefNames = String.Join(",", this.inventory.Boots.RefNames.ToArray());
                    invItem.ItemInventoryStatusCode = 9;
                    invItem.ItemType = this.inventory.Boots.GetType().ToString();

                    invItem.ItemArmorValue = this.inventory.Boots.ArmorValue;

                    playerQuery.InventoryItems.Add(invItem);
                }

                //now that the new items are saved, let's delete all the old items.
                foreach (var item in dbItems)
                {
                    db.InventoryItems.DeleteOnSubmit(item);
                }

                //playerQuery.level = this.level;
                //playerQuery.ExpUntilNext = this.expUntilNext;
                this.writeToClient("Saving " + this.name + "...\r\n");

                try
                {
                    db.SubmitChanges();
                    //this.writeToClient(this.name + " saved.");
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                    Debug.Print(ex.StackTrace);
                }
            }
        }

        //protected virtual void OnAttackedAndHit(AttackedAndHitEventArgs e)
        //{
        //    EventHandler<AttackedAndHitEventArgs> handler = this.AttackedAndHit;

        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}

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

        protected override void Die()
        {
            base.Die();

            this.attackTimer.Enabled = false;

            this.writeToClient("You fall to one knee and gasp for breath.\r\nYour wounds are overwhelming you.\r\nAs the world begins to fade, " + this.combatTarget.Name + " stands over you, then finishes you off.\r\n" + colorizer.Colorize("You are DEAD!\r\n", "red"));
            
            this.inCombat = false;
            this.combatTarget = null;
            this.isDead = true;
        }

        private void LevelUp()
        {
            this.level++;
            this.writeToClient("Congrats!  You've gained a level!\r\n");
            if (expChart.ExpChart[level] - this.currentLevelExp < 0)
            {
                int tempXp = Math.Abs(expChart.ExpChart[level] - this.currentLevelExp);
                this.currentLevelExp = 0;
                this.currentLevelExp = tempXp;
            }
        }

        public override void ReceiveAttack(Character sender, int potentialdamage, string attackerName)
        {
            bool success = true;
            this.inCombat = true;

            this.combatTarget = sender;

            //dodging an attack
            if (this.rand.Next(1, 101) <= this.agility)
            {
                //raise attacked and dodged event
                this.OnAttackedAndDodge(new AttackedAndDodgeEventArgs(attackerName, this.name, this.x, this.y, this.z));
                success = false;
            }

            //TODO: implement parrying an attack

            if (success)
            {
                Monitor.TryEnter(this.hplock);
                try
                {
                    this.currentHitpoints -= potentialdamage;
                    this.writeToClient(sender.Name + " hits you, doing some damage.\r\n");
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

                this.OnAttackedAndHit(new AttackedAndHitEventArgs(attackerName, this.name, this.x, this.y, this.z));

                if (this.currentHitpoints <= 0)
                {
                    this.Die();
                }
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

        private void HandleAttackedAndHit(object sender, AttackedAndHitEventArgs e)
        {

            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                if (e.AttackerName == this.name)
                {
                    this.writeToClient("You hit " + e.DefenderName + ", doing some damage.\r\n\r\n");
                }
                else
                {
                    this.writeToClient(e.AttackerName + " hits " + e.DefenderName + ", doing some damage.\r\n\r\n");
                }
            }
        }

        private void HandleAttackedAndDodge(object sender, AttackedAndDodgeEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                if (e.AttackerName == this.name)
                {
                    this.writeToClient(e.DefenderName + " dodges your attack.\r\n");
                }
                else
                {
                    this.writeToClient(e.DefenderName + " dodges " + e.AttackerName + "'s attack.\r\n");
                }
            }
        }

        private void HandleNPCDied(object sender, DiedEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                if (sender == this.combatTarget)
                {
                    //this.combatTarget = null;
                    //TODO: extract this into a helper method so that handleplayerdied can call it too to keep this DRY
                    double baseXp = Math.Ceiling(Math.Sqrt(Math.Pow(Convert.ToDouble(this.combatTarget.Level) * 5000.0, 1.0 + (Convert.ToDouble(this.combatTarget.Level) / 100.0))));
                    int adjustedXp = Convert.ToInt32(Math.Ceiling(baseXp * (Convert.ToDouble(this.combatTarget.Level) / Convert.ToDouble(this.level))));
                    
                    this.inCombat = false;
                    this.writeToClient(e.DefenderName + " collapsed... DEAD!\r\nYour share of the experience is " + adjustedXp + " points.\r\n");
                    
                    this.currentLevelExp += adjustedXp;
                    this.totalExperience += adjustedXp;

                    if (this.level < 100)
                    {
                        if (this.currentLevelExp >= expChart.ExpChart[this.level+1])
                        {
                            this.LevelUp();
                        }
                    }
                }
                else
                {
                    this.writeToClient(e.AttackerName + " slays " + e.DefenderName + "\r\n");
                }
            }
        }

        private void HandlePlayerDied(object sender, DiedEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                if (sender == this.combatTarget)
                {
                    //this.combatTarget = null;
                    this.inCombat = false;
                    this.writeToClient(e.DefenderName + " collapsed... DEAD!\r\n");
                }
                else
                {
                    this.writeToClient(e.AttackerName + " slays " + e.DefenderName + "\r\n");
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

        private void savePlayerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Save();
        }

        private void attackTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!this.combatTarget.IsDead)
            {
                int damage = 0;
                if (this.inventory.Wielded != null)
                {
                    damage = Convert.ToInt32(Math.Sqrt(this.strength + this.inventory.Wielded.Damage) + rand.Next(1, this.level) + 1);
                }
                else
                {
                    damage = Convert.ToInt32(Math.Sqrt(this.strength + 0) + rand.Next(1, this.level) + 1);
                }
                Debug.Print(damage.ToString());
                this.combatTarget.ReceiveAttack(this, 5, this.name);
            }
            else
            {
                this.combatTarget = null;
                this.attackTimer.Enabled = false;
            }
        }

        private void HandlePlayerAttack(object sender, AttackEventArgs e)
        {
            if (e.X == this.x && e.Y == this.y && e.Z == this.z)
            {
                Character attacker = (Character)sender;

                if (e.CombatTarget == this)
                {
                    this.inCombat = true;
                    this.combatTarget = attacker;
                    
                    this.attackTimer = new System.Timers.Timer();
                    this.attackTimer.Elapsed += new ElapsedEventHandler(attackTimer_Elapsed);
                    this.attackTimer.Interval = Math.Ceiling((double)(60000 / this.agility));
                    this.attackTimer.Start();
                }
                else
                {
                    this.writeToClient(attacker.Name + " ATTACKS " + e.CombatTarget.Name + ".\r\n");
                }
            }
        }

        //private void OnTimedEvent(object sender, ElapsedEventArgs e)
        //{
        //    this.timeCounter++;
          
        //    //TODO: MOVE regen
        //    if (this.currentMoves < this.totalMoves)
        //    {
        //        if (this.timeCounter % 50 == 0)
        //        {
        //            this.currentMoves++;
        //        }
        //    }

        //    if (this.inCombat)
        //    {
        //        if (this.combatTarget != null)
        //        {
        //            if (!this.npcs[npcs.IndexOf((NPC)combatTarget)].IsDead)
        //            {
        //                this.npcs[npcs.IndexOf((NPC)combatTarget)].ReceiveAttack(2, this.name);
        //            }
        //            else if (this.npcs[npcs.IndexOf((NPC)combatTarget)].IsDead)
        //            {
        //                this.combatTarget = null;
        //                this.inCombat = false;
        //            }
        //        }
        //    }
        //}

        public string Name
        {
            get { return this.name; }
        }
    }
}
