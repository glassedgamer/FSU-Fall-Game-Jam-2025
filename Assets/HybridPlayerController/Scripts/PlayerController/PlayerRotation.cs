using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace HybridPlayerController
{

    public class PlayerRotation : MonoBehaviour
    {
        private PlayerController player;

        [Header("References")]
        public GameObject cam;
        private CamUtils camUtils;
        public GameObject camContainer;

        [Header("Sensitivity")]
        [Range(5f, 500f)] public float joystickSensitivity = 100f;
        [Range(5f, 500f)] public float mouseSensitivity = 100f;

        private float visualSmoothSpeed = 10f;

        [ReadOnly] public bool canRotateRoot = true;
        [ReadOnly] public bool canRotateVisual = true;
        [ReadOnly] public bool canRotateCam = true;

        private float yaw;
        private float pitch;
        private float camYaw;
        private bool isSnapping = false;

        //for first person rotation on a moving platform
        private Transform _platformParent;
        private Quaternion _platformPrevRot;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (cam != null)
                camUtils = cam.GetComponent<CamUtils>();

            yaw = transform.eulerAngles.y;
            camYaw = yaw;
            pitch = camContainer.transform.rotation.eulerAngles.x;
        }

        private void Update()
        {
            HandleLookInput();
        }
        private void LateUpdate()
        {
            //Apply rotation in late update so all player movement is updated before rotating
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                InheritPlatformYaw();
                ApplyFirstPerson();
            }
            else if (player.mode == PerspectiveMode.ThirdPerson)
                ApplyThirdPerson();
        }
        private void HandleLookInput()
        {
            Vector2 lookInput = player.playerControls.BaseMovement.Look.ReadValue<Vector2>();
            float inputYaw = lookInput.x * mouseSensitivity * Time.deltaTime;
            float inputPitch = lookInput.y * mouseSensitivity * Time.deltaTime;

            if (player.mode == PerspectiveMode.FirstPerson)
            {
                if (canRotateRoot)
                {
                    yaw += inputYaw;
                    camYaw = yaw;
                    pitch -= inputPitch;
                }
            }
            else //third-Person
            {
                if (canRotateCam)
                {
                    camYaw += inputYaw;
                    pitch -= inputPitch;
                }
                if (!isSnapping)
                    yaw = transform.eulerAngles.y;
            }

            float minPitch = (player.mode == PerspectiveMode.FirstPerson)
                ? camUtils.firstPersonClamp.x
                : camUtils.thirdPersonClamp.x;
            float maxPitch = (player.mode == PerspectiveMode.FirstPerson)
                ? camUtils.firstPersonClamp.y
                : camUtils.thirdPersonClamp.y;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        private void ApplyFirstPerson()
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            camContainer.transform.rotation = Quaternion.Euler(pitch, yaw, camContainer.transform.rotation.eulerAngles.z);
        }

        private void ApplyThirdPerson()
        {
            Vector2 moveInput = player.playerControls.BaseMovement.Move.ReadValue<Vector2>();
            Transform visual = player.thirdPersonVisual.transform;
            if (moveInput.sqrMagnitude > 0.01f && canRotateRoot && !isSnapping)
            {
                Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                Vector3 worldDir = camContainer.transform.TransformDirection(moveDir);
                worldDir.y = 0f;
                worldDir.Normalize();

                //store old root rotation and compute target
                Quaternion oldRootRot = transform.rotation;
                Quaternion targetRootRot = Quaternion.LookRotation(worldDir);

                //rotate the root instantly for gameplay
                transform.rotation = targetRootRot;

                //counter-rotate the visual to negate the root's rotation change
                Quaternion deltaRot = targetRootRot * Quaternion.Inverse(oldRootRot);
                visual.rotation = Quaternion.Inverse(deltaRot) * visual.rotation;
            }
            
            if (canRotateVisual)
            {
                //smoothly rotate the visual mesh toward the new root rotation
                visual.rotation = Quaternion.Slerp(visual.rotation, transform.rotation, visualSmoothSpeed * Time.deltaTime);
            }

            camContainer.transform.rotation = Quaternion.Euler(pitch, camYaw, 0f);
        }

        //for first person rotation on a moving platform
        private void InheritPlatformYaw()
        {
            Transform p = transform.parent;

            if (p == null)
            {
                _platformParent = null;
                return;
            }

            if (_platformParent != p)
            {
                _platformParent = p;
                _platformPrevRot = p.rotation;
                return;
            }

            if (isSnapping)
            {
                _platformPrevRot = p.rotation;
                return;
            }

            Quaternion current = p.rotation;
            float deltaYaw = Mathf.DeltaAngle(_platformPrevRot.eulerAngles.y, current.eulerAngles.y);

            if (Mathf.Abs(deltaYaw) > 0.0001f)
            {
                yaw += deltaYaw;
                camYaw = yaw;
            }

            _platformPrevRot = current;
        }
        //

        public void SnapRotation(float targetPitch, float targetYaw, float duration)
        {
            if (player.mode == PerspectiveMode.FirstPerson)
                StartCoroutine(SmoothSnapFP(targetPitch, targetYaw, duration));
            else
                StartCoroutine(SmoothSnapTP(targetYaw, duration));
        }

        public void SnapRotation(float targetPitch, Vector3 direction, float duration)
        {
            float dirYaw = Quaternion.LookRotation(direction).eulerAngles.y;
            SnapRotation(targetPitch, dirYaw, duration);
        }

        private IEnumerator SmoothSnapFP(float targetPitch, float targetYaw, float duration)
        {
            isSnapping = true;
            canRotateRoot = canRotateCam = false;

            float startPitch = pitch;
            float startYaw = yaw;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                pitch = Mathf.LerpAngle(startPitch, targetPitch, t);
                yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
                camYaw = yaw;
                ApplyFirstPerson();
                elapsed += Time.deltaTime;
                yield return null;
            }

            pitch = targetPitch;
            yaw = targetYaw;
            camYaw = yaw;
            ApplyFirstPerson();

            canRotateRoot = canRotateCam = true;
            isSnapping = false;
        }

        private IEnumerator SmoothSnapTP(float targetYaw, float duration)
        {
            isSnapping = true;
            canRotateRoot = false;
            canRotateCam = true;

            float startYaw = yaw;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                yaw = Mathf.LerpAngle(startYaw, targetYaw, t);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                player.thirdPersonVisual.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                camContainer.transform.rotation = Quaternion.Euler(pitch, camYaw, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            yaw = targetYaw;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            player.thirdPersonVisual.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            camContainer.transform.rotation = Quaternion.Euler(pitch, camYaw, 0f);

            canRotateRoot = true;
            isSnapping = false;
        }
    }
}
