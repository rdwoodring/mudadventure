using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Diagnostics;
using System.Threading;
using MUDAdventure.Items.Weapons;
using MUDAdventure.Items.Apparel;

namespace MUDAdventure.Items
{
    ///<summary>
    ///The Item class is the base class for all the items (including weapons, apparel, lights, containers, etc.).  Depending upon the situation, items may be respawnable, or expirable.  
    ///Respawnable items that are picked up will be copied to a player's inventory, disappear from that room in the game, then respawn when the set respawn interval elapses.  
    ///Expirable items that are dropped from a player's inventory will stay in that location until the expiration timer elapses, then it will disappear from the game.
    ///</summary>    
    abstract class Item
    {
        static object expirableItemListLock = new object();

        protected const int expireTime = 60000;

        protected string name, description;
        protected double weight;
        protected int spawnX, spawnY, spawnZ, x, y, z, spawntime, respawnCounter;
        protected bool spawnable, expirable, inInventory, spawned;
        protected List<string> refNames;
        protected List<Item> expirableItemList;
        //protected System.Timers.Timer worldTimer;
        protected System.Timers.Timer respawnTimer;
        protected System.Timers.Timer expirationTimer;

        /// <summary>
        /// Default constructor.        
        /// </summary>
        public Item() { }

        /// <summary>
        /// Constructor that takes any instance of a descendant of Item and copies all of its attributes to create a new instance.
        /// </summary>
        /// <param name="item">Any instance of descendant of Item.</param>
        public Item(Item item)
        {
            this.name = item.name;
            this.description = item.description;

            this.weight = item.weight;

            this.spawnX = item.spawnX;
            this.spawnY = item.spawnY;
            this.spawnZ = item.spawnZ;

            this.spawntime = item.spawntime;
            this.respawnCounter = item.respawnCounter;

            this.spawnable = item.spawnable;
            this.expirable = item.expirable;
            this.spawned = item.spawned;

            this.refNames = item.refNames;            
        }

        /// <summary>
        /// Class constructor for creating "respawnable" items.
        /// </summary>
        /// <param name="n">Name</param>
        /// <param name="d">Description</param>
        /// <param name="w">Weight</param>
        /// <param name="spx">Respawn X coordinate</param>
        /// <param name="spy">Respawn Y coordinate</param>
        /// <param name="spz">Respawn Z coordinate</param>
        /// <param name="sptime">Spawn time (milliseconds) that dictates how long it takes an item to "respawn" after it has been picked up by a player</param>
        /// <param name="spawn">Says that the item should be respawnable</param>
        /// <param name="rn">A list of "reference names" that players will use to reference this item during gameplay</param>
        public Item(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn)
        {
            this.name = n;
            this.description = d;

            this.weight = w;

            this.spawnX = spx;
            this.spawnY = spy;
            this.spawnZ = spz;

            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;

            this.spawntime = sptime;
            this.spawnable = spawn;

            this.refNames = rn;

            this.spawned = true;

            this.respawnTimer = new System.Timers.Timer();
            this.respawnTimer.Interval = sptime;
            this.respawnTimer.Elapsed += new ElapsedEventHandler(respawnTimer_Elapsed);

            this.respawnCounter = 0;            
        }

        /// <summary>
        /// Class constructor for creating "expirable" items.
        /// </summary>
        /// <param name="n">Name</param>
        /// <param name="d">Description</param>
        /// <param name="w">Weight</param>
        /// <param name="x">Current X coordinate</param>
        /// <param name="y">Current Y coordinate</param>
        /// <param name="z">Current Z coordinate</param>
        /// <param name="expire">Says that the item should be expirable</param>
        /// <param name="rn">A list of "reference names" that players will use to reference this item during gameplay</param>
        /// <param name="expirableItemList">The game's list of "expirable" items, passed by reference as objects will automatically remove themselves (expire) when their expiration timer interval has elapsed</param>
        public Item(string n, string d, double w, int x, int y, int z, bool expire, List<string> rn, ref List<Item> expirableItemList)
        {
            this.name = n;
            this.description = d;

            this.weight = w;

            this.x = x;
            this.y = y;
            this.z = z;

            this.expirable = expire;

            this.refNames = rn;

            this.spawned = true;

            this.expirationTimer = new System.Timers.Timer();
            this.expirationTimer.Interval = expireTime;
            this.expirationTimer.Elapsed += new ElapsedEventHandler(expirationTimer_Elapsed);

            this.expirationTimer.Enabled = true;
            this.expirationTimer.Start();

            this.expirableItemList = expirableItemList;
        }

