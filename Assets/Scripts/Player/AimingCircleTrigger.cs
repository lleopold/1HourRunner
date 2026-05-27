using System.Collections.Generic;
using UnityEngine;

public class AimingCircleTrigger : MonoBehaviour
{
    private readonly HashSet<Collider> _zombiesInside = new HashSet<Collider>();
    private readonly Dictionary<Collider, Enemy> _enemyCache = new Dictionary<Collider, Enemy>();
    private readonly List<Collider> _toRemove = new List<Collider>();
    private MeshCollider _meshCollider;

    void Awake()
    {
        _meshCollider = GetComponent<MeshCollider>();
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
            Vector3 closestPoint = _meshCollider.ClosestPoint(zombie.transform.position);
            if (!zombie.bounds.Contains(closestPoint))
                _toRemove.Add(zombie);
        }

        for (int i = 0; i < _toRemove.Count; i++)
            RemoveZombie(_toRemove[i]);
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
