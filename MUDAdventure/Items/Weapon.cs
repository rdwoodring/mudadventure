using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace MUDAdventure.Items
{
    abstract class Weapon : Item
    {
        protected int damage, speed;

        public Weapon() : base() { }

        public Weapon(Weapon weapon) : base(weapon)
        {
            //this.name = weapon.name;
            //this.description = weapon.description;

            //this.weight = weapon.weight;

            //this.spawnX = weapon.spawnX;
            //this.spawnY = weapon.spawnY;
            //this.spawnZ = weapon.spawnZ;

            //this.spawntime = weapon.spawntime;
            //this.respawnCounter = weapon.respawnCounter;

            //this.expireCounter = 0;

            //this.spawnable = weapon.spawnable;
            //this.expirable = weapon.expirable;
            //this.spawned = weapon.spawned;

            //this.refNames = weapon.refNames;
            //this.expirableItemList = weapon.expirableItemList;

            this.damage = weapon.damage;
            this.speed = weapon.speed;
        }

        //for respawnable items
        public Weapon(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, int dmg, int spd) : base(n, d, w, spx, spy, spz, sptime, spawn, rn)
        {
            this.damage = dmg;
            this.speed = spd;
        }

        //for expirable items
        public Weapon(string n, string d, double w, int x, int y, int z, bool expire, List<string> rn, ref List<Item> expirableItemList, int dmg, int spd) : base(n, d, w, x, y, z, expire, rn, ref expirableItemList)
        {
            this.damage = dmg;
            this.speed = spd;
        }

        #region Attribute Accessors

        public int Damage
        {
            get { return this.damage; }
            set { this.damage = value; }
        }

        public int Speed
        {
            get { return this.speed; }
            set { this.speed = value; }
        }

        #endregion
    }
}
