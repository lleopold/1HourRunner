using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceGraphView.Interaction: Drag, snap, edge wiring, and layout persistence for the graph view.
// ==============================================================================
namespace JuiceBox
{
    internal sealed partial class SequenceGraphView
    {

        public void ApplyAll()
        {
            if (_anim == null) return;
            for (int si = 0; si < _strips.Count; si++) ApplySequence(si);
            RunValidation();
            schedule.Execute(RaiseEdges);
        }

        internal void ApplySequence(int si, bool broadcast = true)
        {
            if (_anim == null || si >= _anim.Sequences.Count) return;
            Sequence seq = _anim.Sequences[si];

            Undo.RecordObject(_anim, "Apply JuiceBox Sequence");

            var ordered = GetOrderedNodes(si);

            LoopNode loopNode = GetLoopNodeForStrip(si);
            int loopSlot = (loopNode != null && loopNode.SlotIndex >= 0) ? loopNode.SlotIndex : -1;

            if (loopSlot >= 0)
                for (int i = ordered.Count - 1; i >= 0; i--)
                    if (ordered[i].SlotIndex >= loopSlot)
                    {
                        ordered[i].FlatIndex = -1;
                        ordered.RemoveAt(i);
                    }

            if (ordered.Count == 0)
            {
                if (seq.Property != null)
                    ClearEffectsInProperty(seq.Property);
                seq.LoopMode = LoopMode.None;
                var emptyLayout = LayoutEditorAccess.GetOrCreateLayout(_anim, si);
                emptyLayout.loopNodes = (loopSlot >= 0 && loopSlot < 32) ? (1u << loopSlot) : 0u;
                ((ISequenceEditorData)seq).NeedsRebuild = true;
                EditorUtility.SetDirty(_anim);
                if (broadcast) NotifyLibraryChanged(seq.Name, seq);
                SequenceBackupManager.TrySnapshot(seq, _anim);
                UpdateLoopArrow(si);
                RefreshLeftCapPickers(si);
                return;
            }

            if (seq.Property == null
                || (seq.Property.EffectCount == 0
                    && !PropertyMatchesEffect(seq.Property, ordered[0].SourceEffect)))
            {
                PropertyTypes newType = ordered[0].SourceEffect.ValueType;
                if (seq.Property != null && seq.Type != newType)
                    _window?.SetMessage(
                        $"Sequence \"{seq.Name}\" type changed from {seq.Type} to {newType}.",
                        SequenceEditorWindow.MessageSeverity.Info, 10f);
                seq.Property = MakeEmptyProperty(newType);
                seq.Type = newType;
            }

            ClearEffectsInProperty(seq.Property);

            int lastConnectedSlot = -1;
            for (int i = 0; i < ordered.Count; i++)
            {
                EffectNode node = ordered[i];

                if (i == 0)
                {
                    AddEffectToProperty(seq.Property, node.SourceEffect);
                    lastConnectedSlot = node.SlotIndex;
                }
                else
                {
                    EffectNode prev = ordered[i - 1];
                    if (node.SlotIndex != prev.SlotIndex + 1) continue;

                    AddEffectToProperty(seq.Property, node.SourceEffect);
                    lastConnectedSlot = node.SlotIndex;
                }
            }

            bool loopValid = loopSlot >= 0
                && lastConnectedSlot >= 0
                && loopSlot == lastConnectedSlot + 1;
            seq.LoopMode = loopValid ? LoopMode.Loop : LoopMode.None;
            var applyLayout = LayoutEditorAccess.GetOrCreateLayout(_anim, si);
            applyLayout.loopNodes = (loopSlot >= 0 && loopSlot < 32) ? (1u << loopSlot) : 0u;

            if (si < _strips.Count)
            {
                int runLen = ComputeRunLength(si);
                if (loopValid && loopSlot >= 0)
                    runLen = Mathf.Max(runLen, loopSlot + 1);
                _strips[si].UpdateRunIndicator(runLen);
            }

            ((ISequenceEditorData)seq).NeedsRebuild = true;

            int flat = 0;
            foreach (var node in ordered)
            {
                if (node.SlotIndex >= 0 && node.SlotIndex <= lastConnectedSlot)
                    node.FlatIndex = flat++;
                else
                    node.FlatIndex = -1;
            }

            ResizeValueLists(seq);
            SaveAllNodeData(si);
            if (broadcast) NotifyLibraryChanged(seq.Name, seq);
            SequenceBackupManager.TrySnapshot(seq, _anim);
            UpdateLoopArrow(si);
            RefreshLeftCapPickers(si);
        }

