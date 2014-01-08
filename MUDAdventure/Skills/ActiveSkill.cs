using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace MUDAdventure.Skills
{
    class ActiveSkill : Skill
    {
        protected int coolDown;
        protected Timer coolDownTimer;
        protected bool skillAvailable;

        public ActiveSkill() : base() {}

        public ActiveSkill(string name, int diff, int prof, int hpeffect, int mpeffect, int moveseffect, int carryeffect, int cool) : base(name, diff, prof, hpeffect, mpeffect, moveseffect, carryeffect) 
        {
            this.coolDown = cool;
            this.skillAvailable = true;
        }

        public ActiveSkill(string name, int diff, int prof, int hpeffect, int mpeffect, int moveseffect, int carryeffect, int cool, int levelreq, Dictionary<string, int> _preliminaryPrerequisites) : base(name, diff, prof, hpeffect, mpeffect, moveseffect, carryeffect, levelreq, _preliminaryPrerequisites) 
        {
            this.coolDown = cool;
            this.skillAvailable = true;
        }

        public void Used()
        {
            this.skillAvailable = false;

            this.coolDownTimer = new Timer();
            this.coolDownTimer.Interval = this.coolDown;

            this.coolDownTimer.Elapsed +=new ElapsedEventHandler(coolDownTimer_Elapsed);

            this.coolDownTimer.Enabled = true;
            this.coolDownTimer.Start();
        }

        #region Event Handlers

        private void coolDownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.skillAvailable = true;
        }

        #endregion

        #region Attribute Accessors

        public bool SkillAvailable
        {
            get;
            set;
        }

        public int CoolDown
        {
            get;
            set;
        }

        #endregion
    }
}
