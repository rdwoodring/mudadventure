using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Diagnostics;

namespace MUDAdventure
{
    //the base class for all spawnable in game items including containers, weapons, armor, etc.
    //set it to abstract, because we don't want any actualy instances of class Item, only it's descendents
    abstract class Item
    {
        protected const int expireTime = 60000;

        protected string name, description;
        protected double weight;
        protected int spawnX, spawnY, spawnZ, x, y, z, spawntime, respawnCounter, expireCounter;
        protected bool spawnable, expirable, inInventory, spawned;
        protected List<string> refNames;
        protected List<Item> expirableItemList;
        protected System.Timers.Timer worldTimer;

        public Item() { }

        //for copying an item into inventory
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

            this.expireCounter = 0;

            this.spawnable = item.spawnable;
            this.expirable = item.expirable;
            this.spawned = item.spawned;

            this.refNames = item.refNames;
            this.expirableItemList = item.expirableItemList;
            
            this.worldTimer = item.worldTimer;
            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        }

        public Item(System.Timers.Timer timer, string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, ref List<Item> expirableItemList)
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

            this.expirable = false;

            this.refNames = rn;

            this.spawned = true;

            this.worldTimer = timer;
            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            this.respawnCounter = 0;

            this.expirableItemList = expirableItemList;
        }

        public void PickedUp()
        {            
            this.spawned = false;

            this.x = -9999;
            this.y = -9999;
            this.z = -9999;

            //TODO: raise picked up event
        }

        protected void Spawn()
        {
            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;

            this.respawnCounter = 0;
            this.spawned = true;

            //TODO: raise respawn event
        }

        protected void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (this.spawnable)
            {
                if (this.spawned == false)
                {
                    this.respawnCounter++;
                    Debug.Print(respawnCounter.ToString());
                    if ((this.respawnCounter * 100) >= this.spawntime)
                    {
                        this.Spawn();
                    }
                }
            }
            else if (this.expirable)
            {
                if (!this.inInventory)
                {
                    this.expireCounter++;
                    Debug.Print(expireCounter.ToString());
                    if ((this.expireCounter * 100) >= expireTime)
                    {
                        this.worldTimer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);

                        this.expirableItemList.Remove(this);
                    }
                }
            }
        }

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

        public int ExpireCounter
        {
            get { return this.expireCounter; }
            set { this.expireCounter = value; }
        }

        public System.Timers.Timer WorldTimer
        {
            get { return this.worldTimer; }
            set { this.worldTimer = value; }
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
