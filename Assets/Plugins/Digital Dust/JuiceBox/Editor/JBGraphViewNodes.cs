using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceGraphView.Nodes: Node spawning, removal, and pocket positioning for the graph view.
// ==============================================================================
namespace JuiceBox
{
    internal sealed partial class SequenceGraphView
    {

        public HookNode SpawnHookNode(Vector2 canvasPos)
        {
            var hn = new HookNode(null, -1, _theme, this);
            hn.SetPosition(new Rect(canvasPos, new Vector2(HookNode.W, HookNode.H)));
            AddElement(hn);
            _hookNodes.Add(hn);
            _hookSettled[hn] = canvasPos;
            return hn;
        }

        internal void SpawnHookNodeForSlot(IEffect effect, int slotIndex, Vector2 fallbackPos)
        {
            if (effect == null || slotIndex < 0 || slotIndex >= HookNode.SlotNames.Length) return;
            if (LoadHookPos(effect, slotIndex) != Vector2.zero) return;

            var hn = new HookNode(effect, slotIndex, _theme, this);
            AddElement(hn);
            _hookNodes.Add(hn);

            var effectNode = FindEffectNodeForEffect(effect);
            Vector2 spawnPos = fallbackPos;

            if (effectNode != null && effectNode.StripIndex >= 0
                && effectNode.StripIndex < _strips.Count)
            {
                int si = effectNode.StripIndex;
                var strip = _strips[si];
                float slotCX = effectNode.SlotIndex >= 0
                    ? strip.GetSlotCenter(effectNode.SlotIndex).x
                    : effectNode.GetPosition().x + EffectNode.NodeW * 0.5f;
                int pocket = FindFreePocket(strip, strip.GetNearestPocketIndex(slotCX), false);
                spawnPos = PocketPosition(si, pocket, false, HookNode.W, HookNode.H);
            }

            hn.SetPosition(new Rect(spawnPos, new Vector2(HookNode.W, HookNode.H)));
            _hookSettled[hn] = spawnPos;
            SaveHookPos(effect, slotIndex, spawnPos);

            if (effectNode != null)
            {
                var port = effectNode.GetOutputPort(HookNode.SlotNames[slotIndex]);
                if (port != null)
                {
                    var capturedPort = port;
                    var capturedHn = hn;
                    DeferEdgeCreation(capturedPort, capturedHn.InputPort);
                }
                effectNode.RefreshHookBadge(slotIndex);
            }

            if (_anim != null) EditorUtility.SetDirty(_anim);
        }

        public SmoothingNode SpawnSmoothingNode(Vector2 canvasPos)
        {
            var sn = new SmoothingNode(null, _theme, this);
            sn.SetPosition(new Rect(canvasPos, new Vector2(SmoothingNode.W, SmoothingNode.H)));
            AddElement(sn);
            _smoothingNodes.Add(sn);
            _smoothingSettled[sn] = canvasPos;
            return sn;
        }

        internal void RemoveSmoothingNode(SmoothingNode node)
        {
            if (node.SourceEffect != null)
            {
                node.ClearEffect(node.SourceEffect);
                SaveSmoothingPos(node.SourceEffect, Vector2.zero);
            }

            var affectedStrips = new HashSet<int>();
            foreach (var edge in edges.ToList())
            {
                if (FindOwnerNode(edge.output) != node) continue;
                if (FindOwnerNode(edge.input) is EffectNode en && en.StripIndex >= 0)
                    affectedStrips.Add(en.StripIndex);
                if (edge.input != null) edge.input.Disconnect(edge);
                if (edge.output != null) edge.output.Disconnect(edge);
                RemoveElement(edge);
            }

            _smoothingNodes.Remove(node);
            _smoothingSettled.Remove(node);
            RemoveElement(node);

            foreach (int si in affectedStrips)
                ApplySequence(si);

            RunValidation();
            schedule.Execute(RaiseEdges);
        }

        internal SmoothingNode FindSmoothingNodeForEffect(IEffect effect)
        {
            foreach (var sn in _smoothingNodes)
                if (sn.SourceEffect == effect) return sn;
            return null;
        }

