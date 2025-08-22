using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace LUZEMRIK.BloodDecals
{
    public class BloodDecalManager : MonoBehaviour
    {
        #region Singleton
        private static BloodDecalManager _instance;
        public static BloodDecalManager Instance { get => _instance; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
                Destroy(this);
            else
                _instance = this;
        }
        #endregion

        [SerializeField] private int _poolSize = 128;

        private List<BloodDecal> _decals;
        private Dictionary<Material, Stack<Material>> _materials;
        private int _poolIndex = 0;

        void Start()
        {
            _decals = new(_poolSize);
            for (int i = 0; i < _poolSize; i++)
            {
                BloodDecal bpd = new GameObject("Blood Decal").AddComponent<BloodDecal>();
                bpd.transform.SetParent(transform, false);
                bpd.gameObject.SetActive(false);

                _decals.Add(bpd);
            }
            _materials = new();
        }

        void OnDestroy()
        {
            foreach (var kvp in _materials)
                foreach (var mat in kvp.Value)
                    Destroy(mat);
        }

        public void AddDecal(BloodDecalAsset collectionAsset, Color32 color, Vector3 position, Vector3 normal, Vector3 scale)
        {
            BloodDecal bpd = _decals[_poolIndex];

            bpd.transform.position = position;

            Quaternion rotation = Quaternion.LookRotation(-normal, Vector3.up);
            if (collectionAsset.RandomRotation)
                rotation = Quaternion.AngleAxis(Random.Range(0, 360), normal) * rotation;
            bpd.transform.rotation = rotation;

            bpd.transform.localScale = scale;

            Vector2 uvScale = new Vector2(collectionAsset.RandomX ? Mathf.Sign(Random.value - 0.5f) : 1, 1);
            bpd.Play(collectionAsset, color, uvScale);

            bpd.gameObject.SetActive(true);

            _poolIndex = (_poolIndex + 1) % _poolSize;
        }

        public void ClearDecals()
        {
            _poolIndex = 0;
            for (int i = 0; i < _poolSize; i++)
                _decals[i].gameObject.SetActive(false);
        }

        public Material GetMaterial(Material baseMaterial)
        {
            if (!_materials.ContainsKey(baseMaterial))
                _materials.Add(baseMaterial, new Stack<Material>(_poolSize));

            var stack = _materials[baseMaterial];
            if (stack.Count == 0)
                stack.Push(Instantiate(baseMaterial));

            return stack.Pop();
        }

        public void ReturnMaterial(Material baseMaterial, Material material)
        {
            if (_materials.ContainsKey(baseMaterial))
                _materials[baseMaterial].Push(material);
        }
    }
}
