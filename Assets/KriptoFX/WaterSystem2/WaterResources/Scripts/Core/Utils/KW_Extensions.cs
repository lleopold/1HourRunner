using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace KWS
{
    internal static class KW_Extensions
    {
  
        static float prevRealTime;
        static float lastDeltaTime;

        public struct SphereBounds
        {
            public Vector3 center;
            public float   radius;

            public SphereBounds(Vector3 c, float r)
            {
                center = c;
                radius = r;
            }
        }


        public enum AsyncInitializingStatusEnum
        {
            NonInitialized,
            StartedInitialize,
            Initialized,
            Failed,
            BakingStarted
        }


        internal static bool HasTab(this WaterSystem.WaterSettingsCategory value, WaterSystem.WaterSettingsCategory flag)
        {
            return (value & flag) == flag;
            //(mask.ToUInt32(null) & flag.ToUInt32(null)) != 0
        }

        public static TEnum StringToEnum<TEnum>(this string value, TEnum defaultValue = default) where TEnum : struct
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;

            return Enum.TryParse(value, true, out TEnum result) ? result : defaultValue;
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();

            if (component == null)
            {
                component           = go.AddComponent<T>() as T;
            }

            return component;

        }

        public static GameObject CreateHiddenGameObject(string name)
        {
            var go = new GameObject(name);
#if KWS_DEBUG
            go.hideFlags = HideFlags.DontSave;
#else
            go.hideFlags = HideFlags.HideAndDontSave;
#endif
            return go;

        }
        

        public static void SafeDestroy(params UnityEngine.Object[] components)
        {
            if (!Application.isPlaying)
            {
                foreach (var component in components)
                {
                    if (component) UnityEngine.Object.DestroyImmediate(component);
                }

            }
            else
            {
                foreach (var component in components)
                {
                    if (component) UnityEngine.Object.Destroy(component);
                }
            }
        }

        public static void SafeRelease(params RenderTexture[] renderTextures)
        {
            for (var i = 0; i < renderTextures.Length; i++)
            {
                if (renderTextures[i] != null) renderTextures[i].Release();
                renderTextures[i] = null;
            }
        }

        public static bool IsAlwaysRefreshEnabled()
        {
            #if UNITY_EDITOR
            if (SceneView.lastActiveSceneView == null) return false;
                
                var alwaysRefreshEnabled =  SceneView.lastActiveSceneView.sceneViewState.alwaysRefreshEnabled;
                return alwaysRefreshEnabled;
            #else 
                return false;
            #endif

        }
       
        public static float TotalTime()
        {
            if (WaterSystem.UseNetworkTime) return WaterSystem.NetworkTime * WaterSystem.GlobalTimeScale;
#if UNITY_EDITOR
            
            return (Application.isPlaying ? Time.time  : Time.realtimeSinceStartup * KWS_UpdateManager.EditorRefreshScale) * WaterSystem.GlobalTimeScale;
#else
            return Time.time * WaterSystem.GlobalTimeScale;
#endif
        }

        public static float DeltaTime()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return Time.deltaTime;
            }
            else
            {
                return Mathf.Max(0, lastDeltaTime * KWS_UpdateManager.EditorRefreshScale);
            }
#else
            return Time.deltaTime;
#endif

        }
        

        [Conditional("UNITY_EDITOR")]
        public static void UpdateEditorDeltaTime()
        {
            if (Application.isPlaying) return;
           
            lastDeltaTime = Time.realtimeSinceStartup - prevRealTime;
            prevRealTime = Time.realtimeSinceStartup;
        }



        static bool _initializedPath;
        static string _cachedRootAssetPath;

        public static string GetWaterSystemRootAssetPath()
        {
#if UNITY_EDITOR
            if (_initializedPath) return _cachedRootAssetPath;
            _initializedPath = true;

            var guids = AssetDatabase.FindAssets("KriptoFX.KWS2.Core.Runtime t:asmdef");
            foreach (var guid in guids)
            {
                var asmdefPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(asmdefPath)) continue;

                var asmdefFolder = Path.GetDirectoryName(asmdefPath)?.Replace("\\", "/");
                if (string.IsNullOrEmpty(asmdefFolder)) continue;

                var root = Path.GetDirectoryName(asmdefFolder)?.Replace("\\", "/");
                if (string.IsNullOrEmpty(root)) continue;

                if (AssetDatabase.IsValidFolder(root))
                {
                    _cachedRootAssetPath = root;
                    return _cachedRootAssetPath;
                }
            }

            Debug.LogError("Can't find 'KriptoFX.KWS2.Core.Runtime.asmdef'");
            _cachedRootAssetPath = string.Empty;
            return string.Empty;
           
#else
            return string.Empty;
