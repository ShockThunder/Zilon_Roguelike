﻿using System;
using System.Collections.Generic;
using System.Linq;

using Zilon.Core.CommonServices;
using Zilon.Core.Persons;
using Zilon.Core.Props;
using Zilon.Core.Schemes;

namespace Zilon.Core.Tactics
{
    public class DropResolver : IDropResolver
    {
        private readonly IDropResolverRandomSource _randomSource;
        private readonly ISchemeService _schemeService;
        private readonly IPropFactory _propFactory;

        public DropResolver(IDropResolverRandomSource randomSource, ISchemeService schemeService, IPropFactory propFactory)
        {
            _randomSource = randomSource;
            _schemeService = schemeService;
            _propFactory = propFactory;
        }

        public IProp[] GetProps(IEnumerable<IDropTableScheme> dropTables)
        {
            var materializedDropTables = dropTables.ToArray();
            var props = GenerateContent(materializedDropTables);
            return props;
        }

        private IProp[] GenerateContent(IDropTableScheme[] dropTables)
        {
            var modificators = new IDropTableModificatorScheme[0];
            var rolledRecords = new List<DropTableRecordSubScheme>();

            foreach (var table in dropTables)
            {
                var records = table.Records;
                var recMods = GetModRecords(records, modificators);

                var totalWeight = recMods.Sum(x => x.ModifiedWeight);

                for (var rollIndex = 0; rollIndex < table.Rolls; rollIndex++)
                {
                    var rolledWeight = _randomSource.RollWeight(totalWeight);
                    var recMod = DropRoller.GetRecord(recMods, rolledWeight);

                    if (recMod.Record.SchemeSid == null)
                        continue;

                    rolledRecords.Add(recMod.Record);
                }
            }

            var props = rolledRecords.Select(GenerateProp).ToArray();

            return props;
        }

        private DropTableModRecord[] GetModRecords(IEnumerable<DropTableRecordSubScheme> records,
            IEnumerable<IDropTableModificatorScheme> modificators)
        {
            var modificatorsArray = modificators.ToArray();

            var resultList = new List<DropTableModRecord>();
            foreach (var record in records)
            {
                if (record.SchemeSid == null)
                {
                    resultList.Add(new DropTableModRecord
                    {
                        Record = record,
                        ModifiedWeight = record.Weight
                    });
                    continue;
                }

                var recordModificators = modificatorsArray.Where(x => x.PropSids == null || x.PropSids.Contains(record.SchemeSid));
                var totalWeightMultiplier = recordModificators.Sum(x => x.WeightBonus) + 1;
                resultList.Add(new DropTableModRecord
                {
                    Record = record,
                    ModifiedWeight = (int)Math.Round(record.Weight * totalWeightMultiplier)
                });
            }

            return resultList.ToArray();
        }

        private IProp GenerateProp(DropTableRecordSubScheme record)
        {
            var scheme = _schemeService.GetScheme<IPropScheme>(record.SchemeSid);
            var propClass = GetPropClass(scheme);

            switch (propClass)
            {
                case PropClass.Equipment:
                    var power = _randomSource.RollEquipmentPower(record.MinPower, record.MaxPower);

                    var equipment = _propFactory.CreateEquipment(scheme);
                    return equipment;

                case PropClass.Resource:
                    var rolledCount = _randomSource.RollResourceCount(record.MinCount, record.MaxCount);
                    return new Resource(scheme, rolledCount);

                case PropClass.Concept:

                    var propScheme = _schemeService.GetScheme<IPropScheme>(record.Concept);

                    return new Concept(scheme, propScheme);

                default:
                    throw new ArgumentException($"Неизвестный класс {propClass} объекта {scheme}.");
            }
        }

        private static PropClass GetPropClass(IPropScheme scheme)
        {
            if (scheme.Equip != null)
                return PropClass.Equipment;

            if (scheme.Sid == "conceptual-scheme")
                return PropClass.Concept;

            return PropClass.Resource;
        }

        enum PropClass
        {
            Equipment = 1,
            Resource = 2,
            Concept = 3
        }
    }
}
