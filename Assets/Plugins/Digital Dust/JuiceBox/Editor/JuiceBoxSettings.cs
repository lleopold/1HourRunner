using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// ==============================================================================
//  JuiceBoxSettings: Project-scoped settings and the settings editor window.
// ==============================================================================
namespace JuiceBox
{
    internal static class JuiceBoxSettings
    {
        internal static readonly string[] PermanentHookScanClasses =
        {
         "JuiceBox.StandardFunctions",
         "JuiceBox.StandardFunctions+Easing",
      };

        [System.Serializable]
        private sealed class SettingsData
        {
            public int snapshotMaxIntervalMinutes = 60;
            public int snapshotMinIntervalMinutes = 10;
            public int maxSnapshotsPerSequence = 5;
            public int maxLayoutSnapshots = 3;
            public string hookScanClasses = "";
        }

        private static string _settingsPath;
        private static SettingsData _data;

        private static string SettingsPath
        {
            get
            {
                if (_settingsPath != null) return _settingsPath;
                var guids = AssetDatabase.FindAssets("JuiceBoxSettings t:Script");
                foreach (string guid in guids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (assetPath.EndsWith("JuiceBoxSettings.cs"))
                    {
                        int slash = assetPath.LastIndexOf('/');
                        _settingsPath = assetPath.Substring(0, slash) + "/JuiceBoxSettings.json";
                        return _settingsPath;
                    }
                }
                _settingsPath = "Assets/JuiceBox/Editor/JuiceBoxSettings.json";
                return _settingsPath;
            }
        }

        private static SettingsData Data
        {
            get
            {
                if (_data != null) return _data;
                string path = SettingsPath;
                if (System.IO.File.Exists(path))
                    _data = JsonUtility.FromJson<SettingsData>(
                       System.IO.File.ReadAllText(path));
                if (_data == null) _data = new SettingsData();
                return _data;
            }
        }

        private static void Save()
        {
            string path = SettingsPath;
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(Data, true));
            AssetDatabase.ImportAsset(path);
        }

        public static int SnapshotMaxIntervalMinutes
        {
            get => Data.snapshotMaxIntervalMinutes;
            set { Data.snapshotMaxIntervalMinutes = value; Save(); }
        }

        public static int SnapshotMinIntervalMinutes
        {
            get => Data.snapshotMinIntervalMinutes;
            set { Data.snapshotMinIntervalMinutes = value; Save(); }
        }

        public static int MaxSnapshotsPerSequence
        {
            get => Data.maxSnapshotsPerSequence;
            set { Data.maxSnapshotsPerSequence = value; Save(); }
        }

        public static int MaxLayoutSnapshots
        {
            get => Data.maxLayoutSnapshots;
            set { Data.maxLayoutSnapshots = value; Save(); }
        }

        public static string HookScanClasses
        {
            get => Data.hookScanClasses;
            set { Data.hookScanClasses = value ?? ""; Save(); }
        }

        public static List<string> AllHookScanClasses()
        {
            var list = new List<string>(PermanentHookScanClasses);
            string raw = Data.hookScanClasses;
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (string line in raw.Split('\n'))
            {
                string t = line.Trim();
                if (t.Length == 0) continue;
                if (list.Contains(t)) continue;
                list.Add(t);
            }
            return list;
        }

