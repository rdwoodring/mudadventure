using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Diagnostics;

namespace MUDAdventure
{
    class Light : Item
    {
        public event EventHandler<LightExpiredEventArgs> LightExpired;

        private int totalFuel, currentFuel;
        private bool isLit;

        public Light() { }

        public Light(Light light)
        {
            this.name = light.name;
            this.description = light.description;

            this.weight = light.weight;

            this.spawnX = light.spawnX;
            this.spawnY = light.spawnY;
            this.spawnZ = light.spawnZ;

            this.spawntime = light.spawntime;
            this.respawnCounter = light.respawnCounter;

            this.expireCounter = 0;

            this.spawnable = light.spawnable;
            this.expirable = light.expirable;
            this.spawned = light.spawned;

            this.refNames = light.refNames;
            this.expirableItemList = light.expirableItemList;

            this.worldTimer = light.worldTimer;
            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            this.totalFuel = light.totalFuel;
            this.currentFuel = light.currentFuel;

            this.isLit = false;
        }

        public Light(System.Timers.Timer timer, string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, ref List<Item> expirableItemList, int fuel) : base(timer, n, d, w, spx, spy, spz, sptime, spawn, rn, ref expirableItemList)
        {
            this.totalFuel = fuel;
            this.currentFuel = totalFuel;

            this.isLit = false;
        }

        protected override void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (this.spawnable)
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
            else if (this.expirable)
            {
                if (!this.inInventory)
                {
                    this.expireCounter++;
                    if ((this.expireCounter * 100) >= expireTime)
                    {
                        this.worldTimer.Elapsed -= new ElapsedEventHandler(OnTimedEvent);

                        this.expirableItemList.Remove(this);
                    }
                }
            }

            if (this.isLit)
            {                
                this.currentFuel-= 100;
                Debug.Print(this.currentFuel.ToString() + "/" + this.totalFuel.ToString());

                if (this.currentFuel <= 0)
                {
                    this.isLit = false;
                    //TODO: raise a "light expired event" so that this item is removed from inventory
                    this.OnLightExpired(new LightExpiredEventArgs(this.name));
                }
            }
        }

        public bool IsLit
        {
            get { return this.isLit; }
            set { this.isLit = value; }
        }

        protected virtual void OnLightExpired(LightExpiredEventArgs e)
        {
            EventHandler<LightExpiredEventArgs> handler = this.LightExpired;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        #region Attribute Accessors

        public int CurrentFuel
        {
            get { return this.currentFuel; }
            set { this.currentFuel = value; }
        }

        public int TotalFuel
        {
            get { return this.totalFuel; }
            set { this.totalFuel = value; }
        }

        #endregion
    }
}
