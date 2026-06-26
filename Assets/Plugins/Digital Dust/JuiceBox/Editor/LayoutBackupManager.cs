using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

// ==============================================================================
//  LayoutBackupManager: Rolling JSON backups for graph editor layout data (node positions, loop bitmasks).
// ==============================================================================
namespace JuiceBox
{
    [InitializeOnLoad]
    internal static class LayoutBackupManager
    {
        static LayoutBackupManager()
        {
            _cachedBackupDir = null;
        }

        [System.Serializable]
        public class SnapshotEntry
        {
            public string timestamp;
            public string layoutJson;
        }

        [System.Serializable]
        private class LayoutBackupFile
        {
            public string ownerGlobalId;
            public List<SnapshotEntry> snapshots = new List<SnapshotEntry>();
        }

        [System.Serializable]
        private class LayoutDataWrapper
        {
            public List<JuiceBoxAnimation.SequenceEditorLayout> layouts =
               new List<JuiceBoxAnimation.SequenceEditorLayout>();
        }

        private static readonly Dictionary<string, string> _lastSnapshotHash =
           new Dictionary<string, string>();

        private static string _cachedBackupDir;

        public static string GetBackupDirectory()
        {
            if (_cachedBackupDir != null) return _cachedBackupDir;

            string[] guids = AssetDatabase.FindAssets("LayoutBackupManager t:Script");
            if (guids.Length == 0)
            {
                Debug.LogWarning("JuiceBox: Could not locate LayoutBackupManager.cs. " +
                   "Layout backups will not be written until the file can be found.");
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string relativeDir = Path.GetDirectoryName(assetPath);

            string projectRoot = Application.dataPath.Substring(
               0, Application.dataPath.Length - "Assets".Length);

            _cachedBackupDir = Path.GetFullPath(
               Path.Combine(projectRoot, relativeDir, "..", "Snapshots", "Layouts"));
            return _cachedBackupDir;
        }

        private static string EnsureBackupId(JuiceBoxAnimation anim)
        {
            if (!string.IsNullOrEmpty(((IAnimationEditorComponent)anim)._layoutBackupId))
                return ((IAnimationEditorComponent)anim)._layoutBackupId;

            ((IAnimationEditorComponent)anim)._layoutBackupId = System.Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(anim);
            return ((IAnimationEditorComponent)anim)._layoutBackupId;
        }

        private static string GetOwnerGlobalId(JuiceBoxAnimation anim)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(anim).ToString();
        }

        private static string GetFilePath(JuiceBoxAnimation anim)
        {
            string dir = GetBackupDirectory();
            if (dir == null) return null;
            string id = EnsureBackupId(anim);
            return Path.Combine(dir, id + ".json");
        }

        private static LayoutBackupFile ReadFile(string filePath)
        {
            if (!File.Exists(filePath)) return new LayoutBackupFile();
            try
            {
                string raw = File.ReadAllText(filePath);
                var bf = new LayoutBackupFile();
                JsonUtility.FromJsonOverwrite(raw, bf);
                if (bf.snapshots == null) bf.snapshots = new List<SnapshotEntry>();
                return bf;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(
                   $"JuiceBox: Failed to read layout backup \"{filePath}\": {ex.Message}");
                return new LayoutBackupFile();
            }
        }

