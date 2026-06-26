using UnityEditor;
using UnityEngine;

// ==============================================================================
//  JuiceBoxReadme: ScriptableObject holding the welcome and getting-started content
//  shown by JuiceBoxReadmeEditor, organized into titled sections with optional links.
// ==============================================================================
namespace JuiceBox
{
    public class JuiceBoxReadme : ScriptableObject
    {
        public Texture2D icon;
        public string title;
        public Section[] sections;
        public bool highlightOnImport = true;

        [System.Serializable]
        public class Section
        {
            public string heading;
            [TextArea(2, 10)] public string text;
            public string linkText;
            public string url;
        }

        [ContextMenu("Highlight On Import")]
        void ResetHighlightFlag()
        {
            highlightOnImport = true;
            EditorUtility.SetDirty(this);
        }

        [ContextMenu("Highlight On Import", true)]
        bool ValidateResetHighlightFlag()
        {
            return !highlightOnImport;
        }
    }
}