using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// ==============================================================================
//  JuiceBoxEditorHooks: Re-subscribes to static editor events after domain reload.
// ==============================================================================
namespace JuiceBox
{
    [InitializeOnLoad]
    internal static class JuiceBoxEditorHooks
    {
        [System.ThreadStatic]
        private static bool _writingObjectId;

        static JuiceBoxEditorHooks()
        {
            Processor.WriteObjectIdFunc = WriteObjectId;
            Processor.ResolveObjectIdFunc = ResolveObjectId;

            SequenceBackupManager.OnSnapshotWritten -= OnSnapshotWritten;
            SequenceBackupManager.OnSnapshotWritten += OnSnapshotWritten;

            SequenceLibrary.OnSequenceChanged -= OnSequenceChanged;
            SequenceLibrary.OnSequenceChanged += OnSequenceChanged;
        }

        private static void OnSnapshotWritten(string sequenceName)
        {
            if (!EditorWindow.HasOpenInstances<SequenceEditorWindow>()) return;

            var w = EditorWindow.GetWindow<SequenceEditorWindow>(false, null, false);
            w?.SetMessage("Snapshot saved.", SequenceEditorWindow.MessageSeverity.Info, 30f);
            w?.RefreshRestoreButtons();
        }

        private static void OnSequenceChanged(string sequenceName)
        {
            InternalEditorUtility.RepaintAllViews();

            if (!EditorWindow.HasOpenInstances<SequenceEditorWindow>()) return;

            var w = EditorWindow.GetWindow<SequenceEditorWindow>(false, null, false);
            w?.OnSequenceLibraryChanged(sequenceName);
        }

        private static string WriteObjectId(Object obj)
        {
            if (obj == null || _writingObjectId) return "";
            _writingObjectId = true;
            try
            {
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                return gid.identifierType != 0 ? gid.ToString() : "";
            }
            finally
            {
                _writingObjectId = false;
            }
        }

        private static Object ResolveObjectId(Object obj, string id)
        {
            if (obj != null) return obj;
            if (string.IsNullOrEmpty(id)) return null;
            if (!GlobalObjectId.TryParse(id, out var gid)) return null;
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
        }
    }
}