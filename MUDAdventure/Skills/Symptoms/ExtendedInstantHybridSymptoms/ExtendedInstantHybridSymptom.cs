using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using MUDAdventure.Skills.Symptoms.InstantSymptoms;

namespace MUDAdventure.Skills.Symptoms.ExtendedInstantHybirdSymptoms
{
    /// <summary>
    /// Designed for a symptom with an extended duration that has reptitive instant symptoms at set intervals, i.e. poison (-10 hp every 2 seconds, etc.)
    /// </summary>
    abstract class ExtendedInstantHybridSymptom :Symptom
    {
        protected Timer totalDuration, symptomInterval;
        protected InstantSymptom instantSymptom;

        #region Constructors

        /// <summary>
        /// Default constructoer
        /// </summary>
        public ExtendedInstantHybridSymptom() : base() { }

        /// <summary>
        /// Constructor with arguments
        /// </summary>
        /// <param name="_targetCharacter">The character who is affected by this symptom</param>
        /// <param name="_totalDuration">The total duration that this symptom lasts</param>
        /// <param name="_symptomInterval">The interval at which the instant symptom kicks in</param>
        /// <param name="_instantSymptom">The instant symptom affecting the target character</param>
        public ExtendedInstantHybridSymptom(Character _targetCharacter, string _associatedWith, Timer _totalDuration, Timer _symptomInterval, InstantSymptom _instantSymptom) :base(_targetCharacter, _associatedWith)
        {
            this.totalDuration = _totalDuration;
            this.symptomInterval = _symptomInterval;

            this.instantSymptom = _instantSymptom;
        }

        #endregion

        #region Attribute Accessors

        public Timer TotalDuration
        {
            get { return this.totalDuration; }
            set { this.totalDuration = value; }
        }

        public Timer SymptomInterval
        {
            get { return this.symptomInterval; }
            set { this.symptomInterval = value; }
        }

        public InstantSymptom InstantSymptom
        {
            get { return this.instantSymptom; }
            set { this.instantSymptom = value; }
        }

        #endregion
    }
}
