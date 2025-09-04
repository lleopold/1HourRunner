// File: CameraObstructionFader.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraObstructionFader : MonoBehaviour
{
    public Transform target;                 // player
    public LayerMask occluders;              // Containers layer
    public float sphereRadius = 0.35f;
    public float minAlpha = 0.01f;           // how transparent
    public float fadeSpeed = 6f;             // alpha per second
    public string playerTag = "Player";  // optional tag fallback

    void OnEnable() => StartCoroutine(BindPlayerByTag());

    IEnumerator BindPlayerByTag()
    {
        while (target == null)
        {
            var go = GameObject.FindWithTag(playerTag);
            if (go != null) { target = go.transform; break; }
            yield return null; // try next frame
        }
        Debug.Log("CameraObstructionFader: bound to " + target.name);
    }

    class Faded
    {
        public Renderer r;
        public float cur = 1f, tgt = 1f;
        public MaterialPropertyBlock mpb = new();
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    }

    readonly Dictionary<Renderer, Faded> map = new();
    readonly List<Renderer> frame = new();
    void LateUpdate()
    {
        if (!target) return;

        frame.Clear();
        Vector3 camPos = transform.position;
        Vector3 dir = target.position - camPos;
        float dist = dir.magnitude;

        var hits = Physics.SphereCastAll(
            camPos, sphereRadius, dir.normalized, dist, occluders, QueryTriggerInteraction.Ignore);

        GatherFrameRenderers(hits, frame);   // <— fix: collect parents + children

        foreach (var r in frame)
        {
            if (!r) continue;
            if (!map.TryGetValue(r, out var f))
            {
                f = new Faded { r = r, cur = 1f, tgt = 1f };
                map[r] = f;
                foreach (var m in r.materials) { m.SetFloat("_Surface", 1f); m.renderQueue = 3000; }
            }
            f.tgt = minAlpha;
            Debug.LogWarning("Fading " + r.name + "alfa: " + minAlpha.ToString());
        }

        var toRemove = new List<Renderer>();
        foreach (var kv in map)
        {
            var f = kv.Value;
            bool shouldFade = frame.Contains(f.r);
            f.tgt = shouldFade ? minAlpha : 1f;
            f.cur = Mathf.MoveTowards(f.cur, f.tgt, fadeSpeed * Time.deltaTime);

            f.r.GetPropertyBlock(f.mpb);
            var c = f.mpb.HasVector(Faded.BaseColor) ? (Color)f.mpb.GetVector(Faded.BaseColor) : Color.white;
            c.a = f.cur;
            f.mpb.SetColor(Faded.BaseColor, c);
            f.r.SetPropertyBlock(f.mpb);

            if (!shouldFade && Mathf.Approximately(f.cur, 1f)) toRemove.Add(f.r);
        }

        foreach (var r in toRemove)
        {
            if (r) { foreach (var m in r.materials) { m.SetFloat("_Surface", 0f); m.renderQueue = -1; } r.SetPropertyBlock(null); }
            map.Remove(r);
        }
    }

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
}