        /// <summary>
        /// The Player class calls this method when an instance of an Item is picked up.
        /// This method sets the item's spawned status to false, and moves it to a temporary holding room that is inaccessible to players.
        /// If the item's "respawnable" attribute it set to true, this method will also enable and start a respawn counter so that the items can respawn when the interval elapses.
        /// If the item is not "respawnable" (aka, it's expirable), the item will be moved, but no respawn timer will start.  The item's "expiration" timer that was started in the expirable constructor will elapse in the 'hidden' room and the item will be removed from the expirable item list in the Elapsed Event handler
        /// </summary>
        /// <seealso cref="respawnTimer_Elapsed(object sender, ElapsedEventArgs e)"/>
        /// <seealso cref="expirationTimer_Elapsed(object sender, ElapsedEventArgs e)"/>
        public void PickedUp()
        {            
            this.spawned = false;

            this.x = -9999;
            this.y = -9999;
            this.z = -9999;

            //TODO: raise picked up event

            //if the item is spawnable we need to start the respawn counter
            //otherwise, if it is expirable, we'll do nothing other than move it to the temporary holding room where it will eventually expire and
            //references to it will be removed (hopefully)
            //this works because when a player picks up an expirable item, the really only pick up a "copY" of it
            //when it's dropped, a "copy" of it is dropped with a full expiration counter at initialization
            if (this.spawnable == true)
            {
                this.respawnTimer.Enabled = true;
                this.respawnTimer.Start();
            }
        }

        protected void Spawn()
        {
            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;
            
            this.spawned = true;

            //TODO: raise respawn event
        }

        protected void expirationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.expirationTimer.Stop();
            this.expirationTimer.Enabled = false;

            this.expirationTimer.Elapsed -= new ElapsedEventHandler(expirationTimer_Elapsed);

            Monitor.TryEnter(expirableItemListLock, 3000);

            try
            {
                this.expirableItemList.Remove(this);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
            finally
            {
                Monitor.Exit(expirableItemListLock);
            }
        }

        protected void respawnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.spawned == false)
            {
                this.Spawn();
                this.respawnTimer.Stop();
            }
        }

        //protected virtual void OnTimedEvent(object sender, ElapsedEventArgs e)
        //{
        //    if (this.spawnable)
        //    {
        //        if (this.spawned == false)
        //        {
        //            this.respawnCounter++;
        //            if ((this.respawnCounter * 100) >= this.spawntime)
        //            {
        //                this.Spawn();
        //            }
        //        }
        //    }
        //    else if (this.expirable)
        //    {
        //        if (!this.inInventory)
        //        {
        //            this.expireCounter++;
        //            if ((this.expireCounter * 100) >= expireTime)
        //            {
        //                this.worldTimer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);

        //                this.expirableItemList.Remove(this);
        //            }
        //        }
        //    }
        //}

        #region Conversion Methods

        public Dagger ToDagger()
        {
            //TODO: find a way to generate a compiler error here if this method is called on an incompatible type
            return new Dagger((Dagger)this);
        }

        public Light ToLight()
        {
            //TODO: find a way to generate a compiler error here if this method is called on an incompatible type
            return new Light((Light)this);
        }

        public Sword ToSword()
        {
            return new Sword((Sword)this);
        }

        public Axe ToAxe()
        {
            return new Axe((Axe)this);
        }

        public Headwear ToHeadwear()
        {
            return new Headwear((Headwear)this);
        }

        public Shirt ToShirt()
        {
            return new Shirt((Shirt)this);
        }

        public Gloves ToGloves()
        {
            return new Gloves((Gloves)this);
        }

        public Pants ToPants()
        {
            return new Pants((Pants)this);
        }

        public Boots ToBoots()
        {
            return new Boots((Boots)this);
        }        

        #endregion

        #region Attribute Accessors

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        public double Weight
        {
            get { return this.weight; }
            set { this.weight = value; }
        }

        public int X
        {
            get { return this.x; }
            set { this.x = value; }
        }

        public int Y
        {
            get { return this.y; }
            set { this.y = value; }
        }

        public int Z
        {
            get { return this.z; }
            set { this.z = value; }
        }

        public List<string> RefNames
        {
            get { return this.refNames; }
            set { this.refNames = value; }
        }

        public bool Spawnable
        {
            get { return this.spawnable; }
            set { this.spawnable = value; }
        }

        public bool Expirable
        {
            get { return this.expirable; }
            set { this.expirable = value; }
        }

        public int SpawnX
        {
            get { return this.spawnX; }
            set { this.spawnX = value; }
        }

        public int SpawnY
        {
            get { return this.spawnY; }
            set { this.spawnY = value; }
        }

        public int SpawnZ
        {
            get { return this.spawnZ; }
            set { this.spawnZ = value; }
        }

        public int SpawnTime
        {
            get { return this.spawntime; }
            set { this.spawntime = value; }
        }

        public bool InInventory
        {
            get { return this.inInventory; }
            set { this.inInventory = value; }
        }

        #endregion
    }
}
