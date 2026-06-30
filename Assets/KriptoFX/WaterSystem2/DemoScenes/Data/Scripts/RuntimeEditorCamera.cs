using UnityEngine;

public class RuntimeEditorCamera : MonoBehaviour
{
    public float moveSpeed       = 10f;   
    public float fastSpeed       = 30f;   
    public float lookSensitivity = 2f;    
    public float scrollSpeed     = 10f;  
    public bool  invertY         = false; 

    float rotationX;
    float rotationY;

    void OnEnable()
    {
        var angles = transform.eulerAngles;
        rotationX        = angles.y;
        rotationY        = angles.x;
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible   = false;
    }

    void Update()
    {
       
        rotationX          += Input.GetAxis("Mouse X")                                         * lookSensitivity;
        rotationY          += (invertY ? Input.GetAxis("Mouse Y") : -Input.GetAxis("Mouse Y")) * lookSensitivity;
        rotationY          =  Mathf.Clamp(rotationY, -89f, 89f); 
        transform.rotation =  Quaternion.Euler(rotationY, rotationX, 0);

      
        float speed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : moveSpeed;

      
        Vector3 move = new Vector3(
            Input.GetAxis("Horizontal"),                                           // A/D
            (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0), // Q/E
            Input.GetAxis("Vertical")                                              // W/S
        );

        transform.Translate(move * speed * Time.deltaTime, Space.Self);

      
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
            transform.Translate(Vector3.forward * scroll * scrollSpeed, Space.Self);

       
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }
}