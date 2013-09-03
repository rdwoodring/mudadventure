using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Items
{
    abstract class Apparel : Item
    {
        protected int armorValue;

        #region Constructors

        public Apparel() : base() { }

        public Apparel(Apparel apparel) : base(apparel) { }

        //for respawnable apparel
        public Apparel(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, List<string> rn, int armorVal)
            : base(n, d, w, spx, spy, spz, sptime, spawn, rn)
        {
            this.armorValue = armorVal;
        }

        //for expirable apparel
        public Apparel(string n, string d, double w, int x, int y, int z, bool expire, List<string> rn, ref List<Item> expirableItemList, int armorVal)
            : base(n, d, w, x, y, z, expire, rn, ref expirableItemList)
        {
            this.armorValue = armorVal;
        }

        #endregion

        #region Attribute Accessors

        public int ArmorValue
        {
            get { return this.armorValue; }
            set { this.armorValue = value; }
        }

        #endregion
    }
}