        private void NotifyLibraryChanged(string name, Sequence seq)
        {
            if (_window != null) _window.SuppressLibraryRebuild = true;
            SequenceLibrary.NotifySequenceChanged(name, seq, _anim);
            if (_window != null) _window.SuppressLibraryRebuild = false;
        }

        private void NotifyLibraryRenamed(string oldName, string newName, Sequence seq)
        {
            if (_window != null) _window.SuppressLibraryRebuild = true;
            SequenceLibrary.NotifySequenceRenamed(oldName, newName, seq, _anim);
            if (_window != null) _window.SuppressLibraryRebuild = false;
        }

        private void RunValidation()
        {
            if (_anim == null || _window == null) return;

            string infoMsg = null;
            string warningMsg = null;

            for (int si = 0; si < _anim.Sequences.Count; si++)
            {
                Sequence seq = _anim.Sequences[si];
                string seqName = string.IsNullOrEmpty(seq.Name) ? $"Sequence {si}" : seq.Name;

                if (seq.Property == null || seq.Property.EffectCount == 0)
                {
                    if (infoMsg == null)
                        infoMsg = $"\"{seqName}\" has no effects - drop an effect onto the strip.";
                    continue;
                }

                var container = (IDelegateConnecter)seq.Property;
                var upd = container.ReadSlot("OnUpdate");
                if (upd.mode == 0 && warningMsg == null)
                {
                    warningMsg = $"\"{seqName}\": OnUpdate is not assigned.";
                }
            }

            _window.SetMessage(infoMsg ?? "", SequenceEditorWindow.MessageSeverity.Info);
            _window.SetMessage(warningMsg ?? "", SequenceEditorWindow.MessageSeverity.Warning);
        }

        private static GraphElement FindOwnerNode(VisualElement el)
        {
            if (el is Port port && port.node is GraphElement ge)
                return ge;
            while (el != null)
            {
                if (el is EffectNode || el is HookNode || el is SmoothingNode)
                    return el as GraphElement;
                el = el.parent;
            }
            return null;
        }

        private void RaiseEdges()
        {
            BuildRegionMap();
            RouteEdges();
            var e = edges.GetEnumerator();
            if (e.MoveNext() && e.Current != null
                && e.Current.parent != null
                && e.Current.output?.node?.panel != null
                && e.Current.input?.node?.panel != null)
                e.Current.parent.BringToFront();
        }

        internal void DeferEdgeCreation(Port output, Port input)
        {
            schedule.Execute(() =>
            {
                schedule.Execute(() =>
                {
                    if (output?.node?.panel == null || input?.node?.panel == null) return;
                    var edge = output.ConnectTo<JbChannelEdge>(input);
                    AddElement(edge);
                    schedule.Execute(RaiseEdges);
                });
            });
        }

        private bool IsNodeAtSlot(int si, int slot, Node exclude = null)
        {
            if (slot < _occ[si].Count && _occ[si][slot] != null && _occ[si][slot] != exclude)
                return true;
            foreach (var ln in _loopNodes)
                if (ln.StripIndex == si && ln.SlotIndex == slot && ln != exclude) return true;
            return false;
        }

        internal EffectNode FindEffectNodeForEffect(IEffect effect)
        {
            foreach (var n in _nodes)
                if (n.SourceEffect == effect) return n;
            return null;
        }

        internal HookNode FindHookNodeForSlot(IEffect effect, int slotIndex)
        {
            foreach (var hn in _hookNodes)
                if (hn.SourceEffect == effect && hn.SlotIndex == slotIndex)
                    return hn;
            return null;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var result = new List<Port>();
            GraphElement startEl = FindOwnerNode(startPort);
            if (startEl == null) return result;

            ports.ForEach(port =>
            {
                if (port == startPort) return;
                if (port.direction == startPort.direction) return;

                GraphElement portEl = FindOwnerNode(port);
                if (portEl == null || portEl == startEl) return;

                bool compatible = false;

                if (startEl is EffectNode && portEl is HookNode)
                    compatible = (string)port.userData != "Value";
                else if (startEl is HookNode && portEl is EffectNode)
                    compatible = (string)startPort.userData != "Value";

                else if (startEl is EffectNode && portEl is SmoothingNode)
                    compatible = (string)startPort.userData == "Smoothing";
                else if (startEl is SmoothingNode && portEl is EffectNode)
                    compatible = (string)port.userData == "Smoothing";

                if (compatible) result.Add(port);
            });
            return result;
        }

