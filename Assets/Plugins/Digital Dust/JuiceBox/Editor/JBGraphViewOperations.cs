using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceGraphView.Operations: High-level operations: add/remove sequences, rename, reorder, context menus.
// ==============================================================================
namespace JuiceBox
{
    internal sealed partial class SequenceGraphView
    {

        private void RebuildStrip(int si)
        {
            if (si < 0 || si >= _strips.Count) return;
            var occ = _occ[si];
            int lastOcc = -1;
            for (int i = 0; i < occ.Count; i++) if (occ[i] != null) lastOcc = i;
            int needed = Mathf.Max(FilmStripElement.MinSlots, lastOcc + 2);

            for (int li = 0; li < _loopNodes.Count; li++)
            {
                var ln = _loopNodes[li];
                if (ln.StripIndex == si && ln.SlotIndex >= 0)
                    needed = Mathf.Max(needed, ln.SlotIndex + 2);
            }

            needed = Mathf.Min(needed, MaxSlots);

            while (occ.Count < needed) occ.Add(null);
            while (occ.Count > needed && occ[occ.Count - 1] == null)
                occ.RemoveAt(occ.Count - 1);
            _strips[si].Resize(occ.Count);

            int runLen = ComputeRunLength(si);
            LoopNode activeLn = GetLoopNodeForStrip(si);
            if (activeLn != null && activeLn.SlotIndex >= 0
                && _anim != null && si < _anim.Sequences.Count
                && _anim.Sequences[si].LoopMode != LoopMode.None)
                runLen = Mathf.Max(runLen, activeLn.SlotIndex + 1);
            _strips[si].UpdateRunIndicator(runLen);
            UpdateLoopArrow(si);
            _window?.UpdateScrollbars();
        }

        private int ComputeRunLength(int si)
        {
            for (int i = 0; i < _occ[si].Count; i++)
                if (_occ[si][i] == null) return i;
            return _occ[si].Count;
        }

        private void OnTriggersChanged(int si, int newFlags)
        {
            if (_anim == null || si >= _anim.Sequences.Count) return;
            Undo.RecordObject(_anim, "Change Triggers");
            Sequence seq = _anim.Sequences[si];
            seq.Triggers = (TriggerMode)newFlags;
            EditorUtility.SetDirty(_anim);
            SequenceLibrary.PropagateTriggers(seq.Name, seq, seq.Triggers, _anim);
        }

        private void OnSegmentChanged(int si, int newSegment)
        {
            if (_anim == null || si >= _anim.Sequences.Count) return;
            Undo.RecordObject(_anim, "Change Segment");
            Sequence seq = _anim.Sequences[si];
            seq.Segment = (MEC.Segment)newSegment;
            EditorUtility.SetDirty(_anim);
            if (JuiceBoxCentralController.IsSequenceRunning(seq))
                JuiceBoxCentralController.Instance.SetSegment(seq, seq.Segment);
            SequenceLibrary.PropagateSegment(seq.Name, seq, seq.Segment, _anim);
        }

        private void OnSequenceNameChanged(int si, string newName)
        {
            if (_anim == null || si >= _anim.Sequences.Count) return;

            string nameError = SequenceBackupManager.ValidateSequenceName(newName);
            if (nameError != null)
            {
                _window?.SetMessage(nameError, SequenceEditorWindow.MessageSeverity.Warning);
                return;
            }

            if (_renameStripIndex >= 0 && _renameStripIndex != si)
                FlushPendingRename(_renameStripIndex);

            if (_pendingNewName == null || _renameStripIndex != si)
            {
                _renameStripIndex = si;
                Undo.RecordObject(_anim, "Rename Sequence");
            }

            _pendingNewName = newName;
            _window?.SetMessage("", SequenceEditorWindow.MessageSeverity.Warning);

            _renameSchedule?.Pause();
            _renameSchedule = schedule.Execute(() => FlushPendingRename(si)).StartingIn(10000);
        }

