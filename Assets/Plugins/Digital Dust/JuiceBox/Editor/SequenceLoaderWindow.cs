using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceLoaderWindow: Utility window for loading a sequence from backup into the graph as a new strip.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class SequenceLoaderWindow : EditorWindow
    {
        private SequenceGraphView _graphView;
        private IReadOnlyList<string> _names;
        private Vector2 _scroll;

        public static void Show(SequenceGraphView graphView)
        {
            var w = CreateInstance<SequenceLoaderWindow>();
            w._graphView = graphView;
            w.titleContent = new GUIContent("Load Sequence");
            w.minSize = new Vector2(280f, 200f);
            w.ShowUtility();
        }

        private void OnEnable()
        {
            _names = SequenceBackupManager.GetAllBackupSequenceNames();
        }

        private void OnGUI()
        {
            if (_names == null || _names.Count == 0)
            {
                EditorGUILayout.HelpBox("No backup sequences found.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Choose a sequence to load its latest snapshot:",
               EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            int row = 0;
            foreach (string name in _names)
            {
                Rect r = EditorGUILayout.BeginHorizontal();
                if (row % 2 == 1)
                    EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.08f));
                EditorGUILayout.LabelField(name, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Load", GUILayout.Width(52f)))
                {
                    LoadLatest(name);
                    break;
                }
                if (GUILayout.Button("Delete", GUILayout.Width(52f)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete All Snapshots",
                        $"Delete all snapshots for \"{name}\"?\n\nThis cannot be undone.",
                        "Delete", "Cancel"))
                    {
                        SequenceBackupManager.DeleteBackups(name);
                        _names = SequenceBackupManager.GetAllBackupSequenceNames();
                    }
                    break;
                }
                EditorGUILayout.EndHorizontal();
                row++;
            }
            EditorGUILayout.EndScrollView();
        }

        private void LoadLatest(string sequenceName)
        {
            var snapshots = SequenceBackupManager.GetSnapshots(sequenceName);
            if (snapshots == null || snapshots.Count == 0)
            {
                Debug.LogWarning($"JuiceBox: No snapshots found for \"{sequenceName}\".");
                return;
            }

            var freshSeq = new Sequence(sequenceName);

            int latestIndex = snapshots.Count - 1;
            if (!SequenceBackupManager.RestoreSnapshot(freshSeq, latestIndex, null))
            {
                Debug.LogWarning($"JuiceBox: Failed to restore latest snapshot for \"{sequenceName}\".");
                return;
            }

            _graphView?.LoadSequence(freshSeq);
            Close();
        }
    }
}