        private void OnViewTransformChanged(GraphView _)
        {
            if (_suppressTransformClamp) return;
            float px = contentViewContainer.resolvedStyle.translate.x;
            float py = contentViewContainer.resolvedStyle.translate.y;
            float cx = Mathf.Min(px, 0f);
            float cy = Mathf.Min(py, 0f);
            if (cx != px || cy != py)
            {
                _suppressTransformClamp = true;
                contentViewContainer.style.translate = new Translate(cx, cy, 0f);
                _suppressTransformClamp = false;
            }
            _window?.UpdateScrollbars();
        }

        private GraphViewChange OnGraphChanged(GraphViewChange change)
        {
            if (change.movedElements != null) HandleMovedElements(change.movedElements);
            if (change.edgesToCreate != null) HandleEdgesToCreate(change.edgesToCreate);
            if (change.elementsToRemove != null) HandleElementsToRemove(change.elementsToRemove);

            foreach (var s in _strips) { s.SetTargetSlot(-1); s.SetTargetBottomPocket(-1); s.SetTargetTopPocket(-1); }
            return change;
        }

        private void HandleMovedElements(List<GraphElement> movedElements)
        {
            foreach (var el in movedElements)
            {
                if (el is EffectNode n) SnapNode(n);
                else if (el is HookNode h) SnapHook(h);
                else if (el is SmoothingNode sm) SnapSmoothing(sm);
                else if (el is LoopNode ln) SnapLoopNode(ln);
            }
            if (_anim != null) EditorUtility.SetDirty(_anim);

            // Routed edges draw a cached polyline, so a moved node leaves its wires
            // behind until we recompute. Snapping repositions the node, but the
            // resolved layout the router reads only updates on a later pass, so a
            // single deferred pass can still catch the drop position. Recompute now
            // for already-settled nodes, and once more when the moved nodes'
            // geometry resolves so the wires land on the snapped position.
            schedule.Execute(RaiseEdges);

            bool rerouted = false;
            EventCallback<GeometryChangedEvent> onSettled = null;
            onSettled = _ =>
            {
                if (rerouted) return;
                rerouted = true;
                foreach (var m in movedElements)
                    m?.UnregisterCallback(onSettled);
                RaiseEdges();
            };
            foreach (var el in movedElements)
                el?.RegisterCallback(onSettled);
        }

        private void HandleEdgesToCreate(List<Edge> edgesToCreate)
        {
            uint affectedStrips = 0;
            foreach (var edge in edgesToCreate)
            {
                var outEl = FindOwnerNode(edge.output);
                var inEl = FindOwnerNode(edge.input);
                if (outEl is EffectNode en && inEl is HookNode hn)
                {
                    SnapHook(hn);
                    string slotName = (string)edge.output.userData;
                    int newSlotIdx = System.Array.IndexOf(HookNode.SlotNames, slotName);

                    int oldSlotIdx = hn.SlotIndex;
                    IEffect oldEffect = hn.SourceEffect;
                    if (oldEffect != null && oldSlotIdx >= 0
                        && (oldEffect != en.SourceEffect || oldSlotIdx != newSlotIdx))
                    {
                        oldEffect.WriteSlot(hn.SlotName,
                            (int)DelegateMode.None, null, "", "", "");
                        oldEffect.Reconstruct();
                        SaveHookPos(oldEffect, oldSlotIdx, Vector2.zero);
                        var oldNode = FindEffectNodeForEffect(oldEffect);
                        oldNode?.RefreshHookBadge(oldSlotIdx);
                        if (oldNode != null && oldNode.StripIndex >= 0)
                            affectedStrips |= 1u << oldNode.StripIndex;
                    }

                    bool compatible = oldSlotIdx < 0
                        || SlotsCompatible(oldSlotIdx, newSlotIdx);
                    if (compatible)
                        hn.TransferToEffect(en.SourceEffect, slotName);
                    else
                        en.SourceEffect.WriteSlot(slotName,
                            (int)DelegateMode.None, null, "", "", "");

                    hn.UpdateSource(en.SourceEffect, newSlotIdx);
                    if (!compatible)
                        hn.LoadFromEffect(en.SourceEffect, slotName);
                    SaveHookPos(en.SourceEffect, newSlotIdx, hn.GetPosition().position);
                    en.RefreshHookBadge(newSlotIdx);
                    if (en.StripIndex >= 0)
                        affectedStrips |= 1u << en.StripIndex;
                }
                else if (outEl is SmoothingNode smn && inEl is EffectNode sen)
                {
                    SnapSmoothing(smn);

                    IEffect oldEffect = smn.SourceEffect;
                    if (oldEffect != null && oldEffect != sen.SourceEffect)
                    {
                        SmoothingNode.SetUseSmoothingStatic(oldEffect, false);
                        SaveSmoothingPos(oldEffect, Vector2.zero);
                        var oldNode = FindEffectNodeForEffect(oldEffect);
                        if (oldNode != null && oldNode.StripIndex >= 0)
                            affectedStrips |= 1u << oldNode.StripIndex;
                    }

                    smn.TransferToEffect(sen.SourceEffect);
                    smn.UpdateSource(sen.SourceEffect);
                    SaveSmoothingPos(sen.SourceEffect, smn.GetPosition().position);
                    if (sen.StripIndex >= 0)
                        affectedStrips |= 1u << sen.StripIndex;
                }
            }
            for (int si = 0; si < _strips.Count && si < 32; si++)
                if ((affectedStrips & (1u << si)) != 0) ApplySequence(si);
            RunValidation();
            schedule.Execute(RaiseEdges);
        }