        private void SpawnSmoothingNodeForEffectNode(EffectNode effectNode)
        {
            IEffect effect = effectNode.SourceEffect;
            if (!SmoothingNode.EffectSupportsSmoothing(effect)) return;
            if (_anim == null || effectNode.StripIndex < 0) return;

            var layout = LayoutEditorAccess.GetOrCreateLayout(_anim, effectNode.StripIndex);
            Vector2 pos = LayoutEditorAccess.ReadSmoothingPos(layout, effectNode.FlatIndex);
            bool hasSmoothing = HasSmoothingEnabled(effect);

            if (pos == Vector2.zero && !hasSmoothing) return;
            if (pos == Vector2.zero)
                pos = ComputeDefaultSmoothingPos(effectNode);

            var sn = new SmoothingNode(effect, _theme, this);
            sn.SetPosition(new Rect(pos, new Vector2(SmoothingNode.W, SmoothingNode.H)));
            AddElement(sn);
            _smoothingNodes.Add(sn);
            _smoothingSettled[sn] = pos;

            var port = effectNode.GetOutputPort("Smoothing");
            if (port != null)
            {
                DeferEdgeCreation(sn.OutputPort, port);
            }
        }

        private void SnapSmoothing(SmoothingNode sn)
        {
            float cx = sn.GetPosition().center.x;
            float cy = sn.GetPosition().center.y;
            if (float.IsNaN(cx) || float.IsNaN(cy))
            {
                if (_smoothingSettled.TryGetValue(sn, out var settled))
                {
                    cx = settled.x;
                    cy = settled.y;
                }
                else return;
            }

            if (FindFreePocket(cx, cy, sn, DockRow.Top, out int si, out int pocket, out bool top))
            {
                Vector2 pos = PocketPosition(si, pocket, top, SmoothingNode.W, SmoothingNode.H);
                sn.SetPosition(new Rect(pos, new Vector2(SmoothingNode.W, SmoothingNode.H)));
                _smoothingSettled[sn] = pos;
                if (sn.SourceEffect != null)
                    SaveSmoothingPos(sn.SourceEffect, pos);
            }
            else
            {
                _smoothingSettled[sn] = sn.GetPosition().position;
                if (sn.SourceEffect != null)
                    SaveSmoothingPos(sn.SourceEffect, sn.GetPosition().position);
            }
        }

        private SmoothingNode FindDraggingSmoothing()
        {
            foreach (var sn in _smoothingNodes)
            {
                if (!_smoothingSettled.TryGetValue(sn, out var expected)) continue;
                Vector2 cur = sn.GetPosition().position;
                float dx = cur.x - expected.x, dy = cur.y - expected.y;
                if (dx * dx + dy * dy > 4f) return sn;
            }
            return null;
        }

        private static float TopRowCentreY(FilmStripElement strip)
            => strip.GetPosition().y + FilmStripElement.PerfEdgeInset + FilmStripElement.PerfH * 0.5f;

        private static float BottomRowCentreY(FilmStripElement strip)
            => strip.GetPosition().y + FilmStripElement.StripTotalHeight
               - FilmStripElement.PerfH * 0.5f - FilmStripElement.PerfEdgeInset;

        private static float TopRowDockY(FilmStripElement strip)
            => strip.GetPosition().y + FilmStripElement.PerfEdgeInset;

        private static float BottomRowDockY(FilmStripElement strip)
            => strip.GetPosition().y + FilmStripElement.StripTotalHeight
               - FilmStripElement.PerfH - FilmStripElement.PerfEdgeInset;

