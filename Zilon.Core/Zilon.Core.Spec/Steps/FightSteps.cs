﻿using LightInject;

using TechTalk.SpecFlow;

using Zilon.Core.Client;
using Zilon.Core.Commands;
using Zilon.Core.Spec.Contexts;
using Zilon.Core.Tests.Common;

namespace Zilon.Core.Spec.Steps
{
    [Binding]
    public class FightSteps : GenericStepsBase<CommonGameActionsContext>
    {
        protected FightSteps(CommonGameActionsContext context) : base(context)
        {
        }

        [When(@"Актёр игрока атакует монстра Id:(.*)")]
        public void WhenАктёрИгрокаАтакуетМонстраId(int monsterId)
        {
            var attackCommand = _context.Container.GetInstance<ICommand>("attack");
            var playerState = _context.Container.GetInstance<IPlayerState>();

            var monster = _context.GetMonsterById(monsterId);

            var monsterViewModel = new TestActorViewModel {
                Actor = monster
            };

            playerState.HoverViewModel = monsterViewModel;

            attackCommand.Execute();
        }

    }
}