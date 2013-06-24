using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class NPC
    {
        private int spawnX, spawnY, spawnZ, x, y, z;
        private string name, description;
        private List<string> refNames;

        public NPC(int spx, int spy, int spz, string n, string d, List<string> refnames)
        {
            this.spawnX = spx;
            this.spawnY = spy;
            this.spawnZ = spz;

            this.x = this.spawnX;
            this.y = this.spawnY;
            this.z = this.spawnZ;

            this.name = n;
            this.description = d;

            this.refNames = refnames;
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
    }
}
