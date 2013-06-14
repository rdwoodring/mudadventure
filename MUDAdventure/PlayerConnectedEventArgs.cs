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
            name = s;
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }
}