        private void HandleElementsToRemove(List<GraphElement> elementsToRemove)
        {
            uint affectedStrips = 0;
            foreach (var el in elementsToRemove)
            {
                if (el is Edge edge)
                {
                    var outEl = FindOwnerNode(edge.output);
                    var inEl = FindOwnerNode(edge.input);
                    if (outEl is EffectNode en && inEl is HookNode hn)
                    {
                        string oldSlotName = (string)edge.output.userData;
                        int oldSlotIdx = System.Array.IndexOf(
                            HookNode.SlotNames, oldSlotName);
                        if (oldSlotIdx < 0) continue;

                        bool stillOnOldSlot =
                            hn.SourceEffect == en.SourceEffect
                            && hn.SlotIndex == oldSlotIdx;

                        if (stillOnOldSlot)
                            hn.LoadFromEffect(en.SourceEffect, oldSlotName);

                        en.SourceEffect.WriteSlot(
                            oldSlotName, (int)DelegateMode.None, null, "", "", "");
                        en.SourceEffect.Reconstruct();
                        SaveHookPos(en.SourceEffect, oldSlotIdx, Vector2.zero);
                        en.RefreshHookBadge(oldSlotIdx);

                        if (stillOnOldSlot)
                            hn.UpdateSource(null, -1);

                        if (en.StripIndex >= 0)
                            affectedStrips |= 1u << en.StripIndex;
                    }
                    else if (outEl is SmoothingNode smn && inEl is EffectNode sen)
                    {
                        bool stillOnOld = smn.SourceEffect == sen.SourceEffect;

                        SmoothingNode.SetUseSmoothingStatic(sen.SourceEffect, false);
                        SaveSmoothingPos(sen.SourceEffect, Vector2.zero);

                        if (stillOnOld)
                            smn.UpdateSource(null);

                        if (sen.StripIndex >= 0)
                            affectedStrips |= 1u << sen.StripIndex;
                    }
                }
            }
            for (int si = 0; si < _strips.Count && si < 32; si++)
                if ((affectedStrips & (1u << si)) != 0) ApplySequence(si);
            if (affectedStrips != 0) RunValidation();
        }

