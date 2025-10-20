using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace HybridPlayerController
{

    public enum PerspectiveMode { FirstPerson, ThirdPerson }

    [RequireComponent(typeof(GroundChecker), typeof(Rigidbody), typeof(PlayerUtils))]
    [RequireComponent(typeof(PlayerRotation), typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [HideInInspector] public HybridPlayerControls playerControls;
        public IState CurrentState { get; private set; }

        //State instances
        private Dictionary<Type, IState> _states; //State instances
        public IReadOnlyDictionary<Type, IState> AllStates => _states;//for playerUtils and unlockStateTrigger

        [Header("Player Settings")]
        [Header("Prefrences")]
        [Tooltip("The player’s perspective.")]
        public PerspectiveMode mode;

        [Tooltip("Enable/disable active state of the first person arms object (see References section in Inspector).")]
        public bool showArms;
        [Tooltip("Immersive arms don't follow the camera's pitch.")]
        public bool immersiveArms;
        [Tooltip("If true, the player cannot enter Walk State")]
        public bool SprintOnly;
        [Tooltip("If true, sprinting is toggled on with a button press")]
        public bool SprintToggleable;
        [Tooltip("The change in FOV when moving fast")]
        public float fastMoveFOVChange = 5;

        // — General Movement —
        [Space(4)]
        [Header("General Movement")]
        [Tooltip("How fast the player moves when walking.")]
        public float walkSpeed = 250;
        [Tooltip("How fast the player moves when running.")]
        public float sprintSpeed = 500;
        [Tooltip("How fast the player moves when crouch walking.")]
        public float crouchWalkSpeed = 200;
        [Tooltip("How fast the player moves along a ledge.")]
        public float ledgeMoveSpeed = 250;
        [Tooltip("How fast the player runs on a wall while sprinting.")]
        public float wallRunSpeedSprint = 15;
        [Tooltip("How fast the player slips down while wall running.")]
        public float wallRunDownForce = 3;

        // — Air Movement —
        [Space(4)]
        [Header("Air Movement")]
        [Tooltip("How fast the player moves when rising in air.")]
        public float risingMoveSpeed = 300;
        [Tooltip("How fast the player moves when falling in air.")]
        public float fallingMoveSpeed = 300;
        [Tooltip("Maximum falling speed in the FallingState.")]
        public float maxFallingSpeed = -15;
        [Tooltip("The downward force of gravity exclusive to the player.")]
        public float gravityForce = -30;

        // — Jumping & Vaulting —
        [Space(4)]
        [Header("Jumping & Vaulting")]
        [Tooltip("Upward force of the jump.")]
        public float jumpForce = 15;
        [Tooltip("How many jumps you can make in-air, after jumping off of the ground. (Double-jumping)")]
        public int extraJumps = 1;
        [Tooltip("Grace period allowing jumps after leaving a platform.")]
        public float coyoteTime = .3f;
        [Tooltip("Time it takes to vault.")]
        public float vaultTime = 0.3f;
        [Tooltip("Upward force of the vault-jump.")]
        public float vaultJumpUpForce = 10;
        [Tooltip("Forward force of the vault-jump.")]
        public float vaultJumpForwardForce = 15;

        // — Diving & Sliding —
        [Space(4)]
        [Header("Diving & Sliding")]
        [Tooltip("Forward force of the dive.")]
        public float diveForce = 15;
        [Tooltip("Initial speed of a slide.")]
        public float slideSpeed = 15;
        [Tooltip("Rate of speed reduction while sliding.")]
        public float slideReductionRate = 10f;
        [Tooltip("The amount of time it takes, after a slide, to be able to slide again.")]
        public float slideDelay = 3f;



        [Space(50)]
        [Header("     Read Only")]
        [ReadOnly] public bool isSprinting;
        [ReadOnly] public bool isGrounded;
        [ReadOnly] public bool isOnSlope;
        [ReadOnly] public bool isOnSteepSlope;
        [ReadOnly] public bool HasCeilingAbove;
        [ReadOnly] public bool canMove = true;
        [ReadOnly] public bool canSlide = true;
        [ReadOnly] public bool justJumped;
        [ReadOnly] public bool justWallRanRight;
        [ReadOnly] public bool justWallRanLeft;
        [ReadOnly] public bool wallRight;
        [ReadOnly] public bool wallLeft;
        [ReadOnly] public int extraJumpCount;
        [ReadOnly] public Vector3 moveInput;
        [ReadOnly] public float moveSpeed = 0;
        [ReadOnly] public Vector3 moveVector;
        [ReadOnly] public Transform checkpoint;

        [Space(50)]
        [Header("References")]
        public GameObject cam;
        public GameObject camContainer;
        public GameObject thirdPersonVisual;
        public PlayerUtils playerUtils;
        public LayerMask platformingLayer;
        public LayerMask worldLayer;
        public LayerMask playerLayer;
        public GameObject firstPersonVisualArms;//for crouch state and slide state and animation
        public Transform firstPersonHandPos;
        public Transform thirdPersonHandPos;
        [HideInInspector] public GroundChecker groundChecker;
        [HideInInspector] public WallChecker wallChecker;
        [HideInInspector] public Rigidbody rb;
        [HideInInspector] public ConstantForce gravityComponent;
        public Animator firstPersonAnimator;
        public Animator thirdPersonAnimator;
        [HideInInspector] public Animator animator;//This is the active animator. Set based on the perspective mode on start
        [HideInInspector] public LineRenderer grappleLine;
        public GameObject crossHair;

        //for platforming
        [HideInInspector] public SwingBar mySwingBar;
        [HideInInspector] public SwingBar lastSwingBar;
        [HideInInspector] public Vector3 grapplePoint;
        [HideInInspector] public RaycastHit nextWallRunHit;
        [HideInInspector] public bool nextWallRunIsRight;
    
        private void Awake()
        {
            playerControls = new HybridPlayerControls();
            rb = GetComponent<Rigidbody>();
            gravityComponent = GetComponent<ConstantForce>();
            grappleLine = GetComponentInChildren<LineRenderer>();
            groundChecker = GetComponent<GroundChecker>();
            wallChecker = GetComponent<WallChecker>();

            //reflectively find and instantiate every IState
            var stateTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IState).IsAssignableFrom(t)
                            && !t.IsInterface
                            && !t.IsAbstract);

            _states = stateTypes.ToDictionary(
                t => t,
                t => (IState)Activator.CreateInstance(t)
            );

            //load unlock status on each
            foreach (var state in _states.Values)
                state.LoadUnlockStatus();
        }
        void OnEnable()
        {
            playerControls.Enable();
        }

        void OnDisable()
        {
            playerControls.Disable();
        }
        void Start()
        {
            canMove = true;
            gravityComponent.force = new Vector3(0, gravityForce, 0);
            CurrentState = GetState<IdleState>();
            CurrentState.EnterState(this);
            extraJumpCount = extraJumps;
            if(!showArms || mode == PerspectiveMode.ThirdPerson)
            {
                firstPersonVisualArms.SetActive(false);
            }
            else if (!immersiveArms)
                firstPersonVisualArms.transform.parent = cam.transform;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (mode == PerspectiveMode.FirstPerson)
                animator = firstPersonAnimator;
            else if (mode == PerspectiveMode.ThirdPerson)
                animator = thirdPersonAnimator;

            animator.Play("Idle");
        }

        void Update()
        {
            #if UNITY_6000_0_OR_NEWER
            float forwardVelocity = Vector3.Dot(rb.linearVelocity, transform.forward);
            #endif

            #if !UNITY_6000_0_OR_NEWER
            float forwardVelocity = Vector3.Dot(rb.velocity, transform.forward);
            #endif

            CurrentState.UpdateState(this);

            isGrounded = groundChecker.IsGrounded();
            isOnSlope = groundChecker.IsOnSlope();
            HasCeilingAbove = wallChecker.HasCeilingAbove();

            if (isGrounded)
            {
                //justJumped = false; //Jump related stuff is done per state
                //extraJumpCount = extraJumps; //Jump related stuff is done per state
                justWallRanRight = false;
                justWallRanLeft = false;
                lastSwingBar = null;
            }

            //Enableing isSprinting
            //isSprinting is currently used in IdleState, RisingState, and FallingState
            if (SprintOnly)
            {
                isSprinting = true;
            }
            else if (SprintToggleable)//Button toggle sprint
            {
                if (playerControls.BaseMovement.Sprint.triggered && !isSprinting)
                {
                    isSprinting = true;
                }
                else if (playerControls.BaseMovement.Sprint.triggered && isSprinting || playerControls.BaseMovement.Move.ReadValue<Vector2>() == Vector2.zero)
                {
                    isSprinting = false;
                }
            }
            else if (!SprintToggleable)//Button holding sprint
            {
                if (playerControls.BaseMovement.Sprint.IsPressed())
                {
                    isSprinting = true;
                }
                else
                {
                    isSprinting = false;
                }
            }
            else 
            {
                isSprinting = false;
            }
            //

            //Handle move input
            Vector3 moveDirection = new Vector2(playerControls.BaseMovement.Move.ReadValue<Vector2>().x, playerControls.BaseMovement.Move.ReadValue<Vector2>().y);
            moveInput = moveDirection.normalized;
            //
        }
        private void FixedUpdate()
        {
            CurrentState.FixedUpdateState(this);

            if (mode == PerspectiveMode.FirstPerson)
            {
                moveVector = GetMoveVector(moveInput, moveSpeed, transform.forward);
            }
            else//third person
            {
                Vector3 flatCamForward = Vector3.ProjectOnPlane(camContainer.transform.forward, Vector3.up).normalized;
                moveVector = GetMoveVector(moveInput, moveSpeed, flatCamForward);//So the player moves forward when pressing forward button always
            }
            if (canMove)
            {
                Move(moveVector);
            }
        }
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }
        public Vector3 GetMoveVector(Vector2 direction, float speed, Vector3 viewDir)
        {
            Vector3 right = Vector3.Cross(Vector3.up, viewDir).normalized;
            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
            Vector3 movement = (right * direction.x + forward * direction.y) * speed * Time.deltaTime;
            #if UNITY_6000_0_OR_NEWER
            return new Vector3(movement.x, rb.linearVelocity.y, movement.z);
            #endif

            #if !UNITY_6000_0_OR_NEWER
            return new Vector3(movement.x, rb.velocity.y, movement.z);
            #endif

        }
        public void Move(Vector3 moveVector)
        {
            //Debug.Log(isOnSlope);
            if (!isOnSlope)
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = moveVector;
                #endif

                #if !UNITY_6000_0_OR_NEWER
                rb.velocity = moveVector;
                #endif
            }
            else if (isOnSlope)//is on slope
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = groundChecker.slopeMoveVector;
                if (rb.linearVelocity.magnitude > moveSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
                }
                #endif

                #if !UNITY_6000_0_OR_NEWER
                rb.velocity = groundChecker.slopeMoveVector;
                if (rb.velocity.magnitude > moveSpeed)
                {
                    rb.velocity = rb.velocity.normalized * moveSpeed;
                }
                #endif

            }
        }

        //State Transitioning
        public void TransitionToState(IState newState)
        {
            if (newState.isUnlocked && newState != CurrentState)
            {
                if (playerUtils.logStateChanges)
                {
                    Debug.Log("Player exited " + CurrentState + " and entered " + newState + ".");
                }
                CurrentState.ExitState(this);
                CurrentState = newState;
                CurrentState.EnterState(this);
            }
            else
            {
                if (!newState.isUnlocked)
                {
                    Debug.Log("Cant Transition to " + newState + ". Its locked.");
                }
                if (newState == CurrentState)
                {
                    Debug.Log("Already in " + newState + ".");
                }
            }
        }
        public void TransitionToState<T>() where T : IState
        {
            TransitionToState(GetState<T>());
        }
        public T GetState<T>() where T : IState //generic used in states, groundChecker
        {
            return (T)_states[typeof(T)];
        }

        public IState GetState(Type stateType)//non-generic overload for playerUtils and unlockState trigger
        {
            if (_states.TryGetValue(stateType, out var state))
                return state;

            throw new KeyNotFoundException($"No IState registered for type {stateType}");
        }
        public bool IsInState<T>() where T : IState => CurrentState is T;
        //

        public void Die()
        {
            TransitionToState<IdleState>();
            GetComponent<PlayerRotation>().SnapRotation(0, checkpoint.rotation.eulerAngles.y, 0);
            transform.position = checkpoint.position;
        }

        public void ChangeAnimation(string animationName, float crossFadeDuration = .1f)
        {
            animator.CrossFade(animationName, crossFadeDuration);
        }
        void OnDrawGizmos()
        {
            #if UNITY_EDITOR
            if (CurrentState != null && playerUtils.drawCurrentState)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                style.alignment = TextAnchor.MiddleCenter;

                Vector3 textPositionA = transform.position + Vector3.up * 2f;
                UnityEditor.Handles.Label(textPositionA, "State: " + CurrentState.GetType().Name, style);
            }
            if (playerUtils.drawGroundChecker)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                style.alignment = TextAnchor.MiddleCenter;

                Vector3 textPositionB = transform.position + Vector3.up * 1.75f;
                UnityEditor.Handles.Label(textPositionB, "IsGrounded: " + isGrounded, style);
            }
            if ((IsInState<FallingState>() || IsInState<RisingState>() || IsInState<DiveState>()) && playerUtils.drawBarCheck)
            {
                //For the box in falling and rising state that checks for a swing bar
                Gizmos.color = Color.yellow;
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position + transform.forward * .5f, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(.5f, 2f, 2f));
                Gizmos.matrix = oldMatrix;
            }
            #endif
        }
    }
}
