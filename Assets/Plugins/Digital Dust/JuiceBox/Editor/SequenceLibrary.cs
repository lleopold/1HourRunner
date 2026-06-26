using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceLibrary: Propagates sequence edits to all loaded components that share the same sequence name.
// ==============================================================================
namespace JuiceBox
{
    [InitializeOnLoad]
    internal static class SequenceLibrary
    {
        public static event System.Action<string> OnSequenceChanged;

        static SequenceLibrary() { }

        // One matched sequence on a loaded component. 'index' is the position
        // within anim.Sequences.
        internal struct SiblingSequence
        {
            public JuiceBoxAnimation anim;
            public Sequence seq;
            public int index;
        }

        // Reused buffer for CollectSiblingSequences. Callers must finish consuming
        // the returned list before calling CollectSiblingSequences again; no
        // current caller re-enters during iteration.
        private static readonly List<SiblingSequence> _siblingBuffer = new List<SiblingSequence>();

        // Walks every loaded JuiceBoxAnimation and returns each sequence whose name
        // matches seqName. Matching is by name only: there is no shared-definition
        // concept, so duplicates are linked purely by name. Entries are grouped by
        // component (all matches on one anim are contiguous).
        private static List<SiblingSequence> CollectSiblingSequences(string seqName,
            IAnimationEditorComponent context)
        {
            _siblingBuffer.Clear();
            if (string.IsNullOrEmpty(seqName) || context == null) return _siblingBuffer;

            var allAnims = context.GetInstances();
            for (int a = 0; a < allAnims.Count; a++)
            {
                JuiceBoxAnimation anim = allAnims[a];
                if (anim == null || anim.Sequences == null) continue;
                for (int i = 0; i < anim.Sequences.Count; i++)
                {
                    Sequence seq = anim.Sequences[i];
                    if (seq == null || seq.Name != seqName) continue;
                    _siblingBuffer.Add(new SiblingSequence { anim = anim, seq = seq, index = i });
                }
            }
            return _siblingBuffer;
        }

        private static bool AnimationContainsSequence(JuiceBoxAnimation anim, Sequence target)
        {
            if (anim == null || anim.Sequences == null || target == null) return false;
            for (int i = 0; i < anim.Sequences.Count; i++)
                if (anim.Sequences[i] == target) return true;
            return false;
        }

        public static void NotifySequenceChanged(string name, Sequence source,
            IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(name) || source == null) return;

            string json = EditorJsonUtility.ToJson(source);

            var siblings = CollectSiblingSequences(name, context);
            for (int i = 0; i < siblings.Count; i++)
            {
                SiblingSequence sib = siblings[i];
                if (sib.seq == source) continue;

                EditorJsonUtility.FromJsonOverwrite(json, sib.seq);
                Processor.FinalizeSerialization();
                EditorUtility.SetDirty(sib.anim);
            }

            OnSequenceChanged?.Invoke(name);
        }

        public static void NotifySequenceRenamed(string oldName, string newName, Sequence source,
            IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(oldName)) return;
            if (oldName == newName) return;

            var siblings = CollectSiblingSequences(oldName, context);
            for (int i = 0; i < siblings.Count; i++)
            {
                SiblingSequence sib = siblings[i];
                // Skip the entire component that owns the source sequence; the
                // caller is responsible for renaming the source itself.
                if (AnimationContainsSequence(sib.anim, source)) continue;

                sib.seq.Name = newName;
                EditorUtility.SetDirty(sib.anim);
            }

