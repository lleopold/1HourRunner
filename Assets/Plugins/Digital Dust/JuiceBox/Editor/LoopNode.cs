using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

// ==============================================================================
//  LoopNode: Graph node marking where a sequence loops back to the beginning.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class LoopNode : Node
    {
        public override VisualElement contentContainer => this;

        public int SlotIndex { get; set; } = -1;
        public int StripIndex { get; set; } = -1;

        private bool _isFloating;
        private readonly JbTheme _theme;
        private readonly SequenceGraphView _graphView;

        public LoopNode(int stripIndex, JbTheme theme, SequenceGraphView graphView)
        {
            StripIndex = stripIndex;
            _theme = theme.NodeBg == default ? JbTheme.Default : theme;
            _graphView = graphView;

            capabilities = Capabilities.Selectable | Capabilities.Movable;
            style.width = EffectNode.NodeW;
            style.height = EffectNode.NodeH;
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

            tooltip = "Sequence loops back to the beginning at this point";

            BuildContent();
        }

        private void BuildContent()
        {
            var header = new VisualElement { pickingMode = PickingMode.Ignore };
            header.style.position = Position.Absolute;
            header.style.left = header.style.top = 0f;
            header.style.width = EffectNode.NodeW;
            header.style.height = 14f;
            header.style.backgroundColor = _theme.NodeHeader;
            Add(header);

            var lbl = new Label("Loop to beginning") { pickingMode = PickingMode.Ignore };
            lbl.style.position = Position.Absolute;
            lbl.style.left = 10f;
            lbl.style.right = 10f;
            lbl.style.top = 0f;
            lbl.style.bottom = 0f;
            lbl.style.fontSize = 28f;
            lbl.style.color = _theme.FieldVal;
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbl.style.whiteSpace = WhiteSpace.Normal;
            Add(lbl);

            var closeBtn = new Button(() => _graphView?.RemoveLoopNode(this)) { text = "\u00d7" };
            closeBtn.style.position = Position.Absolute;
            closeBtn.style.left = EffectNode.NodeW - 34f;
            closeBtn.style.top = 1f;
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

        public void SetFloating(bool floating)
        {
            if (_isFloating == floating) return;
            _isFloating = floating;
            FilmStripElement.SetBorder(this,
               floating ? _theme.NodeBorderFloat : _theme.NodeBorder, 1.5f);
        }

        public void SetOrdinal(bool isDragging)
        {
            FilmStripElement.SetBorder(this,
               isDragging ? _theme.NodeBorderDrag : _theme.NodeBorder,
               isDragging ? 2f : 1.5f);
            _isFloating = false;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            FilmStripElement.SetBorder(this, _theme.NodeBorderSel, 2f);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            FilmStripElement.SetBorder(this,
               _isFloating ? _theme.NodeBorderFloat : _theme.NodeBorder, 1.5f);
        }
    }
}