        private void FlushPendingRename(int si)
        {
            if (_anim == null || _pendingNewName == null) return;
            if (si >= _anim.Sequences.Count) return;

            string newName = _pendingNewName;
            if (string.IsNullOrWhiteSpace(newName)) newName = "";
            string name = _anim.Sequences[si].Name;

            _pendingNewName = null;
            _renameStripIndex = -1;
            _renameSchedule?.Pause();
            _renameSchedule = null;

            if (name == newName) return;

            int newNameRefs = SequenceLibrary.CountReferences(newName, _anim);
            bool hasNewBackups = SequenceBackupManager.HasBackups(newName);

            if (newNameRefs > 0 || hasNewBackups)
            {
                string source = newNameRefs > 0
                    ? $"{newNameRefs} component(s)"
                    : "backup data";
                bool load = EditorUtility.DisplayDialog(
                    "Sequence Already Exists",
                    $"A sequence named \"{newName}\" already exists ({source}). Load its data?",
                    "Load", "Cancel");

                if (!load)
                {
                    ResetStripDisplayName(si, name, null);
                    return;
                }

                string originalJson = EditorJsonUtility.ToJson(_anim.Sequences[si]);

                if (newNameRefs > 0)
                {
                    string json = null;
                    for (int i = 0; i < _anim.Sequences.Count; i++)
                    {
                        if (i == si) continue;
                        if (_anim.Sequences[i].Name == newName)
                        {
                            json = EditorJsonUtility.ToJson(_anim.Sequences[i]);
                            break;
                        }
                    }

                    if (json == null)
                        json = SequenceLibrary.GetSequenceJson(newName, _anim);

                    if (json != null)
                    {
                        EditorJsonUtility.FromJsonOverwrite(json, _anim.Sequences[si]);
                        FinalizeSerialization();
                    }
                }
                else
                {
                    var snaps = SequenceBackupManager.GetSnapshots(newName);
                    if (snaps.Count > 0)
                    {
                        string json = snaps[snaps.Count - 1].sequenceJson;
                        EditorJsonUtility.FromJsonOverwrite(json, _anim.Sequences[si]);
                        FinalizeSerialization();
                    }
                }

                _anim.Sequences[si].Name = newName;
                EditorUtility.SetDirty(_anim);

                bool hasOldBackups = SequenceBackupManager.HasBackups(name);
                if (hasOldBackups && hasNewBackups)
                {
                    bool choose = EditorUtility.DisplayDialog(
                        "Snapshot Conflict",
                        $"Both \"{name}\" and \"{newName}\" have snapshot history. " +
                        "Choose which snapshots to keep, or cancel the rename.",
                        "Choose\u2026", "Cancel Rename");

                    if (!choose)
                    {
                        EditorJsonUtility.FromJsonOverwrite(originalJson, _anim.Sequences[si]);
                        FinalizeSerialization();
                        _anim.Sequences[si].Name = name;
                        EditorUtility.SetDirty(_anim);
                        ResetStripDisplayName(si, name, null);
                        RefreshRestoreButtons();
                        _window?.Rebuild();
                        return;
                    }

                    int snapChoice = EditorUtility.DisplayDialogComplex(
                        "Choose Snapshots",
                        "Which snapshots should the renamed sequence keep? " +
                        "All snapshots from the unchosen sequence will be permanently deleted.",
                        $"Keep \"{newName}\"", "Keep Both", $"Keep \"{name}\"");

                    if (snapChoice == 0)
                    {
                        SequenceBackupManager.DeleteBackups(name);
                    }
                    else if (snapChoice == 2)
                    {
                        SequenceBackupManager.DeleteBackups(newName);
                        SequenceBackupManager.PerformFileRename(name, newName);
                    }
                }
                else if (hasOldBackups)
                {
                    SequenceBackupManager.PerformFileRename(name, newName);
                }

                RefreshRestoreButtons();
                _window?.Rebuild();
                return;
            }

            int refCount = SequenceLibrary.CountReferences(name, _anim);

            if (refCount > 1)
            {
                int choice = EditorUtility.DisplayDialogComplex(
                    "Rename Sequence",
                    $"{refCount} component(s) share the sequence \"{name}\". " +
                    "Rename everywhere or fork as an independent copy?",
                    "Rename All", "Cancel", "Fork");

                if (choice == 1)
                {
                    ResetStripDisplayName(si, name, null);
                    return;
                }

                _anim.Sequences[si].Name = newName;
                EditorUtility.SetDirty(_anim);

                if (choice == 0)
                {
                    NotifyLibraryRenamed(name, newName, _anim.Sequences[si]);
                    HandleBackupsOnRename(name, newName);
                }
            }
            else
            {
                _anim.Sequences[si].Name = newName;
                EditorUtility.SetDirty(_anim);
                NotifyLibraryRenamed(name, newName, _anim.Sequences[si]);
                HandleBackupsOnRename(name, newName);
            }

            RefreshRestoreButtons();
        }

        private void HandleBackupsOnRename(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                if (SequenceBackupManager.HasBackups(oldName))
                {
                    bool del = EditorUtility.DisplayDialog(
                        "Sequence Snapshots",
                        $"\"{oldName}\" has saved snapshots. Delete them, or keep them " +
                        "orphaned under the old name?",
                        "Delete", "Orphan");
                    if (del)
                        SequenceBackupManager.DeleteBackups(oldName);
                }
                return;
            }
            SequenceBackupManager.PerformFileRename(oldName, newName);
        }

        private void ResetStripDisplayName(int si, string name, string errorMessage)
        {
            if (si < _strips.Count)
                _strips[si].SetDisplayName(name);
            if (errorMessage != null)
            {
                Debug.LogWarning($"JuiceBox: {errorMessage}");
                _window?.SetMessage(errorMessage, SequenceEditorWindow.MessageSeverity.Warning);
            }
        }

        internal void CommitPendingRename()
        {
            if (_renameStripIndex >= 0)
                FlushPendingRename(_renameStripIndex);
        }