        private bool IsPocketOccupied(FilmStripElement strip, int pocketIndex,
           bool isTop, GraphElement exclude = null)
        {
            float cx = strip.GetPocketCentreX(pocketIndex);
            float threshold = HookNode.W * 0.5f;
            float rowCentreY = isTop ? TopRowCentreY(strip) : BottomRowCentreY(strip);
            const float yEpsilon = 2f;

            if (isTop)
            {
                foreach (var sn in _smoothingNodes)
                {
                    if (sn == exclude) continue;
                    if (!_smoothingSettled.TryGetValue(sn, out var pos)) continue;
                    if (Mathf.Abs(pos.y + SmoothingNode.H * 0.5f - rowCentreY) > yEpsilon) continue;
                    if (Mathf.Abs(pos.x + SmoothingNode.W * 0.5f - cx) < threshold) return true;
                }
            }
            foreach (var hn in _hookNodes)
            {
                if (hn == exclude) continue;
                if (!_hookSettled.TryGetValue(hn, out var pos)) continue;
                if (Mathf.Abs(pos.y + HookNode.H * 0.5f - rowCentreY) > yEpsilon) continue;
                if (Mathf.Abs(pos.x + HookNode.W * 0.5f - cx) < threshold) return true;
            }
            return false;
        }

        private int FindFreePocket(FilmStripElement strip, int idealIndex,
           bool isTop, GraphElement exclude = null)
        {
            int count = strip.GetPocketCount();
            if (count <= 0) return idealIndex;
            if (!IsPocketOccupied(strip, idealIndex, isTop, exclude)) return idealIndex;
            for (int offset = 1; offset < count; offset++)
            {
                int right = idealIndex + offset;
                if (right < count && !IsPocketOccupied(strip, right, isTop, exclude)) return right;
                int left = idealIndex - offset;
                if (left >= 0 && !IsPocketOccupied(strip, left, isTop, exclude)) return left;
            }
            return idealIndex;
        }

        internal enum DockRow { Top, Bottom, Both }

        private bool FindFreePocket(float cx, float cy, GraphElement exclude,
           DockRow row, out int stripIndex, out int pocketIndex, out bool isTop)
        {
            stripIndex = -1; pocketIndex = -1; isTop = false;
            float bestDy = float.MaxValue;
            const float yThreshold = FilmStripElement.PerfH;
            const float xThreshold = FilmStripElement.TopPerfW * 1.5f;

            for (int si = 0; si < _strips.Count; si++)
            {
                float topDy = row == DockRow.Bottom ? float.MaxValue : Mathf.Abs(cy - TopRowCentreY(_strips[si]));
                float botDy = row == DockRow.Top ? float.MaxValue : Mathf.Abs(cy - BottomRowCentreY(_strips[si]));
                float closestDy = topDy < botDy ? topDy : botDy;
                if (closestDy > yThreshold) continue;

                float nearestPx = _strips[si].GetNearestPocketCentreX(cx);
                if (Mathf.Abs(cx - nearestPx) > xThreshold) continue;

                if (closestDy < bestDy)
                {
                    bestDy = closestDy;
                    stripIndex = si;
                    isTop = topDy < botDy;
                }
            }

            if (stripIndex < 0) return false;

            FilmStripElement strip = _strips[stripIndex];
            int nearest = strip.GetNearestPocketIndex(cx);
            pocketIndex = FindFreePocket(strip, nearest, isTop, exclude);

            float actualPx = strip.GetPocketCentreX(pocketIndex);
            if (Mathf.Abs(cx - actualPx) > xThreshold)
            {
                stripIndex = -1;
                pocketIndex = -1;
                return false;
            }

            return true;
        }

        private Vector2 PocketPosition(int si, int pocket, bool isTop, float nodeW, float nodeH)
        {
            FilmStripElement strip = _strips[si];
            float px = strip.GetPocketCentreX(pocket);
            float rowY = isTop ? TopRowDockY(strip) : BottomRowDockY(strip);
            return new Vector2(
                px - nodeW * 0.5f,
                rowY + FilmStripElement.PerfH * 0.5f - nodeH * 0.5f);
        }

        private LoopNode GetLoopNodeForStrip(int si)
        {
            foreach (var ln in _loopNodes)
                if (ln.StripIndex == si) return ln;
            return null;
        }

