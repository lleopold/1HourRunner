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

    bool _built;

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

        AddToClassList(RootClass);

        // Click/hover states
        this.RegisterCallback<PointerEnterEvent>(_ => AddToClassList("hover"));
        this.RegisterCallback<PointerLeaveEvent>(_ => RemoveFromClassList("hover"));
        this.RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(this));

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
}
