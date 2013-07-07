using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    abstract class Weapon : Item
    {
        protected int damage, speed;

        public Weapon() { }

        public Weapon(System.Timers.Timer timer, string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, int dmg,int spd) :base (timer, n, d, w, spx, spy, spz, sptime, spawn, rn)
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
