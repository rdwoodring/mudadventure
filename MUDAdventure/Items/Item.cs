using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    //the base class for all spawnable in game items including containers, weapons, armor, etc.
    //set it to abstract, because we don't want any actualy instances of class Item, only it's descendents
    abstract class Item
    {
        protected const int expireTime = 60000;

        protected string name, description;
        protected double weight;
        protected int spawnX, spawnY, spawnZ, x, y, z, respawnTime, respawnCounter, expireCounter;
        protected bool spawnable, expirable, inInventory;

        public Item() { }

        public Item(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn)
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

            this.respawnTime = sptime;
            this.spawnable = spawn;

            this.expirable = false;
        }

        public Item(string n, string d, double w, int spx, int spy, int spz, bool exp)
        {
            this.name = n;
            this.description = d;

            this.weight = w;

            this.spawnX = spx;
            this.spawnY = spy;
            this.spawnZ = spz;

            this.expirable = exp;

            this.spawnable = false;
        }

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
    }
}
