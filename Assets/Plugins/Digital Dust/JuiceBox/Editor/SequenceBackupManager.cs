using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceBackupManager: Rolling JSON backups for sequence data, with time-gated automatic snapshots.
// ==============================================================================
namespace JuiceBox
{
    [InitializeOnLoad]
    public static class SequenceBackupManager
    {
        static SequenceBackupManager()
        {
            _cachedBackupDir = null;
        }

        public static event System.Action<string> OnSnapshotWritten;

        [System.Serializable]
        public class SnapshotEntry
        {
            public string timestamp;

            public string sequenceJson;
        }

        [System.Serializable]
        private class BackupFile
        {
            public string sequenceName;
            public List<SnapshotEntry> snapshots = new List<SnapshotEntry>();
        }

#if UNITY_6000_0_OR_NEWER
        private static readonly Dictionary<EntityId, System.DateTime> _animNextAllowedUtc =
           new Dictionary<EntityId, System.DateTime>();
#else
        private static readonly Dictionary<int, System.DateTime> _animNextAllowedUtc =
           new Dictionary<int, System.DateTime>();
#endif

        private static readonly Dictionary<string, string> _lastSnapshotHash =
           new Dictionary<string, string>();

        private static readonly Dictionary<string, (string newName, double triggerTime)> _pendingRenames =
           new Dictionary<string, (string, double)>();

        private static bool _renameUpdateHooked = false;

        internal static readonly char[] InvalidFileNameChars =
           { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public static string ValidateSequenceName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            if (name.IndexOfAny(InvalidFileNameChars) >= 0)
                return "Sequence name contains an invalid character. Avoid: \\ / : * ? \" < > |";
            char first = name[0];
            if (first == '.' || first == ' ')
                return "Sequence name must not start with a dot or space.";
            char last = name[name.Length - 1];
            if (last == '.' || last == ' ')
                return "Sequence name must not end with a dot or space.";
            return null;
        }

        private static string _cachedBackupDir;

        public static string GetBackupDirectory()
        {
            if (_cachedBackupDir != null) return _cachedBackupDir;

            string[] guids = AssetDatabase.FindAssets("SequenceBackupManager t:Script");
            if (guids.Length == 0)
            {
                Debug.LogWarning("JuiceBox: Could not locate SequenceBackupManager.cs. " +
                   "Backups will not be written until the file can be found.");
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            string relativeDir = Path.GetDirectoryName(assetPath);

            string projectRoot = Application.dataPath.Substring(
               0, Application.dataPath.Length - "Assets".Length);

            _cachedBackupDir = Path.GetFullPath(Path.Combine(projectRoot, relativeDir, "..", "Snapshots", "Sequences"));
            return _cachedBackupDir;
        }

        private static string GetFilePath(string sequenceName)
        {
            string dir = GetBackupDirectory();
            if (dir == null) return null;
            return Path.Combine(dir, sequenceName + ".json");
        }

        public static bool HasBackups(string sequenceName)
        {
            if (string.IsNullOrEmpty(sequenceName)) return false;
            string filePath = GetFilePath(sequenceName);
            return filePath != null && File.Exists(filePath);
        }

        internal static bool DeleteBackups(string sequenceName)
        {
            if (string.IsNullOrEmpty(sequenceName)) return false;
            string filePath = GetFilePath(sequenceName);
            if (filePath == null || !File.Exists(filePath)) return false;

            try
            {
                File.Delete(filePath);
                string meta = filePath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
                _lastSnapshotHash.Remove(filePath);
                AssetDatabase.Refresh();
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"JuiceBox: Failed to delete backup file for \"{sequenceName}\": {ex.Message}");
                return false;
            }
        }

        public static IReadOnlyList<string> GetAllBackupSequenceNames()
        {
            string dir = GetBackupDirectory();
            if (dir == null || !Directory.Exists(dir))
                return System.Array.Empty<string>();

            string[] files = Directory.GetFiles(dir, "*.json");
            var names = new List<string>(files.Length);
            foreach (string f in files)
                names.Add(Path.GetFileNameWithoutExtension(f));
            names.Sort(System.StringComparer.OrdinalIgnoreCase);
            return names;
        }

        private static BackupFile ReadFile(string filePath)
        {
            if (!File.Exists(filePath)) return new BackupFile();
            try
            {
                string raw = File.ReadAllText(filePath);
                var bf = new BackupFile();
                JsonUtility.FromJsonOverwrite(raw, bf);
                if (bf.snapshots == null) bf.snapshots = new List<SnapshotEntry>();
                return bf;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"JuiceBox: Failed to read backup file \"{filePath}\": {ex.Message}");
                return new BackupFile();
            }
        }

