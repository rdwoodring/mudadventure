using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure
{
    class ExperienceChart
    {
        private Dictionary<int, int> expChart;

        /// <summary>
        /// Default constructor
        /// </summary>       
        public ExperienceChart() 
        {
            expChart = new Dictionary<int, int>();
            for (int i = 2; i <= 100; i++)
            {
                expChart.Add(i, Convert.ToInt32(Math.Round(Math.Sqrt(Math.Pow(Convert.ToDouble(i)*5000000.0, 1.0+(Convert.ToDouble(i)/100))))));
            }
        }

        /// <summary>
        /// Constructor accepting a "seed value" that determines how much exp is needed to gain the next level.  Default is 5000000
        /// </summary>
        /// <param name="seedValue">A seed value used to determine how much exp is required to gain each level.</param>
        public ExperienceChart(int seedValue)
        {
            expChart = new Dictionary<int, int>();
            for (int i = 2; i <= 100; i++)
            {
                expChart.Add(i, Convert.ToInt32(Math.Round(Math.Sqrt(Math.Pow(Convert.ToDouble(i) * Convert.ToDouble(seedValue), 1.0 + (Convert.ToDouble(i) / 100))))));
            }
        }

        public int GetLevelExperience(int lev)
        {
            return (this.expChart[lev]);
        }

        #region Attribute Accessors

        public int Count
        {
            get { return this.expChart.Count; }
        }

        public Dictionary<int, int> ExpChart
        {
            get { return this.expChart; }
        }

        #endregion
    }
}
