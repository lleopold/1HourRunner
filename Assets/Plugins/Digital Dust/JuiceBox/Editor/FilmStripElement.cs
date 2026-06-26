using System.Collections.Generic;
using System.Linq;
using MEC;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  FilmStripElement: Horizontal film-strip visual element representing one sequence in the graph editor.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class FilmStripElement : GraphElement
    {
        public const float NodeSize = 260f;
        public const float ArrowWidth = 40f;
        public const float Stride = NodeSize + ArrowWidth;
        public const float LeftCapW = 200f;
        public const float EndCapW = 69f;
        public const float PadX = 15f;
        public const float PadY = 98f;
        public const int MinSlots = 3;

        internal const float PerfW = 90f;
        internal const float PerfH = 80f;
        internal const float PerfEdgeInset = 2f;
        internal const float TopPerfW = 135f;

        public static float StripTotalHeight => PadY * 2f + NodeAreaH;
        internal const float NodeAreaH = EffectNode.NodeH;

        private JbTheme _theme;
        private VisualElement _leftCapEl;

        private readonly Sequence _sequence;
        private readonly int _stripIndex;
        private readonly int _stripCount;
        private readonly System.Action<int, string> _onNameChanged;
        private readonly System.Action<int, int> _onSegmentChanged;
        private readonly System.Action<FilmStripElement> _onCloseRequested;
        private readonly System.Action<int, int> _onTriggersChanged;
        private readonly System.Action<int, int> _onMoveRequested;
        private readonly System.Action<int, bool, Vector2, GameObject> _onInitialPickerClicked;
        private readonly System.Action<int, Vector2, GameObject> _onUpdatePickerClicked;
        private SequenceGraphView _graphView;
        private TextField _nameField;
        private Button _restoreBtn;

        internal void SetDisplayName(string name)
        {
            if (_nameField != null)
                _nameField.SetValueWithoutNotify(name);
        }

        private bool HasRestoreSnapshots()
        {
            return !string.IsNullOrEmpty(_sequence.Name)
                && SequenceBackupManager.HasBackups(_sequence.Name);
        }

        internal void RefreshRestoreButton()
        {
            if (_restoreBtn != null)
                _restoreBtn.style.display = HasRestoreSnapshots()
                    ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement _capUpdateBadgeBg;
        private Label _capUpdateBadgeLbl;
        private VisualElement _capUpdateBadgeRoot;
        private VisualElement _capValBadgeBg;
        private Label _capValBadgeLbl;
        private VisualElement _capValBadgeRoot;
        private readonly List<VisualElement> _slotBoxes = new List<VisualElement>();
        private readonly List<VisualElement> _arrowBoxes = new List<VisualElement>();
        private readonly List<Label> _arrowLabels = new List<Label>();
        private readonly List<VisualElement> _perfElements = new List<VisualElement>();
        private readonly List<VisualElement> _pocketBoxes = new List<VisualElement>();
        private readonly List<VisualElement> _topPocketBoxes = new List<VisualElement>();
        private VisualElement _endCap;
        private VisualElement _runIndicator;
        private int _highlightedSlot = -1;
        private bool _highlightedDenied;
        private int _highlightedPocket = -1;
        private int _highlightedTopPocket = -1;
        private readonly float _slot0X;
        private readonly float _slot0Y;

        public FilmStripElement(Sequence sequence, Vector2 canvasOrigin, int slotCount,
           int stripIndex = 0, int stripCount = 1, JbTheme theme = default,
           System.Action<int, string> onNameChanged = null,
           System.Action<int, int> onSegmentChanged = null,
           SequenceGraphView graphView = null,
           System.Action<FilmStripElement> onCloseRequested = null,
           System.Action<int, int> onTriggersChanged = null,
           System.Action<int, bool, Vector2, GameObject> onInitialPickerClicked = null,
           System.Action<int, Vector2, GameObject> onUpdatePickerClicked = null,
           System.Action<int, int> onMoveRequested = null)
        {
            _theme = theme.StripBg == default ? JbTheme.Default : theme;
            _sequence = sequence;
            _slot0X = LeftCapW + PadX;
            _slot0Y = PadY;
            _onNameChanged = onNameChanged;
            _onSegmentChanged = onSegmentChanged;
            _graphView = graphView;
            _stripIndex = stripIndex;
            _stripCount = stripCount;
            _onCloseRequested = onCloseRequested;
            _onTriggersChanged = onTriggersChanged;
            _onInitialPickerClicked = onInitialPickerClicked;
            _onUpdatePickerClicked = onUpdatePickerClicked;
            _onMoveRequested = onMoveRequested;

            capabilities = 0;
            pickingMode = PickingMode.Ignore;

            style.backgroundColor = _theme.StripBg;
            style.borderTopLeftRadius = 7;
            style.borderTopRightRadius = 7;
            style.borderBottomLeftRadius = 7;
            style.borderBottomRightRadius = 7;
            style.overflow = Overflow.Hidden;

            SetPosition(new Rect(canvasOrigin.x, canvasOrigin.y,
               ComputeWidth(slotCount), StripTotalHeight));

            BuildLeftCap();
            BuildRunIndicator();
            BuildDynamicContent(slotCount);
        }

        public void SetTargetSlot(int slot, bool denied = false)
        {
            if (slot == _highlightedSlot && denied == _highlightedDenied) return;
            _highlightedSlot = slot;
            _highlightedDenied = denied;
            for (int i = 0; i < _slotBoxes.Count; i++)
            {
                bool hi = i == slot;
                Color border = hi
                    ? (denied ? _theme.SlotDenied : _theme.SlotHi)
                    : _theme.SlotIdle;
                SetBorder(_slotBoxes[i], border, hi ? 2f : 1f);
                _slotBoxes[i].style.backgroundColor = hi
                    ? (StyleColor)(denied ? _theme.SlotBgDenied : _theme.SlotBgHi)
                    : StyleKeyword.None;
            }
        }

        public void SetTargetBottomPocket(int index)
        {
            if (index == _highlightedPocket) return;
            _highlightedPocket = index;
            for (int i = 0; i < _pocketBoxes.Count; i++)
            {
                bool hi = i == index;
                SetBorder(_pocketBoxes[i], hi ? _theme.SlotHi : _theme.PocketBorder, hi ? 2f : 1f);
                _pocketBoxes[i].style.backgroundColor =
                   hi ? (StyleColor)_theme.SlotBgHi : (StyleColor)_theme.PocketBg;
            }
        }

        public void SetTargetTopPocket(int index)
        {
            if (index == _highlightedTopPocket) return;
            _highlightedTopPocket = index;
            for (int i = 0; i < _topPocketBoxes.Count; i++)
            {
                bool hi = i == index;
                SetBorder(_topPocketBoxes[i], hi ? _theme.SlotHi : _theme.PocketBorder, hi ? 2f : 1f);
                _topPocketBoxes[i].style.backgroundColor =
                   hi ? (StyleColor)_theme.SlotBgHi : (StyleColor)_theme.PocketBg;
            }
        }

        public void UpdateRunIndicator(int runLength)
        {
            if (_runIndicator == null) return;
            if (runLength <= 0 || _slotBoxes.Count == 0)
            { _runIndicator.style.display = DisplayStyle.None; return; }
            int clamped = Mathf.Min(runLength, _slotBoxes.Count);
            float w = clamped * NodeSize + Mathf.Max(0, clamped - 1) * ArrowWidth;
            _runIndicator.style.display = DisplayStyle.Flex;
            _runIndicator.style.width = w + 8f;
        }

        public void Resize(int newSlotCount)
        {
            foreach (var b in _slotBoxes) Remove(b);
            foreach (var a in _arrowBoxes) Remove(a);
            foreach (var p in _perfElements) Remove(p);
            if (_endCap != null) { Remove(_endCap); _endCap = null; }
            _slotBoxes.Clear(); _arrowBoxes.Clear(); _arrowLabels.Clear();
            _perfElements.Clear(); _pocketBoxes.Clear(); _topPocketBoxes.Clear();
            Rect cur = GetPosition();
            SetPosition(new Rect(cur.x, cur.y, ComputeWidth(newSlotCount), StripTotalHeight));
            BuildDynamicContent(newSlotCount);
            if (_highlightedSlot >= newSlotCount) { _highlightedSlot = -1; _highlightedDenied = false; }
            _highlightedPocket = -1;
            _highlightedTopPocket = -1;
        }

        public float GetSlotCanvasX(int i) => GetPosition().x + _slot0X + i * Stride;

        public int GetNearestPocketIndex(float canvasX)
        {
            const float gap = 4f;
            float spacing = TopPerfW + gap;
            int slotCount = _slotBoxes.Count;
            float areaW = slotCount * NodeSize + Mathf.Max(0, slotCount - 1) * ArrowWidth;
            int count = Mathf.Max(2, Mathf.CeilToInt((areaW + gap) / spacing));
            float localX = canvasX - GetPosition().x;
            float relX = localX - _slot0X - TopPerfW * 0.5f;
            return Mathf.Clamp(Mathf.RoundToInt(relX / spacing), 0, count - 1);
        }

        public float GetNearestPocketCentreX(float canvasX)
        {
            int i = GetNearestPocketIndex(canvasX);
            return GetPosition().x + _slot0X + i * (TopPerfW + 4f) + TopPerfW * 0.5f;
        }

        public int GetPocketCount()
        {
            const float gap = 4f;
            float spacing = TopPerfW + gap;
            int slotCount = _slotBoxes.Count;
            float areaW = slotCount * NodeSize + Mathf.Max(0, slotCount - 1) * ArrowWidth;
            return Mathf.Max(2, Mathf.CeilToInt((areaW + gap) / spacing));
        }

        public float GetPocketCentreX(int index)
        {
            return GetPosition().x + _slot0X + index * (TopPerfW + 4f) + TopPerfW * 0.5f;
        }

        public float GetSlotCanvasY() => GetPosition().y + _slot0Y;
        public Vector2 GetSlotPosition(int i) =>
           new Vector2(GetSlotCanvasX(i), GetSlotCanvasY());
        public Vector2 GetSlotCenter(int i) =>
           GetSlotPosition(i) + new Vector2(NodeSize * 0.5f, NodeAreaH * 0.5f);
        public int SlotCount => _slotBoxes.Count;

        private void BuildLeftCap()
        {
            var cap = new VisualElement();
            _leftCapEl = cap;
            cap.style.position = Position.Absolute;
            cap.style.left = 0; cap.style.top = 0;
            cap.style.width = LeftCapW; cap.style.height = StripTotalHeight;
            cap.style.backgroundColor = _theme.CapBg;
            cap.style.borderRightWidth = 1f;
            cap.style.borderRightColor = _theme.CapBorder;

            float lp = 10f;
            float contentH = 345f;
            float y = Mathf.Max(26f, (StripTotalHeight - contentH) * 0.5f);

            BuildCapNameField(cap, lp, ref y);
            BuildCapTriggerSection(cap, lp, ref y);
            BuildCapTimingSegmentSection(cap, lp, ref y);
            BuildCapOnUpdateSection(cap, lp, ref y);
            BuildCapInitialValueSection(cap, lp, ref y);
            BuildCapRestoreButton(cap, lp, y);
            BuildCapMoveCloseButtons(cap, lp);

            Add(cap);
        }

        private void BuildCapNameField(VisualElement cap, float lp, ref float y)
        {
            // -- Name --
            var nameField = new TextField { value = _sequence.Name ?? "" };
            _nameField = nameField;

            var nameLabel = new Label("Sequence name:") { pickingMode = PickingMode.Ignore };
            nameLabel.style.position = Position.Absolute;
            nameLabel.style.left = lp;
            nameLabel.style.top = y - 16f;
            nameLabel.style.fontSize = 10f;
            nameLabel.style.color = _theme.CapBorder;
            cap.Add(nameLabel);
            nameField.style.position = Position.Absolute;
            nameField.style.left = lp; nameField.style.top = y;
            nameField.style.width = LeftCapW - lp * 2f; nameField.style.height = 24f;
            nameField.style.fontSize = 16f;
            nameField.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameField.tooltip = "Sequence name. No two sequences can have the same name, and you can use the name to share sequences with different animations";
            nameField.Q("unity-text-input").style.color = _theme.CapName;
            nameField.RegisterValueChangedCallback(evt =>
               _onNameChanged?.Invoke(_stripIndex, evt.newValue));
            nameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    _graphView?.CommitPendingRename();
            }, TrickleDown.TrickleDown);
            nameField.RegisterCallback<FocusOutEvent>(evt =>
                _graphView?.CommitPendingRename());
            cap.Add(nameField);
            y += 28f;
        }

        private void BuildCapTriggerSection(VisualElement cap, float lp, ref float y)
        {
            // -- TRIGGER --
            cap.Add(CapLabel("TRIGGER", lp, y)); y += 18f;
            var triggerField = new EnumFlagsField(_sequence.Triggers);
            triggerField.style.position = Position.Absolute;
            triggerField.style.left = lp; triggerField.style.top = y;
            triggerField.style.width = LeftCapW - lp * 2f; triggerField.style.height = 24f;
            triggerField.style.fontSize = 14f;
            triggerField.tooltip = "Starts the sequence automatically";
            int capturedTriggerStrip = _stripIndex;
            triggerField.RegisterValueChangedCallback(evt =>
               _onTriggersChanged?.Invoke(capturedTriggerStrip,
                  System.Convert.ToInt32(evt.newValue)));
            cap.Add(triggerField); y += 28f;
        }

        private void BuildCapTimingSegmentSection(VisualElement cap, float lp, ref float y)
        {
            // -- TIMING SEGMENT --
            var segmentVals = System.Enum.GetValues(typeof(MEC.Segment))
               .Cast<MEC.Segment>()
               .Where(s => s != MEC.Segment.Invalid)
               .ToList();
            var segmentNames = segmentVals.Select(s => s.ToString()).ToArray();
            int defaultSegIdx = segmentVals.IndexOf(_sequence.Segment);
            if (defaultSegIdx < 0) defaultSegIdx = 0;
            cap.Add(CapLabel("TIMING SEGMENT", lp, y)); y += 18f;
            var segField = CapDropdown(segmentNames, defaultSegIdx, lp, y, LeftCapW - lp * 2f);
            segField.tooltip = "Timing segment for this sequence";
            ((PopupField<string>)segField).RegisterValueChangedCallback(evt =>
            {
                int segIdx = System.Array.IndexOf(segmentNames, evt.newValue);
                int segVal = segIdx >= 0 ? (int)segmentVals[segIdx] : 0;
                _onSegmentChanged?.Invoke(_stripIndex, segVal);
            });
            cap.Add(segField); y += 28f;
        }

        private void BuildCapOnUpdateSection(VisualElement cap, float lp, ref float y)
        {
            // -- ON UPDATE picker --
            cap.Add(HRule(lp, y, LeftCapW - lp * 2f)); y += 7f;

            cap.Add(CapLabel("ON UPDATE", lp, y)); y += 18f;
            (_capUpdateBadgeRoot, _capUpdateBadgeBg, _capUpdateBadgeLbl) =
                BuildCapDelegatePicker(lp, y, LeftCapW - lp * 2f, "update");
            _capUpdateBadgeRoot.tooltip = "The function called each frame to apply the animated value to your target property";
            cap.Add(_capUpdateBadgeRoot); y += 28f;
        }

        private void BuildCapInitialValueSection(VisualElement cap, float lp, ref float y)
        {
            // -- INITIAL VALUE picker --
            cap.Add(HRule(lp, y, LeftCapW - lp * 2f)); y += 7f;

            cap.Add(CapLabel("INITIAL VALUE", lp, y)); y += 18f;
            (_capValBadgeRoot, _capValBadgeBg, _capValBadgeLbl) =
                BuildCapInitialPicker(lp, y, LeftCapW - lp * 2f, isVelocity: false);
            _capValBadgeRoot.tooltip = "An optional field that provides the starting value for the sequence";
            cap.Add(_capValBadgeRoot); y += 28f;
        }

        private void BuildCapRestoreButton(VisualElement cap, float lp, float y)
        {
            // -- Restore button --
            _restoreBtn = new Button(() =>
               SequenceRestoreWindow.Show(_sequence, _graphView?.TargetAnimation))
            {
                text = "Restore from Snapshot"
            };
            _restoreBtn.tooltip = "Restore a previous version of this sequence from snapshots";
            _restoreBtn.style.position = Position.Absolute;
            _restoreBtn.style.left = lp;
            _restoreBtn.style.top = y;
            _restoreBtn.style.width = LeftCapW - lp * 2f;
            _restoreBtn.style.height = 24f;
            _restoreBtn.style.fontSize = 14f;
            _restoreBtn.style.display = HasRestoreSnapshots() ? DisplayStyle.Flex : DisplayStyle.None;
            cap.Add(_restoreBtn);
        }

        private void BuildCapMoveCloseButtons(VisualElement cap, float lp)
        {
            // -- Move up/down + close buttons --
            int capturedMoveIdx = _stripIndex;
            bool canMoveUp = _stripIndex > 0;
            bool canMoveDown = _stripIndex < _stripCount - 1;

            var upBtn = new Button(() => _onMoveRequested?.Invoke(capturedMoveIdx, -1))
            { text = "\u25B2" };
            upBtn.style.position = Position.Absolute;
            upBtn.style.left = LeftCapW - 84f;
            upBtn.style.top = 4f;
            upBtn.style.width = 24f; upBtn.style.height = 24f;
            upBtn.style.fontSize = 12f;
            upBtn.style.paddingLeft = upBtn.style.paddingRight = 0f;
            upBtn.style.paddingTop = upBtn.style.paddingBottom = 0f;
            upBtn.style.backgroundColor = Color.clear;
            upBtn.style.borderTopWidth = upBtn.style.borderBottomWidth =
            upBtn.style.borderLeftWidth = upBtn.style.borderRightWidth = 0f;
            upBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (canMoveUp)
            {
                upBtn.style.color = _theme.CapBorder;
                upBtn.RegisterCallback<MouseEnterEvent>(_ => upBtn.style.color = _theme.SlotHi);
                upBtn.RegisterCallback<MouseLeaveEvent>(_ => upBtn.style.color = _theme.CapBorder);
            }
            else
            {
                upBtn.SetEnabled(false);
                upBtn.style.color = new Color(1f, 1f, 1f, 0.15f);
            }
            cap.Add(upBtn);

            var downBtn = new Button(() => _onMoveRequested?.Invoke(capturedMoveIdx, 1))
            { text = "\u25BC" };
            downBtn.style.position = Position.Absolute;
            downBtn.style.left = LeftCapW - 56f;
            downBtn.style.top = 4f;
            downBtn.style.width = 24f; downBtn.style.height = 24f;
            downBtn.style.fontSize = 12f;
            downBtn.style.paddingLeft = downBtn.style.paddingRight = 0f;
            downBtn.style.paddingTop = downBtn.style.paddingBottom = 0f;
            downBtn.style.backgroundColor = Color.clear;
            downBtn.style.borderTopWidth = downBtn.style.borderBottomWidth =
            downBtn.style.borderLeftWidth = downBtn.style.borderRightWidth = 0f;
            downBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            if (canMoveDown)
            {
                downBtn.style.color = _theme.CapBorder;
                downBtn.RegisterCallback<MouseEnterEvent>(_ => downBtn.style.color = _theme.SlotHi);
                downBtn.RegisterCallback<MouseLeaveEvent>(_ => downBtn.style.color = _theme.CapBorder);
            }
            else
            {
                downBtn.SetEnabled(false);
                downBtn.style.color = new Color(1f, 1f, 1f, 0.15f);
            }
            cap.Add(downBtn);

            var closeBtn = new Button(() => _onCloseRequested?.Invoke(this))
            { text = "\u00d7" };
            closeBtn.style.position = Position.Absolute;
            closeBtn.style.left = LeftCapW - 28f;
            closeBtn.style.top = 4f;
            closeBtn.style.width = 24f; closeBtn.style.height = 24f;
            closeBtn.style.fontSize = 18f;
            closeBtn.style.paddingLeft = closeBtn.style.paddingRight = 0f;
            closeBtn.style.paddingTop = closeBtn.style.paddingBottom = 0f;
            closeBtn.style.backgroundColor = Color.clear;
            closeBtn.style.borderTopWidth = closeBtn.style.borderBottomWidth =
            closeBtn.style.borderLeftWidth = closeBtn.style.borderRightWidth = 0f;
            closeBtn.style.color = _theme.CapBorder;
            closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            closeBtn.RegisterCallback<MouseEnterEvent>(_ => closeBtn.style.color = _theme.SlotHi);
            closeBtn.RegisterCallback<MouseLeaveEvent>(_ => closeBtn.style.color = _theme.CapBorder);
            cap.Add(closeBtn);
        }

        private void BuildRunIndicator()
        {
            _runIndicator = new VisualElement { pickingMode = PickingMode.Ignore };
            _runIndicator.style.position = Position.Absolute;
            _runIndicator.style.left = _slot0X - 4f;
            _runIndicator.style.top = _slot0Y - 4f;
            _runIndicator.style.width = 0f;
            _runIndicator.style.height = NodeAreaH + 8f;
            _runIndicator.style.borderTopLeftRadius = 5;
            _runIndicator.style.borderTopRightRadius = 5;
            _runIndicator.style.borderBottomLeftRadius = 5;
            _runIndicator.style.borderBottomRightRadius = 5;
            _runIndicator.style.backgroundColor = _theme.RunBg;
            _runIndicator.style.display = DisplayStyle.None;
            SetBorder(_runIndicator, _theme.RunBorder, 1.5f);
            Add(_runIndicator);
        }

        private void BuildDynamicContent(int slotCount)
        {
            AddPerforations(slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                if (i > 0) AddArrow(i - 1);
                AddSlotBox(i);
            }
            BuildEndCap(slotCount);
        }

        private void AddPerforations(int slotCount)
        {
            float areaL = _slot0X;
            float areaW = slotCount * NodeSize + Mathf.Max(0, slotCount - 1) * ArrowWidth;
            const float gap = 4f;
            int count = Mathf.Max(2, Mathf.CeilToInt((areaW + gap) / (TopPerfW + gap)));

            for (int row = 0; row < 2; row++)
            {
                bool isBottom = row == 1;
                float rowY = isBottom
                   ? StripTotalHeight - PerfH - PerfEdgeInset
                   : PerfEdgeInset;
                Color bg = _theme.PocketBg;
                Color border = _theme.PocketBorder;

                for (int i = 0; i < count; i++)
                {
                    float cx = areaL + i * (TopPerfW + gap) + TopPerfW * 0.5f;
                    var p = new VisualElement { pickingMode = PickingMode.Ignore };
                    p.style.position = Position.Absolute;
                    p.style.left = cx - TopPerfW * 0.5f; p.style.top = rowY;
                    p.style.width = TopPerfW; p.style.height = PerfH;
                    p.style.borderTopLeftRadius = p.style.borderTopRightRadius =
                    p.style.borderBottomLeftRadius = p.style.borderBottomRightRadius = 8;
                    p.style.backgroundColor = bg;
                    SetBorder(p, border, 1f);
                    _perfElements.Add(p); Add(p);
                    if (isBottom) _pocketBoxes.Add(p);
                    else _topPocketBoxes.Add(p);
                }
            }
        }

        private void AddSlotBox(int i)
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.style.position = Position.Absolute;
            box.style.left = _slot0X + i * Stride; box.style.top = _slot0Y;
            box.style.width = NodeSize; box.style.height = NodeAreaH;
            box.style.borderTopLeftRadius = box.style.borderTopRightRadius =
            box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 5;
            SetBorder(box, _theme.SlotIdle, 1f);
            var num = new Label((i + 1).ToString()) { pickingMode = PickingMode.Ignore };
            num.style.position = Position.Absolute;
            num.style.right = 6; num.style.bottom = 5;
            num.style.fontSize = 10f; num.style.color = _theme.SlotNum;
            box.Add(num); _slotBoxes.Add(box); Add(box);
        }

        private void AddArrow(int afterSlot)
        {
            var arrow = new VisualElement { pickingMode = PickingMode.Ignore };
            arrow.style.position = Position.Absolute;
            arrow.style.left = _slot0X + afterSlot * Stride + NodeSize;
            arrow.style.top = _slot0Y;
            arrow.style.width = ArrowWidth; arrow.style.height = NodeAreaH;
            arrow.style.alignItems = Align.Center; arrow.style.justifyContent = Justify.Center;
            var lbl = new Label("\u2192") { pickingMode = PickingMode.Ignore };
            lbl.style.fontSize = 14f; lbl.style.color = _theme.Arrow;
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            arrow.Add(lbl); _arrowBoxes.Add(arrow); _arrowLabels.Add(lbl); Add(arrow);
        }

        public void SetLoopArrow(int arrowIndex, bool isLoop)
        {
            if (arrowIndex < 0 || arrowIndex >= _arrowBoxes.Count) return;
            var box = _arrowBoxes[arrowIndex];
            var lbl = _arrowLabels[arrowIndex];

            VisualElement existingArc = box.Q("loop-arc");

            if (isLoop)
            {
                lbl.style.display = DisplayStyle.None;
                if (existingArc == null)
                {
                    var arc = new VisualElement { name = "loop-arc" };
                    arc.style.position = Position.Absolute;
                    arc.style.left = 0; arc.style.top = 0;
                    arc.style.width = ArrowWidth; arc.style.height = NodeAreaH;
                    Color arcColor = _theme.Arc;
                    arc.generateVisualContent += ctx => DrawLoopArc(ctx, arcColor);
                    box.Add(arc);
                }
            }
            else
            {
                lbl.style.display = DisplayStyle.Flex;
                existingArc?.RemoveFromHierarchy();
            }
        }

        private static void DrawLoopArc(MeshGenerationContext ctx, Color color)
        {
            var p = ctx.painter2D;
            p.strokeColor = color;
            p.lineWidth = 2f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            const float lx = 8f;
            const float rx = ArrowWidth - 5f;
            const float h = NodeAreaH;
            float topY = h * 0.46f;
            float botY = h * 0.54f;

            p.BeginPath();
            p.MoveTo(new Vector2(lx, topY));
            p.BezierCurveTo(
                new Vector2(rx, topY),
                new Vector2(rx, botY),
                new Vector2(lx, botY));
            p.Stroke();

            const float hs = 4f;
            p.BeginPath();
            p.MoveTo(new Vector2(lx, topY));
            p.LineTo(new Vector2(lx + hs, topY - hs));
            p.MoveTo(new Vector2(lx, topY));
            p.LineTo(new Vector2(lx + hs, topY + hs));
            p.Stroke();
        }

        private void BuildEndCap(int slotCount)
        {
            _endCap = new VisualElement { pickingMode = PickingMode.Ignore };
            _endCap.style.position = Position.Absolute;
            _endCap.style.left = ComputeWidth(slotCount) - EndCapW;
            _endCap.style.top = 0; _endCap.style.width = EndCapW;
            _endCap.style.height = StripTotalHeight;
            _endCap.style.borderLeftWidth = 1f;
            _endCap.style.borderLeftColor = _theme.SlotIdle;
            _endCap.style.alignItems = Align.Center; _endCap.style.justifyContent = Justify.Center;
            Add(_endCap);
        }

        private static float ComputeWidth(int slotCount) =>
           LeftCapW + PadX + slotCount * NodeSize
           + Mathf.Max(0, slotCount - 1) * ArrowWidth + PadX + EndCapW;

        internal static void SetBorder(VisualElement el, Color c, float w)
        {
            el.style.borderTopColor = c; el.style.borderBottomColor = c;
            el.style.borderLeftColor = c; el.style.borderRightColor = c;
            el.style.borderTopWidth = w; el.style.borderBottomWidth = w;
            el.style.borderLeftWidth = w; el.style.borderRightWidth = w;
        }

        private VisualElement HRule(float x, float y, float width)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.style.position = Position.Absolute; el.style.left = x; el.style.top = y;
            el.style.width = width; el.style.height = 1f;
            el.style.backgroundColor = _theme.HRule;
            return el;
        }

        private Label CapLabel(string text, float x, float y) =>
           new Label(text)
           {
               pickingMode = PickingMode.Ignore,
               style = { position = Position.Absolute, left = x, top = y,
                      fontSize = 14f, color = _theme.CapLabel,
                      unityFontStyleAndWeight = FontStyle.Normal }
           };

        private static VisualElement CapDropdown(
           string[] options, int defaultIndex, float x, float y, float width)
        {
            var choices = options.ToList();
            int clampedDef = Mathf.Clamp(defaultIndex, 0, choices.Count - 1);
            var field = new PopupField<string>(choices, clampedDef);
            field.style.position = Position.Absolute;
            field.style.left = x; field.style.top = y;
            field.style.width = width; field.style.height = 24f; field.style.fontSize = 14f;
            return field;
        }

        private (VisualElement root, VisualElement badge, Label label) BuildCapInitialPicker(
            float x, float y, float width, bool isVelocity)
        {
            var root = new VisualElement();
            root.style.position = Position.Absolute;
            root.style.left = x; root.style.top = y;
            root.style.width = width; root.style.height = 24f;
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.backgroundColor = _theme.FieldBgMiss;
            root.style.paddingLeft = 4f; root.style.paddingRight = 4f;
            root.style.borderTopLeftRadius = root.style.borderTopRightRadius =
            root.style.borderBottomLeftRadius = root.style.borderBottomRightRadius = 2;
            root.style.opacity = 1f;

            var badge = new VisualElement { pickingMode = PickingMode.Ignore };
            badge.style.width = 6f;
            badge.style.height = 16f;
            badge.style.marginRight = 6f;
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius =
            badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 2;
            badge.style.backgroundColor = _theme.FieldMissVal;
            root.Add(badge);

            var label = new Label("zero") { pickingMode = PickingMode.Ignore };
            label.style.fontSize = 13f;
            label.style.color = _theme.FieldMissVal;
            label.style.flexGrow = 1f;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Clip;
            root.Add(label);

            bool capturedIsVelocity = isVelocity;
            int capturedStripIndex = _stripIndex;
            root.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (root.style.opacity.value < 0.99f) return;
                evt.StopPropagation();
                _onInitialPickerClicked?.Invoke(
                    capturedStripIndex, capturedIsVelocity, evt.originalMousePosition, null);
            });

            DelegatePicker.RegisterDragToPick(root, _theme, (droppedGo, mousePos) =>
            {
                if (root.style.opacity.value < 0.99f) return;
                _onInitialPickerClicked?.Invoke(
                    capturedStripIndex, capturedIsVelocity, mousePos, droppedGo);
            });

            return (root, badge, label);
        }

        private (VisualElement root, VisualElement badge, Label label) BuildCapDelegatePicker(
            float x, float y, float width, string tag)
        {
            var root = new VisualElement();
            root.style.position = Position.Absolute;
            root.style.left = x; root.style.top = y;
            root.style.width = width; root.style.height = 24f;
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.backgroundColor = _theme.FieldBgMiss;
            root.style.paddingLeft = 4f; root.style.paddingRight = 4f;
            root.style.borderTopLeftRadius = root.style.borderTopRightRadius =
            root.style.borderBottomLeftRadius = root.style.borderBottomRightRadius = 2;
            root.style.opacity = 1f;

            var badge = new VisualElement { pickingMode = PickingMode.Ignore };
            badge.style.width = 6f;
            badge.style.height = 16f;
            badge.style.marginRight = 6f;
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius =
            badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 2;
            badge.style.backgroundColor = _theme.FieldMissVal;
            root.Add(badge);

            var label = new Label("\u2014 unset \u2014") { pickingMode = PickingMode.Ignore };
            label.style.fontSize = 13f;
            label.style.color = _theme.FieldMissVal;
            label.style.flexGrow = 1f;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Clip;
            root.Add(label);

            int capturedStripIndex = _stripIndex;
            root.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                if (root.style.opacity.value < 0.99f) return;
                evt.StopPropagation();
                _onUpdatePickerClicked?.Invoke(capturedStripIndex, evt.originalMousePosition, null);
            });

            DelegatePicker.RegisterDragToPick(root, _theme, (droppedGo, mousePos) =>
            {
                if (root.style.opacity.value < 0.99f) return;
                _onUpdatePickerClicked?.Invoke(capturedStripIndex, mousePos, droppedGo);
            });

            return (root, badge, label);
        }

        public void UpdateLeftCapPickers(
            int updateMode, string updateMethod, string updateRelDesc, bool updateLive,
            bool valueEnabled, int valueMode, string valueMethod, string valueRelDesc, bool valueLive)
        {
            PaintInitialPicker(_capUpdateBadgeRoot, _capUpdateBadgeBg, _capUpdateBadgeLbl,
                true, updateMode, updateMethod, updateRelDesc, updateLive);
            PaintInitialPicker(_capValBadgeRoot, _capValBadgeBg, _capValBadgeLbl,
                valueEnabled, valueMode, valueMethod, valueRelDesc, valueLive, "zero");
        }

        private void PaintInitialPicker(
            VisualElement root, VisualElement badge, Label label,
            bool enabled, int mode, string method, string relDesc, bool liveOk,
            string unsetText = "\u2014 unset \u2014")
        {
            if (root == null || badge == null || label == null) return;

            if (!enabled)
            {
                root.style.opacity = 0.45f;
                root.style.backgroundColor = _theme.FieldBgMiss;
                badge.style.backgroundColor = _theme.FieldMissVal;
                label.text = "\u2014 disabled \u2014";
                label.style.color = _theme.FieldMissVal;
                return;
            }

            root.style.opacity = 1f;
            bool bound = mode != 0;
            bool healthy = bound && liveOk;
            if (bound)
            {
                root.style.backgroundColor = healthy ? _theme.FieldBg : _theme.FieldBgMiss;
                badge.style.backgroundColor = healthy ? _theme.FieldVal : _theme.FieldMissVal;
                label.text = string.IsNullOrEmpty(method) ? "\u2026" : CleanMethodDisplay(mode, method, relDesc);
                label.style.color = healthy ? _theme.FieldVal : _theme.FieldMissVal;
            }
            else
            {
                root.style.backgroundColor = _theme.FieldBgMiss;
                badge.style.backgroundColor = _theme.FieldMissVal;
                label.text = unsetText;
                label.style.color = _theme.FieldMissVal;
            }
        }

        private static string CleanMethodDisplay(int mode, string method, string relDesc)
        {
            string name;
            if (method.Length > 4 && (method.StartsWith("get_") || method.StartsWith("set_")))
                name = char.ToUpperInvariant(method[4]) + method.Substring(5);
            else
                name = method;
            string rel = DelegatePicker.RelationshipLabel(mode, relDesc);
            if (rel != null) return rel + "-" + name;
            return name;
        }

        public void ApplyTheme(JbTheme t)
        {
            _theme = t;
            style.backgroundColor = t.StripBg;
            if (_leftCapEl != null)
            {
                _leftCapEl.style.backgroundColor = t.CapBg;
                _leftCapEl.style.borderRightColor = t.CapBorder;
            }
            if (_runIndicator != null)
            {
                _runIndicator.style.backgroundColor = t.RunBg;
                SetBorder(_runIndicator, t.RunBorder, 1.5f);
            }
            for (int i = 0; i < _slotBoxes.Count; i++)
            {
                bool hi = i == _highlightedSlot;
                Color border = hi
                    ? (_highlightedDenied ? t.SlotDenied : t.SlotHi)
                    : t.SlotIdle;
                SetBorder(_slotBoxes[i], border, hi ? 2f : 1f);
            }
        }
    }
}