        internal void OnLeftCapInitialClicked(int si, bool velocity, Vector2 pos, GameObject prePickedTarget)
        {
            if (_anim == null || si < 0 || si >= _anim.Sequences.Count) return;
            var seq = _anim.Sequences[si];
            if (seq.Property == null) return;

            string slotName = "SetStartingValue";
            string undoLabel = "Set SetStartingValue";
            System.Type valueType = PropertyTypeToSystemType(seq.Type);

            Vector2 localPos = this.WorldToLocal(pos);
            DelegatePicker.Show(
                this,
                (IDelegateConnecter)seq.Property,
                slotName,
                valueType,
                localPos,
                _theme,
                () =>
                {
                    Undo.RecordObject(_anim, undoLabel);
                    EditorUtility.SetDirty(_anim);
                    SequenceLibrary.PropagatePropertySlot(seq.Name, seq, slotName, _anim);
                    RefreshLeftCapPickers(si);
                    RunValidation();
                },
                selfObject: _anim != null ? _anim.gameObject : null,
                prePickedTarget: prePickedTarget);
        }

        internal void OnLeftCapUpdateClicked(int si, Vector2 pos, GameObject prePickedTarget)
        {
            if (_anim == null || si < 0 || si >= _anim.Sequences.Count) return;
            var seq = _anim.Sequences[si];
            if (seq.Property == null) return;

            System.Type valueType = PropertyTypeToSystemType(seq.Type);

            Vector2 localPos = this.WorldToLocal(pos);
            DelegatePicker.Show(
                this,
                (IDelegateConnecter)seq.Property,
                "OnUpdate",
                valueType,
                localPos,
                _theme,
                () =>
                {
                    Undo.RecordObject(_anim, "Set OnUpdate");
                    EditorUtility.SetDirty(_anim);
                    SequenceLibrary.PropagatePropertySlot(seq.Name, seq, "OnUpdate", _anim);
                    RefreshLeftCapPickers(si);
                    RunValidation();
                },
                selfObject: _anim != null ? _anim.gameObject : null,
                prePickedTarget: prePickedTarget);
        }

        internal void RefreshLeftCapPickers(int si)
        {
            if (_anim == null || si < 0 || si >= _anim.Sequences.Count) return;
            if (si >= _strips.Count) return;

            var seq = _anim.Sequences[si];
            int updMode = 0; string updMeth = ""; string updRelDesc = "";
            int valMode = 0; string valMeth = ""; string valRelDesc = "";
            bool valueEnabled = seq.Property != null;
            bool updLive = true, valLive = true;

            if (seq.Property != null)
            {
                var container = (IDelegateConnecter)seq.Property;
                var u = container.ReadSlot("OnUpdate");
                updMode = u.mode; updMeth = u.method ?? ""; updRelDesc = u.relDesc ?? "";
                var v = container.ReadSlot("SetStartingValue");
                valMode = v.mode; valMeth = v.method ?? ""; valRelDesc = v.relDesc ?? "";

                updLive = updMode == 0 || container.GetLiveDelegate("OnUpdate") != null;
                valLive = valMode == 0 || container.GetLiveDelegate("SetStartingValue") != null;
            }

            _strips[si].UpdateLeftCapPickers(
                updMode, updMeth, updRelDesc, updLive,
                valueEnabled, valMode, valMeth, valRelDesc, valLive);
        }

        internal void RefreshRestoreButtons()
        {
            for (int si = 0; si < _strips.Count; si++)
                _strips[si].RefreshRestoreButton();
        }

