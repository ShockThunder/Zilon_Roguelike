﻿using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using NUnit.Framework;

using Zilon.Core.Client;
using Zilon.Core.Common;
using Zilon.Core.Components;
using Zilon.Core.MapGenerators.PrimitiveStyle;
using Zilon.Core.Persons;
using Zilon.Core.Schemes;
using Zilon.Core.Tactics;
using Zilon.Core.Tactics.Behaviour;
using Zilon.Core.Tactics.Spatial;
using Zilon.Core.Tests.Common;
using Zilon.Core.Tests.Common.Schemes;

namespace Zilon.Core.Tests.Commands
{
    [TestFixture]
    public abstract class CommandTestBase
    {
        protected IServiceCollection Container { get; private set; }
        protected IServiceProvider ServiceProvider { get; set; }

        [SetUp]
        public async System.Threading.Tasks.Task SetUpAsync()
        {
            Container = new ServiceCollection();

            var testMap = await SquareMapFactory.CreateAsync(10).ConfigureAwait(false);

            var sectorMock = new Mock<ISector>();
            sectorMock.SetupGet(x => x.Map).Returns(testMap);
            var sector = sectorMock.Object;

            var sectorManagerMock = new Mock<ISectorManager>();
            sectorManagerMock.SetupGet(x => x.CurrentSector).Returns(sector);
            var sectorManager = sectorManagerMock.Object;
            var simpleAct = CreateSimpleAct();
            var cooldownAct = CreateActWithCooldown();
            var cooldownResolvedAct = CreateActWithResolvedCooldown();

            var actCarrierMock = new Mock<ITacticalActCarrier>();
            actCarrierMock.SetupGet(x => x.Acts)
                .Returns(new[] { simpleAct, cooldownAct, cooldownResolvedAct });
            var actCarrier = actCarrierMock.Object;

            var equipmentCarrierMock = new Mock<IEquipmentCarrier>();
            equipmentCarrierMock.SetupGet(x => x.Slots).Returns(new[] { new PersonSlotSubScheme {
                Types = EquipmentSlotTypes.Hand
            } });
            var equipmentCarrier = equipmentCarrierMock.Object;

            var personMock = new Mock<IPerson>();
            personMock.SetupGet(x => x.TacticalActCarrier).Returns(actCarrier);
            personMock.SetupGet(x => x.EquipmentCarrier).Returns(equipmentCarrier);
            var person = personMock.Object;

            var actorMock = new Mock<IActor>();
            var actorNode = testMap.Nodes.OfType<HexNode>().SelectBy(0, 0);
            actorMock.SetupGet(x => x.Node).Returns(actorNode);
            actorMock.SetupGet(x => x.Person).Returns(person);
            var actor = actorMock.Object;

            var actorVmMock = new Mock<IActorViewModel>();
            actorVmMock.SetupProperty(x => x.Actor, actor);
            var actorVm = actorVmMock.Object;

            var humanTaskSourceMock = new Mock<IHumanActorTaskSource>();
            var humanTaskSource = humanTaskSourceMock.Object;

            var playerStateMock = new Mock<ISectorUiState>();
            playerStateMock.SetupProperty(x => x.ActiveActor, actorVm);
            playerStateMock.SetupProperty(x => x.TaskSource, humanTaskSource);
            playerStateMock.SetupProperty(x => x.TacticalAct, simpleAct);
            var playerState = playerStateMock.Object;

            var gameLoopMock = new Mock<IGameLoop>();
            var gameLoop = gameLoopMock.Object;

            var usageServiceMock = new Mock<ITacticalActUsageService>();
            var usageService = usageServiceMock.Object;

            Container.AddSingleton(factory => sectorManager);
            Container.AddSingleton(factory => humanTaskSourceMock);
            Container.AddSingleton(factory => playerState);
            Container.AddSingleton(factory => gameLoop);
            Container.AddSingleton(factory => usageService);

            RegisterSpecificServices(testMap, playerStateMock);

            ServiceProvider = Container.BuildServiceProvider();
        }

        private static ITacticalAct CreateSimpleAct()
        {
            var actMock = new Mock<ITacticalAct>();
            var actStatScheme = new TestTacticalActStatsSubScheme
            {
                Range = new Range<int>(1, 2)
            };
            actMock.SetupGet(x => x.Stats).Returns(actStatScheme);
            var act = actMock.Object;
            return act;
        }

        private static ITacticalAct CreateActWithCooldown()
        {
            var actMock = new Mock<ITacticalAct>();
            var actStatScheme = new TestTacticalActStatsSubScheme
            {
                Range = new Range<int>(1, 2)
            };
            actMock.SetupGet(x => x.Stats).Returns(actStatScheme);
            actMock.SetupGet(x => x.CurrentCooldown).Returns(1);
            var act = actMock.Object;
            return act;
        }

        private static ITacticalAct CreateActWithResolvedCooldown()
        {
            var actMock = new Mock<ITacticalAct>();
            var actStatScheme = new TestTacticalActStatsSubScheme
            {
                Range = new Range<int>(1, 2)
            };
            actMock.SetupGet(x => x.Stats).Returns(actStatScheme);
            actMock.SetupGet(x => x.CurrentCooldown).Returns(0);
            var act = actMock.Object;
            return act;
        }

        protected abstract void RegisterSpecificServices(IMap testMap, Mock<ISectorUiState> playerStateMock);
    }
}
