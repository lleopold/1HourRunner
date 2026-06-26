using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceRestoreWindow: Utility window for restoring a sequence from a previous snapshot.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class SequenceRestoreWindow : EditorWindow
    {
        private Sequence _sequence;
        private JuiceBoxAnimation _anim;
        private IReadOnlyList<SequenceBackupManager.SnapshotEntry> _snapshots;
        private Vector2 _scroll;

        public static void Show(Sequence sequence, JuiceBoxAnimation anim)
        {
            var w = CreateInstance<SequenceRestoreWindow>();
            w._sequence = sequence;
            w._anim = anim;
            w.titleContent = new GUIContent($"Restore - {sequence.Name}");
            w.minSize = new Vector2(320f, 180f);
            w.ShowUtility();
            w.Populate();
        }

        private void Populate()
        {
            _snapshots = _sequence != null
               ? SequenceBackupManager.GetSnapshots(_sequence.Name)
               : System.Array.Empty<SequenceBackupManager.SnapshotEntry>();
        }

        private void OnGUI()
        {
            if (_sequence == null) { Close(); return; }

            if (_snapshots == null || _snapshots.Count == 0)
            {
                EditorGUILayout.HelpBox(
                   $"No snapshots found for \"{_sequence.Name}\".", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(
               "Select a snapshot to restore. This will overwrite the current sequence.",
               EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int row = 0;
            for (int i = _snapshots.Count - 1; i >= 0; i--)
            {
                var entry = _snapshots[i];
                string timestamp = FormatTimestamp(entry.timestamp);

                Rect r = EditorGUILayout.BeginHorizontal();
                if (row % 2 == 1)
                    EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.08f));
                EditorGUILayout.LabelField(timestamp, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Restore", GUILayout.Width(60f)))
                {
                    TryRestore(i, timestamp);
                    break;
                }
                if (GUILayout.Button("Delete", GUILayout.Width(52f)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete Snapshot",
                        $"Delete the snapshot from {timestamp}?\n\nThis cannot be undone.",
                        "Delete", "Cancel"))
                    {
                        SequenceBackupManager.DeleteSnapshot(_sequence.Name, i);
                        Populate();
                        if (_snapshots.Count == 0)
                            SequenceBackupManager.DeleteBackups(_sequence.Name);
                        RefreshEditorRestoreButtons();
                        if (_snapshots.Count == 0) { Close(); return; }
                    }
                    break;
                }
                EditorGUILayout.EndHorizontal();
                row++;
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Delete All Snapshots"))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete All Snapshots",
                    $"Delete all snapshots for \"{_sequence.Name}\"?\n\nThis cannot be undone.",
                    "Delete All", "Cancel"))
                {
                    SequenceBackupManager.DeleteBackups(_sequence.Name);
                    RefreshEditorRestoreButtons();
                    Close();
                    return;
                }
            }
        }

        private void TryRestore(int index, string label)
        {
            if (!EditorUtility.DisplayDialog(
               "Restore Sequence",
               $"Restore \"{_sequence.Name}\" to the snapshot from\n{label}?\n\nThis will overwrite the current sequence data.",
               "Restore", "Cancel"))
                return;

            if (!SequenceBackupManager.RestoreSnapshot(_sequence, index, null))
            {
                Debug.LogWarning($"JuiceBox: Failed to restore snapshot for \"{_sequence.Name}\".");
                return;
            }

            ((ISequenceEditorData)_sequence).NeedsRebuild = true;
            SequenceLibrary.NotifySequenceChanged(_sequence.Name, _sequence, _anim);
            Close();
        }

        private static string FormatTimestamp(string isoTimestamp)
        {
            if (string.IsNullOrEmpty(isoTimestamp)) return "Unknown";
            if (System.DateTime.TryParse(isoTimestamp, null,
               System.Globalization.DateTimeStyles.RoundtripKind,
               out System.DateTime utc))
                return utc.ToLocalTime().ToString("dd MMM yyyy  h:mm tt");
            return isoTimestamp;
        }

        private static void RefreshEditorRestoreButtons()
        {
            var windows = Resources.FindObjectsOfTypeAll<SequenceEditorWindow>();
            for (int i = 0; i < windows.Length; i++)
                windows[i].RefreshRestoreButtons();
        }
    }
}