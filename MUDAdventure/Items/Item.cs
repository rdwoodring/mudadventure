using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

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
        private System.Timers.Timer worldTimer;

        public Item() { }

        public Item(System.Timers.Timer timer, string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn)
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
        }

        public void PickedUp()
        {            
            this.spawned = false;

            this.x = -9999;
            this.y = -9999;
            this.z = -9999;

            //TODO: raise picked up event
        }

        private void Spawn()
        {
            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;

            this.respawnCounter = 0;
            this.spawned = true;

            //TODO: raise respawn event
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (this.spawned == false)
            {
                this.respawnCounter++;

                if ((this.respawnCounter * 100) >= this.spawntime)
                {
                    this.Spawn();
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
        #endregion
    }
}
