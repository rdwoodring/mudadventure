using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class PlayerMovedEventArgs : EventArgs
    {
        private int x, y, oldx, oldy;
        private string name, direction;

        public PlayerMovedEventArgs(int xloc, int yloc, int oldxloc, int oldyloc, string pname, string dir)
        {
            this.x=xloc;
            this.y=yloc;

            this.oldx = oldxloc;
            this.oldy = oldyloc;

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
