using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  SmoothingNode: Graph node for editing spring parameters (frequency, damping, air resistance) on a Follow effect.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class SmoothingNode : Node
    {
        public override VisualElement contentContainer => this;

        public static float W => FilmStripElement.TopPerfW - 6f;
        public static float H => FilmStripElement.PerfH - 6f;

        public IEffect SourceEffect { get; private set; }

        private float _frequency = 1f;
        private float _dampingRatio = 1f;
        private float _airResistance = 0f;

        public Port OutputPort { get; private set; }

        private FloatField _freqField;
        private FloatField _dampField;
        private FloatField _airField;

        private readonly JbTheme _theme;
        private readonly SequenceGraphView _graphView;

        public SmoothingNode(IEffect effect,
           JbTheme theme = default, SequenceGraphView graphView = null)
        {
            SourceEffect = effect;
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

            if (effect != null)
                LoadFromEffect(effect);

            BuildContent();
        }

        private void BuildContent()
        {
            var titleLbl = new Label("Smoothing") { pickingMode = PickingMode.Ignore };
            titleLbl.style.position = Position.Absolute;
            titleLbl.style.left = 6f;
            titleLbl.style.top = 2f;
            titleLbl.style.fontSize = 14f;
            titleLbl.style.color = _theme.PortAction;
            Add(titleLbl);

            _freqField = MakeFieldRow("Freq", _frequency, 22f, 0.001f,
               v => { _frequency = v; PushToEffect(); });
            _freqField.tooltip = "Oscillation speed. Higher values make the spring stiffer";
            _dampField = MakeFieldRow("Damp", _dampingRatio, 40f, 0f,
               v => { _dampingRatio = v; PushToEffect(); });
            _dampField.tooltip = "1 = evenly damped. Below 1 = bouncy. Above 1 = sluggish";
            _airField = MakeFieldRow("Air", _airResistance, 58f, 0f,
               v => { _airResistance = v; PushToEffect(); });
            _airField.tooltip = "Linear drag on velocity. Higher values slow movement";

            OutputPort = Port.Create<JbChannelEdge>(
               Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
               typeof(object));
            OutputPort.portName = "";
            OutputPort.portColor = _theme.PortAction;
            OutputPort.style.position = Position.Absolute;
            OutputPort.style.left = W - 55f;
            OutputPort.style.top = -4f;
            Add(OutputPort);

            var closeBtn = new Button(() => _graphView?.RemoveSmoothingNode(this))
            { text = "\u00d7" };
            closeBtn.style.position = Position.Absolute;
            closeBtn.style.left = W - 28f;
            closeBtn.style.top = 2f;
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

        private FloatField MakeFieldRow(string label, float initialValue,
           float y, float minVal, System.Action<float> onChange)
        {
            float rowH = 16f;
            float fieldX = 3f;
            float fieldW = W - 6f;

            var row = new VisualElement();
            row.style.position = Position.Absolute;
            row.style.left = fieldX;
            row.style.top = y;
            row.style.width = fieldW;
            row.style.height = rowH;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = _theme.FieldBg;
            row.style.paddingLeft = row.style.paddingRight = 3f;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius =
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 2;

            var lbl = new Label(label) { pickingMode = PickingMode.Ignore };
            lbl.style.fontSize = 14f;
            lbl.style.color = _theme.FieldLbl;
            lbl.style.flexGrow = 1f;
            lbl.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(lbl);

            var field = new FloatField { value = initialValue };
            field.style.width = fieldW * 0.48f;
            field.style.height = rowH - 2f;
            field.style.fontSize = 14f;
            field.RegisterValueChangedCallback(evt =>
            {
                float clamped = Mathf.Max(minVal, evt.newValue);
                onChange(clamped);
                if (clamped != evt.newValue) field.SetValueWithoutNotify(clamped);
                _graphView?.ApplyAll();
            });
            row.Add(field);
            Add(row);
            return field;
        }

        public void UpdateSource(IEffect effect)
        {
            SourceEffect = effect;
            RefreshDisplay();
        }

        public void LoadFromEffect(IEffect effect)
        {
            if (effect == null) return;
            var smoothing = GetSmoothing(effect);
            if (smoothing == null) return;
            _frequency = smoothing.Frequency;
            _dampingRatio = smoothing.DampingRatio;
            _airResistance = smoothing.AirResistance;
        }

        public void TransferToEffect(IEffect effect)
        {
            if (effect == null) return;
            var smoothing = GetSmoothing(effect);
            if (smoothing == null) return;
            smoothing.Frequency = _frequency;
            smoothing.DampingRatio = _dampingRatio;
            smoothing.AirResistance = _airResistance;
            SetUseSmoothingStatic(effect, true);
        }

        public void ClearEffect(IEffect effect)
        {
            if (effect == null) return;
            SetUseSmoothingStatic(effect, false);
            var smoothing = GetSmoothing(effect);
            if (smoothing == null) return;
            smoothing.Frequency = 1f;
            smoothing.DampingRatio = 1f;
            smoothing.AirResistance = 0f;
        }

        public void RefreshDisplay()
        {
            _freqField?.SetValueWithoutNotify(_frequency);
            _dampField?.SetValueWithoutNotify(_dampingRatio);
            _airField?.SetValueWithoutNotify(_airResistance);
        }

        private void PushToEffect()
        {
            if (SourceEffect == null) return;
            TransferToEffect(SourceEffect);
        }

        private static ISmoothing GetSmoothing(IEffect effect)
            => effect?.GetSmoothing();

        internal static void SetUseSmoothingStatic(IEffect effect, bool value)
        {
            if (effect is IUseSmoothing ius) ius.UseSmoothing = value;
        }

        public static bool EffectSupportsSmoothing(IEffect effect)
            => effect != null && effect.Kind != EffectKind.Tween && effect.Kind != EffectKind.Shake;
    }
}