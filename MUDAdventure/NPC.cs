using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MUDAdventure
{
    class NPC
    {
        private int spawnX, spawnY, spawnZ, x, y, z, spawntime, hitpoints, wimpy;
        private string name, description;
        private List<string> refNames;
        private bool isDead, inCombat;
        private Object combatTarget;

        private Object hpLock = new Object();

        public NPC(int spx, int spy, int spz, string n, string d, List<string> refnames, int sptime, int hp, int wimp)
        {
            this.spawnX = spx;
            this.spawnY = spy;
            this.spawnZ = spz;

            this.spawntime = sptime;

            this.hitpoints = hp;
            this.wimpy = wimp;

            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;

            this.name = n;
            this.description = d;

            this.refNames = new List<string>();            
            foreach (string refname in refnames)
            {
                this.refNames.Add(refname.ToLower());
            }

            this.isDead = false;
            this.inCombat = false;
        }

        public string ReceiveAttack(int potentialdamage)
        {
            //TODO: implement dodge, parry, armor damage reduction or prevention
            this.inCombat = true;

            Monitor.TryEnter(this.hpLock);
            try
            {
                this.hitpoints -= potentialdamage;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
                Console.WriteLine("Trace: {0}", ex.StackTrace);
            }
            finally
            {
                Monitor.Exit(this.hpLock);
            }

            if (this.hitpoints <= 0)
            {
                this.Die();
                return "You hit an NPC, doing some damage.\r\nAn NPC falls over, dead.";
            }
            else
            {
                return "You hit an NPC, doing some damage.";
            }
        }

        private void Die()
        {
            //TODO: implement die logic
            this.isDead = true;
            this.inCombat = false;
        }

        public bool IsDead
        {
            get { return this.isDead; }
            set { this.isDead = value; }
        }

        public int X
        {
            get { return this.x; }
            set { this.x = value; }
        }

        public int Y
        {
            get { return this.y; }
            set { this.y = value; }
        }

        public int Z
        {
            get { return this.z; }
            set { this.z = value; }
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        public List<string> RefNames
        {
            get { return this.refNames; }
            set { this.refNames = value; }
        }

        public Object CombatTarget
        {
            get { return this.combatTarget; }
            set { this.combatTarget = value; }
        }
    }
}
