using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Text;

namespace MUDAdventure
{
    class Dagger : Weapon
    {

        public Dagger() { }

        public Dagger(Dagger dagger)
        {
            this.name = dagger.name;
            this.description = dagger.description;

            this.weight = dagger.weight;

            this.spawnX = dagger.spawnX;
            this.spawnY = dagger.spawnY;
            this.spawnZ = dagger.spawnZ;

            this.spawntime = dagger.spawntime;
            this.respawnCounter = dagger.respawnCounter;

            this.expireCounter = 0;

            this.spawnable = dagger.spawnable;
            this.expirable = dagger.expirable;
            this.spawned = dagger.spawned;

            this.refNames = dagger.refNames;
            this.expirableItemList = dagger.expirableItemList;
            this.worldTimer = dagger.worldTimer;

            this.damage = dagger.damage;
            this.speed = dagger.speed;

            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        }

        //for a dagger that will respawn at a specific location
        public Dagger(System.Timers.Timer timer, string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, ref List<Item> expirableItemList, int dmg, int spd) : base(timer, n, d, w, spx, spy, spz, sptime, true, rn, ref expirableItemList, dmg, spd)
        {
        }
    }
}
