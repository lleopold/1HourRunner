using System.Collections.Generic;
using UnityEngine;

internal class AimingCircleTrigger : MonoBehaviour
{
    private HashSet<Collider> zombiesInside = new HashSet<Collider>();

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Zombie"))
        {
            zombiesInside.Add(other);
            Enemy enemyScript = other.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.SetOutline(true);
            }
        }
    }

    /// <summary>
    /// works really bad, need to fix
    /// </summary>
    void FixedUpdate() // Run after physics updates
    {
        //Debug.Log("Checking zombies inside trigger: " + zombiesInside.Count);
        if (zombiesInside.Count == 0) return;

        List<Collider> zombiesToRemove = new List<Collider>();

        foreach (Collider zombie in zombiesInside)
        {
            Vector3 closestPoint = GetComponent<MeshCollider>().ClosestPoint(zombie.transform.position);
            if (!zombie.bounds.Contains(closestPoint))
            {
                //Debug.Log("Zombie is outside the aiming circle!");
                OnTriggerExit(zombie); // Manually trigger exit
                zombiesToRemove.Add(zombie);
            }
            else
            {
                //Debug.Log("Zombie is still inside the aiming circle.");
            }
        }

        foreach (Collider zombie in zombiesToRemove)
        {
            zombiesInside.Remove(zombie);
        }
    }
    public void ClearAllOutlinedZombies()
    {
        foreach (Collider zombie in zombiesInside)
        {
            Enemy enemyScript = zombie.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.SetOutline(false);
            }
        }
        zombiesInside.Clear();
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log("Zombie exited: " + other.name);
        if (other != null && other.CompareTag("Zombie"))
        {
            Enemy enemyScript = other.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.SetOutline(false);
            }
        }
    }
}