        private static void WriteFile(string filePath, LayoutBackupFile bf)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, JsonUtility.ToJson(bf, prettyPrint: true));
        }

        private static string ComputeSHA1(string input)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                return System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static string SerializeLayouts(JuiceBoxAnimation anim)
        {
            var wrapper = new LayoutDataWrapper();
            if (((IAnimationEditorComponent)anim)._editorLayouts != null)
                wrapper.layouts = ((IAnimationEditorComponent)anim)._editorLayouts;
            return EditorJsonUtility.ToJson(wrapper);
        }

        private static List<JuiceBoxAnimation.SequenceEditorLayout> DeserializeLayouts(
           string json)
        {
            var wrapper = new LayoutDataWrapper();
            EditorJsonUtility.FromJsonOverwrite(json, wrapper);
            return wrapper.layouts ?? new List<JuiceBoxAnimation.SequenceEditorLayout>();
        }

        private static bool CheckAndResolveOwnership(
           JuiceBoxAnimation anim, ref string filePath)
        {
            if (!File.Exists(filePath)) return true;

            LayoutBackupFile bf = ReadFile(filePath);
            string currentOwner = GetOwnerGlobalId(anim);

            if (!string.IsNullOrEmpty(bf.ownerGlobalId)
                && bf.ownerGlobalId != currentOwner)
            {
                ((IAnimationEditorComponent)anim)._layoutBackupId = System.Guid.NewGuid().ToString("N");
                EditorUtility.SetDirty(anim);
                filePath = GetFilePath(anim);
                return true;
            }

            return true;
        }

        private static bool WriteSnapshotIfChanged(
           JuiceBoxAnimation anim, string filePath)
        {
            string layoutJson = SerializeLayouts(anim);
            string hash = ComputeSHA1(layoutJson);

            if (!_lastSnapshotHash.TryGetValue(filePath, out string cachedHash))
            {
                if (File.Exists(filePath))
                {
                    LayoutBackupFile existing = ReadFile(filePath);
                    if (existing.snapshots != null && existing.snapshots.Count > 0)
                    {
                        string lastJson = existing.snapshots[
                           existing.snapshots.Count - 1].layoutJson;
                        cachedHash = ComputeSHA1(lastJson ?? "");
                        _lastSnapshotHash[filePath] = cachedHash;
                    }
                }
            }

            if (hash == cachedHash)
                return false;

            LayoutBackupFile bf = ReadFile(filePath);
            bf.ownerGlobalId = GetOwnerGlobalId(anim);
            if (bf.snapshots == null) bf.snapshots = new List<SnapshotEntry>();

            if (bf.snapshots.Count >= JuiceBoxSettings.MaxLayoutSnapshots)
                bf.snapshots.RemoveAt(0);

            bf.snapshots.Add(new SnapshotEntry
            {
                timestamp = System.DateTime.UtcNow.ToString("o"),
                layoutJson = layoutJson
            });

            WriteFile(filePath, bf);
            AssetDatabase.Refresh();

            _lastSnapshotHash[filePath] = hash;

            return true;
        }

        internal static void ResetHashCache()
        {
            _lastSnapshotHash.Clear();
        }

        internal static System.DateTime? GetLastSnapshotTimestamp(JuiceBoxAnimation anim)
        {
            if (anim == null) return null;
            string filePath = GetFilePath(anim);
            if (filePath == null || !File.Exists(filePath)) return null;

            LayoutBackupFile bf = ReadFile(filePath);
            if (bf.snapshots == null || bf.snapshots.Count == 0) return null;

            string lastTs = bf.snapshots[bf.snapshots.Count - 1].timestamp;
            if (System.DateTime.TryParse(lastTs, null,
               System.Globalization.DateTimeStyles.RoundtripKind,
               out System.DateTime lastTime))
                return lastTime;

            return null;
        }

        private static bool AllSequencesUnnamed(JuiceBoxAnimation anim)
        {
            if (anim == null || anim.Sequences == null || anim.Sequences.Count == 0)
                return true;
            for (int i = 0; i < anim.Sequences.Count; i++)
                if (!string.IsNullOrWhiteSpace(anim.Sequences[i].Name))
                    return false;
            return true;
        }

        public static bool WriteIfChanged(JuiceBoxAnimation anim)
        {
            if (EditorApplication.isPlaying) return false;
            if (anim == null) return false;
            if (AllSequencesUnnamed(anim)) return false;
            if (((IAnimationEditorComponent)anim)._editorLayouts == null || ((IAnimationEditorComponent)anim)._editorLayouts.Count == 0)
                return false;

            string filePath = GetFilePath(anim);
            if (filePath == null) return false;

            if (!CheckAndResolveOwnership(anim, ref filePath))
                return false;

            return WriteSnapshotIfChanged(anim, filePath);
        }

        public static bool ForceSnapshot(JuiceBoxAnimation anim)
        {
            if (EditorApplication.isPlaying) return false;
            if (anim == null) return false;
            if (AllSequencesUnnamed(anim)) return false;
            if (((IAnimationEditorComponent)anim)._editorLayouts == null || ((IAnimationEditorComponent)anim)._editorLayouts.Count == 0)
                return false;

            string filePath = GetFilePath(anim);
            if (filePath == null) return false;

            if (!CheckAndResolveOwnership(anim, ref filePath))
                return false;

            return WriteSnapshotIfChanged(anim, filePath);
        }

        public static IReadOnlyList<SnapshotEntry> GetSnapshots(
           JuiceBoxAnimation anim)
        {
            if (anim == null || string.IsNullOrEmpty(((IAnimationEditorComponent)anim)._layoutBackupId))
                return System.Array.Empty<SnapshotEntry>();

            string filePath = GetFilePath(anim);
            if (filePath == null || !File.Exists(filePath))
                return System.Array.Empty<SnapshotEntry>();

            LayoutBackupFile bf = ReadFile(filePath);
            return bf.snapshots?.AsReadOnly()
               ?? (IReadOnlyList<SnapshotEntry>)System.Array.Empty<SnapshotEntry>();
        }

        public static bool RestoreSnapshot(JuiceBoxAnimation anim, int index)
        {
            if (anim == null || string.IsNullOrEmpty(((IAnimationEditorComponent)anim)._layoutBackupId))
                return false;

            string filePath = GetFilePath(anim);
            if (filePath == null || !File.Exists(filePath)) return false;

            LayoutBackupFile bf = ReadFile(filePath);
            if (bf.snapshots == null || index < 0 || index >= bf.snapshots.Count)
                return false;

            string json = bf.snapshots[index].layoutJson;
            if (string.IsNullOrEmpty(json)) return false;

            ((IAnimationEditorComponent)anim)._editorLayouts = DeserializeLayouts(json);
            EditorUtility.SetDirty(anim);
            return true;
        }

        public static bool DeleteSnapshot(JuiceBoxAnimation anim, int index)
        {
            if (anim == null || string.IsNullOrEmpty(((IAnimationEditorComponent)anim)._layoutBackupId))
                return false;

            string filePath = GetFilePath(anim);
            if (filePath == null || !File.Exists(filePath)) return false;

            LayoutBackupFile bf = ReadFile(filePath);
            if (bf.snapshots == null || index < 0 || index >= bf.snapshots.Count)
                return false;

            bf.snapshots.RemoveAt(index);
            WriteFile(filePath, bf);
            _lastSnapshotHash.Remove(filePath);
            return true;
        }

        public static void DeleteAllSnapshots(JuiceBoxAnimation anim)
        {
            if (anim == null || string.IsNullOrEmpty(((IAnimationEditorComponent)anim)._layoutBackupId))
                return;

            string filePath = GetFilePath(anim);
            if (filePath == null) return;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                string metaPath = filePath + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
                _lastSnapshotHash.Remove(filePath);
                AssetDatabase.Refresh();
            }
        }
    }

    internal sealed class LayoutRestoreWindow : EditorWindow
    {
        private JuiceBoxAnimation _anim;
        private IReadOnlyList<LayoutBackupManager.SnapshotEntry> _snapshots;
        private Vector2 _scroll;

        public static void Show(JuiceBoxAnimation anim)
        {
            var w = CreateInstance<LayoutRestoreWindow>();
            w._anim = anim;
            w.titleContent = new GUIContent("Restore Layout");
            w.minSize = new Vector2(320f, 180f);
            w.ShowUtility();
            w.Populate();
        }

        private void Populate()
        {
            _snapshots = _anim != null
               ? LayoutBackupManager.GetSnapshots(_anim)
               : System.Array.Empty<LayoutBackupManager.SnapshotEntry>();
        }

        private void OnGUI()
        {
            if (_anim == null) { Close(); return; }

            if (_snapshots == null || _snapshots.Count == 0)
            {
                EditorGUILayout.HelpBox(
                   "No layout snapshots found for this component.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(
               "Select a snapshot to restore. This will overwrite all node positions.",
               EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = _snapshots.Count - 1; i >= 0; i--)
            {
                var entry = _snapshots[i];
                string timestamp = FormatTimestamp(entry.timestamp);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(timestamp, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Restore", GUILayout.Width(60f)))
                {
                    TryRestore(i, timestamp);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void TryRestore(int index, string label)
        {
            if (!EditorUtility.DisplayDialog(
               "Restore Layout",
               $"Restore layout to the snapshot from\n{label}?\n\n" +
               "This will overwrite all node positions, spaces, and loop nodes.",
               "Restore", "Cancel"))
                return;

            if (!LayoutBackupManager.RestoreSnapshot(_anim, index))
            {
                Debug.LogWarning("JuiceBox: Failed to restore layout snapshot.");
                return;
            }

            if (EditorWindow.HasOpenInstances<SequenceEditorWindow>())
            {
                var w = EditorWindow.GetWindow<SequenceEditorWindow>(false, null, false);
                if (w != null && w.TargetAnimation == _anim)
                    w.Rebuild();
            }

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
    }
}