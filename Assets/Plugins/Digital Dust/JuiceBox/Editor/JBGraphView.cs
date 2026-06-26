using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static JuiceBox.Processor;
using static UnityEditor.Experimental.GraphView.Port;

// ==============================================================================
//  SequenceGraphView: Core graph view for the sequence editor. Manages strips, nodes, and edges.
// ==============================================================================
namespace JuiceBox
{
    internal sealed partial class SequenceGraphView : GraphView
    {
        private const float StripOriginX = 50f;
        private const float StripOriginY = 50f;
        private const float SequenceGap = 52f;
        private const float SnapYThreshold = FilmStripElement.NodeAreaH * 1.5f;
        private const float SnapXThreshold = FilmStripElement.Stride * 0.5f;
        internal const float NodeMargin = 5f;
        private const int MaxSlots = 32;

        private readonly List<FilmStripElement> _strips = new List<FilmStripElement>();
        private readonly SequenceEditorWindow _window;
        private JuiceBoxAnimation _anim;
        private IAnimationEditorComponent Ed => (IAnimationEditorComponent)_anim;

        private JbTheme _theme = JbTheme.Default;
        internal JbTheme Theme => _theme;
        internal GameObject TargetGameObject => _anim != null ? _anim.gameObject : null;
        internal JuiceBoxAnimation TargetAnimation => _anim;

        private readonly List<List<EffectNode>> _occ = new List<List<EffectNode>>();
        private readonly List<EffectNode> _nodes = new List<EffectNode>();
        private readonly Dictionary<EffectNode, Vector2> _settled =
           new Dictionary<EffectNode, Vector2>();

        private readonly List<HookNode> _hookNodes = new List<HookNode>();
        private readonly Dictionary<HookNode, Vector2> _hookSettled =
           new Dictionary<HookNode, Vector2>();

        private readonly List<SmoothingNode> _smoothingNodes = new List<SmoothingNode>();
        private readonly Dictionary<SmoothingNode, Vector2> _smoothingSettled =
           new Dictionary<SmoothingNode, Vector2>();

        private readonly List<LoopNode> _loopNodes = new List<LoopNode>();
        private readonly Dictionary<LoopNode, Vector2> _loopSettled =
           new Dictionary<LoopNode, Vector2>();

        private readonly List<EffectNode> _scratch = new List<EffectNode>();

        private bool _suppressTransformClamp;
        private static readonly Comparison<EffectNode> _bySlot = (a, b) => a.SlotIndex.CompareTo(b.SlotIndex);
        private static readonly Comparison<EffectNode> _byPosX = (a, b) =>
           (a.GetPosition().x + EffectNode.NodeW * 0.5f).CompareTo(b.GetPosition().x + EffectNode.NodeW * 0.5f);

        private readonly EdgeDropSpawnListener _edgeDropListener;

        private string _pendingNewName;
        private int _renameStripIndex = -1;
        private IVisualElementScheduledItem _renameSchedule;

        public SequenceGraphView(SequenceEditorWindow window)
        {
            _window = window;
            _edgeDropListener = new EdgeDropSpawnListener(this);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());

            AddToClassList("jb-seq-view");
            var guids = AssetDatabase.FindAssets("SequenceEditorWindow t:StyleSheet");
            if (guids.Length > 0)
            {
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                   AssetDatabase.GUIDToAssetPath(guids[0]));
                if (sheet != null) styleSheets.Add(sheet);
            }
            else
                Debug.LogWarning("JuiceBox: SequenceEditorWindow.uss not found.");

            var grid = new GridBackground();
            grid.AddToClassList("jb-seq-grid");
            grid.StretchToParentSize();
            Insert(0, grid);

            graphViewChanged += OnGraphChanged;
            viewTransformChanged += OnViewTransformChanged;
            schedule.Execute(PollDragFeedback).Every(33);

