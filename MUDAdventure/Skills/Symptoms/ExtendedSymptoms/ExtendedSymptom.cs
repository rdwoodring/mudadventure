using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace MUDAdventure.Skills.Symptoms.ExtendedSymptoms
{
    ///<summary>
    ///For skills/spells/effects that have a lasting static effect, i.e. confusion, fatigue, reduced health/move/mana regen etc.
    ///</summary>
    abstract class ExtendedSymptom : Symptom
    {
        protected Timer duration;

        #region Constructors

        public ExtendedSymptom() : base() { }

        public ExtendedSymptom(Character _targetCharacter, string _associatedWith, double _symptomDuration) : base(_targetCharacter, _associatedWith)
        {
            this.duration = new Timer(_symptomDuration);
            //TODO: assign a handler to the timer and start it.  When it expires, remove the symptom from the target character's symptom list
        }

        #endregion

        #region Attribute Accessors

        public Timer Duration
        {
            get { return this.duration; }
            set { this.duration = value; }
        }

        #endregion
    }
}
