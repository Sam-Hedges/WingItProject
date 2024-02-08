using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {

        private CharacterController CharacterController => GetComponent<CharacterController>();
        private Camera _mainCam;

        [Header("Movement")] 
        public string yes;
        public bool isMoving
        {
            get => _isMovementPressed;
            set => _isMovementPressed = value;
        }
        private bool IsGrounded => CharacterController.isGrounded;
        public bool jumped;
        [SerializeField] private float playerSpeed = 4.5f;
        [SerializeField] private float playerSprintSpeed = 10f;
        [SerializeField] private float sprintTransitionSpeed = 0.1f;
        [SerializeField] private float jumpHeight;
        [SerializeField] private float rotationFactorPerFrame = 15f;
        [SerializeField, Range(0f, 50f)] private float gravity = 35f;
        [SerializeField] private Vector3 velocity;

        [Header("Dash")]
        public bool dashed;
        [SerializeField] private float dashSpeed;
        [SerializeField] private float dashTime;

        [Header("Attack")]
        [SerializeField] private Vector3 attackOrigin;
        [SerializeField] private float attackDistance;
        [SerializeField] private float attackArc;

        [Header("Animation")]
        public Animator animator;
        [SerializeField] private Vector2 idleActionRandTime = new Vector2(30, 60);
        private bool idleAction = false;
        private Coroutine idle;
        public float sprintTransTime = 0f;

        [Header("Audio")]
        public AudioSource audioFootsteps;

        public static PlayerController instance;
        
        //Camera Directions
        private Vector3 CameraTransformForward => ScaleCameraTransform(_mainCam.transform.forward);
        private Vector3 CameraTransformRight => ScaleCameraTransform(_mainCam.transform.right);
        private Vector3 ScaleCameraTransform(Vector3 cameraTransform)
        {
            return Vector3.Scale(cameraTransform.normalized, new Vector3(1, 0, 1));
        }
        
            
        // Player Action Asset
        private PlayerActions _inputAction;
        
        // Player movement related variables
        private Vector2 _movementInputVector;
        private Vector3 _movementOutputVector;
        private bool _isMovementPressed;
        
        //Cached Animation Property Indexes
        private static readonly int a_Jumped = Animator.StringToHash("jumped");
        private static readonly int a_MoveSpeed = Animator.StringToHash("moveSpeed");
        private static readonly int a_IsGrounded = Animator.StringToHash("isGrounded");
        private static readonly int a_Moving = Animator.StringToHash("moving");
        private static readonly int a_IdleAction = Animator.StringToHash("idleAction");
        private static readonly int a_Attack1 = Animator.StringToHash("attack");
        private static readonly int a_Dash1 = Animator.StringToHash("dash");

        [ExecuteInEditMode]
        void OnDrawGizmos()
        {
            // Draws the bounds of the attack in the editor based of the attack variables
            #region AttackGizmo

            // Sets the gizmo colour
            Gizmos.color = Color.blue;

            // Gets the current player position and adds on the attack origin vector to find position the attack will originate from
            Vector3 pos = transform.position + attackOrigin;

            // Draws two lines exteneding outwards based of the attack origin and the distance of the attack
            Gizmos.DrawLine(pos, pos + new Vector3(-attackArc, 0, attackDistance));
            Gizmos.DrawLine(pos, pos + new Vector3(attackArc, 0, attackDistance));

            // If the attack vectors are not perpendicular then a line is draw between them to represent the distance between
            if (attackArc != 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pos + new Vector3(-attackArc, 0, attackDistance), pos + new Vector3(attackArc, 0, attackDistance));
            }

            #endregion
        }

        void Awake()
        {
            // Creates the static variable instance to the current player controller so that other scripts within the scene can reference the player easier
            instance = GetComponent<PlayerController>();
            _mainCam = Camera.main; 
            
            _inputAction = new PlayerActions();

            _inputAction.PlayerControls.Movement.started += OnMovementInput;
            _inputAction.PlayerControls.Movement.canceled += OnMovementInput;
            _inputAction.PlayerControls.Movement.performed += OnMovementInput;

        }

        void OnMovementInput(InputAction.CallbackContext context)
        {
            _movementInputVector = context.ReadValue<Vector2>();
            _movementOutputVector = CameraTransformForward * _movementInputVector.y + CameraTransformRight * _movementInputVector.x;
            _isMovementPressed = _movementInputVector != Vector2.zero;
        }
        
        void OnEnable()
        {
            _inputAction.PlayerControls.Enable(); // Enable PlayerControls Action Map
        }
        
        void OnDisable()
        {
            _inputAction.PlayerControls.Disable(); // Disable PlayerControls Action Map
        }
        
        void Start()
        {
            // Sets the current camera to main in case a camera doesn't get assigned in the editor
            if (_mainCam == null) { _mainCam = Camera.main; }
        }

        void Update()
        {
            InputMagnitude(_movementInputVector);
            if(_isMovementPressed) { HandleMovement(playerSpeed); }
            Gravity();
            FootstepAudio();
        }

        void HandleAnimation()
        {
            
        }

        // Adds the force of the gravity variable over time to the vertical axis of the player so the get push down if in mid air
        void Gravity()
        {

            // Works out terminal velocity of pigeon based on the gravity
            float terminalVelocity = -(Mathf.Sqrt(3f * 900f * gravity / 1.6f * 1200f * 0.4f) / 1000);

            // if the player is not grounded increase the vertical velocity
            if (!IsGrounded) { velocity.y += -gravity * Time.deltaTime; }

            //If the pigeon falls faster than terminal velocity then the fall is clamped
            if (velocity.y < terminalVelocity) { velocity.y = terminalVelocity; }

            // if the player is grounded reset the velocity
            if (IsGrounded && velocity.y < 0f)
            {
                velocity.y = 0f;

                animator.SetTrigger(a_IsGrounded);
            }

            // applies the new velocity vector to the character controller
            CharacterController.Move(velocity * Time.deltaTime);
        }


        // Applies jump force to the player when called
        void Jump()
        {
            // Applies a positive force to the y of the velocity so that the player jumps and so the Gravity() method can add a downwards force over time
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * -gravity);

            animator.SetTrigger(a_Jumped);
        }


        /// <summary>
        /// Applies movement to the player character based on the players input
        /// </summary>
        /// <param name="speed"></param>
        /// <param name="dash"></param>
        private void HandleMovement(float speed, bool dash = false)
        {
            // If player dashes, additional speed is added to the movement
            speed *= dash ? dashSpeed : 1; 
            
            // Calculate final movement Vector
            Vector3 moveDirection = Vector3.ClampMagnitude(_movementOutputVector, 1f) * speed * Time.deltaTime;
            
            HandleRotation(moveDirection);
            
            // Inputs the final movement vector to the character controller component
            CharacterController.Move(moveDirection);
        }

        /// <summary>
        /// Lerps the player characters rotation to match its movement direction
        /// </summary>
        private void HandleRotation(Vector3 i)
        { 
            Debug.Log(_movementInputVector);
            
            Quaternion currentRotation = transform.rotation;
            
            Quaternion targetRotation = Quaternion.LookRotation(i);
            
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }

        // Receives the inputs that the player makes and acts upon them calling the corresponding methods
        private void InputMagnitude(Vector2 input)
        {
            // When the movement axis are activated the Move() method is called
            #region Movement

            // This stops the player character rotating back to forward relative to the camera when the player isn't moving
            if (_isMovementPressed)
            {
                // Changes the move speed whether the player is sprinting or not
                float finalSpeed = _inputAction.PlayerControls.Sprint.GetButton() ? playerSprintSpeed : playerSpeed;
                
                if (_inputAction.PlayerControls.Sprint.GetButton()) // If the player is sprinting then set the correct move speed
                {
                    if (sprintTransTime < 0.999f)
                    {
                        sprintTransTime = Mathf.Lerp(sprintTransTime, 1f, sprintTransitionSpeed); // Lerps between from the walk anim to the sprint anim
                    }
                    else
                    {
                        sprintTransTime = 1f;
                    }

                    animator.SetFloat(a_MoveSpeed, sprintTransTime);
                }
                else // If the player is walking then set the correct move speed
                {
                    if (sprintTransTime > 0.001f)
                    {
                        sprintTransTime = Mathf.Lerp(sprintTransTime, 0f, sprintTransitionSpeed); // Lerps between from the sprint anim to the walk anim
                    }
                    else
                    {
                        sprintTransTime = 0f;
                    }

                    animator.SetFloat(a_MoveSpeed, sprintTransTime);
                }

                // Stops the Idle Action coroutine if the player is moving
                if (idleAction)
                {
                    StopCoroutine(idle);
                    idleAction = false;
                }
                
                //HandleRotation();
                
                HandleMovement(finalSpeed);

                // Sets the animator parameter "moving" to true and the script variable "moving" to true
                animator.SetBool(a_Moving, true);
                isMoving = true;

            }

            if (!_isMovementPressed)
            {
                // Sets the animator parameter "moving" to false and the script variable "moving" to false
                animator.SetBool(a_Moving, false);
                isMoving = false;

                // Starts the Idle Action coroutine if the player is not moving
                if (!idleAction)
                {
                    idle = StartCoroutine(TriggerIdle());
                    idleAction = true;
                }
            }

            #endregion

            // When the dash button is pressed the Dash() coroutine is triggered
            if (_inputAction.PlayerControls.Dash.GetButtonDown() && !jumped && _isMovementPressed && !dashed)
            {
                dashed = true;

                StartCoroutine(Dash(input));

                animator.SetTrigger(a_Dash1);
            }

            // When the jump button is pressed down the Jump() method is called
            if (_inputAction.PlayerControls.Jump.GetButtonDown() && !jumped)
            {
                jumped = true;

                Jump();
            }

            // When the jump button is pressed down the Jump() method is called
            if (_inputAction.PlayerControls.Attack.GetButtonDown() && !jumped)
            {

                Attack();
            }
        }


        // Gets called when the player makes no movement input to trigger the secondary idle animation
        IEnumerator TriggerIdle()
        {
            // Generates a random integer used as a variable for the wait time before the rest of the corountine can continue
            float time = Random.Range(idleActionRandTime.x, idleActionRandTime.y);
            yield return new WaitForSeconds(time);

            // Triggers the players' animator parameter "idleAction" to activate the secondary idle animation
            animator.SetTrigger(a_IdleAction);

            // Sets idle action back to false so that the coroutine will be run again in the InputMagnitude() method
            idleAction = false;

            // Ends the coroutine
            yield break;
        }


        // Dash coroutine
        IEnumerator Dash(Vector2 input)
        {
            // Gets the starting time of the dash
            float startTime = Time.time;

            animator.SetFloat(a_MoveSpeed, 1f);

            // While the current time is smaller than the (start time + the dash time) then run the code within the while loop
            while (Time.time < startTime + dashTime)
            {
                /* Moves the character in the last faced direction and tells the Move() method that this 
               movement is currently a dash so it uses the dash speed rather than the default speed */
                HandleMovement(playerSpeed, true);

                // Forces the coroutine to wait until the next frame until it can run again. This makes the while loop act as a update while active
                yield return null;
            }

            // Tells the rest of the script that the dash has finished
            dashed = false;

            // Ends the coroutine
            yield break;
        }


        void FootstepAudio()
        {
            if ((!isMoving || jumped) && audioFootsteps.isPlaying)
            {
                audioFootsteps.Stop();
            }
            else if (isMoving && !audioFootsteps.isPlaying)
            {
                audioFootsteps.Play();
            }
        }


        // Used to get the normalized forward direction of the player based of the camera direction so that forward is always away from the player, etc...

        void Attack()
        {
            animator.SetTrigger(a_Attack1);
        }

        public void Damaged(float damage)
        {

        }

    }
}