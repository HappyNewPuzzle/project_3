using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class IdleGuildEquipmentItem
{
    public string id;
    public string name;
    public string slot;
    public string rarity;
    public int tier;
    public int attackBonus;
    public int speedBonus;
    public int criticalBonus;
    public bool equipped;
    public int Score => attackBonus * 10 + speedBonus * 6 + criticalBonus * 8 + tier * 15;
}

[Serializable]
public sealed class IdleGuildEquipmentCollection
{
    public List<IdleGuildEquipmentItem> items = new List<IdleGuildEquipmentItem>();
}

public sealed class IdleGuildEquipmentInventory
{
    private const string SaveKey = "IdleGuild.Equipment.InventoryV2";
    private readonly IdleGuildEquipmentCollection data;
    public IReadOnlyList<IdleGuildEquipmentItem> Items => data.items;
    public int Count => data.items.Count;
    public IdleGuildEquipmentItem Equipped => data.items.Find(item => item.equipped);

    public IdleGuildEquipmentInventory()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        data = string.IsNullOrEmpty(json) ? new IdleGuildEquipmentCollection() :
            JsonUtility.FromJson<IdleGuildEquipmentCollection>(json) ?? new IdleGuildEquipmentCollection();
    }

    public IdleGuildEquipmentItem Drop(int stage, bool boss)
    {
        int tier = Mathf.Max(1, stage / 3 + UnityEngine.Random.Range(0, boss ? 3 : 2));
        float roll = UnityEngine.Random.value + (boss ? 0.22f : 0f);
        string rarity = roll > 1.05f ? "Legendary" : roll > 0.82f ? "Epic" : roll > 0.55f ? "Rare" : "Common";
        int rarityPower = rarity == "Legendary" ? 4 : rarity == "Epic" ? 3 : rarity == "Rare" ? 2 : 1;
        string[] slots = { "Weapon", "Armor", "Accessory" };
        string slot = slots[UnityEngine.Random.Range(0, slots.Length)];
        var item = new IdleGuildEquipmentItem
        {
            id = Guid.NewGuid().ToString("N"), name = rarity + " " + slot + " +" + tier,
            slot = slot, rarity = rarity, tier = tier,
            attackBonus = tier * rarityPower + (slot == "Weapon" ? tier * 2 : 0),
            speedBonus = slot == "Accessory" ? Mathf.Max(1, rarityPower - 1) : 0,
            criticalBonus = slot == "Armor" ? Mathf.Max(1, rarityPower - 1) : 0
        };
        data.items.Add(item);
        SortByPower();
        Save();
        return item;
    }

    public bool AutoEquipBest()
    {
        if (data.items.Count == 0) return false;
        foreach (var item in data.items) item.equipped = false;
        SortByPower();
        var equippedSlots = new HashSet<string>();
        foreach (var item in data.items)
        {
            if (equippedSlots.Add(item.slot)) item.equipped = true;
        }
        Save();
        return true;
    }

    public int SellUnequipped(bool dismantle)
    {
        int value = 0;
        for (int index = data.items.Count - 1; index >= 0; index--)
        {
            if (data.items[index].equipped) continue;
            value += dismantle ? Mathf.Max(1, data.items[index].tier) : 15 + data.items[index].tier * 12;
            data.items.RemoveAt(index);
        }
        Save();
        return value;
    }

    public void SortByPower() => data.items.Sort((left, right) => right.Score.CompareTo(left.Score));

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
