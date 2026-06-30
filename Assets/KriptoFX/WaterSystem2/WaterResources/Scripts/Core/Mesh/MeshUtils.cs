using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static KWS.KWS_ShaderConstants;
using Random = UnityEngine.Random;

namespace KWS
{
    internal static class MeshUtils
    {
        static uint[]                                   _args                    = new uint[5] { 0, 0, 0, 0, 0 };
        static GraphicsBuffer.IndirectDrawIndexedArgs[] _indirectDrawIndexedArgs = new GraphicsBuffer.IndirectDrawIndexedArgs[1];

        public static ComputeBuffer InitializeInstanceArgsBuffer(Mesh instancedMesh, int instanceCount, ComputeBuffer argsBuffer, bool isStereo)
        {
            if (instancedMesh == null) return argsBuffer;

            if (isStereo) instanceCount *= 2;

            // Arguments for drawing mesh.
            // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
            _args[0] = instancedMesh.GetIndexCount(0);
            _args[1] = (uint)instanceCount;
            _args[2] = instancedMesh.GetIndexStart(0);
            _args[3] = instancedMesh.GetBaseVertex(0);
            if (argsBuffer == null) argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(_args);
            return argsBuffer;
        }

        public static GraphicsBuffer InitializeInstanceArgsGraphicsBuffer(Mesh instancedMesh, int instanceCount, GraphicsBuffer argsBuffer, bool isStereo)
        {
            if (instancedMesh == null) return argsBuffer;
            if (isStereo) instanceCount *= 2;

            _indirectDrawIndexedArgs[0].baseVertexIndex       = instancedMesh.GetBaseVertex(0);
            _indirectDrawIndexedArgs[0].indexCountPerInstance = instancedMesh.GetIndexCount(0);
            _indirectDrawIndexedArgs[0].instanceCount         = (uint)instanceCount;
            _indirectDrawIndexedArgs[0].startIndex            = instancedMesh.GetIndexStart(0);

            if (argsBuffer == null) argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
            argsBuffer.SetData(_indirectDrawIndexedArgs);
            return argsBuffer;
        }

