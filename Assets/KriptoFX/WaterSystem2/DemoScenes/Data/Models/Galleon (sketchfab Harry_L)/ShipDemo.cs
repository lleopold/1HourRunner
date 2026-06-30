using UnityEngine;


public class ShipDemo : MonoBehaviour
{
    public Rigidbody RigidBody;

   
    public bool UseAutoPilot = true;
    public float MaxForwardSpeed = 10f;
    public float MaxBackwardSpeed = 5f;
    public float Acceleration = 2f;
    public float TurnTorque = 200f;
    public float MaxTurnSpeed = 1f;

  
    public float ForwardDrag = 0.1f;
    public float SidewaysDrag = 2f;
    public float VerticalDrag = 2f;

   
    public GameObject Steering;
    public float SteeringVisualSpeed = 0.2f;
    public float SteeringVisualAngle = 500f;

  
    public Vector3 ForceOffset = new Vector3(0, -1, -2); 

    float             currentTurnInput;
    float             wheelRotationVisual;
    

    
#if UNITY_6000_0_OR_NEWER
    private Vector3 RbVelocity
    {
        get => RigidBody.linearVelocity;
        set => RigidBody.linearVelocity = value;
    }
#else
    private Vector3 RbVelocity
    {
        get => RigidBody.velocity;
        set => RigidBody.velocity = value;
    }
#endif
    
    void Update()
    {
        //if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
         //   currentTurnInput = 0;
        
        Vector3 localVelocity = transform.InverseTransformDirection(RbVelocity);
        float   targetSpeed   = 0;

        targetSpeed = MaxForwardSpeed;
       // if (Input.GetKey(KeyCode.W) || UseAutoPilot)      targetSpeed = MaxForwardSpeed;
       // else if (Input.GetKey(KeyCode.S))                 targetSpeed = -MaxBackwardSpeed;

        localVelocity.z = Mathf.MoveTowards(localVelocity.z, targetSpeed, Acceleration * Time.fixedDeltaTime);

   
        localVelocity.x *= 1 - SidewaysDrag * Time.fixedDeltaTime;
        localVelocity.y *= 1 - VerticalDrag * Time.fixedDeltaTime;
        localVelocity.z *= 1 - ForwardDrag * Time.fixedDeltaTime;

        RbVelocity = transform.TransformDirection(localVelocity);

     
        float input = 0;
       // if (Input.GetKey(KeyCode.A)) input = -1;
        //else if (Input.GetKey(KeyCode.D)) input = 1;

        currentTurnInput = Mathf.Lerp(currentTurnInput, input, Time.fixedDeltaTime * 3f);
        if (Mathf.Abs(currentTurnInput) > 0.01f)
        {
            Vector3 forcePos = transform.TransformPoint(ForceOffset);
            RigidBody.AddTorque(Vector3.up * currentTurnInput * TurnTorque, ForceMode.Force);
        }

      
        if (Steering != null)
        {
            wheelRotationVisual = Mathf.MoveTowards(wheelRotationVisual, input, SteeringVisualSpeed * Time.deltaTime);
            float zRot = Mathf.SmoothStep(-SteeringVisualAngle, SteeringVisualAngle, (wheelRotationVisual + 1) * 0.5f);
            Steering.transform.localRotation = Quaternion.Euler(0, 0, zRot);
        }
        
        float roll           = transform.eulerAngles.z;
        if (roll > 180) roll -= 360;
        float rollCorrection = -roll          * 0.9f;
        RigidBody.AddTorque(transform.forward * rollCorrection, ForceMode.Force);

     
        Vector3 av = RigidBody.angularVelocity;
        av.x                      *= 0.95f; 
        av.z                      *= 0.95f; 
        RigidBody.angularVelocity =  av;
    }

    void OnDrawGizmosSelected()
    {
        if (RigidBody == null) return;

        Vector3 forcePoint = transform.TransformPoint(ForceOffset);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(forcePoint, 2.0f);
        Gizmos.DrawLine(forcePoint, forcePoint + transform.forward * 2);
    }
}
