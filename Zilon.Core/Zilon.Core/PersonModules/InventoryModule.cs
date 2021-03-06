﻿using Zilon.Core.Props;

namespace Zilon.Core.PersonModules
{
    /// <summary>
    /// Инвентарь персонажа.
    /// </summary>
    public sealed class InventoryModule : PropStoreBase, IInventoryModule
    {
        public InventoryModule(): base()
        {
            IsActive = true;
        }

        public string Key { get => nameof(IInventoryModule); }
        public bool IsActive { get; set; }
    }
}
