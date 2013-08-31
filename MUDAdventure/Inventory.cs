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
        private Light light;
        //private Apparel head, torso, pants, hands, feet, neck, rfinger, lfinger;
        //private Shield shield;
        private List<Item> generalInventory = new List<Item>();
        private double weight;
        private object inventoryLock = new object();

        public Inventory()
        {
            this.weight = 0;
        }

        //public void AddItem(Item item)
        //{
        //    Monitor.TryEnter(inventoryLock, 3000);
        //    try
        //    {
        //        this.generalInventory.Add(item);
        //        this.weight += item.Weight;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.Print(ex.Message.ToString());
        //        Debug.Print(ex.StackTrace.ToString());
        //    }
        //    finally
        //    {
        //        Monitor.Exit(inventoryLock);
        //    }
        //}

        public void AddItem(Dagger dagger)
        {
            Monitor.TryEnter(inventoryLock, 3000);
            try
            {
                this.generalInventory.Add(dagger);
                this.weight += dagger.Weight;
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

        public void AddItem(Light light)
        {
            Monitor.TryEnter(inventoryLock, 3000);
            try
            {
                this.generalInventory.Add(light);
                this.weight += light.Weight;
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
            int index = -1;

            Monitor.TryEnter(inventoryLock, 3000);
            try
            {
                if (this.generalInventory.Contains(item))
                {
                    index = this.generalInventory.IndexOf(item);
                }
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

            if (index >= 0)
            {
                this.RemoveItem(index);
            }
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

        public Light Light
        {
            get { return this.light; }
            set { this.light = value; }
        }

        #endregion

    }
}
