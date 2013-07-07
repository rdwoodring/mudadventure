using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class SpawnedEventArgs : EventArgs
    {
        private string name;
        private int x, y, z;

        public SpawnedEventArgs(string spawnedname, int xloc, int yloc, int zloc)
        {
            this.name = spawnedname;

            this.x = xloc;
            this.y = yloc;
            this.z = zloc;
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
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
