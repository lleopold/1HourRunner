using System;
using System.Collections.Generic;

/// <summary>
/// Plain serializable save model. Serialized to JSON via Unity's JsonUtility.
/// NOTE: JsonUtility cannot serialize dictionaries, so stat allocations are
/// stored as List&lt;StatPoint&gt; (key -&gt; clicks). Enums serialize as ints.
/// The ScriptableObject configs remain the read-only "naked defaults"; this
/// save holds only the progression deltas on top of them.
/// </summary>
[Serializable]
public class SaveData
{
    public int version = 1;
    public int slotIndex = -1;          // -1 = not bound to a slot yet (prevents stray writes)
    public string lastPlayedIso = "";   // ISO 8601 timestamp; drives Continue = most recent
    public int globalXP;

    public PlayerEnum chosenPlayer = PlayerEnum.GreenHat_basic;
    public WeaponEnum chosenWeapon = WeaponEnum.WPN_AP85;

    public List<CharacterProgress> characters = new();
    public List<WeaponProgress> weapons = new();

    public CharacterProgress GetOrCreateCharacter(PlayerEnum id)
    {
        foreach (var c in characters)
            if (c.id == id) return c;
        var created = new CharacterProgress { id = id };
        characters.Add(created);
        return created;
    }

    public WeaponProgress GetOrCreateWeapon(WeaponEnum id)
    {
        foreach (var w in weapons)
            if (w.id == id) return w;
        var created = new WeaponProgress { id = id };
        weapons.Add(created);
        return created;
    }
}

/// <summary>Shared surface so Progression can treat characters and weapons uniformly.</summary>
public interface IProgressItem
{
    List<StatPoint> Stats { get; }
    int XpSpent { get; set; }
}

[Serializable]
public class CharacterProgress : IProgressItem
{
    public PlayerEnum id;
    public int level = 1;
    public int xpSpent;                 // total XP spent on this character (for refund math)
    public List<StatPoint> stats = new();

    public List<StatPoint> Stats => stats;
    public int XpSpent { get => xpSpent; set => xpSpent = value; }
}

[Serializable]
public class WeaponProgress : IProgressItem
{
    public WeaponEnum id;
    public int xpSpent;                 // total XP spent on this weapon (for refund math)
    public List<StatPoint> stats = new();

    public List<StatPoint> Stats => stats;
    public int XpSpent { get => xpSpent; set => xpSpent = value; }
}

[Serializable]
public struct StatPoint
{
    public string key;
    public int clicks;
}
