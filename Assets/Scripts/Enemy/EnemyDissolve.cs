using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDissolve : MonoBehaviour
{
    [Header("Material using your Shader Graph dissolve")]
    public Material dissolveTemplate;

    [Header("Timing")]
    public float delayBefore = 0.0f;
    public float dissolveSeconds = 1.2f;

    [Header("Graph property names")]
    public string propDissolve = "_DissolveAmount";
    public string propBaseMap = "_BaseMap";
    public string propBaseColor = "_BaseColor";

    [Header("Direction guard")]
    [Tooltip("If your graph dissolves when the value DECREASES, turn this on.")]
    public bool invertDirection = false; // true = animate 1->0, false = 0->1

    Renderer[] _renderers;
    List<Material[]> _replaced;

    void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        _replaced = new List<Material[]>(_renderers.Length);
        foreach (var r in _renderers) _replaced.Add(null);
    }

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

        // Build per-renderer material arrays using the dissolve shader
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            var srcMats = r.sharedMaterials;
            var dstMats = new Material[srcMats.Length];

            for (int m = 0; m < srcMats.Length; m++)
            {
                var src = srcMats[m];
                var dst = new Material(dissolveTemplate);

                if (src)
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

                // Start value: fully visible for both modes
                if (dst.HasProperty(propDissolve))
                    dst.SetFloat(propDissolve, invertDirection ? 1f : 0f);

                dstMats[m] = dst;
            }

            r.sharedMaterials = dstMats;
            _replaced[i] = dstMats;
        }

        if (delayBefore > 0f) yield return new WaitForSeconds(delayBefore);

        // Animate in the correct direction
        float t = 0f;
        while (t < dissolveSeconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / dissolveSeconds);
            float value = invertDirection ? (1f - a) : a;

            for (int i = 0; i < _replaced.Count; i++)
            {
                var mats = _replaced[i];
                if (mats == null) continue;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat && mat.HasProperty(propDissolve))
                        mat.SetFloat(propDissolve, value);
                }
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
