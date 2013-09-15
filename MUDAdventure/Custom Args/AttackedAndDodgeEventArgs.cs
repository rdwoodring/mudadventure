using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class AttackedAndDodgeEventArgs : EventArgs
    {
        private string attackerName, defenderName;
        private int x, y, z;

        public AttackedAndDodgeEventArgs(string attackername, string defendername, int xloc, int yloc, int zloc)
        {
            this.attackerName = attackername;
            this.defenderName = defendername;

            this.x = xloc;
            this.y = yloc;
            this.z = zloc;
        }

        public string AttackerName
        {
            get { return this.attackerName; }
            set { this.attackerName = value; }
        }

        public string DefenderName
        {
            get { return this.defenderName; }
            set { this.defenderName = value; }
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