        public void LoadSequence(Sequence seq)
        {
            if (_anim == null || seq == null) return;
            Undo.RecordObject(_anim, "Load Sequence");

            _anim.Sequences.Add(seq);
            LayoutEditorAccess.GetOrCreateLayout(_anim, _anim.Sequences.Count - 1);
            int si = _anim.Sequences.Count - 1;

            float y = StripOriginY + si * (FilmStripElement.StripTotalHeight + SequenceGap);
            int effectCount = CountEffects(seq);
            int initialSlots = Mathf.Max(FilmStripElement.MinSlots, effectCount + 1);

            var strip = new FilmStripElement(seq, new Vector2(StripOriginX, y),
               initialSlots, si, _anim.Sequences.Count, _theme, OnSequenceNameChanged,
               OnSegmentChanged, this, RemoveStrip, OnTriggersChanged,
               OnLeftCapInitialClicked, OnLeftCapUpdateClicked, MoveStrip);
            AddElement(strip);
            _strips.Add(strip);

            var occ = new List<EffectNode>();
            for (int s = 0; s < initialSlots; s++) occ.Add(null);
            _occ.Add(occ);

            SpawnNodesForStrip(si);
            SpawnLoopNodesForStrip(si);
            RebuildStrip(si);
            RefreshLeftCapPickers(si);

            SaveAllNodeData(si);

            RunValidation();
            schedule.Execute(RaiseEdges);
            _window?.UpdateScrollbars();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 canvasPos = contentViewContainer.WorldToLocal(evt.mousePosition);

            int hitStrip = -1;
            for (int si = 0; si < _strips.Count; si++)
            {
                if (_strips[si].GetPosition().Contains(canvasPos))
                {
                    hitStrip = si;
                    break;
                }
            }

            if (hitStrip >= 0 && hitStrip < _anim.Sequences.Count)
            {
                Sequence hitSeq = _anim.Sequences[hitStrip];
                bool hasEffects = hitSeq.Property != null && hitSeq.Property.EffectCount > 0;

                if (hasEffects)
                {
                    PropertyTypes pt = hitSeq.Type;
                    int capSi = hitStrip;
                    Vector2 capPos = canvasPos;
                    evt.menu.AppendAction($"Add Effect/Tween",
                       _ => AddNewEffect(capSi, CreateEffect(EffectKind.Tween, pt), capPos));
                    evt.menu.AppendAction($"Add Effect/Follow",
                       _ => AddNewEffect(capSi, CreateEffect(EffectKind.Follow, pt), capPos));
                    evt.menu.AppendAction($"Add Effect/Shake",
                       _ => AddNewEffect(capSi, CreateEffect(EffectKind.Shake, pt), capPos));
                }
                else
                {
                    for (int vi = 0; vi < _vtValues.Length; vi++)
                    {
                        PropertyTypes capPt = _vtValues[vi];
                        string label = _vtLabels[vi];
                        int capSi = hitStrip;
                        Vector2 capPos = canvasPos;
                        evt.menu.AppendAction($"Add Effect/Tween/{label}",
                           _ => AddNewEffect(capSi, CreateEffect(EffectKind.Tween, capPt), capPos));
                        evt.menu.AppendAction($"Add Effect/Follow/{label}",
                           _ => AddNewEffect(capSi, CreateEffect(EffectKind.Follow, capPt), capPos));
                        evt.menu.AppendAction($"Add Effect/Shake/{label}",
                           _ => AddNewEffect(capSi, CreateEffect(EffectKind.Shake, capPt), capPos));
                    }
                }
            }
            else
            {
                for (int vi = 0; vi < _vtValues.Length; vi++)
                {
                    PropertyTypes capPt = _vtValues[vi];
                    string label = _vtLabels[vi];
                    int capSi = hitStrip;
                    Vector2 capPos = canvasPos;
                    evt.menu.AppendAction($"Add Effect/Tween/{label}",
                       _ => AddNewEffect(capSi, CreateEffect(EffectKind.Tween, capPt), capPos));
                    evt.menu.AppendAction($"Add Effect/Follow/{label}",
                       _ => AddNewEffect(capSi, CreateEffect(EffectKind.Follow, capPt), capPos));
                    evt.menu.AppendAction($"Add Effect/Shake/{label}",
                       _ => AddNewEffect(capSi, CreateEffect(EffectKind.Shake, capPt), capPos));
                }
            }

            {
                int capSi = hitStrip;
                Vector2 capPos = canvasPos;
                evt.menu.AppendAction("Add Effect/Loop",
                   _ => AddLoopNode(capSi, capPos));
            }

            evt.menu.AppendSeparator();
            {
                int capSi = hitStrip;
                Vector2 capPos = canvasPos;
                evt.menu.AppendAction("Control Nodes/Hook",
                   _ => SpawnHookNode(capPos));
                evt.menu.AppendAction("Control Nodes/Smoothing",
                   _ => SpawnSmoothingNode(capPos));
            }

            if (hitStrip < 0)
            {
                evt.menu.AppendSeparator();
                Vector2 capPos = canvasPos;
                evt.menu.AppendAction("Add Sequence", _ => AddSequence(capPos));
            }
        }

        internal Vector2 WorldToCanvas(Vector2 worldPos)
        {
            return contentViewContainer.WorldToLocal(worldPos);
        }

        internal void ShowAddEffectMenu(Vector2 canvasPos)
        {
            if (_anim == null) return;

            var menu = new GenericMenu();

            for (int vi = 0; vi < _vtValues.Length; vi++)
            {
                PropertyTypes capPt = _vtValues[vi];
                string label = _vtLabels[vi];
                Vector2 capPos = canvasPos;

                menu.AddItem(new GUIContent($"Add Effect/Tween/{label}"), false,
                    () => AddNewEffect(-1, CreateEffect(EffectKind.Tween, capPt), capPos));
                menu.AddItem(new GUIContent($"Add Effect/Follow/{label}"), false,
                    () => AddNewEffect(-1, CreateEffect(EffectKind.Follow, capPt), capPos));
                menu.AddItem(new GUIContent($"Add Effect/Shake/{label}"), false,
                    () => AddNewEffect(-1, CreateEffect(EffectKind.Shake, capPt), capPos));
            }

            {
                Vector2 capPos = canvasPos;
                menu.AddItem(new GUIContent("Add Effect/Loop"), false,
                    () => AddLoopNode(-1, capPos));
            }

            menu.AddSeparator("");
            {
                Vector2 capPos = canvasPos;
                menu.AddItem(new GUIContent("Control Nodes/Hook"), false,
                    () => SpawnHookNode(capPos));
                menu.AddItem(new GUIContent("Control Nodes/Smoothing"), false,
                    () => SpawnSmoothingNode(capPos));
            }

            menu.ShowAsContext();
        }

