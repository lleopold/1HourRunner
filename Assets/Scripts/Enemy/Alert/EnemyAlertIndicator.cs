using MoreMountains.Feedbacks;
using System.Collections;
using UnityEngine;
public class EnemyAlertIndicator : MonoBehaviour
{
    [Header("Resources path (under Assets/Resources)")]
    public string prefabPath = "Indicators/Exclamation";
    public Vector3 worldOffset = new Vector3(0f, 0.2f, 0f);
    public bool faceCamera = true;

    [Header("Telegraph")]
    public float defaultDuration = 0.5f;

    private GameObject _instance;
    private MMF_Player _mmf;
    private Coroutine _activeCo;
    private bool _initialized;

    public bool IsShowing => _instance != null && _instance.activeSelf;

    private void LateUpdate()
    {
        if (_instance == null || !_instance.activeSelf) return;

        // Follow head position (approx: use renderer bounds)
        if (TryGetHeadPosition(out Vector3 headPos))
        {
            _instance.transform.position = headPos + worldOffset;
        }
        else
        {
            _instance.transform.position = transform.position + worldOffset;
        }

        if (faceCamera && Camera.main != null)
        {
            _instance.transform.forward = (Camera.main.transform.position - _instance.transform.position).normalized * -1f;
        }
    }

    public void EnsureReady()
    {
        if (_initialized) return;
        var prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"EnemyAlertIndicator: Prefab not found at Resources/{prefabPath}");
            return;
        }
        _instance = Instantiate(prefab, transform.position + worldOffset, Quaternion.identity, null);
        _mmf = _instance.GetComponent<MMF_Player>();
        _instance.SetActive(false);
        _initialized = true;
    }

    public void Cancel()
    {
        if (_activeCo != null)
        {
            StopCoroutine(_activeCo);
            _activeCo = null;
        }
        if (_instance) _instance.SetActive(false);
    }

    public Coroutine Telegraph(MonoBehaviour runner, float duration, System.Action onComplete, bool playFeedback = true)
    {
        EnsureReady();
        Cancel();
        _activeCo = runner.StartCoroutine(TelegraphCo(duration > 0 ? duration : defaultDuration, onComplete, playFeedback));
        return _activeCo;
    }

    private IEnumerator TelegraphCo(float duration, System.Action onComplete, bool playFeedback)
    {
        if (_instance == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        _instance.SetActive(true);

        if (_mmf && playFeedback)
        {
            _mmf.StopFeedbacks();
            _mmf.PlayFeedbacks();
        }

        yield return new WaitForSeconds(duration);

        _instance.SetActive(false);
        onComplete?.Invoke();
        _activeCo = null;
    }

    private bool TryGetHeadPosition(out Vector3 pos)
    {
        pos = Vector3.zero;
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return false;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        pos = new Vector3(b.center.x, b.max.y - (b.size.y * 0.15f), b.center.z);
        return true;
    }
}