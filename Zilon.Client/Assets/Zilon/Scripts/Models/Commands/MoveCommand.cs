﻿using Assets.Zilon.Scripts.Models.CombatScene;
using Assets.Zilon.Scripts.Models.SectorScene;

namespace Assets.Zilon.Scripts.Models.Commands
{
    /// <summary>
    /// Команда на перемещение взвода в указанный узел карты.
    /// </summary>
    class MoveCommand : ActorCommandBase
    {

        public MoveCommand(ISectorManager sectorManager,
            IPlayerState playerState) : 
            base(sectorManager, playerState)
        {
            
        }

        public override bool CanExecute()
        {
            return true;
        }

        protected override void ExecuteTacticCommand()
        {
            var sector = _sectorManager.CurrentSector;
            var selectedNodeVM = _playerState.SelectedNode;
            
            var targetNode = selectedNodeVM.Node;
            _playerState.TaskSource.IntentMove(targetNode);
            sector.Update();
        }
    }
}