        private static void WriteFile(string filePath, BackupFile bf)
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

        private static bool WriteSnapshotIfChanged(Sequence sequence, string filePath, bool raiseEvent)
        {
            string seqJson = EditorJsonUtility.ToJson(sequence);
            string hash = ComputeSHA1(seqJson);

            if (!_lastSnapshotHash.TryGetValue(filePath, out string cachedHash))
            {
                if (File.Exists(filePath))
                {
                    BackupFile existing = ReadFile(filePath);
                    if (existing.snapshots != null && existing.snapshots.Count > 0)
                    {
                        string lastJson = existing.snapshots[existing.snapshots.Count - 1].sequenceJson;
                        cachedHash = ComputeSHA1(lastJson ?? "");
                        _lastSnapshotHash[filePath] = cachedHash;
                    }
                }
            }

            if (hash == cachedHash)
                return false;

            BackupFile bf = ReadFile(filePath);
            bf.sequenceName = sequence.Name;
            if (bf.snapshots == null) bf.snapshots = new List<SnapshotEntry>();

            if (bf.snapshots.Count >= JuiceBoxSettings.MaxSnapshotsPerSequence)
                bf.snapshots.RemoveAt(0);

            bf.snapshots.Add(new SnapshotEntry
            {
                timestamp = System.DateTime.UtcNow.ToString("o"),
                sequenceJson = seqJson
            });

            WriteFile(filePath, bf);
            AssetDatabase.Refresh();

            _lastSnapshotHash[filePath] = hash;

            if (raiseEvent)
                OnSnapshotWritten?.Invoke(sequence.Name);

            return true;
        }

        public static void ResetTimeGates()
        {
            _animNextAllowedUtc.Clear();
            _lastSnapshotHash.Clear();
            LayoutBackupManager.ResetHashCache();
        }

        public static bool TrySnapshot(Sequence sequence, JuiceBoxAnimation anim)
        {
            if (EditorApplication.isPlaying) return false;
            if (sequence == null || anim == null) return false;
            if (string.IsNullOrEmpty(sequence.Name)) return false;
            if (sequence.Property == null || sequence.Property.EffectCount == 0) return false;

#if UNITY_6000_0_OR_NEWER
            EntityId animId = anim.GetEntityId();
#else
            int animId = anim.GetInstanceID();
#endif

            if (_animNextAllowedUtc.TryGetValue(animId, out System.DateTime nextAllowed) &&
                System.DateTime.UtcNow < nextAllowed)
                return false;

            if (!_animNextAllowedUtc.ContainsKey(animId))
            {
                System.DateTime? lastTime = LayoutBackupManager.GetLastSnapshotTimestamp(anim);
                if (lastTime.HasValue)
                {
                    System.DateTime nextTime = lastTime.Value.AddMinutes(
                        JuiceBoxSettings.SnapshotMaxIntervalMinutes);
                    if (System.DateTime.UtcNow < nextTime)
                    {
                        _animNextAllowedUtc[animId] = nextTime;
                        return false;
                    }
                }
                else
                {
                    _animNextAllowedUtc[animId] = System.DateTime.UtcNow.AddMinutes(
                        JuiceBoxSettings.SnapshotMinIntervalMinutes);
                    return false;
                }
            }

            string filePath = GetFilePath(sequence.Name);
            bool anyWritten = false;

            if (filePath != null)
                anyWritten |= WriteSnapshotIfChanged(sequence, filePath, raiseEvent: true);

            anyWritten |= LayoutBackupManager.WriteIfChanged(anim);

            int delayMinutes = anyWritten
                ? JuiceBoxSettings.SnapshotMaxIntervalMinutes
                : JuiceBoxSettings.SnapshotMinIntervalMinutes;
            _animNextAllowedUtc[animId] = System.DateTime.UtcNow.AddMinutes(delayMinutes);

            return anyWritten;
        }

