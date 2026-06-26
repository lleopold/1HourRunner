using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  EffectNode: Graph node representing a single effect (Tween, Follow, or Shake) in the sequence editor.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class EffectNode : Node
    {
        public override VisualElement contentContainer => this;

        public const float NodeW = FilmStripElement.NodeSize - SequenceGraphView.NodeMargin * 2f;
        public const float NodeH = 190f;

        public int SlotIndex { get; set; } = -1;
        public int StripIndex { get; set; } = -1;
        public int FlatIndex { get; set; }
        public IEffect SourceEffect { get; }

        private JbTheme _theme;
        private VisualElement _headerEl;
        private bool _isFloating;
        private JuiceBoxAnimation _anim;
        private SequenceGraphView _graphView;

        private readonly Dictionary<string, Port> _outputPorts = new Dictionary<string, Port>();
        private readonly VisualElement[] _hookBadgeEls = new VisualElement[4];
        private readonly Label[] _hookBadgeLbls = new Label[4];
        private EventCallback<MouseDownEvent> _endCondClickHandler;

        public EffectNode(IEffect effect, int stripIndex, int slotIndex, int flatIndex,
           JuiceBoxAnimation anim = null, JbTheme theme = default,
           SequenceGraphView graphView = null)
        {
            SourceEffect = effect;
            StripIndex = stripIndex;
            SlotIndex = slotIndex;
            FlatIndex = flatIndex;
            _anim = anim;
            _graphView = graphView;
            _theme = theme.NodeBg == default ? JbTheme.Default : theme;

            capabilities = Capabilities.Selectable | Capabilities.Movable;
            style.width = NodeW;
            style.height = NodeH;
            style.backgroundColor = _theme.NodeBg;
            style.borderTopLeftRadius = 5;
            style.borderTopRightRadius = 5;
            style.borderBottomLeftRadius = 5;
            style.borderBottomRightRadius = 5;
            style.overflow = Overflow.Hidden;
            FilmStripElement.SetBorder(this, _theme.NodeBorder, 1.5f);

            titleContainer.style.display = DisplayStyle.None;
            inputContainer.style.display = DisplayStyle.None;
            outputContainer.style.display = DisplayStyle.None;

            BuildVisuals(effect);
        }

        private void NotifyChanged()
        {
            if (_anim != null) EditorUtility.SetDirty(_anim);
            if (_graphView != null && StripIndex >= 0) _graphView.ApplySequence(StripIndex);
        }

        public void SetOrdinal(int n, bool isDragging)
        {
            FilmStripElement.SetBorder(this, isDragging ? _theme.NodeBorderDrag : _theme.NodeBorder,
               isDragging ? 2f : 1.5f);
            _isFloating = false;
        }

        public void SetFloating(bool floating)
        {
            if (_isFloating == floating) return;
            _isFloating = floating;
            ApplyStateBorder();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            ApplyStateBorder();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            ApplyStateBorder();
        }

        private void ApplyStateBorder()
        {
            if (selected)
                FilmStripElement.SetBorder(this,
                    _isFloating ? _theme.NodeBorderFloatSel : _theme.NodeBorderSel, 2f);
            else
                FilmStripElement.SetBorder(this,
                    _isFloating ? _theme.NodeBorderFloat : _theme.NodeBorder, 1.5f);
        }

        public Port GetOutputPort(string delegateName) =>
           _outputPorts.TryGetValue(delegateName, out var p) ? p : null;

        private static List<MethodInfo> _easingMethodsCache;

        private void BuildVisuals(IEffect effect)
        {
            System.Type valueType = GetValueType(effect);
            EffectKind kind = effect.Kind;
            float lp = 6f, fw = NodeW - lp * 2f;
            const float rowH = 26f, rowGap = 30f;

            _headerEl = Abs(0f, 0f, NodeW, 10f, _theme.NodeHeader);
            Add(_headerEl);

            BuildBadges(effect, kind, lp, fw);

            BuildContentRows(effect, kind, lp, fw, rowH, rowGap);

            BuildSmoothingPort(kind);

            BuildHookBadges(effect, lp, NodeH - 33f, fw, valueType);

            BuildCloseButton();
        }

        private void BuildBadges(IEffect effect, EffectKind kind, float lp, float fw)
        {
            // -- Type/value badges --
            var badges = Abs(lp, 12f, fw, 22f, Color.clear);
            badges.style.flexDirection = FlexDirection.Row;
            Color bBg = kind == EffectKind.Tween ? _theme.BadgeTweenBg
                      : kind == EffectKind.Shake ? _theme.BadgeShakeBg
                      : _theme.BadgeAdvBg;
            Color bText = kind == EffectKind.Tween ? _theme.BadgeTweenText
                        : kind == EffectKind.Shake ? _theme.BadgeShakeText
                        : _theme.BadgeAdvText;

            string kindLabel = KindLabel(kind);
            var typeBadge = MakeBadgeBtn(kindLabel, bBg, bText);
            typeBadge.tooltip = kind == EffectKind.Tween
               ? "Interpolates from current value to target over a fixed duration"
               : kind == EffectKind.Shake
               ? "Periodic oscillation around the starting value with optional decay"
               : "Chases a target continuously at a given speed. Connect a Smoothing node for spring physics";
            var dataBadge = MakeBadgeBtn(ValueTypeLabel(effect.ValueType), _theme.BadgeTypeBg, _theme.BadgeTypeText);
            dataBadge.tooltip = "Value type this effect operates on";
            badges.Add(typeBadge);
            badges.Add(dataBadge);
            Add(badges);
        }

        private void BuildContentRows(IEffect effect, EffectKind kind, float lp, float fw, float rowH, float rowGap)
        {
            // -- Content rows --
            float y = 36f;

            if (kind == EffectKind.Tween)
            {
                Add(DurationRow(effect, lp, y, fw, rowH)); y += rowGap;
                Add(EasingRow(effect, lp, y, fw, rowH)); y += rowGap;
            }
            else if (kind == EffectKind.Shake)
            {
                Add(WaveformRow(effect, lp, y, fw, rowH)); y += rowGap;
                Add(AmplitudeRow(effect, lp, y, fw, rowH)); y += rowGap;
                Add(ShakeDurationRow(effect, lp, y, fw, rowH)); y += rowGap;
                Add(EasingRow(effect, lp, y, fw, rowH)); y += rowGap;
            }
            else
            {
                Add(SpeedRow(effect, lp, y, fw, rowH)); y += rowGap;
                AddEndCondSection(effect, lp, y, fw, rowH); y += rowGap * 2f;
            }

            if (kind != EffectKind.Shake)
            {
                string getTargetLabel = "GetTarget";
                var getTargetRow = DelegateBadge(getTargetLabel, "GetTargetValue", effect, lp, y, fw, rowH);
                getTargetRow.tooltip = "The value this effect moves toward. Must be set for every effect";
                Add(getTargetRow); y += rowGap;
            }
        }

        private void BuildSmoothingPort(EffectKind kind)
        {
            // -- Smoothing port --
            if (kind != EffectKind.Tween && kind != EffectKind.Shake)
            {
                var smoothPort = _graphView.CreateSpawnablePort(
                   Orientation.Vertical, Direction.Input,
                   Port.Capacity.Single, typeof(object));
                smoothPort.portName = "";
                smoothPort.userData = "Smoothing";
                smoothPort.portColor = _theme.PortAction;
                smoothPort.tooltip = "Connect a smoothing node to configure spring parameters";
                smoothPort.style.position = Position.Absolute;
                smoothPort.style.left = NodeW - 50f;
                smoothPort.style.top = -4f;
                Add(smoothPort);
                _outputPorts["Smoothing"] = smoothPort;
            }
        }

        private void BuildCloseButton()
        {
            // -- Close button --
            var closeBtn = new Button(() => _graphView?.RemoveEffectNode(this))
            { text = "\u00d7" };
            closeBtn.style.position = Position.Absolute;
            closeBtn.style.left = NodeW - 34f;
            closeBtn.style.top = 1f;
            closeBtn.style.width = 24f; closeBtn.style.height = 24f;
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

        private VisualElement LiveRow(string label, float x, float y, float w, float h)
        {
            var row = new VisualElement();
            row.style.position = Position.Absolute;
            row.style.left = x; row.style.top = y;
            row.style.width = w; row.style.height = h;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = _theme.FieldBg;
            row.style.paddingLeft = 4f; row.style.paddingRight = 4f;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius =
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 2;

            var lbl = new Label(label) { pickingMode = PickingMode.Ignore };
            lbl.style.fontSize = 14f;
            lbl.style.color = _theme.FieldLbl;
            lbl.style.flexGrow = 1f;
            row.Add(lbl);
            return row;
        }

        private VisualElement DurationRow(IEffect effect, float x, float y, float w, float h)
        {
            var row = LiveRow("Duration", x, y, w, h);
            row.tooltip = "How long the effect takes to complete, in seconds";
            var iec = effect as IEndCondition;
            var wft = iec?.EndCondition as WaitForTime;
            if (wft == null)
            {
                wft = new WaitForTime { Time = 1f };
                if (iec != null) iec.EndCondition = wft;
            }
            var input = new FloatField { value = wft.Time };
            input.style.width = w * 0.48f; input.style.height = h - 2f;
            input.style.fontSize = 14f;
            input.RegisterValueChangedCallback(evt =>
            {
                wft.Time = Mathf.Max(0f, evt.newValue);
                NotifyChanged();
            });
            row.Add(input);
            return row;
        }

        private VisualElement SpeedRow(IEffect effect, float x, float y, float w, float h)
        {
            var row = LiveRow("Speed", x, y, w, h);
            row.tooltip = "Units per second toward the target";
            if (!(effect is ISpeed isp)) return row;

            bool isInf = float.IsPositiveInfinity(isp.Speed);

            var input = new FloatField { value = isInf ? 1f : isp.Speed };
            input.style.width = w * 0.34f; input.style.height = h - 2f;
            input.style.fontSize = 14f;
            input.style.display = isInf ? DisplayStyle.None : DisplayStyle.Flex;
            input.RegisterValueChangedCallback(evt =>
            {
                isp.Speed = evt.newValue;
                NotifyChanged();
            });

            var infToggle = new Toggle { value = isInf, text = "Inf" };
            infToggle.tooltip = "Makes the value track the target without any delay";
            infToggle.style.height = h - 2f;
            infToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    isp.Speed = float.PositiveInfinity;
                    input.style.display = DisplayStyle.None;
                }
                else
                {
                    isp.Speed = input.value;
                    input.style.display = DisplayStyle.Flex;
                }
                NotifyChanged();
            });

            row.Add(infToggle);
            row.Add(input);
            return row;
        }

        private void AddEndCondSection(IEffect effect, float x, float y, float w, float h)
        {
            var valueContainer = new VisualElement();
            valueContainer.style.position = Position.Absolute;
            valueContainer.style.left = x; valueContainer.style.top = y + (h + 2f);
            valueContainer.style.width = w; valueContainer.style.height = h;
            valueContainer.style.flexDirection = FlexDirection.Row;
            valueContainer.style.alignItems = Align.Center;
            valueContainer.style.backgroundColor = _theme.FieldBg;
            valueContainer.style.paddingLeft = 4f; valueContainer.style.paddingRight = 4f;
            valueContainer.style.borderTopLeftRadius = valueContainer.style.borderTopRightRadius =
            valueContainer.style.borderBottomLeftRadius = valueContainer.style.borderBottomRightRadius = 2;
            BuildEndCondValueControls(valueContainer, effect);

            var typeRow = LiveRow("End Cond.", x, y, w, h);
            typeRow.tooltip = "When this effect ends. Wait Time: after a fixed duration. Condition: when a custom condition returns true. In Range: when within a distance of the target. Never End: never completes on its own; runs until stopped externally";
            var iec = effect as IEndCondition;
            var ec = iec?.EndCondition;
            bool ecMissing = ec == null;
            var typeNames = new List<string> { "Wait Time", "Condition", "In Range", "Never End" };
            if (ecMissing) typeNames.Insert(0, "ERROR");
            string currentType = ec is WaitForTime ? "Wait Time"
                               : ec is WaitForCondition ? "Condition"
                               : ec is WaitForever ? "Never End"
                               : ec != null ? "In Range"
                               : "ERROR";
            int currentIdx = typeNames.IndexOf(currentType);
            if (currentIdx < 0) currentIdx = 0;

            var popup = new PopupField<string>(typeNames, currentIdx);
            popup.style.width = w * 0.62f; popup.style.height = h - 2f;
            popup.style.fontSize = 14f;
            popup.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "ERROR") return;
                var newCond = CreateDefaultEndCondition(evt.newValue, effect);
                if (iec != null) iec.EndCondition = newCond;
                BuildEndCondValueControls(valueContainer, effect);
                NotifyChanged();
            });
            typeRow.Add(popup);

            Add(typeRow);
            Add(valueContainer);
        }

        private void BuildEndCondValueControls(VisualElement container, IEffect effect)
        {
            container.Clear();
            if (_endCondClickHandler != null)
            {
                container.UnregisterCallback(_endCondClickHandler);
                _endCondClickHandler = null;
            }
            container.style.backgroundColor = StyleKeyword.Null;
            var lbl = new Label("") { pickingMode = PickingMode.Ignore };
            lbl.style.fontSize = 14f;
            lbl.style.color = _theme.FieldLbl;
            lbl.style.flexGrow = 1f;

            var ec = (effect as IEndCondition)?.EndCondition;

            if (ec is WaitForTime wft)
            {
                container.tooltip = "Duration in seconds before the effect completes";
                lbl.text = "Time";
                container.Add(lbl);
                var input = new FloatField { value = wft.Time };
                input.style.width = container.resolvedStyle.width * 0.48f;
                input.style.height = container.resolvedStyle.height - 2f;
                input.style.fontSize = 14f;
                container.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    input.style.width = container.resolvedStyle.width * 0.48f;
                    input.style.height = container.resolvedStyle.height - 2f;
                });
                input.RegisterValueChangedCallback(evt =>
                {
                    wft.Time = Mathf.Max(0f, evt.newValue);
                    NotifyChanged();
                });
                container.Add(input);
            }
            else if (ec is WaitForCondition wfc)
            {
                container.tooltip = "Click to bind a function that returns true when the effect should end";
                var (mode, _, _, meth, relDesc) = wfc.ReadSlot(nameof(wfc.EvaluateCondition));
                bool configured = mode != 0;
                bool healthy = IsSlotFunctional(mode, relDesc, wfc, nameof(wfc.EvaluateCondition));

                lbl.text = "Condition";
                lbl.style.color = _theme.FieldLbl;
                container.Add(lbl);

                var valLbl = PI(new Label(configured ? SlotDisplayMethod(mode, meth, relDesc) : "\u2014 unset \u2014"));
                valLbl.style.fontSize = 14f;
                valLbl.style.color = healthy ? _theme.FieldVal : _theme.FieldMissVal;
                container.Add(valLbl);
                container.style.backgroundColor = healthy ? _theme.FieldBg : _theme.FieldBgMiss;

                _endCondClickHandler = evt =>
                {
                    if (evt.button != 0 || _graphView == null) return;
                    evt.StopPropagation();
                    Vector2 pos = _graphView.WorldToLocal(evt.originalMousePosition);
                    DelegatePicker.Show(_graphView, wfc, nameof(wfc.EvaluateCondition),
                       GetValueType(effect), pos, _theme, () =>
                       {
                           var (newMode, _, _, newMeth, newRelDesc) = wfc.ReadSlot(nameof(wfc.EvaluateCondition));
                           bool nowConfigured = newMode != 0;
                           bool nowHealthy = IsSlotFunctional(newMode, newRelDesc, wfc, nameof(wfc.EvaluateCondition));
                           valLbl.text = nowConfigured ? SlotDisplayMethod(newMode, newMeth, newRelDesc) : "\u2014 unset \u2014";
                           valLbl.style.color = nowHealthy ? _theme.FieldVal : _theme.FieldMissVal;
                           container.style.backgroundColor = nowHealthy ? _theme.FieldBg : _theme.FieldBgMiss;
                           _graphView?.PropagateEffectSlotEdit(effect, nameof(wfc.EvaluateCondition));
                       }, selfObject: _anim != null ? _anim.gameObject : null);
                };
                container.RegisterCallback(_endCondClickHandler);
            }
            else if (ec is IRange ir)
            {
                container.tooltip = "Distance from target and velocity threshold at which the effect ends";
                const float kMin = Epsilon;
                const float kMax = 128f;
                float kLogRatio = Mathf.Log10(kMax / kMin);

                float rangeVal = ir.Range;
                var rangeLbl = new Label($"Rng {rangeVal:G3}") { pickingMode = PickingMode.Ignore };
                rangeLbl.style.fontSize = 12f;
                rangeLbl.style.color = _theme.FieldLbl;
                rangeLbl.style.width = 48f;
                container.Add(rangeLbl);

                float sliderVal = rangeVal > 0f
                    ? Mathf.Clamp01(Mathf.Log10(rangeVal / kMin) / kLogRatio)
                    : 0f;
                var rangeSlider = new Slider(0f, 1f) { value = sliderVal };
                rangeSlider.style.width = 60f;
                rangeSlider.RegisterValueChangedCallback(evt =>
                {
                    float actual = kMin * Mathf.Pow(kMax / kMin, evt.newValue);
                    ir.Range = actual;
                    rangeLbl.text = $"Rng {actual:G3}";
                    NotifyChanged();
                });
                container.Add(rangeSlider);

                float velVal = ir.Velocity;
                var velLbl = new Label(velVal > 0f ? $"Vel {velVal:G3}" : "Vel off") { pickingMode = PickingMode.Ignore };
                velLbl.style.fontSize = 12f;
                velLbl.style.color = _theme.FieldLbl;
                velLbl.style.width = 48f;
                velLbl.style.marginLeft = 4f;
                container.Add(velLbl);

                float velSliderVal = velVal > 0f
                    ? Mathf.Clamp01(Mathf.Log10(velVal / kMin) / kLogRatio)
                    : 0f;
                var velSlider = new Slider(0f, 1f) { value = velSliderVal };
                velSlider.style.width = 60f;
                velSlider.RegisterValueChangedCallback(evt =>
                {
                    float actual = evt.newValue < 0.01f ? 0f : kMin * Mathf.Pow(kMax / kMin, evt.newValue);
                    ir.Velocity = actual;
                    velLbl.text = actual > 0f ? $"Vel {actual:G3}" : "Vel off";
                    NotifyChanged();
                });
                container.Add(velSlider);
            }
            else
            {
                lbl.text = "\u2014 unset \u2014";
                container.Add(lbl);
            }
        }

        private static WaitCondition CreateDefaultEndCondition(string typeName, IEffect effect)
        {
            if (typeName == "Never End") return new WaitForever();
            if (typeName == "Wait Time") return new WaitForTime { Time = 1f };
            if (typeName == "Condition") return new WaitForCondition();
            WaitCondition cond;
            switch (effect.ValueType)
            {
                case PropertyTypes.Float: cond = new WaitForFloatWithinRange(); break;
                case PropertyTypes.Vector2: cond = new WaitForVector2WithinRange(); break;
                case PropertyTypes.Vector3: cond = new WaitForVector3WithinRange(); break;
                case PropertyTypes.Vector4: cond = new WaitForVector4WithinRange(); break;
                case PropertyTypes.Quaternion: cond = new WaitForQuaternionWithinRange(); break;
                default: return new WaitForTime { Time = 1f };
            }

            ((IRange)cond).Range = 0.2f;
            return cond;
        }

        private VisualElement WaveformRow(IEffect effect, float x, float y, float w, float h)
        {
            var row = LiveRow("Waveform", x, y, w, h);
            row.tooltip = "Waveform shape and oscillation frequency (Hz)";

            Waveform current = GetShakeWaveform(effect);
            var waveNames = new List<string> { "Sine", "Triangle", "Square", "Sawtooth" };
            int curIdx = (int)current;
            if (curIdx < 0 || curIdx >= waveNames.Count) curIdx = 0;

            var wavePopup = new PopupField<string>(waveNames, curIdx);
            wavePopup.style.width = w * 0.34f;
            wavePopup.style.height = h - 2f;
            wavePopup.style.fontSize = 12f;
            wavePopup.RegisterValueChangedCallback(evt =>
            {
                int idx = waveNames.IndexOf(evt.newValue);
                if (idx >= 0) SetShakeWaveform(effect, (Waveform)idx);
                NotifyChanged();
            });
            row.Add(wavePopup);

            var freqLabel = new Label("Hz") { pickingMode = PickingMode.Ignore };
            freqLabel.style.fontSize = 10f;
            freqLabel.style.color = _theme.FieldLbl;
            freqLabel.style.marginLeft = 4f;
            freqLabel.style.marginRight = 1f;
            row.Add(freqLabel);

            var freqInput = new FloatField { value = GetShakeFrequency(effect) };
            freqInput.style.width = w * 0.22f;
            freqInput.style.height = h - 2f;
            freqInput.style.fontSize = 11f;
            freqInput.RegisterValueChangedCallback(evt =>
            {
                SetShakeFrequency(effect, evt.newValue);
                NotifyChanged();
            });
            row.Add(freqInput);

            return row;
        }

        private VisualElement AmplitudeRow(IEffect effect, float x, float y, float w, float h)
        {
            var row = LiveRow("Amp", x, y, w, h);
            row.tooltip = "Per-axis oscillation magnitude";

            int axes;
            string[] labels;
            switch (effect.ValueType)
            {
                case PropertyTypes.Float:
                    axes = 1; labels = null; break;
                case PropertyTypes.Vector2:
                    axes = 2; labels = new[] { "X", "Y" }; break;
                case PropertyTypes.Vector3:
                    axes = 3; labels = new[] { "X", "Y", "Z" }; break;
                case PropertyTypes.Vector4:
                    axes = 4; labels = new[] { "X", "Y", "Z", "W" }; break;
                case PropertyTypes.Quaternion:
                    axes = 3; labels = new[] { "X\u00b0", "Y\u00b0", "Z\u00b0" }; break;
                default:
                    axes = 1; labels = null; break;
            }

            float fieldW = axes == 1 ? w * 0.48f : (w * 0.62f) / axes;

            for (int a = 0; a < axes; a++)
            {
                int capturedAxis = a;
                if (labels != null)
                {
                    var axLbl = new Label(labels[a]) { pickingMode = PickingMode.Ignore };
                    axLbl.style.fontSize = 10f;
                    axLbl.style.color = _theme.FieldLbl;
                    axLbl.style.marginLeft = a > 0 ? 2f : 0f;
                    axLbl.style.marginRight = 1f;
                    row.Add(axLbl);
                }
                var field = new FloatField { value = GetShakeAmpAxis(effect, a) };
                field.style.width = fieldW;
                field.style.height = h - 2f;
                field.style.fontSize = 11f;
                field.RegisterValueChangedCallback(evt =>
                {
                    SetShakeAmpAxis(effect, capturedAxis, evt.newValue);
                    NotifyChanged();
                });
                row.Add(field);
            }

            return row;
        }

        private VisualElement ShakeDurationRow(IEffect effect, float x, float y, float w, float h)
        {
            var row = LiveRow("Duration", x, y, w, h);
            row.tooltip = "Decay duration in seconds (Inf = continuous)";

            float dur = GetShakeDuration(effect);
            bool isInf = float.IsPositiveInfinity(dur);

            var input = new FloatField { value = isInf ? 1f : dur };
            input.style.width = w * 0.34f;
            input.style.height = h - 2f;
            input.style.fontSize = 14f;
            input.style.display = isInf ? DisplayStyle.None : DisplayStyle.Flex;
            input.RegisterValueChangedCallback(evt =>
            {
                SetShakeDuration(effect, evt.newValue);
                NotifyChanged();
            });

            var infToggle = new Toggle { value = isInf, text = "Inf" };
            infToggle.tooltip = "Continuous \u2014 shake plays indefinitely without decaying";
            infToggle.style.height = h - 2f;
            infToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    SetShakeDuration(effect, float.PositiveInfinity);
                    input.style.display = DisplayStyle.None;
                }
                else
                {
                    SetShakeDuration(effect, input.value);
                    input.style.display = DisplayStyle.Flex;
                }
                NotifyChanged();
            });

            row.Add(infToggle);
            row.Add(input);
            return row;
        }

        private static float GetShakeFrequency(IEffect effect)
        {
            if (effect is ShakeFloat sf) return sf.Frequency;
            if (effect is ShakeVector2 sv2) return sv2.Frequency;
            if (effect is ShakeVector3 sv3) return sv3.Frequency;
            if (effect is ShakeVector4 sv4) return sv4.Frequency;
            if (effect is ShakeQuaternion sq) return sq.Frequency;
            return 1f;
        }

        private static void SetShakeFrequency(IEffect effect, float value)
        {
            if (effect is ShakeFloat sf) sf.Frequency = Mathf.Max(0f, value);
            else if (effect is ShakeVector2 sv2) sv2.Frequency = Mathf.Max(0f, value);
            else if (effect is ShakeVector3 sv3) sv3.Frequency = Mathf.Max(0f, value);
            else if (effect is ShakeVector4 sv4) sv4.Frequency = Mathf.Max(0f, value);
            else if (effect is ShakeQuaternion sq) sq.Frequency = Mathf.Max(0f, value);
        }

        private static Waveform GetShakeWaveform(IEffect effect)
        {
            if (effect is ShakeFloat sf) return sf.WaveformType;
            if (effect is ShakeVector2 sv2) return sv2.WaveformType;
            if (effect is ShakeVector3 sv3) return sv3.WaveformType;
            if (effect is ShakeVector4 sv4) return sv4.WaveformType;
            if (effect is ShakeQuaternion sq) return sq.WaveformType;
            return Waveform.Sine;
        }

        private static void SetShakeWaveform(IEffect effect, Waveform value)
        {
            if (effect is ShakeFloat sf) sf.WaveformType = value;
            else if (effect is ShakeVector2 sv2) sv2.WaveformType = value;
            else if (effect is ShakeVector3 sv3) sv3.WaveformType = value;
            else if (effect is ShakeVector4 sv4) sv4.WaveformType = value;
            else if (effect is ShakeQuaternion sq) sq.WaveformType = value;
        }

        private static float GetShakeAmpAxis(IEffect effect, int axis)
        {
            if (effect is ShakeFloat sf) return sf.Amplitude;
            if (effect is ShakeVector2 sv2) return axis == 0 ? sv2.Amplitude.x : sv2.Amplitude.y;
            if (effect is ShakeVector3 sv3) return axis == 0 ? sv3.Amplitude.x : axis == 1 ? sv3.Amplitude.y : sv3.Amplitude.z;
            if (effect is ShakeVector4 sv4) return axis == 0 ? sv4.Amplitude.x : axis == 1 ? sv4.Amplitude.y : axis == 2 ? sv4.Amplitude.z : sv4.Amplitude.w;
            if (effect is ShakeQuaternion sq) return axis == 0 ? sq.Amplitude.x : axis == 1 ? sq.Amplitude.y : sq.Amplitude.z;
            return 0f;
        }

        private static void SetShakeAmpAxis(IEffect effect, int axis, float value)
        {
            if (effect is ShakeFloat sf) { sf.Amplitude = value; return; }
            if (effect is ShakeVector2 sv2)
            {
                var a = sv2.Amplitude;
                if (axis == 0) a.x = value; else a.y = value;
                sv2.Amplitude = a; return;
            }
            if (effect is ShakeVector3 sv3)
            {
                var a = sv3.Amplitude;
                if (axis == 0) a.x = value; else if (axis == 1) a.y = value; else a.z = value;
                sv3.Amplitude = a; return;
            }
            if (effect is ShakeVector4 sv4)
            {
                var a = sv4.Amplitude;
                if (axis == 0) a.x = value;
                else if (axis == 1) a.y = value;
                else if (axis == 2) a.z = value; else a.w = value;
                sv4.Amplitude = a; return;
            }
            if (effect is ShakeQuaternion sq)
            {
                var a = sq.Amplitude;
                if (axis == 0) a.x = value; else if (axis == 1) a.y = value; else a.z = value;
                sq.Amplitude = a;
            }
        }

        private static float GetShakeDuration(IEffect effect)
        {
            var iec = effect as IEndCondition;
            var wft = iec?.EndCondition as WaitForTime;
            return wft != null ? wft.Time : 1f;
        }

        private static void SetShakeDuration(IEffect effect, float value)
        {
            var iec = effect as IEndCondition;
            var wft = iec?.EndCondition as WaitForTime;
            if (wft == null)
            {
                wft = new WaitForTime { Time = value };
                if (iec != null) iec.EndCondition = wft;
            }
            else
            {
                wft.Time = Mathf.Max(0f, value);
            }
        }

        private VisualElement EasingRow(IEffect effect, float x, float y, float w, float h)
        {
            var row = LiveRow("Easing", x, y, w, h);
            row.tooltip = "Softens the beginning and/or end of the tween in different ways";
            if (effect.Kind != EffectKind.Tween && effect.Kind != EffectKind.Shake) return row;

            if (_easingMethodsCache == null)
            {
                _easingMethodsCache = typeof(StandardFunctions.Easing)
                   .GetMethods(BindingFlags.Public | BindingFlags.Static)
                   .Where(m => m.ReturnType == typeof(float)
                      && m.GetParameters().Length == 1
                      && m.GetParameters()[0].ParameterType == typeof(float))
                   .ToList();
            }
            var methods = _easingMethodsCache;

            var names = new List<string> { "\u2014 unset \u2014" };
            names.AddRange(methods.Select(m => m.Name));

            var liveEasing = effect.GetLiveDelegate("Easing");
            string current;
            bool isLambda = false;
            if (liveEasing == null)
            {
                current = "\u2014 unset \u2014";
            }
            else
            {
                string mname = liveEasing.Method.Name;
                if (mname.StartsWith("<"))
                {
                    current = "(lambda)";
                    isLambda = true;
                }
                else
                {
                    current = mname;
                }
            }

            int currentIdx = isLambda ? 0 : names.IndexOf(current);
            if (currentIdx < 0) currentIdx = 0;

            if (isLambda)
            {
                var customLbl = new Label("(custom)") { pickingMode = PickingMode.Ignore };
                customLbl.style.fontSize = 14f;
                customLbl.style.color = _theme.FieldVal;
                row.Add(customLbl);
                return row;
            }

            var popup = new PopupField<string>(names, currentIdx);
            popup.style.width = w * 0.6f; popup.style.height = h - 2f;
            popup.style.fontSize = 14f;
            popup.RegisterValueChangedCallback(evt =>
            {
                int idx = names.IndexOf(evt.newValue);
                if (idx <= 0)
                {
                    effect.WriteSlot("Easing", (int)Processor.DelegateMode.None,
                        null, "", "", "");
                    effect.SetLiveDelegate("Easing", null);
                }
                else
                {
                    var mi = methods[idx - 1];
                    var del = (System.Func<float, float>)mi.CreateDelegate(
                        typeof(System.Func<float, float>));
                    effect.WriteSlot("Easing", (int)Processor.DelegateMode.Static,
                        null, mi.DeclaringType.AssemblyQualifiedName, mi.Name, "");
                    effect.SetLiveDelegate("Easing", del);
                }
                NotifyChanged();
            });
            row.Add(popup);
            return row;
        }

        private VisualElement DelegateBadge(
            string rowLabel, string slotName, IEffect effect, float x, float y, float w, float h)
        {
            var (mode, _, _, method, relDesc) = effect.ReadSlot(slotName);
            bool configured = mode != 0;
            bool healthy = IsSlotFunctional(mode, relDesc, effect, slotName);

            var row = new VisualElement();
            row.style.position = Position.Absolute;
            row.style.left = x; row.style.top = y;
            row.style.width = w; row.style.height = h;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = healthy ? _theme.FieldBg : _theme.FieldBgMiss;
            row.style.paddingLeft = 4f; row.style.paddingRight = 4f;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius =
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 2;

            var lbl = PI(new Label(rowLabel));
            lbl.style.fontSize = 14f; lbl.style.color = _theme.FieldLbl; lbl.style.flexGrow = 1f;
            row.Add(lbl);

            var valLbl = PI(new Label(configured ? SlotDisplayMethod(mode, method, relDesc) : "\u2014 unset \u2014"));
            valLbl.style.fontSize = 14f;
            valLbl.style.color = healthy ? _theme.FieldVal : _theme.FieldMissVal;
            row.Add(valLbl);

            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0 || _graphView == null) return;
                evt.StopPropagation();
                Vector2 pos = _graphView.WorldToLocal(evt.originalMousePosition);
                DelegatePicker.Show(_graphView, effect, slotName, GetValueType(effect), pos, _theme, () =>
                {
                    var (newMode, _, _, newMeth, newRelDesc) = effect.ReadSlot(slotName);
                    bool nowConfigured = newMode != 0;
                    bool nowHealthy = IsSlotFunctional(newMode, newRelDesc, effect, slotName);
                    row.style.backgroundColor = nowHealthy ? _theme.FieldBg : _theme.FieldBgMiss;
                    valLbl.text = nowConfigured ? SlotDisplayMethod(newMode, newMeth, newRelDesc) : "\u2014 unset \u2014";
                    valLbl.style.color = nowHealthy ? _theme.FieldVal : _theme.FieldMissVal;
                    _graphView?.PropagateEffectSlotEdit(effect, slotName);
                }, selfObject: _anim != null ? _anim.gameObject : null);
            });

            DelegatePicker.RegisterDragToPick(row, _theme, (droppedGo, mousePos) =>
            {
                if (_graphView == null) return;
                Vector2 pos = _graphView.WorldToLocal(mousePos);
                DelegatePicker.Show(_graphView, effect, slotName, GetValueType(effect), pos, _theme, () =>
                {
                    var (newMode, _, _, newMeth, newRelDesc) = effect.ReadSlot(slotName);
                    bool nowConfigured = newMode != 0;
                    bool nowHealthy = IsSlotFunctional(newMode, newRelDesc, effect, slotName);
                    row.style.backgroundColor = nowHealthy ? _theme.FieldBg : _theme.FieldBgMiss;
                    valLbl.text = nowConfigured ? SlotDisplayMethod(newMode, newMeth, newRelDesc) : "\u2014 unset \u2014";
                    valLbl.style.color = nowHealthy ? _theme.FieldVal : _theme.FieldMissVal;
                    _graphView?.PropagateEffectSlotEdit(effect, slotName);
                }, selfObject: _anim != null ? _anim.gameObject : null,
                prePickedTarget: droppedGo);
            });
            return row;
        }

        private static readonly string[] BadgeSlotNames = { "OnStart", "OnDone", "ModifyEffectState", "SetStartingVelocity" };
        private static readonly string[] BadgeDisplayNames = { "On Start", "On Done", "Signal", "Kick" };
        private static readonly string[] BadgeTooltips =
        {
            "Called once when this effect begins",
            "Called once when this effect completes",
            "Override the way this effect runs from code. Can Pause, Stop the effect from ending, terminate it early, abort it (without triggering OnDone), or restart it",
            "Sets the starting velocity when this effect begins. Receives the current velocity from the previous effect"
        };
        private static readonly int[] BadgeVisualOrder = { 1, 2, 3, 0 };
        private const int BadgeCount = 4;

        private void BuildHookBadges(IEffect effect, float lp, float y, float fw, System.Type valueType)
        {
            Debug.Assert(BadgeSlotNames.Length == BadgeCount
                && BadgeDisplayNames.Length == BadgeCount
                && BadgeTooltips.Length == BadgeCount
                && BadgeVisualOrder.Length == BadgeCount
                && _hookBadgeEls.Length == BadgeCount
                && HookNode.SlotNames.Length == BadgeCount,
                "Badge array length mismatch - update all arrays when adding a new slot.");

            bool isShake = effect.Kind == EffectKind.Shake;
            bool hideKick = effect.Kind == EffectKind.Tween || isShake;
            int visibleCount = (hideKick && !isShake) ? BadgeCount - 1 : BadgeCount;
            float slotW = (fw - (visibleCount - 1f)) / visibleCount;

            int visualSlot = isShake ? 1 : 0;
            for (int i = 0; i < BadgeCount; i++)
            {
                if (hideKick && BadgeSlotNames[i] == "SetStartingVelocity")
                {
                    if (isShake)
                        AddOvertimeBadge(effect, lp, y, slotW, 0);
                    continue;
                }

                int hookIdx = i;
                string slotName = BadgeSlotNames[i];
                int visualPos = hideKick ? visualSlot : BadgeVisualOrder[i];

                Debug.Assert(slotName == HookNode.SlotNames[i],
                    $"BadgeSlotNames[{i}] ({slotName}) != HookNode.SlotNames[{i}] ({HookNode.SlotNames[i]})");

                var (mode, _, _, method, relDesc) = effect.ReadSlot(slotName);
                bool bound = IsSlotFunctional(mode, relDesc, effect, slotName);

                var badge = new VisualElement();
                badge.tooltip = BadgeTooltips[i];
                badge.style.position = Position.Absolute;
                badge.style.left = lp + visualPos * (slotW + 1f);
                badge.style.top = y;
                badge.style.width = slotW;
                badge.style.height = 26f;
                badge.style.flexDirection = FlexDirection.Column;
                badge.style.alignItems = Align.Center;
                badge.style.justifyContent = Justify.FlexStart;
                badge.style.paddingTop = 2f;
                badge.style.backgroundColor = bound ? _theme.FieldBg : _theme.FieldBgMiss;
                badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius =
                badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 2;

                var nameLbl = PI(new Label(BadgeDisplayNames[i]));
                nameLbl.style.fontSize = 14f;
                nameLbl.style.color = bound ? _theme.FieldVal : _theme.FieldMissVal;
                nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                badge.Add(nameLbl);

                var port = _graphView.CreateSpawnablePort(Orientation.Vertical, Direction.Output,
                   Port.Capacity.Multi, typeof(object));
                port.portName = "";
                port.userData = slotName;
                port.portColor = _theme.PortAction;
                port.style.height = 14f;
                port.style.minWidth = 14f;
                port.style.marginTop = -4f;
                badge.Add(port);
                _outputPorts[slotName] = port;

                badge.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0 || _graphView == null) return;
                    evt.StopPropagation();
                    Vector2 pos = _graphView.WorldToLocal(evt.originalMousePosition);
                    DelegatePicker.Show(_graphView, effect, slotName, valueType, pos, _theme, () =>
                    {
                        var (newMode, _, _, newMeth, _) = effect.ReadSlot(slotName);
                        bool nowBound = newMode != 0;
                        if (nowBound && _graphView.FindHookNodeForSlot(effect, hookIdx) == null)
                        {
                            Rect r = GetPosition();
                            Vector2 sp = new Vector2(r.x + lp + visualPos * (slotW + 1f), r.yMax + 4f);
                            _graphView.SpawnHookNodeForSlot(effect, hookIdx, sp);
                        }
                        RefreshHookBadge(hookIdx);
                        _graphView?.PropagateEffectSlotEdit(effect, slotName);
                    },
                    () =>
                    {
                        var hn = _graphView?.FindHookNodeForSlot(effect, hookIdx);
                        if (hn != null) _graphView.RemoveHookNode(hn);
                        RefreshHookBadge(hookIdx);
                        NotifyChanged();
                    },
                    _anim != null ? _anim.gameObject : null,
                    effect.ReadValueSlot(slotName));
                });

                DelegatePicker.RegisterDragToPick(badge, _theme, (droppedGo, mousePos) =>
                {
                    if (_graphView == null) return;
                    Vector2 pos = _graphView.WorldToLocal(mousePos);
                    DelegatePicker.Show(_graphView, effect, slotName, valueType, pos, _theme, () =>
                    {
                        var (newMode, _, _, newMeth, _) = effect.ReadSlot(slotName);
                        bool nowBound = newMode != 0;
                        if (nowBound && _graphView.FindHookNodeForSlot(effect, hookIdx) == null)
                        {
                            Rect r = GetPosition();
                            Vector2 sp = new Vector2(r.x + lp + visualPos * (slotW + 1f), r.yMax + 4f);
                            _graphView.SpawnHookNodeForSlot(effect, hookIdx, sp);
                        }
                        RefreshHookBadge(hookIdx);
                        _graphView?.PropagateEffectSlotEdit(effect, slotName);
                    },
                    () =>
                    {
                        var hn = _graphView?.FindHookNodeForSlot(effect, hookIdx);
                        if (hn != null) _graphView.RemoveHookNode(hn);
                        RefreshHookBadge(hookIdx);
                        NotifyChanged();
                    },
                    _anim != null ? _anim.gameObject : null,
                    effect.ReadValueSlot(slotName),
                    prePickedTarget: droppedGo);
                });

                _hookBadgeEls[i] = badge;
                _hookBadgeLbls[i] = nameLbl;
                Add(badge);
                visualSlot++;
            }
        }

        private void AddOvertimeBadge(IEffect effect, float lp, float y, float slotW, int visualPos)
        {
            var badge = new VisualElement();
            badge.tooltip = "When Duration finishes, keeps the shake running until it returns to its start, avoiding a sudden jump at the end";
            badge.style.position = Position.Absolute;
            badge.style.left = lp + visualPos * (slotW + 1f);
            badge.style.top = y;
            badge.style.width = slotW;
            badge.style.height = 26f;
            badge.style.flexDirection = FlexDirection.Column;
            badge.style.alignItems = Align.Center;
            badge.style.justifyContent = Justify.FlexStart;
            badge.style.paddingTop = -2f;
            badge.style.backgroundColor = _theme.FieldBg;
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius =
            badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 2;

            var nameLbl = PI(new Label("Overtime"));
            nameLbl.style.fontSize = 13f;
            nameLbl.style.color = _theme.FieldVal;
            nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.Add(nameLbl);

            var toggle = new Toggle { value = GetShakeDeferUntilSettled(effect) };
            toggle.style.marginTop = 0f;
            toggle.style.marginLeft = 0f;
            toggle.RegisterValueChangedCallback(evt =>
            {
                SetShakeDeferUntilSettled(effect, evt.newValue);
                NotifyChanged();
            });
            badge.Add(toggle);
            Add(badge);
        }

        private static bool GetShakeDeferUntilSettled(IEffect effect)
        {
            if (effect is ShakeFloat sf) return sf.DeferUntilSettled;
            if (effect is ShakeVector2 sv2) return sv2.DeferUntilSettled;
            if (effect is ShakeVector3 sv3) return sv3.DeferUntilSettled;
            if (effect is ShakeVector4 sv4) return sv4.DeferUntilSettled;
            if (effect is ShakeQuaternion sq) return sq.DeferUntilSettled;
            return false;
        }

        private static void SetShakeDeferUntilSettled(IEffect effect, bool value)
        {
            if (effect is ShakeFloat sf) sf.DeferUntilSettled = value;
            else if (effect is ShakeVector2 sv2) sv2.DeferUntilSettled = value;
            else if (effect is ShakeVector3 sv3) sv3.DeferUntilSettled = value;
            else if (effect is ShakeVector4 sv4) sv4.DeferUntilSettled = value;
            else if (effect is ShakeQuaternion sq) sq.DeferUntilSettled = value;
        }

        public void RefreshHookBadge(int hookSlotIndex)
        {
            Debug.Assert(hookSlotIndex >= 0 && hookSlotIndex < BadgeCount,
                $"RefreshHookBadge: hookSlotIndex {hookSlotIndex} out of range [0,{BadgeCount}).");
            if (hookSlotIndex < 0 || hookSlotIndex >= BadgeCount) return;
            if (_hookBadgeEls[hookSlotIndex] == null) return;
            string slotName = BadgeSlotNames[hookSlotIndex];
            var (mode, _, _, _, relDesc) = SourceEffect.ReadSlot(slotName);
            bool bound = IsSlotFunctional(mode, relDesc, SourceEffect, slotName);
            _hookBadgeEls[hookSlotIndex].style.backgroundColor = bound ? _theme.FieldBg : _theme.FieldBgMiss;
            if (_hookBadgeLbls[hookSlotIndex] != null)
                _hookBadgeLbls[hookSlotIndex].style.color = bound ? _theme.FieldVal : _theme.FieldMissVal;
        }

        private static string SlotDisplayMethod(int mode, string method, string relDesc)
        {
            if (string.IsNullOrEmpty(method)) return "\u2026";
            string name;
            if (method.Length > 4 && (method.StartsWith("get_") || method.StartsWith("set_")))
                name = char.ToUpperInvariant(method[4]) + method.Substring(5);
            else
                name = method;
            string rel = DelegatePicker.RelationshipLabel(mode, relDesc);
            if (rel != null) return rel + "-" + name;
            return name;
        }

        private bool IsSlotFunctional(int mode, string relDesc, IEffect container, string slotName)
        {
            if (mode == 0) return false;
            if (container.GetLiveDelegate(slotName) == null) return false;
            if (mode == (int)Processor.DelegateMode.RelativeInstance
                || mode == (int)Processor.DelegateMode.RelativeStatic)
            {
                if (_anim == null) return true;
                return Processor.ResolveDescriptor(_anim.gameObject, relDesc) != null;
            }
            return true;
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

        private VisualElement MakeBadgeBtn(string text, Color bg, Color textCol)
        {
            var el = new VisualElement();
            el.style.backgroundColor = bg;
            el.style.paddingLeft = 5f; el.style.paddingRight = 5f;
            el.style.paddingTop = 1f; el.style.paddingBottom = 1f;
            el.style.marginRight = 3f;
            el.style.borderTopLeftRadius = el.style.borderTopRightRadius =
            el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = 2;
            var lbl = PI(new Label(text));
            lbl.style.fontSize = 14f; lbl.style.color = textCol;
            el.Add(lbl);
            return el;
        }

        private static VisualElement Abs(float x, float y, float w, float h, Color bg)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.style.position = Position.Absolute;
            el.style.left = x; el.style.top = y; el.style.width = w; el.style.height = h;
            if (bg != Color.clear) el.style.backgroundColor = bg;
            return el;
        }

        private static T PI<T>(T el) where T : VisualElement
        { el.pickingMode = PickingMode.Ignore; return el; }

        private static string KindLabel(EffectKind kind)
        {
            switch (kind)
            {
                case EffectKind.Tween: return "Tween";
                case EffectKind.Follow: return "Follow";
                case EffectKind.Shake: return "Shake";
                case EffectKind.Wait: return "Wait";
                default:
                    Assert.IsTrue(false, $"Unknown EffectKind value: {kind}");
                    return "?";
            }
        }

        private static string ValueTypeLabel(PropertyTypes vt)
        {
            switch (vt)
            {
                case PropertyTypes.Float: return "Float";
                case PropertyTypes.Vector2: return "Vec2";
                case PropertyTypes.Vector3: return "Vec3";
                case PropertyTypes.Vector4: return "Vec4";
                case PropertyTypes.Quaternion: return "Quat";
                default:
                    Assert.IsTrue(false, $"Unknown PropertyTypes value: {vt}");
                    return "?";
            }
        }

        public void ApplyTheme(JbTheme t)
        {
            _theme = t;
            style.backgroundColor = t.NodeBg;
            ApplyStateBorder();
            if (_headerEl != null)
                _headerEl.style.backgroundColor = t.NodeHeader;
        }
    }
}