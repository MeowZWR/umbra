﻿using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets2;
using Umbra.Common;

namespace Umbra.Game;

public readonly struct EquipmentSlot(string itemName, uint iconId, byte slotId, byte durability, byte spiritbond)
{
    public readonly bool   IsEmpty     => ItemName == "";
    public readonly string ItemName   = itemName;
    public readonly uint   IconId     = iconId;
    public readonly byte   SlotId     = slotId;
    public readonly byte   Durability = durability;
    public readonly byte   Spiritbond = spiritbond;
}

[Service]
internal unsafe class EquipmentRepository : IEquipmentRepository
{
    /// <summary>
    /// Returns a list of the player's currently equipped gear.
    /// </summary>
    public List<EquipmentSlot> Slots { get; } = [];

    /// <summary>
    /// Returns the percentage of the lowest durability item of the player's
    /// currently equipped gear.
    /// </summary>
    public byte LowestDurability { get; private set; }

    /// <summary>
    /// Returns the percentage of the highest spiritbond item of the player's
    /// currently equipped gear.
    /// </summary>
    public byte HighestSpiritbond { get; private set; }

    private readonly IDataManager        _dataManager;
    private readonly InventoryManager*   _inventoryManager;
    private readonly InventoryContainer* _equipmentContainer;

    public EquipmentRepository(IDataManager dataManager)
    {
        _dataManager            = dataManager;
        _inventoryManager       = InventoryManager.Instance();
        _equipmentContainer     = _inventoryManager->GetInventoryContainer(InventoryType.EquippedItems);

        for (var slot = 0; slot < 13; slot++) {
            Slots.Add(new("", 0, 0, 0, 0));
        }

        Update();
    }

    [OnTick(interval: 1000)]
    public void Update()
    {
        ushort lowestDurability  = ushort.MaxValue;
        ushort highestSpiritbond = 0;

        for (var slot = 0; slot < 13; slot++) {
            InventoryItem* equipment = _equipmentContainer->GetInventorySlot(slot);

            if (equipment == null || equipment->ItemId == 0) {
                Slots[slot] = new("", 0, 0, 0, 0);
                continue;
            }

            lowestDurability  = Math.Min(lowestDurability, equipment->Condition);
            highestSpiritbond = Math.Max(highestSpiritbond, equipment->Spiritbond);

            var item = _dataManager.GetExcelSheet<Item>()!.GetRow(equipment->ItemId)!;

            Slots[slot] = new(
                item.Name.ToDalamudString().ToString(),
                item.Icon,
                (byte)slot,
                (byte)(equipment->Condition / 300f),
                (byte)(equipment->Spiritbond / 100f)
            );
        }

        LowestDurability  = (byte)(lowestDurability / 300f);
        HighestSpiritbond = (byte)(highestSpiritbond / 100f);
    }
}
