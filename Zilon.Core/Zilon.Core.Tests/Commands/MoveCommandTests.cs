﻿using FluentAssertions;

using LightInject;

using Moq;

using NUnit.Framework;

using System.Linq;

using Zilon.Core.Client;
using Zilon.Core.Tactics;
using Zilon.Core.Tactics.Behaviour;
using Zilon.Core.Tactics.Behaviour.Bots;
using Zilon.Core.Tactics.Spatial;
using Zilon.Core.Tests.TestCommon;

namespace Zilon.Core.Commands.Tests
{
    [TestFixture()]
    public class MoveCommandTests
    {
        private ServiceContainer _container;


        /// <summary>
        /// Тест проверяет, что можно перемещаться в пустые узлы карты.
        /// </summary>
        [Test]
        public void CanExecuteTest()
        {
            // ARRANGE
            var command = _container.GetInstance<MoveCommand>();



            // ACT
            var canExecute = command.CanExecute();


            // ASSERT
            canExecute.Should().Be(true);
        }

        /// <summary>
        /// Тест проверяет, что при выполнении команды корректно фисируется намерение игрока на атаку.
        /// </summary>
        [Test]
        public void ExecuteTest()
        {
            var command = _container.GetInstance<MoveCommand>();
            var humanTaskSourceMock = _container.GetInstance<Mock<IHumanActorTaskSource>>();
            var playerState = _container.GetInstance<IPlayerState>();



            // ACT
            command.Execute();


            // ASSERT
            var target = ((IMapNodeViewModel)playerState.HoverViewModel).Node;
            humanTaskSourceMock.Verify(x => x.IntentMove(target));
        }

        [SetUp]
        public void SetUp()
        {
            _container = new ServiceContainer();

            var testMap = new TestGridGenMap();

            var sectorMock = new Mock<ISector>();
            sectorMock.SetupGet(x => x.Map).Returns(testMap);
            var sector = sectorMock.Object;

            var sectorManagerMock = new Mock<ISectorManager>();
            sectorManagerMock.SetupProperty(x => x.CurrentSector, sector);
            var sectorManager = sectorManagerMock.Object;


            var actorMock = new Mock<IActor>();
            var actorNode = testMap.Nodes.OfType<HexNode>().SelectBy(0, 0);
            actorMock.SetupGet(x => x.Node).Returns(actorNode);
            var actor = actorMock.Object;

            var actorVmMock = new Mock<IActorViewModel>();
            actorVmMock.SetupProperty(x => x.Actor, actor);
            var actorVm = actorVmMock.Object;


            var targetNode = testMap.Nodes.OfType<HexNode>().SelectBy(1, 0);
            var targetVmMock = new Mock<IMapNodeViewModel>();
            targetVmMock.SetupProperty(x => x.Node, targetNode);
            var targetVm = targetVmMock.Object;

            var decisionSourceMock = new Mock<IDecisionSource>();
            var decisionSource = decisionSourceMock.Object;

            var humanTaskSourceMock = new Mock<IHumanActorTaskSource>();
            var humanTaskSource = humanTaskSourceMock.Object;

            var playerStateMock = new Mock<IPlayerState>();
            playerStateMock.SetupProperty(x => x.ActiveActor, actorVm);
            playerStateMock.SetupProperty(x => x.HoverViewModel, targetVm);
            playerStateMock.SetupProperty(x => x.TaskSource, humanTaskSource);
            var playerState = playerStateMock.Object;


            _container.Register<MoveCommand>();
            _container.Register(factory => sectorManager);
            _container.Register(factory => playerState);
            _container.Register(factory => humanTaskSourceMock);
        }
    }
}