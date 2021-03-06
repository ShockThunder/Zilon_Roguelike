﻿using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Zilon.Core.Components;
using Zilon.Core.PersonModules;
using Zilon.Core.Persons;
using Zilon.Core.Tactics;

public class HumanActorGraphicController : MonoBehaviour
{
    private Dictionary<int, VisualPropHolder> _visualSlots;

    public IActor Actor { get; set; }
    public ActorGraphicBase Graphic;

    public void Start()
    {
        _visualSlots = new Dictionary<int, VisualPropHolder>();

        ProjectSlotsToVisual();

        UpdateEquipment();

        Actor.Person.GetModule<IEquipmentModule>().EquipmentChanged += EquipmentCarrierOnEquipmentChanged;
    }

    public void OnDestroy()
    {
        Actor.Person.GetModule<IEquipmentModule>().EquipmentChanged -= EquipmentCarrierOnEquipmentChanged;
    }

    private void ProjectSlotsToVisual()
    {
        var humanHumanoidGraphic = (HumanoidActorGraphic)Graphic;

        var visualHolderList = humanHumanoidGraphic.VisualHolders.ToList();

        var equipmentCarrier = Actor.Person.GetModule<IEquipmentModule>();
        for (var slotIndex = 0; slotIndex < equipmentCarrier.Slots.Length; slotIndex++)
        {
            var slot = equipmentCarrier.Slots[slotIndex];
            var visualHolder = visualHolderList.FirstOrDefault(x => (x.SlotTypes & slot.Types) > 0);
            if (visualHolder != null)
            {
                _visualSlots[slotIndex] = visualHolder;
                visualHolderList.Remove(visualHolder);
            }
        }
    }

    private void EquipmentCarrierOnEquipmentChanged(object sender, EquipmentChangedEventArgs e)
    {
        UpdateEquipment();
    }

    private void UpdateEquipment()
    {
        var equipmentCarrier = Actor.Person.GetModule<IEquipmentModule>();
        for (var slotIndex = 0; slotIndex < equipmentCarrier.Slots.Length; slotIndex++)
        {
            if (_visualSlots.TryGetValue(slotIndex, out VisualPropHolder holder))
            {
                foreach (Transform visualProp in holder.transform)
                {
                    Destroy(visualProp.gameObject);
                }

                VisualProp visualPropResource = null;
                var equipment = equipmentCarrier[slotIndex];
                if (equipment != null)
                {
                    var schemeSid = equipment.Scheme.Sid;
                    if (equipment.Scheme.IsMimicFor != null)
                    {
                        schemeSid = equipment.Scheme.IsMimicFor;
                    }

                    visualPropResource = Resources.Load<VisualProp>($"VisualProps/{schemeSid}");
                }

                if (visualPropResource != null)
                {
                    Instantiate(visualPropResource, holder.transform);
                }
                else
                {
                    if ((holder.SlotTypes & EquipmentSlotTypes.Body) > 0)
                    {
                        var noneArmor = Resources.Load<VisualProp>($"VisualProps/none-armor");
                        Instantiate(noneArmor, holder.transform);
                    }
                    else if ((holder.SlotTypes & EquipmentSlotTypes.Head) > 0)
                    {
                        var noneHead = Resources.Load<VisualProp>($"VisualProps/none-head");
                        Instantiate(noneHead, holder.transform);
                    }
                }
            }
        }
    }
}