        private void SpawnLoopNodesForStrip(int si)
        {
            if (_anim == null || si >= _anim.Sequences.Count) return;
            var loopLayout = LayoutEditorAccess.GetOrCreateLayout(_anim, si);
            if (loopLayout.loopNodes == 0) return;

            for (int slot = 0; slot < 32; slot++)
            {
                if ((loopLayout.loopNodes & (1u << slot)) == 0) continue;

                var ln = new LoopNode(si, _theme, this);
                ln.SlotIndex = slot;
                ln.StripIndex = si;
                Vector2 pos = NodeCanvasPos(si, slot);
                ln.SetPosition(new Rect(pos, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                AddElement(ln);
                _loopNodes.Add(ln);
                _loopSettled[ln] = pos;
                ln.SetFloating(false);
                break;
            }
        }

        private void AddLoopNode(int stripHint, Vector2 canvasPos)
        {
            if (_anim == null) return;

            if (stripHint < 0)
            {
                var fln = new LoopNode(-1, _theme, this);
                fln.SlotIndex = -1;
                fln.SetPosition(new Rect(canvasPos, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                AddElement(fln);
                _loopNodes.Add(fln);
                _loopSettled[fln] = canvasPos;
                fln.SetFloating(true);
                EditorUtility.SetDirty(_anim);
                return;
            }

            int si = stripHint;
            if (si < 0 || si >= _strips.Count) return;
            if (GetLoopNodeForStrip(si) != null) return;

            var ln = new LoopNode(si, _theme, this);
            AddElement(ln);
            _loopNodes.Add(ln);

            int bestSlot = FindNearestLoopSlot(si, canvasPos);
            if (bestSlot >= 0)
            {
                ln.SlotIndex = bestSlot;
                Vector2 np = NodeCanvasPos(si, bestSlot);
                ln.SetPosition(new Rect(np, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                _loopSettled[ln] = np;
                ln.SetFloating(false);
            }
            else
            {
                ln.SlotIndex = -1;
                ln.SetPosition(new Rect(canvasPos, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                _loopSettled[ln] = canvasPos;
                ln.SetFloating(true);
            }

            RebuildStrip(si);
            ApplySequence(si);
            RunValidation();
            EditorUtility.SetDirty(_anim);
        }

        private int FindNearestLoopSlot(int si, Vector2 canvasPos)
        {
            float cx = canvasPos.x;
            float cy = canvasPos.y;
            float sCY = _strips[si].GetSlotCanvasY() + FilmStripElement.NodeAreaH * 0.5f;
            if (Mathf.Abs(cy - sCY) > SnapYThreshold) return -1;

            int bestSlot = -1;
            float bestD = float.MaxValue;
            int slotCheck = Mathf.Min(_occ[si].Count + 2, MaxSlots);
            for (int slot = 0; slot < slotCheck; slot++)
            {
                if (IsNodeAtSlot(si, slot)) continue;
                float d = Mathf.Abs(cx - _strips[si].GetSlotCenter(slot).x);
                if (d > SnapXThreshold) continue;
                if (d < bestD) { bestD = d; bestSlot = slot; }
            }
            return bestSlot;
        }

        internal void RemoveLoopNode(LoopNode node)
        {
            int si = node.StripIndex;
            _loopNodes.Remove(node);
            _loopSettled.Remove(node);
            RemoveElement(node);

            if (si >= 0)
            {
                RebuildStrip(si);
                ApplySequence(si);
            }
            RunValidation();
            _window?.UpdateScrollbars();
        }

        private void SnapLoopNode(LoopNode node)
        {
            float cx = node.GetPosition().x + EffectNode.NodeW * 0.5f;
            float cy = node.GetPosition().y + EffectNode.NodeH * 0.5f;
            if (float.IsNaN(cx) || float.IsNaN(cy))
            {
                if (_loopSettled.TryGetValue(node, out var settled))
                {
                    cx = settled.x;
                    cy = settled.y;
                }
                else return;
            }

            int prevSi = node.StripIndex;
            int bestSi = -1, bestSlot = -1;
            float bestD = float.MaxValue;

            for (int si = 0; si < _strips.Count; si++)
            {
                float sCY = _strips[si].GetSlotCanvasY() + FilmStripElement.NodeAreaH * 0.5f;
                if (Mathf.Abs(cy - sCY) > SnapYThreshold) continue;

                int slotCheck = Mathf.Min(_occ[si].Count + 2, MaxSlots);
                for (int slot = 0; slot < slotCheck; slot++)
                {
                    if (IsNodeAtSlot(si, slot, node)) continue;

                    float d = Mathf.Abs(cx - _strips[si].GetSlotCenter(slot).x);
                    if (d > SnapXThreshold) continue;
                    if (d < bestD) { bestD = d; bestSi = si; bestSlot = slot; }
                }
            }

            node.StripIndex = bestSi;
            node.SlotIndex = bestSi >= 0 ? bestSlot : -1;

            if (bestSi >= 0)
            {
                Vector2 np = NodeCanvasPos(bestSi, bestSlot);
                node.SetPosition(new Rect(np, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                _loopSettled[node] = np;
                node.SetFloating(false);
            }
            else
            {
                _loopSettled[node] = node.GetPosition().position;
                node.SetFloating(true);
            }

            if (prevSi >= 0) RebuildStrip(prevSi);
            if (bestSi >= 0 && bestSi != prevSi) RebuildStrip(bestSi);

            if (prevSi >= 0) ApplySequence(prevSi);
            if (bestSi >= 0 && bestSi != prevSi) ApplySequence(bestSi);

            RunValidation();
            _window?.UpdateScrollbars();
        }

        private LoopNode FindDraggingLoopNode()
        {
            foreach (var ln in _loopNodes)
            {
                if (!_loopSettled.TryGetValue(ln, out var expected)) continue;
                Vector2 cur = ln.GetPosition().position;
                float dx = cur.x - expected.x, dy = cur.y - expected.y;
                if (dx * dx + dy * dy > 4f) return ln;
            }
            return null;
        }

        private void TentativeLoopSlot(LoopNode dragging, out int outSi, out int outSlot)
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
                int slotCheck = Mathf.Min(_occ[si].Count + 2, MaxSlots);
                for (int slot = 0; slot < slotCheck; slot++)
                {
                    if (IsNodeAtSlot(si, slot, dragging)) continue;
                    float d = Mathf.Abs(cx - _strips[si].GetSlotCenter(slot).x);
                    if (d > SnapXThreshold) continue;
                    if (d < bestD) { bestD = d; outSi = si; outSlot = slot; }
                }
            }
        }

        private void UpdateLoopArrow(int si)
        {
            if (si < 0 || si >= _strips.Count) return;
            FilmStripElement strip = _strips[si];

            int arrowCount = strip.SlotCount - 1;
            for (int i = 0; i < arrowCount; i++)
                strip.SetLoopArrow(i, false);

            if (_anim == null || si >= _anim.Sequences.Count) return;
            if (_anim.Sequences[si].LoopMode == LoopMode.None) return;

            LoopNode ln = GetLoopNodeForStrip(si);
            if (ln == null || ln.SlotIndex < 0) return;

            strip.SetLoopArrow(ln.SlotIndex, true);
        }

        private Vector2 LoadHookPos(IEffect effect, int slotIndex)
        {
            if (_anim == null || effect == null) return Vector2.zero;
            var en = FindEffectNodeForEffect(effect);
            if (en == null || en.StripIndex < 0) return Vector2.zero;
            var layout = LayoutEditorAccess.GetOrCreateLayout(_anim, en.StripIndex);
            return LayoutEditorAccess.ReadHookPos(layout, en.FlatIndex, slotIndex);
        }

        private void SaveHookPos(IEffect effect, int slotIndex, Vector2 pos)
        {
            if (_anim == null || effect == null) return;
            var en = FindEffectNodeForEffect(effect);
            if (en == null || en.StripIndex < 0) return;
            var layout = LayoutEditorAccess.GetOrCreateLayout(_anim, en.StripIndex);
            LayoutEditorAccess.WriteHookPos(layout, en.FlatIndex, slotIndex, pos);
        }

        private void SaveSmoothingPos(IEffect effect, Vector2 pos)
        {
            if (_anim == null || effect == null) return;
            var en = FindEffectNodeForEffect(effect);
            if (en == null || en.StripIndex < 0) return;
            var layout = LayoutEditorAccess.GetOrCreateLayout(_anim, en.StripIndex);
            LayoutEditorAccess.WriteSmoothingPos(layout, en.FlatIndex, pos);
        }

        private Vector2 ComputeDefaultHookPos(EffectNode effectNode, int slotIndex)
        {
            if (effectNode.StripIndex < 0 || effectNode.StripIndex >= _strips.Count)
                return new Vector2(
                   effectNode.GetPosition().x + EffectNode.NodeW * 0.5f - HookNode.W * 0.5f,
                   effectNode.GetPosition().yMax + 4f);

            int si = effectNode.StripIndex;
            var strip = _strips[si];
            float slotCX = effectNode.SlotIndex >= 0
                ? strip.GetSlotCenter(effectNode.SlotIndex).x
                : effectNode.GetPosition().x + EffectNode.NodeW * 0.5f;

            int nearestPocket = strip.GetNearestPocketIndex(slotCX);
            int pocketCount = strip.GetPocketCount();

            int idealPocket = slotIndex <= 1
                ? Mathf.Max(0, nearestPocket - 1)
                : Mathf.Min(pocketCount - 1, nearestPocket + 1);

            int pocket = FindFreePocket(strip, idealPocket, false);
            return PocketPosition(si, pocket, false, HookNode.W, HookNode.H);
        }

        private Vector2 ComputeDefaultSmoothingPos(EffectNode effectNode)
        {
            if (effectNode.StripIndex < 0 || effectNode.StripIndex >= _strips.Count)
                return new Vector2(
                   effectNode.GetPosition().x + EffectNode.NodeW * 0.5f - SmoothingNode.W * 0.5f,
                   effectNode.GetPosition().y - SmoothingNode.H - 4f);

            int si = effectNode.StripIndex;
            var strip = _strips[si];
            float slotCX = effectNode.SlotIndex >= 0
                ? strip.GetSlotCenter(effectNode.SlotIndex).x
                : effectNode.GetPosition().x + EffectNode.NodeW * 0.5f;

            int pocket = FindFreePocket(strip, strip.GetNearestPocketIndex(slotCX), true);
            return PocketPosition(si, pocket, true, SmoothingNode.W, SmoothingNode.H);
        }

        private static bool HasSmoothingEnabled(IEffect effect)
            => effect is IUseSmoothing ius && ius.UseSmoothing;

        private static bool SlotsCompatible(int a, int b)
            => (a <= 1) == (b <= 1);

        internal void RefreshHookNodeForSlot(IEffect effect, int slotIndex)
        {
            foreach (var hn in _hookNodes)
                if (hn.SourceEffect == effect && hn.SlotIndex == slotIndex)
                {
                    hn.LoadFromEffect(effect, hn.SlotName);
                    hn.RefreshDisplay();
                }
        }

        private void SpawnNodesForStrip(int si)
        {
            if (_anim == null || si >= _anim.Sequences.Count) return;
            Sequence seq = _anim.Sequences[si];
            if (seq.Property == null) return;

            var layout = LayoutEditorAccess.GetOrCreateLayout(_anim, si);
            int nextSlot = 0;
            int flatIndex = 0;

            while (_occ[si].Count < FilmStripElement.MinSlots) _occ[si].Add(null);

            if (seq.Property != null)
            {
                int effectCount = seq.Property.EffectCount;
                for (int ei = 0; ei < effectCount; ei++)
                {
                    IEffect effect = seq.Property.GetEffect(ei);
                    if (effect == null) continue;
                    var node = new EffectNode(effect, si, -1, flatIndex, _anim, _theme, this);
                    AddElement(node);
                    _nodes.Add(node);

                    bool hasSaved = flatIndex < layout.effectNodePositions.Count
                       && layout.effectNodePositions[flatIndex] != Vector2.zero;

                    Vector2 startPos = hasSaved
                       ? layout.effectNodePositions[flatIndex]
                       : NodeCanvasPos(si, nextSlot++);

                    if (!hasSaved)
                        while (_occ[si].Count <= nextSlot - 1) _occ[si].Add(null);

                    AssignNodeFromPosition(node, startPos);
                    flatIndex++;
                }
            }

            foreach (var effectNode in _nodes)
                if (effectNode.StripIndex == si)
                {
                    SpawnHookNodesForEffectNode(effectNode);
                    SpawnSmoothingNodeForEffectNode(effectNode);
                }
        }

        private void SpawnHookNodesForEffectNode(EffectNode effectNode)
        {
            IEffect effect = effectNode.SourceEffect;
            if (_anim == null || effectNode.StripIndex < 0) return;

            var hookLayout = LayoutEditorAccess.GetOrCreateLayout(_anim, effectNode.StripIndex);

            for (int si = 0; si < HookNode.SlotNames.Length; si++)
            {
                bool hasDelegate = effect.ReadSlot(HookNode.SlotNames[si]).mode != 0;
                bool hasValueSlot = !effect.ReadValueSlot(HookNode.SlotNames[si]).IsNone;
                if (!hasDelegate && !hasValueSlot) continue;

                Vector2 hookPos = LayoutEditorAccess.ReadHookPos(hookLayout, effectNode.FlatIndex, si);
                if (hookPos == Vector2.zero)
                    hookPos = ComputeDefaultHookPos(effectNode, si);

                var hn = new HookNode(effect, si, _theme, this);
                hn.SetPosition(new Rect(hookPos, new Vector2(HookNode.W, HookNode.H)));
                AddElement(hn);
                _hookNodes.Add(hn);
                _hookSettled[hn] = hookPos;

                var port = effectNode.GetOutputPort(HookNode.SlotNames[si]);
                if (port != null)
                {
                    DeferEdgeCreation(port, hn.InputPort);
                }
            }
        }

        internal void RebuildStripNodes(int si)
        {
            var effectsInStrip = new HashSet<IEffect>();
            foreach (var n in _nodes)
                if (n.StripIndex == si) effectsInStrip.Add(n.SourceEffect);

            for (int i = _hookNodes.Count - 1; i >= 0; i--)
            {
                var hn = _hookNodes[i];
                if (!effectsInStrip.Contains(hn.SourceEffect)) continue;
                foreach (var edge in edges.ToList())
                    if (FindOwnerNode(edge.input) == hn || FindOwnerNode(edge.output) == hn)
                        RemoveElement(edge);
                RemoveElement(hn);
                _hookSettled.Remove(hn);
                _hookNodes.RemoveAt(i);
            }

            for (int i = _smoothingNodes.Count - 1; i >= 0; i--)
            {
                var sn = _smoothingNodes[i];
                if (!effectsInStrip.Contains(sn.SourceEffect)) continue;
                foreach (var edge in edges.ToList())
                    if (FindOwnerNode(edge.input) == sn || FindOwnerNode(edge.output) == sn)
                        RemoveElement(edge);
                RemoveElement(sn);
                _smoothingSettled.Remove(sn);
                _smoothingNodes.RemoveAt(i);
            }

            for (int i = _loopNodes.Count - 1; i >= 0; i--)
            {
                if (_loopNodes[i].StripIndex != si) continue;
                RemoveElement(_loopNodes[i]);
                _loopSettled.Remove(_loopNodes[i]);
                _loopNodes.RemoveAt(i);
            }

            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                if (_nodes[i].StripIndex != si) continue;
                RemoveElement(_nodes[i]);
                _nodes.RemoveAt(i);
            }

            for (int s = 0; s < _occ[si].Count; s++) _occ[si][s] = null;

            SpawnNodesForStrip(si);
            SpawnLoopNodesForStrip(si);
            RebuildStrip(si);
            ApplySequence(si);
            RunValidation();
            schedule.Execute(RaiseEdges);
        }

    }
}