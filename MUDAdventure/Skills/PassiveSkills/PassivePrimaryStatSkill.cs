using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills.PassiveSkills
{
    class PassivePrimaryStatSkill : Skill
    {
        protected int? effectOnStrength, effectOnAgility, effectOnConstitution, effectOnIntelligence, effectOnLearning;

        #region Constructors

        public PassivePrimaryStatSkill() : base() { }

        public PassivePrimaryStatSkill(string name, int diff, int? prof, int? effOnStr, int? effOnAgi, int? effOnCon, int? effOnInt, int? effOnLea) : base(name, diff, prof)
        {
            this.effectOnStrength = effOnStr;
            this.effectOnAgility = effOnAgi;
            this.effectOnConstitution = effOnCon;
            this.effectOnIntelligence = effOnInt;
            this.effectOnLearning = effOnLea;
        }

        public PassivePrimaryStatSkill(string name, int diff, int? prof, int levelreq, Dictionary<string, int> _preliminaryPrerequisites, int? effOnStr, int? effOnAgi, int? effOnCon, int? effOnInt, int? effOnLea)
            : base(name, diff, prof,levelreq, _preliminaryPrerequisites)
        {
            this.effectOnStrength = effOnStr;
            this.effectOnAgility = effOnAgi;
            this.effectOnConstitution = effOnCon;
            this.effectOnIntelligence = effOnInt;
            this.effectOnLearning = effOnLea;
        }

        #endregion

        #region Attribute Accessors

        public int? EffectOnStrength
        {
            get { return this.effectOnStrength; }
            set { this.effectOnStrength = value; }
        }

        public int? EffectOnAgility
        {
            get { return this.effectOnAgility; }
            set { this.effectOnAgility = value; }
        }

        public int? EffectOnConstitution
        {
            get { return this.effectOnConstitution; }
            set { this.effectOnConstitution = value; }
        }

        public int? EffectOnIntellingence
        {
            get { return this.effectOnIntelligence; }
            set { this.effectOnIntelligence = value; }
        }

        public int? EffectOnLearning
        {
            get { return this.effectOnLearning; }
            set { this.effectOnLearning = value; }
        }

        #endregion
    }
}
