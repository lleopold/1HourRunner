using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static JuiceBox.Processor;

// ==============================================================================
//  DelegatePicker: Floating overlay for binding a delegate slot to a target method or field.
// ==============================================================================
namespace JuiceBox
{
    internal sealed class DelegatePicker : VisualElement
    {

#if UNITY_6000_8_OR_NEWER
        [Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
#endif
        private static Type[] s_resolvedStaticTypes;

        private static Type[] GetAllowedStaticTypes()
        {
            if (s_resolvedStaticTypes != null) return s_resolvedStaticTypes;
            var names = JuiceBoxSettings.AllHookScanClasses();
            var types = new List<Type>();
            var assemblies = JuiceBoxSettings.GetLoadedAssemblies();
            foreach (string name in names)
            {
                foreach (var asm in assemblies)
                {
                    var t = asm.GetType(name);
                    if (t != null) { types.Add(t); break; }
                }
            }
            s_resolvedStaticTypes = types.ToArray();
            return s_resolvedStaticTypes;
        }

        internal static void InvalidateStaticTypeCache()
        {
            s_resolvedStaticTypes = null;
            s_scanCache.Clear();
        }

#if UNITY_6000_8_OR_NEWER
        [Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
#endif
        private static readonly Dictionary<string, Dictionary<Type, MethodInfo[]>> s_scanCache =
           new Dictionary<string, Dictionary<Type, MethodInfo[]>>();

        private const int SEC_NONE = 0;
        private const int SEC_INSTANCE = 1;
        private const int SEC_STATIC = 2;

        private UnityEngine.Object _targetObj;
        private string _relativeDescriptor = "./";
        private string _relationshipLabel = "Self";
        private bool _unrelated;

        private int _pickedSection = SEC_NONE;

        private Type _instType;
        private string _instMethod = "";

        private Type _statType;
        private string _statMethod = "";

        private bool _instIsField;
        private bool _statIsField;

        private bool _evalOnce;
        private bool _hasExistingBinding;

        private readonly Processor.IDelegateConnecter _effect;
        private readonly string _slotName;
        private readonly JbTheme _theme;
        private readonly Action _onCommit;
        private readonly Action _onDelete;
        private readonly GameObject _selfObject;

        private readonly Type _returnType;
        private readonly Type[] _paramTypes;
        private readonly bool _strictParams;

        private VisualElement _contentZone;
        private GraphView _graphView;
        private Vector2 _contentAnchor;
        private bool _dragging;
        private Vector2 _dragMouseStart;

        public static void Show(
           VisualElement graphRoot,
           IDelegateConnecter effect,
           string slotName,
           Type valueType,
           Vector2 position,
           JbTheme theme,
           Action onCommit,
           Action onDelete = null,
           GameObject selfObject = null,
           ValueSlotRef valueSlot = default,
           GameObject prePickedTarget = null)
        {
            graphRoot.Q<DelegatePicker>()?.RemoveFromHierarchy();
            var picker = new DelegatePicker(effect, slotName, valueType, theme,
               onCommit, onDelete, selfObject, valueSlot, prePickedTarget);

            float w = 340f;
            picker.style.position = Position.Absolute;
            picker.style.width = w;

            var gv = graphRoot as GraphView;
            if (gv != null)
            {
                picker._graphView = gv;
                var cvc = gv.contentViewContainer;
                picker._contentAnchor = cvc.WorldToLocal(gv.LocalToWorld(position));
                picker.UpdatePosition();
                picker.RegisterCallback<GeometryChangedEvent>(_ => picker.UpdatePosition());
                gv.viewTransformChanged += picker.OnViewTransformChanged;
                picker.RegisterCallback<DetachFromPanelEvent>(picker.OnDetach);
            }
            else
            {
                float h = graphRoot.resolvedStyle.height;
                float x = graphRoot.resolvedStyle.width > w
                   ? Mathf.Clamp(position.x, 0f, graphRoot.resolvedStyle.width - w) : 0f;
                float y = h > 0f
                   ? Mathf.Clamp(position.y, 0f, h - Mathf.Min(picker.resolvedStyle.height, h)) : 0f;
                picker.style.left = x;
                picker.style.top = y;
            }

            graphRoot.Add(picker);
            picker.BringToFront();
        }

        private static readonly Color DragHighlight = new Color(0.5f, 0.5f, 0.5f, 0.4f);

        internal static void RegisterDragToPick(
            VisualElement badge, JbTheme theme,
            Action<GameObject, Vector2> onDrop)
        {
            badge.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                GameObject go = NormalizeDraggedObject();
                if (go == null) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                badge.style.borderTopColor = badge.style.borderBottomColor =
                badge.style.borderLeftColor = badge.style.borderRightColor = DragHighlight;
                badge.style.borderTopWidth = badge.style.borderBottomWidth =
                badge.style.borderLeftWidth = badge.style.borderRightWidth = 1f;
                evt.StopPropagation();
            });

            badge.RegisterCallback<DragPerformEvent>(evt =>
            {
                GameObject go = NormalizeDraggedObject();
                if (go == null) return;
                DragAndDrop.AcceptDrag();
                ClearDragHighlight(badge);
                evt.StopPropagation();
                onDrop(go, evt.mousePosition);
            });

            badge.RegisterCallback<DragLeaveEvent>(_ => ClearDragHighlight(badge));
            badge.RegisterCallback<DragExitedEvent>(_ => ClearDragHighlight(badge));
        }

        private static GameObject NormalizeDraggedObject()
        {
            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length == 0) return null;
            var obj = refs[0];
            if (obj is GameObject go) return go;
            if (obj is Component c) return c.gameObject;
            return null;
        }

        private static void ClearDragHighlight(VisualElement badge)
        {
            badge.style.borderTopColor = badge.style.borderBottomColor =
            badge.style.borderLeftColor = badge.style.borderRightColor = Color.clear;
            badge.style.borderTopWidth = badge.style.borderBottomWidth =
            badge.style.borderLeftWidth = badge.style.borderRightWidth = 0f;
        }