        public static bool ForceSnapshot(Sequence sequence)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("JuiceBox: Snapshot skipped - cannot snapshot while in play mode.");
                return false;
            }
            if (sequence == null) return false;
            if (string.IsNullOrWhiteSpace(sequence.Name))
                return false;
            if (sequence.Property == null || sequence.Property.EffectCount == 0)
            {
                Debug.LogWarning($"JuiceBox: ForceSnapshot - sequence \"{sequence.Name}\" has no " +
                   "effects. Add and dock at least one effect node before snapshotting.");
                return false;
            }

            string filePath = GetFilePath(sequence.Name);
            if (filePath == null) return false;

            return WriteSnapshotIfChanged(sequence, filePath, raiseEvent: false);
        }

        public static IReadOnlyList<SnapshotEntry> GetSnapshots(string sequenceName)
        {
            if (string.IsNullOrEmpty(sequenceName)) return System.Array.Empty<SnapshotEntry>();
            string filePath = GetFilePath(sequenceName);
            if (filePath == null || !File.Exists(filePath)) return System.Array.Empty<SnapshotEntry>();
            BackupFile bf = ReadFile(filePath);
            return bf.snapshots?.AsReadOnly() ??
               (IReadOnlyList<SnapshotEntry>)System.Array.Empty<SnapshotEntry>();
        }

        public static bool RestoreSnapshot(Sequence sequence, int index, Object dirtyTarget)
        {
            if (sequence == null || string.IsNullOrEmpty(sequence.Name)) return false;
            string filePath = GetFilePath(sequence.Name);
            if (filePath == null || !File.Exists(filePath)) return false;

            BackupFile bf = ReadFile(filePath);
            if (bf.snapshots == null || index < 0 || index >= bf.snapshots.Count) return false;

            string json = bf.snapshots[index].sequenceJson;
            if (string.IsNullOrEmpty(json)) return false;

            EditorJsonUtility.FromJsonOverwrite(json, sequence);
            Processor.FinalizeSerialization();

            if (dirtyTarget != null)
                EditorUtility.SetDirty(dirtyTarget);

            return true;
        }

        public static bool DeleteSnapshot(string sequenceName, int index)
        {
            if (string.IsNullOrEmpty(sequenceName)) return false;
            string filePath = GetFilePath(sequenceName);
            if (filePath == null || !File.Exists(filePath)) return false;

            BackupFile bf = ReadFile(filePath);
            if (bf.snapshots == null || index < 0 || index >= bf.snapshots.Count) return false;

            bf.snapshots.RemoveAt(index);
            WriteFile(filePath, bf);

            _lastSnapshotHash.Remove(filePath);

            return true;
        }

        public static void NotifySequenceRenamed(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName)) return;
            if (oldName == newName) return;

            string nameError = ValidateSequenceName(newName);
            if (nameError != null)
            {
                Debug.LogWarning($"JuiceBox: Cannot schedule backup rename from \"{oldName}\" " +
                   $"to \"{newName}\" - {nameError}");
                return;
            }

            _pendingRenames[oldName] = (newName, EditorApplication.timeSinceStartup + 10.0);
            EnsureRenameUpdateHooked();
        }

        private static void EnsureRenameUpdateHooked()
        {
            if (_renameUpdateHooked) return;
            EditorApplication.update += PollPendingRenames;
            _renameUpdateHooked = true;
        }

        private static void PollPendingRenames()
        {
            if (_pendingRenames.Count == 0)
            {
                EditorApplication.update -= PollPendingRenames;
                _renameUpdateHooked = false;
                return;
            }

            List<string> toProcess = null;
            foreach (var kvp in _pendingRenames)
            {
                if (EditorApplication.timeSinceStartup >= kvp.Value.triggerTime)
                {
                    if (toProcess == null) toProcess = new List<string>();
                    toProcess.Add(kvp.Key);
                }
            }

            if (toProcess == null) return;

            foreach (string oldName in toProcess)
            {
                string newName = _pendingRenames[oldName].newName;
                _pendingRenames.Remove(oldName);
                PerformFileRename(oldName, newName);
            }
        }

        internal static void PerformFileRename(string oldName, string newName)
        {
            string oldPath = GetFilePath(oldName);
            string newPath = GetFilePath(newName);
            if (oldPath == null || newPath == null) return;

            if (!File.Exists(oldPath)) return;

            try
            {
                if (File.Exists(newPath))
                {
                    File.Delete(newPath);
                    string existingMeta = newPath + ".meta";
                    if (File.Exists(existingMeta))
                        File.Delete(existingMeta);
                }

                File.Move(oldPath, newPath);

                string oldMeta = oldPath + ".meta";
                string newMeta = newPath + ".meta";
                if (File.Exists(oldMeta))
                    File.Move(oldMeta, newMeta);

                AssetDatabase.Refresh();

                if (_lastSnapshotHash.TryGetValue(oldPath, out string hash))
                {
                    _lastSnapshotHash[newPath] = hash;
                    _lastSnapshotHash.Remove(oldPath);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"JuiceBox: Failed to rename backup file from \"{oldName}\" " +
                   $"to \"{newName}\": {ex.Message}");
            }
        }
    }
}