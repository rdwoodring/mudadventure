﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class DiedEventArgs : EventArgs
    {
        private string defenderName, attackerName;
        private int x, y, z;

        public DiedEventArgs(string defendername, string attackername, int xloc, int yloc, int zloc)
        {
            this.defenderName = defendername;
            this.attackerName = attackername;

            this.x = xloc;
            this.y = yloc;
            this.z = zloc;
        }

        public string DefenderName
        {
            get { return this.defenderName; }
            set { this.defenderName = value; }
        }

        public string AttackerName
        {
            get { return this.attackerName; }
            set { this.attackerName = value; }
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

