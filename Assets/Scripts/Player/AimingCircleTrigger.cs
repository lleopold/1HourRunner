using System.Collections.Generic;
using UnityEngine;

public class AimingCircleTrigger : MonoBehaviour
{
    private readonly HashSet<Collider> _zombiesInside = new HashSet<Collider>();
    private readonly Dictionary<Collider, Enemy> _enemyCache = new Dictionary<Collider, Enemy>();
    private readonly List<Collider> _toRemove = new List<Collider>();

    // Cone shape pushed in by PlayerAimVisuals each frame (local space: apex at this
    // transform, fan opens around local +Z). Used for a deterministic containment test
    // instead of the convex MeshCollider, whose convex hull + per-frame rebuild make
    // trigger-exit / ClosestPoint unreliable.
    private float _coneHalfAngleDeg = 0f;
    private float _coneRadius = 0f;

    public void SetConeShape(float angleDeg, float radius)
    {
        _coneHalfAngleDeg = angleDeg * 0.5f;
        _coneRadius = radius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            _zombiesInside.Add(other);
            Enemy enemy = other.GetComponent<Enemy>();
            _enemyCache[other] = enemy;
            enemy?.SetOutline(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other != null && other.CompareTag("Zombie"))
            RemoveZombie(other);
    }

    void FixedUpdate()
    {
        if (_zombiesInside.Count == 0) return;

        _toRemove.Clear();
        foreach (Collider zombie in _zombiesInside)
        {
            if (zombie == null)
            {
                _toRemove.Add(zombie);
                continue;
            }
            if (!IsInsideCone(zombie))
                _toRemove.Add(zombie);
        }

        for (int i = 0; i < _toRemove.Count; i++)
            RemoveZombie(_toRemove[i]);
    }

    // True while any part of the zombie still lies within the aim cone (angle + range),
    // tested in this transform's local space. A zombie-radius margin keeps it selected
    // until it fully clears the cone, avoiding edge flicker.
    private bool IsInsideCone(Collider zombie)
    {
        // Not yet initialised by PlayerAimVisuals -> fall back to "keep" (don't drop everything).
        if (_coneRadius <= 0f) return true;

        Vector3 local = transform.InverseTransformPoint(zombie.bounds.center);
        Vector2 flat = new Vector2(local.x, local.z);
        float dist = flat.magnitude;
        if (dist < 1e-4f) return true; // sitting on the apex

        // Horizontal radius of the zombie, used as a tolerance margin.
        Vector3 ext = zombie.bounds.extents;
        float zombieRadius = Mathf.Max(ext.x, ext.z);

        // Range test.
        if (dist - zombieRadius > _coneRadius) return false;

        // Angle test: angle off the cone's forward (+Z), shrunk by the zombie's angular size.
        float angleDeg = Mathf.Atan2(flat.x, flat.y) * Mathf.Rad2Deg;
        float angularMargin = Mathf.Asin(Mathf.Clamp01(zombieRadius / dist)) * Mathf.Rad2Deg;
        return Mathf.Abs(angleDeg) - angularMargin <= _coneHalfAngleDeg;
    }

    private void RemoveZombie(Collider zombie)
    {
        if (_enemyCache.TryGetValue(zombie, out Enemy enemy))
        {
            enemy?.SetOutline(false);
            _enemyCache.Remove(zombie);
        }
        _zombiesInside.Remove(zombie);
    }

    public void ClearAllOutlinedZombies()
    {
        foreach (var kvp in _enemyCache)
            kvp.Value?.SetOutline(false);
        _zombiesInside.Clear();
        _enemyCache.Clear();
    }

    /// <summary>Returns the set of zombie colliders currently inside the aiming circle.</summary>
    public HashSet<Collider> GetZombiesInside() => _zombiesInside;
}
