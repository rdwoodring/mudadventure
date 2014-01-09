using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills
{
    class SkillTree
    {
        private List<Skill> skills;

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public SkillTree() { }

        /// <summary>
        /// Constructor with arguments
        /// </summary>
        /// <param name="_skills">A list of all skills</param>
        public SkillTree(List<Skill> _skills)
        {
            this.skills = _skills;
        }

        #endregion

        public void InitializeSkillTree()
        {
            if (this.skills != null)
            {
                foreach(Skill s in this.skills)
                {
                    try
                    {
                        Console.Write("Attempting to initialize skill {0}...", s.SkillName);
                        s.InitializeSkill(this);
                        Console.WriteLine(" Success.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error. Encountered an exception");
                    }                    
                }
            }
            else
            {
                //TODO: throw an exception or alert the user, then abort the program.  We can't initialize an empty skill tree.
            }
        }

        #region Attribute Accessors

        public List<Skill> Skills
        {
            get { return this.skills; }
            set { this.skills = value; }
        }

        #endregion
    }
}
