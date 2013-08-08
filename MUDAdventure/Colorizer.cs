using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MUDAdventure
{
    class Colorizer
    {
        private static Dictionary<string, string> colors = new Dictionary<string, string>();

        static Colorizer()
        {
            colors.Add("reset", "\x1B[0m");
            colors.Add("black", "\x1B[30m");
            colors.Add("red", "\x1B[31m");
            colors.Add("green", "\x1B[32m");
            colors.Add("yellow", "\x1B[33m");
            colors.Add("blue", "\x1B[34m");
            colors.Add("magenta", "\x1B[35m");
            colors.Add("cyan", "\x1B[36m");
            colors.Add("white", "\x1B[37m");
        }

        public string Colorize(string input, string color)
        {
            if (colors.ContainsKey(color))
            {
                input = colors[color].ToString() + input + colors["reset"].ToString();
            }
            else
            {
                Debug.Write("Color " + color + " found in colorizer.  Text returned unchanged.");
            }

            return input;
        }
    }
}
