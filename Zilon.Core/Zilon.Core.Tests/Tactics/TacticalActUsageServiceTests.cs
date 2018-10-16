﻿using JetBrains.Annotations;

using Moq;

using NUnit.Framework;

using Zilon.Core.Common;
using Zilon.Core.Components;
using Zilon.Core.Persons;
using Zilon.Core.Schemes;
using Zilon.Core.Tactics;
using Zilon.Core.Tactics.Spatial;
using Zilon.Core.Tests.Common.Schemes;

namespace Zilon.Core.Tests.Tactics
{
    [TestFixture]
    public class TacticalActUsageServiceTests
    {
        private ITacticalActUsageRandomSource _actUsageRandomSource;
        private Mock<IPerkResolver> _perkResolverMock;
        private IPerkResolver _perkResolver;
        private ITacticalAct _act;
        private IPerson _person;

        /// <summary>
        /// Тест проверяет, что сервис использования действий если монстр стал мёртв,
        /// то засчитывается прогресс по перкам.
        /// </summary>
        [Test]
        public void UseOn_MonsterHitByActAndKill_SetPerkProgress()
        {
            // ARRANGE

            var actUsageService = new TacticalActUsageService(_actUsageRandomSource, _perkResolver);

            var actorMock = new Mock<IActor>();
            actorMock.SetupGet(x => x.Node).Returns(new HexNode(0, 0));
            actorMock.SetupGet(x => x.Person).Returns(_person);
            var actor = actorMock.Object;

            var monsterMock = CreateOnHitMonsterMock();
            var monster = monsterMock.Object;



            // ACT
            actUsageService.UseOn(actor, monster, _act);



            // ASSERT
            _perkResolverMock.Verify(x => x.ApplyProgress(
                It.Is<IJobProgress>(progress => CheckDefeateProgress(progress, monster)),
                It.IsAny<IEvolutionData>()
                ), Times.Once);
        }

