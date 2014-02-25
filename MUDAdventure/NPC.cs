using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MUDAdventure
{
    //TODO: set this class to abstract.  we don't want any actualy instances of class NPC, only its descendents
    class NPC : Character
    {
        public event EventHandler<PlayerMovedEventArgs> NPCMoved;
        public event EventHandler<FledEventArgs> NPCFled;
        public event EventHandler<FleeFailEventArgs> NPCFleeFail;
        //public event EventHandler<AttackedAndHitEventArgs> NPCAttackedAndHit;
        //public event EventHandler<DiedEventArgs> NPCDied;
        public event EventHandler<SpawnedEventArgs> NPCSpawned;

        private int spawnX, spawnY, spawnZ, spawntime, wimpy;
        private string description;
        private List<string> refNames;        
        private ObservableCollection<Player> players;
        private Dictionary<string, Room> rooms;

        protected System.Timers.Timer respawnTimer;

        public NPC(int spx, int spy, int spz, string n, string d, List<string> refnames, int sptime, int hp, int wimp, ObservableCollection<Player> playerList, Dictionary<string, Room> roomList, int lev, int str, int agi, int con, int intel, int lea)
        {
            this.spawnX = spx;
            this.spawnY = spy;
            this.spawnZ = spz;

            this.rooms = roomList;

            this.spawntime = sptime;

            this.totalHitpoints = hp;
            this.currentHitpoints = this.totalHitpoints;
            this.wimpy = wimp;

            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;
            this.rooms.TryGetValue(this.x + "," + this.y + "," + this.z, out this.currentRoom);

            this.name = n;
            this.description = d;

            this.refNames = new List<string>();            
            foreach (string refname in refnames)
            {
                this.refNames.Add(refname.ToLower());
            }

            this.isDead = false;
            this.inCombat = false;
            this.isFleeing = false;

            this.players = playerList;

            this.strength = str;
            this.agility = agi;
            this.constitution = con;
            this.intelligence = intel;
            this.learning = lea;

            this.level = lev;
        }
        


        protected override void Die()
        {            
            //this.OnNPCDied(new DiedEventArgs(this.name, this.combatTarget.Name, this.x, this.y, this.z));

            base.Die();

            this.attackTimer.Enabled = false;
            this.attackTimer.Stop();
            this.attackTimer.Elapsed -= this.attackTimer_Elapsed;

            //this.attackTimer = null;

            //TODO: implement die logic
            this.isDead = true;
            this.inCombat = false;
            this.combatTarget = null;

            //TODO: figure out a better way to do this...
            this.x = -9999;
            this.y = -9999;
            this.z = -9999;            
            

            this.respawnTimer = new System.Timers.Timer();
            this.respawnTimer.Elapsed += new ElapsedEventHandler(respawnTimer_Elapsed);    //TODO: implement handler & add logic to stop the timer        
            this.respawnTimer.Interval = this.spawntime;
            this.respawnTimer.Enabled = true;
            this.respawnTimer.Start();
        }

        public override void ReceiveAttack(Character sender, int potentialdamage, string attackerName)
        {
            base.ReceiveAttack(sender, potentialdamage, attackerName);

            this.attackTimer = new System.Timers.Timer();
            this.attackTimer.Elapsed += new ElapsedEventHandler(attackTimer_Elapsed);
            this.attackTimer.Interval = Math.Ceiling((double)(60000 / this.agility));
            this.attackTimer.Start();
        }

        #region Attribute Accessors

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

        #endregion

        private void Move(string dir)
        {
            if (!this.inCombat || this.isFleeing)
            {
                int oldx, oldy, oldz;
                oldx = this.x;
                oldy = this.y;
                oldz = this.z;

                //getting current room.
                this.rooms.TryGetValue(this.x + "," + this.y + "," + this.z, out currentRoom);

                //make sure the room we are in exists and is not null
                if (currentRoom != null)
                {                   
                    //check the movement direction, and see if an exit is available that way.
                    switch (dir)
                    {
                        case "n":
                            if (currentRoom.NorthExit)
                            {
                                this.y++;
                            }
                            break;
                        case "s":
                            if (currentRoom.SouthExit)
                            {
                                this.y--;
                            }
                            break;
                        case "e":
                            if (currentRoom.EastExit)
                            {
                                this.x++;
                            }
                            break;
                        case "w":
                            if (currentRoom.WestExit)
                            {
                                this.x--;
                            }
                            break;
                    }
                }

                //if we have moved, let's raise some events then
                if (this.x != oldx || this.y != oldy || this.z != oldz)
                {
                    if (!this.isFleeing) //just a regular move, no flee here
                    {
                        this.OnNPCMoved(new PlayerMovedEventArgs(this.x, this.y, this.z, oldx, oldy, oldz, this.name, dir));
                        rooms.TryGetValue(this.x.ToString() + "," + this.y.ToString() + "," + this.z.ToString(), out this.currentRoom);
                    }
                    else if (this.isFleeing) //yup, someone's running away
                    {
                        this.OnNPCFled(new FledEventArgs(this.x, this.y, this.z, oldx, oldy, oldz, this.name, dir));
                        this.combatTarget = null;
                        this.inCombat = false;
                    }
                }
            }
        }

        //private void OnTimedEvent(object sender, ElapsedEventArgs e)
        //{
        //    if (this.inCombat)
        //    {
        //        Debug.Print((((double)this.currentHitpoints / (double)this.totalHitpoints)*100).ToString());

        //        if (this.wimpy >= ((double)this.currentHitpoints / (double)this.totalHitpoints)*100)
        //        {
        //            //fleeing is not guaranteed success.  there is a one in two chance that fleeing will even be successful
        //            //on top of that, an exit is chosen randomly, so it is also possible that a non-viable exit will be chosen
        //            //basically, it ain't good news

        //            int flee = rand.Next(1, 2);
        //            if (flee == 1) //the flee was successful
        //            {
        //                int direction = rand.Next(1, 4);
        //                switch (direction)
        //                {
        //                    case 1:
        //                        this.isFleeing = true;
        //                        this.Move("n");
        //                        break;
        //                    case 2:
        //                        this.isFleeing = true;
        //                        this.Move("e");
        //                        break;
        //                    case 3:
        //                        this.isFleeing = true;
        //                        this.Move("s");
        //                        break;
        //                    case 4:
        //                        this.isFleeing = true;
        //                        this.Move("w");
        //                        break;
        //                }
        //            }
        //            else
        //            {
        //                this.OnNPCFleeFail(new FleeFailEventArgs(this.x, this.y, this.z, this.name));
        //            }
        //        }
        //        else
        //        {
        //            if (this.combatTarget != null)
        //            {
        //                if (!this.players[players.IndexOf((Player)combatTarget)].IsDead)
        //                {
        //                    this.players[players.IndexOf((Player)combatTarget)].ReceiveAttack(2, this.name);
        //                }
        //                else if (this.players[players.IndexOf((Player)combatTarget)].IsDead)
        //                {
        //                    this.inCombat = false;
        //                    this.combatTarget = null;
        //                }
        //            }
        //        }
        //    }
        //    else if (this.isDead)
        //    {
        //        this.respawnCounter++;

        //        if ((this.respawnCounter * 100) >= this.spawntime)
        //        {
        //            this.Spawn();
        //        }
        //    }
        //    else
        //    {
        //        //TODO: implement random movement logic
        //        //TODO: implement zones for yelling and to restrict npc movement
        //    }
        //}

        private void Spawn()
        {
            this.isDead = false;
            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;

            this.currentHitpoints = this.totalHitpoints;
            
            this.OnNPCSpawned(new SpawnedEventArgs(this.name, this.x, this.y, this.z));
        }

        protected virtual void OnNPCMoved(PlayerMovedEventArgs e)
        {
            EventHandler<PlayerMovedEventArgs> handler = this.NPCMoved;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnNPCFled(FledEventArgs e)
        {
            EventHandler<FledEventArgs> handler = this.NPCFled;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnNPCFleeFail(FleeFailEventArgs e)
        {
            EventHandler<FleeFailEventArgs> handler = this.NPCFleeFail;

            if (handler != null)
            {
                handler(this, e);
            }
        }             

        protected virtual void OnNPCSpawned(SpawnedEventArgs e)
        {
            EventHandler<SpawnedEventArgs> handler = this.NPCSpawned;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        /********************************************************/
        /*              EVENT HANDLERS                          */
        /********************************************************/

        protected virtual void respawnTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.respawnTimer.Stop();

            this.Spawn();
        }

        protected virtual void attackTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (this.inCombat)
            {
                if (this.combatTarget != null)
                {
                    if (!this.combatTarget.IsDead)
                    {
                        //TODO: sub in actual damage calculation
                        this.combatTarget.ReceiveAttack(this, 5, this.name);
                    }
                    else
                    {
                        this.combatTarget = null;
                        this.attackTimer.Enabled = false;
                    }
                }
            }
            else
            {
                this.attackTimer.Enabled = false;
            }
        }
    }
}
