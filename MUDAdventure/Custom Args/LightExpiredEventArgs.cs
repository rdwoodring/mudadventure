using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class LightExpiredEventArgs : EventArgs
    {
        private string name;

        public LightExpiredEventArgs(string n)
        {
            this.name = n;
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
    }
}
