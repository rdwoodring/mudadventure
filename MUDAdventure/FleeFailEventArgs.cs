using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class FleeFailEventArgs : EventArgs
    {
        private int x, y, z;
        private string name;

        public FleeFailEventArgs(int xloc, int yloc, int zloc, string pname)
        {
            this.x = xloc;
            this.y = yloc;
            this.z = zloc;


            this.name = pname;          
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
    }
}