        private void UpdatePosition()
        {
            if (_graphView == null) return;
            var cvc = _graphView.contentViewContainer;
            Vector2 screenPos = _graphView.WorldToLocal(cvc.LocalToWorld(_contentAnchor));
            float w = 340f;
            float ph = resolvedStyle.height;
            if (float.IsNaN(ph) || ph <= 0f) ph = 380f;
            float gw = _graphView.resolvedStyle.width;
            float gh = _graphView.resolvedStyle.height;
            float x = gw > w ? Mathf.Clamp(screenPos.x, 0f, gw - w) : 0f;
            float y = gh > ph ? Mathf.Clamp(screenPos.y, 0f, gh - ph) : 0f;
            style.left = x;
            style.top = y;
        }

        private void OnViewTransformChanged(GraphView _) => UpdatePosition();

        private void OnDetach(DetachFromPanelEvent evt)
        {
            if (_graphView != null)
                _graphView.viewTransformChanged -= OnViewTransformChanged;
        }

        private DelegatePicker(IDelegateConnecter effect, string slotName, Type valueType,
           JbTheme theme, Action onCommit, Action onDelete, GameObject selfObject,
           ValueSlotRef valueSlot = default, GameObject prePickedTarget = null)
        {
            _effect = effect;
            _slotName = slotName;
            _theme = theme;
            _onCommit = onCommit;
            _onDelete = onDelete;
            _selfObject = selfObject;

            (_returnType, _paramTypes) = SlotSignature(slotName, valueType, valueSlot);
            _strictParams = !valueSlot.IsNone;

            SeedFromEffect();

            var (evalUnlocked, evalDefault) = GetEvalOnceCategory();
            if (!evalUnlocked)
                _evalOnce = evalDefault;
            else
                _evalOnce = _hasExistingBinding
                    ? _effect.ReadEvalOnce(_slotName)
                    : evalDefault;

            if (prePickedTarget != null)
            {
                _targetObj = prePickedTarget;
                _instType = null; _instMethod = "";
                _statType = null; _statMethod = "";
                _instIsField = false; _statIsField = false;
                _pickedSection = SEC_NONE;

                Transform target = prePickedTarget.transform;
                if (target != null && _selfObject != null)
                {
                    var (desc, label) = ComputeRelationship(target, _selfObject.transform);
                    _relativeDescriptor = desc;
                    _relationshipLabel = label;
                    _unrelated = label == "Unrelated";
                }
                else
                {
                    _relativeDescriptor = "";
                    _relationshipLabel = "Unrelated";
                    _unrelated = true;
                }
            }

            BuildUI();
        }

        private void SeedFromEffect()
        {
            var (mode, obj, cls, meth, relDesc) = _effect.ReadSlot(_slotName);
            var dm = (DelegateMode)mode;
            if (dm == DelegateMode.None) return;

            _hasExistingBinding = true;
            switch (dm)
            {
                case DelegateMode.Static:
                case DelegateMode.FlatStatic:
                    _pickedSection = SEC_STATIC;
                    _statType = ResolveType(cls);
                    _statMethod = meth ?? "";
                    break;

                case DelegateMode.Instance:
                    _targetObj = obj is Component instComp ? instComp.gameObject : obj;
                    _unrelated = true;
                    _relationshipLabel = "Unrelated";
                    _relativeDescriptor = "";
                    _pickedSection = SEC_INSTANCE;
                    if (obj != null)
                    {
                        _instType = TypeOnObject(obj, meth);
                        if (_selfObject != null)
                            DetectRelationship();
                    }
                    break;

                case DelegateMode.BoundStatic:
                    _targetObj = obj;
                    _unrelated = true;
                    _relationshipLabel = "Unrelated";
                    _relativeDescriptor = "";
                    _pickedSection = SEC_STATIC;
                    _statType = ResolveType(cls);
                    _statMethod = meth ?? "";
                    if (obj != null && _selfObject != null)
                        DetectRelationship();
                    break;

                case DelegateMode.RelativeInstance:
                    _relativeDescriptor = relDesc ?? "./";
                    _pickedSection = SEC_INSTANCE;
                    _instMethod = meth ?? "";
                    if (_selfObject != null)
                    {
                        var resolved = Processor.ResolveDescriptor(_selfObject, relDesc);
                        if (resolved != null)
                        {
                            _targetObj = resolved;
                            DetectRelationship();
                            _instType = TypeOnObject(resolved, meth);
                        }
                        else
                        {
                            _relationshipLabel = "Error";
                        }
                    }
                    break;

                case DelegateMode.RelativeStatic:
                    _relativeDescriptor = relDesc ?? "./";
                    _pickedSection = SEC_STATIC;
                    _statType = ResolveType(cls);
                    _statMethod = meth ?? "";
                    if (_selfObject != null)
                    {
                        var resolved = Processor.ResolveDescriptor(_selfObject, relDesc);
                        if (resolved != null)
                        {
                            _targetObj = resolved;
                            DetectRelationship();
                        }
                        else
                        {
                            _relationshipLabel = "Error";
                        }
                    }
                    break;

                case DelegateMode.FieldAccess:
                    if (!string.IsNullOrEmpty(relDesc))
                    {
                        _relativeDescriptor = relDesc;
                        _pickedSection = SEC_INSTANCE;
                        _instMethod = meth ?? "";
                        _instIsField = true;
                        _instType = ResolveType(cls);
                        if (_selfObject != null)
                        {
                            var resolvedFA = Processor.ResolveDescriptor(_selfObject, relDesc);
                            if (resolvedFA != null)
                            {
                                _targetObj = resolvedFA;
                                DetectRelationship();
                                if (_instType == null)
                                    _instType = TypeOnObjectForField(resolvedFA, meth);
                            }
                            else
                            {
                                _relationshipLabel = "Error";
                            }
                        }
                    }
                    else if (obj != null)
                    {
                        _targetObj = obj is Component fldComp ? fldComp.gameObject : obj;
                        _unrelated = true;
                        _relationshipLabel = "Unrelated";
                        _relativeDescriptor = "";
                        _pickedSection = SEC_INSTANCE;
                        _instMethod = meth ?? "";
                        _instIsField = true;
                        _instType = ResolveType(cls);
                        if (_instType == null)
                            _instType = TypeOnObjectForField(obj, meth);
                        if (_selfObject != null)
                            DetectRelationship();
                    }
                    else
                    {
                        _pickedSection = SEC_STATIC;
                        _statType = ResolveType(cls);
                        _statMethod = meth ?? "";
                        _statIsField = true;
                    }
                    break;
            }
        }

