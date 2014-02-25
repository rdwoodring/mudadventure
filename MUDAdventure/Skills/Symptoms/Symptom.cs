using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MUDAdventure.Skills.Symptoms
{
    abstract class Symptom
    {

        protected Character targetCharacter;
        protected string associatedWith;

        #region Constructors

        ///<summary>
        ///Default constructor
        ///</summary>
        public Symptom() { }

        public Symptom(Character _targetCharacter, string _associatedWith)
        {
            this.targetCharacter = _targetCharacter;
            this.associatedWith = _associatedWith;
        }

        #endregion

        public abstract void DoEffect();

        #region Attribute Accessors

        public Character TargetCharacter
        {
            get { return this.targetCharacter; }
            set { this.targetCharacter = value; }
        }

        public string AssociatedWith
        {
            get { return this.associatedWith; }
            set { this.associatedWith = value; }
        }

        #endregion
    }
}
