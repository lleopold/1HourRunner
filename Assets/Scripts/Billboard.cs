using UnityEngine;

public class BillbBoard : MonoBehaviour
{
    public Transform cam;

    private void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main.transform;
        }
        transform.LookAt(transform.position + cam.forward);
    }

}
