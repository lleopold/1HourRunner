using System.Collections.Generic;
using UnityEngine;
using static JuiceBox.Processor;

// ==============================================================================
//  LayoutEditorAccess: Static helpers for reading and writing editor layout data on JuiceBoxAnimation.
// ==============================================================================
namespace JuiceBox
{
    public static class LayoutEditorAccess
    {
        public static JuiceBoxAnimation.SequenceEditorLayout GetOrCreateLayout(
            JuiceBoxAnimation anim, int sequenceIndex)
        {
            if (((IAnimationEditorComponent)anim)._editorLayouts == null)
                ((IAnimationEditorComponent)anim)._editorLayouts = new List<JuiceBoxAnimation.SequenceEditorLayout>();
            while (((IAnimationEditorComponent)anim)._editorLayouts.Count <= sequenceIndex)
                ((IAnimationEditorComponent)anim)._editorLayouts.Add(new JuiceBoxAnimation.SequenceEditorLayout());
            return ((IAnimationEditorComponent)anim)._editorLayouts[sequenceIndex];
        }

        public static Vector2 ReadHookPos(
            JuiceBoxAnimation.SequenceEditorLayout layout, int effectIndex, int slotIndex)
        {
            if (layout == null || layout.hookNodes == null) return Vector2.zero;
            for (int i = 0; i < layout.hookNodes.Count; i++)
            {
                var sn = layout.hookNodes[i];
                if (sn.effectIndex == effectIndex && sn.slotIndex == slotIndex)
                    return sn.position;
            }
            return Vector2.zero;
        }

        public static void WriteHookPos(
            JuiceBoxAnimation.SequenceEditorLayout layout, int effectIndex, int slotIndex, Vector2 pos)
        {
            if (layout == null) return;
            if (layout.hookNodes == null)
                layout.hookNodes = new List<JuiceBoxAnimation.SubnodeLayout>();
            for (int i = 0; i < layout.hookNodes.Count; i++)
            {
                var sn = layout.hookNodes[i];
                if (sn.effectIndex == effectIndex && sn.slotIndex == slotIndex)
                {
                    sn.position = pos;
                    layout.hookNodes[i] = sn;
                    return;
                }
            }
            layout.hookNodes.Add(new JuiceBoxAnimation.SubnodeLayout
            {
                effectIndex = effectIndex,
                slotIndex = slotIndex,
                position = pos
            });
        }

        public static Vector2 ReadSmoothingPos(
            JuiceBoxAnimation.SequenceEditorLayout layout, int effectIndex)
        {
            if (layout == null || layout.smoothingNodes == null) return Vector2.zero;
            for (int i = 0; i < layout.smoothingNodes.Count; i++)
            {
                if (layout.smoothingNodes[i].effectIndex == effectIndex)
                    return layout.smoothingNodes[i].position;
            }
            return Vector2.zero;
        }

        public static void WriteSmoothingPos(
            JuiceBoxAnimation.SequenceEditorLayout layout, int effectIndex, Vector2 pos)
        {
            if (layout == null) return;
            if (layout.smoothingNodes == null)
                layout.smoothingNodes = new List<JuiceBoxAnimation.SubnodeLayout>();
            for (int i = 0; i < layout.smoothingNodes.Count; i++)
            {
                var sn = layout.smoothingNodes[i];
                if (sn.effectIndex == effectIndex)
                {
                    sn.position = pos;
                    layout.smoothingNodes[i] = sn;
                    return;
                }
            }
            layout.smoothingNodes.Add(new JuiceBoxAnimation.SubnodeLayout
            {
                effectIndex = effectIndex,
                slotIndex = 0,
                position = pos
            });
        }
    }
}