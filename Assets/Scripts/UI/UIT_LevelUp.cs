// File: Assets/Scripts/UI/UIT_LevelUp.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIT_LevelUp : MonoBehaviour
{
    // ----- Types -----
    public enum PowerupType { Health, Reload, Running, ClipSize }
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    public struct PowerupOffer
    {
        public PowerupType type;
        public Rarity rarity;
        public string title;
        public string description;
        public string iconPath; // Resources path to a Sprite
    }

    // ----- UI refs -----
    [HideInInspector] public VisualElement _root; // set in Awake
    Button _btn1, _btn2, _btn3;
    VisualElement _icon1, _icon2, _icon3;
    Label _lab1, _lab2, _lab3;
    Label _desc1, _desc2, _desc3;

    // Current offers
    PowerupOffer _o1, _o2, _o3;

    void Awake()
    {
        // Hook root and elements
        var doc = GetComponent<UIDocument>();
        _root = doc.rootVisualElement;

        _btn1 = _root.Q<Button>("btn_pwr1");
        _btn2 = _root.Q<Button>("btn_pwr2");
        _btn3 = _root.Q<Button>("btn_pwr3");

        _icon1 = _root.Q<VisualElement>("icon_pwr1");
        _icon2 = _root.Q<VisualElement>("icon_pwr2");
        _icon3 = _root.Q<VisualElement>("icon_pwr3");

        _lab1 = _root.Q<Label>("lab_pwr1");
        _lab2 = _root.Q<Label>("lab_pwr2");
        _lab3 = _root.Q<Label>("lab_pwr3");

        _desc1 = _btn1.Q<Label>(className: "choice__desc");
        _desc2 = _btn2.Q<Label>(className: "choice__desc");
        _desc3 = _btn3.Q<Label>(className: "choice__desc");

        // Clicks
        //_btn1?.RegisterCallback<ClickEvent>(_ => OnPick(_o1));
        //_btn2?.RegisterCallback<ClickEvent>(_ => OnPick(_o2));
        //_btn3?.RegisterCallback<ClickEvent>(_ => OnPick(_o3));

        _btn1.clicked += () => OnPick(_o1);
        _btn2.clicked += () => OnPick(_o2);
        _btn3.clicked += () => OnPick(_o3);

        // Start hidden by default
        _root.visible = false;
        _root.SetEnabled(false);
        enabled = false;
    }

    // ========== Public API ==========
    // Call this before showing the panel.
    public void SetOffers(List<PowerupOffer> offers)
    {
        // Expect exactly 3
        if (offers == null || offers.Count < 3) return;
        _o1 = offers[0];
        _o2 = offers[1];
        _o3 = offers[2];

        // Roll rarity if caller didn’t set
        if (!HasRarity(_o1)) _o1.rarity = RollRarity();
        if (!HasRarity(_o2)) _o2.rarity = RollRarity();
        if (!HasRarity(_o3)) _o3.rarity = RollRarity();

        // Fill UI
        SetCard(_icon1, _lab1, _desc1, _o1);
        SetCard(_icon2, _lab2, _desc2, _o2);
        SetCard(_icon3, _lab3, _desc3, _o3);

        // Show
        _root.visible = true;
        _root.SetEnabled(true);
        enabled = true;
        _root.schedule.Execute(() => _btn1.Focus()); // focus first button
    }

    // Optional helper to generate standard 3 offers (Health/Reload/Running).
    public List<PowerupOffer> GenerateDefaultOffers()
    {
        return new List<PowerupOffer>
        {
            MakeOffer(PowerupType.Health,   "More Health",   "+25% max HP",       "PowerUp/Icons/more_health"),
            MakeOffer(PowerupType.Reload,   "Faster Reload", "-20% reload time",  "PowerUp/Icons/faster_reload"),
            MakeOffer(PowerupType.Running,  "Faster Running","+15% move speed",   "PowerUp/Icons/faster_run"),
        };
    }

    // ========== Internals ==========
    static bool HasRarity(PowerupOffer o) => o.rarity >= 0;

    static PowerupOffer MakeOffer(PowerupType t, string title, string desc, string iconPath)
    {
        return new PowerupOffer { type = t, rarity = (Rarity)(-1), title = title, description = desc, iconPath = iconPath };
    }

    void SetCard(VisualElement icon, Label title, Label desc, PowerupOffer o)
    {
        // Title/desc
        if (title != null) title.text = o.title;
        if (desc != null) desc.text = o.description;

        // Icon sprite
        if (icon != null)
        {
            // clear previous ring classes
            ClearRing(icon);
            AddRing(icon, o.rarity);

            var sprite = Resources.Load<Sprite>(o.iconPath);
            if (sprite != null) icon.style.backgroundImage = Background.FromSprite(sprite);
        }
    }

    // Rarity roll: 50/30/10/9/1 (common..legendary)
    public static Rarity RollRarity()
    {
        float r = Random.value;
        if (r < 0.50f) return Rarity.Common;
        if (r < 0.80f) return Rarity.Uncommon;
        if (r < 0.90f) return Rarity.Rare;
        if (r < 0.99f) return Rarity.Epic;
        return Rarity.Legendary;
    }

    void ClearRing(VisualElement ve)
    {
        ve.RemoveFromClassList("ring-common");
        ve.RemoveFromClassList("ring-uncommon");
        ve.RemoveFromClassList("ring-rare");
        ve.RemoveFromClassList("ring-epic");
        ve.RemoveFromClassList("ring-legendary");
    }
    void AddRing(VisualElement ve, Rarity r)
    {
        switch (r)
        {
            case Rarity.Common: ve.AddToClassList("ring-common"); break;
            case Rarity.Uncommon: ve.AddToClassList("ring-uncommon"); break;
            case Rarity.Rare: ve.AddToClassList("ring-rare"); break;
            case Rarity.Epic: ve.AddToClassList("ring-epic"); break;
            case Rarity.Legendary: ve.AddToClassList("ring-legendary"); break;
        }
    }

    void OnPick(PowerupOffer offer)
    {
        ApplyPowerup(offer);
        // Hide panel and resume
        _root.visible = false;
        _root.SetEnabled(false);
        enabled = false;

        // If you pause via a central manager, call it here.
        // Example: FindObjectOfType<Player>()?.PauseGame(false);
        Time.timeScale = 1f; // fallback resume
    }

    void ApplyPowerup(PowerupOffer offer)
    {
        var cfg = PlayerConfigSingleton.Instance.PlayerConfig;

        // Rarity multipliers (tweak to taste)
        float mul = offer.rarity switch
        {
            Rarity.Common => 1.00f,
            Rarity.Uncommon => 1.20f,
            Rarity.Rare => 1.40f,
            Rarity.Epic => 1.70f,
            Rarity.Legendary => 2.00f,
            _ => 1f
        };

        switch (offer.type)
        {
            case PowerupType.Health:
                cfg.health += 25f * mul;                // additive
                break;

            case PowerupType.Reload:
                cfg.reloadSpeed *= Mathf.Max(0.1f, 1f - 0.20f * mul); // faster = lower time
                break;

            case PowerupType.Running:
                cfg.speed *= 1f + 0.15f * mul;          // multiplicative bonus
                break;

            case PowerupType.ClipSize:
                // Ensure you have such a stat; example only:
                // cfg.maxAmmo += Mathf.RoundToInt(10 * mul);
                break;
        }

        PlayerConfigSingleton.Instance.SaveConfigToFile();
    }
}
