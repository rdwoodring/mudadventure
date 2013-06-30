using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class FledEventArgs : EventArgs
    {
        private int x, y, z, oldx, oldy, oldz;
        private string name, direction;

        public FledEventArgs(int xloc, int yloc, int zloc, int oldxloc, int oldyloc, int oldzloc, string pname, string dir)
        {
            this.x = xloc;
            this.y = yloc;
            this.z = zloc;

            this.oldx = oldxloc;
            this.oldy = oldyloc;
            this.oldz = oldzloc;

            this.name = pname;

            switch (dir)
            {
                case "n":
                    this.direction = "north";
                    break;
                case "s":
                    this.direction = "south";
                    break;
                case "e":
                    this.direction = "east";
                    break;
                case "w":
                    this.direction = "west";
                    break;
            }
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

        public int OldX
        {
            get { return this.oldx; }
            set { this.oldx = value; }
        }

        public int OldY
        {
            get { return this.oldy; }
            set { this.oldy = value; }
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }

        public string Direction
        {
            get { return this.direction; }
            set { this.direction = value; }
        }
    }
}
