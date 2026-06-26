using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  SequenceEditorWindow: Main editor window hosting the sequence graph view and toolbar.
// ==============================================================================
namespace JuiceBox
{
    public class SequenceEditorWindow : EditorWindow
    {
        public static void Open()
        {
            var w = GetWindow<SequenceEditorWindow>();
            w.titleContent = new GUIContent("Sequence Editor");
            w.minSize = new Vector2(860f, 440f);
        }

        public static void Open(JuiceBoxAnimation target)
        {
            var w = GetWindow<SequenceEditorWindow>();
            w.titleContent = new GUIContent("Sequence Editor");
            w.minSize = new Vector2(860f, 440f);
            w.SetTarget(target);
        }

        [SerializeField] private JuiceBoxAnimation _target;
        [SerializeField] private string _targetGlobalId;
        [SerializeField] private float _savedZoom = 1f;
        [SerializeField] private Vector3 _savedViewPos;
        private SequenceGraphView _graphView;
        private VisualElement _messageBar;
        private Label _messageLabel;
        private Button _addEffectBtn;

        internal bool SuppressLibraryRebuild;

        internal JuiceBoxAnimation TargetAnimation => _target;

        private VisualElement _hTrack, _hThumb, _vTrack, _vThumb;
        private bool _hDragging, _vDragging;
        private float _dragStartMouse, _dragStartScroll, _dragMaxScroll, _dragScrollRange;
        private IVisualElementScheduledItem _vRepeat, _hRepeat;
        private int _vRepeatPointerId = -1, _hRepeatPointerId = -1;
        private float _vRepeatPointerY, _hRepeatPointerX;
        private const float ScrollbarSize = 12f;
        private const float MsgBarH = 28f;
        private const long RepeatInitialMs = 600;
        private const long RepeatIntervalMs = 120;

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            BuildUI();
            if (_target != null)
            {
                Rebuild();
                return;
            }
            if (Selection.activeGameObject != null)
            {
                var anim = Selection.activeGameObject.GetComponent<JuiceBoxAnimation>();
                if (anim != null) SetTarget(anim);
            }
        }

