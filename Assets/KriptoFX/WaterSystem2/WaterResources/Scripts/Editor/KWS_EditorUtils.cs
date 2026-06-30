#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;


namespace KWS
{
    internal static class KWS_EditorUtils
    {
        static Texture2D _buttonTex;
        static Texture2D _editorTabTex;
        static VideoTooltipWindow window;
        static string pathToHelpVideos;
        public static Color DefaultLineColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        private static Color _globalFieldColor = new Color(0.75f, 0.85f, 1.0f);
        
        static List<string> _layers       = new List<string>();
        static List<int>    _layerNumbers = new List<int>();

        
        
        #region styles



        static GUIStyle _helpBoxStyle;

        public static GUIStyle HelpBoxStyle
        {
            get
            {
                if (_helpBoxStyle == null)
                {
                    _helpBoxStyle = new GUIStyle("button");
                    _helpBoxStyle.alignment = TextAnchor.MiddleCenter;
                    _helpBoxStyle.stretchHeight = false;
                    _helpBoxStyle.stretchWidth = false;
                }

                return _helpBoxStyle;
            }
        }

        static GUIStyle _buttonStyle;

        public static GUIStyle ButtonStyle
        {
            get
            {
                if (_buttonStyle == null)
                {
                    _buttonStyle = new GUIStyle();
                    _buttonStyle.overflow.left = ButtonStyle.overflow.right = 3;
                    _buttonStyle.overflow.top = 1;
                    _buttonStyle.overflow.bottom = 2;
                }
              
                if (_buttonTex == null)
                {
                    _buttonTex = CreateTex(32, 32, EditorGUIUtility.isProSkin ? new Color(78 / 255f, 79 / 255f, 80 / 255f) : new Color(171 / 255f, 171 / 255f, 171 / 255f));
                    _buttonStyle.normal.background = _buttonTex;
                }

                ;
                return _buttonStyle;
            }
        }

        private static GUIStyle _notesLabelStyleEmpty;

        public static GUIStyle NotesLabelStyleEmpty
        {
            get
            {
                if (_notesLabelStyleEmpty == null)
                {
                    _notesLabelStyleEmpty = new GUIStyle("label");
                    _notesLabelStyleEmpty.normal.textColor = new Color(1, 1, 1, 0);
                }

                return _notesLabelStyleEmpty;
            }
        }

        private static GUIStyle _notesLabelStyleFade;

        public static GUIStyle NotesLabelStyleFade
        {
            get
            {
                if (_notesLabelStyleFade == null)
                {
                    _notesLabelStyleFade                  = new GUIStyle("label");
                    _notesLabelStyleFade.normal.textColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
                    _notesLabelStyleFade.wordWrap         = true;
                }

                return _notesLabelStyleFade;
            }
        }

        private static GUIStyle _notesLabelStyleInfo;

        public static GUIStyle NotesLabelStyleInfo
        {
            get
            {
                if (_notesLabelStyleInfo == null)
                {
                    _notesLabelStyleInfo = new GUIStyle("label");
                    _notesLabelStyleInfo.normal.textColor = new Color(0.75f, 0.95f, 0.75f, 0.95f);
                }

                return _notesLabelStyleInfo;
            }
        }

        static GUIStyle _notesLabelStyle;

        public static GUIStyle NotesLabelStyle
        {
            get
            {
                if (_notesLabelStyle == null)
                {
                    _notesLabelStyle                  = new GUIStyle(GUI.skin.label);
                    _notesLabelStyle.normal.textColor = new Color(0.85f, 0.75f, 0.75f, 0.95f);
                    _notesLabelStyle.wordWrap         = true;
                }

                return _notesLabelStyle;
            }
        }

        static GUIStyle _tabNameTextStyle;

        public static GUIStyle TabNameTextStyle
        {
            get
            {
                if (_tabNameTextStyle == null)
                {
                    _tabNameTextStyle = new GUIStyle(EditorStyles.foldout);
                    _tabNameTextStyle.fontSize = 13;
                    _tabNameTextStyle.padding = new RectOffset(16, 0, 0, 0);
                    //_tabNameTextStyle.clipping = TextClipping.Overflow;
                    _tabNameTextStyle.fontStyle = FontStyle.Bold;
                }

                return _tabNameTextStyle;
            }
        }