        public void AddSequence(Vector2 canvasPos, PropertyTypes type = PropertyTypes.Vector3)
        {
            if (_anim == null) return;
            Undo.RecordObject(_anim, "Add Sequence");

            var seq = new Sequence("") { Type = type };
            seq.Property = MakeEmptyProperty(type);
            _anim.Sequences.Add(seq);
            LayoutEditorAccess.GetOrCreateLayout(_anim, _anim.Sequences.Count - 1);
            int si = _anim.Sequences.Count - 1;

            Vector2 origin = canvasPos == Vector2.zero
                ? new Vector2(StripOriginX, StripOriginY + si * (FilmStripElement.StripTotalHeight + SequenceGap))
                : canvasPos;

            int initialSlots = FilmStripElement.MinSlots;
            var strip = new FilmStripElement(seq, origin, initialSlots, si, _anim.Sequences.Count, _theme, OnSequenceNameChanged, OnSegmentChanged, this, RemoveStrip, OnTriggersChanged, OnLeftCapInitialClicked, OnLeftCapUpdateClicked, MoveStrip);
            AddElement(strip);
            _strips.Add(strip);

            var occ = new List<EffectNode>();
            for (int s = 0; s < initialSlots; s++) occ.Add(null);
            _occ.Add(occ);

            EditorUtility.SetDirty(_anim);
            _window?.UpdateScrollbars();
        }

        private static Property MakeEmptyProperty(PropertyTypes type)
        {
            switch (type)
            {
                case PropertyTypes.Float: return new PropertyFloat { Effects = new System.Collections.Generic.List<Effect<float>>() };
                case PropertyTypes.Vector2: return new PropertyVector2 { Effects = new System.Collections.Generic.List<Effect<Vector2>>() };
                case PropertyTypes.Vector3: return new PropertyVector3 { Effects = new System.Collections.Generic.List<Effect<Vector3>>() };
                case PropertyTypes.Vector4: return new PropertyVector4 { Effects = new System.Collections.Generic.List<Effect<Vector4>>() };
                case PropertyTypes.Quaternion: return new PropertyQuaternion { Effects = new System.Collections.Generic.List<Effect<Quaternion>>() };
                default:
                    Assert.IsTrue(false, $"Unknown PropertyTypes value: {type}");
                    return null;
            }
        }

        private static readonly PropertyTypes[] _vtValues =
        {
            PropertyTypes.Float, PropertyTypes.Vector2, PropertyTypes.Vector3,
            PropertyTypes.Vector4, PropertyTypes.Quaternion
        };
        private static readonly string[] _vtLabels =
            { "Float", "Vec2", "Vec3", "Vec4", "Quat" };

