using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDissolve : MonoBehaviour
{
    [Header("Assign a material that uses your Shader Graph dissolve")]
    public Material dissolveTemplate;              // create 1 material from your dissolve graph

    [Header("Timing")]
    public float delayBefore = 0.0f;               // wait before starting dissolve
    public float dissolveSeconds = 1.2f;           // dissolve duration

    [Header("Property names in your graph")]
    public string propDissolve = "_DissolveAmount";    // match your graph property
    public string propBaseMap = "_BaseMap";            // URP Lit base texture
    public string propBaseColor = "_BaseColor";        // URP Lit base color

    Renderer[] _renderers;
    List<Material[]> _originals;     // optional if you want to restore instead of destroy
    List<Material[]> _replaced;      // per-renderer material arrays we assign

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        _originals = new List<Material[]>(_renderers.Length);
        _replaced = new List<Material[]>(_renderers.Length);
        foreach (var r in _renderers)
        {
            _originals.Add(r.sharedMaterials);
            _replaced.Add(null);
        }
    }

    // Call this from your death code
    public void TriggerDissolveAndDestroy(float? seconds = null)
    {
        if (seconds.HasValue) dissolveSeconds = seconds.Value;
        StopAllCoroutines();
        StartCoroutine(Co_DissolveThenDestroy());
    }

    IEnumerator Co_DissolveThenDestroy()
    {
        if (dissolveTemplate == null)
        {
            Debug.LogWarning("EnemyDissolve: dissolveTemplate not set.");
            Destroy(gameObject);
            yield break;
        }

        // 1) Build new material arrays using the dissolve shader, copy base color/texture
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            var srcMats = r.sharedMaterials;
            var dstMats = new Material[srcMats.Length];
            for (int m = 0; m < srcMats.Length; m++)
            {
                var src = srcMats[m];
                var dst = new Material(dissolveTemplate); // per-submesh instance
                // Copy common visuals
                if (src != null)
                {
                    if (src.HasProperty(propBaseMap) && dst.HasProperty(propBaseMap))
                        dst.SetTexture(propBaseMap, src.GetTexture(propBaseMap));
                    else if (src.HasProperty("_MainTex") && dst.HasProperty(propBaseMap))
                        dst.SetTexture(propBaseMap, src.GetTexture("_MainTex"));

                    if (src.HasProperty(propBaseColor) && dst.HasProperty(propBaseColor))
                        dst.SetColor(propBaseColor, src.GetColor(propBaseColor));
                    else if (src.HasProperty("_Color") && dst.HasProperty(propBaseColor))
                        dst.SetColor(propBaseColor, src.GetColor("_Color"));
                }
                // start fully visible
                if (dst.HasProperty(propDissolve)) dst.SetFloat(propDissolve, 0f);
                dstMats[m] = dst;
            }
            r.sharedMaterials = dstMats;
            _replaced[i] = dstMats;
        }

        if (delayBefore > 0f) yield return new WaitForSeconds(delayBefore);

        // 2) Animate DissolveAmount 0→1
        float t = 0f;
        while (t < dissolveSeconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / dissolveSeconds);
            for (int i = 0; i < _replaced.Count; i++)
            {
                var mats = _replaced[i];
                if (mats == null) continue;
                for (int m = 0; m < mats.Length; m++)
                    if (mats[m] && mats[m].HasProperty(propDissolve))
                        mats[m].SetFloat(propDissolve, a);
            }
            yield return null;
        }

        // 3) Done
        Destroy(gameObject);
    }
}
