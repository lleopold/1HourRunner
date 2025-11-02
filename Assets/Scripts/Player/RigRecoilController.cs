using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace ZombieGame
{
    /// <summary>
    /// Handles weapon recoil by manipulating Two Bone IK Constraint weights.
    /// Recoil is based on weapon recoil (0-100%) reduced by player's recoilReduction stat.
    /// This class is instantiated and managed by PlayerControllerInput, no manual setup needed.
    /// </summary>
    public class RigRecoilController
    {
        private readonly MonoBehaviour _owner;
        private TwoBoneIKConstraint _leftHandRig;
        private TwoBoneIKConstraint _rightHandRig;

        // Recoil settings - INCREASED SPEEDS
        private float _recoilSpeed = 10f;      // Was 0.15f - much faster kick
        private float _recoverySpeed = 5f;     // Was 0.08f - much faster recovery
        private float _maxRecoilDuration = 0.3f;

        // Runtime state
        private float _currentRecoilWeight = 0f;
        private float _targetRecoilWeight = 0f;
        private Coroutine _recoilCoroutine;

        public RigRecoilController(MonoBehaviour owner)
        {
            _owner = owner;
            InitializeRigReferences();
        }

        /// <summary>
        /// Automatically finds and caches the Two Bone IK Constraint references.
        /// </summary>
        private void InitializeRigReferences()
        {
            // Find RigRecoil GameObject and get all TwoBoneIKConstraints
            Transform rigRecoil = FindChildRecursive(_owner.transform, "RigRecoil");

            if (rigRecoil == null)
            {
                Debug.LogWarning("RigRecoilController: RigRecoil GameObject not found. Recoil will not work.");
                return;
            }

            TwoBoneIKConstraint[] constraints = rigRecoil.GetComponentsInChildren<TwoBoneIKConstraint>();

            foreach (var constraint in constraints)
            {
                if (constraint.name.Contains("LeftHand"))
                {
                    _leftHandRig = constraint;
                }
                else if (constraint.name.Contains("RightHand"))
                {
                    _rightHandRig = constraint;
                }
            }

            if (_leftHandRig == null || _rightHandRig == null)
            {
                Debug.LogWarning($"RigRecoilController: Missing hand rig constraints. Left: {_leftHandRig != null}, Right: {_rightHandRig != null}");
            }
            else
            {
                Debug.Log("RigRecoilController: Successfully initialized hand rig constraints.");
            }
        }

        /// <summary>
        /// Recursively searches for a child transform by name.
        /// </summary>
        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Applies recoil based on weapon recoil value and player's recoilReduction stat.
        /// Formula: FinalRecoil = max(WeaponRecoil - PlayerRecoilReduction, 0) / 100
        /// </summary>
        /// <param name="weaponRecoil">Weapon recoil value (0-100)</param>
        /// <param name="playerRecoilReduction">Player's recoil reduction stat (0-100)</param>
        public void ApplyRecoil(float weaponRecoil, float playerRecoilReduction)
        {
            if (_leftHandRig == null || _rightHandRig == null)
            {
                Debug.LogWarning("RigRecoilController: Cannot apply recoil, rig constraints not initialized.");
                return;
            }

            // Calculate final recoil: weapon recoil reduced by player stat
            float effectiveRecoil = Mathf.Max(weaponRecoil - playerRecoilReduction, 0f);
            float finalRecoilPercent = Mathf.Clamp01(effectiveRecoil / 100f);

            // Stop any existing recoil animation
            if (_recoilCoroutine != null)
            {
                _owner.StopCoroutine(_recoilCoroutine);
            }

            // Start new recoil animation
            _recoilCoroutine = _owner.StartCoroutine(RecoilSequence(finalRecoilPercent));

            Debug.Log($"Rig Recoil: Weapon={weaponRecoil}%, Reduction={playerRecoilReduction}%, Final={effectiveRecoil}% (Weight={finalRecoilPercent:F2})");
        }

        /// <summary>
        /// Animates the recoil: quick increase, hold briefly, then gradual recovery.
        /// </summary>
        private IEnumerator RecoilSequence(float targetWeight)
        {
            Debug.Log($"Starting Recoil Sequence to Target Weight: {targetWeight:F2}");
            _targetRecoilWeight = targetWeight;
            float recoilStartTime = Time.time;

            // Phase 1: Quick recoil increase
            while (_currentRecoilWeight < _targetRecoilWeight)
            {
                _currentRecoilWeight = Mathf.MoveTowards(
                    _currentRecoilWeight,
                    _targetRecoilWeight,
                    _recoilSpeed * Time.deltaTime
                );
                UpdateRigWeights(_currentRecoilWeight);
                yield return null;
            }

            // Phase 2: Hold at peak for a brief moment
            yield return new WaitForSeconds(0.05f);

            // Phase 3: Gradual recovery
            _targetRecoilWeight = 0f;

            while (_currentRecoilWeight > 0.01f)
            {
                // Force faster recovery if recoil has been active too long
                float recoveryMultiplier = (Time.time - recoilStartTime > _maxRecoilDuration) ? 2f : 1f;

                _currentRecoilWeight = Mathf.MoveTowards(
                    _currentRecoilWeight,
                    0f,
                    _recoverySpeed * recoveryMultiplier * Time.deltaTime
                );

                UpdateRigWeights(_currentRecoilWeight);
                yield return null;
            }

            // Ensure weights are completely reset
            _currentRecoilWeight = 0f;
            UpdateRigWeights(0f);
            _recoilCoroutine = null;
        }

        /// <summary>
        /// Updates the weight of both hand rigs.
        /// </summary>
        private void UpdateRigWeights(float weight)
        {
            if (_leftHandRig != null)
            {
                _leftHandRig.weight = weight;
            }

            if (_rightHandRig != null)
            {
                _rightHandRig.weight = weight;
            }
        }

        /// <summary>
        /// Force cancels any active recoil and resets the rig weights.
        /// Call this when switching weapons or on player death.
        /// </summary>
        public void CancelRecoil()
        {
            if (_recoilCoroutine != null)
            {
                _owner.StopCoroutine(_recoilCoroutine);
                _recoilCoroutine = null;
            }

            _currentRecoilWeight = 0f;
            _targetRecoilWeight = 0f;
            UpdateRigWeights(0f);
        }

        /// <summary>
        /// Returns the current recoil weight (0-1).
        /// </summary>
        public float GetCurrentRecoilWeight()
        {
            return _currentRecoilWeight;
        }

        /// <summary>
        /// Allows runtime adjustment of recoil animation speeds.
        /// </summary>
        public void SetRecoilSpeeds(float recoilSpeed, float recoverySpeed, float maxDuration)
        {
            _recoilSpeed = Mathf.Max(0.01f, recoilSpeed);
            _recoverySpeed = Mathf.Max(0.01f, recoverySpeed);
            _maxRecoilDuration = Mathf.Max(0.1f, maxDuration);
        }
    }
}