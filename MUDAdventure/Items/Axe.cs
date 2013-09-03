using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Items
{
    class Axe : Weapon
    {
        public Axe() : base() { }

        public Axe(Axe axe)
            : base(axe)
        {
            //nothing to put here
        }

        //for respawnable axes
        public Axe(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, int dmg, int spd)
            : base(n, d, w, spx, spy, spz, sptime, spawn, rn, dmg, spd)
        {
        }

        //for expirable axes
        public Axe(string n, string d, double w, int x, int y, int z, bool expire, List<string> rn, ref List<Item> expirableItemList, int dmg, int spd)
            : base(n, d, w, x, y, z, expire, rn, ref expirableItemList, dmg, spd)
        {
        }
    }
}