        private void OnDisable()
        {
            if (_graphView != null)
            {
                _savedZoom = _graphView.ViewScale;
                _savedViewPos = _graphView.ViewPosition;
                if (!string.IsNullOrEmpty(_targetGlobalId))
                    EditorPrefs.SetFloat("JuiceBox_Zoom_" + _targetGlobalId, _savedZoom);
            }
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            rootVisualElement.Clear();
            _graphView = null; _messageBar = null; _messageLabel = null;
            _addEffectBtn = null;
            _vRepeat?.Pause(); _vRepeat = null;
            _hRepeat?.Pause(); _hRepeat = null;
            _vRepeatPointerId = -1; _hRepeatPointerId = -1;
            _hTrack = null; _hThumb = null; _vTrack = null; _vThumb = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && _target != null)
            {
                _targetGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(_target).ToString();
            }

            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                if (_target == null && !string.IsNullOrEmpty(_targetGlobalId))
                {
                    if (GlobalObjectId.TryParse(_targetGlobalId, out var gid))
                        _target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid)
                           as JuiceBoxAnimation;
                }
                if (_target == null && Selection.activeGameObject != null)
                    _target = Selection.activeGameObject.GetComponent<JuiceBoxAnimation>();
                Rebuild();
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject == null) return;
            var anim = Selection.activeGameObject.GetComponent<JuiceBoxAnimation>();
            if (anim != null && anim != _target) SetTarget(anim);
        }

        public void SetTarget(JuiceBoxAnimation anim)
        {
            if (anim != _target)
            {
                SequenceBackupManager.ResetTimeGates();
            }
            _target = anim;
            _targetGlobalId = anim != null
               ? GlobalObjectId.GetGlobalObjectIdSlow(anim).ToString()
               : "";
            Rebuild();
        }

        internal enum MessageSeverity { Info, Warning, Error }

        private readonly string[] _msgSlots = new string[3];
        private readonly IVisualElementScheduledItem[] _msgTimers = new IVisualElementScheduledItem[3];
        private string _flashMessage;
        private bool _flashElevated;
        private IVisualElementScheduledItem _flashTimer;

        internal void SetMessage(string text, MessageSeverity severity, float timeoutSeconds = 0f)
        {
            if (timeoutSeconds > 0f && !string.IsNullOrEmpty(text))
            {
                _flashMessage = text;
                _flashElevated = severity != MessageSeverity.Info;
                _flashTimer?.Pause();
                _flashTimer = rootVisualElement.schedule.Execute(() =>
                {
                    _flashMessage = null;
                    RenderMessage();
                }).StartingIn((long)(timeoutSeconds * 1000f));
            }
            else
            {
                int idx = (int)severity;
                _msgSlots[idx] = text ?? "";
                _msgTimers[idx]?.Pause();
                _msgTimers[idx] = null;
            }

            RenderMessage();
        }

        private void RenderMessage()
        {
            if (_messageLabel == null) return;
            var th = _graphView?.Theme ?? JbTheme.Default;

            string text; bool elevated;
            if (!string.IsNullOrEmpty(_flashMessage))
            { text = _flashMessage; elevated = _flashElevated; }
            else if (!string.IsNullOrEmpty(_msgSlots[(int)MessageSeverity.Error]))
            { text = _msgSlots[(int)MessageSeverity.Error]; elevated = true; }
            else if (!string.IsNullOrEmpty(_msgSlots[(int)MessageSeverity.Warning]))
            { text = _msgSlots[(int)MessageSeverity.Warning]; elevated = true; }
            else
            { text = _msgSlots[(int)MessageSeverity.Info]; elevated = false; }

            _messageLabel.text = text;
            _messageLabel.style.color = elevated ? th.MsgWarnText : th.MsgBarText;
            _messageBar.style.backgroundColor = elevated ? th.MsgWarnBg : th.MsgBarBg;
            _messageBar.style.borderBottomColor = elevated ? th.MsgWarnBorder : th.MsgBarBorder;
        }

        internal void OnSequenceLibraryChanged(string sequenceName)
        {
            if (SuppressLibraryRebuild) return;
            if (_target == null) return;
            if (_target.Sequences == null) { Repaint(); return; }

            for (int i = 0; i < _target.Sequences.Count; i++)
            {
                if (_target.Sequences[i].Name == sequenceName)
                {
                    Rebuild();
                    return;
                }
            }

            Repaint();
        }

        internal void RefreshRestoreButtons()
        {
            _graphView?.RefreshRestoreButtons();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var t = JbTheme.Default;
            _messageBar = new VisualElement();
            _messageBar.style.height = 28f;
            _messageBar.style.flexShrink = 0f;
            _messageBar.style.backgroundColor = t.MsgBarBg;
            _messageBar.style.borderBottomWidth = 1f;
            _messageBar.style.borderBottomColor = t.MsgBarBorder;
            _messageBar.style.flexDirection = FlexDirection.Row;
            _messageBar.style.alignItems = Align.Center;
            _messageBar.style.paddingLeft = 12f;
            _messageBar.style.paddingRight = 8f;
            rootVisualElement.Add(_messageBar);

            _messageLabel = new Label("") { pickingMode = PickingMode.Ignore };
            _messageLabel.style.fontSize = 11f;
            _messageLabel.style.flexGrow = 1f;
            _messageLabel.style.color = t.MsgBarText;
            _messageBar.Add(_messageLabel);

            var addSeqBtn = new Button(() => _graphView?.AddSequence(Vector2.zero))
            {
                text = "+ Sequence"
            };
            addSeqBtn.style.height = 20f;
            addSeqBtn.style.paddingLeft = addSeqBtn.style.paddingRight = 10f;
            addSeqBtn.style.fontSize = 10f;
            addSeqBtn.style.marginRight = 6f;
            addSeqBtn.tooltip = "Create a new animation sequence on this component";
            _messageBar.Add(addSeqBtn);

            _addEffectBtn = new Button(() => OnAddEffectClicked())
            {
                text = "+ Effect"
            };
            _addEffectBtn.style.height = 20f;
            _addEffectBtn.style.paddingLeft = _addEffectBtn.style.paddingRight = 10f;
            _addEffectBtn.style.fontSize = 10f;
            _addEffectBtn.style.marginRight = 6f;
            _addEffectBtn.tooltip = "Add a floating effect or control node";
            _messageBar.Add(_addEffectBtn);

            var loadBtn = new Button(() => SequenceLoaderWindow.Show(_graphView))
            {
                text = "Load..."
            };
            loadBtn.style.height = 20f;
            loadBtn.style.paddingLeft = loadBtn.style.paddingRight = 10f;
            loadBtn.style.fontSize = 10f;
            loadBtn.style.marginRight = 6f;
            loadBtn.tooltip = "Load a sequence from another JuiceBoxAnimation component";
            _messageBar.Add(loadBtn);

            var snapshotBtn = new Button(() => TakeSnapshotsAll())
            {
                text = "Snapshot"
            };
            snapshotBtn.style.height = 20f;
            snapshotBtn.style.paddingLeft = snapshotBtn.style.paddingRight = 10f;
            snapshotBtn.style.fontSize = 10f;
            snapshotBtn.style.marginRight = 6f;
            snapshotBtn.tooltip = "Save a snapshot of all sequences for later restore";
            _messageBar.Add(snapshotBtn);

            var settingsBtn = new Button(() => JuiceBoxSettingsWindow.Open());
            settingsBtn.style.height = 20f;
            settingsBtn.style.width = 22f;
            settingsBtn.style.paddingLeft = settingsBtn.style.paddingRight = 0f;
            settingsBtn.style.marginLeft = 4f;
            var gearIcon = new Image { image = EditorGUIUtility.IconContent("d_Settings").image };
            gearIcon.style.width = 14f;
            gearIcon.style.height = 14f;
            gearIcon.style.alignSelf = Align.Center;
            gearIcon.style.marginTop = 1f;
            settingsBtn.Add(gearIcon);
            settingsBtn.tooltip = "Open JuiceBox settings";
            _messageBar.Add(settingsBtn);

            _graphView = new SequenceGraphView(this);
            _graphView.style.flexGrow = 1f;
            rootVisualElement.Add(_graphView);

            BuildScrollbars();
        }

        private void OnAddEffectClicked()
        {
            if (_graphView == null || _target == null) return;

            Rect btnWorld = _addEffectBtn.worldBound;
            Rect gvWorld = _graphView.worldBound;
            Vector2 worldPos = new Vector2(btnWorld.center.x, gvWorld.y + 40f);
            Vector2 canvasPos = _graphView.WorldToCanvas(worldPos);

            _graphView.ShowAddEffectMenu(canvasPos);
        }

        internal void Rebuild()
        {
            if (_graphView == null) BuildUI();
            if (_target == null)
            {
                SetMessage("No JuiceBox Animation selected.", MessageSeverity.Info);
                _graphView.ClearStrips();
                UpdateScrollbars();
                return;
            }
            Processor.FinalizeSerialization();
            titleContent = new GUIContent($"Sequence Editor - {_target.gameObject.name}");
            SetMessage("", MessageSeverity.Info);
            _graphView.SetTarget(_target);

            string zoomKey = !string.IsNullOrEmpty(_targetGlobalId)
               ? "JuiceBox_Zoom_" + _targetGlobalId : "";
            float z = !string.IsNullOrEmpty(zoomKey) && EditorPrefs.HasKey(zoomKey)
               ? EditorPrefs.GetFloat(zoomKey) : _savedZoom;
            Vector3 p = _savedViewPos;
            _graphView.schedule.Execute(() =>
            {
                _graphView.UpdateViewTransform(p, new Vector3(z, z, 1f));
                _graphView.schedule.Execute(() => UpdateScrollbars());
            });
            UpdateScrollbars();
        }

        private void BuildScrollbars()
        {
            var trackColor = new Color(0f, 0f, 0f, 0.30f);
            var thumbColor = new Color(1f, 1f, 1f, 0.20f);

            BuildVerticalScrollTrack(trackColor, thumbColor);
            BuildHorizontalScrollTrack(trackColor, thumbColor);
            BuildScrollCorner(trackColor);
            BuildVerticalThumbDrag();
            BuildHorizontalThumbDrag();
        }

        private void BuildVerticalScrollTrack(Color trackColor, Color thumbColor)
        {
            // -- Vertical track (right edge, below message bar, above H-scrollbar) --
            _vTrack = new VisualElement { pickingMode = PickingMode.Position };
            _vTrack.style.position = Position.Absolute;
            _vTrack.style.right = 0f;
            _vTrack.style.top = MsgBarH;
            _vTrack.style.width = ScrollbarSize;
            _vTrack.style.backgroundColor = trackColor;
            rootVisualElement.Add(_vTrack);

            _vTrack.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_graphView == null) return;
                float scale = _graphView.ViewScale;
                float viewHCanvas = _graphView.layout.height / scale;
                float scrollY = -_graphView.ViewPosition.y / scale;
                Rect bounds = _graphView.ComputeContentBounds();
                float effectiveContentH = Mathf.Max(bounds.yMax, scrollY + viewHCanvas);
                float maxScroll = Mathf.Max(0f, effectiveContentH - viewHCanvas);

                float thumbTop = _vThumb.resolvedStyle.top;
                float clickY = evt.localPosition.y;
                float pageAmount = viewHCanvas;
                float newScroll = clickY < thumbTop
                    ? Mathf.Max(0f, scrollY - pageAmount)
                    : Mathf.Min(maxScroll, scrollY + pageAmount);

                Vector3 pos = _graphView.ViewPosition;
                _graphView.SetViewPosition(new Vector3(pos.x, -newScroll * scale, pos.z));
                UpdateScrollbars();

                _vRepeatPointerY = clickY;
                _vRepeatPointerId = evt.pointerId;
                _vTrack.CapturePointer(evt.pointerId);
                _vRepeat?.Pause();
                _vRepeat = _vTrack.schedule.Execute(VScrollRepeatTick)
                    .Every(RepeatIntervalMs).StartingIn(RepeatInitialMs);
                evt.StopPropagation();
            });

            _vTrack.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_vRepeatPointerId == evt.pointerId)
                    _vRepeatPointerY = evt.localPosition.y;
            });
            _vTrack.RegisterCallback<PointerUpEvent>(evt => StopVScrollRepeat());
            _vTrack.RegisterCallback<PointerCaptureOutEvent>(evt => StopVScrollRepeat());

            _vThumb = new VisualElement();
            _vThumb.style.position = Position.Absolute;
            _vThumb.style.left = 2f; _vThumb.style.right = 2f;
            _vThumb.style.top = 0f;
            _vThumb.style.height = 0f;
            _vThumb.style.backgroundColor = thumbColor;
            _vThumb.style.borderTopLeftRadius = _vThumb.style.borderTopRightRadius =
            _vThumb.style.borderBottomLeftRadius = _vThumb.style.borderBottomRightRadius = 5f;
            _vTrack.Add(_vThumb);
        }

        private void BuildHorizontalScrollTrack(Color trackColor, Color thumbColor)
        {
            // -- Horizontal track (bottom edge, left of V-scrollbar) ----------------
            _hTrack = new VisualElement { pickingMode = PickingMode.Position };
            _hTrack.style.position = Position.Absolute;
            _hTrack.style.bottom = 0f;
            _hTrack.style.left = 0f;
            _hTrack.style.height = ScrollbarSize;
            _hTrack.style.backgroundColor = trackColor;
            rootVisualElement.Add(_hTrack);

            _hTrack.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_graphView == null) return;
                float scale = _graphView.ViewScale;
                float viewWCanvas = _graphView.layout.width / scale;
                float scrollX = -_graphView.ViewPosition.x / scale;
                Rect bounds = _graphView.ComputeContentBounds();
                float effectiveContentW = Mathf.Max(bounds.xMax, scrollX + viewWCanvas);
                float maxScroll = Mathf.Max(0f, effectiveContentW - viewWCanvas);

                float thumbLeft = _hThumb.resolvedStyle.left;
                float clickX = evt.localPosition.x;
                float pageAmount = viewWCanvas;
                float newScroll = clickX < thumbLeft
                    ? Mathf.Max(0f, scrollX - pageAmount)
                    : Mathf.Min(maxScroll, scrollX + pageAmount);

                Vector3 pos = _graphView.ViewPosition;
                _graphView.SetViewPosition(new Vector3(-newScroll * scale, pos.y, pos.z));
                UpdateScrollbars();

                _hRepeatPointerX = clickX;
                _hRepeatPointerId = evt.pointerId;
                _hTrack.CapturePointer(evt.pointerId);
                _hRepeat?.Pause();
                _hRepeat = _hTrack.schedule.Execute(HScrollRepeatTick)
                    .Every(RepeatIntervalMs).StartingIn(RepeatInitialMs);
                evt.StopPropagation();
            });

            _hTrack.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_hRepeatPointerId == evt.pointerId)
                    _hRepeatPointerX = evt.localPosition.x;
            });
            _hTrack.RegisterCallback<PointerUpEvent>(evt => StopHScrollRepeat());
            _hTrack.RegisterCallback<PointerCaptureOutEvent>(evt => StopHScrollRepeat());

            _hThumb = new VisualElement();
            _hThumb.style.position = Position.Absolute;
            _hThumb.style.top = 2f; _hThumb.style.bottom = 2f;
            _hThumb.style.left = 0f;
            _hThumb.style.width = 0f;
            _hThumb.style.backgroundColor = thumbColor;
            _hThumb.style.borderTopLeftRadius = _hThumb.style.borderTopRightRadius =
            _hThumb.style.borderBottomLeftRadius = _hThumb.style.borderBottomRightRadius = 5f;
            _hTrack.Add(_hThumb);
        }

        private void BuildScrollCorner(Color trackColor)
        {
            // -- Corner fill (bottom-right square between the two tracks) -----------
            var corner = new VisualElement { pickingMode = PickingMode.Ignore };
            corner.style.position = Position.Absolute;
            corner.style.right = 0f; corner.style.bottom = 0f;
            corner.style.width = ScrollbarSize; corner.style.height = ScrollbarSize;
            corner.style.backgroundColor = trackColor;
            rootVisualElement.Add(corner);
        }

        private void BuildVerticalThumbDrag()
        {
            // -- Thumb drag - vertical ---------------------------------------------
            _vThumb.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_graphView == null) return;
                _vDragging = true;
                _dragStartMouse = evt.position.y;
                float scale = _graphView.ViewScale;
                float scrollNow = -_graphView.ViewPosition.y / scale;
                _dragStartScroll = scrollNow;
                float viewCanvas = _graphView.layout.height / scale;
                Rect bounds = _graphView.ComputeContentBounds();
                float effectiveContentH = Mathf.Max(bounds.yMax, scrollNow + viewCanvas);
                _dragMaxScroll = Mathf.Max(0f, effectiveContentH - viewCanvas);
                _dragScrollRange = Mathf.Max(0f, _vTrack.layout.height - _vThumb.layout.height);
                _vThumb.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            _vThumb.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_vDragging || _graphView == null || _dragScrollRange <= 0f) return;
                float scale = _graphView.ViewScale;
                float delta = evt.position.y - _dragStartMouse;
                float newScroll = Mathf.Clamp(
                   _dragStartScroll + delta / _dragScrollRange * _dragMaxScroll,
                   0f, _dragMaxScroll);
                Vector3 pos = _graphView.ViewPosition;
                _graphView.SetViewPosition(new Vector3(pos.x, -newScroll * scale, pos.z));
                _vThumb.style.top = _dragMaxScroll > 0f
                   ? newScroll / _dragMaxScroll * _dragScrollRange : 0f;
                evt.StopPropagation();
            });
            _vThumb.RegisterCallback<PointerUpEvent>(evt =>
            {
                _vDragging = false;
                _vThumb.ReleasePointer(evt.pointerId);
                UpdateScrollbars();
                evt.StopPropagation();
            });
        }

        private void BuildHorizontalThumbDrag()
        {
            // -- Thumb drag - horizontal -------------------------------------------
            _hThumb.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_graphView == null) return;
                _hDragging = true;
                _dragStartMouse = evt.position.x;
                float scale = _graphView.ViewScale;
                float scrollNow = -_graphView.ViewPosition.x / scale;
                _dragStartScroll = scrollNow;
                float viewCanvas = _graphView.layout.width / scale;
                Rect bounds = _graphView.ComputeContentBounds();
                float effectiveContentW = Mathf.Max(bounds.xMax, scrollNow + viewCanvas);
                _dragMaxScroll = Mathf.Max(0f, effectiveContentW - viewCanvas);
                _dragScrollRange = Mathf.Max(0f, _hTrack.layout.width - _hThumb.layout.width);
                _hThumb.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });
            _hThumb.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_hDragging || _graphView == null || _dragScrollRange <= 0f) return;
                float scale = _graphView.ViewScale;
                float delta = evt.position.x - _dragStartMouse;
                float newScroll = Mathf.Clamp(
                   _dragStartScroll + delta / _dragScrollRange * _dragMaxScroll,
                   0f, _dragMaxScroll);
                Vector3 pos = _graphView.ViewPosition;
                _graphView.SetViewPosition(new Vector3(-newScroll * scale, pos.y, pos.z));
                _hThumb.style.left = _dragMaxScroll > 0f
                   ? newScroll / _dragMaxScroll * _dragScrollRange : 0f;
                evt.StopPropagation();
            });
            _hThumb.RegisterCallback<PointerUpEvent>(evt =>
            {
                _hDragging = false;
                _hThumb.ReleasePointer(evt.pointerId);
                UpdateScrollbars();
                evt.StopPropagation();
            });
        }

        // -- Trough hold-to-repeat ---------------------------------------------
        // While the trough is held, the press pages once, then after a short delay
        // the repeat pages at a steady interval. Each tick reads the live pointer
        // position so the direction stays correct if the thumb shifts, and pages only
        // while the pointer is outside the thumb in that direction and scroll has not
        // bottomed out. When the moving thumb catches the pointer the tick no-ops, so
        // the scrolling halts but resumes if the pointer moves further into the trough.
        private void VScrollRepeatTick()
        {
            if (_graphView == null) { StopVScrollRepeat(); return; }
            float scale = _graphView.ViewScale;
            float viewHCanvas = _graphView.layout.height / scale;
            float scrollY = -_graphView.ViewPosition.y / scale;
            Rect bounds = _graphView.ComputeContentBounds();
            float effectiveContentH = Mathf.Max(bounds.yMax, scrollY + viewHCanvas);
            float maxScroll = Mathf.Max(0f, effectiveContentH - viewHCanvas);
            if (maxScroll <= 0f) return;

            // Thumb geometry from the live scroll, computed the same way as
            // UpdateScrollbars to avoid reading the resolvedStyle layout-pass lag.
            float trackH = _vTrack.layout.height;
            float vRatio = Mathf.Clamp01(viewHCanvas / effectiveContentH);
            float thumbH = vRatio * trackH;
            float thumbTop = Mathf.Clamp01(scrollY / maxScroll) * (trackH - thumbH);

            float pageAmount = viewHCanvas;
            float newScroll;
            if (_vRepeatPointerY < thumbTop)
            {
                if (scrollY <= 0f) return;
                newScroll = Mathf.Max(0f, scrollY - pageAmount);
            }
            else if (_vRepeatPointerY > thumbTop + thumbH)
            {
                if (scrollY >= maxScroll) return;
                newScroll = Mathf.Min(maxScroll, scrollY + pageAmount);
            }
            else { return; }

            Vector3 pos = _graphView.ViewPosition;
            _graphView.SetViewPosition(new Vector3(pos.x, -newScroll * scale, pos.z));
            UpdateScrollbars();
        }

        private void HScrollRepeatTick()
        {
            if (_graphView == null) { StopHScrollRepeat(); return; }
            float scale = _graphView.ViewScale;
            float viewWCanvas = _graphView.layout.width / scale;
            float scrollX = -_graphView.ViewPosition.x / scale;
            Rect bounds = _graphView.ComputeContentBounds();
            float effectiveContentW = Mathf.Max(bounds.xMax, scrollX + viewWCanvas);
            float maxScroll = Mathf.Max(0f, effectiveContentW - viewWCanvas);
            if (maxScroll <= 0f) return;

            float trackW = _hTrack.layout.width;
            float hRatio = Mathf.Clamp01(viewWCanvas / effectiveContentW);
            float thumbW = hRatio * trackW;
            float thumbLeft = Mathf.Clamp01(scrollX / maxScroll) * (trackW - thumbW);

            float pageAmount = viewWCanvas;
            float newScroll;
            if (_hRepeatPointerX < thumbLeft)
            {
                if (scrollX <= 0f) return;
                newScroll = Mathf.Max(0f, scrollX - pageAmount);
            }
            else if (_hRepeatPointerX > thumbLeft + thumbW)
            {
                if (scrollX >= maxScroll) return;
                newScroll = Mathf.Min(maxScroll, scrollX + pageAmount);
            }
            else { return; }

            Vector3 pos = _graphView.ViewPosition;
            _graphView.SetViewPosition(new Vector3(-newScroll * scale, pos.y, pos.z));
            UpdateScrollbars();
        }

        private void StopVScrollRepeat()
        {
            _vRepeat?.Pause();
            _vRepeat = null;
            if (_vRepeatPointerId >= 0 && _vTrack != null &&
                _vTrack.HasPointerCapture(_vRepeatPointerId))
                _vTrack.ReleasePointer(_vRepeatPointerId);
            _vRepeatPointerId = -1;
        }

        private void StopHScrollRepeat()
        {
            _hRepeat?.Pause();
            _hRepeat = null;
            if (_hRepeatPointerId >= 0 && _hTrack != null &&
                _hTrack.HasPointerCapture(_hRepeatPointerId))
                _hTrack.ReleasePointer(_hRepeatPointerId);
            _hRepeatPointerId = -1;
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            if (_hTrack == null || _vTrack == null) return;
            _hTrack.style.width = evt.newRect.width - ScrollbarSize;
            _vTrack.style.height = evt.newRect.height - MsgBarH - ScrollbarSize;
            UpdateScrollbars();
        }

        internal void UpdateScrollbars()
        {
            if (_graphView == null || _hTrack == null || _vTrack == null) return;
            if (_hDragging || _vDragging) return;

            float rootW = rootVisualElement.layout.width;
            float rootH = rootVisualElement.layout.height;
            if (rootW < 1f) return;

            float trackW = rootW - ScrollbarSize;
            float trackH = rootH - MsgBarH - ScrollbarSize;

            float scale = _graphView.ViewScale;
            Rect bounds = _graphView.ComputeContentBounds();
            Vector3 viewPos = _graphView.ViewPosition;

            float viewW = _graphView.layout.width;
            float viewWCanvas = viewW / scale;
            float contentW = bounds.xMax;
            float scrollX = -viewPos.x / scale;

            float effectiveContentW = Mathf.Max(contentW, scrollX + viewWCanvas);
            float hRatio = Mathf.Clamp01(viewWCanvas / effectiveContentW);
            bool hVisible = hRatio < 1f;
            _hThumb.style.display = hVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (hVisible && trackW > 1f)
            {
                float thumbW = hRatio * trackW;
                float maxScroll = Mathf.Max(0f, effectiveContentW - viewWCanvas);
                float thumbLeft = maxScroll > 0f
                   ? Mathf.Clamp01(scrollX / maxScroll) * (trackW - thumbW)
                   : 0f;
                _hThumb.style.width = thumbW;
                _hThumb.style.left = thumbLeft;
            }

            float viewH = _graphView.layout.height;
            float viewHCanvas = viewH / scale;
            float contentH = bounds.yMax;
            float scrollY = -viewPos.y / scale;

            float effectiveContentH = Mathf.Max(contentH, scrollY + viewHCanvas);
            float vRatio = Mathf.Clamp01(viewHCanvas / effectiveContentH);
            bool vVisible = vRatio < 1f;
            _vThumb.style.display = vVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (vVisible && trackH > 1f)
            {
                float thumbH = vRatio * trackH;
                float maxScroll = Mathf.Max(0f, effectiveContentH - viewHCanvas);
                float thumbTop = maxScroll > 0f
                   ? Mathf.Clamp01(scrollY / maxScroll) * (trackH - thumbH)
                   : 0f;
                _vThumb.style.height = thumbH;
                _vThumb.style.top = thumbTop;
            }
        }

        private void TakeSnapshotsAll()
        {
            if (_target == null || _target.Sequences == null) return;

            bool anyWritten = false;
            bool anyAttempted = false;
            bool anyNamed = false;
            foreach (Sequence seq in _target.Sequences)
            {
                bool named = !string.IsNullOrWhiteSpace(seq.Name);
                if (named) anyNamed = true;
                bool eligible = named &&
                                seq.Property != null &&
                                seq.Property.EffectCount > 0;
                if (eligible) anyAttempted = true;

                if (SequenceBackupManager.ForceSnapshot(seq))
                    anyWritten = true;
            }

            if (LayoutBackupManager.ForceSnapshot(_target))
                anyWritten = true;

            if (anyWritten)
            {
                SetMessage("Snapshot saved.", MessageSeverity.Info, 30f);
                RefreshRestoreButtons();
            }
            else if (anyAttempted)
                SetMessage("Nothing to save - data unchanged.", MessageSeverity.Info, 3f);
            else if (!anyNamed)
                SetMessage("Nothing to save - only named sequences are snapshotted.", MessageSeverity.Info, 3f);
            else
                SetMessage("Nothing to save - check Console for details.", MessageSeverity.Warning);
        }
    }
}