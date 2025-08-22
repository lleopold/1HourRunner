using System;
using UnityEngine;


public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private MouseSensitivity _mouseSensitivity;
    [SerializeField] private CameraAngle _cameraAngle;  //Probably for deleting
    [SerializeField] private CameraAngleX _cameraAngleX;
    [SerializeField] private float currentZoom;
    [SerializeField] public float currentZoomAngle;

    private void Awake()
    {
        currentZoom = 15.0f;
    }
    private void Start()
    {
        currentZoomAngle = 50f;
    }
    void Update()
    {
        if (target == null)
        {
            target = GameObject.Find("Player").transform;
        }
        if (target != null)
        {
            SetXRotationTo60Degrees(transform);
        }
    }
    void SetXRotationTo60Degrees(Transform rotate)
    {
        rotate.eulerAngles = new Vector3(currentZoomAngle, rotate.eulerAngles.y, rotate.eulerAngles.z);
    }

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position - transform.forward * ZoomWithMouse(transform); //Vratiti ako ne valja
        }
    }
    public float ZoomWithMouse(Transform transform)
    {
        float scrollDelta = -Input.GetAxis("Mouse ScrollWheel");
        float minZoom = 4.44f;
        float maxZoom = 32f;
        currentZoom = Mathf.Clamp(currentZoom + scrollDelta * 3, minZoom, maxZoom);
        return currentZoom;
    }
}

[Serializable]
public struct MouseSensitivity
{
    public float horizontal;
    public float vertical;
}
public struct CameraRotation
{
    public float pitch;
    public float yaw;
}

[Serializable]
public struct CameraAngle
{
    public float min;
    public float max;
}
public struct CameraAngleX
{
    public float min;
    public float max;
}