        private void DetectRelationship()
        {
            if (_targetObj == null || _selfObject == null) return;
            Transform target = _targetObj is GameObject go ? go.transform
                             : _targetObj is Component c ? c.transform
                             : null;
            if (target == null) return;
            var (desc, label) = ComputeRelationship(target, _selfObject.transform);
            _relativeDescriptor = desc;
            _relationshipLabel = label;
        }

        private (bool enabled, bool defaultIsOnce) GetEvalOnceCategory()
        {
            switch (_slotName)
            {
                case "OnUpdate":
                case "EvaluateCondition":
                    return (false, false);

                case "OnStart":
                case "OnDone":
                case "OnComplete":
                case "SetStartingValue":
                case "SetStartingVelocity":
                    return (false, true);

                case "Duration":
                case "EndConditionTime":
                case "EndConditionRange":
                case "EndConditionVelocity":
                case "Speed":
                case "SmoothingFrequency":
                case "SmoothingDamping":
                case "AirResistance":
                    return (true, true);

                case "ModifyEffectState":
                case "WindSpeed":
                case "TimescaleInput":
                    return (true, false);

                case "GetTargetValue":
                    if (_effect is Processor.IEffect ie
                        && ie.Kind == Processor.EffectKind.Tween)
                        return (true, true);
                    return (true, false);

                default:
                    return (true, false);
            }
        }

        private bool GetEvalOnceDefault()
        {
            var (_, defaultIsOnce) = GetEvalOnceCategory();
            return defaultIsOnce;
        }

        private void UpdateEvalButton(Button btn, string label = null)
        {
            if (label == null) label = _evalOnce ? "Refresh Once" : "Refresh Every Frame";
            btn.text = label;
            btn.style.color = _evalOnce
                ? new Color(0.6f, 0.9f, 0.6f, 1f)
                : new Color(0.75f, 0.78f, 0.9f, 1f);
            btn.style.backgroundColor = _evalOnce
                ? new Color(0.18f, 0.25f, 0.18f, 1f)
                : new Color(0.22f, 0.22f, 0.28f, 1f);
        }