            OnSequenceChanged?.Invoke(newName);
        }

        public static int CountReferences(string name, IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(name)) return 0;

            var siblings = CollectSiblingSequences(name, context);
            int count = 0;
            JuiceBoxAnimation lastAnim = null;
            for (int i = 0; i < siblings.Count; i++)
            {
                // Entries are grouped by component, so a change of anim marks a
                // distinct referencing component.
                if (siblings[i].anim != lastAnim)
                {
                    count++;
                    lastAnim = siblings[i].anim;
                }
            }
            return count;
        }

        public static string GetSequenceJson(string name, JuiceBoxAnimation exclude)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var siblings = CollectSiblingSequences(name, exclude);
            for (int i = 0; i < siblings.Count; i++)
                if (siblings[i].anim != exclude)
                    return EditorJsonUtility.ToJson(siblings[i].seq);

            return null;
        }

        // -- Targeted delegate-slot propagation --------------------------------

        // Copies one delegate slot (binding metadata, value slot, and eval-once
        // flag) from 'from' to 'to', then reconstructs 'to' so relative descriptors
        // resolve against the target's own owner GameObject. The value-slot and
        // eval-once writes are no-ops on slots that do not use them.
        private static void CopySlot(IDelegateConnecter from, IDelegateConnecter to, string slotName)
        {
            var (mode, obj, cls, method, relDesc) = from.ReadSlot(slotName);
            to.WriteSlot(slotName, mode, obj, cls, method, relDesc);
            to.WriteValueSlot(slotName, from.ReadValueSlot(slotName));
            to.WriteEvalOnce(slotName, from.ReadEvalOnce(slotName));
            to.Reconstruct();
        }

        // Propagates one delegate slot on the effect at effectIndex (within
        // sourceSeq's Property) to the same-index effect on every other sequence
        // that shares the name. Running siblings update without a restart: the
        // active coroutine's merged effect list holds the same Effect objects as
        // the serialized Property, so reconstructing rebinds the delegate in place.
        public static void PropagateEffectSlot(string seqName, Sequence sourceSeq,
            int effectIndex, string slotName, IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(seqName) || sourceSeq == null || sourceSeq.Property == null) return;
            if (effectIndex < 0 || effectIndex >= sourceSeq.Property.EffectCount) return;

            IDelegateConnecter from = sourceSeq.Property.GetEffect(effectIndex);
            if (from == null) return;

            var siblings = CollectSiblingSequences(seqName, context);
            for (int i = 0; i < siblings.Count; i++)
            {
                SiblingSequence sib = siblings[i];
                if (sib.seq == sourceSeq || sib.seq.Property == null) continue;
                if (effectIndex >= sib.seq.Property.EffectCount) continue;

                IDelegateConnecter to = sib.seq.Property.GetEffect(effectIndex);
                if (to == null) continue;

                CopySlot(from, to, slotName);
                EditorUtility.SetDirty(sib.anim);
            }
        }

        // Propagates one cap delegate slot (OnUpdate, SetStartingValue) on
        // sourceSeq's Property to every other sequence that shares the name. Cap
        // delegates are copied by reference into the running merged property at
        // setup rather than shared, so a running sibling is restarted to pick up
        // the change.
        public static void PropagatePropertySlot(string seqName, Sequence sourceSeq, string slotName,
            IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(seqName) || sourceSeq == null || sourceSeq.Property == null) return;

            IDelegateConnecter from = (IDelegateConnecter)sourceSeq.Property;

            var siblings = CollectSiblingSequences(seqName, context);
            for (int i = 0; i < siblings.Count; i++)
            {
                SiblingSequence sib = siblings[i];
                if (sib.seq == sourceSeq || sib.seq.Property == null) continue;

                CopySlot(from, (IDelegateConnecter)sib.seq.Property, slotName);
                EditorUtility.SetDirty(sib.anim);

                if (JuiceBoxCentralController.IsSequenceRunning(sib.seq))
                    sib.anim.StartSequence(sib.index);
            }
        }

        // Restarts every running sibling that shares the name (excluding the
        // source). Used after a structural broadcast (e.g. effect removal) that the
        // running coroutine cannot apply in place, so it must be re-wrapped.
        public static void RestartRunningSiblings(string seqName, Sequence sourceSeq,
            IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(seqName)) return;

            var siblings = CollectSiblingSequences(seqName, context);
            for (int i = 0; i < siblings.Count; i++)
            {
                SiblingSequence sib = siblings[i];
                if (sib.seq == sourceSeq) continue;
                if (JuiceBoxCentralController.IsSequenceRunning(sib.seq))
                    sib.anim.StartSequence(sib.index);
            }
        }

        // -- Sequence-field propagation ----------------------------------------

        // Propagates the trigger flags to every other sequence that shares the
        // name. No runtime action: triggers only decide which lifecycle events
        // auto-start a sequence, so they do not affect anything already running.
        public static void PropagateTriggers(string seqName, Sequence sourceSeq, TriggerMode triggers,
            IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(seqName)) return;

            var siblings = CollectSiblingSequences(seqName, context);
            for (int i = 0; i < siblings.Count; i++)
            {
                SiblingSequence sib = siblings[i];
                if (sib.seq == sourceSeq) continue;

                sib.seq.Triggers = triggers;
                EditorUtility.SetDirty(sib.anim);
            }
        }

        // Propagates the timing segment to every other sequence that shares the
        // name. Running siblings are moved to the new segment live; there is a
        // dedicated controller call for this, so no restart is needed.
        public static void PropagateSegment(string seqName, Sequence sourceSeq, MEC.Segment segment,
            IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(seqName)) return;

            var siblings = CollectSiblingSequences(seqName, context);
            for (int i = 0; i < siblings.Count; i++)
            {
                SiblingSequence sib = siblings[i];
                if (sib.seq == sourceSeq) continue;

                sib.seq.Segment = segment;
                EditorUtility.SetDirty(sib.anim);

                if (JuiceBoxCentralController.IsSequenceRunning(sib.seq))
                    JuiceBoxCentralController.Instance.SetSegment(sib.seq, segment);
            }
        }

        // An effect at removedFlatIndex was removed from every sequence named
        // seqName. The per-sequence editor layout is keyed by flat effect index,
        // so reindex each sibling's layout: drop the records at the removed index
        // and shift everything above it down one. Cosmetic node placement only;
        // the runtime effect list is shortened separately by the caller.
        internal static void PropagateEditorLayout(
            string seqName, int removedFlatIndex, IAnimationEditorComponent context)
        {
            if (string.IsNullOrEmpty(seqName) || removedFlatIndex < 0 || context == null)
                return;

            var siblings = CollectSiblingSequences(seqName, context);
            if (siblings.Count == 0) return;
            // Copy out of the reused buffer before mutating layouts.
            var targets = new List<SiblingSequence>(siblings);

            for (int s = 0; s < targets.Count; s++)
            {
                var ied = (IAnimationEditorComponent)targets[s].anim;
                var layouts = ied._editorLayouts;
                if (layouts == null || targets[s].index >= layouts.Count) continue;
                JuiceBoxAnimation.SequenceEditorLayout layout = layouts[targets[s].index];
                if (layout == null) continue;

                if (removedFlatIndex < layout.effectNodePositions.Count)
                    layout.effectNodePositions.RemoveAt(removedFlatIndex);
                layout.loopNodes = ReindexLoopMask(layout.loopNodes, removedFlatIndex);
                ReindexSubnodeList(layout.hookNodes, removedFlatIndex);
                ReindexSubnodeList(layout.smoothingNodes, removedFlatIndex);
                ReindexSubnodeList(layout.valueNodes, removedFlatIndex);

                EditorUtility.SetDirty(targets[s].anim);
            }
        }

        // Drops subnode records whose effectIndex is the removed effect and
        // decrements those above it.
        private static void ReindexSubnodeList(
            List<JuiceBoxAnimation.SubnodeLayout> list, int flatIndex)
        {
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                JuiceBoxAnimation.SubnodeLayout sn = list[i];
                if (sn.effectIndex == flatIndex)
                    list.RemoveAt(i);
                else if (sn.effectIndex > flatIndex)
                {
                    sn.effectIndex--;
                    list[i] = sn;
                }
            }
        }

        // Clears the bit at flatIndex and shifts every higher bit down one so the
        // per-effect loop flags stay aligned after the effect list shifts.
        private static uint ReindexLoopMask(uint mask, int flatIndex)
        {
            if (flatIndex < 0 || flatIndex >= 32) return mask;
            uint low = (flatIndex == 0) ? 0u : (mask & ((1u << flatIndex) - 1u));
            uint high = (flatIndex >= 31) ? 0u : ((mask >> (flatIndex + 1)) << flatIndex);
            return low | high;
        }
    }
}