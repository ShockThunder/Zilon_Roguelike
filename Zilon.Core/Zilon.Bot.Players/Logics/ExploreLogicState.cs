﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Zilon.Core.Graphs;
using Zilon.Core.Tactics;
using Zilon.Core.Tactics.Behaviour;
using Zilon.Core.Tactics.Behaviour.Bots;
using Zilon.Core.Tactics.Spatial;

namespace Zilon.Bot.Players.Logics
{
    //TODO Перепроверить работу этого состояния.
    // Есть подозрение, что оно не работает.
    public sealed class ExploreLogicState : MoveLogicStateBase
    {
        public ExploreLogicState(IDecisionSource decisionSource, ISectorManager sectorManager) : base(decisionSource, sectorManager)
        {
        }

        public override IActorTask GetTask(IActor actor, ILogicStrategyData strategyData)
        {
            if (MoveTask == null)
            {
                MoveTask = CreateBypassMoveTask(actor, strategyData);

                if (MoveTask != null)
                {
                    return MoveTask;
                }
                else
                {
                    // Это может произойти, если актёр не выбрал следующий узел.
                    // Тогда переводим актёра в режим ожидания.

                    IdleTask = new IdleTask(actor, DecisionSource);
                    return IdleTask;
                }
            }
            else
            {
                if (!MoveTask.IsComplete)
                {
                    // Если команда на перемещение к целевой точке патруля не закончена,
                    // тогда продолжаем её.
                    // Предварительно проверяем, не мешает ли что-либо её продолжить выполнять.
                    if (!MoveTask.CanExecute())
                    {
                        MoveTask = CreateBypassMoveTask(actor, strategyData);
                    }

                    if (MoveTask != null)
                    {
                        return MoveTask;
                    }

                    IdleTask = new IdleTask(actor, DecisionSource);
                    return IdleTask;
                }
                else
                {
                    Complete = true;
                    return null;
                }
            }
        }

        private IEnumerable<IGraphNode> WriteObservedNodes(IActor actor, ILogicStrategyData strategyData)
        {
            var observeNodes = Map.Nodes.Where(x => Map.DistanceBetween(x, actor.Node) < 5);

            foreach (var mapNode in observeNodes)
            {
                strategyData.ObserverdNodes.Add(mapNode);
            }

            // Собираем пограничные неисследованные узлы.
            var frontNodes = new HashSet<IGraphNode>();
            foreach (var observedNode in strategyData.ObserverdNodes)
            {
                var nextNodes = Map.GetNext(observedNode);

                var notObservedNextNodes = nextNodes.Where(x => !strategyData.ObserverdNodes.Contains(x));

                foreach (var edgeNode in notObservedNextNodes)
                {
                    frontNodes.Add(edgeNode);
                }

                // Примечаем выходы
                if (Map.Transitions.ContainsKey(observedNode))
                {
                    strategyData.ExitNodes.Add(observedNode);
                }
            }

            var emptyFrontNodes = !frontNodes.Any();
            var allNodesObserved = Map.Nodes.All(x => strategyData.ObserverdNodes.Contains(x));

            Debug.Assert((emptyFrontNodes && allNodesObserved) || !emptyFrontNodes,
                "Это состояние выполняется, только если есть неисследованые узлы.");

            return frontNodes;
        }

        private MoveTask CreateBypassMoveTask(IActor actor, ILogicStrategyData strategyData)
        {
            IEnumerable<IGraphNode> availableNodes;
            var frontNodes = WriteObservedNodes(actor, strategyData).ToArray();
            if (frontNodes.Any())
            {
                availableNodes = frontNodes;
            }
            else
            {
                availableNodes = strategyData.ObserverdNodes;
            }

            var availableNodesArray = availableNodes as HexNode[] ?? availableNodes.ToArray();
            for (var i = 0; i < 3; i++)
            {
                var targetNode = DecisionSource.SelectTargetRoamingNode(availableNodesArray);

                if (Map.IsPositionAvailableFor(targetNode, actor))
                {
                    var moveTask = new MoveTask(actor, targetNode, Map);

                    return moveTask;
                }
            }

            return null;
        }
    }
}
