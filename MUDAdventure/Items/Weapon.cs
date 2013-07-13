using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace MUDAdventure
{
    abstract class Weapon : Item
    {
        protected int damage, speed;

        public Weapon() { }

        public Weapon(Weapon weapon)
        {
            this.name = weapon.name;
            this.description = weapon.description;

            this.weight = weapon.weight;

            this.spawnX = weapon.spawnX;
            this.spawnY = weapon.spawnY;
            this.spawnZ = weapon.spawnZ;

            this.spawntime = weapon.spawntime;
            this.respawnCounter = weapon.respawnCounter;

            this.expireCounter = 0;

            this.spawnable = weapon.spawnable;
            this.expirable = weapon.expirable;
            this.spawned = weapon.spawned;

            this.refNames = weapon.refNames;
            this.expirableItemList = weapon.expirableItemList;
            this.worldTimer = weapon.worldTimer;

            this.damage = weapon.damage;
            this.speed = weapon.speed;

            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        }

        public Weapon(System.Timers.Timer timer, string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, ref List<Item> expirableItemList, int dmg, int spd) : base(timer, n, d, w, spx, spy, spz, sptime, spawn, rn, ref expirableItemList)
        {

            this.damage = dmg;
            this.speed = spd;
        }        

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
    }
}
