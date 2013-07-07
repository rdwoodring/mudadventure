using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class Dagger : Weapon
    {

        public Dagger() { }

        //for a dagger that will respawn at a specific location
        public Dagger(string n, string d, double w, int spx, int spy, int spz, int sptime, bool spawn, int dmg, int spd) : base(n, d, w, spx, spy, spz, sptime, true, dmg, spd)
        {
        }
    }
}