        // -- Loaded-assembly access ---------------------------------------------
        // On Unity 6.5+ this uses the AssemblyLoadContext-aware API, which omits
        // assemblies in the unloaded state; AppDomain.CurrentDomain.GetAssemblies
        // is the pre-6.5 fallback (flagged as unsafe under CoreCLR code reload).
        internal static IReadOnlyList<Assembly> GetLoadedAssemblies()
        {
#if UNITY_6000_5_OR_NEWER
            return UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }

    }

    internal sealed class JuiceBoxSettingsWindow : EditorWindow
    {

        public static void Open()
        {
            var w = GetWindow<JuiceBoxSettingsWindow>(utility: true);
            w.titleContent = new GUIContent("JuiceBox Settings");
            w.minSize = new Vector2(340f, 280f);
        }

        private int _maxInterval;
        private int _minInterval;
        private int _maxSnaps;
        private int _maxLayoutSnaps;
        private List<string> _userScanTypes = new List<string>();
        private int _asmIndex;
        private int _typeIndex;
        private Vector2 _listScroll;

        private static Dictionary<string, List<string>> s_typesByAssembly;
        private static string[] s_assemblyNames;

        private static readonly string[] s_skipPrefixes =
        {
         "UnityEngine", "UnityEditor", "Unity.", "System.", "mscorlib",
         "Mono.", "netstandard", "nunit.", "MEC", "TMPro", "Cinemachine",
      };

        private void OnEnable()
        {
            _maxInterval = JuiceBoxSettings.SnapshotMaxIntervalMinutes;
            _minInterval = JuiceBoxSettings.SnapshotMinIntervalMinutes;
            _maxSnaps = JuiceBoxSettings.MaxSnapshotsPerSequence;
            _maxLayoutSnaps = JuiceBoxSettings.MaxLayoutSnapshots;
            _userScanTypes = ParseUserTypes(JuiceBoxSettings.HookScanClasses);
            EnsureTypeCacheBuilt();
            _asmIndex = 0;
            _typeIndex = 0;
        }

        private void OnGUI()
        {
            const float labelW = 210f;
            const float numW = 46f;
            const float unitW = 28f;

            GUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Max time between snapshots", "Automatic snapshots are taken at most this often"), GUILayout.Width(labelW));
            _maxInterval = EditorGUILayout.IntField(_maxInterval, GUILayout.Width(numW));
            EditorGUILayout.LabelField("min", GUILayout.Width(unitW));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Min time between snapshots", "Suppress automatic snapshots if one was taken within this window"), GUILayout.Width(labelW));
            _minInterval = EditorGUILayout.IntField(_minInterval, GUILayout.Width(numW));
            EditorGUILayout.LabelField("min", GUILayout.Width(unitW));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Max snapshots per sequence", "Oldest snapshots are deleted when this limit is reached"), GUILayout.Width(labelW));
            _maxSnaps = EditorGUILayout.IntField(_maxSnaps, GUILayout.Width(numW));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Max layout snapshots", "Maximum number of graph layout snapshots to keep"), GUILayout.Width(labelW));
            _maxLayoutSnaps = EditorGUILayout.IntField(_maxLayoutSnaps, GUILayout.Width(numW));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6f);

            JuiceBoxAnimation activeAnim = null;
            if (EditorWindow.HasOpenInstances<SequenceEditorWindow>())
            {
                var seqWin = EditorWindow.GetWindow<SequenceEditorWindow>(false, null, false);
                if (seqWin != null)
                    activeAnim = seqWin.TargetAnimation;
            }

            EditorGUI.BeginDisabledGroup(activeAnim == null);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore Layout"))
            {
                if (activeAnim != null)
                    LayoutRestoreWindow.Show(activeAnim);
            }
            if (GUILayout.Button("Delete Current Layout Snapshot"))
            {
                if (activeAnim != null &&
                    EditorUtility.DisplayDialog("Delete Layout Backups",
                       "Delete all layout backup snapshots for this component?\n\nThis cannot be undone.",
                       "Delete", "Cancel"))
                {
                    LayoutBackupManager.DeleteAllSnapshots(activeAnim);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (activeAnim == null)
                EditorGUILayout.HelpBox("Open a sequence editor graph to enable layout backup controls.", MessageType.Info);

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Additional static method classes", EditorStyles.boldLabel);
            GUILayout.Space(2f);

            if (s_assemblyNames != null && s_assemblyNames.Length > 0)
            {
                _asmIndex = Mathf.Clamp(_asmIndex, 0, s_assemblyNames.Length - 1);

                EditorGUILayout.BeginHorizontal();
                int newAsm = EditorGUILayout.Popup(_asmIndex, s_assemblyNames);
                if (newAsm != _asmIndex) { _asmIndex = newAsm; _typeIndex = 0; }

                string[] filtered = GetFilteredTypeNames(s_assemblyNames[_asmIndex]);
                _typeIndex = Mathf.Clamp(_typeIndex, 0, Mathf.Max(0, filtered.Length - 1));

                EditorGUI.BeginDisabledGroup(filtered.Length == 0);
                _typeIndex = EditorGUILayout.Popup(
                   _typeIndex,
                   filtered.Length > 0 ? filtered : new[] { "(none available)" });
                if (GUILayout.Button("+", GUILayout.Width(26f)) && filtered.Length > 0)
                    _userScanTypes.Add(filtered[_typeIndex]);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No eligible types found.", MessageType.Info);
            }

            GUILayout.Space(4f);

            int totalItems = JuiceBoxSettings.PermanentHookScanClasses.Length
                           + _userScanTypes.Count;
            float listH = Mathf.Clamp(totalItems * 20f + 4f, 44f, 160f);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(listH));

            foreach (string t in JuiceBoxSettings.PermanentHookScanClasses)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(t);
                GUILayout.Space(26f);
                EditorGUILayout.EndHorizontal();
            }

            int removeAt = -1;
            for (int i = 0; i < _userScanTypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_userScanTypes[i]);
                if (GUILayout.Button("\u2212", GUILayout.Width(22f))) removeAt = i;
                EditorGUILayout.EndHorizontal();
            }
            if (removeAt >= 0) _userScanTypes.RemoveAt(removeAt);

            EditorGUILayout.EndScrollView();

            GUILayout.Space(8f);

            GUILayout.Space(8f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                Apply();
                Close();
            }
            if (GUILayout.Button("Cancel"))
                Close();
            EditorGUILayout.EndHorizontal();
        }

        private void Apply()
        {
            _maxInterval = Mathf.Clamp(_maxInterval, 1, 1440);
            _minInterval = Mathf.Clamp(_minInterval, 1, 60);
            _maxSnaps = Mathf.Clamp(_maxSnaps, 1, 20);
            _maxLayoutSnaps = Mathf.Clamp(_maxLayoutSnaps, 1, 20);

            JuiceBoxSettings.SnapshotMaxIntervalMinutes = _maxInterval;
            JuiceBoxSettings.SnapshotMinIntervalMinutes = _minInterval;
            JuiceBoxSettings.MaxSnapshotsPerSequence = _maxSnaps;
            JuiceBoxSettings.MaxLayoutSnapshots = _maxLayoutSnaps;
            JuiceBoxSettings.HookScanClasses = string.Join("\n", _userScanTypes);

            DelegatePicker.InvalidateStaticTypeCache();
        }

        private static void EnsureTypeCacheBuilt()
        {
            if (s_typesByAssembly != null) return;
            s_typesByAssembly = new Dictionary<string, List<string>>();

            var assemblies = JuiceBoxSettings.GetLoadedAssemblies();
            foreach (var asm in assemblies)
            {
                string asmName = asm.GetName().Name;

                bool skip = false;
                foreach (string prefix in s_skipPrefixes)
                    if (asmName.StartsWith(prefix)) { skip = true; break; }
                if (skip) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                var eligible = new List<string>();
                foreach (var t in types)
                {
                    if (!t.IsClass || t.IsGenericTypeDefinition) continue;
                    if (t.FullName == null) continue;
                    var methods = t.GetMethods(
                       BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    if (methods.Length > 0) eligible.Add(t.FullName);
                }

                if (eligible.Count == 0) continue;
                eligible.Sort(StringComparer.OrdinalIgnoreCase);
                s_typesByAssembly[asmName] = eligible;
            }

            var keys = new List<string>(s_typesByAssembly.Keys);
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            s_assemblyNames = keys.ToArray();
        }

        private string[] GetFilteredTypeNames(string asmName)
        {
            if (s_typesByAssembly == null ||
                !s_typesByAssembly.TryGetValue(asmName, out var all))
                return Array.Empty<string>();

            var result = new List<string>();
            foreach (string t in all)
            {
                bool alreadyPresent = false;
                foreach (string p in JuiceBoxSettings.PermanentHookScanClasses)
                    if (t == p) { alreadyPresent = true; break; }
                if (alreadyPresent) continue;
                if (_userScanTypes.Contains(t)) continue;
                result.Add(t);
            }
            return result.ToArray();
        }

        private static List<string> ParseUserTypes(string raw)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(raw)) return result;
            foreach (string line in raw.Split('\n'))
            {
                string t = line.Trim();
                if (t.Length > 0) result.Add(t);
            }
            return result;
        }
    }
}