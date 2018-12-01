﻿using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using Moq;

using NUnit.Framework;
using Zilon.Core.CommonServices.Dices;
using Zilon.Core.MapGenerators;
using Zilon.Core.Players;
using Zilon.Core.Schemes;
using Zilon.Core.Tactics;
using Zilon.Core.Tactics.Behaviour.Bots;
using Zilon.Core.Tactics.Spatial;
using Zilon.Core.Tests.Common.Schemes;

namespace Zilon.Core.Tests.MapGenerators
{
    [TestFixture]
    public class SectorProceduralGeneratorTests
    {
        /// <summary>
        /// Тест проверяет, что для различных карт генератор сектора работает без ошибок.
        /// </summary>
        [Test]
        public void Create_DifferentMaps_NoExceptions()
        {
            // ARRANGE
            var randomSource = new TestSnakeRandomSource();
            var mapFactory = new DungeonMapFactory(randomSource);

            var schemeService = CreateSchemeService();
            var botPlayer = CreateBotPlayer();
            var generator = CreateGenerator(randomSource, schemeService, botPlayer, mapFactory);



            // ACT
            Action act = () => {
                var sector = generator.Generate();
            };



            // ASSERT
            act.Should().NotThrow();
        }

        /// <summary>
        /// Тест проверяет, что для различных карт генератор сектора работает без ошибок.
        /// </summary>
        [Test]
        [TestCase(1)]
        [TestCase(123)]
        [TestCase(3257)]
        [TestCase(636)]
        [TestCase(100000)]
        public void Create_DifferentMapsRealDice_NoExceptions(int diceSeed)
        {
            // ARRANGE
            var dice = new Dice(diceSeed);
            var randomSource = new SectorGeneratorRandomSource(dice);
            var mapFactory = new DungeonMapFactory(randomSource);

            var schemeService = CreateSchemeService();
            var botPlayer = CreateBotPlayer();
            var generator = CreateGenerator(randomSource, schemeService, botPlayer, mapFactory);



            // ACT
            Action act = () =>
            {
                var sector = generator.Generate();
            };



            // ASSERT
            act.Should().NotThrow();
        }

        private static SectorProceduralGenerator CreateGenerator(ISectorGeneratorRandomSource randomSource,
            ISchemeService schemeService,
            IBotPlayer botPlayer,
            IMapFactory mapFactory)
        {
            var dropResolverMock = new Mock<IDropResolver>();
            var dropResolver = dropResolverMock.Object;

            var actorManagerMock = new Mock<IActorManager>();
            var actorManager = actorManagerMock.Object;

            var propContainerManagerMock = new Mock<IPropContainerManager>();
            var propContainerManager = propContainerManagerMock.Object;

            return new SectorProceduralGenerator(
                actorManager,
                propContainerManager,
                randomSource,
                botPlayer, 
                schemeService,
                dropResolver,
                mapFactory);
        }


        private static ISector CreateSector()
        {
            var patrolRoutes = new Dictionary<IActor, IPatrolRoute>();
            var sectorMock = new Mock<ISector>();
            sectorMock.SetupGet(x => x.PatrolRoutes).Returns(patrolRoutes);
            var sector = sectorMock.Object;
            return sector;
        }

        private static IMap CreateFakeMap()
        {
            var nodes = new List<IMapNode>();
            var edges = new List<IEdge>();
            var mapMock = new Mock<IMap>();
            mapMock.SetupGet(x => x.Nodes).Returns(nodes);
            mapMock.SetupGet(x => x.Edges).Returns(edges);
            var map = mapMock.Object;
            return map;
        }

        private static ISchemeService CreateSchemeService()
        {
            var schemeServiceMock = new Mock<ISchemeService>();

            var propScheme = new TestPropScheme
            {
                Sid = "test-prop"
            };
            
            schemeServiceMock.Setup(x => x.GetScheme<IPropScheme>(It.IsAny<string>()))
                .Returns(propScheme);

            var trophyTableScheme = new TestDropTableScheme(0, new DropTableRecordSubScheme[0])
            {
                Sid = "default"
            };
            schemeServiceMock.Setup(x => x.GetScheme<IDropTableScheme>(It.IsAny<string>()))
                .Returns(trophyTableScheme);

            var monsterScheme = new TestMonsterScheme
            {
                PrimaryAct = new TestTacticalActStatsSubScheme()
            };

            schemeServiceMock.Setup(x => x.GetScheme<IMonsterScheme>(It.IsAny<string>()))
                .Returns(monsterScheme);

            var schemeService = schemeServiceMock.Object;
            return schemeService;
        }

        private static IBotPlayer CreateBotPlayer()
        {
            var botPlayerMock = new Mock<IBotPlayer>();
            var botPlayer = botPlayerMock.Object;
            return botPlayer;
        }
    }
}