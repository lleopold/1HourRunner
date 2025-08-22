using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

namespace LUZEMRIK.BloodDecals
{
    public class BloodDecal : MonoBehaviour
    {
        private DecalProjector _projector;
        private BloodDecalAsset _decalAsset;
        private Material _materialInstance;
        private float _animSeconds;
        private float _animTimer;

        const float SMOOTH_EDGE = 0.001f;

        void Awake()
        {
            _projector = gameObject.AddComponent<DecalProjector>();
        }

        void OnDestroy()
        {
            if (_materialInstance != null)
                BloodDecalManager.Instance.ReturnMaterial(_projector.material, _materialInstance);
        }

        public void Play(BloodDecalAsset decalAsset, Color32 color, Vector2 uvScale)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            StopAllCoroutines();


            if (_materialInstance != null)
                BloodDecalManager.Instance.ReturnMaterial(_decalAsset.BaseMaterial, _materialInstance);

            _decalAsset = decalAsset;
            _materialInstance = BloodDecalManager.Instance.GetMaterial(decalAsset.BaseMaterial);
            _projector.material = _materialInstance;

            _materialInstance.SetTexture("_AlphaMap", decalAsset.RandomTexture);
            _materialInstance.SetColor("_Color", color);
            _materialInstance.SetFloat("_Edge1", 0.0f);
            _projector.pivot = new Vector3(0.0f, 0.0f, decalAsset.ZOffset);
            _projector.size = new Vector3(1.0f, 1.0f, decalAsset.Depth);
            _projector.startAngleFade = 0;
            _projector.endAngleFade = 44;
            _projector.uvScale = uvScale;
            _animSeconds = decalAsset.AnimationTimerSeconds;
            _animTimer = 0.0f;
            StartCoroutine(AnimationCoroutine());
        }

        IEnumerator AnimationCoroutine()
        {
            static float easeOut(float x, float y) => 1.0f - Mathf.Pow(1.0f - x, y);
            static float easeIn(float x, float y) => Mathf.Pow(x, y);

            while (_animTimer <= _animSeconds)
            {
                float edge1 = easeOut(Mathf.Clamp01(_animTimer / _animSeconds), 5);
                _materialInstance.SetFloat("_Edge1", edge1);
                _animTimer += Time.deltaTime * Time.timeScale;
                yield return null;
            }
            _materialInstance.SetFloat("_Edge1", 1.0f);
        }
    }
}