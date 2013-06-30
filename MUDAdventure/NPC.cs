using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Collections.ObjectModel;

namespace MUDAdventure
{
    class NPC
    {
        private int spawnX, spawnY, spawnZ, x, y, z, spawntime, totalHitpoints, currentHitpoints, wimpy;
        private string name, description;
        private List<string> refNames;
        private bool isDead, inCombat;
        private Object combatTarget;
        private System.Timers.Timer worldTimer;
        private ObservableCollection<Player> players;

        private Object hpLock = new Object();

        public NPC(int spx, int spy, int spz, string n, string d, List<string> refnames, int sptime, int hp, int wimp, System.Timers.Timer timer, ObservableCollection<Player> playerList)
        {
            this.spawnX = spx;
            this.spawnY = spy;
            this.spawnZ = spz;

            this.spawntime = sptime;

            this.totalHitpoints = hp;
            this.currentHitpoints = this.totalHitpoints;
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

            this.worldTimer = timer;

            this.worldTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            this.players = playerList;
        }

        public string ReceiveAttack(int potentialdamage)
        {
            //TODO: implement dodge, parry, armor damage reduction or prevention
            this.inCombat = true;

            Monitor.TryEnter(this.hpLock);
            try
            {
                this.currentHitpoints -= potentialdamage;
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

            if (this.currentHitpoints <= 0)
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
            this.combatTarget = null;
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

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (this.inCombat)
            {
                if (this.wimpy >= ((double)this.currentHitpoints / (double)this.totalHitpoints)*100)
                {
                    //TODO: implement flee logic
                }
                else
                {
                    //call attack method depending upon speed.
                    if (!this.players[players.IndexOf((Player)combatTarget)].IsDead)
                    {
                        this.players[players.IndexOf((Player)combatTarget)].ReceiveAttack(2);
                    }
                    else if (this.players[players.IndexOf((Player)combatTarget)].IsDead)
                    {
                        this.inCombat = false;
                        this.combatTarget = null;
                    }
                }
            }
            else
            {
                //TODO: implement random movement logic
            }
        }
    }
}
