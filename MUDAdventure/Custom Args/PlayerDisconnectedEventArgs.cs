using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class PlayerDisconnectedEventArgs : EventArgs
    {
        private string name;

        public PlayerDisconnectedEventArgs(string s)
        {
            this.name = s;
        }

        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
    }
}
