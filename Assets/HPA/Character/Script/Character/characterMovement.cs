// Description : MouseController : use to controller where the character is look to.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HP.Generics
{
    public class characterMovement : MonoBehaviour
    {
        public bool                     SeeInspector = true;

        public Rigidbody                rbBodyCharacter;           // Reference to the character body
        public Transform                tangentStartPosition;
        public Transform                objCamera;                 // Reference to the camera
        public GameObject               addForceObj;              // Position where forces is add to the character
        public Transform                refHead;                    // use for focus camera in inGameGloabalManager in the Hierarchy

        private string                  s_mouseAxisX = "Mouse X";                // Default Mouse Inputs
        private string                  s_mouseAxisY = "Mouse Y";


        public float                    currentDesktop_X_Axis = 0;
        public float                    currentDesktop_Y_Axis = 0;
        public float                    speedKeybordMovement = 2;



        public float                    minimum = -60f;            // Limit camera Y movement
        public float                    maximum = 60f;

        public float                    characterSpeed = 2;            // Character speed when moving left right or forward backward


        public float                    sensibilityMouse = 2;  // Mouse sensibility
        public AnimationCurve           animationCurveMouse;


        public float                    mouseY = 0;            // current X camera Rotation

        private float                   tmpXAxis = 0;         // temporary values
        private float                   tmpYAxis = 0;

        public LayerMask                myLayerMask;


        private float                   XAxis = 0;
        private float                   YAxis = 0;

        private float                   mouseVertical = 0;

        float                           mouseInputX = 0;

        float                           mouseHorizontal = 0;

        public float                    BrakeForce = 35f;
        public float                    Coeff = .15f;
        public float                    MaxSpeed = 1f;

        public scPreventClimbing        preventClimbing;

        // Crouch
        public bool                     allowCrouch = false;
        public bool                     b_Crouch = false;
        public float                    targetScaleCrouch = .5f;
        private float                   refScaleCrouch = 1f;
        public float                    crouchSpeed = 3f;
        public float                    heightCheck = 2.05f;
        public LayerMask                layerCheckCrouch;

        // Run
        public float                    speedMultiplier = 3;
        private float                   currentSpeedMultiplier = 1;
        public bool                     b_AllowRun = false;
        public bool                     isRunning = false;


        public float                    gravityScale = 1.0f;
        private static float            globalGravity = -9.81f;
        public float                    MaxAngle = 70;
        private float                   currentAngle = 0;
        private Vector3                 circlePos = Vector3.zero;
        public bool                     moreInfoMaxAngle = true;

        //-> Variables use to check if the character is touching the floor
        public bool                     isOnFloor = true;
        private float                   hitDistance = .35f;
        public float                    hitDistanceMin = .45f;
        public float                    hitDistanceMax = .75f;
        public LayerMask                myLayer;
        public Vector3                  rayPosition = Vector3.zero;

        public PhysicsMaterial           pMove;
        public PhysicsMaterial           pStop;
        public PhysicsMaterial           pIce;
        private CapsuleCollider         charCol;

        // Use to know if the player touching something. Use to if the character is grounded
        public LayerMask                myLayer02;
        public float                    overlapSize = .2f;
        public float                    overlapPos = .11f;
        public bool                     b_Overlap = false;

        //Layers 12 and 17. Use to know if the character is touching a door or a drawer
        public bool                     b_TouchLayer12_17 = false;

        public int                      jumpForce = 4;
        public float                    jumpSpeed = 10;
        public bool                     b_IsJumping = false;
        public float                    minimumJump = .2f;

        public float                    GravityFallSpeed = 4;
        public float                    heightRoof = .45f;

        public float                    fallCurve;
        public AnimationCurve           animFallCurve;

        public KeyCode                  upKey       = KeyCode.E;
        public KeyCode                  downKey     = KeyCode.D;
        public KeyCode                  leftKey     = KeyCode.S;
        public KeyCode                  rightKey    = KeyCode.F;

        public KeyCode                  runKey      = KeyCode.LeftShift;
        public KeyCode                  jumpKey     = KeyCode.Space;
        public KeyCode                  crouchKey   = KeyCode.C;

        Vector3                         joyInput = Vector3.zero;
        public bool                     isMovementAllowed = true;

        private void Start()
        {
            #region 
            refScaleCrouch = gameObject.transform.localScale.y;     // Save the character standing size
            charCol = GetComponent<CapsuleCollider>(); 
            #endregion
        }


        public void charaGeneralMovementController()
        {
            bodyMovement();
        }

        void Update()
        {
            #region 
            if (isMovementAllowed)
            {
                if (Input.GetKeyDown(jumpKey) && !b_IsJumping && isOnFloor)
                    StartCoroutine(Jump());

                joyInput = new Vector3(0, 0, 0);

                if (joyInput.sqrMagnitude > 1.0f)
                    joyInput = joyInput.normalized;

                mouseHorizontal = Input.GetAxis(s_mouseAxisX);
                mouseVertical = Input.GetAxis(s_mouseAxisY);

                XAxis = returnDesktopXAxis();
                YAxis = returnDesktopYAxis();

                mouseInputX = Input.GetAxis("Mouse X");

                bodyRotation();
                cameraRotation();

                CheckCrouch();
                CheckRun();
            }
            #endregion
        }

        void CheckCrouch()
        {
            #region 
            if (allowCrouch)
            {
                if (Input.GetKeyDown(crouchKey))
                {
                    if (b_Crouch && AP_CheckIfPlayerCanStopCrouching() || !b_Crouch)
                    {
                        b_Crouch = !b_Crouch;
                    }

                }
            } 
            #endregion
        }

        void CheckRun()
        {
            #region Run
            if (b_AllowRun)
            {
                if (Input.GetKey(runKey) && !b_Crouch)
                {
                    isRunning = true;
                    currentSpeedMultiplier = speedMultiplier;
                }
                else
                {
                    isRunning = false;
                    currentSpeedMultiplier = 1;
                }
            }
            #endregion
        }

        private void FixedUpdate()
        {
            #region 
            AP_OverlapSphere();
            Ap_isOnFloor();
            AP_ApplyGravity();

            CrouchUpdate(); 
            #endregion
        }

        void CrouchUpdate()
        {
            #region 
            // Crouch: Check if the character scale need to be updated
            if (allowCrouch)
            {
                if (b_Crouch && gameObject.transform.localScale.y != targetScaleCrouch)
                {
                    gameObject.transform.localScale = Vector3.MoveTowards(gameObject.transform.localScale,
                                                                          new Vector3(gameObject.transform.localScale.x, targetScaleCrouch, gameObject.transform.localScale.z),
                                                                          Time.deltaTime * crouchSpeed);
                }
                else if (!b_Crouch && gameObject.transform.localScale.y != refScaleCrouch)
                {
                    gameObject.transform.localScale = Vector3.MoveTowards(gameObject.transform.localScale,
                                                                          new Vector3(gameObject.transform.localScale.x, refScaleCrouch, gameObject.transform.localScale.z),
                                                                          Time.deltaTime * crouchSpeed);
                }
            } 
            #endregion
        }

        //--> Desktop Case : Body rotation
        private void bodyRotation()
        {
            #region
            if (mouseHorizontal != 0)
            {
                tmpXAxis = mouseInputX * 1.1f;
                tmpXAxis *= sensibilityMouse * 1.2f;
            }
            else
            {
                tmpXAxis = 0;
            }

            objCamera.transform.Rotate(0, tmpXAxis, 0);
            #endregion
        }

        private void cameraRotation()
        {
            #region
            if (mouseVertical != 0)
            {
                tmpYAxis = mouseVertical;

                tmpYAxis = Mathf.Clamp(tmpYAxis, -3f, 3f);
                tmpYAxis *= 1.5f;

                mouseY -= tmpYAxis * sensibilityMouse * returnInvertMouseAxis() * 1.2f;
            }

            mouseY = Mathf.Clamp(mouseY, minimum, maximum);

            objCamera.localEulerAngles = new Vector3(
                mouseY,
                objCamera.localEulerAngles.y,
                0);
            #endregion
        }

        void bodyMovement()
        {
            #region Move the character left right forward backward

            addForceObj.transform.localEulerAngles
            = new Vector3(addForceObj.transform.localEulerAngles.x,
                objCamera.transform.localEulerAngles.y,
                addForceObj.transform.localEulerAngles.z);

            Vector3 Direction = new Vector3(0, 0, 0);


            Direction += FindTangentX() * XAxis;

            Direction += FindTangentZ() * YAxis;


            if (preventClimbing.b_preventClimbing)
            {
                Direction.y = 0;
            }

            //Debug.Log("smoothStart: " + smoothStart);

            if (isOnFloor)
            {
                if (currentAngle >= 180 - MaxAngle)
                    rbBodyCharacter.AddForceAtPosition(Direction * characterSpeed * currentSpeedMultiplier, addForceObj.transform.position, ForceMode.Force);            // move the character

                Vector3 opposite = rbBodyCharacter.transform.InverseTransformDirection(-rbBodyCharacter.linearVelocity);                          // Opposite force to stop the character

                rbBodyCharacter.AddRelativeForce(opposite * BrakeForce * Coeff, ForceMode.Force);


                if (rbBodyCharacter.linearVelocity.magnitude > MaxSpeed)
                    rbBodyCharacter.linearVelocity = rbBodyCharacter.linearVelocity.normalized * MaxSpeed;
            }
            else
            {
                rbBodyCharacter.AddForceAtPosition(Direction * characterSpeed * currentSpeedMultiplier, addForceObj.transform.position, ForceMode.Force);            // move the character 

                Vector3 opposite = rbBodyCharacter.transform.InverseTransformDirection(new Vector3(-rbBodyCharacter.linearVelocity.x ,0,- rbBodyCharacter.linearVelocity.z));                          // Opposite force to stop the character

                rbBodyCharacter.AddRelativeForce(opposite * BrakeForce * Coeff, ForceMode.Force);


                if (rbBodyCharacter.linearVelocity.magnitude > MaxSpeed)
                    rbBodyCharacter.linearVelocity = rbBodyCharacter.linearVelocity.normalized * MaxSpeed;

            }

           

            #endregion
        }

        private float returnDesktopXAxis()
        {
            #region
            float result = currentDesktop_X_Axis;
            bool b_PressKey = false;
            if (Input.GetKey(leftKey))
            {
                if (result > 0)
                    result = 0;
                result = Mathf.MoveTowards(result, -1, Time.deltaTime * speedKeybordMovement);
                b_PressKey = true;
            }
            if (Input.GetKey(rightKey))
            {
                if (result < 0)
                    result = 0;
                result = Mathf.MoveTowards(result, 1, Time.deltaTime * speedKeybordMovement);
                b_PressKey = true;
            }

            if (!b_PressKey)
            {
                result = Mathf.MoveTowards(result, 0, Time.deltaTime * speedKeybordMovement * 2);
            }

            currentDesktop_X_Axis = result;
            return result; 
            #endregion
        }

        private float returnDesktopYAxis()
        {
            #region 
            float result = currentDesktop_Y_Axis;
            bool b_PressKey = false;
            if (Input.GetKey(downKey))
            {
                if (result > 0)
                    result = 0;
                result = Mathf.MoveTowards(result, -1, Time.deltaTime * speedKeybordMovement);
                b_PressKey = true;
            }
            if (Input.GetKey(upKey))
            {
                if (result < 0)
                    result = 0;
                result = Mathf.MoveTowards(result, 1, Time.deltaTime * speedKeybordMovement);
                b_PressKey = true;
            }

            if (!b_PressKey)
            {
                result = Mathf.MoveTowards(result, 0, Time.deltaTime * speedKeybordMovement * 2);
            }

            currentDesktop_Y_Axis = result;

            return result; 
            #endregion
        }

        Vector3 FindTangentZ()
        {
            #region 
            Vector3 tangente = Vector3.zero;
            RaycastHit hit2;
            if (Physics.Raycast(tangentStartPosition.position, -Vector3.up, out hit2, 10, myLayerMask))
            {
                hit2.normal.Normalize();

                //Debug.DrawRay(hit2.point, hit2.normal , Color.white);
                tangente = Vector3.Cross(hit2.normal, -addForceObj.transform.right);

                if (tangente.magnitude == 0)
                { tangente = Vector3.Cross(hit2.normal, Vector3.up); }
                Debug.DrawRay(hit2.point, tangente, Color.yellow);
            }
            //Debug.Log (tangente);
            return tangente; 
            #endregion
        }

        Vector3 FindTangentX()
        {
            #region 
            Vector3 tangente = Vector3.zero;
            RaycastHit hit2;
            if (Physics.Raycast(tangentStartPosition.position, -Vector3.up, out hit2, 10, myLayerMask))
            {
                hit2.normal.Normalize();

                Vector3 myDirection = Vector3.Cross(addForceObj.transform.right, hit2.normal);

                Debug.DrawRay(hit2.point, hit2.normal, Color.white);
                tangente = Vector3.Cross(hit2.normal, myDirection);

                if (tangente.magnitude == 0)
                    tangente = Vector3.Cross(hit2.normal, Vector3.up);

                Debug.DrawRay(hit2.point, tangente, Color.red);
            }
            //Debug.Log (tangente);
            return tangente; 
            #endregion
        }

        int returnInvertMouseAxis()
        {
            #region 
            int result = 1;
            return result; 
            #endregion
        }

        public void charaStopMoving()
        {
            #region  Stop moving the character is pause is activated
            if (rbBodyCharacter.linearVelocity != Vector3.zero)
                rbBodyCharacter.linearVelocity = Vector3.zero;
            #endregion
        }

        private void AP_ApplyGravity()
        {
            #region Apply Gravity
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * .1f, -Vector3.up, out hit, 100.0f))
            {
                if (isOnFloor)
                {
                    currentAngle = Vector3.SignedAngle(hit.normal, -Vector3.up, Vector3.up);
                    gravityScale = 1 - (180 - currentAngle) / 80;
                    circlePos = hit.point;
                }
                else
                {
                    //gravityScale = 1;
                }
            }


            if (b_TouchLayer12_17 && isOnFloor)  // Character is touching door or drawer
            {
                charCol.material = pIce;
                gravityScale = 0;

                rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
            }
            else if (currentAngle < 180 - MaxAngle || !isOnFloor)
            {
                charCol.material = pIce;
                fallCurve = Mathf.MoveTowards(fallCurve, 1, Time.deltaTime);
                gravityScale = Mathf.MoveTowards(gravityScale, 20, animFallCurve.Evaluate(fallCurve) * GravityFallSpeed * Time.deltaTime);

                rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
            }
            else if (YAxis == 0 && XAxis == 0)
            {
                charCol.material = pStop;
                rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
                gravityScale = 0;
            }
            else if (YAxis == 0 && XAxis != 0)
            {
                charCol.material = pMove;
                rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
                gravityScale = 0;
            }
            else
            {
                charCol.material = pMove;
                rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
            }

            if (rbBodyCharacter.linearVelocity.sqrMagnitude * 10000 < 2 && YAxis == 0 && XAxis == 0 && isOnFloor)
            {
                rbBodyCharacter.constraints =
                    RigidbodyConstraints.FreezePositionX |
                    RigidbodyConstraints.FreezePositionY |
                    RigidbodyConstraints.FreezePositionZ |
                    RigidbodyConstraints.FreezeRotation;

                gravityScale = 0;
            }

            if (b_IsJumping)
            {
                charCol.material = pIce;
                rbBodyCharacter.constraints = RigidbodyConstraints.FreezeRotation;
               // gravityScale = 0;
            }

            Vector3 gravity = globalGravity * gravityScale * Vector3.up;
            rbBodyCharacter.AddForce(gravity, ForceMode.Acceleration);
            #endregion
        }

        public void Ap_isOnFloor()
        {
            #region isOnFloor: Check if the character is touching the floor
            float offset = .6f * (180 - currentAngle) / 80;

            if (isOnFloor)
                hitDistance = hitDistanceMax + offset;
            else
                hitDistance = hitDistanceMin + offset;

            if (Physics.Raycast(transform.position + Vector3.up * .1f, -Vector3.up, hitDistance, myLayer))
            {
                if (b_Overlap)
                    isOnFloor = true;
                rayPosition = transform.position + Vector3.up * .1f;
            }
            else
            {
                if (b_Overlap)
                    isOnFloor = false;
                rayPosition = transform.position;
            }
            #endregion
        }

        private void AP_OverlapSphere()
        {
            #region OverlapSphere: Check if the character is touching something. If not we consider the character is not touching the floor.
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * overlapPos, overlapSize, myLayer02);

            if (hits.Length > 0)
            {
                b_Overlap = true;
            }
            else
            {
                b_Overlap = false;
                isOnFloor = false;
            }
            #endregion
        }

        private bool AP_CheckIfPlayerCanStopCrouching()
        {
            #region Check if the height above the character is enough to leave the crouch mode
            Debug.DrawRay(transform.position + Vector3.up * .1f, Vector3.up * heightCheck, Color.yellow);
            if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.up, heightCheck, layerCheckCrouch))
                return false;
            else
                return true;
            #endregion
        }


        void OnCollisionEnter(Collision collision)
        {
            #region Check if the player is on collision with a door or a drawer
            if (collision.gameObject.layer == 12 || collision.gameObject.layer == 17)
            { b_TouchLayer12_17 = true; } 
            #endregion
        }
        void OnCollisionStay(Collision collision)
        {
            #region 
            if (collision.gameObject.layer == 12 || collision.gameObject.layer == 17)
            { b_TouchLayer12_17 = true; }

            if (collision.gameObject.layer == 18)   // Specific objsFloor: Prevent Player to be stuck. If the player is on collision with this layer: It means that the player touch the floor.
            { /*Debug.Log("Border"); */isOnFloor = true; } 
            #endregion
        }

        void OnCollisionExit(Collision collision)
        {
            #region 
            if (collision.gameObject.layer == 12 || collision.gameObject.layer == 17)
            { b_TouchLayer12_17 = false; } 
            #endregion
        }

        private void OnDrawGizmos()
        {
            #region 
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(circlePos, .1f);

            if (rayPosition == transform.position)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.blue;

            Gizmos.DrawSphere(rayPosition, .1f);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transform.position + Vector3.up * .334f, overlapSize);
            #endregion
        }


        public float jumpMulti01 = 1;
        public float jumpMulti02 = 1;
        public IEnumerator Jump()
        {
            #region
            fallCurve = 0;
            float t = 0;
            b_IsJumping = true;
            bool keyUp = false;

           // 

            rbBodyCharacter.AddForceAtPosition(Vector3.up * 5 * jumpMulti01, addForceObj.transform.position, ForceMode.Impulse);
            while(t<.5f)
            //while (t != jumpForce)
            {
                if (Input.GetKeyUp(jumpKey))
                    keyUp = true;

                if (AP_CheckIfPlayerIsTouchingRoof() || keyUp /*&& t > jumpForce * minimumJump*/)
                {
                    //Stop Jump
                    //t = jumpForce;
                    t = 2;
                }
                else
                {
                    // rbBodyCharacter.AddForceAtPosition(Vector3.up * t * Time.deltaTime * 50, addForceObj.transform.position, ForceMode.Impulse);            // move the character


                    //t = Mathf.MoveTowards(t, jumpForce, Time.deltaTime * jumpSpeed);
                    float jumpBoostYIfPlayerIsMoving = 1f - rbBodyCharacter.linearVelocity.normalized.y;
                    rbBodyCharacter.AddForceAtPosition(Vector3.up * (.25f + .25f * jumpBoostYIfPlayerIsMoving) * Time.deltaTime * 100 * jumpMulti02, addForceObj.transform.position, ForceMode.Impulse);
                    t += Time.deltaTime;
                }

                yield return new WaitForEndOfFrame();
            }

            b_IsJumping = false;
            fallCurve = 0;

            yield return null; 
            #endregion
        }

       // public bool TouchRoof;

        private bool AP_CheckIfPlayerIsTouchingRoof()
        {
            #region Check If Player touch the roof (jump check)
            if (Physics.Raycast(refHead.transform.position + Vector3.up * .1f, Vector3.up, heightRoof, layerCheckCrouch))
            {
                //Debug.Log(TouchRoof);
                //TouchRoof = true;
                return true;
            }

            else
            {
                //Debug.Log(TouchRoof);
                //TouchRoof = false;
                return false;
            }
                
            #endregion
        }

        public void ResetMovement()
        {
            #region 
            currentDesktop_X_Axis = 0;
            currentDesktop_Y_Axis = 0;
            YAxis = 0;
            XAxis = 0;
            isRunning = false;
            gravityScale = 0;
            isMovementAllowed = true;
            charCol.material = pStop;
            rbBodyCharacter.linearVelocity *=0;
            rbBodyCharacter.angularVelocity *=0;
            #endregion
        }
    }
}
