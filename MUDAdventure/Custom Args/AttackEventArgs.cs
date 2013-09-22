using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class AttackEventArgs: EventArgs
    {
        private Character combatTarget;
        private int x, y, z;

        public AttackEventArgs(Character target, int xloc, int yloc, int zloc)
        {
            this.combatTarget = target;

            this.x = xloc;
            this.y = yloc;
            this.z = zloc;
        }

        public Character CombatTarget
        {
            get { return this.combatTarget; }
            set { this.combatTarget = value; }
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
    }
}
