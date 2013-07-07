using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class PlayerConnectedEventArgs : EventArgs
    {
        private string name;

        public PlayerConnectedEventArgs(string s)
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