        static GUIStyle _expertButtonStyle;

        public static GUIStyle ExpertButtonStyle
        {
            get
            {
                if (_expertButtonStyle == null)
                {
                    _expertButtonStyle = new GUIStyle(GUI.skin.button);
                    _expertButtonStyle.fontSize = 10;
                    _expertButtonStyle.normal.textColor = new Color(1, 1, 1, 0.5f);
                }

                return _expertButtonStyle;
            }
        }

        static GUIStyle _editorTabStyle;

        public static GUIStyle EditorTabStyle
        {
            get
            {
                if (_editorTabStyle == null)
                {
                    _editorTabStyle = new GUIStyle(GUI.skin.box);
                }

                if (_editorTabTex == null)
                {
                    _editorTabTex = CreateBorderTex(128, 128, new Color(0f, 1f, 0f, 0.6f));
                    _editorTabStyle.normal.background = _editorTabTex;
                }

                return _editorTabStyle;
            }
        }

        static Texture2D CreateTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        static Texture2D CreateBorderTex(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1) pix[y * height + x] = col;
                }
            }

            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }


        static GUIStyle _boldText;

        public static GUIStyle BoldText
        {
            get
            {
                if (_boldText == null)
                {
                    _boldText = new GUIStyle(EditorStyles.boldLabel);
                    _boldText.fontSize = 16;
                    _boldText.fontStyle = FontStyle.Bold;
                }

                return _boldText;
            }
        }

        #endregion

        #region GUI

        public static void OpenHelpVideoWindow(KWS_EditorVideoTipsSettings.VideoSettings videoSettings)
        {
            if (window != null) window.Close();
            if (window == null) window = (VideoTooltipWindow)EditorWindow.GetWindow(typeof(VideoTooltipWindow));
            
            window.TipSettings = videoSettings;
            window.maxSize     = new Vector2(854, 480);
            window.minSize     = new Vector2(854, 480);
            window.Show();
        }

        public static void Line(Color color = default, int thickness = 1, int padding = 10, Vector2Int margin = default)
        {
            if (color == default) color = DefaultLineColor;
            var r = EditorGUILayout.GetControlRect(false, GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding * 0.5f;

            if (margin == default) margin = new Vector2Int(16, 90);
            r.x += margin.x;
            r.width = EditorGUIUtility.currentViewWidth - margin.y;

            EditorGUI.DrawRect(r, color);
        }

        static void HelpWindowButton(KWS_EditorVideoTipsSettings.VideoSettings videoSettings)
        {
            var guiEnabled = GUI.enabled;
            GUI.enabled = true;
            GUILayout.Label("", GUILayout.Width(6));
            if (GUILayout.Button("?", HelpBoxStyle, GUILayout.Width(16), GUILayout.Height(16))) OpenHelpVideoWindow(videoSettings);
            GUI.enabled = guiEnabled;
        }

        public static float Slider(string text, string description, float value, float leftValue, float rightValue, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.Slider(new GUIContent(text, description), value, leftValue, rightValue);
            if(videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public static int IntSlider(string text, string description, int value, int leftValue, int rightValue, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.IntSlider(new GUIContent(text, description), value, leftValue, rightValue); 
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public static int IntField(string text, string description, int value, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.IntField(new GUIContent(text, description), value);
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public static float FloatField(string text, string description, float value, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.FloatField(new GUIContent(text, description), value);
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public static Vector2 Vector2Field(string text, string description, Vector2 value, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.Vector2Field(new GUIContent(text, description), value);
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public static Vector3 Vector3Field(string text, string description, Vector3 value, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.Vector3Field(new GUIContent(text, description), value);
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public static Color ColorField(string text, string description, Color value, bool shoeEyedropper, bool showAlpha, bool hdr, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.ColorField(new GUIContent(text, description), value, shoeEyedropper, showAlpha, hdr);
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public static Color ColorFieldHUE(string text, string description, Color value, bool shoeEyedropper, bool showAlpha, bool hdr, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.ColorField(new GUIContent(text, description), value, shoeEyedropper, showAlpha, hdr);
            Color.RGBToHSV(newValue, out float H, out float S, out float V);
            var hueValue = Color.HSVToRGB(H, S, 1);
           
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return hueValue;
        }

        public static Enum EnumPopup(string text, string description, Enum value, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.EnumPopup(new GUIContent(text, description), value);
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }
        
      
        public static int MaskField(string text, string description, int mask, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginHorizontal();
        
            _layers.Clear();
            _layerNumbers.Clear();

           for (int i = 0; i < 32; i++)
           {
               string layerName = LayerMask.LayerToName(i);
               if (layerName != "")
               {
                   _layers.Add(layerName);
                   _layerNumbers.Add(i);
               }
           }

           int maskVal = 0;
           for (int i = 0; i < _layerNumbers.Count; i++)
           {
               if (((1 << _layerNumbers[i]) & mask) > 0)
               {
                   maskVal |= (1 << i);
               }
           }

           maskVal = EditorGUILayout.MaskField(new GUIContent(text, description), maskVal, _layers.ToArray());
           
           int finalMask = 0;
           for (int i = 0; i < _layerNumbers.Count; i++)
           {
               if ((maskVal & (1 << i)) > 0)
               {
                   finalMask |= (1 << _layerNumbers[i]);
               }
           }

            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return finalMask;
        }

        public static bool Toggle(string text, string description, bool value, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal();
            var newValue = EditorGUILayout.Toggle(new GUIContent(text, description), value, options);
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            return newValue;
        }

        public delegate void WaterTabSettings();


        internal static void KWS_Tab(ref bool isVisibleTab, string tabName, WaterTabSettings settings, WaterSystem.WaterSettingsCategory waterTab, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal(ButtonStyle);
            EditorGUILayout.LabelField("", GUILayout.MaxWidth(9));
            isVisibleTab = EditorGUILayout.Foldout(isVisibleTab, new GUIContent(tabName), true, TabNameTextStyle);
          

            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            if (isVisibleTab)
            {
                GUILayout.Space(5);
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                settings.Invoke();
                if (EditorGUI.EndChangeCheck())
                {
                    WaterSystem.OnAnyWaterSettingsChanged?.Invoke(waterTab);
                }

                GUILayout.Space(5);
         
                EditorGUI.indentLevel--;
                //GUILayout.Space(15);
            }

            EditorGUILayout.EndVertical();

        }

       

        internal static void KWS_Tab(bool isWaterActive, ref bool isToogleSelected, ref bool isVisibleTab, string tabName, WaterTabSettings settings, WaterSystem.WaterSettingsCategory waterTab, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal(ButtonStyle);

            EditorGUI.BeginChangeCheck();
            isToogleSelected = EditorGUILayout.Toggle(isToogleSelected, GUILayout.MaxWidth(14), GUILayout.MaxHeight(16));
            if (EditorGUI.EndChangeCheck()) WaterSystem.OnAnyWaterSettingsChanged?.Invoke(waterTab);

            GUILayout.Space(14);
            isVisibleTab = EditorGUILayout.Foldout(isVisibleTab, new GUIContent(tabName), true, TabNameTextStyle);
           
            if (videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();

            if (isVisibleTab)
            {
                if (isWaterActive) GUI.enabled = isToogleSelected;
                GUILayout.Space(5);
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                settings.Invoke();
                if (EditorGUI.EndChangeCheck()) WaterSystem.OnAnyWaterSettingsChanged?.Invoke(waterTab);

                GUILayout.Space(5);

                EditorGUI.indentLevel--;
                //GUILayout.Space(15);
                if (isWaterActive) GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
        }


        public static void KWS_EditorTab(bool isEditorEnabled, WaterTabSettings settings)
        {
            if (isEditorEnabled)
            {
                GUI.enabled = true;
                using (new EditorGUILayout.VerticalScope(EditorTabStyle)) settings.Invoke();
                GUI.enabled = false;
            }
            else settings.Invoke();
        }

        public static void KWS_EditorMessage(string message, MessageType messageType)
        {
            EditorGUILayout.HelpBox(message, messageType);
        }

        public static bool SaveButton(string txt, bool isActive)
        {
            return SaveButton(txt, isActive, EditorStyles.miniButton);
        }

        public static bool SaveButton(string txt, bool isActive, GUIStyle style)
        {
            if (isActive)
            {
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                var state = GUILayout.Button(txt, style);
                GUI.backgroundColor = oldColor;
                return state;
            }
            else return GUILayout.Button(txt, style);
        }

        #endregion

        public enum UnityPipeline
        {
            Builtin,
            URP,
            HDRP,
            Unknown
        }

        public static UnityPipeline GetCurrentPipeline()
        {
            var pipeline     = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null)
            {
                return UnityPipeline.Builtin;
            }
            else
            {
                string rpName = pipeline.GetType().ToString();

                if (rpName.Contains("UniversalRenderPipelineAsset") || rpName.Contains("UniversalRenderPipeline"))
                {
                    return UnityPipeline.URP;
                }
                else if (rpName.Contains("HDRenderPipelineAsset"))
                {
                    return UnityPipeline.HDRP;
                }
                else
                {
                    return UnityPipeline.Unknown;
                }
            }
        }

        public static bool IsHDModuleInstalled()
        {
            return IsScriptAvailable("KWS_Ocean");

        }

        
        public static Camera GetSceneCamera()
        {
            //if (SceneView.lastActiveSceneView != null) _sceneCamera = SceneView.lastActiveSceneView.camera;
            //else
            //{
            //    var camCurrent                                                                        = Camera.current;
            //    if (camCurrent != null && camCurrent.cameraType == CameraType.SceneView) _sceneCamera = camCurrent;
            //}
            //return _sceneCamera;
            return SceneView.lastActiveSceneView.camera;
        }

        static Vector3 _latestMouseWorldPos;

        public static SerializedProperty Get(this SerializedObject settings, System.Object obj)
        {
            return settings.FindProperty(nameof(obj));
        }


        public static Vector3 GetMouseWorldPosProjectedToWaterPlane(float height, Event e)
        {
            var mousePos = e.mousePosition;
            var plane = new Plane(Vector3.down, height);
            var ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (plane.Raycast(ray, out var distanceToPlane))
            {
                _latestMouseWorldPos = ray.GetPoint(distanceToPlane);
                return _latestMouseWorldPos;
            }

            return _latestMouseWorldPos;
        }


        public static void Release()
        {
            KW_Extensions.SafeDestroy(_buttonTex, _editorTabTex);
        }

        public static void SetEditorCameraPosition(Vector3 worldPos)
        {
            SceneView.lastActiveSceneView.LookAt(worldPos);
        }

        public static void LockLeftClickSelection(int controlID)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                {
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
            }
        }

        public static bool MouseRaycast(out RaycastHit hit)
        {
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                return true;
            }

            return false;
        }


        public static void DisplayMessageNotification(string msg, bool useDebugError, float time = 5)
        {
            if (useDebugError) Debug.LogError(msg);

#if UNITY_EDITOR
            foreach (UnityEditor.SceneView sv in UnityEditor.SceneView.sceneViews)
                sv.ShowNotification(new GUIContent(msg), time);
#endif
        }

        static void ReplaceShaderText(string shaderText, string pattern, string newText)
        {
            var result = Regex.Replace(shaderText, pattern, newText);

        }

        public static void SetShaderTextDefine(string shaderPath, bool lockFile, string define, bool enabled)
        {
            
            
            var pathToShadersFolder    = KW_Extensions.GetShadersFolderAssetPath();
            var platorfmSpecificShader = Path.Combine(pathToShadersFolder, shaderPath);
            var shaderText             = File.ReadAllText(platorfmSpecificShader);

            var pattern = @"\/{0,}#define\s{1,}" + define; //pattern for such lines with spaces      ->     //#define   ENVIRO_FOG  //comment
            var newText = enabled ? $"#define {define}" : $"//#define {define}";
            var newShader = Regex.Replace(shaderText, pattern, newText, RegexOptions.Singleline);

            if (lockFile)
            {
                FileInfo file = new FileInfo(platorfmSpecificShader);
                file.IsReadOnly = false;
                File.WriteAllText(platorfmSpecificShader, newShader);
                file.IsReadOnly = true;
            }
            else
            {
                File.WriteAllText(platorfmSpecificShader, newShader);
            }
       
            //Debug.Log("Write shader keyword " + define);
            
        }
        
        public static UnityPipeline GetActiveShaderDefine(string shaderPath)
        {
            var pathToShadersFolder = KW_Extensions.GetShadersFolderAssetPath();
            var fullPath            = Path.Combine(pathToShadersFolder, shaderPath);
            var shaderText          = File.ReadAllText(fullPath);

            if (shaderText.Contains("#define KWS_BUILTIN")) return UnityPipeline.Builtin;
            if (shaderText.Contains("#define KWS_URP")) return UnityPipeline.URP;
            if (shaderText.Contains("#define KWS_HDRP")) return UnityPipeline.HDRP;

            return UnityPipeline.Unknown;
        }

        public static void DisableAllShaderTextDefines(string shaderPath, bool lockFile, params string[] defines)
        {
            var pathToShadersFolder    = KW_Extensions.GetShadersFolderAssetPath();
            var platorfmSpecificShader = Path.Combine(pathToShadersFolder, shaderPath);
            var shaderText             = File.ReadAllText(platorfmSpecificShader);

            foreach (var define in defines)
            {
                var pattern   = @"\/{0,}#define\s{1,}" + define; //pattern for such lines with spaces      ->     //#define   ENVIRO_FOG  //comment
                var newText   = $"//#define {define}";
                shaderText = Regex.Replace(shaderText, pattern, newText, RegexOptions.Singleline);

            }
            if (lockFile)
            {
                FileInfo file = new FileInfo(platorfmSpecificShader);
                file.IsReadOnly = false;
                File.WriteAllText(platorfmSpecificShader, shaderText);
                file.IsReadOnly = true;
            }
            else
            {
                File.WriteAllText(platorfmSpecificShader, shaderText);
            }
           
        }

        public static void SetShaderTextQueue(string shaderPath, int queue)
        {
            var pathToShadersFolder    = KW_Extensions.GetShadersFolderAssetPath();
            var platorfmSpecificShader = Path.Combine(pathToShadersFolder, shaderPath);
            var shaderText             = File.ReadAllText(platorfmSpecificShader);

            var queueText = queue == 3000 ? "" : queue > 3000 ? $"+{queue - 3000}" : $"-{3000 - queue}";
            var newText = $"\"Queue\" = \"Transparent{queueText}\"";

            var pattern = "\"Queue\"\\s{0,}=\\s{0,}\"Transparent.{0,2}\"";
            var newShader = Regex.Replace(shaderText, pattern, newText, RegexOptions.Singleline);

            File.WriteAllText(platorfmSpecificShader, newShader);
        }

        public static void ChangeShaderTextIncludePath(string shaderPath, string shaderDefine, string newPath)
        {
            if (shaderDefine.Length == 0) return;

            var pathToShadersFolder    = KW_Extensions.GetShadersFolderAssetPath();
            var platorfmSpecificShader = Path.Combine(pathToShadersFolder, shaderPath);
            if (!File.Exists(platorfmSpecificShader))
            {
                Debug.LogError($"Can't find the file {platorfmSpecificShader}");
                return;
            }

            var shaderLines = File.ReadAllLines(platorfmSpecificShader);

            var searchPattern = $"#if defined({shaderDefine})";
            var lineIdx       = 0;
            var insideBlock   = false;
    
            while (lineIdx < shaderLines.Length)
            {
                if (!insideBlock && shaderLines[lineIdx].Contains(searchPattern))
                {
                    insideBlock = true;
                }
                else if (insideBlock)
                {
                    if (shaderLines[lineIdx].Contains("#include"))
                    {
                        shaderLines[lineIdx] = $"   #include \"{newPath}\"";
                        break;
                    }
            
                    if (shaderLines[lineIdx].Contains("#endif") || shaderLines[lineIdx].Contains("#if "))
                    {
                        Debug.LogWarning($"No #include found in block {shaderDefine}");
                        break;
                    }
                }

                lineIdx++;
            }

            File.WriteAllLines(platorfmSpecificShader, shaderLines);
        }

        
        
        
        

        public static int GetEnabledDefineIndex(string shaderPath, List<string> defines)
        {
            var pathToShadersFolder    = KW_Extensions.GetShadersFolderAssetPath();
            var platorfmSpecificShader = Path.Combine(pathToShadersFolder, shaderPath);
            if (!File.Exists(platorfmSpecificShader))
            {
                Debug.LogError($"Can't find the file {platorfmSpecificShader}");
                return 0;
            }

            var shaderLines = File.ReadAllLines(platorfmSpecificShader);

            for (var index = 0; index < defines.Count; index++)
            {
                var define = defines[index];
                if (define.Length == 0) continue;
                var pattern = $"#define {define}";

                for (int lineIndex = 0; lineIndex < shaderLines.Length; lineIndex++)
                {
                    if (shaderLines[lineIndex] == pattern) return index;
                }
            }

            return 0;
        }


        public static bool CompareValues(this ScriptableObject obj1, ScriptableObject obj2)
        {
            if (obj1 == null || obj2 == null)
            {
                return false;
            }

            if (obj1.GetType() != obj2.GetType())
            {
                return false;
            }

            var type1 = obj1.GetType();
            var type2 = obj2.GetType();
            var profileFields = type1.GetFields();
            foreach (var profileFieldInfo in profileFields)
            {
                var value1 = type1.GetField(profileFieldInfo.Name).GetValue(obj1);
                var value2 = type2.GetField(profileFieldInfo.Name).GetValue(obj2);
                if (!Equals(value1, value2)) return false;
            }


            return true;
        }

        public static string GetNormalizedSceneName()
        {
            var sceneName                       = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.Length < 1) sceneName = "EmptyScene";
            return sceneName;
        }



        public static Texture2DArray CopyGpuTextureToCpu(this RenderTexture rt)
        {
            if (rt == null) return null;
            if (rt.dimension != UnityEngine.Rendering.TextureDimension.Tex2DArray)
            {
                Debug.LogError("RenderTexture is not Texture2DArray");
                return null;
            }

            int w      = rt.width;
            int h      = rt.height;
            int slices = rt.volumeDepth;

            var texArray          = new Texture2DArray(rt.width, rt.height, rt.volumeDepth, rt.graphicsFormat, rt.useMipMap ? TextureCreationFlags.MipChain : TextureCreationFlags.None);

            var prevRT = RenderTexture.active;

            for (int slice = 0; slice < slices; slice++)
            {
              
                var tmp = RenderTexture.GetTemporary(w, h, 0, rt.graphicsFormat);
                Graphics.CopyTexture(rt, slice, 0, tmp, 0, 0);
              
                RenderTexture.active = tmp;
                var tex2D = new Texture2D(w, h, rt.graphicsFormat, rt.useMipMap ? TextureCreationFlags.MipChain : TextureCreationFlags.None);
                tex2D.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex2D.Apply();

                texArray.SetPixels(tex2D.GetPixels(), slice);

                RenderTexture.ReleaseTemporary(tmp);
            }

            texArray.Apply();
            RenderTexture.active = prevRT;

            return texArray;
        }


        public enum UsedChannels
        {
            _R = 0,
            _RG = 1,
            _RGB = 2,
            _RGBA = 3
        }

        public static void SaveRenderTextureToPNG(this RenderTexture rt, string pathToFileWithoutExtension, bool useMips, bool autoRefresh)
        {
#if UNITY_EDITOR
            if (rt == null) return;

            var currentRT = RenderTexture.active;
            RenderTexture.active = rt;
            
            var tex          = new Texture2D(rt.width, rt.height, rt.graphicsFormat, useMips ? TextureCreationFlags.MipChain : TextureCreationFlags.None);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            var rawData = tex.EncodeToPNG();
            File.WriteAllBytes(pathToFileWithoutExtension + ".png", rawData);
            
            RenderTexture.active = currentRT;
            KW_Extensions.SafeDestroy(tex);
           if(autoRefresh) AssetDatabase.Refresh();
#endif
        }
        
        
        public static void SaveRenderTextureToEXR(this RenderTexture rt, string pathToFileWithoutExtension, bool useMips, bool autoRefresh = true)
        {
#if UNITY_EDITOR
            if (rt == null) return;

            var fullName  = pathToFileWithoutExtension + ".exr";
            var directory = Path.GetDirectoryName(fullName);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var       currentRT = RenderTexture.active;
            Texture2D tex       = null;

            try
            {
                RenderTexture.active = rt;

                tex = new Texture2D(
                    rt.width,
                    rt.height,
                    rt.graphicsFormat,
                    useMips ? TextureCreationFlags.MipChain : TextureCreationFlags.None
                );

                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply(useMips, false);

                var rawData = tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                File.WriteAllBytes(fullName, rawData);
            }
            finally
            {
                RenderTexture.active = currentRT;
                if (tex != null) KW_Extensions.SafeDestroy(tex);
            }

            if (autoRefresh)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            var relativePath = pathToFileWithoutExtension.GetRelativeToAssetsPath() + ".exr";
            var importer     = AssetImporter.GetAtPath(relativePath) as TextureImporter;

            if (importer != null)
            {
                importer.textureType        = TextureImporterType.Lightmap;
                importer.sRGBTexture        = false;
                importer.alphaSource        = TextureImporterAlphaSource.None;
                importer.mipmapEnabled      = useMips;
                importer.streamingMipmaps   = false;
                importer.wrapMode           = TextureWrapMode.Repeat;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
#endif
        }

        
        public static void SaveTexture(this Texture2D tex, string pathToFileWithoutExtension, bool useAutomaticCompressionFormat, bool autoRefresh, UsedChannels usedChannels = default, bool isHDR = false, bool mipChain = false)
        {
#if UNITY_EDITOR
            KWS_TextureImporter.Write(tex, pathToFileWithoutExtension, useAutomaticCompressionFormat, usedChannels, isHDR, mipChain);
            if(autoRefresh) AssetDatabase.Refresh();
#endif
        }

        public static void SaveRenderTexture(this RenderTexture rt, string pathToFileWithoutExtension, bool useAutomaticCompressionFormat, bool autoRefresh, UsedChannels usedChannels = default, bool isHDR = false, bool mipChain = false)
        {
#if UNITY_EDITOR
            if (rt == null) return;

            var currentRT = RenderTexture.active;
            RenderTexture.active = rt;

            var targetFormat = isHDR ? TextureFormat.RGBAHalf : TextureFormat.RGBA32;
            var tex = new Texture2D(rt.width, rt.height, targetFormat, false, linear: true);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            KWS_TextureImporter.Write(tex, pathToFileWithoutExtension, useAutomaticCompressionFormat, usedChannels, isHDR, mipChain);
            RenderTexture.active = currentRT;
            KW_Extensions.SafeDestroy(tex);
            if(autoRefresh) AssetDatabase.Refresh();
#endif
        }

        public static void SaveRenderTexture(this RenderTexture rt, string pathToFileWithoutExtension, bool autoRefresh)
        {
#if UNITY_EDITOR
            if (rt == null) return;

            var currentRT = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(rt.width, rt.height, GraphicsFormatUtility.GetTextureFormat(rt.graphicsFormat), false, linear: true);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            KWS_TextureImporter.Write(tex, pathToFileWithoutExtension, false, UsedChannels._RGBA, false, false);
            RenderTexture.active = currentRT;
            KW_Extensions.SafeDestroy(tex);
            if(autoRefresh) AssetDatabase.Refresh();
#endif
        }


        public static void SaveRenderTextureDepth32(this RenderTexture rt, string pathToFileWithoutExtension, bool autoRefresh)
        {
#if UNITY_EDITOR
            if (rt == null) return;
            var currentRT = RenderTexture.active;

            var tempRT    = RenderTexture.GetTemporary(rt.width, rt.height, 0, GraphicsFormat.R32_SFloat);
            Graphics.Blit(rt, tempRT);
            RenderTexture.active = tempRT;

            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, linear: true);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            KWS_TextureImporter.Write(tex, pathToFileWithoutExtension, false, UsedChannels._RGBA, false, false);
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(tempRT);

            KW_Extensions.SafeDestroy(tex);
            if(autoRefresh) AssetDatabase.Refresh();
#endif
        }



        /////////////////////////////////////////////////// kws2/////////////////////////////////////////////


        internal static void KWS2_Tab(ref bool isVisibleTab, string tabName, WaterTabSettings settings, WaterSystem.WaterSettingsCategory waterTab, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null, float foldoutSpace = 0)
        {

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal(ButtonStyle);
            GUILayout.Space(-10);
            EditorGUILayout.LabelField("", GUILayout.MaxWidth(9));
            if(foldoutSpace > 0) GUILayout.Space(foldoutSpace);
            isVisibleTab = EditorGUILayout.Foldout(isVisibleTab, new GUIContent(tabName), true, TabNameTextStyle);

            if(videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();
            if (isVisibleTab)
            {
                GUILayout.Space(5);
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                settings.Invoke();
                if (EditorGUI.EndChangeCheck())
                {
                    WaterSystem.OnAnyWaterSettingsChanged?.Invoke(waterTab);
                }

                GUILayout.Space(5);

                EditorGUI.indentLevel--;
                //GUILayout.Space(15);
            }

            EditorGUILayout.EndVertical();


        }

        internal static void KWS2_TabWithEnabledToogle(ref bool isToogleSelected, ref bool isVisibleTab, string tabName, WaterTabSettings settings, WaterSystem.WaterSettingsCategory waterTab, KWS_EditorVideoTipsSettings.VideoSettings videoSettings = null, float foldoutSpace = 0)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal(ButtonStyle);

            EditorGUI.BeginChangeCheck();
            isToogleSelected = EditorGUILayout.Toggle(isToogleSelected, GUILayout.MaxWidth(14), GUILayout.MaxHeight(16));
            if (EditorGUI.EndChangeCheck()) WaterSystem.OnAnyWaterSettingsChanged?.Invoke(waterTab);

            if(foldoutSpace > 0) GUILayout.Space(foldoutSpace);
            isVisibleTab = EditorGUILayout.Foldout(isVisibleTab, new GUIContent(tabName), true, TabNameTextStyle);


            if(videoSettings != null) HelpWindowButton(videoSettings);
            EditorGUILayout.EndHorizontal();

            if (isVisibleTab)
            {
                GUILayout.Space(5);
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                settings.Invoke();
                if (EditorGUI.EndChangeCheck()) WaterSystem.OnAnyWaterSettingsChanged?.Invoke(waterTab);

                GUILayout.Space(5);


                EditorGUI.indentLevel--;
                //GUILayout.Space(15);
            }

            EditorGUILayout.EndVertical();

        }
        //
        // #if KWS_URP
        //     public static List<UnityEngine.Rendering.Universal.ScriptableRendererFeature> GetRendererFeatures() 
        //     {
        //         var renderer = (GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)?.scriptableRenderer;
        //         return typeof(UnityEngine.Rendering.Universal.ScriptableRenderer)
        //               .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance)
        //              ?.GetValue(renderer) as List<UnityEngine.Rendering.Universal.ScriptableRendererFeature>;
        //     }
        // #endif
        
        
        
        public static bool IsModuleInstalled(string asmdefName)
        {
            var guids = AssetDatabase.FindAssets("t:asmdef " + asmdefName);
            return guids != null && guids.Length > 0;
        }
        
        public static bool IsScriptAvailable(string fullTypeName)
        {
            var guids = AssetDatabase.FindAssets("t:MonoScript " + fullTypeName);
            return guids != null && guids.Length > 0;
        }
    }

}
#endif