using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Diagnostics;

namespace MUDAdventure.Items
{
    class Light : Item
    {
        public event EventHandler<LightExpiredEventArgs> LightExpired;

        private int totalFuel, currentFuel;
        private bool isLit;
        private Timer fuelTimer;

        public Light() : base() { }

        public Light(Light light) :base(light)
        {
            //this.name = light.name;
            //this.description = light.description;

            //this.weight = light.weight;

            //this.spawnX = light.spawnX;
            //this.spawnY = light.spawnY;
            //this.spawnZ = light.spawnZ;

            //this.spawntime = light.spawntime;
            //this.respawnCounter = light.respawnCounter;

            //this.expireCounter = 0;

            //this.spawnable = light.spawnable;
            //this.expirable = light.expirable;
            //this.spawned = light.spawned;

            //this.refNames = light.refNames;
            //this.expirableItemList = light.expirableItemList;

            this.totalFuel = light.totalFuel;
            this.currentFuel = light.currentFuel;

            this.isLit = false;
            //this.fuelTimer = new Timer();

            //this.fuelTimer.Elapsed += new ElapsedEventHandler(fuelTimer_Elapsed);
        }

        //for respawnable items
        public Light(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, int fuel) : base(n, d, w, spx, spy, spz, sptime, spawn, rn)
        {
            this.totalFuel = fuel;
            this.currentFuel = totalFuel;

            this.isLit = false;
        }

        //for expirable items
        public Light(string n, string d, double w, int x, int y, int z, bool expire, List<string> rn, ref List<Item> expirableItemList, int curfuel, int totfuel) : base(n, d, w, x, y, z, expire, rn, ref expirableItemList)
        {
            this.totalFuel = totfuel;
            this.currentFuel = curfuel;

            this.isLit = false;
        }

        public void Ignite()
        {
            this.isLit = true;

            this.fuelTimer = new Timer();

            this.fuelTimer.Elapsed += new ElapsedEventHandler(fuelTimer_Elapsed);            

            this.fuelTimer.Interval = this.currentFuel;
            this.fuelTimer.Enabled = true;
            this.fuelTimer.Start();
        }               

        protected virtual void OnLightExpired(LightExpiredEventArgs e)
        {
            EventHandler<LightExpiredEventArgs> handler = this.LightExpired;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        //womp womp, light's out of fuel
        protected void fuelTimer_Elapsed(object sender, EventArgs e)
        {
            this.isLit = false;
            //TODO: raise a "light expired event" so that this item is removed from inventory
            this.OnLightExpired(new LightExpiredEventArgs(this.name));
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

        public bool IsLit
        {
            get { return this.isLit; }
            set { this.isLit = value; }
        }

        #endregion
    }
}
