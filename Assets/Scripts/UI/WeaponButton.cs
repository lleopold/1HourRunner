using System;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Put this in Assets/Scripts/UI/WeaponButton.cs
public class WeaponButton : VisualElement
{
    public new class UxmlFactory : UxmlFactory<WeaponButton, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlStringAttributeDescription _slotType = new() { name = "slot-type", defaultValue = "PRIMARY" };
        UxmlStringAttributeDescription _weaponName = new() { name = "weapon-name", defaultValue = "XM4" };
        UxmlStringAttributeDescription _topRight = new() { name = "top-right", defaultValue = "GUNSMITH" };
        UxmlStringAttributeDescription _bottomRight = new() { name = "bottom-right", defaultValue = "LVL 1" };
        UxmlStringAttributeDescription _imagePath = new() { name = "sprite-path", defaultValue = "" }; // Resources path, optional

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var self = (WeaponButton)ve;

            var slotType = _slotType.GetValueFromBag(bag, cc);
            var weaponName = _weaponName.GetValueFromBag(bag, cc);
            var topRight = _topRight.GetValueFromBag(bag, cc);
            var bottomRight = _bottomRight.GetValueFromBag(bag, cc);
            var imagePath = _imagePath.GetValueFromBag(bag, cc);

            self.BuildIfNeeded();
            self.SetTexts(slotType, weaponName, topRight, bottomRight);

            if (!string.IsNullOrEmpty(imagePath))
            {
                var tex = Resources.Load<Texture2D>(imagePath);
                if (tex != null) self.SetImage(tex);
            }
        }
    }

    // element names (match UXML)
    const string RootClass = "weapon-btn";
    const string ImgName = "img";
    const string TLName = "tl";
    const string NameName = "name";
    const string TRName = "tr";
    const string BRName = "br";

    VisualElement _img;
    Label _tl, _name, _tr, _br;
    VisualElement _accentLine;

    bool _built;
    bool _isActive;
    bool _locked;
    float _pulseStartTime;
    IVisualElementScheduledItem _pulseJob;
    VisualElement _lockedOverlay;

    public bool IsLocked => _locked;
    public event Action<WeaponButton> Clicked;

    public WeaponButton() => BuildIfNeeded();

    public void BuildIfNeeded()
    {
        if (_built) return;

        var tpl = Resources.Load<VisualTreeAsset>("UI/WeaponButton");
        if (tpl == null) { Debug.LogError("Missing Resources/UI/WeaponButton.uxml"); return; }
        Add(tpl.Instantiate());

        var style = Resources.Load<StyleSheet>("UI/WeaponButton");
        if (style != null) styleSheets.Add(style);

        _img = this.Q<VisualElement>(ImgName);
        _tl = this.Q<Label>(TLName);
        _name = this.Q<Label>(NameName);
        _tr = this.Q<Label>(TRName);
        _br = this.Q<Label>(BRName);
        _accentLine = this.Q<VisualElement>("accent-line");

        AddToClassList(RootClass);

        // Click/hover states
        this.RegisterCallback<PointerEnterEvent>(_ => AddToClassList("hover"));
        this.RegisterCallback<PointerLeaveEvent>(_ => RemoveFromClassList("hover"));
        this.RegisterCallback<ClickEvent>(_ => { if (!_locked) Clicked?.Invoke(this); });

        // When attached to panel, restart pulse if already marked active (handles early SetActive calls)
        this.RegisterCallback<AttachToPanelEvent>(_ =>
        {
            _accentLine = this.Q<VisualElement>("accent-line");
            if (_isActive) StartPulse();
        });

        _built = true;
    }

    // Public API
    public void SetImage(Texture2D tex) =>
        _img.style.backgroundImage = new StyleBackground(tex);

    public void SetTexts(string slotType, string weaponName, string topRight, string bottomRight)
    {
        _tl.text = slotType;
        _name.text = weaponName;
        _tr.text = "";// topRight;
        _br.text = bottomRight;
    }

    // Convenience for one-shot setup
    public void SetData(Texture2D tex, string slotType, string weaponName, string topRight, string bottomRight)
    {
        if (tex) SetImage(tex);
        SetTexts(slotType, weaponName, topRight, bottomRight);
    }

    public void SetActive(bool active)
    {
        if (_isActive == active) return;
        _isActive = active;

        if (_accentLine == null)
            _accentLine = this.Q<VisualElement>("accent-line");

        if (active)
        {
            AddToClassList("is-active");
            RemoveFromClassList("selected");
            StartPulse();
        }
        else
        {
            RemoveFromClassList("is-active");
            StopPulse();
        }
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (locked)
        {
            if (_lockedOverlay == null)
            {
                _lockedOverlay = new VisualElement { name = "locked-overlay" };
                _lockedOverlay.style.position = Position.Absolute;
                _lockedOverlay.style.left = 0; _lockedOverlay.style.top = 0;
                _lockedOverlay.style.right = 0; _lockedOverlay.style.bottom = 0;
                _lockedOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.62f);
                _lockedOverlay.style.alignItems = Align.Center;
                _lockedOverlay.style.justifyContent = Justify.Center;
                _lockedOverlay.pickingMode = PickingMode.Ignore;

                var lbl = new Label("LOCKED");
                lbl.pickingMode = PickingMode.Ignore;
                lbl.style.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                lbl.style.fontSize = 10;
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                lbl.style.letterSpacing = 3f;
                _lockedOverlay.Add(lbl);
                Add(_lockedOverlay);
            }
            _lockedOverlay.style.display = DisplayStyle.Flex;
            style.opacity = 0.45f;
        }
        else
        {
            if (_lockedOverlay != null)
                _lockedOverlay.style.display = DisplayStyle.None;
            style.opacity = 1f;
        }
    }

    // External tick no longer needed — pulse is self-driven via schedule
    public void Tick(float deltaTime) { }

    void StartPulse()
    {
        StopPulse();
        _pulseStartTime = Time.realtimeSinceStartup;
        // schedule fires every 16ms independently of MonoBehaviour
        _pulseJob = schedule.Execute(() =>
        {
            if (_accentLine == null) return;
            float t = Time.realtimeSinceStartup - _pulseStartTime;
            float opacity = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(t * 3.5f));
            _accentLine.style.opacity = opacity;
        }).Every(16).StartingIn(0);
    }

    void StopPulse()
    {
        _pulseJob?.Pause();
        _pulseJob = null;
        if (_accentLine != null)
            _accentLine.style.opacity = 0f;
    }
}
