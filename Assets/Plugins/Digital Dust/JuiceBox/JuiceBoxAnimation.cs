using MEC;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Concurrent;
#endif

// ==============================================================================
//  JuiceBoxAnimation: User-facing MonoBehaviour for playing, pausing, and stopping animation sequences.
// ==============================================================================
namespace JuiceBox
{
    // Editor-facing accessors for JuiceBoxAnimation's layout data. Declared in the
    // runtime assembly (not the core DLL) because it references the layout types
    // nested on JuiceBoxAnimation, which the DLL cannot see.
    public interface IAnimationEditorComponent
    {
        List<JuiceBoxAnimation.SequenceEditorLayout> _editorLayouts { get; set; }
        string _layoutBackupId { get; set; }
        List<JuiceBoxAnimation> GetInstances();
    }

    /// <summary>
    /// Main component for JuiceBox animations. Add this to a GameObject to define
    /// and control animation sequences. Use the Sequence Editor window to build
    /// sequences visually, then play them at runtime via StartSequence, Pause, Resume, and Stop.
    /// </summary>
    public class JuiceBoxAnimation : Processor, IAnimationEditorComponent
    {
#if UNITY_EDITOR
        // Editor-only registry of every loaded component. Replaces the per-edit
        // Resources.FindObjectsOfTypeAll scan in sibling propagation: the managed
        // constructor enqueues each deserialized instance (active, disabled, asset,
        // or prefab-stage alike) and the drain folds them into _instances on the
        // next query. The constructor runs even when no Unity message method does
        // (edit mode, disabled objects), which is where propagation happens.
#if UNITY_6000_8_OR_NEWER
        [Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
#endif
        private static readonly ConcurrentQueue<JuiceBoxAnimation> _pending =
            new ConcurrentQueue<JuiceBoxAnimation>();
#if UNITY_6000_8_OR_NEWER
        [Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
#endif
        private static readonly List<JuiceBoxAnimation> _instances =
            new List<JuiceBoxAnimation>();
        [NonSerialized] private bool _registered;

        // Enqueue only. Deserialization may run off the main thread and forbids the
        // Scripting API here, so nothing else happens; the lock-free queue makes the
        // enqueue safe from any thread.
        public JuiceBoxAnimation()
        {
            _pending.Enqueue(this);
        }

        List<JuiceBoxAnimation> IAnimationEditorComponent.GetInstances()
        {
            return DrainAndGetInstances();
        }

        // Folds pending instances into the registered set and returns it. Returns
        // the live list, not a copy, so the hot propagation path iterates without
        // allocating. Callers must not mutate it.
        private static List<JuiceBoxAnimation> DrainAndGetInstances()
        {
            while (_pending.TryDequeue(out JuiceBoxAnimation anim))
            {
                if (anim == null || anim._registered) continue;
                anim._registered = true;
                _instances.Add(anim);
            }
            return _instances;
        }
#else
        List<JuiceBoxAnimation> IAnimationEditorComponent.GetInstances() { return null; }
#endif

        /// <summary>All animation sequences on this component. Each sequence targets one property
        /// type and contains an ordered list of effects (tweens, follows, shakes).</summary>
        [SerializeField] public List<Sequence> Sequences = new List<Sequence>();

        [SerializeField]
        internal List<SequenceEditorLayout> _editorLayouts =
           new List<SequenceEditorLayout>();

        [SerializeField, HideInInspector] internal string _layoutBackupId;
        List<SequenceEditorLayout> IAnimationEditorComponent._editorLayouts { get => _editorLayouts; set => _editorLayouts = value; }
        string IAnimationEditorComponent._layoutBackupId { get => _layoutBackupId; set => _layoutBackupId = value; }

        [System.Serializable]
        public struct SubnodeLayout
        {
            public Vector2 position;
            public int effectIndex;
            public int slotIndex;
        }

        [System.Serializable]
        public class SequenceEditorLayout
        {
            public List<Vector2> effectNodePositions = new List<Vector2>();
            public uint loopNodes;
            public List<SubnodeLayout> hookNodes = new List<SubnodeLayout>();
            public List<SubnodeLayout> smoothingNodes = new List<SubnodeLayout>();
            public List<SubnodeLayout> valueNodes = new List<SubnodeLayout>();
        }

        [System.Serializable]
        public struct CrossSequenceEdge
        {
            public int sourceSequenceIndex;
            public int sourceEffectIndex;
            public int sourceSlotIndex;
            public int targetSequenceIndex;
            public int targetEffectIndex;
            public int targetSlotIndex;
        }

        [System.Serializable]
        public class AnimationEditorLayout
        {
            public List<SubnodeLayout> hookNodes = new List<SubnodeLayout>();
            public List<SubnodeLayout> valueNodes = new List<SubnodeLayout>();
        }

        [SerializeField]
        internal List<CrossSequenceEdge> _crossSequenceEdges =
           new List<CrossSequenceEdge>();

        [SerializeField]
        internal AnimationEditorLayout _animationLayout =
           new AnimationEditorLayout();

        [System.Serializable]
        public class SequenceAnimationData
        {
            public float Timescale = 1f;
        }

        [SerializeField]
        internal List<SequenceAnimationData> _animationData =
           new List<SequenceAnimationData>();

        private readonly Dictionary<Sequence, CoroutineHandle> _handles = new Dictionary<Sequence, CoroutineHandle>();
        private readonly Dictionary<Sequence, float> _savedTimescales = new Dictionary<Sequence, float>();

        /// <summary>Returns true if the sequence at the given index is currently running.</summary>
        public bool IsRunning(int index)
        {
            if (index < 0 || index >= Sequences.Count) return false;
            return JuiceBoxCentralController.IsSequenceRunning(Sequences[index]);
        }

        /// <summary>Returns true if the first sequence with the given name is currently running.</summary>
        public bool IsRunning(string sequenceName)
        {
            for (int i = 0; i < Sequences.Count; i++)
            {
                if (Sequences[i].Name == sequenceName)
                {
                    return JuiceBoxCentralController.IsSequenceRunning(Sequences[i]);
                }
            }
            return false;
        }

        /// <summary>Pauses all running sequences by storing their timescales and setting them to zero.</summary>
        public void Pause()
        {
            for (int i = 0; i < Sequences.Count; i++)
                PauseSequence(Sequences[i]);
        }

        /// <summary>Pauses the sequence at the given index.</summary>
        public void Pause(int index)
        {
            if (index < 0 || index >= Sequences.Count)
            {
                Debug.LogWarning($"JuiceBoxAnimation: index {index} is out of range (count = {Sequences.Count}).", this);
                return;
            }
            PauseSequence(Sequences[index]);
        }

        /// <summary>Pauses the first sequence whose Name matches. Case-sensitive.</summary>
        public void Pause(string sequenceName)
        {
            for (int i = 0; i < Sequences.Count; i++)
            {
                if (Sequences[i].Name == sequenceName)
                {
                    PauseSequence(Sequences[i]);
                    return;
                }
            }
            Debug.LogWarning($"JuiceBoxAnimation: no sequence named \"{sequenceName}\" found.", this);
        }

        /// <summary>Resumes all paused sequences by restoring their stored timescales.</summary>
        public void Resume()
        {
            for (int i = 0; i < Sequences.Count; i++)
                ResumeSequence(Sequences[i]);
        }

        /// <summary>Resumes the sequence at the given index.</summary>
        public void Resume(int index)
        {
            if (index < 0 || index >= Sequences.Count)
            {
                Debug.LogWarning($"JuiceBoxAnimation: index {index} is out of range (count = {Sequences.Count}).", this);
                return;
            }
            ResumeSequence(Sequences[index]);
        }

        /// <summary>Resumes the first sequence whose Name matches. Case-sensitive.</summary>
        public void Resume(string sequenceName)
        {
            for (int i = 0; i < Sequences.Count; i++)
            {
                if (Sequences[i].Name == sequenceName)
                {
                    ResumeSequence(Sequences[i]);
                    return;
                }
            }
            Debug.LogWarning($"JuiceBoxAnimation: no sequence named \"{sequenceName}\" found.", this);
        }

        /// <summary>Starts the sequence at the given index, stopping and restarting if already running.</summary>
        public void StartSequence(int index)
        {
            if (index < 0 || index >= Sequences.Count)
            {
                Debug.LogWarning($"JuiceBoxAnimation: index {index} is out of range (count = {Sequences.Count}).", this);
                return;
            }
            PlaySequence(Sequences[index]);
        }

        /// <summary>Starts the first sequence whose Name matches. Case-sensitive.</summary>
        public void StartSequence(string sequenceName)
        {
            for (int i = 0; i < Sequences.Count; i++)
            {
                if (Sequences[i].Name == sequenceName)
                {
                    PlaySequence(Sequences[i]);
                    return;
                }
            }
            Debug.LogWarning($"JuiceBoxAnimation: no sequence named \"{sequenceName}\" found.", this);
        }

        /// <summary>Stops all running sequences on this animation and clears any saved pause state.</summary>
        public void Stop()
        {
            for (int i = 0; i < Sequences.Count; i++)
                JuiceBoxCentralController.Instance.StopAll(Sequences[i]);

            _handles.Clear();
            _savedTimescales.Clear();
        }

        /// <summary>Stops the sequence at the given index.</summary>
        public void Stop(int index)
        {
            if (index < 0 || index >= Sequences.Count)
            {
                Debug.LogWarning($"JuiceBoxAnimation: index {index} is out of range (count = {Sequences.Count}).", this);
                return;
            }
            StopSequence(Sequences[index]);
        }

        /// <summary>Stops the first sequence whose Name matches. Case-sensitive.</summary>
        public void Stop(string sequenceName)
        {
            for (int i = 0; i < Sequences.Count; i++)
            {
                if (Sequences[i].Name == sequenceName)
                {
                    StopSequence(Sequences[i]);
                    return;
                }
            }
            Debug.LogWarning($"JuiceBoxAnimation: no sequence named \"{sequenceName}\" found.", this);
        }

        private void PlaySequence(Sequence sequence)
        {
            JuiceBoxCentralController.Instance.StopAll(sequence);
            _savedTimescales.Remove(sequence);
            sequence.MyGameObject = gameObject;
            _handles[sequence] = JuiceBoxCentralController.Instance.Play(sequence);
        }

        private void StopSequence(Sequence sequence)
        {
            JuiceBoxCentralController.Instance.StopAll(sequence);
            _handles.Remove(sequence);
            _savedTimescales.Remove(sequence);
        }

        private void PauseSequence(Sequence sequence)
        {
            if (sequence.Property == null) return;
            if (!JuiceBoxCentralController.Instance.IsRunning(sequence)) return;
            if (_savedTimescales.ContainsKey(sequence)) return;
            _savedTimescales[sequence] = sequence.Property.Timescale;
            sequence.Property.Timescale = 0f;
        }

        private void ResumeSequence(Sequence sequence)
        {
            float saved;
            if (!_savedTimescales.TryGetValue(sequence, out saved)) return;
            _savedTimescales.Remove(sequence);
            if (sequence.Property != null) sequence.Property.Timescale = saved;
        }

        private void Awake()
        {
            if (_animationData == null) return;
            int count = Sequences.Count < _animationData.Count
                ? Sequences.Count : _animationData.Count;
            for (int i = 0; i < count; i++)
                if (Sequences[i].Property != null)
                    Sequences[i].Property.Timescale = _animationData[i].Timescale;
        }

        private void Start()
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnStart) != 0)
                    StartSequence(i);
        }

        private void OnEnable()
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnEnable) != 0)
                    StartSequence(i);
        }

        private void OnDisable()
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnDisable) != 0)
                    StartSequence(i);
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (_registered)
            {
                _registered = false;
                _instances.Remove(this);
            }
#endif
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnDestroy) != 0)
                    StartSequence(i);
        }

        private void OnTriggerEnter(Collider other)
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnTriggerEnter) != 0)
                    StartSequence(i);
        }

        private void OnTriggerExit(Collider other)
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnTriggerExit) != 0)
                    StartSequence(i);
        }

        private void OnCollisionEnter(Collision collision)
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnCollisionEnter) != 0)
                    StartSequence(i);
        }

        private void OnCollisionExit(Collision collision)
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnCollisionExit) != 0)
                    StartSequence(i);
        }

        private void OnBecameVisible()
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnBecameVisible) != 0)
                    StartSequence(i);
        }

        private void OnBecameInvisible()
        {
            for (int i = 0; i < Sequences.Count; i++)
                if ((Sequences[i].Triggers & TriggerMode.OnBecameInvisible) != 0)
                    StartSequence(i);
        }
    }
}