        public static void InitializePropertiesBuffer<T>(List<T> meshProperties, ref ComputeBuffer propertiesBuffer, bool isStereo) where T : struct
        {
            var instanceCount = meshProperties.Count;
            if (instanceCount == 0) return;

            if (isStereo) instanceCount *= 2;

            if (propertiesBuffer == null)
            {
                propertiesBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(T)));
            }
            else if (instanceCount > propertiesBuffer.count)
            {
                propertiesBuffer.Dispose();
                propertiesBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(T)));
                //Debug.Log("ReInitialize PropertiesBuffer");
            }

            if (isStereo)
            {
                var tempArr = new T[meshProperties.Count * 2];
                var idx     = 0;
                for (var i = 0; i < tempArr.Length; i += 2)
                {
                    var meshProperty = meshProperties[idx];
                    tempArr[i]     = meshProperty;
                    tempArr[i + 1] = meshProperty;
                    idx++;
                }

                propertiesBuffer.SetData(tempArr);
            }
            else propertiesBuffer.SetData(meshProperties);
        }

        public static void InitializePropertiesBuffer<T>(CommandBuffer cmd, List<T> meshProperties, ref ComputeBuffer propertiesBuffer, bool isStereo) where T : struct
        {
            var instanceCount = meshProperties.Count;
            if (instanceCount == 0) return;

            if (isStereo) instanceCount *= 2;

            if (propertiesBuffer == null)
            {
                propertiesBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(T)));
            }
            else if (instanceCount > propertiesBuffer.count)
            {
                propertiesBuffer.Dispose();
                propertiesBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(T)));
                //Debug.Log("ReInitialize PropertiesBuffer");
            }

            if (isStereo)
            {
                var tempArr = new T[meshProperties.Count * 2];
                var idx     = 0;
                for (var i = 0; i < tempArr.Length; i += 2)
                {
                    var meshProperty = meshProperties[idx];
                    tempArr[i]     = meshProperty;
                    tempArr[i + 1] = meshProperty;
                    idx++;
                }

                cmd.SetBufferData(propertiesBuffer, tempArr);
            }
            else cmd.SetBufferData(propertiesBuffer, meshProperties);
        }


        public static Mesh GenerateInstanceMesh(Vector2Int meshResolution)
        {
            var vertices  = KW_Extensions.InitializeListWithDefaultValues((meshResolution.x + 1) * (meshResolution.y + 1), Vector3.zero);
            var uv        = KW_Extensions.InitializeListWithDefaultValues(vertices.Count,                                  Vector2.zero);
            var colors    = KW_Extensions.InitializeListWithDefaultValues(vertices.Count,                                  Color.white);
            var normals   = KW_Extensions.InitializeListWithDefaultValues(vertices.Count,                                  Vector3.up);
            var triangles = KW_Extensions.InitializeListWithDefaultValues(meshResolution.x * meshResolution.y * 6,         0);

            var quadOffset = new Vector2(1f / meshResolution.x, 1f / meshResolution.y);

            for (int i = 0, y = 0; y <= meshResolution.y; y++)
            for (var x = 0; x <= meshResolution.x; x++, i++)
            {
                vertices[i] = new Vector3((float)x / meshResolution.x - 0.5f, 0, (float)y / meshResolution.y - 0.5f);

                //uv used as mask for seam vertexes, 0.1 = down, 0.2 = left, 0.3 = top, 0.4 = right,
                //pattern for down looks like that
                //  □---------□---------□           □---------□---------□
                //  |      ∕  |      ∕  |           |      ∕  |      ∕  |
                //  |    ∕    |    ∕    |           |    ∕    |    ∕    |
                //  |  ∕      |  ∕      |           |  ∕      |  ∕      |
                //  □---------□---------□     ->    □---------□---------□  
                //  |      ∕  |      ∕  |           |      ∕     \      |
                //  |    ∕    |    ∕    |           |    ∕         \    |
                //  |  ∕      |  ∕      |           |  ∕             \  |
                //  □---------■---------□           □---------□---->----■
                //  1.0      0.1       1.0         1.0       1.0       0.1
                uint  flag   = 0;
                float offset = 0;

                if (y == 0 && x % 2 == 1)
                {
                    flag   = SetFlag(flag, 1);
                    offset = quadOffset.x;
                }

                if (x == 0 && y % 2 == 1)
                {
                    flag   = SetFlag(flag, 2);
                    offset = quadOffset.y;
                }

                if (y == meshResolution.y && x % 2 == 1)
                {
                    flag   = SetFlag(flag, 3);
                    offset = quadOffset.x;
                }

                if (x == meshResolution.x && y % 2 == 1)
                {
                    flag   = SetFlag(flag, 4);
                    offset = quadOffset.y;
                }

                if (y == 0) flag                = SetFlag(flag, 5);
                if (x == 0) flag                = SetFlag(flag, 6);
                if (y == meshResolution.y) flag = SetFlag(flag, 7);
                if (x == meshResolution.x) flag = SetFlag(flag, 8);

                uv[i] = new Vector2(flag, offset);
            }

            for (int ti = 0, vi = 0, y = 0; y < meshResolution.y; y++, vi++)
            for (var x = 0; x < meshResolution.x; x++, ti += 6, vi++)
            {
                triangles[ti]     = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + meshResolution.x + 1;
                triangles[ti + 5] = vi + meshResolution.x + 2;
            }

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);

            return mesh;
        }

        static uint SetFlag(uint data, int bit)
        {
            return data | 1u << bit;
        }


        static void CreatePlane(Vector2Int res, List<Vector3> vertices, List<Vector3> normals, List<Color> colors, List<int> triangles)
        {
            var lastTrisIdx = triangles.Count;
            var lastVertIdx = vertices.Count;

            for (int y = 0; y <= res.y; y++)
            for (var x = 0; x <= res.x; x++)
            {
                vertices.Add(new Vector3((float)x / res.x - 0.5f, -1.0f, (float)y / res.y - 0.5f));
                normals.Add(Vector3.down);
                colors.Add(Color.black);
            }


            var trisCounts = res.x * res.y * 6;
            for (int i = 0; i < trisCounts; i++) triangles.Add(0);

            for (int y = 0; y < res.y; y++)
            {
                for (int x = 0; x < res.x; x++)
                {
                    int ti = lastTrisIdx + (y * (res.y) + x) * 6;
                    triangles[ti + 0] = lastVertIdx + (y * (res.x + 1))               + x;
                    triangles[ti + 2] = lastVertIdx + ((y         + 1) * (res.x + 1)) + x;
                    triangles[ti + 1] = lastVertIdx + ((y         + 1) * (res.x + 1)) + x + 1;

                    triangles[ti + 3] = lastVertIdx + (y * (res.x + 1))               + x;
                    triangles[ti + 5] = lastVertIdx + ((y         + 1) * (res.x + 1)) + x + 1;
                    triangles[ti + 4] = lastVertIdx + (y               * (res.x + 1)) + x + 1;
                }
            }
        }

        public static Mesh CreatePlaneXZMesh(int meshResolution, float scale)
        {
            var vertices  = new Vector3[(meshResolution + 1) * (meshResolution + 1)];
            var uv        = new Vector2[vertices.Length];
            var triangles = new int[meshResolution * meshResolution * 6];
            var normals   = new Vector3[vertices.Length];

            for (int i = 0, y = 0; y <= meshResolution; y++)
            for (var x = 0; x <= meshResolution; x++, i++)
            {
                vertices[i] = new Vector3(x * scale / meshResolution - 0.5f     * scale, 0, y * scale / meshResolution - 0.5f * scale);
                uv[i]       = new Vector2(x         * scale / meshResolution, y * scale / meshResolution);
                normals[i]  = new Vector3(0, 1, 0);
            }

            for (int ti = 0, vi = 0, y = 0; y < meshResolution; y++, vi++)
            for (var x = 0; x < meshResolution; x++, ti += 6, vi++)
            {
                triangles[ti]     = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + meshResolution + 1;
                triangles[ti + 5] = vi + meshResolution + 2;
            }

            var indexFormat = vertices.Length >= 65536 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            var mesh        = new Mesh { hideFlags = HideFlags.DontSave, indexFormat = indexFormat };

            mesh.Clear();
            mesh.vertices  = vertices;
            mesh.uv        = uv;
            mesh.triangles = triangles;
            mesh.normals   = normals;
            return mesh;
        }

        public static Mesh CreatePlaneMesh(int meshResolution, float scale)
        {
            var vertices  = new Vector3[(meshResolution + 1) * (meshResolution + 1)];
            var uv        = new Vector2[vertices.Length];
            var triangles = new int[meshResolution * meshResolution * 6];

            for (int i = 0, y = 0; y <= meshResolution; y++)
            for (var x = 0; x <= meshResolution; x++, i++)
            {
                vertices[i] = new Vector3(x * scale / meshResolution - 0.5f     * scale, y * scale / meshResolution - 0.5f * scale, 0);
                uv[i]       = new Vector2(x         * scale / meshResolution, y * scale / meshResolution);
            }

            for (int ti = 0, vi = 0, y = 0; y < meshResolution; y++, vi++)
            for (var x = 0; x < meshResolution; x++, ti += 6, vi++)
            {
                triangles[ti]     = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + meshResolution + 1;
                triangles[ti + 5] = vi + meshResolution + 2;
            }

            var mesh = new Mesh { hideFlags = HideFlags.DontSave, indexFormat = IndexFormat.UInt32 };

            mesh.Clear();
            mesh.vertices  = vertices;
            mesh.uv        = uv;
            mesh.triangles = triangles;
            return mesh;
        }

        public static Mesh CreateCubeMesh(float heightOffset = 0)
        {
            Vector3[] vertices =
            {
                new Vector3(-0.5f, -0.5f + heightOffset, -0.5f),
                new Vector3(0.5f,  -0.5f + heightOffset, -0.5f),
                new Vector3(0.5f,  0.5f  + heightOffset, -0.5f),
                new Vector3(-0.5f, 0.5f  + heightOffset, -0.5f),
                new Vector3(-0.5f, 0.5f  + heightOffset, 0.5f),
                new Vector3(0.5f,  0.5f  + heightOffset, 0.5f),
                new Vector3(0.5f,  -0.5f + heightOffset, 0.5f),
                new Vector3(-0.5f, -0.5f + heightOffset, 0.5f),
            };

            int[] triangles =
            {
                0, 2, 1, //face front
                0, 3, 2,
                2, 3, 4, //face top
                2, 4, 5,
                1, 2, 5, //face right
                1, 5, 6,
                0, 7, 4, //face left
                0, 4, 3,
                5, 4, 7, //face back
                5, 7, 6,
                0, 6, 7, //face bottom
                0, 1, 6
            };

            var cubeMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };

            cubeMesh.Clear();
            cubeMesh.vertices  = vertices;
            cubeMesh.triangles = triangles;
            cubeMesh.RecalculateNormals();

            return cubeMesh;
        }

        public static Mesh CreateSphereMesh(float radius, int longitudeSegments, int latitudeSegments)
        {
            Mesh mesh = new Mesh();

            int       vertexCount = (longitudeSegments + 1) * (latitudeSegments + 1);
            Vector3[] vertices    = new Vector3[vertexCount];
            Vector2[] uv          = new Vector2[vertexCount];
            int[]     triangles   = new int[longitudeSegments * latitudeSegments * 6];

            int vertIndex = 0;
            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                float latAngle = Mathf.PI * lat / latitudeSegments;
                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    float lonAngle = 2 * Mathf.PI * lon / longitudeSegments;

                    float x = radius * Mathf.Sin(latAngle) * Mathf.Cos(lonAngle);
                    float y = radius * Mathf.Cos(latAngle);
                    float z = radius * Mathf.Sin(latAngle) * Mathf.Sin(lonAngle);

                    vertices[vertIndex] = new Vector3(x, y, z);
                    uv[vertIndex]       = new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments);

                    vertIndex++;
                }
            }

            int triIndex = 0;
            for (int lat = 0; lat < latitudeSegments; lat++)
            {
                for (int lon = 0; lon < longitudeSegments; lon++)
                {
                    int current = lat * (longitudeSegments + 1) + lon;
                    int next    = current                       + longitudeSegments + 1;

                    triangles[triIndex++] = current;
                    triangles[triIndex++] = next;
                    triangles[triIndex++] = current + 1;

                    triangles[triIndex++] = next;
                    triangles[triIndex++] = next    + 1;
                    triangles[triIndex++] = current + 1;
                }
            }

            mesh.vertices  = vertices;
            mesh.uv        = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        public static Mesh CreateTriangle(float size)
        {
            Mesh  mesh    = new Mesh();
            float offsetY = -size / 3;
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(0,         size + offsetY, 0),
                new Vector3(-size / 2, offsetY,        -size / (2 * Mathf.Sqrt(3))),
                new Vector3(size  / 2, offsetY,        -size / (2 * Mathf.Sqrt(3))),
                new Vector3(0,         offsetY,        size  / Mathf.Sqrt(3))
            };

            int[] triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3,
                0, 3, 1,
                1, 3, 2
            };

            Vector2[] uv = new Vector2[]
            {
                new Vector2(0.5f, 1),
                new Vector2(0,    0),
                new Vector2(1,    0),
                new Vector2(0.5f, 0.5f)
            };

            mesh.vertices  = vertices;
            mesh.triangles = triangles;
            mesh.uv        = uv;
            mesh.RecalculateNormals();

            return mesh;
        }

        public static Mesh CreateMeshFromBounds(Bounds bounds, Vector3 waterSize, Vector3 waterPos)
        {
            var size = bounds.size;
            size   =  KW_Extensions.ClampVector3(size, Vector3.one, Vector3.one * 20000);
            size.x /= waterSize.x;
            size.y /= waterSize.y;
            size.z /= waterSize.z;

            var extents = size * 0.5f;
            var center  = bounds.center;
            center -= waterPos;

            var min = center - extents;
            var max = center + extents;

            Vector3[] vertices =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, min.y, max.z),
            };

            int[] triangles =
            {
                0, 2, 1, //face front
                0, 3, 2,
                2, 3, 4, //face top
                2, 4, 5,
                1, 2, 5, //face right
                1, 5, 6,
                0, 7, 4, //face left
                0, 4, 3,
                5, 4, 7, //face back
                5, 7, 6,
                0, 6, 7, //face bottom
                0, 1, 6
            };

            var cubeMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };

            cubeMesh.Clear();
            cubeMesh.vertices  = vertices;
            cubeMesh.triangles = triangles;
            cubeMesh.RecalculateNormals();

            return cubeMesh;
        }

        public static Mesh GenerateUnderwaterBottomSkirt(Vector2Int meshResolution)
        {
            var vertices  = new List<Vector3>();
            var normals   = new List<Vector3>();
            var colors    = new List<Color>();
            var triangles = new List<int>();

            CreatePlane(new Vector2Int(1, 1), vertices, normals, colors, triangles);

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            return mesh;
        }

        static void SmoothHeights(ref Vector3[] verts, int[] tris, int iterations = 3, float strength = 0.5f, bool preserveBorder = true)
        {
            int vCount = verts.Length;


            var neighbors                                 = new List<int>[vCount];
            for (int i = 0; i < vCount; i++) neighbors[i] = new List<int>();

            for (int t = 0; t < tris.Length; t += 3)
            {
                int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                if (a == b || b == c || c == a) continue;
                neighbors[a].Add(b);
                neighbors[a].Add(c);
                neighbors[b].Add(a);
                neighbors[b].Add(c);
                neighbors[c].Add(a);
                neighbors[c].Add(b);
            }


            bool[] isBorder = new bool[vCount];
            if (preserveBorder)
            {
                var edgeUse = new Dictionary<(int, int), int>(tris.Length);

                void AddEdge(int i, int j)
                {
                    var e = (Math.Min(i, j), Math.Max(i, j));
                    edgeUse[e] = edgeUse.TryGetValue(e, out var c) ? c + 1 : 1;
                }

                for (int t = 0; t < tris.Length; t += 3)
                {
                    int a = tris[t], b = tris[t + 1], c = tris[t + 2];
                    AddEdge(a, b);
                    AddEdge(b, c);
                    AddEdge(c, a);
                }

                foreach (var kv in edgeUse)
                    if (kv.Value == 1)
                    {
                        isBorder[kv.Key.Item1] = true;
                        isBorder[kv.Key.Item2] = true;
                    }
            }


            var yNew = new float[vCount];
            for (int it = 0; it < iterations; it++)
            {
                for (int i = 0; i < vCount; i++)
                {
                    if (preserveBorder && isBorder[i])
                    {
                        yNew[i] = verts[i].y;
                        continue;
                    }

                    var list = neighbors[i];
                    if (list.Count == 0)
                    {
                        yNew[i] = verts[i].y;
                        continue;
                    }

                    float avg                                = 0f;
                    for (int k = 0; k < list.Count; k++) avg += verts[list[k]].y;
                    avg /= list.Count;

                    yNew[i] = Mathf.Lerp(verts[i].y, avg, strength);
                }

                for (int i = 0; i < vCount; i++)
                {
                    verts[i].y = yNew[i];
                }
            }
        }


        public delegate bool WaterAlphaMaskEvaluator(Color pixel);

        public delegate float WaterHeightEvaluator(Color pixel);

        public static Mesh CreateZoneMesh(Texture2D tex, Vector3 zoneSize, WaterAlphaMaskEvaluator maskEvaluator, WaterHeightEvaluator heightEvaluator, int blurRadius = 2)
        {
            int width  = tex.width;
            int height = tex.height;
            var pixels = tex.GetPixels();


            var bluredPixels = new Color[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var currentPixel = pixels[y * width + x];
                    if (maskEvaluator(currentPixel) == false) continue;

                    bluredPixels[y * width + x] = currentPixel;

                    for (int dy = -blurRadius; dy <= blurRadius; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= height) continue;

                        for (int dx = -blurRadius; dx <= blurRadius; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= width) continue;

                            int   nIndex   = ny * width + nx;
                            Color neighbor = pixels[nIndex];

                            if (maskEvaluator(neighbor) == false)
                            {
                                bluredPixels[nIndex] = currentPixel;
                            }
                        }
                    }
                }
            }

            pixels = bluredPixels;


            var vertices = new Vector3[width * height];
            var uvs      = new Vector2[width * height];

            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                var idx = z * width + x;
                var px  = pixels[idx];

                float h = heightEvaluator(px);

                var wx = x                        / (float)(width  - 1) - 0.5f;
                var wz = z                        / (float)(height - 1) - 0.5f;
                vertices[idx] = new Vector3(wx, h / zoneSize.y, wz);
                uvs[idx]      = new Vector2(x     / (float)(width - 1), z / (float)(height - 1));
            }

            var tris = new List<int>();
            for (var z = 0; z < height - 1; z++)
            for (var x = 0; x < width - 1; x++)
            {
                var idx   = z * width + x;
                var idxR  = idx       + 1;
                var idxD  = idx       + width;
                var idxDR = idxD      + 1;

                if (
                    maskEvaluator(pixels[idx])  &&
                    maskEvaluator(pixels[idxD]) &&
                    maskEvaluator(pixels[idxR])
                )
                {
                    tris.Add(idx);
                    tris.Add(idxD);
                    tris.Add(idxR);
                }

                if (
                    maskEvaluator(pixels[idxR]) &&
                    maskEvaluator(pixels[idxD]) &&
                    maskEvaluator(pixels[idxDR])
                )
                {
                    tris.Add(idxR);
                    tris.Add(idxD);
                    tris.Add(idxDR);
                }
            }


            var mesh = new Mesh() { name = "BakedZoneMesh"};

            var vertList = vertices.ToArray();
            var uvList   = uvs.ToArray();
            var trisList = tris.ToArray();

            KW_Extensions.RemoveUnusedVertices(ref vertList, ref uvList, ref trisList);

            SmoothHeights(ref vertList, trisList, iterations: 4, strength: 0.25f, preserveBorder: false);

            mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices    = vertList.ToArray();
            mesh.uv          = uvList.ToArray();
            mesh.triangles   = trisList.ToArray();


            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var normals = mesh.normals;
            for (int i = 0; i < normals.Length; i++)
            {
                var n = normals[i];
                n.y        *= zoneSize.y;
                normals[i] =  n.normalized;
            }

            mesh.normals = normals;

            return mesh;
        }

        public static Mesh CreateZoneMesh(Vector2Int textureSize)
        {
            int width  = textureSize.x;
            int height = textureSize.y;

            var vertices = new Vector3[width * height];
            var uvs      = new Vector2[width * height];

            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                var idx = z * width + x;

                var wx = x / (float)(width  - 1) - 0.5f;
                var wz = z / (float)(height - 1) - 0.5f;
                vertices[idx] = new Vector3(wx, 0, wz);
                uvs[idx]      = new Vector2(x / (float)(width - 1), z / (float)(height - 1));
            }

            var tris = new List<int>();
            for (var z = 0; z < height - 1; z++)
            for (var x = 0; x < width - 1; x++)
            {
                var idx   = z * width + x;
                var idxR  = idx       + 1;
                var idxD  = idx       + width;
                var idxDR = idxD      + 1;

                tris.Add(idx);
                tris.Add(idxD);
                tris.Add(idxR);

                tris.Add(idxR);
                tris.Add(idxD);
                tris.Add(idxDR);
            }

            var mesh = new Mesh() { name = "TempBakedZoneMesh"};

            mesh.indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

            mesh.vertices  = vertices;
            mesh.uv        = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public delegate bool ParticleSpawnThresholdEvaluator(Color center, Color right, Color top, Color additionalDataColor, out Vector3 direction);


        public static Mesh CreateZoneSplashPointCloud(Texture2D tex, Texture2D texAdditionalData, Vector3 zoneSize, ParticleSpawnThresholdEvaluator spawnThresholdEvaluator, WaterHeightEvaluator heightEval,float heightOffset,  float  triRadius = 1.0f, float jitterRadius = 1.0f)
        {
            int width  = tex.width, height = tex.height;
            var pixels = tex.GetPixels();
            var additionalDataPixels = texAdditionalData.GetPixels();

            static int Clamp(int v, int max) => (v < 0) ? 0 : (v >= max ? max - 1 : v);
            int        Idx(int   x, int y)   => y * width + x;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var tris  = new List<int>();

            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                var centerPixel         = pixels[Idx(x,                   z)];
                var rightPixel          = pixels[Idx(Clamp(x + 1, width), z)];
                var leftPixel           = pixels[Idx(x,                   Clamp(z + 1, height))];
                var additionalDataPixel = additionalDataPixels[Idx(x,     z)];

                var requireSpawnParticle = spawnThresholdEvaluator(centerPixel, rightPixel, leftPixel, additionalDataPixel, out var dir);
                if (!requireSpawnParticle) continue;

                var posX = (x / (float)(width  - 1)) * zoneSize.x - zoneSize.x * 0.5f;
                var posZ = (z / (float)(height - 1)) * zoneSize.z - zoneSize.z * 0.5f;
                var posY = heightEval(centerPixel);

                var   jitter = Random.insideUnitSphere * jitterRadius;
                jitter.y = heightOffset * 0.1f;
                
                var centerPos = new Vector3(posX, posY + heightOffset, posZ) + jitter;
                
                
                
                float r = triRadius;
                var   u = Vector3.right;   
                var   v = Vector3.forward;

                var p0 = centerPos                  + v * r;                                 
                var p1 = centerPos - v * (r * 0.5f) + u * (r * 0.8660254f);                 
                var p2 = centerPos                  - v * (r * 0.5f) - u * (r * 0.8660254f); 

                int baseIndex = verts.Count;
                verts.Add(p0);
                verts.Add(p1);
                verts.Add(p2);

                norms.Add(dir);
                norms.Add(dir);
                norms.Add(dir);


                tris.Add(baseIndex + 0);
                tris.Add(baseIndex + 1);
                tris.Add(baseIndex + 2);
            }

            var mesh = new Mesh { name = "SplashPointCloud" };
            mesh.indexFormat = verts.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0, true);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}