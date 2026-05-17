using System;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelButton : VisualElement
{
    public new class UxmlFactory : UxmlFactory<LevelButton, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        readonly UxmlStringAttributeDescription _mapType   = new() { name = "map-type",   defaultValue = "MAP"     };
        readonly UxmlStringAttributeDescription _levelName = new() { name = "level-name", defaultValue = "LEVEL"   };
        readonly UxmlStringAttributeDescription _number    = new() { name = "number",      defaultValue = "01"      };
        readonly UxmlStringAttributeDescription _tag       = new() { name = "tag",         defaultValue = "OUTDOOR" };

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var self = (LevelButton)ve;
            self.BuildIfNeeded();
            self.SetTexts(
                _mapType.GetValueFromBag(bag, cc),
                _levelName.GetValueFromBag(bag, cc),
                _number.GetValueFromBag(bag, cc),
                _tag.GetValueFromBag(bag, cc));
        }
    }

    VisualElement _root;      // LevelButtonRoot — the element with level-btn class
    VisualElement _accentLine;
    bool _built;
    bool _isActive;
    float _pulseStartTime;
    IVisualElementScheduledItem _pulseJob;

    public event Action<LevelButton> Clicked;

    public LevelButton() => BuildIfNeeded();

    public void BuildIfNeeded()
    {
        if (_built) return;

        var tpl = Resources.Load<VisualTreeAsset>("UI/LevelButton");
        if (tpl == null) { Debug.LogError("[LevelButton] Missing Resources/UI/LevelButton.uxml"); return; }
        Add(tpl.Instantiate());

        // LevelButtonRoot carries level-btn class and all CSS — target it for state changes
        _root       = this.Q<VisualElement>("LevelButtonRoot");
        _accentLine = this.Q<VisualElement>("accent-line");

        RegisterCallback<ClickEvent>(_ => Clicked?.Invoke(this));

        RegisterCallback<AttachToPanelEvent>(_ =>
        {
            _root       = this.Q<VisualElement>("LevelButtonRoot");
            _accentLine = this.Q<VisualElement>("accent-line");
            if (_isActive) StartPulse();
        });

        _built = true;
    }

    public void SetTexts(string mapType, string levelName, string number, string tag)
    {
        var tl   = this.Q<Label>("tl");
        var name = this.Q<Label>("name");
        var tr   = this.Q<Label>("tr");
        var br   = this.Q<Label>("br");
        if (tl   != null) tl.text   = mapType;
        if (name != null) name.text = levelName;
        if (tr   != null) tr.text   = number;
        if (br   != null) br.text   = tag;
    }

    public void SetActive(bool active)
    {
        if (_isActive == active) return;
        _isActive = active;

        if (_accentLine == null) _accentLine = this.Q<VisualElement>("accent-line");
        if (_root == null)       _root       = this.Q<VisualElement>("LevelButtonRoot");

        if (active)
        {
            _root?.AddToClassList("is-active");
            StartPulse();
        }
        else
        {
            _root?.RemoveFromClassList("is-active");
            StopPulse();
        }
    }

    void StartPulse()
    {
        StopPulse();
        _pulseStartTime = Time.realtimeSinceStartup;
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
