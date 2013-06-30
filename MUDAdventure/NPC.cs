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
        public event EventHandler<PlayerMovedEventArgs> NPCMoved;
        public event EventHandler<FledEventArgs> NPCFled;

        private int spawnX, spawnY, spawnZ, x, y, z, spawntime, totalHitpoints, currentHitpoints, wimpy;
        private string name, description;
        private List<string> refNames;        
        private Object combatTarget;
        private System.Timers.Timer worldTimer;
        private ObservableCollection<Player> players;
        private Dictionary<string, Room> rooms;
        private Room currentRoom;
        private Random rand = new Random();

        private Object hpLock = new Object();

        /*************************************************/
        /*         FINITE STATE MACHINES                 */
        /*************************************************/
        private bool isDead, inCombat, isFleeing;

        public NPC(int spx, int spy, int spz, string n, string d, List<string> refnames, int sptime, int hp, int wimp, System.Timers.Timer timer, ObservableCollection<Player> playerList, Dictionary<string, Room> roomList)
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
                    }
                }
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (this.inCombat)
            {
                if (this.wimpy >= ((double)this.currentHitpoints / (double)this.totalHitpoints)*100)
                {
                    //fleeing is not guaranteed success.  there is a one in three chance that fleeing will even be successful
                    //on top of that, an exit is chosen randomly, so it is also possible that a non-viable exit will be chosen
                    //basically, it ain't good news

                    int flee = rand.Next(1, 3);
                    if (flee == 1) //the flee was successful
                    {
                        int direction = rand.Next(1, 4);
                        switch (direction)
                        {
                            case 1:
                                this.isFleeing = true;
                                this.Move("n");
                                break;
                            case 2:
                                this.isFleeing = true;
                                this.Move("e");
                                break;
                            case 3:
                                this.isFleeing = true;
                                this.Move("s");
                                break;
                            case 4:
                                this.isFleeing = true;
                                this.Move("w");
                                break;
                        }                        
                    }
                }
                else
                {
                    //TODO: call attack method depending upon speed.
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

        /********************************************************/
        /*              EVENT HANDLERS                          */
        /********************************************************/

        
    }
}
