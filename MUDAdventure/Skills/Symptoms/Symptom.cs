using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills.Symptoms
{
    abstract class Symptom
    {
        protected Character targetCharacter;

        #region Constructors

        ///<summary>
        ///Default constructor
        ///</summary>
        public Symptom() { }

        public Symptom(Character _targetCharacter)
        {
            this.targetCharacter = _targetCharacter;
        }

        #endregion

        public abstract void DoEffect();

        #region Attribute Accessors

        public Character TargetCharacter
        {
            get { return this.targetCharacter; }
            set { this.targetCharacter = value; }
        }

        #endregion
    }
}
