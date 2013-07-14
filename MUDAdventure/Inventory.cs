using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace MUDAdventure
{
    class Inventory
    {
        private Weapon wielded;
        //private Apparel head, torso, pants, hands, feet, neck, rfinger, lfinger;
        //private Shield shield;
        private List<Item> generalInventory = new List<Item>();
        private double weight;
        private object inventoryLock = new object();

        public Inventory()
        {
            this.weight = 0;
        }

        public void AddItem(Item item)
        {
            Monitor.TryEnter(inventoryLock, 3000);
            try
            {
                this.generalInventory.Add(item);
                this.weight += item.Weight;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message.ToString());
                Debug.Print(ex.StackTrace.ToString());
            }
            finally
            {
                Monitor.Exit(inventoryLock);
            }
        }

        public void RemoveItem(Item item)
        {
        }

        public void RemoveItem(int index)
        {
            Monitor.TryEnter(inventoryLock, 3000);
            try
            {
                this.weight -= this.generalInventory[index].Weight;
                this.generalInventory.RemoveAt(index);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message.ToString());
                Debug.Print(ex.StackTrace.ToString());
            }
            finally
            {
                Monitor.Exit(inventoryLock);
            }
        }

        #region Attribute Accessors

        public List<Item> ListInventory()
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