        private void AddNewEffect(int stripHint, IEffect effect, Vector2 canvasPos)
        {
            if (_anim == null || effect == null) return;

            int seqIdx = stripHint >= 0 ? stripHint
                       : (_strips.Count > 0 ? 0 : -1);

            Undo.RecordObject(_anim, "Add Effect");

            int flatIndex = 0;
            if (stripHint >= 0 && seqIdx >= 0 && seqIdx < _anim.Sequences.Count)
            {
                Sequence seq = _anim.Sequences[seqIdx];
                if (seq.Property == null
                    || (seq.Property.EffectCount == 0
                        && !PropertyMatchesEffect(seq.Property, effect)))
                {
                    PropertyTypes newType = effect.ValueType;
                    if (seq.Property != null && seq.Type != newType)
                        _window?.SetMessage(
                            $"Sequence \"{seq.Name}\" type changed from {seq.Type} to {newType}.",
                            SequenceEditorWindow.MessageSeverity.Info, 10f);
                    seq.Property = MakeEmptyProperty(newType);
                    seq.Type = newType;
                }
                flatIndex = AddEffectToProperty(seq.Property, effect);

                var addLayout = LayoutEditorAccess.GetOrCreateLayout(_anim, seqIdx);
                while (addLayout.effectNodePositions.Count <= flatIndex)
                    addLayout.effectNodePositions.Add(Vector2.zero);
                addLayout.effectNodePositions[flatIndex] = canvasPos;
            }

            var node = new EffectNode(effect, seqIdx, -1, flatIndex, _anim, _theme, this);
            AddElement(node);
            _nodes.Add(node);
            node.SetPosition(new Rect(canvasPos, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
            _settled[node] = canvasPos;

            if (stripHint >= 0 && stripHint < _occ.Count)
            {
                float cx = canvasPos.x;
                int bestSlot = -1;
                float bestD = float.MaxValue;
                for (int slot = 0; slot < _occ[stripHint].Count; slot++)
                {
                    if (IsNodeAtSlot(stripHint, slot)) continue;
                    float d = Mathf.Abs(cx - _strips[stripHint].GetSlotCenter(slot).x);
                    if (d < bestD) { bestD = d; bestSlot = slot; }
                }
                if (bestSlot >= 0)
                {
                    node.StripIndex = stripHint; node.SlotIndex = bestSlot;
                    _occ[stripHint][bestSlot] = node;
                    Vector2 np = NodeCanvasPos(stripHint, bestSlot);
                    node.SetPosition(new Rect(np, new Vector2(EffectNode.NodeW, EffectNode.NodeH)));
                    _settled[node] = np;
                    node.SetFloating(false);
                    RebuildStrip(stripHint);
                }
                else
                {
                    SnapNode(node);
                }
            }
            else
                node.SetFloating(true);

            EditorUtility.SetDirty(_anim);
            RunValidation();
        }

        internal void RemoveEffectNode(EffectNode node)
        {
            this.Q<DelegatePicker>()?.RemoveFromHierarchy();

            int si = node.StripIndex;
            bool stripValid = _anim != null && si >= 0 && si < _anim.Sequences.Count;

            if (stripValid)
            {
                Undo.RecordObject(_anim, "Remove Effect Node");
                Sequence seq = _anim.Sequences[si];
                if (seq.Property != null)
                {
                    int removedFlat = IndexOfEffect(seq, node.SourceEffect);
                    seq.Property.RemoveEffect(node.SourceEffect);
                    if (removedFlat >= 0)
                        SequenceLibrary.PropagateEditorLayout(
                            seq.Name, removedFlat, (IAnimationEditorComponent)_anim);
                }
            }

            foreach (var hn in RemoveNodeOutputEdges(node))
            {
                _hookNodes.Remove(hn);
                _hookSettled.Remove(hn);
                RemoveElement(hn);
            }

            if (stripValid)
            {
                RebuildStripNodes(si);

                // Removal is structural; a running coroutine cannot splice the
                // effect out of its shared list in place. RebuildStripNodes has
                // already broadcast the shortened sequence to siblings, so restart
                // any running siblings, and the source, to re-wrap them.
                Sequence seq = _anim.Sequences[si];
                SequenceLibrary.RestartRunningSiblings(seq.Name, seq, _anim);
                if (JuiceBoxCentralController.IsSequenceRunning(seq))
                    _anim.StartSequence(si);
            }
            else
            {
                _nodes.Remove(node);
                _settled.Remove(node);
                RemoveElement(node);
            }
        }

        internal void RemoveHookNode(HookNode hook)
        {
            IEffect srcEffect = hook.SourceEffect;
            string srcSlot = hook.SlotName;
            int srcStrip = -1;

            if (srcEffect != null && hook.SlotIndex >= 0)
            {
                var srcNode = FindEffectNodeForEffect(srcEffect);
                if (srcNode != null) srcStrip = srcNode.StripIndex;

                srcEffect.WriteSlot(
                    srcSlot, (int)DelegateMode.None, null, "", "", "");
                srcEffect.Reconstruct();
                SaveHookPos(srcEffect, hook.SlotIndex, Vector2.zero);
                srcNode?.RefreshHookBadge(hook.SlotIndex);
            }

            var affectedStrips = new HashSet<int>();
            foreach (var edge in edges.ToList())
            {
                if (FindOwnerNode(edge.input) != hook) continue;
                if (FindOwnerNode(edge.output) is EffectNode en && en.StripIndex >= 0)
                    affectedStrips.Add(en.StripIndex);
                if (edge.input != null) edge.input.Disconnect(edge);
                if (edge.output != null) edge.output.Disconnect(edge);
                RemoveElement(edge);
            }

            _hookNodes.Remove(hook);
            _hookSettled.Remove(hook);
            RemoveElement(hook);

            foreach (int si in affectedStrips)
                ApplySequence(si, broadcast: false);

            // Push the now-cleared slot to siblings. The effect is shared with any
            // running coroutine, so the unbind applies live without a restart.
            if (srcEffect != null && srcStrip >= 0 && srcStrip < _anim.Sequences.Count)
            {
                Sequence srcSeq = _anim.Sequences[srcStrip];
                int idx = IndexOfEffect(srcSeq, srcEffect);
                if (idx >= 0)
                    SequenceLibrary.PropagateEffectSlot(srcSeq.Name, srcSeq, idx, srcSlot, _anim);
            }

            RunValidation();
            schedule.Execute(RaiseEdges);
        }

        // Index of an effect within a sequence's merged Property effect list, or -1.
        private static int IndexOfEffect(Sequence seq, IEffect effect)
        {
            if (seq == null || seq.Property == null || effect == null) return -1;
            int n = seq.Property.EffectCount;
            for (int i = 0; i < n; i++)
                if (ReferenceEquals(seq.Property.GetEffect(i), effect)) return i;
            return -1;
        }

        // Persists a hook delegate edit on the source and pushes just that slot to
        // siblings. The picker has already written and reconstructed the source
        // effect, and the structure is unchanged, so no strip rebuild is needed.
        internal void PropagateEffectSlotEdit(IEffect effect, string slotName)
        {
            if (effect == null || _anim == null) return;

            var node = FindEffectNodeForEffect(effect);
            if (node == null || node.StripIndex < 0 || node.StripIndex >= _anim.Sequences.Count) return;

            Sequence seq = _anim.Sequences[node.StripIndex];
            int idx = IndexOfEffect(seq, effect);
            if (idx < 0) return;

            Undo.RecordObject(_anim, "Edit JuiceBox Slot");
            EditorUtility.SetDirty(_anim);
            SequenceLibrary.PropagateEffectSlot(seq.Name, seq, idx, slotName, _anim);
            SequenceBackupManager.TrySnapshot(seq, _anim);
            RunValidation();
            schedule.Execute(RaiseEdges);
        }

        private void MoveStrip(int si, int direction)
        {
            if (_anim == null) return;
            int ti = si + direction;
            if (si < 0 || si >= _anim.Sequences.Count) return;
            if (ti < 0 || ti >= _anim.Sequences.Count) return;

            Undo.RecordObject(_anim, "Reorder Sequence");

            var seqs = _anim.Sequences;
            var tmp = seqs[si]; seqs[si] = seqs[ti]; seqs[ti] = tmp;

            if (Ed._editorLayouts != null)
            {
                while (Ed._editorLayouts.Count < seqs.Count)
                    Ed._editorLayouts.Add(new JuiceBoxAnimation.SequenceEditorLayout());
                var ltmp = Ed._editorLayouts[si];
                Ed._editorLayouts[si] = Ed._editorLayouts[ti];
                Ed._editorLayouts[ti] = ltmp;

                float step = FilmStripElement.StripTotalHeight + SequenceGap;
                ShiftLayoutY(Ed._editorLayouts[si], -direction * step);
                ShiftLayoutY(Ed._editorLayouts[ti], direction * step);
            }

            EditorUtility.SetDirty(_anim);
            SetTarget(_anim);
            _window?.UpdateScrollbars();
        }

        private static void ShiftLayoutY(JuiceBoxAnimation.SequenceEditorLayout layout, float dy)
        {
            if (layout == null) return;
            for (int i = 0; i < layout.effectNodePositions.Count; i++)
            {
                Vector2 p = layout.effectNodePositions[i];
                if (p != Vector2.zero)
                    layout.effectNodePositions[i] = new Vector2(p.x, p.y + dy);
            }
            if (layout.hookNodes != null)
            {
                for (int i = 0; i < layout.hookNodes.Count; i++)
                {
                    var sn = layout.hookNodes[i];
                    if (sn.position != Vector2.zero)
                    {
                        sn.position = new Vector2(sn.position.x, sn.position.y + dy);
                        layout.hookNodes[i] = sn;
                    }
                }
            }
            if (layout.smoothingNodes != null)
            {
                for (int i = 0; i < layout.smoothingNodes.Count; i++)
                {
                    var sn = layout.smoothingNodes[i];
                    if (sn.position != Vector2.zero)
                    {
                        sn.position = new Vector2(sn.position.x, sn.position.y + dy);
                        layout.smoothingNodes[i] = sn;
                    }
                }
            }
            if (layout.valueNodes != null)
            {
                for (int i = 0; i < layout.valueNodes.Count; i++)
                {
                    var sn = layout.valueNodes[i];
                    if (sn.position != Vector2.zero)
                    {
                        sn.position = new Vector2(sn.position.x, sn.position.y + dy);
                        layout.valueNodes[i] = sn;
                    }
                }
            }
        }

        private void RemoveStrip(FilmStripElement strip)
        {
            int si = _strips.IndexOf(strip);
            if (si < 0 || _anim == null) return;

            Undo.RecordObject(_anim, "Remove Sequence");

            var effectsInStrip = new HashSet<IEffect>();
            for (int i = 0; i < _nodes.Count; i++)
                if (_nodes[i].StripIndex == si) effectsInStrip.Add(_nodes[i].SourceEffect);

            var hooksToRemove = new HashSet<HookNode>();
            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                if (_nodes[i].StripIndex != si) continue;
                foreach (var hn in RemoveNodeOutputEdges(_nodes[i]))
                    hooksToRemove.Add(hn);
            }

            foreach (var hn in hooksToRemove)
            {
                _hookNodes.Remove(hn);
                _hookSettled.Remove(hn);
                RemoveElement(hn);
            }

            for (int i = _smoothingNodes.Count - 1; i >= 0; i--)
            {
                var sn = _smoothingNodes[i];
                if (sn.SourceEffect == null || !effectsInStrip.Contains(sn.SourceEffect)) continue;
                foreach (var edge in edges.ToList())
                    if (FindOwnerNode(edge.input) == sn || FindOwnerNode(edge.output) == sn)
                        RemoveElement(edge);
                _smoothingSettled.Remove(sn);
                RemoveElement(sn);
                _smoothingNodes.RemoveAt(i);
            }

            for (int i = _loopNodes.Count - 1; i >= 0; i--)
            {
                if (_loopNodes[i].StripIndex != si) continue;
                _loopSettled.Remove(_loopNodes[i]);
                RemoveElement(_loopNodes[i]);
                _loopNodes.RemoveAt(i);
            }

            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                if (_nodes[i].StripIndex != si) continue;
                RemoveElement(_nodes[i]);
                _nodes.RemoveAt(i);
            }

            foreach (var node in _nodes)
                if (node.StripIndex > si) node.StripIndex--;
            foreach (var ln in _loopNodes)
                if (ln.StripIndex > si) ln.StripIndex--;

            RemoveElement(_strips[si]);
            _strips.RemoveAt(si);
            _occ.RemoveAt(si);
            _anim.Sequences.RemoveAt(si);
            if (Ed._editorLayouts != null && si < Ed._editorLayouts.Count)
                Ed._editorLayouts.RemoveAt(si);

            float shift = FilmStripElement.StripTotalHeight + SequenceGap;
            for (int j = si; j < _strips.Count; j++)
            {
                Rect pos = _strips[j].GetPosition();
                _strips[j].SetPosition(new Rect(pos.x, pos.y - shift, pos.width, pos.height));
            }

            EditorUtility.SetDirty(_anim);
            RunValidation();
            schedule.Execute(RaiseEdges);
            _window?.UpdateScrollbars();
        }

