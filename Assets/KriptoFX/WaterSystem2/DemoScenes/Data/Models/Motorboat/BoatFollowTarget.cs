using KWS;
using UnityEngine;

public class BoatFollowTarget : MonoBehaviour
{
    public  Transform target;
    public  float     maxSpeed         = 10f;
    public  float     turnSpeed        = 6f;
    public  float     stopDistance     = 2f;
    public  float     slowDownDistance = 20f;
    public  Vector3   AngleOffset;
    public  float     speedSmoothTime = 0.8f;
    private float     speedVel;
    
    private Rigidbody rb;
    private float     currentSpeed;

    private void Awake()
    {
        rb               = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

    }

    private void FixedUpdate()
    {
        if (target == null) return;

        Vector3 to = target.position - rb.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist < 0.001f) return;

        Vector3 dir = to / dist;

        float targetSpeed = 0f;
        if (dist > stopDistance)
        {
            float t = Mathf.Clamp01((dist - stopDistance) / Mathf.Max(0.01f, slowDownDistance - stopDistance));
            targetSpeed = maxSpeed * t;
        }

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVel, speedSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);

        Vector3 step = dir * currentSpeed * Time.fixedDeltaTime;
        Vector3 next = rb.position + step;
        next.y = rb.position.y;
        rb.MovePosition(next);

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(AngleOffset);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
    }
}