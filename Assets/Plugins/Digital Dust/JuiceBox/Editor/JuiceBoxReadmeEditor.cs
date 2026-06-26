using System.IO;
using UnityEditor;
using UnityEngine;

// ==============================================================================
//  JuiceBoxReadmeEditor: custom inspector that renders a JuiceBoxReadme asset as a
//  formatted welcome page, and highlights the asset in the Project view on import.
// ==============================================================================
namespace JuiceBox
{
    [CustomEditor(typeof(JuiceBoxReadme))]
    [InitializeOnLoad]
    public class JuiceBoxReadmeEditor : Editor
    {
        private const float SectionSpacing = 16f;
        private const float MaxIconWidth = 128f;

        private bool _initialized;
        private GUIStyle _titleStyle;
        private GUIStyle _headingStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _linkStyle;

        static JuiceBoxReadmeEditor()
        {
            EditorApplication.delayCall += HighlightOnImport;
        }

        static void HighlightOnImport()
        {
            string[] ids = AssetDatabase.FindAssets("t:JuiceBoxReadme");
            if (ids.Length == 0) return;

            string path = AssetDatabase.GUIDToAssetPath(ids[0]);
            JuiceBoxReadme readme = AssetDatabase.LoadAssetAtPath<JuiceBoxReadme>(path);
            if (readme == null || !readme.highlightOnImport) return;

            readme.highlightOnImport = false;
            EditorUtility.SetDirty(readme);
            AssetDatabase.SaveAssetIfDirty(readme);
            Selection.activeObject = readme;
        }

        public override void OnInspectorGUI()
        {
            JuiceBoxReadme readme = (JuiceBoxReadme)target;
            Init();

            GUILayout.BeginHorizontal();
            if (readme.icon != null)
            {
                float iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, MaxIconWidth);
                GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
            }
            GUILayout.Label(readme.title, _titleStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(SectionSpacing);

            if (readme.sections == null) return;

            for (int i = 0; i < readme.sections.Length; i++)
            {
                JuiceBoxReadme.Section section = readme.sections[i];

                if (!string.IsNullOrEmpty(section.heading))
                    GUILayout.Label(section.heading, _headingStyle);

                if (!string.IsNullOrEmpty(section.text))
                    GUILayout.Label(section.text, _bodyStyle);

                if (!string.IsNullOrEmpty(section.linkText))
                {
                    if (LinkLabel(new GUIContent(section.linkText)))
                        OpenLink(readme, section.url);
                }

                GUILayout.Space(SectionSpacing);
            }
        }

        private static void OpenLink(JuiceBoxReadme readme, string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                Application.OpenURL(url);
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(readme);
            string assetDir = Path.GetDirectoryName(assetPath);
            string fullPath = Path.Combine(assetDir, url);
            fullPath = Path.GetFullPath(fullPath);

            if (File.Exists(fullPath))
                Application.OpenURL("file:///" + fullPath.Replace('\\', '/'));
            else
                Debug.LogWarning("JuiceBox: Could not find file at " + fullPath);
        }

        private void Init()
        {
            if (_initialized) return;

            _bodyStyle = new GUIStyle(EditorStyles.label);
            _bodyStyle.wordWrap = true;
            _bodyStyle.fontSize = 14;
            _bodyStyle.richText = true;

            _titleStyle = new GUIStyle(_bodyStyle);
            _titleStyle.fontSize = 26;

            _headingStyle = new GUIStyle(_bodyStyle);
            _headingStyle.fontSize = 18;
            _headingStyle.fontStyle = FontStyle.Bold;

            _linkStyle = new GUIStyle(_bodyStyle);
            _linkStyle.wordWrap = false;
            _linkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
            _linkStyle.stretchWidth = false;

            _initialized = true;
        }

        private bool LinkLabel(GUIContent label)
        {
            Rect position = GUILayoutUtility.GetRect(label, _linkStyle);

            Handles.BeginGUI();
            Handles.color = _linkStyle.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);
            return GUI.Button(position, label, _linkStyle);
        }
    }
}