using System;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON save system. 3 slots, unencrypted, stored under
/// Application.persistentDataPath/saves/slotN.json. Replaces the old
/// c:\Temp config writers. The currently loaded slot lives in a static field
/// that survives scene loads (like DataHolder).
/// </summary>
public static class SaveManager
{
    public const int SLOT_COUNT = 3;

    private static SaveData _current;

    /// <summary>The active save. Auto-creates an in-memory default if nothing loaded yet.</summary>
    public static SaveData Current
    {
        get
        {
            if (_current == null)
                _current = new SaveData();
            return _current;
        }
    }

    public static bool HasLoadedSave => _current != null && _current.slotIndex >= 0 && Exists(_current.slotIndex);

    // Reset static state when entering Play mode so a fresh session starts clean.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic() => _current = null;

    private static string Dir => Path.Combine(Application.persistentDataPath, "saves");
    private static string PathFor(int slot) => Path.Combine(Dir, $"slot{slot}.json");

    public static bool Exists(int slot) => File.Exists(PathFor(slot));

    public static bool HasAnySave()
    {
        for (int i = 0; i < SLOT_COUNT; i++)
            if (Exists(i)) return true;
        return false;
    }

    /// <summary>Create a fresh save in the given slot, make it current, and persist it.</summary>
    public static SaveData NewGame(int slot)
    {
        _current = new SaveData { slotIndex = slot };
        ApplyToDataHolder(_current);
        Save();
        return _current;
    }

    /// <summary>Load a slot from disk and make it current. Returns null if the file is missing/corrupt.</summary>
    public static SaveData Load(int slot)
    {
        if (!Exists(slot))
        {
            Debug.LogWarning($"[SaveManager] No save in slot {slot}.");
            return null;
        }
        try
        {
            string json = File.ReadAllText(PathFor(slot));
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) { Debug.LogError($"[SaveManager] Slot {slot} parsed to null."); return null; }
            data.slotIndex = slot;
            _current = data;
            ApplyToDataHolder(_current);
            return _current;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to load slot {slot}: {e.Message}");
            return null;
        }
    }

    /// <summary>Persist the current save to its own slot.</summary>
    public static void Save()
    {
        var save = Current;
        if (save.slotIndex < 0 || save.slotIndex >= SLOT_COUNT)
        {
            Debug.LogWarning($"[SaveManager] Current save has invalid slot {save.slotIndex}; not writing.");
            return;
        }
        Save(save.slotIndex);
    }

    public static void Save(int slot)
    {
        var save = Current;
        save.slotIndex = slot;
        // Capture the live selection so Continue/Load restore the right character & weapon.
        save.chosenPlayer = DataHolder.ChosenPlayer;
        save.chosenWeapon = DataHolder.chosenWeapon;
        save.lastPlayedIso = DateTime.UtcNow.ToString("o");

        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(PathFor(slot), JsonUtility.ToJson(save, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to save slot {slot}: {e.Message}");
        }
    }

    public static void Delete(int slot)
    {
        try
        {
            if (Exists(slot)) File.Delete(PathFor(slot));
            if (_current != null && _current.slotIndex == slot) _current = null;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to delete slot {slot}: {e.Message}");
        }
    }

    public static SlotInfo[] ListSlots()
    {
        var infos = new SlotInfo[SLOT_COUNT];
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            var info = new SlotInfo { slot = i, exists = Exists(i) };
            if (info.exists)
            {
                var data = ReadHeader(i);
                if (data != null)
                {
                    info.player = data.chosenPlayer;
                    info.globalXP = data.globalXP;
                    info.lastPlayedIso = data.lastPlayedIso;
                }
            }
            infos[i] = info;
        }
        return infos;
    }

    /// <summary>Slot with the newest lastPlayed timestamp, or -1 if none exist.</summary>
    public static int MostRecentSlot()
    {
        int best = -1;
        DateTime bestTime = DateTime.MinValue;
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (!Exists(i)) continue;
            var data = ReadHeader(i);
            if (data == null) continue;
            if (DateTime.TryParse(data.lastPlayedIso, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var t))
            {
                if (t >= bestTime) { bestTime = t; best = i; }
            }
            else if (best < 0)
            {
                best = i; // fallback if timestamp unparseable
            }
        }
        return best;
    }

    private static SaveData ReadHeader(int slot)
    {
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor(slot))); }
        catch { return null; }
    }

    private static void ApplyToDataHolder(SaveData data)
    {
        DataHolder.ChosenPlayer = data.chosenPlayer;
        DataHolder.chosenWeapon = data.chosenWeapon;
    }
}

public struct SlotInfo
{
    public int slot;
    public bool exists;
    public PlayerEnum player;
    public int globalXP;
    public string lastPlayedIso;
}
