using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace MUDAdventure
{
    abstract class Character
    {
        protected string name;
        protected int strength, agility, constitution, intelligence, learning;
        protected int x, y, z; //x, y, and z coordinates
        protected int level;
        protected int totalMoves, currentMoves, totalHitpoints, currentHitpoints; //current moves, total moves, current hp, total hp TODO: add MP to this
        protected Random rand = new Random(); //a random number... it gets used...
        protected Room currentRoom;
        protected Character combatTarget; //TODO: make this into a list to account for multiple attackers, i.e. a target queu

        protected Inventory inventory = new Inventory();

        public event EventHandler<AttackedAndHitEventArgs> AttackedAndHit;
        public event EventHandler<AttackedAndDodgeEventArgs> AttackedAndDodge;
        public event EventHandler<DiedEventArgs> Died;
        public event EventHandler<AttackEventArgs> Attack;
        //TODO: add an event for the inital attack so that the defender can start their own attack timer and respond in kind

        #region Finite State Machines

        protected bool isDead, inCombat, isFleeing;

        #endregion

        #region Timers

        protected System.Timers.Timer healthRegen;
        protected System.Timers.Timer attackTimer;

        #endregion

        #region Locks

        protected object hplock = new object();

        #endregion

        #region Constructors

        public Character() { }

        #endregion

        #region Event Handlers

        protected void healthRegen_Elapsed(object sender, ElapsedEventArgs e)
        {
            Monitor.TryEnter(this.hplock, 3000);
            try
            {
                if (this.currentHitpoints < this.totalHitpoints)
                {
                    if ((this.currentHitpoints + (Math.Ceiling((this.totalHitpoints * .01))) <= this.totalHitpoints))
                    {
                        this.currentHitpoints += Convert.ToInt32(Math.Ceiling((this.totalHitpoints * .01)));
                    }
                    else
                    {
                        this.currentHitpoints = this.totalHitpoints;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                Debug.Print(ex.StackTrace);
            }
            finally
            {
                Monitor.Exit(this.hplock);
            }
        }

        #endregion

        #region Attribute Accessors

        public Character CombatTarget
        {
            get { return this.combatTarget; }
            set { this.combatTarget = value; }
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

        public int Level
        {
            get { return this.level; }
        }

        #endregion

        public virtual void ReceiveAttack(Character sender, int potentialdamage, string attackerName)
        {
            bool success = true;            
            this.inCombat = true;

            this.combatTarget = sender;

            //dodging an attack
            if (this.rand.Next(1, 101) <= this.agility)
            {
                //raise attacked and dodged event
                this.OnAttackedAndDodge(new AttackedAndDodgeEventArgs(attackerName, this.name, this.x, this.y, this.z));
                success = false;
            }

            //TODO: implement parrying an attack

            if (success)
            {
                Monitor.TryEnter(this.hplock);
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
                    Monitor.Exit(this.hplock);
                }

                this.OnAttackedAndHit(new AttackedAndHitEventArgs(attackerName, this.name, this.x, this.y, this.z));

                if (this.currentHitpoints <= 0)
                {                    
                    this.Die();
                }
            }
        }

        protected virtual void Die() 
        {
            this.OnDied(new DiedEventArgs(this.name, this.combatTarget.Name, this.x, this.y, this.z));
        }

        protected void OnDied(DiedEventArgs e)
        {
            EventHandler<DiedEventArgs> handler = this.Died;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnAttackedAndHit(AttackedAndHitEventArgs e)
        {
            EventHandler<AttackedAndHitEventArgs> handler = this.AttackedAndHit;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnAttackedAndDodge(AttackedAndDodgeEventArgs e)
        {
            EventHandler<AttackedAndDodgeEventArgs> handler = this.AttackedAndDodge;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected void OnAttack(AttackEventArgs e)
        {
            EventHandler<AttackEventArgs> handler = this.Attack;

            if (handler != null)
            {
                handler(this, e);
            }
        }

    }
}
