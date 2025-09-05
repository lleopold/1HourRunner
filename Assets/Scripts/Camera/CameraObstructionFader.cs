// File: CameraObstructionFader.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraObstructionFader : MonoBehaviour
{
    public Transform target;                 // player (bound by tag if null)
    public LayerMask occluders;              // Containers layer
    public float sphereRadius = 0.35f;
    public float minAlpha = 0.2f;            // 0..1
    public float fadeSpeed = 6f;             // alpha per second
    public string playerTag = "Player";
    public bool verboseLogs = false;

    [Header("Fallback shader (URP/Lit Transparent). If null, auto-find.")]
    public Shader transparentFallback;

    [Header("Debug / Forcing")]
    public bool forceSwap; // Force using fallback swap path even if material seems editable

    void OnEnable() => StartCoroutine(BindPlayerByTag());
    IEnumerator BindPlayerByTag()
    {
        while (!target)
        {
            var go = GameObject.FindWithTag(playerTag);
            if (go) { target = go.transform; break; }
            yield return null;
        }
        if (verboseLogs && target) Debug.Log("CameraObstructionFader: bound -> " + target.name);
    }

    // ===== Internals =====
    class Faded
    {
        public Renderer r;
        public float cur = 1f, tgt = 1f;
        public MaterialPropertyBlock mpb = new();
        public bool capturedColor;
        public Color baseColor = Color.white;
        public bool processed; // has EnsureTransparentCapable run?

        // In-place transparency support state
        public struct MatState
        {
            public Material mat;
            public float surface;
            public int renderQueue;
            public string renderTypeTag;
            public float zwrite;
            public int srcBlend;
            public int dstBlend;
            public float alphaClip; // _AlphaClip toggle (bool stored as float) or -1 if not present
            public bool kwTransparent, kwAlphaPremul, kwAlphaTest;
        }
        public readonly List<MatState> originalStates = new();

        // Swap state
        public Material[] originalSharedMats; // for full restore
        public bool usingSwap;
        public Material[] swapMats;           // URP/Lit Transparent clones

        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public static readonly int StdColor = Shader.PropertyToID("_Color");
        public static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        public static readonly int MainTex = Shader.PropertyToID("_MainTex");
        public static readonly int ZWrite = Shader.PropertyToID("_ZWrite");
        public static readonly int Surface = Shader.PropertyToID("_Surface");
        public static readonly int Blend = Shader.PropertyToID("_Blend");
        public static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        public static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        public static readonly int AlphaClip = Shader.PropertyToID("_AlphaClip");         // bool (float)
        public static readonly int Cutoff = Shader.PropertyToID("_Cutoff");               // threshold (leave as-is)
    }

    readonly Dictionary<Renderer, Faded> map = new();
    readonly List<Renderer> frame = new();
    readonly List<Renderer> toRemove = new();

    void OnDisable()
    {
        // Ensure full restore if object disabled
        foreach (var kv in map)
        {
            var f = kv.Value;
            if (f?.r) { RestoreRenderer(f); f.r.SetPropertyBlock(null); }
        }
        map.Clear();
        frame.Clear();
        toRemove.Clear();
    }

    // Optional external trigger if materials changed at runtime
    public void ForceRefresh()
    {
        foreach (var kv in map)
        {
            var f = kv.Value;
            if (!f.processed)
            {
                EnsureTransparentCapable(f);
                f.processed = true;
            }
        }
        if (verboseLogs) Debug.Log("CameraObstructionFader: ForceRefresh processed existing renderers.");
    }

    void LateUpdate()
    {
        if (!target) return;

        frame.Clear();
        Vector3 camPos = transform.position;
        Vector3 dir = target.position - camPos;
        float dist = dir.magnitude;
        if (dist <= 0.001f) return;

        var hits = Physics.SphereCastAll(camPos, sphereRadius, dir.normalized, dist, occluders, QueryTriggerInteraction.Ignore);
        GatherFrameRenderers(hits, frame);
        //FilterBetweenCameraAndTarget(frame, transform.position, target.position, sphereRadius);

        if (verboseLogs)
            Debug.Log($"CameraObstructionFader: frame renderer count={frame.Count}");

        // Mark to fade
        for (int i = 0; i < frame.Count; i++)
        {
            var r = frame[i];
            if (!r) continue;
            if (!map.TryGetValue(r, out var f))
            {
                f = new Faded { r = r };
                map[r] = f;
                if (verboseLogs) Debug.Log("CameraObstructionFader: new renderer added -> " + r.name);
            }

            if (!f.processed)
            {
                EnsureTransparentCapable(f);
                f.processed = true;
            }

            f.tgt = minAlpha;
        }

        // Animate & restore
        toRemove.Clear();
        foreach (var kv in map)
        {
            var f = kv.Value;
            bool shouldFade = frame.Contains(f.r);
            f.tgt = shouldFade ? minAlpha : 1f;
            f.cur = Mathf.MoveTowards(f.cur, f.tgt, fadeSpeed * Time.deltaTime);

            if (!f.capturedColor)
            {
                Color c = Color.white;
                var mats = f.r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (!m) continue;
                    if (m.HasColor(Faded.BaseColor)) { c = m.GetColor(Faded.BaseColor); break; }
                    if (m.HasColor(Faded.StdColor)) { c = m.GetColor(Faded.StdColor); break; }
                }
                f.baseColor = c;
                f.capturedColor = true;
            }

            var outC = f.baseColor; outC.a = f.cur;

            // Apply color both via MPB and (for in-place) directly to material to cover shaders not using MPB alpha
            if (!f.usingSwap)
            {
                var instMats = f.r.materials; // already instanced previously
                for (int i = 0; i < instMats.Length; i++)
                {
                    var m = instMats[i];
                    if (!m) continue;
                    if (m.HasColor(Faded.BaseColor)) m.SetColor(Faded.BaseColor, outC);
                    if (m.HasColor(Faded.StdColor)) m.SetColor(Faded.StdColor, outC);
                }
            }

            f.r.GetPropertyBlock(f.mpb);
            f.mpb.SetColor(Faded.BaseColor, outC);
            f.mpb.SetColor(Faded.StdColor, outC);
            f.r.SetPropertyBlock(f.mpb);

            if (!shouldFade && Mathf.Approximately(f.cur, 1f))
                toRemove.Add(f.r);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            var r = toRemove[i];
            if (!r) continue;
            if (map.TryGetValue(r, out var f))
            {
                RestoreRenderer(f);
                r.SetPropertyBlock(null);
            }
            map.Remove(r);
        }
    }

    // --- Ensure the renderer can show transparency, else swap to URP/Lit Transparent ---
    void EnsureTransparentCapable(Faded f)
    {
        // If already assigned swap mats (e.g. re-entry), skip
        if (f.usingSwap) return;

        var instancedMats = f.r.materials; // force instancing
        bool attemptInPlace = !forceSwap;

        // Require URP-style surface property OR URP shader name heuristic, otherwise prefer swap
        if (attemptInPlace)
        {
            for (int i = 0; i < instancedMats.Length; i++)
            {
                var m = instancedMats[i];
                if (!m) continue;
                bool hasSurface = m.HasFloat(Faded.Surface);
                bool hasColor = m.HasColor(Faded.BaseColor) || m.HasColor(Faded.StdColor);
                bool looksURP = m.shader && m.shader.name.Contains("Universal");
                if (!(hasSurface && looksURP) && !(hasSurface && hasColor))
                {
                    attemptInPlace = false;
                    break;
                }
            }
        }

        if (attemptInPlace && TryMakeInPlaceTransparent(f, instancedMats))
        {
            if (verboseLogs) Debug.Log($"CameraObstructionFader: In-place transparency OK for {f.r.name}");
            f.usingSwap = false;
            return;
        }

        // Fallback / swap path
        if (verboseLogs) Debug.Log($"CameraObstructionFader: Falling back to swap for {f.r.name}");
        if (!transparentFallback)
            transparentFallback = Shader.Find("Universal Render Pipeline/Lit");

        if (!transparentFallback)
        {
            if (verboseLogs) Debug.LogWarning("CameraObstructionFader: URP/Lit shader not found; transparency may fail.");
            return;
        }

        f.originalSharedMats = f.r.sharedMaterials;
        f.swapMats = new Material[f.originalSharedMats.Length];

        for (int i = 0; i < f.swapMats.Length; i++)
        {
            var src = f.originalSharedMats[i];
            var dst = new Material(transparentFallback)
            {
                name = (src ? src.name : "Mat") + "_FadeClone"
            };

            // Standard transparent setup
            if (dst.HasFloat(Faded.Surface)) dst.SetFloat(Faded.Surface, 1f);
            if (dst.HasFloat(Faded.Blend)) dst.SetFloat(Faded.Blend, 0f);
            if (dst.HasInt(Faded.SrcBlend)) dst.SetInt(Faded.SrcBlend, (int)BlendMode.SrcAlpha);
            if (dst.HasInt(Faded.DstBlend)) dst.SetInt(Faded.DstBlend, (int)BlendMode.OneMinusSrcAlpha);
            if (dst.HasFloat(Faded.ZWrite)) dst.SetFloat(Faded.ZWrite, 0f);
            if (dst.HasFloat(Faded.AlphaClip)) dst.SetFloat(Faded.AlphaClip, 0f);

            dst.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            dst.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            dst.DisableKeyword("_ALPHATEST_ON");
            dst.SetOverrideTag("RenderType", "Transparent");
            dst.renderQueue = (int)RenderQueue.Transparent;

            if (src)
            {
                if (src.HasColor(Faded.BaseColor)) dst.SetColor(Faded.BaseColor, src.GetColor(Faded.BaseColor));
                else if (src.HasColor(Faded.StdColor)) dst.SetColor(Faded.StdColor, src.GetColor(Faded.StdColor));

                var tex = src.HasTexture(Faded.BaseMap) ? src.GetTexture(Faded.BaseMap)
                        : src.HasTexture(Faded.MainTex) ? src.GetTexture(Faded.MainTex)
                        : null;
                if (tex) dst.SetTexture(Faded.BaseMap, tex);
            }

            f.swapMats[i] = dst;
        }

        f.r.sharedMaterials = f.swapMats;
        f.usingSwap = true;
    }

    bool TryMakeInPlaceTransparent(Faded f, Material[] mats)
    {
        f.originalStates.Clear();
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (!m) continue;

            var st = new Faded.MatState
            {
                mat = m,
                surface = m.HasFloat(Faded.Surface) ? m.GetFloat(Faded.Surface) : 0f,
                renderQueue = m.renderQueue,
                renderTypeTag = m.GetTag("RenderType", false),
                zwrite = m.HasFloat(Faded.ZWrite) ? m.GetFloat(Faded.ZWrite) : 1f,
                srcBlend = m.HasInt(Faded.SrcBlend) ? m.GetInt(Faded.SrcBlend) : (int)BlendMode.One,
                dstBlend = m.HasInt(Faded.DstBlend) ? m.GetInt(Faded.DstBlend) : (int)BlendMode.Zero,
                alphaClip = m.HasFloat(Faded.AlphaClip) ? m.GetFloat(Faded.AlphaClip) : -1f,
                kwTransparent = m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"),
                kwAlphaPremul = m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON"),
                kwAlphaTest = m.IsKeywordEnabled("_ALPHATEST_ON")
            };
            f.originalStates.Add(st);

            if (m.HasFloat(Faded.Surface)) m.SetFloat(Faded.Surface, 1f);
            if (m.HasFloat(Faded.Blend)) m.SetFloat(Faded.Blend, 0f);
            if (m.HasInt(Faded.SrcBlend)) m.SetInt(Faded.SrcBlend, (int)BlendMode.SrcAlpha);
            if (m.HasInt(Faded.DstBlend)) m.SetInt(Faded.DstBlend, (int)BlendMode.OneMinusSrcAlpha);
            if (m.HasFloat(Faded.ZWrite)) m.SetFloat(Faded.ZWrite, 0f);
            if (m.HasFloat(Faded.AlphaClip)) m.SetFloat(Faded.AlphaClip, 0f);

            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        // Verify
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (!m) continue;
            if (!IsRuntimeTransparent(m))
            {
                if (verboseLogs) Debug.Log($"CameraObstructionFader: In-place verification failed on {m.name}; will swap.");
                // Restore immediately before swap
                RestoreRenderer(f);
                return false;
            }
        }
        return true;
    }

    bool IsRuntimeTransparent(Material m)
    {
        if (!m) return false;
        bool rq = m.renderQueue >= (int)RenderQueue.Transparent;
        int src = m.HasInt(Faded.SrcBlend) ? m.GetInt(Faded.SrcBlend) : -1;
        int dst = m.HasInt(Faded.DstBlend) ? m.GetInt(Faded.DstBlend) : -1;
        bool blends = (src == (int)BlendMode.SrcAlpha && dst == (int)BlendMode.OneMinusSrcAlpha);
        bool z = !m.HasFloat(Faded.ZWrite) || Mathf.Approximately(m.GetFloat(Faded.ZWrite), 0f);
        return rq && blends && z;
    }

    void RestoreRenderer(Faded f)
    {
        if (f.usingSwap)
        {
            if (f.originalSharedMats != null)
                f.r.sharedMaterials = f.originalSharedMats;
            f.swapMats = null;
            f.originalSharedMats = null;
            f.usingSwap = false;
            return;
        }

        // In-place restore of original render states
        for (int i = 0; i < f.originalStates.Count; i++)
        {
            var st = f.originalStates[i];
            var m = st.mat;
            if (!m) continue;

            if (m.HasFloat(Faded.Surface)) m.SetFloat(Faded.Surface, st.surface);
            if (m.HasFloat(Faded.ZWrite)) m.SetFloat(Faded.ZWrite, st.zwrite);
            if (m.HasInt(Faded.SrcBlend)) m.SetInt(Faded.SrcBlend, st.srcBlend);
            if (m.HasInt(Faded.DstBlend)) m.SetInt(Faded.DstBlend, st.dstBlend);
            if (st.alphaClip >= 0f && m.HasFloat(Faded.AlphaClip)) m.SetFloat(Faded.AlphaClip, st.alphaClip);

            m.renderQueue = st.renderQueue;
            m.SetOverrideTag("RenderType", string.IsNullOrEmpty(st.renderTypeTag) ? "Opaque" : st.renderTypeTag);

            if (st.kwTransparent) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); else m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (st.kwAlphaPremul) m.EnableKeyword("_ALPHAPREMULTIPLY_ON"); else m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (st.kwAlphaTest) m.EnableKeyword("_ALPHATEST_ON"); else m.DisableKeyword("_ALPHATEST_ON");
        }
        f.originalStates.Clear();
    }

    // ---- Utility: collect Renderers and keep only those between camera and player ----
    void GatherFrameRenderers(RaycastHit[] hits, List<Renderer> outList)
    {
        var seen = new HashSet<Renderer>();
        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].collider.transform;

            var r0 = t.GetComponent<Renderer>();
            if (r0 && seen.Add(r0)) outList.Add(r0);

            var rp = t.GetComponentsInParent<Renderer>(true);
            for (int j = 0; j < rp.Length; j++) if (seen.Add(rp[j])) outList.Add(rp[j]);

            var rc = t.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < rc.Length; j++) if (seen.Add(rc[j])) outList.Add(rc[j]);
        }
    }

    void FilterBetweenCameraAndTarget(List<Renderer> list, Vector3 camPos, Vector3 tgtPos, float radius)
    {
        var dir = tgtPos - camPos;
        float dist = dir.magnitude;
        if (dist <= 0f) { list.Clear(); return; }
        Vector3 dn = dir / dist;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            var r = list[i];
            if (!r) { list.RemoveAt(i); continue; }
            float t = Vector3.Dot(r.bounds.center - camPos, dn);
            if (t < 0f || t > dist) { list.RemoveAt(i); continue; }
            Vector3 p = camPos + dn * t;
            if (DistancePointToBounds(p, r.bounds) > radius) list.RemoveAt(i);
        }
    }

    static float DistancePointToBounds(Vector3 p, Bounds b)
    {
        Vector3 cp = b.ClosestPoint(p);
        return Vector3.Distance(p, cp);
    }
}
