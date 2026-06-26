using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  HookNode: Graph node for a delegate slot (OnStart, OnDone, Signal, Kick) on an effect.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class HookNode : Node
    {
        public override VisualElement contentContainer => this;

        public static float W => FilmStripElement.TopPerfW - 6f;
        public static float H => FilmStripElement.PerfH - 6f;

        public static readonly string[] SlotNames =
            { "OnStart", "OnDone", "ModifyEffectState", "SetStartingVelocity" };

        private static readonly string[] SlotTooltips =
        {
            "Called once when this effect begins",
            "Called once when this effect completes",
            "Override the way this effect runs from code. Can Pause, Stop the effect from ending, terminate it early, abort it (without triggering OnDone), or restart it",
            "Sets the starting velocity when this effect begins. Receives the current velocity from the previous effect"
        };

        public IEffect SourceEffect { get; private set; }
        public int SlotIndex { get; private set; } = -1;
        public string SlotName =>
            SlotIndex >= 0 && SlotIndex < SlotNames.Length ? SlotNames[SlotIndex] : "";

        private DelegateMode _localMode;
        private UnityEngine.Object _localObj;
        private string _localCls = "";
        private string _localMethod = "";
        private string _localRelDesc = "";

        public Port InputPort { get; private set; }
        public Port ValueOutputPort { get; private set; }

        private VisualElement _badge;
        private Label _slotLbl;
        private Label _methodLbl;

        private readonly JbTheme _theme;
        private readonly SequenceGraphView _graphView;

        public HookNode(IEffect effect, int slotIndex,
            JbTheme theme = default, SequenceGraphView graphView = null)
        {
            SourceEffect = effect;
            SlotIndex = slotIndex;
            _theme = theme.SubnodeBg == default ? JbTheme.Default : theme;
            _graphView = graphView;

            capabilities = Capabilities.Selectable | Capabilities.Movable;
            style.width = W;
            style.height = H;
            style.borderTopLeftRadius = style.borderTopRightRadius =
            style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;
            style.backgroundColor = _theme.SubnodeBg;
            style.overflow = Overflow.Hidden;
            FilmStripElement.SetBorder(this, _theme.SubnodeBorder, 1f);

            titleContainer.style.display = DisplayStyle.None;
            inputContainer.style.display = DisplayStyle.None;
            outputContainer.style.display = DisplayStyle.None;

            if (effect != null && slotIndex >= 0)
                LoadFromEffect(effect, SlotNames[slotIndex]);

            BuildContent();
        }

        private void BuildContent()
        {
            _slotLbl = new Label(SlotIndex >= 0 ? SlotNames[SlotIndex] : "Hook")
            { pickingMode = PickingMode.Ignore };
            _slotLbl.style.position = Position.Absolute;
            _slotLbl.style.left = 5f;
            _slotLbl.style.top = 18f;
            _slotLbl.style.fontSize = 14f;
            _slotLbl.style.color = _theme.PortAction;
            if (SlotIndex >= 0 && SlotIndex < SlotTooltips.Length)
                _slotLbl.tooltip = SlotTooltips[SlotIndex];
            Add(_slotLbl);

            _badge = new VisualElement();
            _badge.tooltip = "Click to configure the delegate for this hook";
            _badge.style.position = Position.Absolute;
            _badge.style.left = 3f;
            _badge.style.top = 38f;
            _badge.style.width = W - 6f;
            _badge.style.height = 18f;
            _badge.style.flexDirection = FlexDirection.Row;
            _badge.style.alignItems = Align.Center;
            _badge.style.paddingLeft = _badge.style.paddingRight = 3f;
            _badge.style.borderTopLeftRadius = _badge.style.borderTopRightRadius =
            _badge.style.borderBottomLeftRadius = _badge.style.borderBottomRightRadius = 2;
            RefreshBadgeBg();

            _methodLbl = new Label(BuildMethodText()) { pickingMode = PickingMode.Ignore };
            _methodLbl.style.fontSize = 14f;
            _methodLbl.style.color = IsLiveDelegateBound()
                    ? _theme.FieldVal : _theme.FieldMissVal;
            _methodLbl.style.flexGrow = 1f;
            _methodLbl.style.overflow = Overflow.Hidden;
            _badge.Add(_methodLbl);

            _badge.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0 || SourceEffect == null || SlotIndex < 0) return;
                evt.StopPropagation();
                var root = _graphView as VisualElement;
                if (root == null) return;
                Vector2 pos = root.WorldToLocal(evt.originalMousePosition);
                System.Type vt = GetValueType(SourceEffect);
                DelegatePicker.Show(root, SourceEffect, SlotName, vt, pos, _theme, () =>
                {
                    LoadFromEffect(SourceEffect, SlotName);
                    RefreshDisplay();
                    _graphView?.FindEffectNodeForEffect(SourceEffect)
                               ?.RefreshHookBadge(SlotIndex);
                    _graphView?.PropagateEffectSlotEdit(SourceEffect, SlotName);
                },
                () =>
                {
                    _graphView?.RemoveHookNode(this);
                },
                _graphView?.TargetGameObject,
                SourceEffect.ReadValueSlot(SlotName));
            });

            DelegatePicker.RegisterDragToPick(_badge, _theme, (droppedGo, mousePos) =>
            {
                if (SourceEffect == null || SlotIndex < 0) return;
                var root = _graphView as VisualElement;
                if (root == null) return;
                Vector2 pos = root.WorldToLocal(mousePos);
                System.Type vt = GetValueType(SourceEffect);
                DelegatePicker.Show(root, SourceEffect, SlotName, vt, pos, _theme, () =>
                {
                    LoadFromEffect(SourceEffect, SlotName);
                    RefreshDisplay();
                    _graphView?.FindEffectNodeForEffect(SourceEffect)
                               ?.RefreshHookBadge(SlotIndex);
                    _graphView?.PropagateEffectSlotEdit(SourceEffect, SlotName);
                },
                () =>
                {
                    _graphView?.RemoveHookNode(this);
                },
                _graphView?.TargetGameObject,
                SourceEffect.ReadValueSlot(SlotName),
                prePickedTarget: droppedGo);
            });
            Add(_badge);

            InputPort = Port.Create<JbChannelEdge>(
                Orientation.Horizontal, Direction.Input, Port.Capacity.Single,
                typeof(object));
            InputPort.portName = "";
            InputPort.portColor = _theme.PortAction;
            InputPort.style.position = Position.Absolute;
            InputPort.style.top = -4f;
            CenterPortHorizontally(InputPort);
            Add(InputPort);

            var closeBtn = new Button(() => _graphView?.RemoveHookNode(this))
            { text = "\u00d7" };
            closeBtn.style.position = Position.Absolute;
            closeBtn.style.left = W - 28f;
            closeBtn.style.top = -3f;
            closeBtn.style.width = 24f;
            closeBtn.style.height = 24f;
            closeBtn.style.fontSize = 18f;
            closeBtn.style.paddingLeft = closeBtn.style.paddingRight = 0f;
            closeBtn.style.paddingTop = closeBtn.style.paddingBottom = 0f;
            closeBtn.style.backgroundColor = Color.clear;
            closeBtn.style.borderTopWidth = closeBtn.style.borderBottomWidth =
            closeBtn.style.borderLeftWidth = closeBtn.style.borderRightWidth = 0f;
            closeBtn.style.color = _theme.NodeBorder;
            closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeBtn.RegisterCallback<MouseEnterEvent>(_ => closeBtn.style.color = _theme.SlotHi);
            closeBtn.RegisterCallback<MouseLeaveEvent>(_ => closeBtn.style.color = _theme.NodeBorder);
            Add(closeBtn);
        }

        private void CenterPortHorizontally(Port port)
        {
            port.style.left = W * 0.5f - 7f;

            port.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var connector = port.Q("connector");
                if (connector == null || connector.layout.width <= 0f) return;
                float connectorCentreInPort =
                    connector.layout.x + connector.layout.width * 0.5f;
                float targetLeft = W * 0.5f - connectorCentreInPort;
                float currentLeft = port.resolvedStyle.left;
                if (Mathf.Abs(currentLeft - targetLeft) > 0.5f)
                    port.style.left = targetLeft;
            });
        }

        public void UpdateSource(IEffect effect, int slotIndex)
        {
            SourceEffect = effect;
            SlotIndex = slotIndex;
            if (_slotLbl != null)
            {
                _slotLbl.text = slotIndex >= 0 ? SlotNames[slotIndex] : "Hook";
                _slotLbl.style.color = _theme.PortAction;
            }
            if (InputPort != null)
                InputPort.portColor = _theme.PortAction;
            RefreshDisplay();
        }

        public void LoadFromEffect(IEffect effect, string slotName)
        {
            if (effect == null) return;
            var (mode, obj, cls, meth, relDesc) = effect.ReadSlot(slotName);
            _localMode = (DelegateMode)mode;
            _localObj = obj;
            _localCls = cls ?? "";
            _localMethod = meth ?? "";
            _localRelDesc = relDesc ?? "";
        }

        public void TransferToEffect(IEffect effect, string slotName)
        {
            if (effect == null) return;
            effect.WriteSlot(slotName,
                (int)_localMode, _localObj, _localCls, _localMethod, _localRelDesc);
            effect.Reconstruct();
        }

        public void RefreshDisplay()
        {
            if (_methodLbl == null) return;
            _methodLbl.text = BuildMethodText();
            _methodLbl.style.color = IsLiveDelegateBound()
                    ? _theme.FieldVal : _theme.FieldMissVal;
            RefreshBadgeBg();
        }

        private string BuildMethodText()
        {
            if (_localMode == DelegateMode.None || string.IsNullOrEmpty(_localMethod))
                return "\u2014 unset \u2014";
            string name;
            if (_localMethod.Length > 4
                && (_localMethod.StartsWith("get_") || _localMethod.StartsWith("set_")))
                name = char.ToUpperInvariant(_localMethod[4]) + _localMethod.Substring(5);
            else
                name = _localMethod;
            string rel = DelegatePicker.RelationshipLabel((int)_localMode, _localRelDesc);
            if (rel != null) return rel + "-" + name;
            return name;
        }

        private bool IsLiveDelegateBound()
        {
            if (_localMode == DelegateMode.None) return false;
            if (SourceEffect == null || SourceEffect.GetLiveDelegate(SlotName) == null) return false;
            if (_localMode == DelegateMode.RelativeInstance
                || _localMode == DelegateMode.RelativeStatic)
            {
                GameObject go = _graphView != null ? _graphView.TargetGameObject : null;
                if (go == null) return true;
                return Processor.ResolveDescriptor(go, _localRelDesc) != null;
            }
            return true;
        }

        private void RefreshBadgeBg()
        {
            if (_badge == null) return;
            _badge.style.backgroundColor = IsLiveDelegateBound()
                ? _theme.FieldBg : _theme.FieldBgMiss;
        }

        private static System.Type GetValueType(IEffect effect)
        {
            switch (effect.ValueType)
            {
                case PropertyTypes.Float: return typeof(float);
                case PropertyTypes.Vector2: return typeof(Vector2);
                case PropertyTypes.Vector3: return typeof(Vector3);
                case PropertyTypes.Vector4: return typeof(Vector4);
                case PropertyTypes.Quaternion: return typeof(Quaternion);
                default:
                    Assert.IsTrue(false, $"Unknown PropertyTypes value: {effect.ValueType}");
                    return null;
            }
        }
    }
}