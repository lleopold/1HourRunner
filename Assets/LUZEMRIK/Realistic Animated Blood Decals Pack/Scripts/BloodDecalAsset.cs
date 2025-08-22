using UnityEngine;

namespace LUZEMRIK.BloodDecals
{
    [CreateAssetMenu(menuName = "LUZEMRIK/BloodDecalAsset")]
    public class BloodDecalAsset : ScriptableObject
    {
        [SerializeField] private Material _baseMaterial;
        [SerializeField] private Texture2D[] _textures;
        [SerializeField] private float _animationTimerSeconds = 1;
        [SerializeField] private float _zOffset = 0;
        [SerializeField] private float _depth = 0.25f;
        [SerializeField] private bool _randomRotation = true;
        [SerializeField] private bool _randomX = true;

        public Material BaseMaterial { get => _baseMaterial; }
        public Texture2D[] Textures { get => _textures; }
        public Texture2D RandomTexture => _textures[Random.Range(0, _textures.Length)];
        public float AnimationTimerSeconds { get => _animationTimerSeconds; }
        public float ZOffset { get => _zOffset; }
        public float Depth { get => _depth; }
        public bool RandomRotation { get => _randomRotation; }
        public bool RandomX { get => _randomX; }
    }
}