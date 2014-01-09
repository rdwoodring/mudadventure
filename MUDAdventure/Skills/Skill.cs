using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills
{
    class Skill
    {
        protected string skillName;
        protected int difficulty;
        protected int? proficiency;
        protected int effectOnHP, effectOnMP, effectOnMoves, effectOnCarryWeight; //negative number for a "cost," positive number for a "bonus"
        //protected int daggerProfRequired, swordProfRequired, axeProfRequired, parryProfRequired, dodgeProfRequired, roamerProfRequired, travellerProfRequired, packmuleProfRequired, oxenProfRequired, fieldmedicineProfRequired, sneakProfRequired, hideProfRequired, ambushProfRequired, pickpocketProfRequired, picklockProfRequired;
        protected Dictionary<string, int> preliminaryPrerequisites;
        protected Dictionary<Skill, int> finalPrerequisites;
        protected int levelRequired;

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public Skill() { }

        /// <summary>
        /// Constructor with arguments that specify the skill's effects.  This constructor does not require a list of preliminary prerequisite skills (i.e. skills that must be learned to a certain percentage before this skill can be learned), so this constructor is useful for instantiating 'base' skills that are available to low level characters.
        /// </summary>
        /// <param name="name">The skill's name, i.e. parry, dodge, etc.</param>
        /// <param name="diff">How difficult the skill is to learn.  1 being easiest, 10 being hardest.</param>
        /// <param name="prof">How proficient a player is at the skill, as a percentage. 0 being worst, 100 being completely mastered.</param>
        /// <param name="hpeffect">The effect that using this skill has on HP.  A positive number is an HP bonus, a negative number is an HP cost.</param>
        /// <param name="moveseffect">The effect that using this skill has on MP.  A positive number is an HP bonus, a negative number is an MP cost.</param>
        /// <param name="mpeffect">The effect that using this skill has on Moves.  A positive number is an HP bonus, a negative number is an Moves cost.</param>
        public Skill(string name, int diff, int? prof, int hpeffect, int mpeffect, int moveseffect, int carryeffect)
        {
            this.skillName = name;
            this.difficulty = diff;
            this.proficiency = prof;

            this.effectOnHP = hpeffect;
            this.effectOnMP = mpeffect;
            this.effectOnMoves = moveseffect;
            this.effectOnCarryWeight = carryeffect;

            this.levelRequired = 0;
            this.preliminaryPrerequisites = new Dictionary<string, int>();

            this.finalPrerequisites = new Dictionary<Skill, int>();
        }

        /// <summary>
        /// Constructor with arguments specify the skill's effects.  This constructor accepts a dictionary of prerequisite skills in a key/pair format.  The key being the skill's name and the value being the required proficiency in that skill in order to learn this skill.
        /// </summary>
        /// <param name="name">The skill's name, i.e. parry, dodge, etc.</param>
        /// <param name="diff">How difficult the skill is to learn.  1 being easiest, 10 being hardest.</param>
        /// <param name="prof">How proficient a player is at the skill, as a percentage. 0 being worst, 100 being completely mastered.</param>
        /// <param name="hpeffect">The effect that using this skill has on HP.  A positive number is an HP bonus, a negative number is an HP cost.</param>
        /// <param name="moveseffect">The effect that using this skill has on MP.  A positive number is an MP bonus, a negative number is an MP cost.</param>
        /// <param name="mpeffect">The effect that using this skill has on Moves.  A positive number is an Moves bonus, a negative number is an Moves cost.</param>
        /// <param name="carryeffect">The effect that using this skill has on Carrying capacity. A positive number is a Carrying capacity bonus, a negative number is a Carrying capacity cost</param>
        /// <param name="levelreq">The required minimum level needed to begin practicing this skill. 0-100</param>
        /// <param name="_preliminaryPrerequisites">A dictionary of "prerequisite" skills that must be learned before learning this skill.  Passed as key/pair format where the key is the skill's name as a string and the value is the required proficiency.  Profiency must be an int between 1-100.</param>
        public Skill(string name, int diff, int? prof, int hpeffect, int mpeffect, int moveseffect, int carryeffect, int levelreq, Dictionary<string, int> _preliminaryPrerequisites)
        {
            this.skillName = name;
            this.difficulty = diff;
            this.proficiency = prof;

            this.effectOnHP = hpeffect;
            this.effectOnMP = mpeffect;
            this.effectOnMoves = moveseffect;
            this.effectOnCarryWeight = carryeffect;

            this.levelRequired = levelreq;

            this.preliminaryPrerequisites = _preliminaryPrerequisites;

            this.finalPrerequisites = new Dictionary<Skill, int>();
        }

        #endregion

        public void InitializeSkill(SkillTree skillTree)
        {
            if (this.preliminaryPrerequisites.Count > 0)
            {
                foreach(KeyValuePair<string, int> preReq in this.preliminaryPrerequisites)
                {
                    if (preReq.Value <= 0 || preReq.Value > 100)
                    {
                        //TODO: throw an exception.  Can't have a prerequisite that is less than 0% proficiency or greater than 100%
                    }
                    else
                    {
                        Skill querySkill = (from skill in skillTree.Skills
                                            where skill.SkillName.ToLower() == preReq.Key.ToLower()
                                            select skill).FirstOrDefault();

                        if (querySkill != null)
                        {
                            this.finalPrerequisites.Add(querySkill, preReq.Value);
                        }
                        else
                        {
                            //TODO: throw an exception.  Couldn't find the skill defined in the skill tree.  User should go back and review their skill tree file to ensure that all "prerequisite" skills that are referenced are defined.
                            throw new Exception();
                        }
                    }
                }
            }
        }

        #region Attribute Accessors

        public string SkillName
        {
            get { return this.skillName; }
            set { this.skillName = value; }
        }

        public int Difficulty
        {
            get { return this.difficulty; }
        }

        public int? Proficiency
        {
            get { return this.proficiency; }
            set { this.proficiency = value; }
        }

        public int LevelRequired
        {
            get;
            set;
        }

        #endregion
    }
}
