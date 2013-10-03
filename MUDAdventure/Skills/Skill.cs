using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills
{
    class Skill
    {
        private string skillName;
        private int difficulty, proficiency;
        private int effectOnHP, effectOnMP, effectOnMoves, effectOnCarryWeight; //negative number for a "cost," positive number for a "bonus"
        private int daggerProfRequired, swordProfRequired, axeProfRequired, parryProfRequired, dodgeProfRequired, roamerProfRequired, travellerProfRequired, packmuleProfRequired, oxenProfRequired, fieldmedicineProfRequired, sneakProfRequired, hideProfRequired, ambushProfRequired, pickpocketProfRequired, picklockProfRequired;
        private int levelRequired;

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public Skill() { }

        /// <summary>
        /// Constructor with arguments
        /// </summary>
        /// <param name="name">The skill's name, i.e. parry, dodge, etc.</param>
        /// <param name="diff">How difficult the skill is to learn.  1 being easiest, 10 being hardest.</param>
        /// <param name="prof">How proficient a player is at the skill, as a percentage. 0 being worst, 100 being completely mastered.</param>
        /// <param name="hpeffect">The effect that using this skill has on HP.  A positive number is an HP bonus, a negative number is an HP cost.</param>
        /// <param name="moveseffect">The effect that using this skill has on MP.  A positive number is an HP bonus, a negative number is an MP cost.</param>
        /// <param name="mpeffect">The effect that using this skill has on Moves.  A positive number is an HP bonus, a negative number is an Moves cost.</param>
        public Skill(string name, int diff, int prof, int hpeffect, int mpeffect, int moveseffect, int carryeffect)
        {
            this.skillName = name;
            this.difficulty = diff;
            this.proficiency = prof;

            this.effectOnHP = hpeffect;
            this.effectOnMP = mpeffect;
            this.effectOnMoves = moveseffect;
            this.effectOnCarryWeight = carryeffect;

            this.levelRequired = 0;
            this.daggerProfRequired = this.swordProfRequired = this.axeProfRequired = this.parryProfRequired = this.dodgeProfRequired = this.roamerProfRequired = this.travellerProfRequired = this.packmuleProfRequired = this.oxenProfRequired = this.fieldmedicineProfRequired = this.sneakProfRequired = this.hideProfRequired = this.ambushProfRequired = this.pickpocketProfRequired = this.picklockProfRequired = 0;
        }

        /// <summary>
        /// Constructor with arguments
        /// </summary>
        /// <param name="name">The skill's name, i.e. parry, dodge, etc.</param>
        /// <param name="diff">How difficult the skill is to learn.  1 being easiest, 10 being hardest.</param>
        /// <param name="prof">How proficient a player is at the skill, as a percentage. 0 being worst, 100 being completely mastered.</param>
        /// <param name="hpeffect">The effect that using this skill has on HP.  A positive number is an HP bonus, a negative number is an HP cost.</param>
        /// <param name="moveseffect">The effect that using this skill has on MP.  A positive number is an MP bonus, a negative number is an MP cost.</param>
        /// <param name="mpeffect">The effect that using this skill has on Moves.  A positive number is an Moves bonus, a negative number is an Moves cost.</param>
        /// <param name="carryeffect">The effect that using this skill has on Carrying capacity. A positive number is a Carrying capacity bonus, a negative number is a Carrying capacity cost</param>
        /// <param name="levelreq">The required minimum level needed to begin practicing this skill. 0-100</param>
        /// <param name="dagreq">The required proficiency in the dagger skill to begin practicing this skill. 0-100</param>
        /// <param name="swordreq">The required proficiency in the sword skill to begin practicing this skill. 0-100</param>
        /// <param name="axereq">The required proficiency in the axe skill to begin practicing this skill. 0-100</param>
        /// <param name="parryreq">The required proficiency in parry skill to begin practicing this skill. 0-100</param>
        /// <param name="dodgereq">The required proficiency in dodge skill to begin practicing this skill. 0-100</param>
        /// <param name="roamerreq">The required proficiency in the roamer skill to begin practicing this skill. 0-100</param>
        /// <param name="travellerreq">The required proficiency in the traveller skill to begin practicing this skill. 0-100</param>
        /// <param name="packmulereq">The required proficiency in the packmule skill to begin practicing this skill. 0-100</param>
        /// <param name="oxenreq">The required proficiency in the oxen skill to begin practicing this skill. 0-100</param>
        /// <param name="fieldmedreq">The required proficiency in the field medicine skill to begin practicing this skill. 0-100</param>
        /// <param name="sneakreq">The required proficiency in the sneak skill to begin practicing this skill. 0-100</param>
        /// <param name="hidereq">The required proficiency in the hide skill to begin practicing this skill. 0-100</param>
        /// <param name="ambushreq">The required proficiency in the ambush skill to begin practicing this skill. 0-100</param>
        /// <param name="pickpocketreq">The required proficiency in the pickpocket skill to begin practicing this skill. 0-100</param>
        /// <param name="picklockreq">The required proficiency in the picklock skill to begin practicing this skill. 0-100</param>
        public Skill(string name, int diff, int prof, int hpeffect, int mpeffect, int moveseffect, int carryeffect, int levelreq, int dagreq, int swordreq, int axereq, int parryreq, int dodgereq, int roamerreq, int travellerreq, int packmulereq, int oxenreq, int fieldmedreq, int sneakreq, int hidereq, int ambushreq, int pickpocketreq, int picklockreq)
        {
            this.skillName = name;
            this.difficulty = diff;
            this.proficiency = prof;

            this.effectOnHP = hpeffect;
            this.effectOnMP = mpeffect;
            this.effectOnMoves = moveseffect;
            this.effectOnCarryWeight = carryeffect;

            this.levelRequired = levelreq;

            this.daggerProfRequired = dagreq;
            this.swordProfRequired = swordreq;
            this.axeProfRequired = axereq;
            this.parryProfRequired = parryreq;
            this.dodgeProfRequired = dodgereq;
            this.roamerProfRequired = roamerreq;
            this.travellerProfRequired = travellerreq;
            this.packmuleProfRequired = packmulereq;
            this.oxenProfRequired = oxenreq;
            this.fieldmedicineProfRequired = fieldmedreq;
            this.sneakProfRequired = sneakreq;
            this.hideProfRequired = hidereq;
            this.ambushProfRequired = ambushreq;
            this.pickpocketProfRequired = pickpocketreq;
            this.picklockProfRequired = picklockreq;
        }

        #endregion

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

        public int Proficiency
        {
            get { return this.proficiency; }
            set { this.proficiency = value; }
        }

        #endregion
    }
}
