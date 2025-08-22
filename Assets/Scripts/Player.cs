using System.Text.RegularExpressions;
using UnityEngine;

public class Player
{
    public GameObject playerGameObjectPrefab;
    public GameObject playerGameObjectInstance;
    public GameObject weaponGameObjectPrefab;
    public GameObject weaponGameObjectInstance;

    void Start()
    {

    }


    public Player CreatePlayer()
    {
        return this;
    }
    Transform FindRecursive(Transform parent, string pattern)
    {
        Regex regex = new Regex(pattern);

        if (regex.IsMatch(parent.name))
        {
            return parent;
        }

        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, pattern);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public void AttachWeapon()
    {
        //mixamorig1:RightHand
        string bonename = "mixamorig[0-9]:RightHand";
        Transform rightHandTransform = FindRecursive(playerGameObjectInstance.transform, bonename);
        if (rightHandTransform == null)
        {
            bonename = "mixamorig:RightHand";
            rightHandTransform = FindRecursive(playerGameObjectInstance.transform, bonename);
        }
        weaponGameObjectInstance.name = "Weapon";
        weaponGameObjectInstance.transform.parent = rightHandTransform;

        if (DataHolder.weaponType == WeaponType.H1)
        {
            weaponGameObjectInstance.transform.localPosition = new Vector3(-0.0464068204f, 0.186311349f, 0.041510012f);
            weaponGameObjectInstance.transform.localRotation = Quaternion.Euler(279.578644f, 325.888763f, 119.821121f);
        }
        //2H
        if (DataHolder.weaponType == WeaponType.H2)
        {
            weaponGameObjectInstance.transform.localPosition = new Vector3(-0.111000001f, 0.444000006f, 0.0560000017f);
            weaponGameObjectInstance.transform.localRotation = Quaternion.Euler(274.885498f, 351.066345f, 85.1047287f);
        }
    }
}
