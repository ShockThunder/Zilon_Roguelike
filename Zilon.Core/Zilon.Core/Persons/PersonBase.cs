﻿using System.Collections.Generic;
using Zilon.Core.PersonModules;

namespace Zilon.Core.Persons
{
    public abstract class PersonBase : IPerson
    {
        private readonly IDictionary<string, IPersonModule> _modules;

        /// <inheritdoc/>
        public abstract int Id { get; set; }

        /// <inheritdoc/>
        public abstract PhysicalSize PhysicalSize { get; }

        protected PersonBase()
        {
            _modules = new Dictionary<string, IPersonModule>();
        }

        /// <inheritdoc/>
        public TPersonModule GetModule<TPersonModule>(string key) where TPersonModule : IPersonModule
        {
            return (TPersonModule)_modules[key];
        }

        /// <inheritdoc/>
        public bool HasModule(string key)
        {
            return _modules.ContainsKey(key);
        }

        /// <inheritdoc/>
        public void AddModule<TPersonModule>(TPersonModule sectorObjectModule) where TPersonModule : IPersonModule
        {
            _modules.Add(sectorObjectModule.Key, sectorObjectModule);
        }
    }
}