        private List<HookNode> RemoveNodeOutputEdges(EffectNode node)
        {
            var hooksToCheck = new HashSet<HookNode>();
            foreach (var edge in edges.ToList())
            {
                if (FindOwnerNode(edge.output) != node) continue;
                if (FindOwnerNode(edge.input) is HookNode hn) hooksToCheck.Add(hn);
                if (edge.input != null) edge.input.Disconnect(edge);
                if (edge.output != null) edge.output.Disconnect(edge);
                RemoveElement(edge);
            }

            var orphaned = new List<HookNode>();
            if (hooksToCheck.Count > 0)
            {
                var remaining = edges.ToList();
                foreach (var hn in hooksToCheck)
                {
                    bool stillConnected = false;
                    foreach (var e in remaining)
                        if (FindOwnerNode(e.input) == hn) { stillConnected = true; break; }
                    if (!stillConnected) orphaned.Add(hn);
                }
            }
            return orphaned;
        }

        private static Effect CreateEffect(EffectKind kind, PropertyTypes valueType)
        {
            if (kind == EffectKind.Tween)
            {
                var wft = new WaitForTime { Time = 1f };
                switch (valueType)
                {
                    case PropertyTypes.Float: return new TweenFloat { EndCondition = wft };
                    case PropertyTypes.Vector2: return new TweenVector2 { EndCondition = wft };
                    case PropertyTypes.Vector3: return new TweenVector3 { EndCondition = wft };
                    case PropertyTypes.Vector4: return new TweenVector4 { EndCondition = wft };
                    case PropertyTypes.Quaternion: return new TweenQuaternion { EndCondition = wft };
                }
            }
            else if (kind == EffectKind.Shake)
            {
                var wft = new WaitForTime { Time = 1f };
                switch (valueType)
                {
                    case PropertyTypes.Float: return new ShakeFloat { EndCondition = wft, Amplitude = 1f };
                    case PropertyTypes.Vector2: return new ShakeVector2 { EndCondition = wft, Amplitude = Vector2.one };
                    case PropertyTypes.Vector3: return new ShakeVector3 { EndCondition = wft, Amplitude = Vector3.one };
                    case PropertyTypes.Vector4: return new ShakeVector4 { EndCondition = wft, Amplitude = Vector4.one };
                    case PropertyTypes.Quaternion: return new ShakeQuaternion { EndCondition = wft, Amplitude = new Quaternion(1f, 1f, 1f, 0f) };
                }
            }
            else
            {
                switch (valueType)
                {
                    case PropertyTypes.Float:
                        {
                            var range = new WaitForFloatWithinRange();
                            range.Range = 0.01f;
                            return new FollowFloat { EndCondition = range, Speed = 1f };
                        }
                    case PropertyTypes.Vector2:
                        {
                            var range = new WaitForVector2WithinRange();
                            range.Range = 0.01f;
                            return new FollowVector2 { EndCondition = range, Speed = 1f };
                        }
                    case PropertyTypes.Vector3:
                        {
                            var range = new WaitForVector3WithinRange();
                            range.Range = 0.01f;
                            return new FollowVector3 { EndCondition = range, Speed = 1f };
                        }
                    case PropertyTypes.Vector4:
                        {
                            var range = new WaitForVector4WithinRange();
                            range.Range = 0.01f;
                            return new FollowVector4 { EndCondition = range, Speed = 1f };
                        }
                    case PropertyTypes.Quaternion:
                        {
                            var range = new WaitForQuaternionWithinRange();
                            range.Range = 0.5f;
                            return new FollowQuaternion { EndCondition = range, Speed = 1f };
                        }
                }
            }
            Assert.IsTrue(false, $"Unknown EffectKind/PropertyTypes combination: {kind}/{valueType}");
            return null;
        }
    }
}