        private void BuildUI()
        {
            style.backgroundColor = _theme.NodeBg;
            style.borderTopLeftRadius = style.borderTopRightRadius =
            style.borderBottomLeftRadius = style.borderBottomRightRadius = 4;
            FilmStripElement.SetBorder(this, _theme.NodeBorder, 1f);
            style.paddingLeft = style.paddingRight =
            style.paddingTop = style.paddingBottom = 5f;

            var title = new VisualElement();
            title.style.flexDirection = FlexDirection.Row;
            title.style.alignItems = Align.Center;
            title.style.marginBottom = 2f;

            var titleLbl = new Label($"Bind: {_slotName}") { pickingMode = PickingMode.Ignore };
            titleLbl.style.fontSize = 13.5f;
            titleLbl.style.color = _theme.CapName;
            titleLbl.style.flexGrow = 1f;
            title.Add(titleLbl);

            var closeX = new Button(CloseCommit) { text = "\u00d7" };
            closeX.style.width = 20f; closeX.style.height = 20f;
            closeX.style.fontSize = 15f;
            closeX.style.paddingLeft = closeX.style.paddingRight = 0f;
            closeX.style.paddingTop = closeX.style.paddingBottom = 0f;
            closeX.style.backgroundColor = Color.clear;
            closeX.style.color = _theme.FieldLbl;
            FlatBtn(closeX);
            title.Add(closeX);
            title.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                _dragging = true;
                _dragMouseStart = evt.mousePosition;
                title.CaptureMouse();
                evt.StopPropagation();
            });
            title.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (!_dragging) return;
                Vector2 delta = evt.mousePosition - _dragMouseStart;
                _dragMouseStart = evt.mousePosition;
                float nx = resolvedStyle.left + delta.x;
                float ny = resolvedStyle.top + delta.y;
                style.left = nx;
                style.top = ny;
                if (_graphView != null)
                {
                    var cvc = _graphView.contentViewContainer;
                    _contentAnchor = cvc.WorldToLocal(
                       _graphView.LocalToWorld(new Vector2(nx, ny)));
                }
                evt.StopPropagation();
            });
            title.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button != 0 || !_dragging) return;
                _dragging = false;
                title.ReleaseMouse();
                evt.StopPropagation();
            });
            Add(title);

            Info(this, FormatSignature(_returnType, _paramTypes));
            if (_paramTypes.Length > 0 && !_strictParams)
                Info(this, "or: " + FormatSignature(_returnType, Type.EmptyTypes));

            _contentZone = new VisualElement();
            _contentZone.style.marginBottom = 5f;
            Add(_contentZone);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.height = 30f;

            var applyBtn = new Button(Apply) { text = "Apply" };
            applyBtn.style.flexGrow = 1f; applyBtn.style.height = 30f;
            applyBtn.style.fontSize = 13.5f;
            applyBtn.style.backgroundColor = _theme.SlotBgHi;
            applyBtn.style.color = _theme.SlotHi;
            FlatBtn(applyBtn);
            btnRow.Add(applyBtn);

            if (_onDelete != null)
            {
                var deleteBtn = new Button(OnDelete) { text = "Delete Hook" };
                deleteBtn.style.flexGrow = 1f; deleteBtn.style.height = 30f;
                deleteBtn.style.fontSize = 13.5f;
                deleteBtn.style.backgroundColor = _theme.FieldBgMiss;
                deleteBtn.style.color = _theme.FieldMissVal;
                FlatBtn(deleteBtn);
                btnRow.Add(deleteBtn);
            }

            var cancelBtn = new Button(OnClose) { text = "Cancel" };
            cancelBtn.style.flexGrow = 1f; cancelBtn.style.height = 30f;
            cancelBtn.style.fontSize = 13.5f;
            cancelBtn.style.backgroundColor = _theme.FieldBg;
            cancelBtn.style.color = _theme.FieldLbl;
            FlatBtn(cancelBtn);
            btnRow.Add(cancelBtn);
            Add(btnRow);

            RebuildContent();
        }

        private void OnClose() => RemoveFromHierarchy();

        private void CloseCommit()
        {
            if (_hasExistingBinding && !HasCompleteSelection())
            {
                _effect.WriteEvalOnce(_slotName, _evalOnce);
                _effect.Reconstruct();
                _onCommit?.Invoke();
                RemoveFromHierarchy();
                return;
            }
            Apply();
        }

        private bool HasCompleteSelection()
            => (_pickedSection == SEC_INSTANCE && !string.IsNullOrEmpty(_instMethod))
            || (_pickedSection == SEC_STATIC && _statType != null && !string.IsNullOrEmpty(_statMethod));

        private void OnDelete()
        {
            _onDelete?.Invoke();
            RemoveFromHierarchy();
        }

        private void RebuildContent()
        {
            _contentZone.Clear();

            BuildRelationshipRow();
            BuildTargetRow();
            BuildBrokenDescriptorWarning();

            GameObject scanTarget = ResolveTargetForScan();

            BuildInstanceSection(scanTarget);

            BuildStaticSection();
        }

        private void BuildRelationshipRow()
        {
            // -- Relationship label + EvalOnce toggle row --
            var relRow = new VisualElement();
            relRow.style.flexDirection = FlexDirection.Row;
            relRow.style.alignItems = Align.Center;
            relRow.style.marginBottom = 2f;

            var relLbl = new Label(_relationshipLabel) { pickingMode = PickingMode.Ignore };
            relLbl.style.fontSize = 13f;
            relLbl.style.color = _relationshipLabel == "Error" ? _theme.FieldMissVal : _theme.CapName;
            relLbl.style.flexGrow = 1f;
            relRow.Add(relLbl);

            var (evalEnabled, _) = GetEvalOnceCategory();
            Button evalBtn = null;
            evalBtn = new Button(() =>
            {
                if (!evalEnabled) return;
                _evalOnce = !_evalOnce;
                UpdateEvalButton(evalBtn);
            });
            UpdateEvalButton(evalBtn);
            evalBtn.SetEnabled(evalEnabled);
            evalBtn.style.flexGrow = 0f;
            evalBtn.style.flexShrink = 0f;
            evalBtn.style.minWidth = 110f;
            evalBtn.style.paddingLeft = 4f;
            evalBtn.style.paddingRight = 4f;
            evalBtn.style.paddingTop = 0f;
            evalBtn.style.paddingBottom = 0f;
            evalBtn.style.height = 18f;
            evalBtn.style.fontSize = 10f;
            evalBtn.style.borderTopLeftRadius = evalBtn.style.borderTopRightRadius =
            evalBtn.style.borderBottomLeftRadius = evalBtn.style.borderBottomRightRadius = 3f;
            evalBtn.style.borderTopWidth = evalBtn.style.borderBottomWidth =
            evalBtn.style.borderLeftWidth = evalBtn.style.borderRightWidth = 0f;
            evalBtn.tooltip = evalEnabled
                ? "Toggle between invoking this delegate every frame or once when the effect starts"
                : _evalOnce ? "This delegate is always invoked once at start"
                            : "This delegate is always invoked every frame";
            relRow.Add(evalBtn);
            _contentZone.Add(relRow);
        }

        private void BuildTargetRow()
        {
            // -- Target + Unrelated row --
            var tgtRow = new VisualElement();
            tgtRow.style.flexDirection = FlexDirection.Row;
            tgtRow.style.marginBottom = 4f;
            tgtRow.style.alignItems = Align.Center;

            var objField = new ObjectField { objectType = typeof(UnityEngine.Object) };
            objField.tooltip = "The GameObject or component to bind to. Leave as Self to use the animation's own GameObject";
            objField.value = _targetObj;
            objField.style.flexGrow = 1f;
            objField.style.flexShrink = 1f;
            Field(objField);
            if (_targetObj == null)
            {
                objField.schedule.Execute(() =>
                {
                    var input = objField.Q<VisualElement>(className: "unity-object-field__input");
                    var lbl = input?.Q<Label>();
                    if (lbl != null) lbl.text = "Self";
                });
            }
            tgtRow.Add(objField);

            bool canToggleUnrelated = _targetObj != null
                && _relationshipLabel != "Unrelated";
            var unrelToggle = new Toggle("Unrelated");
            unrelToggle.tooltip = "Bind without requiring a relative path to the target";
            unrelToggle.value = _unrelated;
            unrelToggle.SetEnabled(canToggleUnrelated);
            unrelToggle.style.flexGrow = 0f;
            unrelToggle.style.flexShrink = 1f;
            unrelToggle.style.marginLeft = 4f;
            unrelToggle.style.marginRight = 0f;
            unrelToggle.style.paddingLeft = 0f;
            unrelToggle.style.paddingRight = 0f;
            unrelToggle.style.fontSize = 10f;
            unrelToggle.schedule.Execute(() =>
            {
                var input = unrelToggle.Q(className: "unity-toggle__input");
                if (input != null)
                {
                    input.style.flexGrow = 0f;
                    input.style.marginLeft = 0f;
                    input.style.marginRight = 2f;
                    input.style.paddingLeft = 0f;
                    input.style.paddingRight = 0f;
                    input.style.minWidth = StyleKeyword.Auto;
                }
                var baseLabel = unrelToggle.Q(className: "unity-base-field__label");
                if (baseLabel != null)
                {
                    baseLabel.style.minWidth = StyleKeyword.Auto;
                    baseLabel.style.marginLeft = 0f;
                    baseLabel.style.marginRight = 0f;
                    baseLabel.style.paddingLeft = 0f;
                    baseLabel.style.paddingRight = 0f;
                }
                var textLabel = unrelToggle.Q(className: "unity-toggle__text");
                if (textLabel != null)
                {
                    textLabel.style.marginLeft = 0f;
                    textLabel.style.paddingLeft = 0f;
                }
            });
            unrelToggle.RegisterValueChangedCallback(evt =>
            {
                _unrelated = evt.newValue;
            });
            tgtRow.Add(unrelToggle);
            _contentZone.Add(tgtRow);

            objField.RegisterValueChangedCallback(evt =>
            {
                _targetObj = evt.newValue is Component c ? c.gameObject : evt.newValue;
                _instType = null; _instMethod = "";
                _statType = null; _statMethod = "";
                _pickedSection = SEC_NONE;

                if (_targetObj == null)
                {
                    _relativeDescriptor = "./";
                    _relationshipLabel = "Self";
                    _unrelated = false;
                }
                else
                {
                    Transform target = _targetObj is GameObject go ? go.transform
                                     : _targetObj is Component cc ? cc.transform
                                     : null;
                    if (target != null && _selfObject != null)
                    {
                        var (desc, label) = ComputeRelationship(target, _selfObject.transform);
                        _relativeDescriptor = desc;
                        _relationshipLabel = label;
                        _unrelated = label == "Unrelated";
                    }
                    else
                    {
                        _relativeDescriptor = "";
                        _relationshipLabel = "Unrelated";
                        _unrelated = true;
                    }
                }
                RebuildContent();
            });
        }

        private void BuildBrokenDescriptorWarning()
        {
            // -- Broken descriptor warning --
            if (_targetObj == null && _relativeDescriptor != "./"
                && !string.IsNullOrEmpty(_relativeDescriptor))
            {
                var warnLbl = new Label("Descriptor '" + _relativeDescriptor + "' does not resolve.")
                { pickingMode = PickingMode.Ignore };
                warnLbl.style.fontSize = 11f;
                warnLbl.style.color = _theme.FieldMissVal;
                warnLbl.style.whiteSpace = WhiteSpace.Normal;
                warnLbl.style.marginBottom = 3f;
                _contentZone.Add(warnLbl);
            }
        }

        private GameObject ResolveTargetForScan()
        {
            if (_targetObj is GameObject go) return go;
            if (_targetObj is Component c) return c.gameObject;
            return _selfObject;
        }

        private void BuildInstanceSection(GameObject scanTarget)
        {
            SectionHeader("Instance");

            if (scanTarget == null)
            {
                Info("Select a target to see instance members.");
                return;
            }

            Type fieldType = GetFieldScanType(_returnType, _paramTypes);
            bool isSetter = IsSetterSignature(_returnType, _paramTypes);

            var typesWithMembers = GatherInstanceTypes(
                scanTarget, _returnType, _paramTypes, _strictParams, fieldType, isSetter);
            if (typesWithMembers.Count == 0)
            {
                Info("No matching instance members.");
                return;
            }

            var typeList = typesWithMembers.Select(x => x.type).ToList();
            var typeNames = typeList.Select(t => t.Name).Prepend("\u2014 Type \u2014").ToList();

            int curTypeIdx = _instType == null ? 0 : typeList.IndexOf(_instType) + 1;
            if (curTypeIdx < 0 || curTypeIdx >= typeNames.Count) curTypeIdx = 0;

            Lbl("Type");
            var typePopup = new PopupField<string>(typeNames, curTypeIdx);
            typePopup.tooltip = "Component type on the target to bind to";
            Field(typePopup);
            _contentZone.Add(typePopup);

            var memberZone = new VisualElement();
            _contentZone.Add(memberZone);

            if (_instType != null)
            {
                var entry = typesWithMembers.Find(x => x.type == _instType);
                BuildMemberDropdown(memberZone,
                    entry.methods ?? Array.Empty<MethodInfo>(),
                    entry.fields ?? Array.Empty<FieldInfo>(),
                    _instMethod, _instIsField, SEC_INSTANCE);
            }

            typePopup.RegisterValueChangedCallback(evt =>
            {
                int idx = typeNames.IndexOf(evt.newValue);
                _instType = idx > 0 ? typeList[idx - 1] : null;
                _instMethod = "";
                _instIsField = false;
                _pickedSection = _instType != null ? SEC_INSTANCE : SEC_NONE;
                if (_pickedSection == SEC_INSTANCE)
                { _statType = null; _statMethod = ""; _statIsField = false; }
                memberZone.Clear();
                if (_instType != null)
                {
                    var entry = typesWithMembers.Find(x => x.type == _instType);
                    BuildMemberDropdown(memberZone,
                        entry.methods ?? Array.Empty<MethodInfo>(),
                        entry.fields ?? Array.Empty<FieldInfo>(),
                        "", false, SEC_INSTANCE);
                }
            });
        }

        private void BuildStaticSection()
        {
            SectionHeader("Static");

            Type fieldType = GetFieldScanType(_returnType, _paramTypes);
            bool isSetter = IsSetterSignature(_returnType, _paramTypes);

            var methodCandidates = GetStaticCandidates(_returnType, _paramTypes, _strictParams);
            var fieldCandidates = GetStaticFieldCandidates(fieldType, isSetter);

            var allTypes = new HashSet<Type>();
            foreach (var kv in methodCandidates) allTypes.Add(kv.Key);
            foreach (var kv in fieldCandidates) allTypes.Add(kv.Key);

            if (allTypes.Count == 0)
            {
                Info("No matching static members.");
                return;
            }

            var typeList = new List<Type>(allTypes);
            typeList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            var typeNames = typeList.Select(t => t.Name).Prepend("\u2014 Type \u2014").ToList();

            int curTypeIdx = _statType == null ? 0 : typeList.IndexOf(_statType) + 1;
            if (curTypeIdx < 0 || curTypeIdx >= typeNames.Count) curTypeIdx = 0;

            Lbl("Type");
            var typePopup = new PopupField<string>(typeNames, curTypeIdx);
            typePopup.tooltip = "Class containing the static method to bind";
            Field(typePopup);
            _contentZone.Add(typePopup);

            var memberZone = new VisualElement();
            _contentZone.Add(memberZone);

            if (_statType != null)
            {
                methodCandidates.TryGetValue(_statType, out var initMethods);
                fieldCandidates.TryGetValue(_statType, out var initFields);
                BuildMemberDropdown(memberZone,
                    initMethods ?? Array.Empty<MethodInfo>(),
                    initFields ?? Array.Empty<FieldInfo>(),
                    _statMethod, _statIsField, SEC_STATIC);
            }

            typePopup.RegisterValueChangedCallback(evt =>
            {
                int idx = typeNames.IndexOf(evt.newValue);
                _statType = idx > 0 ? typeList[idx - 1] : null;
                _statMethod = "";
                _statIsField = false;
                _pickedSection = _statType != null ? SEC_STATIC : SEC_NONE;
                if (_pickedSection == SEC_STATIC)
                { _instType = null; _instMethod = ""; _instIsField = false; }
                memberZone.Clear();
                if (_statType != null)
                {
                    methodCandidates.TryGetValue(_statType, out var ms);
                    fieldCandidates.TryGetValue(_statType, out var fs);
                    BuildMemberDropdown(memberZone,
                        ms ?? Array.Empty<MethodInfo>(),
                        fs ?? Array.Empty<FieldInfo>(),
                        "", false, SEC_STATIC);
                }
            });
        }

        private static string CleanMethodName(string name)
        {
            if (name.Length > 4 && (name.StartsWith("get_") || name.StartsWith("set_")))
            {
                return char.ToUpperInvariant(name[4]) + name.Substring(5);
            }
            return name;
        }

        private void BuildMemberDropdown(VisualElement zone,
            MethodInfo[] methods, FieldInfo[] fields,
            string currentMember, bool currentIsField, int section)
        {
            var entries = new List<(string display, string raw, bool isField)>();

            for (int i = 0; i < fields.Length; i++)
                entries.Add((fields[i].Name, fields[i].Name, true));

            for (int i = 0; i < methods.Length; i++)
            {
                string name = methods[i].Name;
                if (name.Length > 4 && (name.StartsWith("get_") || name.StartsWith("set_")))
                    entries.Add((CleanMethodName(name), name, false));
            }

            for (int i = 0; i < methods.Length; i++)
            {
                string name = methods[i].Name;
                if (name.Length > 4 && (name.StartsWith("get_") || name.StartsWith("set_")))
                    continue;
                entries.Add((CleanMethodName(name) + "()", name, false));
            }

            var names = new List<string>(entries.Count + 1);
            names.Add("\u2014 Member \u2014");
            for (int i = 0; i < entries.Count; i++)
                names.Add(entries[i].display);

            int curIdx = 0;
            if (!string.IsNullOrEmpty(currentMember))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].raw == currentMember && entries[i].isField == currentIsField)
                    {
                        curIdx = i + 1;
                        break;
                    }
                }
            }

            Lbl(zone, "Member");
            var popup = new PopupField<string>(names, curIdx);
            popup.tooltip = "Method or field to bind";
            Field(popup);
            popup.RegisterValueChangedCallback(evt =>
            {
                int idx = names.IndexOf(evt.newValue);
                if (idx > 0)
                {
                    var picked = entries[idx - 1];
                    if (section == SEC_INSTANCE)
                    {
                        _instMethod = picked.raw;
                        _instIsField = picked.isField;
                    }
                    else
                    {
                        _statMethod = picked.raw;
                        _statIsField = picked.isField;
                    }
                }
                else
                {
                    if (section == SEC_INSTANCE)
                    {
                        _instMethod = "";
                        _instIsField = false;
                    }
                    else
                    {
                        _statMethod = "";
                        _statIsField = false;
                    }
                }
                _pickedSection = section;
            });
            zone.Add(popup);
        }

        private void Apply()
        {
            int mode = (int)DelegateMode.None;
            UnityEngine.Object obj = null;
            string cls = "", method = "", relD = "";

            if (_pickedSection == SEC_INSTANCE
                && !string.IsNullOrEmpty(_instMethod))
            {
                method = _instMethod;

                if (_instIsField)
                {
                    mode = (int)DelegateMode.FieldAccess;
                    cls = _instType != null ? _instType.AssemblyQualifiedName : "";

                    if (_targetObj == null)
                    {
                        relD = "./";
                    }
                    else if (_unrelated)
                    {
                        UnityEngine.Object instTarget = _targetObj;
                        if (_targetObj is GameObject goInst
                            && _instType != null
                            && _instType != typeof(GameObject))
                            instTarget = goInst.GetComponent(_instType);
                        obj = instTarget;
                    }
                    else
                    {
                        relD = _relativeDescriptor;
                    }
                }
                else
                {
                    if (_targetObj == null)
                    {
                        mode = (int)DelegateMode.RelativeInstance;
                        relD = "./";
                    }
                    else if (_unrelated)
                    {
                        mode = (int)DelegateMode.Instance;
                        UnityEngine.Object instTarget = _targetObj;
                        if (_targetObj is GameObject goInst
                            && _instType != null
                            && _instType != typeof(GameObject))
                            instTarget = goInst.GetComponent(_instType);
                        obj = instTarget;
                    }
                    else
                    {
                        mode = (int)DelegateMode.RelativeInstance;
                        relD = _relativeDescriptor;
                    }
                }
            }
            else if (_pickedSection == SEC_STATIC
                     && _statType != null
                     && !string.IsNullOrEmpty(_statMethod))
            {
                method = _statMethod;
                cls = _statType.AssemblyQualifiedName;

                if (_statIsField)
                {
                    mode = (int)DelegateMode.FieldAccess;
                }
                else
                {
                    if (_targetObj == null)
                    {
                        var candidates = GetStaticCandidates(_returnType, _paramTypes, _strictParams);
                        bool isFlat = false;
                        if (candidates.TryGetValue(_statType, out var methods))
                        {
                            for (int i = 0; i < methods.Length; i++)
                            {
                                if (methods[i].Name == _statMethod
                                    && methods[i].GetParameters().Length == 0)
                                {
                                    isFlat = true;
                                    break;
                                }
                            }
                        }
                        mode = isFlat ? (int)DelegateMode.FlatStatic : (int)DelegateMode.Static;
                    }
                    else if (_unrelated)
                    {
                        mode = (int)DelegateMode.BoundStatic;
                        obj = _targetObj;
                    }
                    else
                    {
                        mode = (int)DelegateMode.RelativeStatic;
                        relD = _relativeDescriptor;
                    }
                }
            }

            _effect.WriteSlot(_slotName, mode, obj, cls, method, relD);
            _effect.WriteEvalOnce(_slotName, _evalOnce);
            _effect.Reconstruct();
            _onCommit?.Invoke();
            RemoveFromHierarchy();
        }

        private static Dictionary<Type, MethodInfo[]> GetStaticCandidates(
           Type returnType, Type[] paramTypes, bool strict = false)
        {
            string key = "S:" + returnType.FullName + ":"
               + string.Join(",", paramTypes.Select(t => t.FullName))
               + (strict ? ":strict" : "");
            if (!s_scanCache.TryGetValue(key, out var result))
            {
                result = ScanAllowedTypes(returnType, paramTypes, strict);
                s_scanCache[key] = result;
            }
            return result;
        }

        private static Dictionary<Type, MethodInfo[]> ScanAllowedTypes(
           Type returnType, Type[] paramTypes, bool strict = false)
        {
            bool vd = returnType == typeof(void);
            var result = new Dictionary<Type, MethodInfo[]>();
            foreach (var type in GetAllowedStaticTypes())
            {
                var matches = type.GetMethods(
                      BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                   .Where(m =>
                   {
                       if (vd ? m.ReturnType != typeof(void) : m.ReturnType != returnType)
                           return false;
                       var ps = m.GetParameters();
                       if (ps.Length == 0) return !strict;
                       if (ps.Length != paramTypes.Length) return false;
                       for (int i = 0; i < paramTypes.Length; i++)
                           if (ps[i].ParameterType != paramTypes[i]) return false;
                       return true;
                   })
                   .ToArray();
                if (matches.Length > 0)
                    result[type] = matches;
            }
            return result;
        }

        private static readonly HashSet<string> s_excludedInstanceMethods =
           new HashSet<string> { "CancelInvoke", "StopAllCoroutines" };

        private static MethodInfo[] GetInstanceCandidates(
           Type type, Type returnType, Type[] paramTypes, bool strict = false)
        {
            string key = "I:" + type.FullName + ":" + returnType.FullName + ":"
               + string.Join(",", paramTypes.Select(t => t.FullName))
               + (strict ? ":strict" : "");
            if (!s_scanCache.TryGetValue(key, out var dict))
            {
                bool vd = returnType == typeof(void);
                bool hasLeadingObject = paramTypes.Length > 0 && paramTypes[0] == typeof(GameObject);
                Type[] strippedParams = null;
                if (hasLeadingObject)
                {
                    strippedParams = new Type[paramTypes.Length - 1];
                    for (int j = 0; j < strippedParams.Length; j++)
                        strippedParams[j] = paramTypes[j + 1];
                }

                var matches = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                   .Where(m =>
                   {
                       if (s_excludedInstanceMethods.Contains(m.Name)) return false;
                       if (vd ? m.ReturnType != typeof(void) : m.ReturnType != returnType)
                           return false;
                       var ps = m.GetParameters();
                       if (ps.Length == 0) return !strict;
                       if (ps.Length == paramTypes.Length)
                       {
                           bool ok = true;
                           for (int i = 0; i < paramTypes.Length; i++)
                               if (ps[i].ParameterType != paramTypes[i]) { ok = false; break; }
                           if (ok) return true;
                       }
                       if (strippedParams != null && ps.Length == strippedParams.Length)
                       {
                           for (int i = 0; i < strippedParams.Length; i++)
                               if (ps[i].ParameterType != strippedParams[i]) return false;
                           return true;
                       }
                       return false;
                   })
                   .ToArray();
                dict = new Dictionary<Type, MethodInfo[]> { { type, matches } };
                s_scanCache[key] = dict;
            }
            return dict.TryGetValue(type, out var r) ? r : Array.Empty<MethodInfo>();
        }

        private static Type GetFieldScanType(Type returnType, Type[] paramTypes)
        {
            if (returnType != typeof(void) && paramTypes.Length == 0)
                return returnType;
            if (returnType == typeof(void) && paramTypes.Length == 1)
                return paramTypes[0];
            if (returnType != typeof(void) && paramTypes.Length == 1
                && paramTypes[0] == typeof(GameObject))
                return returnType;
            if (returnType == typeof(void) && paramTypes.Length == 2
                && paramTypes[0] == typeof(GameObject))
                return paramTypes[1];
            return null;
        }

        private static bool IsSetterSignature(Type returnType, Type[] paramTypes)
        {
            return (returnType == typeof(void) && paramTypes.Length == 1)
                || (returnType == typeof(void) && paramTypes.Length == 2
                    && paramTypes[0] == typeof(GameObject));
        }

        private static FieldInfo[] GetMatchingInstanceFields(
            Type type, Type fieldType, bool isSetter)
        {
            if (fieldType == null) return Array.Empty<FieldInfo>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var result = new List<FieldInfo>();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != fieldType) continue;
                if (isSetter && fields[i].IsInitOnly) continue;
                result.Add(fields[i]);
            }
            return result.ToArray();
        }

        private static FieldInfo[] GetMatchingStaticFields(
            Type type, Type fieldType, bool isSetter)
        {
            if (fieldType == null) return Array.Empty<FieldInfo>();
            var fields = type.GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            var result = new List<FieldInfo>();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != fieldType) continue;
                if (isSetter && fields[i].IsInitOnly) continue;
                result.Add(fields[i]);
            }
            return result.ToArray();
        }

        private static Dictionary<Type, FieldInfo[]> GetStaticFieldCandidates(
            Type fieldType, bool isSetter)
        {
            var result = new Dictionary<Type, FieldInfo[]>();
            foreach (var type in GetAllowedStaticTypes())
            {
                var fields = GetMatchingStaticFields(type, fieldType, isSetter);
                if (fields.Length > 0)
                    result[type] = fields;
            }
            return result;
        }

        private static List<(Type type, MethodInfo[] methods, FieldInfo[] fields)> GatherInstanceTypes(
            UnityEngine.Object obj, Type returnType, Type[] paramTypes,
            bool strict, Type fieldType, bool isSetter)
        {
            var types = new List<Type>();
            if (obj is GameObject go)
            {
                types.Add(typeof(GameObject));
                foreach (var c in go.GetComponents<Component>())
                    if (!types.Contains(c.GetType())) types.Add(c.GetType());
            }
            else if (obj != null) types.Add(obj.GetType());

            var result = new List<(Type, MethodInfo[], FieldInfo[])>();
            foreach (var t in types)
            {
                var ms = GetInstanceCandidates(t, returnType, paramTypes, strict);
                var fs = GetMatchingInstanceFields(t, fieldType, isSetter);
                if (ms.Length > 0 || fs.Length > 0)
                    result.Add((t, ms, fs));
            }
            return result;
        }

        private static (Type ret, Type[] parms) SlotSignature(string slotName, Type valueType,
           ValueSlotRef valueSlot = default)
        {
            if (!valueSlot.IsNone)
            {
                Type valType = ValueSlotTypeMap(valueSlot.Type);
                if (valType != null)
                {
                    switch (slotName)
                    {
                        case "OnStart":
                        case "OnDone": return (typeof(void), new[] { valType });
                        case "EvaluateCondition": return (typeof(bool), new[] { valType });
                        case "ModifyEffectState": return (typeof(SignalEffect), new[] { valType });
                    }
                }
            }

            switch (slotName)
            {
                case "GetTargetValue":
                case "SetStartingValue": return (valueType, new[] { typeof(GameObject) });
                case "SetStartingVelocity":
                    Type velType = valueType == typeof(Quaternion) ? typeof(Vector3) : valueType;
                    return (velType, new[] { typeof(GameObject), velType });
                case "OnUpdate": return (typeof(void), new[] { typeof(GameObject), valueType });
                case "OnStart":
                case "OnDone": return (typeof(void), Type.EmptyTypes);
                case "EvaluateCondition": return (typeof(bool), Type.EmptyTypes);
                case "ModifyEffectState": return (typeof(SignalEffect), Type.EmptyTypes);
                default: return (typeof(void), Type.EmptyTypes);
            }
        }

        private static Type TypeOnObject(UnityEngine.Object obj, string methodName)
        {
            if (obj == null || string.IsNullOrEmpty(methodName)) return null;
            var types = new List<Type>();
            if (obj is GameObject go)
            {
                types.Add(typeof(GameObject));
                foreach (var c in go.GetComponents<Component>())
                    if (!types.Contains(c.GetType())) types.Add(c.GetType());
            }
            else types.Add(obj.GetType());
            return types.Find(t =>
               t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) != null);
        }

        private static Type TypeOnObjectForField(UnityEngine.Object obj, string fieldName)
        {
            if (obj == null || string.IsNullOrEmpty(fieldName)) return null;
            var types = new List<Type>();
            if (obj is GameObject go)
            {
                types.Add(typeof(GameObject));
                foreach (var c in go.GetComponents<Component>())
                    if (!types.Contains(c.GetType())) types.Add(c.GetType());
            }
            else types.Add(obj.GetType());
            return types.Find(t =>
                t.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance) != null);
        }

        private static Type ResolveType(string aqn)
        {
            if (string.IsNullOrEmpty(aqn)) return null;
            var t = Type.GetType(aqn);
            if (t != null) return t;
            foreach (var asm in JuiceBoxSettings.GetLoadedAssemblies())
            {
                t = asm.GetType(aqn);
                if (t != null) return t;
            }
            int comma = aqn.IndexOf(',');
            if (comma > 0)
            {
                string shortName = aqn.Substring(0, comma).Trim();
                foreach (var asm in JuiceBoxSettings.GetLoadedAssemblies())
                {
                    t = asm.GetType(shortName);
                    if (t != null) return t;
                }
            }
            return null;
        }

        internal static string RelationshipLabel(int mode, string relDesc)
        {
            var dm = (DelegateMode)mode;
            if (dm == DelegateMode.None || dm == DelegateMode.Static || dm == DelegateMode.FlatStatic)
                return null;
            if (dm == DelegateMode.Instance || dm == DelegateMode.BoundStatic)
                return "Unrelated";
            if (string.IsNullOrEmpty(relDesc)) return null;
            if (relDesc == "./") return "Self";
            bool hasParent = relDesc.Contains("../");
            string core = relDesc;
            int colon = core.IndexOf(':');
            if (colon >= 0) core = core.Substring(colon + 1);
            string afterParents = core.Replace("../", "");
            if (afterParents.Length == 0) return "Parent";
            if (hasParent) return "Sibling";
            return "Child";
        }

        private static (string descriptor, string label) ComputeRelationship(
            Transform target, Transform root)
        {
            if (target == root) return ("./", "Self");

            Transform[] rootChildren = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < rootChildren.Length; i++)
            {
                if (rootChildren[i] == target)
                {
                    string desc = ComputeIndexedName(rootChildren, target);
                    return (desc, "Child");
                }
            }

            int parentLevels = 0;
            Transform walk = root.parent;
            while (walk != null)
            {
                parentLevels++;
                if (walk == target)
                {
                    string desc = "";
                    for (int i = 0; i < parentLevels; i++) desc += "../";
                    return (desc, "Parent");
                }
                walk = walk.parent;
            }

            walk = root.parent;
            int levels = 1;
            while (walk != null)
            {
                Transform[] subtree = walk.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < subtree.Length; i++)
                {
                    if (subtree[i] == target)
                    {
                        string prefix = "";
                        for (int j = 0; j < levels; j++) prefix += "../";
                        string name = ComputeIndexedName(subtree, target);
                        return (prefix + name, "Sibling");
                    }
                }
                walk = walk.parent;
                levels++;
            }

            return ("", "Unrelated");
        }

        private static string ComputeIndexedName(Transform[] searchSpace, Transform target)
        {
            string targetName = target.name;
            int index = 0;
            int totalMatches = 0;
            for (int i = 0; i < searchSpace.Length; i++)
            {
                if (searchSpace[i].name == targetName)
                {
                    if (searchSpace[i] == target) index = totalMatches;
                    totalMatches++;
                }
            }
            if (index > 0) return index + ":" + targetName;
            return targetName;
        }

        private static string FormatSignature(Type returnType, Type[] paramTypes)
        {
            string ret = returnType == typeof(void) ? "void" : returnType.Name;
            string parms = paramTypes.Length == 0
               ? ""
               : string.Join(", ", paramTypes.Select(t => t.Name));
            return $"{ret} MethodName({parms})";
        }

        private void Lbl(string text) => Lbl(_contentZone, text);

        private void Lbl(VisualElement parent, string text)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = 11f; l.style.color = _theme.FieldLbl;
            l.style.marginBottom = 2f;
            parent.Add(l);
        }

        private void Info(string text) => Info(_contentZone, text);

        private void Info(VisualElement parent, string text)
        {
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.fontSize = 10.5f;
            l.style.color = _theme.NodeBorder;
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginBottom = 3f;
            parent.Add(l);
        }

        private void SectionHeader(string text)
        {
            var lbl = new Label(text) { pickingMode = PickingMode.Ignore };
            lbl.style.fontSize = 10f;
            lbl.style.color = _theme.CapLabel;
            lbl.style.marginTop = 6f;
            lbl.style.marginBottom = 2f;
            lbl.style.borderBottomWidth = 1f;
            lbl.style.borderBottomColor = _theme.PortDivider;
            lbl.style.paddingBottom = 2f;
            _contentZone.Add(lbl);
        }

        private static void Field(VisualElement el)
        { el.style.height = 28f; el.style.fontSize = 12f; el.style.marginBottom = 4f; }

        private static void FlatBtn(Button b)
        {
            b.style.borderTopWidth = b.style.borderBottomWidth =
            b.style.borderLeftWidth = b.style.borderRightWidth = 0f;
        }

    }
}