            RegisterCallback<CustomStyleResolvedEvent>(_ =>
            {
                _theme = JbTheme.ReadFrom(customStyle);
                foreach (var s in _strips) s.ApplyTheme(_theme);
                foreach (var n in _nodes) n.ApplyTheme(_theme);
                foreach (var h in _hookNodes) { /* HookNode theme is set at construction */ }
            });
        }

        internal Port CreateSpawnablePort(
           Orientation orientation, Direction direction,
           Port.Capacity capacity, Type portType)
        {
            return new SpawnPort(orientation, direction, capacity, portType, _edgeDropListener);
        }

        private sealed class SpawnPort : Port
        {
            internal SpawnPort(Orientation o, Direction d, Capacity c,
               Type t, IEdgeConnectorListener listener)
               : base(o, d, c, t)
            {
                this.AddManipulator(new EdgeConnector<JbChannelEdge>(listener));
            }
        }

        private sealed class EdgeDropSpawnListener : IEdgeConnectorListener
        {
            private readonly SequenceGraphView _gv;
            private GraphViewChange _change;
            private readonly List<Edge> _edgesToCreate = new List<Edge>();
            private readonly List<GraphElement> _edgesToDelete = new List<GraphElement>();

            public EdgeDropSpawnListener(SequenceGraphView gv)
            {
                _gv = gv;
                _change.edgesToCreate = _edgesToCreate;
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                _edgesToCreate.Clear();
                _edgesToCreate.Add(edge);

                _edgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                    foreach (var c in edge.input.connections)
                        if (c != edge) _edgesToDelete.Add(c);
                if (edge.output.capacity == Capacity.Single)
                    foreach (var c in edge.output.connections)
                        if (c != edge) _edgesToDelete.Add(c);
                if (_edgesToDelete.Count > 0)
                    graphView.DeleteElements(_edgesToDelete);

                var result = _edgesToCreate;
                if (graphView.graphViewChanged != null)
                    result = graphView.graphViewChanged(_change).edgesToCreate;

                foreach (var e in result)
                {
                    graphView.AddElement(e);
                    edge.input.Connect(e);
                    edge.output.Connect(e);
                }
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                Port sourcePort = edge.output ?? edge.input;
                if (sourcePort == null) return;

                var ownerNode = FindOwnerNode(sourcePort);

                if (ownerNode is HookNode)
                {
                    if (edge.input != null) edge.input.Disconnect(edge);
                    if (edge.output != null) edge.output.Disconnect(edge);
                    _gv.RemoveElement(edge);
                    _gv.ApplyAll();
                    return;
                }

                var effectNode = ownerNode as EffectNode;
                if (effectNode == null) return;

                string portData = (string)sourcePort.userData;
                if (string.IsNullOrEmpty(portData)) return;

                Vector2 graphPos = _gv.contentViewContainer.WorldToLocal(position);

                if (portData == "Smoothing")
                    SpawnSmoothingFromDrop(effectNode, sourcePort, graphPos);
                else
                    SpawnHookFromDrop(effectNode, sourcePort, portData, graphPos);

                _gv.ApplyAll();
                if (_gv._anim != null) EditorUtility.SetDirty(_gv._anim);
            }

            private void SpawnSmoothingFromDrop(EffectNode effectNode, Port sourcePort, Vector2 pos)
            {
                IEffect effect = effectNode.SourceEffect;
                if (effect == null) return;
                if (_gv.FindSmoothingNodeForEffect(effect) != null) return;

                var sn = _gv.SpawnSmoothingNode(pos);
                sn.TransferToEffect(effect);
                sn.UpdateSource(effect);
                _gv.SnapSmoothing(sn);

                var capturedPort = sourcePort;
                var capturedSn = sn;
                _gv.DeferEdgeCreation(capturedSn.OutputPort, capturedPort);
            }

            private void SpawnHookFromDrop(EffectNode effectNode, Port sourcePort,
               string portData, Vector2 pos)
            {
                IEffect effect = effectNode.SourceEffect;
                if (effect == null) return;

                int slotIndex = Array.IndexOf(HookNode.SlotNames, portData);
                if (slotIndex < 0) return;
                if (_gv.FindHookNodeForSlot(effect, slotIndex) != null) return;

                var hn = _gv.SpawnHookNode(pos);
                hn.UpdateSource(effect, slotIndex);
                hn.LoadFromEffect(effect, HookNode.SlotNames[slotIndex]);
                _gv.SnapHook(hn);
                effectNode.RefreshHookBadge(slotIndex);

                var capturedPort = sourcePort;
                var capturedHn = hn;
                _gv.DeferEdgeCreation(capturedPort, capturedHn.InputPort);
            }
        }

        public void SetTarget(JuiceBoxAnimation anim)
        {
            ClearStrips();
            _anim = anim;
            if (anim?.Sequences == null) return;

            float y = StripOriginY;
            for (int i = 0; i < anim.Sequences.Count; i++)
            {
                Sequence seq = anim.Sequences[i];
                int effectCount = CountEffects(seq);
                int initialSlots = Mathf.Max(FilmStripElement.MinSlots, effectCount + 1);

                int si = i;
                var strip = new FilmStripElement(seq, new Vector2(StripOriginX, y),
                   initialSlots, si, anim.Sequences.Count, _theme, OnSequenceNameChanged,
                   OnSegmentChanged, this, RemoveStrip, OnTriggersChanged,
                   OnLeftCapInitialClicked, OnLeftCapUpdateClicked, MoveStrip);
                AddElement(strip);
                _strips.Add(strip);

                var occ = new List<EffectNode>();
                for (int s = 0; s < initialSlots; s++) occ.Add(null);
                _occ.Add(occ);

                y += FilmStripElement.StripTotalHeight + SequenceGap;
            }

            int[] nextSl = new int[_strips.Count];

            for (int si = 0; si < anim.Sequences.Count; si++)
            {
                Sequence seq = anim.Sequences[si];
                if (seq.Property == null) continue;
                var layout = LayoutEditorAccess.GetOrCreateLayout(anim, si);
                int count = seq.Property.EffectCount;

                for (int ei = 0; ei < count; ei++)
                {
                    IEffect effect = seq.Property.GetEffect(ei);
                    if (effect == null) continue;

                    var node = new EffectNode(effect, si, -1, ei, _anim, _theme, this);
                    AddElement(node);
                    _nodes.Add(node);

                    bool hasSaved = ei < layout.effectNodePositions.Count
                       && layout.effectNodePositions[ei] != Vector2.zero;

                    Vector2 startPos = hasSaved
                       ? layout.effectNodePositions[ei]
                       : NodeCanvasPos(si, nextSl[si]++);

                    if (!hasSaved) while (_occ[si].Count <= nextSl[si] - 1) _occ[si].Add(null);
                    AssignNodeFromPosition(node, startPos);
                }
            }

            for (int si = 0; si < _strips.Count; si++)
            {
                SpawnLoopNodesForStrip(si);
                RebuildStrip(si);
                RefreshLeftCapPickers(si);
            }

            foreach (var effectNode in _nodes)
            {
                SpawnHookNodesForEffectNode(effectNode);
                SpawnSmoothingNodeForEffectNode(effectNode);
            }

            RunValidation();
            schedule.Execute(RaiseEdges);
        }

        public void ClearStrips()
        {
            _pendingNewName = null;
            _renameStripIndex = -1;
            _renameSchedule?.Pause();
            _renameSchedule = null;

            var edgeList = new List<GraphElement>();
            foreach (var e in edges) edgeList.Add(e);
            foreach (var e in edgeList) RemoveElement(e);
            foreach (var n in _nodes) RemoveElement(n);
            foreach (var h in _hookNodes) RemoveElement(h);
            foreach (var sm in _smoothingNodes) RemoveElement(sm);
            foreach (var ln in _loopNodes) RemoveElement(ln);
            foreach (var s in _strips) RemoveElement(s);
            _nodes.Clear(); _occ.Clear(); _settled.Clear(); _strips.Clear();
            _hookNodes.Clear(); _hookSettled.Clear();
            _smoothingNodes.Clear(); _smoothingSettled.Clear();
            _loopNodes.Clear(); _loopSettled.Clear();
            _anim = null;
        }

        public IReadOnlyList<FilmStripElement> Strips => _strips;

        internal Vector3 ViewPosition => new Vector3(
           contentViewContainer.resolvedStyle.translate.x,
           contentViewContainer.resolvedStyle.translate.y, 0f);
        internal float ViewScale => contentViewContainer.resolvedStyle.scale.value.x;
        internal void SetViewPosition(Vector3 pos) =>
           contentViewContainer.style.translate = new Translate(pos.x, pos.y, pos.z);

        internal Rect ComputeContentBounds()
        {
            if (_strips.Count == 0) return new Rect(0, 0, 0, 0);
            float xMax = 0f, yMax = 0f;
            foreach (var strip in _strips)
            {
                Rect r = strip.GetPosition();
                xMax = Mathf.Max(xMax, r.xMax + SequenceGap);
                yMax = Mathf.Max(yMax, r.yMax + SequenceGap);
            }
            foreach (var n in _nodes)
            {
                Rect r = n.GetPosition();
                xMax = Mathf.Max(xMax, r.xMax + SequenceGap);
                yMax = Mathf.Max(yMax, r.yMax + SequenceGap);
            }
            foreach (var n in _hookNodes)
            {
                Rect r = n.GetPosition();
                xMax = Mathf.Max(xMax, r.xMax + SequenceGap);
                yMax = Mathf.Max(yMax, r.yMax + SequenceGap);
            }
            foreach (var n in _smoothingNodes)
            {
                Rect r = n.GetPosition();
                xMax = Mathf.Max(xMax, r.xMax + SequenceGap);
                yMax = Mathf.Max(yMax, r.yMax + SequenceGap);
            }
            foreach (var n in _loopNodes)
            {
                Rect r = n.GetPosition();
                xMax = Mathf.Max(xMax, r.xMax + SequenceGap);
                yMax = Mathf.Max(yMax, r.yMax + SequenceGap);
            }
            return new Rect(0, 0, xMax, yMax);
        }

    }
}