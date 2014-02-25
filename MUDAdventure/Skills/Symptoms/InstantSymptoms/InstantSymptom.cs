using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills.Symptoms.InstantSymptoms
{
    ///<summary>
    ///For skills/spells/effects that have an instant effect, i.e. hp damage or mp damage
    ///</summary>
    abstract class InstantSymptom : Symptom
    {
        public InstantSymptom() : base() { }

        public InstantSymptom(Character _targetCharacter, string _associatedWith) : base(_targetCharacter, _associatedWith) { }
    }
}