#endif
        }

        public static string GetResourcesFolderAssetPath()
        {
#if UNITY_EDITOR
            var root = GetWaterSystemRootAssetPath();
            if (string.IsNullOrEmpty(root)) return string.Empty;

            var path = $"{root}/WaterQualitySettings/Resources";
            if (AssetDatabase.IsValidFolder(path)) return path;

            Debug.LogError("Can't find 'KriptoFX/WaterSystem2/WaterQualitySettings/Resources' folder");
            return string.Empty;
#else
            return string.Empty;
#endif 
        }

        public static string GetShadersFolderAssetPath()
        {
#if UNITY_EDITOR
            var root = GetWaterSystemRootAssetPath();
            if (string.IsNullOrEmpty(root)) return string.Empty;

            var path = $"{root}/WaterResources/Shaders";
            if (AssetDatabase.IsValidFolder(path)) return path;

            Debug.LogError("Can't find 'KriptoFX/WaterSystem2/WaterResources/Shaders' folder");
            return string.Empty;            
#else
            return string.Empty;
#endif 
        }

        public static string GetRelativeToAssetsPath(this string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return string.Empty;

            fullPath = Path.GetFullPath(fullPath).Replace("\\", "/");
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");

            if (!fullPath.StartsWith(projectRoot + "/"))
            {
                Debug.LogError($"Path is outside project folder: {fullPath}");
                return string.Empty;
            }

            return fullPath.Substring(projectRoot.Length + 1);
        }
        
        public static string GetRelativePathToBuildShaderFeatureMaterials()
        {
#if UNITY_EDITOR
            var resourcesFolderAssetPath = GetResourcesFolderAssetPath();
            if (string.IsNullOrEmpty(resourcesFolderAssetPath)) return string.Empty;

            return $"{resourcesFolderAssetPath}/GeneratedShaderFeatureMaterials";
#else
            return string.Empty;
#endif
        }
        
        public static string GetAssetsRelativePathToFile(string fileName, string assetName)
        {
#if UNITY_EDITOR
            var searchName = Path.GetFileNameWithoutExtension(fileName);

            var guids = UnityEditor.AssetDatabase.FindAssets(searchName);
            foreach (var guid in guids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var fileOnly = Path.GetFileName(assetPath);
                if (!fileOnly.Equals(fileName, StringComparison.OrdinalIgnoreCase)) continue;

                if (assetPath.IndexOf(assetName, StringComparison.OrdinalIgnoreCase) < 0) continue;

                return assetPath.Replace("\\", "/");
            }

            return string.Empty;            
#else
            return string.Empty;
#endif 
        }
        
        

        public static async Task ReadAsync(this ResourceRequest controller)
        {
            var tcs = new TaskCompletionSource<object>();

            void Handler(AsyncOperation n)
            {
                tcs.TrySetResult(null);
            }

            try
            {
                controller.completed += Handler;
                await tcs.Task;
            }
            finally
            {
                controller.completed -= Handler;
            }
        }
        

        static bool CheckAndCreateDirectory(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (directory == null)
            {
                Debug.LogError("Can't find directory: " + path);
                return false;
            }

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            return true;
        }

        public static T SaveScriptableData<T>(this T data, string waterInstanceID, T profile, string namePrefix) where T : ScriptableObject
        {
#if UNITY_EDITOR
            data.name = profile != null ? profile.name : $"{namePrefix}.{waterInstanceID}";
           
            string defaultPath = "Assets";
            if (profile != null)
            {
                defaultPath = AssetDatabase.GetAssetPath(profile);
                defaultPath = defaultPath == String.Empty ? "Assets" : Path.GetDirectoryName(Path.GetRelativePath("Assets", Path.Combine("Assets", defaultPath)));
            }
           
            var filePath = EditorUtility.SaveFilePanelInProject("Save file location", data.name, "asset", "dfdfgdfg", defaultPath);
            if (filePath == String.Empty)
            {
                return profile;
            }
            
            var newData = Object.Instantiate(data);
            AssetDatabase.CreateAsset(newData, filePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Water profile file saved to " + filePath);
            return newData;

#else
            Debug.LogError("You can't save waves data in runtime");
            return default;
#endif
        }

        static void CheckAndRemoveOldFile(string pathWithExtension)
        {
            if (File.Exists(pathWithExtension)) File.Delete(pathWithExtension);
        }

        public static void SaveBinaryData<T>(T[] data, string pathToFileWithoutExtension) where T : struct
        {
            if (!CheckAndCreateDirectory(pathToFileWithoutExtension)) return;
            var fullPath = pathToFileWithoutExtension + ".bytes";
            CheckAndRemoveOldFile(fullPath);
            try
            {
                var byteArray = new byte[data.Length * Marshal.SizeOf(default(T))];
                Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);
                File.WriteAllBytes(fullPath, byteArray);
            }
            catch (Exception e)
            {
                Debug.LogError("SaveDataToFile error: " + e.Message);
                return;
            }
        }

        public static bool IsLayerRendered(this Camera cam, int layer)
        {
            return (cam.cullingMask & (1 << layer)) != 0;
        }

        public static void WeldVertices(ref List<Vector3> verts, ref List<int> tris)
        {
            WeldVertices(ref verts, ref tris, null);
        }

        public static void WeldVertices(ref List<Vector3> verts, ref List<int> tris, ref List<Color> colors, ref List<Vector3> normals)
        {
            var data = new WeldData {colors = colors, normals = normals};
            WeldVertices(ref verts, ref tris, data);
            colors = data.colors;
            normals = data.normals;
        }

        public static void WeldVertices(ref List<Vector3> verts, ref List<int> tris, ref List<Color> colors, ref List<Vector3> normals, ref List<float> uv)
        {
            var data = new WeldData { colors = colors, normals = normals, uvFloat = uv};
            WeldVertices(ref verts, ref tris, data);
            colors  = data.colors;
            normals = data.normals;
            uv = data.uvFloat;
        }

        public static void WeldVertices(ref List<Vector3> verts, ref List<int> tris, ref List<Color> colors, ref List<Vector3> normals, ref List<Vector2> uv)
        {
            var data = new WeldData { colors = colors, normals = normals, uvVector2 = uv };
            WeldVertices(ref verts, ref tris, data);
            colors  = data.colors;
            normals = data.normals;
            uv      = data.uvVector2;
        }

        class WeldData
        {
            public List<Color> colors; 
            public List<Vector3> normals;
            public List<float> uvFloat;
            public List<Vector2> uvVector2;
            public List<Vector3> uvVector3;
            public List<Vector4> uvVector4;
        }

        static void WeldVertices(ref List<Vector3> verts, ref List<int> tris, WeldData weldData)
        {
            var vertsCount = verts.Count;

            var duplicateHashTable = new Dictionary<Vector3, int>();
            var map = new int[vertsCount];
            List<int> hashVerts = new List<int>();

            for (int i = 0; i < vertsCount; i++)
            {
                if (!duplicateHashTable.ContainsKey(verts[i]))
                {
                    duplicateHashTable.Add(verts[i], hashVerts.Count);
                    map[i] = hashVerts.Count;
                    hashVerts.Add(i);
                }
                else
                {
                    map[i] = duplicateHashTable[verts[i]];
                }
            }

            // create new vertices
            var newVerts = new List<Vector3>();
            var newColors = new List<Color>();
            var newNormals = new List<Vector3>();
            var newUvFloats = new List<float>();
            var newUvVector2 = new List<Vector2>();
            var newUvVector3 = new List<Vector3>();
            var newUvVector4 = new List<Vector4>();

            for (int i = 0; i < hashVerts.Count; i++)
            {
                newVerts.Add(verts[hashVerts[i]]);
            }

            if (weldData.colors != null)
            {
                for (int i = 0; i < hashVerts.Count; i++) newColors.Add(weldData.colors[hashVerts[i]]);
            }
            if (weldData.normals != null)
            {
                for (int i = 0; i < hashVerts.Count; i++) newNormals.Add(weldData.normals[hashVerts[i]]);
            }

            if (weldData.uvFloat != null)
            {
                for (int i = 0; i < hashVerts.Count; i++) newUvFloats.Add(weldData.uvFloat[hashVerts[i]]);
            }

            if (weldData.uvVector2 != null)
            {
                for (int i = 0; i < hashVerts.Count; i++) newUvVector2.Add(weldData.uvVector2[hashVerts[i]]);
            }

            if (weldData.uvVector3 != null)
            {
                for (int i = 0; i < hashVerts.Count; i++) newUvVector3.Add(weldData.uvVector3[hashVerts[i]]);
            }


            if (weldData.uvVector4 != null)
            {
                for (int i = 0; i < hashVerts.Count; i++) newUvVector4.Add(weldData.uvVector4[hashVerts[i]]);
            }


            var trisCount = tris.Count;
            for (int i = 0; i < trisCount; i++)
            {
                tris[i] = map[tris[i]];
            }

            verts = newVerts;

            weldData.colors = newColors;
            weldData.normals = newNormals;
            weldData.uvFloat = newUvFloats;
            weldData.uvVector2 = newUvVector2;
            weldData.uvVector3 = newUvVector3;
            weldData.uvVector4 = newUvVector4;
        }

        public static void RemoveUnusedVertices(ref Vector3[] vertices, ref Vector2[] uvs, ref int[] triangles)
        {
            var used        = new HashSet<int>(triangles);
            var oldToNew    = new Dictionary<int, int>();
            var newVertices = new List<Vector3>();
            var newUvs      = new List<Vector2>();

            int newIndex = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (used.Contains(i))
                {
                    oldToNew[i] = newIndex++;
                    newVertices.Add(vertices[i]);
                    if (uvs != null && uvs.Length > i)
                        newUvs.Add(uvs[i]);
                }
            }

            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = oldToNew[triangles[i]];
            }

            vertices = newVertices.ToArray();
            uvs      = newUvs.ToArray();
        }
        
        public static Vector3 BlendNormals(Vector3 n1, Vector3 n2)
        {
            return new Vector3(n1.x + n2.x, n1.y * n2.y, n1.z + n2.z).normalized;
        }

        public static void CalculateNearPlaneWorldPoints(Camera cam, ref Vector3[] nearPlaneWorldPoints)
        {
            nearPlaneWorldPoints[0] = ViewportToWorldPoint(cam, new Vector3(0, 0, cam.nearClipPlane));
            nearPlaneWorldPoints[1] = ViewportToWorldPoint(cam, new Vector3(1, 0, cam.nearClipPlane));
            nearPlaneWorldPoints[2] = ViewportToWorldPoint(cam, new Vector3(0, 1, cam.nearClipPlane));
            nearPlaneWorldPoints[3] = ViewportToWorldPoint(cam, new Vector3(1, 1, cam.nearClipPlane));
            //_nearPlaneWorldPoints[4] = ViewportToWorldPoint(cam, new Vector3(0.5f, 0.5f, cam.nearClipPlane));
            
        }


        public static bool IsCameraNearPlaneInsideAABB(Vector3[] nearPlaneWorldPoints, Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            return (IsPointInsideAABB(nearPlaneWorldPoints[0], min, max)
                 || IsPointInsideAABB(nearPlaneWorldPoints[1], min, max)
                 || IsPointInsideAABB(nearPlaneWorldPoints[2], min, max)
                 || IsPointInsideAABB(nearPlaneWorldPoints[3], min, max)
                 || IsPointInsideAABB(nearPlaneWorldPoints[4], min, max));
        }

        public static bool IsCameraNearPlaneFullyInsideAABB(Vector3[] nearPlaneWorldPoints, Bounds bounds)
        {
            var min = bounds.min;
            var max = bounds.max;
            return (IsPointInsideAABB(nearPlaneWorldPoints[0], min, max)
                 && IsPointInsideAABB(nearPlaneWorldPoints[1], min, max)
                 && IsPointInsideAABB(nearPlaneWorldPoints[2], min, max)
                 && IsPointInsideAABB(nearPlaneWorldPoints[3], min, max)
                 && IsPointInsideAABB(nearPlaneWorldPoints[4], min, max));
        }

        public static bool IsPointInsideAABB(Vector3 point, Vector3 min, Vector3 max)
        {
            return (point.x >= min.x && point.x <= max.x) &&
                   (point.y >= min.y && point.y <= max.y) &&
                   (point.z >= min.z && point.z <= max.z);
        }

        public static Vector3 ClosestPointOnAABB(Vector3 point, Vector3 min, Vector3 max)
        {
            return new Vector3(
                Mathf.Clamp(point.x, min.x, max.x),
                Mathf.Clamp(point.y, min.y, max.y),
                Mathf.Clamp(point.z, min.z, max.z)
            );
        }
        public static float DistanceToAABB(Vector3 cameraPos, Vector3 min, Vector3 max)
        {
            Vector3 closestPoint = ClosestPointOnAABB(cameraPos, min, max);
            return (cameraPos - closestPoint).magnitude;
        }

        //public static Plane[] CalculateFrustumPlanes(ref Plane[] planes, Camera cam, WaterSystem.WaterMeshTypeEnum meshType, float maxWavesHeight)
        //{
        //    //var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        //    GeometryUtility.CalculateFrustumPlanes(cam, planes);
        //    if (meshType == WaterSystem.WaterMeshTypeEnum.InfiniteOcean)
        //    {
        //        planes[0].distance += KWS_Settings.Water.UpdateQuadtreeEveryMetersForward;
        //        planes[1].distance += KWS_Settings.Water.UpdateQuadtreeEveryMetersForward;
        //        planes[2].distance += KWS_Settings.Water.UpdateQuadtreeEveryMetersForward;
        //    }
        //    else if (meshType == WaterSystem.WaterMeshTypeEnum.FiniteBox)
        //    {
        //        planes[0].distance += maxWavesHeight;
        //        planes[1].distance += maxWavesHeight;
        //    }

        //    return planes;
        //}

        public static void CalculateFrustumCorners(ref Vector3[] corners, Camera cam)
        {
            // CORNERS:
            // [0] = Far Bottom Left,  [1] = Far Top Left,  [2] = Far Top Right,  [3] = Far Bottom Right, 
            // [4] = Near Bottom Left, [5] = Near Top Left, [6] = Near Top Right, [7] = Near Bottom Right

            if (cam.orthographic) CalculateFrustumCornersOrthographic(ref corners, cam);
            else CalculateFrustumCornersPerspective(ref corners, cam);
        }

        static void CalculateFrustumCornersOrthographic(ref Vector3[] corners, Camera cam)
        {
            var camTransform = cam.transform;
            var position = camTransform.position;
            var orientation = camTransform.rotation;
            var farClipPlane = cam.farClipPlane;
            var nearClipPlane = cam.nearClipPlane;

            var forward = orientation * Vector3.forward;
            var right = orientation * Vector3.right * cam.orthographicSize * cam.aspect;
            var up = orientation * Vector3.up * cam.orthographicSize;

            corners[0] = position + forward * farClipPlane - up - right;
            corners[1] = position + forward * farClipPlane + up - right;
            corners[2] = position + forward * farClipPlane + up + right;
            corners[3] = position + forward * farClipPlane - up + right;
            corners[4] = position + forward * nearClipPlane - up - right;
            corners[5] = position + forward * nearClipPlane + up - right;
            corners[6] = position + forward * nearClipPlane + up + right;
            corners[7] = position + forward * nearClipPlane - up + right;
        }

        static void CalculateFrustumCornersPerspective(ref Vector3[] corners, Camera cam)
        {
            var position = cam.transform.position;
            var rotation = cam.transform.rotation;
            
            float fovWHalf = cam.fieldOfView * 0.5f;

            Vector3 toRight = Vector3.right * Mathf.Tan(fovWHalf * Mathf.Deg2Rad) * cam.aspect;
            Vector3 toTop = Vector3.up * Mathf.Tan(fovWHalf * Mathf.Deg2Rad);
            var forward = Vector3.forward;

            Vector3 topLeft = (forward - toRight + toTop);
            float camScale = topLeft.magnitude * cam.farClipPlane;

            topLeft.Normalize();
            topLeft *= camScale;

            Vector3 topRight = (forward + toRight + toTop);
            topRight.Normalize();
            topRight *= camScale;

            Vector3 bottomRight = (forward + toRight - toTop);
            bottomRight.Normalize();
            bottomRight *= camScale;

            Vector3 bottomLeft = (forward - toRight - toTop);
            bottomLeft.Normalize();
            bottomLeft *= camScale;

            corners[0] = position + rotation * bottomLeft;
            corners[1] = position + rotation * topLeft;
            corners[2] = position + rotation * topRight;
            corners[3] = position + rotation * bottomRight;

            topLeft = (forward - toRight + toTop);
            camScale = topLeft.magnitude * cam.nearClipPlane;

            topLeft.Normalize();
            topLeft *= camScale;

            topRight = (forward + toRight + toTop);
            topRight.Normalize();
            topRight *= camScale;

            bottomRight = (forward + toRight - toTop);
            bottomRight.Normalize();
            bottomRight *= camScale;

            bottomLeft = (forward - toRight - toTop);
            bottomLeft.Normalize();
            bottomLeft *= camScale;

            corners[4] = position + rotation * bottomLeft;
            corners[5] = position + rotation * topLeft;
            corners[6] = position + rotation * topRight;
            corners[7] = position + rotation * bottomRight;
        }

        public static Vector3 WorldToViewportPoint(Matrix4x4 cameraView, Matrix4x4 cameraProjection, Vector3 point)
        {
            var viewPos   = cameraView       * new Vector4(point.x, point.y, point.z, 1.0f);
            var clip      = cameraProjection * viewPos;
            var w         = Mathf.Max(clip.w, Mathf.Epsilon);
            var viewportX = (clip.x / w + 1.0f) * 0.5f;
            var viewportY = (clip.y / w + 1.0f) * 0.5f;

            return new Vector3(viewportX, viewportY, -viewPos.z);
        }
        public static Vector3 ViewportToWorldPoint(Camera cam, Vector3 point)
        {
            var   f = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * .5f) * point.z;
            var p = new Vector3(f * (point.x * 2 - 1) * cam.aspect, f * (point.y * 2 - 1), point.z);
            p.z = -p.z;
            return cam.cameraToWorldMatrix.MultiplyPoint(p);
        }
        
        
        public static Matrix4x4 GetFittedWorldMatrix(
            Vector3    position,
            Quaternion rotation,
            Vector3    targetSize,
            Mesh       mesh)
        {
            Bounds  b            = mesh.bounds; 
            Vector3 originalSize = b.size;      
            Vector3 scale        = new Vector3(
                targetSize.x / originalSize.x,
                targetSize.y / originalSize.y,
                targetSize.z / originalSize.z
            );

            Matrix4x4 M_world  = Matrix4x4.TRS(position, rotation, Vector3.one);
            Matrix4x4 M_scale  = Matrix4x4.Scale(scale);
            Matrix4x4 M_center = Matrix4x4.Translate(-b.center);
            return M_world * M_scale * M_center;
        }
        

        public static bool IsBoxVisibleApproximated(ref Plane[] planes, Vector3 min, Vector3 max)
        {
            Vector3 vmin, vmax;

            for (int planeIndex = 0; planeIndex < planes.Length; planeIndex++)
            {
                var normal = planes[planeIndex].normal;
                var planeDistance = planes[planeIndex].distance;

                // X axis
                if (normal.x < 0)
                {
                    vmin.x = min.x;
                    vmax.x = max.x;
                }
                else
                {
                    vmin.x = max.x;
                    vmax.x = min.x;
                }

                // Y axis
                if (normal.y < 0)
                {
                    vmin.y = min.y;
                    vmax.y = max.y;
                }
                else
                {
                    vmin.y = max.y;
                    vmax.y = min.y;
                }

                if (normal.z < 0)
                {
                    vmin.z = min.z;
                    vmax.z = max.z;
                }
                else
                {
                    vmin.z = max.z;
                    vmax.z = min.z;
                }

                var dot1 = normal.x * vmin.x + normal.y * vmin.y + normal.z * vmin.z;
                if (dot1 + planeDistance < 0)
                    return false;

            }

            return true;
        }

        public static bool IsBoxVisibleAccurate(ref Plane[] planes, ref Vector3[] corners, Vector3 min, Vector3 max)
        {
            if (!IsBoxVisibleApproximated(ref planes, min, max)) return false;

            int isOutFrustum = 0;
            for (int i = 0; i < 8; i++) isOutFrustum += ((corners[i].x > max.x) ? 1 : 0);
            if (isOutFrustum == 8) return false;
            isOutFrustum = 0;
            for (int i = 0; i < 8; i++) isOutFrustum += ((corners[i].x < min.x) ? 1 : 0);
            if (isOutFrustum == 8) return false;
            isOutFrustum = 0;
            for (int i = 0; i < 8; i++) isOutFrustum += ((corners[i].y > max.y) ? 1 : 0);
            if (isOutFrustum == 8) return false;
            isOutFrustum = 0;
            for (int i = 0; i < 8; i++) isOutFrustum += ((corners[i].y < min.y) ? 1 : 0);
            if (isOutFrustum == 8) return false;
            isOutFrustum = 0;
            for (int i = 0; i < 8; i++) isOutFrustum += ((corners[i].z > max.z) ? 1 : 0);
            if (isOutFrustum == 8) return false;
            isOutFrustum = 0;
            for (int i = 0; i < 8; i++) isOutFrustum += ((corners[i].z < min.z) ? 1 : 0);
            if (isOutFrustum == 8) return false;

            return true;
        }

        static Vector2[] _corners = new Vector2[8];

        public static Rect GetScreenSpaceBounds2(Camera camera, Bounds aabb)
        {
            Vector3[] corners = new Vector3[8];
            corners[0] = camera.WorldToScreenPoint(aabb.min);
            corners[1] = camera.WorldToScreenPoint(new Vector3(aabb.max.x, aabb.min.y, aabb.min.z));
            corners[2] = camera.WorldToScreenPoint(new Vector3(aabb.min.x, aabb.max.y, aabb.min.z));
            corners[3] = camera.WorldToScreenPoint(new Vector3(aabb.max.x, aabb.max.y, aabb.min.z));
            corners[4] = camera.WorldToScreenPoint(new Vector3(aabb.min.x, aabb.min.y, aabb.max.z));
            corners[5] = camera.WorldToScreenPoint(new Vector3(aabb.max.x, aabb.min.y, aabb.max.z));
            corners[6] = camera.WorldToScreenPoint(new Vector3(aabb.min.x, aabb.max.y, aabb.max.z));
            corners[7] = camera.WorldToScreenPoint(aabb.max);

            float minX = Mathf.Infinity;
            float maxX = Mathf.NegativeInfinity;
            float minY = Mathf.Infinity;
            float maxY = Mathf.NegativeInfinity;

            foreach (Vector3 corner in corners)
            {
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }

           
            Rect screenSpaceRect = new Rect(minX, Screen.height - maxY, maxX - minX, maxY - minY);
            return screenSpaceRect;
        }

        public static Rect GetScreenSpaceBounds(Camera cam, Bounds bounds)
        {
            var posX = bounds.center.x + bounds.extents.x;
            var negX = bounds.center.x - bounds.extents.x;
            var posY = bounds.center.y + bounds.extents.y;
            var negY = bounds.center.y - bounds.extents.y;
            var posZ = bounds.center.z + bounds.extents.z;
            var negZ = bounds.center.z - bounds.extents.z;

            _corners[0] = cam.WorldToScreenPoint(new Vector3(posX, posY, posZ));
            _corners[1] = cam.WorldToScreenPoint(new Vector3(posX, posY, negZ));
            _corners[2] = cam.WorldToScreenPoint(new Vector3(posX, negY, posZ));
            _corners[3] = cam.WorldToScreenPoint(new Vector3(posX, negY, negZ));
            _corners[4] = cam.WorldToScreenPoint(new Vector3(negX, posY, posZ));
            _corners[5] = cam.WorldToScreenPoint(new Vector3(negX, posY, negZ));
            _corners[6] = cam.WorldToScreenPoint(new Vector3(negX, negY, posZ));
            _corners[7] = cam.WorldToScreenPoint(new Vector3(negX, negY, negZ));

            var screenSize = new Vector2(cam.scaledPixelWidth, cam.scaledPixelHeight);
            for (int i = 0; i < 8; i++)
            {
                _corners[i].y = screenSize.y - _corners[i].y;
                _corners[i] = ClampVector2(_corners[i], Vector2.zero, screenSize);
            }

            float min_x = _corners[0].x;
            float min_y = _corners[0].y;
            float max_x = _corners[0].x;
            float max_y = _corners[0].y;

            for (int i = 1; i < 8; i++)
            {
                if (_corners[i].x < min_x)
                {
                    min_x = _corners[i].x;
                }
                if (_corners[i].y < min_y)
                {
                    min_y = _corners[i].y;
                }
                if (_corners[i].x > max_x)
                {
                    max_x = _corners[i].x;
                }
                if (_corners[i].y > max_y)
                {
                    max_y = _corners[i].y;
                }
            }

            return new Rect(min_x, screenSize.y - max_y, max_x - min_x, max_y - min_y);

        }
      
        public static Vector3 ClampVector3(Vector3 current, Vector3 min, Vector3 max)
        {
            current.x = Mathf.Clamp(current.x, min.x, max.x);
            current.y = Mathf.Clamp(current.y, min.y, max.y);
            current.z = Mathf.Clamp(current.z, min.z, max.z);
            return current;
        }
        public static Vector2 ClampVector2(Vector2 current, Vector2 min, Vector2 max)
        {
            current.x = Mathf.Clamp(current.x, min.x, max.x);
            current.y = Mathf.Clamp(current.y, min.y, max.y);
            return current;
        }

        public static float DistanceXZ(this Vector3 a, Vector3 b)
        {
            float num1 = a.x - b.x;
            float num3 = a.z - b.z;
            return (float)Math.Sqrt((double)num1 * (double)num1 + (double)num3 * (double)num3);
        }

        public static Vector2Int ScaleIntVector(this Vector2Int vector, Vector2 scale)
        {
            return new Vector2Int((int)(vector.x * scale.x), (int)(vector.y * scale.y));
        }

        public static Vector3 RotateVectorXZ(Vector3 point, float sinAngle, float cosAngle)
        {
            var newVector = point;
            newVector.x = point.x * cosAngle - point.z * sinAngle;
            newVector.z = point.x * sinAngle + point.z * cosAngle;
            return newVector;
        }
        
        public static Vector4 Inverse(this Vector4 v)
        {
            return new Vector4(1.0f / v.x, 1.0f / v.y, 1.0f / v.z, 1.0f / v.w);
        }
        
        public static bool AproximatedEqual(Vector3 me, Vector3 other, float allowedDifference = 0.00001f)
        {
            var dx = me.x - other.x;
            if (Mathf.Abs(dx) > allowedDifference) return false;

            var dy = me.y - other.y;
            if (Mathf.Abs(dy) > allowedDifference) return false;

            var dz = me.z - other.z;
            if(Mathf.Abs(dz) > allowedDifference) return false;

            return true;
        }
        
        public static bool AproximatedEqual(Quaternion a, Quaternion b, float allowedDotThreshold = 0.99999f)
        {
            return Mathf.Abs(Quaternion.Dot(a, b)) > allowedDotThreshold;
        }

        public static float SqrMagnitudeFast(Vector3 vector)
        {
            return (vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
            
        }
        
        public static float Magnitude2(Vector3 vector)
        {
            return Mathf.Sqrt(vector.x * vector.x + vector.z * vector.z);
            
        }

        public static void ScaleAround(ref Vector3 pos, ref Vector3 size, Vector3 pivot, Vector3 newScale)
        {
            var pivotDelta = pos - pivot; 
            var scaleFactor = new Vector3(newScale.x / size.x,
                                          newScale.y / size.y,
                                          newScale.z / size.z);
            pivotDelta.Scale(scaleFactor);
           
            pos = pivot + pivotDelta;
            size = newScale;
        }
    
        public static bool IsAABBIntersectSphere(Bounds bounds, Vector3 center, float radius)
        {
            Vector3 min = bounds.center - bounds.size * 0.5f;
            Vector3 max = bounds.center + bounds.size * 0.5f;

            double ex = Math.Max(min.x - center.x, 0) + Math.Max(center.x - max.x, 0);
            double ey = Math.Max(min.y - center.y, 0) + Math.Max(center.y - max.y, 0);
            double ez = Math.Max(min.z - center.z, 0) + Math.Max(center.z - max.z, 0);

            return (ex < radius) && (ey < radius) && (ez < radius) && (ex * ex + ey * ey + ez * ez < radius * radius);
        }

        public static bool IsAABBIntersectSphereOptimized(Bounds bounds, Vector3 center, float radius)
        {
            var closestPointInAABB = new Vector3(
                Mathf.Clamp(center.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(center.y, bounds.min.y, bounds.max.y),
                Mathf.Clamp(center.z, bounds.min.z, bounds.max.z)
            );

            float distanceSquared = (center - closestPointInAABB).sqrMagnitude;
            return distanceSquared < (radius * radius);
        }


        public static float BoundsToSphereRadius(Bounds bounds, Vector3 scale)
        {
            var halfSize = Vector3.Scale(bounds.size * 0.5f, scale);
            return halfSize.magnitude;
        }


        public static Bounds BoundsLocalToWorld(Bounds localBounds, Transform transform, float pivotOffset, float amplitudeOffset)
        {
            var center = transform.TransformPoint(localBounds.center + new Vector3(0, pivotOffset, 0));

            // transform the local extents' axes
            var extents = localBounds.extents;
            var axisX = transform.TransformVector(extents.x, 0, 0);
            var axisY = transform.TransformVector(0, extents.y, 0);
            var axisZ = transform.TransformVector(0, 0, extents.z);

            // sum their absolute value to get the world extents
            extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
            extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
            extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

            center.y += amplitudeOffset * 0.5f;
            extents.y += amplitudeOffset;
            return new Bounds { center = center, extents = extents };

        }

        public static Bounds GetOrientedBounds(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3[] localOffsets =
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, 0.5f),

                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3( 0.5f, 0.5f, -0.5f),
                new Vector3( 0.5f, 0.5f, 0.5f),
            };

            Bounds bounds = new Bounds(center, Vector3.zero);
            for (int i = 0; i < localOffsets.Length; i++)
            {
                Vector3 worldCorner = center + rotation * Vector3.Scale(localOffsets[i], size);
                bounds.Encapsulate(worldCorner);
            }

            return bounds;
        }
        
        public static bool BoundsIntersectsLod(Bounds zoneBounds, Vector3 camPos, float lodSize)
        {
            var center    = new Vector3(camPos.x, zoneBounds.center.y, camPos.z);
            var lodBounds = new Bounds(center, new Vector3(lodSize, float.MaxValue, lodSize));

            if (!zoneBounds.Intersects(lodBounds)) return false;
            
            var   min     = zoneBounds.min;
            var   max     = zoneBounds.max;
            var   zoneY   = zoneBounds.center.y;
            
            float maxDist = 0f;

            maxDist = Mathf.Max(maxDist, Vector3.Distance(camPos, new Vector3(min.x, zoneY, min.z)));
            maxDist = Mathf.Max(maxDist, Vector2.Distance(camPos, new Vector3(min.x, zoneY, max.z)));
            maxDist = Mathf.Max(maxDist, Vector2.Distance(camPos, new Vector3(max.x, zoneY, min.z)));
            maxDist = Mathf.Max(maxDist, Vector2.Distance(camPos, new Vector3(max.x, zoneY, max.z)));

            if (lodSize * 0.5f > maxDist * 2f) return false;

            return true;
        }
        
        public static bool ContainsXZ(this Bounds bounds, Vector3 point)
        {
            var min       = bounds.min;
            var max       = bounds.max;
            return (point.x >= min.x && point.x <= max.x && point.z >= min.z && point.z <= max.z);
        }

        public static List<T> InitializeListWithDefaultValues<T>(int count, T defaultValue)
        {
            var list = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(defaultValue);
            }

            return list;
        }

        public static void AddDefaultValues<T>(this List<T> list, int count, T defaultValue)
        {
            for (int i = 0; i < count; i++)
            {
                list.Add(defaultValue);
            }
        }

        public static void Swap<T>(this List<T> list, int i, int j)
        {
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        public interface ICacheCamera
        {
            public void Release();
        }

        public static T GetCameraCache<T>(this Dictionary<Camera, T> dictionary, Camera newCamera, int maxElemets = 5) where T : class, ICacheCamera, new()
        {
            if (dictionary.Count > maxElemets) {
                foreach (var dict in dictionary)
                {
                    dict.Value.Release();
                }
                dictionary.Clear();
            }

            if (!dictionary.ContainsKey(newCamera)) dictionary.Add(newCamera, new T());
            return dictionary[newCamera];
        }

        public static void ReleaseCameraCache<T>(this Dictionary<Camera, T> dictionary) where T : class, ICacheCamera, new()
        {
            foreach (var data in dictionary)
            {
                data.Value?.Release();
            }
            dictionary.Clear();
        }

        [Flags]
        public enum WaterLogMessageType
        {
            Info = 1,
            Release = 2,
            ReleaseRT = 4,
            Initialize = 8,
            InitializeRT = 16,
            Error = 32,
            DynamicUpdate = 64,
            StaticUpdate = 128,
            All = ~0
        }

        private static WaterLogMessageType _debugFlags = WaterLogMessageType.Error;
        //private static WaterLogMessageType _debugFlags = WaterLogMessageType.Error | WaterLogMessageType.Initialize | WaterLogMessageType.InitializeRT | WaterLogMessageType.Release;
        //private static WaterLogMessageType _debugFlags = WaterLogMessageType.Error | WaterLogMessageType.Initialize;


        [Conditional("KWS_DEBUG")]
        public static void WaterLog(this object sender, string message, WaterLogMessageType logType = WaterLogMessageType.Info)
        {

            switch (logType)
            {
                case WaterLogMessageType.Info:
                    if (_debugFlags.HasFlag(WaterLogMessageType.Info))
                        Debug.Log($"<color=#ebebeb>{message}</color> : <color=#bababa>{sender} </color>");
                    break;
                case WaterLogMessageType.Release:
                    if (_debugFlags.HasFlag(WaterLogMessageType.Release))
                        Debug.Log($"<color=#86c6db>Release: {message}</color> : <color=#bababa>{sender} </color>");
                    break;
                case WaterLogMessageType.ReleaseRT:
                    if (_debugFlags.HasFlag(WaterLogMessageType.Release))
                        Debug.Log($"<color=#86c6db>ReleaseRT: {message}</color> : <color=#bababa>{sender} </color>");
                    break;
                case WaterLogMessageType.Initialize:
                    if (_debugFlags.HasFlag(WaterLogMessageType.Initialize))
                        Debug.Log($"<color=#70fa75>{message}</color> : <color=#bababa>{sender} </color>");
                    break;
                case WaterLogMessageType.InitializeRT:
                    if (_debugFlags.HasFlag(WaterLogMessageType.InitializeRT))
                        Debug.Log($"<color=#86eb90>InitializeRT: </color>  <color=#bababa>{sender}</color>  <color=#a4f5ba> {message} </color>");
                    break;
                case WaterLogMessageType.Error:
                    if (_debugFlags.HasFlag(WaterLogMessageType.Error))
                        Debug.Log($"<color=#ff0000>{message}</color> : <color=#bababa>{sender} </color>");
                    break;
                case WaterLogMessageType.DynamicUpdate:
                    if (_debugFlags.HasFlag(WaterLogMessageType.DynamicUpdate))
                        Debug.Log($"<color=#ffca69>{message}</color> : <color=#bababa>{sender} </color>");
                    break;
                case WaterLogMessageType.StaticUpdate:
                    if (_debugFlags.HasFlag(WaterLogMessageType.StaticUpdate))
                        Debug.Log($"<color=#36d0ff>{message}</color> : <color=#bababa>{sender} </color>");
                    break;
                default:
                    Debug.Log($"<color=#ebebeb>{message}</color> : <color=#bababa>{sender} </color>");
                    break;
            }
        }
        
        [Conditional("KWS_DEBUG")]
        public static void WaterLog(string message)
        {
            Debug.Log($"<color=#ebebeb>{message}");
        }

        [Conditional("KWS_DEBUG")]
        public static void WaterLog(this object sender, params RenderTexture[] renderTextures)
        {
            var fullMessage = "";
            foreach (var rt in renderTextures)
            {
                if (rt != null) fullMessage += $"{rt.name} ({rt.width}x{rt.height}, {rt.format})  ";
            }

            WaterLog(sender, fullMessage, WaterLogMessageType.InitializeRT);
        }

        [Conditional("KWS_DEBUG")]
        public static void WaterLog(this object sender, params RTHandle[] renderTextures)
        {
            var fullMessage = "";
            foreach (var handle in renderTextures)
            {
                if (handle != null && handle.rt != null)
                {
                    if(handle.rt.volumeDepth == 1) fullMessage += $"{handle.name} ({handle.rt.width}x{handle.rt.height}, {handle.rt.format})  ";
                    else fullMessage += $"{handle.name} ({handle.rt.width}x{handle.rt.height}, {handle.rt.format}, slices:{handle.rt.volumeDepth})  ";
                }
            }

            WaterLog(sender, fullMessage, WaterLogMessageType.InitializeRT);
        }


        public static string SpaceBetweenThousand(int number)
        {
            return string.Format("{0:#,0}", number);
        }
    }
}