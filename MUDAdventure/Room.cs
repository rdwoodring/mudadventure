using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class Room
    {
        private string roomName, roomDescription;
        private bool northExit, southExit, eastExit, westExit;
        int x, y, z;

        public Room(string name, string desc, bool nexit, bool sexit, bool eexit, bool wexit, int xloc, int yloc, int zloc)
        {
            this.roomName = name;
            this.roomDescription = desc;

            this.northExit = nexit;
            this.southExit = sexit;
            this.eastExit = eexit;
            this.westExit = wexit;

            this.x = xloc;
            this.y = yloc;
            this.z = zloc;
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

        public string RoomName
        {
            get { return this.roomName; }
            set { this.roomName = value; }
        }

        public string RoomDescription
        {
            get { return this.roomDescription; }
            set { this.roomDescription = value; }
        }
    }
}
