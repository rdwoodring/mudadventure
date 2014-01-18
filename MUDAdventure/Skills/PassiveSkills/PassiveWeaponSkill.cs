using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MUDAdventure.Items.Weapons;

namespace MUDAdventure.Skills.PassiveSkills
{
    class PassiveWeaponSkill : Skill
    {
        protected Weapon targetWeapon;

        #region Constructors

        public PassiveWeaponSkill() : base() { }

        public PassiveWeaponSkill(string name, int diff, int? prof, Weapon _weapon) : base(name, diff, prof)
        {
            this.targetWeapon = _weapon;
        }

        public PassiveWeaponSkill(string name, int diff, int? prof, int levelreq, Dictionary<string, int> _preliminaryPrerequisites, Weapon _weapon)
            : base(name, diff, prof, levelreq, _preliminaryPrerequisites)
        {
            this.targetWeapon = _weapon;
        }

        #endregion

        #region Attribute Accessors

        public Weapon TargetWeapon
        {
            get { return this.targetWeapon; }
            set { this.targetWeapon = value; }
        }

        #endregion
    }
}
