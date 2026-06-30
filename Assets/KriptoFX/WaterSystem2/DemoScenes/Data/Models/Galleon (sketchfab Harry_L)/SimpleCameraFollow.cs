using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target;
    public float     maxDistance = 20f;
    public float     smoothTime  = 1.0f; // Чем больше — тем медленнее камера ускоряется

    private Vector3 offset;
    private Vector3 lockedCameraPosition;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target != null)
        {
            offset               = transform.position - target.position;
            lockedCameraPosition = transform.position;
        }
    }

    void Update()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        float   distance        = Vector3.Distance(lockedCameraPosition, desiredPosition);

        if (distance > maxDistance)
        {
            lockedCameraPosition = desiredPosition;
        }


        transform.position = Vector3.SmoothDamp(transform.position, lockedCameraPosition, ref velocity, smoothTime);
       // transform.LookAt(target);
    }
}