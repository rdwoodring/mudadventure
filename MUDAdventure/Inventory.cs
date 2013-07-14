using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class Inventory
    {
        private Weapon wielded;
        //private Apparel head, torso, pants, hands, feet, neck, rfinger, lfinger;
        //private Shield shield;
        private Dictionary<int, Item> generalInventory = new Dictionary<int, Item>();
        private double weight;

        public Inventory()
        {
            this.weight = 0;
        }

        public void AddItem(Item item)
        {
            this.generalInventory.Add(this.generalInventory.Count, item);
            this.weight += item.Weight;
        }

        public void RemoveItem(Item item)
        {
        }

        public void RemoveItem(int index)
        {
            this.weight -= this.generalInventory[index].Weight;
            this.generalInventory.Remove(index);            
        }

        #region Attribute Accessors

        public Dictionary<int, Item> ListInventory()
        {
            return this.generalInventory;
        }

        public double Weight
        {
            get { return this.weight; }
            set { this.weight = value; }
        }

        public Weapon Wielded
        {
            get { return this.wielded; }
            set { this.wielded = value; }
        }

        #endregion

    }
}
