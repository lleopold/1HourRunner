using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  JuiceBoxAnimationEditor: Custom inspector for JuiceBoxAnimation (UI Toolkit).
// ==============================================================================
namespace JuiceBox
{
    [CustomEditor(typeof(JuiceBoxAnimation))]
    public class JuiceBoxAnimationEditor : Editor
    {
        private JuiceBoxAnimation _anim;
        private IAnimationEditorComponent Ed => (IAnimationEditorComponent)_anim;

        private Label _summary;

        // Summary refresh cadence, milliseconds.
        private const long SummaryRefreshMs = 500;

        private void OnEnable()
        {
            Processor.FinalizeSerialization();
            _anim = (JuiceBoxAnimation)target;
            if (_anim.Sequences == null)
                _anim.Sequences = new List<Sequence>();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            // -- Summary line (centered grey mini) --
            _summary = new Label();
            _summary.style.unityTextAlign = TextAnchor.MiddleCenter;
            _summary.style.fontSize = 10f;
            _summary.style.color = new Color(0.5f, 0.5f, 0.5f);
            UpdateSummary();
            root.Add(_summary);

            var spacer = new VisualElement();
            spacer.style.height = 6f;
            root.Add(spacer);

            // -- Open button --
            var openBtn = new Button(OpenEditor) { text = "Open Sequence Editor" };
            openBtn.style.height = 34f;
            root.Add(openBtn);

            // Do NOT Bind a SerializedObject here: the inline animation graph lives on the
            // component, so a live binding re-serializes the whole payload every editor frame.
            // The summary reads the model directly and refreshes on a light schedule instead.
            root.schedule.Execute(UpdateSummary).Every(SummaryRefreshMs);

            return root;
        }

        private void OpenEditor()
        {
            SequenceEditorWindow.Open(_anim);
        }

        private void UpdateSummary()
        {
            if (_summary == null || _anim == null) return;
            int seqCount = _anim.Sequences?.Count ?? 0;
            int fxCount = CountTotalEffects();
            string seqWord = seqCount == 1 ? "sequence" : "sequences";
            string fxWord = fxCount == 1 ? "effect" : "effects";
            _summary.text = $"{seqCount} {seqWord}  \u00b7  {fxCount} {fxWord} total";
        }

        private int CountTotalEffects()
        {
            if (_anim.Sequences == null) return 0;
            int total = 0;
            for (int i = 0; i < _anim.Sequences.Count; i++)
            {
                if (_anim.Sequences[i].Property != null)
                    total += _anim.Sequences[i].Property.EffectCount;
                if (Ed._editorLayouts != null
                   && i < Ed._editorLayouts.Count
                   && Ed._editorLayouts[i].loopNodes != 0)
                    total++;
            }
            return total;
        }
    }
}