        private void SnapNode(EffectNode node)
        {
            float cx = node.GetPosition().x + EffectNode.NodeW * 0.5f;
            float cy = node.GetPosition().y + EffectNode.NodeH * 0.5f;

            int prevSi = node.StripIndex, prevSlot = node.SlotIndex;
            if (prevSi >= 0 && prevSlot >= 0 &&
                prevSi < _occ.Count && prevSlot < _occ[prevSi].Count &&
                _occ[prevSi][prevSlot] == node)
                _occ[prevSi][prevSlot] = null;

            int bestSi = -1, bestSlot = -1;
            float bestD = float.MaxValue;

            for (int si = 0; si < _strips.Count; si++)
            {
                float sCY = _strips[si].GetSlotCanvasY() + FilmStripElement.NodeAreaH * 0.5f;
                if (Mathf.Abs(cy - sCY) > SnapYThreshold) continue;

                if (si < _anim.Sequences.Count)
                {
                    var sp = _anim.Sequences[si].Property;
                    if (sp != null && sp.EffectCount > 0
                        && !PropertyMatchesEffect(sp, node.SourceEffect))
                    {
                        _window?.SetMessage(
                            $"Cannot add {node.SourceEffect.ValueType} effect to {_anim.Sequences[si].Type} sequence - remove existing effects first to change type.",
                            SequenceEditorWindow.MessageSeverity.Error, 10f);
                        continue;
                    }
                }

                for (int slot = 0; slot < _occ[si].Count; slot++)
                {
                    if (IsNodeAtSlot(si, slot)) continue;
                    float d = Mathf.Abs(cx - _strips[si].GetSlotCenter(slot).x);
                    if (d > SnapXThreshold) continue;
                    if (d < bestD) { bestD = d; bestSi = si; bestSlot = slot; }
                }
            }

            if (bestSi >= 0)
            {
                node.StripIndex = bestSi; node.SlotIndex = bestSlot;
                _occ[bestSi][bestSlot] = node;
                Vector2 np = NodeCanvasPos(bestSi, bestSlot);
                node.SetPosition(new Rect(np, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                _settled[node] = np;
                node.SetFloating(false);
            }
            else
            {
                node.StripIndex = -1; node.SlotIndex = -1;
                _settled[node] = node.GetPosition().position;
                node.SetFloating(true);
            }

            if (prevSi >= 0) RebuildStrip(prevSi);
            if (bestSi >= 0 && bestSi != prevSi) RebuildStrip(bestSi);

            if (prevSi >= 0) ApplySequence(prevSi);
            if (bestSi >= 0 && bestSi != prevSi) ApplySequence(bestSi);

            RunValidation();
        }

        private void AssignNodeFromPosition(EffectNode node, Vector2 pos)
        {
            float cx = pos.x + EffectNode.NodeW * 0.5f;
            float cy = pos.y + EffectNode.NodeH * 0.5f;
            int bestSi = -1, bestSlot = -1;
            float bestD = float.MaxValue;

            for (int si = 0; si < _strips.Count; si++)
            {
                float sCY = _strips[si].GetSlotCanvasY() + FilmStripElement.NodeAreaH * 0.5f;
                if (Mathf.Abs(cy - sCY) > SnapYThreshold) continue;
                for (int slot = 0; slot < _occ[si].Count; slot++)
                {
                    if (IsNodeAtSlot(si, slot)) continue;
                    float d = Mathf.Abs(cx - _strips[si].GetSlotCenter(slot).x);
                    if (d > SnapXThreshold) continue;
                    if (d < bestD) { bestD = d; bestSi = si; bestSlot = slot; }
                }
            }

            if (bestSi >= 0)
            {
                node.StripIndex = bestSi; node.SlotIndex = bestSlot;
                _occ[bestSi][bestSlot] = node;
                Vector2 np = NodeCanvasPos(bestSi, bestSlot);
                node.SetPosition(new Rect(np, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                _settled[node] = np;
                node.SetFloating(false);
            }
            else
            {
                node.StripIndex = -1; node.SlotIndex = -1;
                node.SetPosition(new Rect(pos, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                _settled[node] = pos;
                node.SetFloating(true);
            }
        }

        private void PollDragFeedback()
        {
            EffectNode dragging = FindDraggingNode();
            HookNode draggingHook = dragging == null ? FindDraggingHook() : null;
            SmoothingNode draggingSmoothing = (dragging == null && draggingHook == null)
               ? FindDraggingSmoothing() : null;
            LoopNode draggingLoop = (dragging == null && draggingHook == null && draggingSmoothing == null)
               ? FindDraggingLoopNode() : null;

            TentativeSlot(dragging, out int tentSi, out int tentSlot);

            int loopTentSi = -1, loopTentSlot = -1;
            if (draggingLoop != null)
                TentativeLoopSlot(draggingLoop, out loopTentSi, out loopTentSlot);

            for (int si = 0; si < _strips.Count; si++)
            {
                int highlightSlot = -1;
                bool denied = false;
                if (si == tentSi)
                {
                    highlightSlot = tentSlot;
                    if (dragging != null && _anim != null && si < _anim.Sequences.Count)
                    {
                        var sp = _anim.Sequences[si].Property;
                        denied = sp != null && sp.EffectCount > 0
                            && !PropertyMatchesEffect(sp, dragging.SourceEffect);
                    }
                }
                else if (si == loopTentSi) highlightSlot = loopTentSlot;
                _strips[si].SetTargetSlot(highlightSlot, denied);
            }

            if (draggingHook != null || draggingSmoothing != null)
            {
                int freeSi = -1, freePocket = -1;
                bool freeIsTop = false;
                if (draggingHook != null)
                {
                    float hcx = draggingHook.GetPosition().center.x;
                    float hcy = draggingHook.GetPosition().center.y;
                    FindFreePocket(hcx, hcy, draggingHook, DockRow.Bottom, out freeSi, out freePocket, out freeIsTop);
                }
                else
                {
                    float scx = draggingSmoothing.GetPosition().center.x;
                    float scy = draggingSmoothing.GetPosition().center.y;
                    FindFreePocket(scx, scy, draggingSmoothing, DockRow.Top, out freeSi, out freePocket, out freeIsTop);
                }

                for (int si = 0; si < _strips.Count; si++)
                {
                    int botPocket = -1;
                    int topPocket = -1;

                    if (si == freeSi)
                    {
                        if (freeIsTop) topPocket = freePocket;
                        else botPocket = freePocket;
                    }

                    _strips[si].SetTargetBottomPocket(botPocket);
                    _strips[si].SetTargetTopPocket(topPocket);
                }
            }
            else
            {
                foreach (var s in _strips)
                {
                    s.SetTargetBottomPocket(-1);
                    s.SetTargetTopPocket(-1);
                }
            }

            for (int si = 0; si < _strips.Count; si++)
            {
                var onStrip = GetNodesByPos(si);
                for (int i = 0; i < onStrip.Count; i++)
                    onStrip[i].SetOrdinal(i + 1, isDragging: onStrip[i] == dragging);
            }

            foreach (var n in _nodes)
                if (n.SlotIndex < 0 && n != dragging) n.SetFloating(true);

            if (draggingLoop != null) draggingLoop.SetOrdinal(isDragging: true);

            foreach (var ln in _loopNodes)
                if (ln.SlotIndex < 0 && ln != draggingLoop) ln.SetFloating(true);
        }

        private EffectNode FindDraggingNode()
        {
            foreach (var n in _nodes)
            {
                Vector2 cur = n.GetPosition().position;
                Vector2 expected;
                if (n.SlotIndex >= 0 && n.StripIndex >= 0 &&
                    n.StripIndex < _strips.Count &&
                    n.SlotIndex < _occ[n.StripIndex].Count)
                    expected = NodeCanvasPos(n.StripIndex, n.SlotIndex);
                else if (_settled.TryGetValue(n, out var s))
                    expected = s;
                else continue;
                float dx = cur.x - expected.x, dy = cur.y - expected.y;
                if (dx * dx + dy * dy > 4f) return n;
            }
            return null;
        }

        private HookNode FindDraggingHook()
        {
            foreach (var h in _hookNodes)
            {
                if (!_hookSettled.TryGetValue(h, out var expected)) continue;
                Vector2 cur = h.GetPosition().position;
                float dx = cur.x - expected.x, dy = cur.y - expected.y;
                if (dx * dx + dy * dy > 4f) return h;
            }
            return null;
        }

        private void SnapHook(HookNode hn)
        {
            float hcx = hn.GetPosition().center.x;
            float hcy = hn.GetPosition().center.y;
            if (float.IsNaN(hcx) || float.IsNaN(hcy))
            {
                if (_hookSettled.TryGetValue(hn, out var settled))
                {
                    hcx = settled.x;
                    hcy = settled.y;
                }
                else return;
            }

            if (FindFreePocket(hcx, hcy, hn, DockRow.Bottom, out int si, out int pocket, out bool top))
            {
                Vector2 pos = PocketPosition(si, pocket, top, HookNode.W, HookNode.H);
                hn.SetPosition(new Rect(pos, new Vector2(HookNode.W, HookNode.H)));
                _hookSettled[hn] = pos;
                if (hn.SourceEffect != null)
                    SaveHookPos(hn.SourceEffect, hn.SlotIndex, pos);
            }
            else
            {
                _hookSettled[hn] = hn.GetPosition().position;
                if (hn.SourceEffect != null)
                    SaveHookPos(hn.SourceEffect, hn.SlotIndex, hn.GetPosition().position);
            }
        }

        private void TentativeSlot(EffectNode dragging, out int outSi, out int outSlot)
        {
            outSi = -1; outSlot = -1;
            if (dragging == null) return;
            float cx = dragging.GetPosition().x + EffectNode.NodeW * 0.5f;
            float cy = dragging.GetPosition().y + EffectNode.NodeH * 0.5f;
            float bestD = float.MaxValue;
            for (int si = 0; si < _strips.Count; si++)
            {
                float sCY = _strips[si].GetSlotCanvasY() + FilmStripElement.NodeAreaH * 0.5f;
                if (Mathf.Abs(cy - sCY) > SnapYThreshold) continue;
                for (int slot = 0; slot < _occ[si].Count; slot++)
                {
                    if (IsNodeAtSlot(si, slot, dragging)) continue;
                    float d = Mathf.Abs(cx - _strips[si].GetSlotCenter(slot).x);
                    if (d > SnapXThreshold) continue;
                    if (d < bestD) { bestD = d; outSi = si; outSlot = slot; }
                }
            }
        }

        private static System.Type PropertyTypeToSystemType(PropertyTypes type)
        {
            switch (type)
            {
                case PropertyTypes.Float: return typeof(float);
                case PropertyTypes.Vector2: return typeof(Vector2);
                case PropertyTypes.Vector3: return typeof(Vector3);
                case PropertyTypes.Vector4: return typeof(Vector4);
                case PropertyTypes.Quaternion: return typeof(Quaternion);
                default: return typeof(Vector3);
            }
        }

        private void SaveAllNodeData(int si)
        {
            if (_anim == null || si >= _anim.Sequences.Count) return;
            Sequence seq = _anim.Sequences[si];
            int total = CountEffects(seq);
            if (total == 0) return;

            var layout = LayoutEditorAccess.GetOrCreateLayout(_anim, si);
            while (layout.effectNodePositions.Count < total) layout.effectNodePositions.Add(Vector2.zero);

            foreach (var n in _nodes)
            {
                if (n.StripIndex != si) continue;
                if (n.FlatIndex < 0 || n.FlatIndex >= total) continue;
                layout.effectNodePositions[n.FlatIndex] = n.GetPosition().position;
            }

            foreach (var hn in _hookNodes)
            {
                if (hn.SourceEffect == null || hn.SlotIndex < 0) continue;
                var en = FindEffectNodeForEffect(hn.SourceEffect);
                if (en == null || en.StripIndex != si) continue;
                LayoutEditorAccess.WriteHookPos(layout, en.FlatIndex, hn.SlotIndex, hn.GetPosition().position);
            }

            foreach (var sn in _smoothingNodes)
            {
                if (sn.SourceEffect == null) continue;
                var en = FindEffectNodeForEffect(sn.SourceEffect);
                if (en == null || en.StripIndex != si) continue;
                LayoutEditorAccess.WriteSmoothingPos(layout, en.FlatIndex, sn.GetPosition().position);
            }

            EditorUtility.SetDirty(_anim);
        }

        private static bool PropertyMatchesEffect(Property prop, IEffect effect)
        {
            if (prop is PropertyFloat) return effect.ValueType == PropertyTypes.Float;
            if (prop is PropertyVector2) return effect.ValueType == PropertyTypes.Vector2;
            if (prop is PropertyVector3) return effect.ValueType == PropertyTypes.Vector3;
            if (prop is PropertyVector4) return effect.ValueType == PropertyTypes.Vector4;
            if (prop is PropertyQuaternion) return effect.ValueType == PropertyTypes.Quaternion;
            return false;
        }

        private static int AddEffectToProperty(Property prop, IEffect effect)
        {
            if (prop.AddEffect(effect)) return prop.EffectCount - 1;
            return 0;
        }

        private Vector2 NodeCanvasPos(int si, int slot) =>
           _strips[si].GetSlotPosition(slot) + new Vector2(NodeMargin, 0f);

        private List<EffectNode> GetOrderedNodes(int si)
        {
            _scratch.Clear();
            foreach (var n in _nodes)
                if (n.StripIndex == si && n.SlotIndex >= 0) _scratch.Add(n);
            _scratch.Sort(_bySlot);
            return _scratch;
        }

        private List<EffectNode> GetNodesByPos(int si)
        {
            _scratch.Clear();
            foreach (var n in _nodes)
                if (n.StripIndex == si && n.SlotIndex >= 0) _scratch.Add(n);
            _scratch.Sort(_byPosX);
            return _scratch;
        }

        private static int CountEffects(Sequence seq)
        {
            return seq.Property != null ? seq.Property.EffectCount : 0;
        }

        private static void ClearEffectsInProperty(Property p)
        {
            if (p is PropertyFloat pf && pf.Effects != null) pf.Effects.Clear();
            else if (p is PropertyVector2 p2 && p2.Effects != null) p2.Effects.Clear();
            else if (p is PropertyVector3 p3 && p3.Effects != null) p3.Effects.Clear();
            else if (p is PropertyVector4 p4 && p4.Effects != null) p4.Effects.Clear();
            else if (p is PropertyQuaternion pq && pq.Effects != null) pq.Effects.Clear();
        }

        private static void ResizeValueLists(Sequence seq)
        {
            int maxFloat = -1, maxInt = -1, maxString = -1;
            int maxVec2 = -1, maxVec3 = -1, maxVec4 = -1, maxQuat = -1, maxRect = -1;
            if (seq.Property != null)
            {
                for (int e = 0; e < seq.Property.EffectCount; e++)
                {
                    var effect = seq.Property.GetEffect(e);
                    if (effect == null) continue;
                    TrackMaxIndex(effect, "OnStart", ref maxFloat, ref maxInt, ref maxString, ref maxVec2, ref maxVec3, ref maxVec4, ref maxQuat, ref maxRect);
                    TrackMaxIndex(effect, "OnDone", ref maxFloat, ref maxInt, ref maxString, ref maxVec2, ref maxVec3, ref maxVec4, ref maxQuat, ref maxRect);
                    TrackMaxIndex(effect, "ModifyEffectState", ref maxFloat, ref maxInt, ref maxString, ref maxVec2, ref maxVec3, ref maxVec4, ref maxQuat, ref maxRect);
                }
            }
            var sed = (ISequenceEditorData)seq;
            EnsureListSize(sed.FloatVals, maxFloat + 1);
            EnsureListSize(sed.IntVals, maxInt + 1);
            EnsureListSize(sed.StringVals, maxString + 1);
            EnsureListSize(sed.Vector2Vals, maxVec2 + 1);
            EnsureListSize(sed.Vector3Vals, maxVec3 + 1);
            EnsureListSize(sed.Vector4Vals, maxVec4 + 1);
            EnsureListSize(sed.QuaternionVals, maxQuat + 1);
            EnsureListSize(sed.RectVals, maxRect + 1);
        }

        private static void TrackMaxIndex(IEffect effect, string slotName,
           ref int maxFloat, ref int maxInt, ref int maxString,
           ref int maxVec2, ref int maxVec3, ref int maxVec4, ref int maxQuat, ref int maxRect)
        {
            var slot = effect.ReadValueSlot(slotName);
            if (slot.IsNone) return;
            switch (slot.Type)
            {
                case ValueSlotType.Float: if (slot.Index > maxFloat) maxFloat = slot.Index; break;
                case ValueSlotType.Int: if (slot.Index > maxInt) maxInt = slot.Index; break;
                case ValueSlotType.String: if (slot.Index > maxString) maxString = slot.Index; break;
                case ValueSlotType.Vector2: if (slot.Index > maxVec2) maxVec2 = slot.Index; break;
                case ValueSlotType.Vector3: if (slot.Index > maxVec3) maxVec3 = slot.Index; break;
                case ValueSlotType.Vector4: if (slot.Index > maxVec4) maxVec4 = slot.Index; break;
                case ValueSlotType.Quaternion: if (slot.Index > maxQuat) maxQuat = slot.Index; break;
                case ValueSlotType.Rect: if (slot.Index > maxRect) maxRect = slot.Index; break;
            }
        }

        private static void EnsureListSize<T>(List<T> list, int minSize)
        {
            if (list == null || minSize <= 0) return;
            while (list.Count < minSize) list.Add(default);
        }

    }
}