        /// <summary>
        /// Тест проверяет, что действием с определённым типом наступления
        /// успешно выполняется при различных типах обороны.
        /// </summary>
        [Test]
        public void UseOn_OffenceTypeVsDefenceType_Success()
        {
            // ARRANGE
            var offenceType = OffenseType.Tactical;
            var defenceType = DefenceType.TacticalDefence;
            var defenceLevel = PersonRuleLevel.Normal;
            var fakeToHitDiceRoll = 5; // 5+ - успех при нормальном уровне обороны

            var actUsageRandomSourceMock = new Mock<ITacticalActUsageRandomSource>();
            actUsageRandomSourceMock.Setup(x => x.RollToHit()).Returns(fakeToHitDiceRoll);
            var actUsageRandomSource = actUsageRandomSourceMock.Object;

            var actUsageService = new TacticalActUsageService(actUsageRandomSource, _perkResolver);

            var actorMock = new Mock<IActor>();
            actorMock.SetupGet(x => x.Node).Returns(new HexNode(0, 0));
            var actor = actorMock.Object;

            var defences = new[] { new PersonDefenceItem(defenceType, defenceLevel) };
            var monsterMock = CreateMonsterMock(defences);
            var monster = monsterMock.Object;

            // Настройка дествия
            var actScheme = new TestTacticalActStatsSubScheme
            {
                Offence = new TestTacticalActOffenceSubScheme
                {
                    Type = offenceType
                }
            };

            var actMock = new Mock<ITacticalAct>();
            actMock.SetupGet(x => x.Stats).Returns(actScheme);
            var act = actMock.Object;



            // ACT
            actUsageService.UseOn(actor, monster, act);



            // ASSERT
            monsterMock.Verify(x => x.TakeDamage(It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// Тест проверяет, что если действие имеет больший ранг пробития,
        /// то броня игнорируется.
        /// </summary>
        [Test]
        public void UseOn_ActApGreaterRankThatArmorRank_IgnoreArmor()
        {
            // ARRANGE
            var offenceType = OffenseType.Tactical;
            var fakeToHitDiceRoll = 2; // успех в ToHit 2+

            var actUsageRandomSourceMock = new Mock<ITacticalActUsageRandomSource>();
            actUsageRandomSourceMock.Setup(x => x.RollToHit()).Returns(fakeToHitDiceRoll);
            var actUsageRandomSource = actUsageRandomSourceMock.Object;

            var actUsageService = new TacticalActUsageService(actUsageRandomSource, _perkResolver);

            var actorMock = new Mock<IActor>();
            actorMock.SetupGet(x => x.Node).Returns(new HexNode(0, 0));
            var actor = actorMock.Object;

            var armors = new[] { new PersonArmorItem(ImpactType.Kinetic, PersonRuleLevel.Normal, 9) };
            var monsterMock = CreateMonsterMock(armors: armors);
            var monster = monsterMock.Object;

            // Настройка дествия
            var actScheme = new TestTacticalActStatsSubScheme
            {
                Offence = new TestTacticalActOffenceSubScheme
                {
                    Type = offenceType,
                    ApRank = 10,
                    Impact = ImpactType.Kinetic
                }
            };

            var actMock = new Mock<ITacticalAct>();
            actMock.SetupGet(x => x.Stats).Returns(actScheme);
            var act = actMock.Object;



            // ACT
            actUsageService.UseOn(actor, monster, act);



            // ASSERT
            monsterMock.Verify(x => x.TakeDamage(It.IsAny<int>()), Times.Once);
            actUsageRandomSourceMock.Verify(x => x.RollArmorSave(), Times.Never);
        }

        /// <summary>
        /// Тест проверяет, что броня поглощает урон.
        /// </summary>
        [Test]
        public void UseOn_ArmorSavePassed_ActEfficientDecrease()
        {
            // ARRANGE
            const OffenseType offenceType = OffenseType.Tactical;
            const int fakeToHitDiceRoll = 2; // успех в ToHit 2+
            const int fakeArmorSaveDiceRoll = 6; // успех в ArmorSave 4+ при раных рангах
            const int fakeActEfficientRoll = 3;  // эффективность пробрасывается D3, максимальный бросок
            const int expectedActEfficient = fakeActEfficientRoll - 1;  // -1 даёт текущая броня

            var actUsageRandomSourceMock = new Mock<ITacticalActUsageRandomSource>();
            actUsageRandomSourceMock.Setup(x => x.RollToHit()).Returns(fakeToHitDiceRoll);
            actUsageRandomSourceMock.Setup(x => x.RollArmorSave()).Returns(fakeArmorSaveDiceRoll);
            actUsageRandomSourceMock.Setup(x => x.RollEfficient(It.IsAny<Roll>())).Returns(fakeActEfficientRoll);
            var actUsageRandomSource = actUsageRandomSourceMock.Object;

            var actUsageService = new TacticalActUsageService(actUsageRandomSource, _perkResolver);

            var actorMock = new Mock<IActor>();
            actorMock.SetupGet(x => x.Node).Returns(new HexNode(0, 0));
            var actor = actorMock.Object;

            var armors = new[] { new PersonArmorItem(ImpactType.Kinetic, PersonRuleLevel.Lesser, 10) };
            var monsterMock = CreateMonsterMock(armors: armors);
            var monster = monsterMock.Object;

            // Настройка дествия
            var actScheme = new TestTacticalActStatsSubScheme
            {
                Offence = new TestTacticalActOffenceSubScheme
                {
                    Type = offenceType,
                    ApRank = 10,
                    Impact = ImpactType.Kinetic
                }
            };

            var actMock = new Mock<ITacticalAct>();
            actMock.SetupGet(x => x.Stats).Returns(actScheme);
            var act = actMock.Object;



            // ACT
            actUsageService.UseOn(actor, monster, act);



            // ASSERT
            actUsageRandomSourceMock.Verify(x => x.RollArmorSave(), Times.Once);
            monsterMock.Verify(x => x.TakeDamage(It.Is<int>(damage => damage == expectedActEfficient)), Times.Once);
        }

        private static Mock<IActor> CreateMonsterMock([CanBeNull] PersonDefenceItem[] defences = null,
            [CanBeNull] PersonArmorItem[] armors = null)
        {
            var monsterMock = new Mock<IActor>();
            monsterMock.SetupGet(x => x.Node).Returns(new HexNode(1, 0));

            var monsterPersonMock = new Mock<IPerson>();
            
            var monsterSurvivalDataMock = new Mock<ISurvivalData>();
            monsterSurvivalDataMock.SetupGet(x => x.IsDead).Returns(false);
            var monsterSurvival = monsterSurvivalDataMock.Object;
            monsterPersonMock.SetupGet(x => x.Survival).Returns(monsterSurvival);

            var monsterCombatStatsMock = new Mock<ICombatStats>();
            var monsterCombatStats = monsterCombatStatsMock.Object;
            monsterPersonMock.SetupGet(x => x.CombatStats).Returns(monsterCombatStats);

            var monsterPerson = monsterPersonMock.Object;
            monsterMock.SetupGet(x => x.Person).Returns(monsterPerson);

            var monsterDefenceStatsMock = new Mock<IPersonDefenceStats>();
            monsterDefenceStatsMock.SetupGet(x => x.Defences).Returns(defences ?? new PersonDefenceItem[0]);
            monsterDefenceStatsMock.SetupGet(x => x.Armors).Returns(armors ?? new PersonArmorItem[0]);
            var monsterDefenceStats = monsterDefenceStatsMock.Object;
            monsterCombatStatsMock.SetupGet(x => x.DefenceStats).Returns(monsterDefenceStats);

            monsterMock.Setup(x => x.TakeDamage(It.IsAny<int>())).Verifiable();

            return monsterMock;
        }

        private static Mock<IActor> CreateOnHitMonsterMock([CanBeNull] PersonDefenceItem[] defences = null,
            [CanBeNull] PersonArmorItem[] armors = null)
        {
            var monsterMock = new Mock<IActor>();
            monsterMock.SetupGet(x => x.Node).Returns(new HexNode(1, 0));

            var monsterPersonMock = new Mock<IPerson>();

            var monsterIsDead = false;
            var monsterSurvivalDataMock = new Mock<ISurvivalData>();
            monsterSurvivalDataMock.SetupGet(x => x.IsDead).Returns(monsterIsDead);
            monsterSurvivalDataMock
                .Setup(x => x.DecreaseStat(
                    It.Is<SurvivalStatType>(s => s == SurvivalStatType.Health),
                    It.IsAny<int>())
                    )
                .Callback(() => monsterIsDead = true);
            var monsterSurvival = monsterSurvivalDataMock.Object;
            monsterPersonMock.SetupGet(x => x.Survival).Returns(monsterSurvival);

            var monsterCombatStatsMock = new Mock<ICombatStats>();
            var monsterCombatStats = monsterCombatStatsMock.Object;
            monsterPersonMock.SetupGet(x => x.CombatStats).Returns(monsterCombatStats);

            var monsterPerson = monsterPersonMock.Object;
            monsterMock.SetupGet(x => x.Person).Returns(monsterPerson);

            var monsterDefenceStatsMock = new Mock<IPersonDefenceStats>();
            monsterDefenceStatsMock.SetupGet(x => x.Defences).Returns(defences ?? new PersonDefenceItem[0]);
            monsterDefenceStatsMock.SetupGet(x => x.Armors).Returns(armors ?? new PersonArmorItem[0]);
            var monsterDefenceStats = monsterDefenceStatsMock.Object;
            monsterCombatStatsMock.SetupGet(x => x.DefenceStats).Returns(monsterDefenceStats);

            monsterMock.Setup(x => x.TakeDamage(It.IsAny<int>()))
                .Callback<int>(damage => monsterSurvival.DecreaseStat(SurvivalStatType.Health, damage))
                .Verifiable();

            return monsterMock;
        }

        private static bool CheckDefeateProgress(IJobProgress progress, IAttackTarget expectedTarget)
        {
            if (progress is DefeatActorJobProgress defeatProgress)
            {
                return defeatProgress.Target == expectedTarget;
            }

            return false;
        }

        [SetUp]
        public void SetUp()
        {
            var actUsageRandomSourceMock = new Mock<ITacticalActUsageRandomSource>();
            actUsageRandomSourceMock.Setup(x => x.RollToHit()).Returns(6);
            _actUsageRandomSource = actUsageRandomSourceMock.Object;

            _perkResolverMock = new Mock<IPerkResolver>();
            _perkResolver = _perkResolverMock.Object;

            var personMock = new Mock<IPerson>();
            _person = personMock.Object;

            var evolutionDataMock = new Mock<IEvolutionData>();
            var evolutionData = evolutionDataMock.Object;
            personMock.SetupGet(x => x.EvolutionData).Returns(evolutionData);

            var actScheme = new TestTacticalActStatsSubScheme
            {
                Offence = new TestTacticalActOffenceSubScheme
                {
                    Type = OffenseType.Tactical,
                    Impact = ImpactType.Kinetic
                }
            };

            var actMock = new Mock<ITacticalAct>();
            actMock.SetupGet(x => x.Stats).Returns(actScheme);
            _act = actMock.Object;
        }
    }
}