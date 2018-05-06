﻿using Zilon.Logic.Tactics;

namespace Assets.Zilon.Scripts.Models.Commands
{
    abstract class CombatCommandBase : ICommand
    {
        protected readonly Combat combat;

        public CombatCommandBase(Combat combat)
        {
            if (combat == null)
            {
                throw new System.ArgumentNullException(nameof(combat));
            }

            this.combat = combat;
        }

        public abstract void Execute();
    }
}