using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Items.Weapons
{
    class Sword : Weapon
    {

        public Sword() : base() { }

        public Sword(Sword sword)
            : base(sword)
        {
            //nothing to put here
        }

        //for respawnable swords
        public Sword(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, int dmg, int spd)
            : base(n, d, w, spx, spy, spz, sptime, spawn, rn, dmg, spd)
        {
        }

        //for expirable swords
        public Sword(string n, string d, double w, int x, int y, int z, bool expire, List<string> rn, ref List<Item> expirableItemList, int dmg, int spd)
            : base(n, d, w, x, y, z, expire, rn, ref expirableItemList, dmg, spd)
        {
        }
    }
}