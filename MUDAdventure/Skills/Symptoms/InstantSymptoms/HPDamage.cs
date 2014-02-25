using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills.Symptoms.InstantSymptoms
{
    /// <summary>
    /// A symptom that deals instant, direct damage to a Player or NPC's current hitpoints.
    /// </summary>
    class HPDamage : InstantSymptom
    {
        #region Constructors

        /// <summary>
        /// Default constructor with no arguments
        /// </summary>
        public HPDamage() : base() { }

        /// <summary>
        /// Constructor with arguments
        /// </summary>
        /// <param name="_targetCharacter">The character upon which the symptom acts.  For this symptom, the character whose HP will be reduced.</param>
        /// <param name="_associatedWith">Which skill, disease, spell, effect, etc. the symptom is associated with.  This is used when displaying effects in the player status screen.</param>
        public HPDamage(Character _targetCharacter, string _associatedWith) : base(_targetCharacter, _associatedWith) { }

        #endregion

        public override void DoEffect()
        {
            //TODO: this is an arbitrary value.  modify to include a more realistic number instead of just "2"
            this.targetCharacter.CurrentHitpoints -= 2;
        }
    }
}
