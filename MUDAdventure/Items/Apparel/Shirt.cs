using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Items.Apparel
{
    class Shirt : Apparel
    {
        public Shirt() : base() { }

        public Shirt(Shirt shirt) : base(shirt) { }

        //for respawnable apparel
        public Shirt(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, int armorVal)
            : base(n, d, w, spx, spy, spz, sptime, spawn, rn, armorVal)
        {
        }

        //for expirable apparel
        public Shirt(string n, string d, double w, int x, int y, int z, bool expire, List<string> rn, ref List<Item> expirableItemList, int armorVal)
            : base(n, d, w, x, y, z, expire, rn, ref expirableItemList, armorVal)
        {
        }
    }
}
