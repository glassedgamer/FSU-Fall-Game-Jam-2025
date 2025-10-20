using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace HybridPlayerController
{

    public class CamUtils : MonoBehaviour
    {
        private Camera cam;
        private PlayerController playerController;
        private Coroutine currentFOVCoroutine;
        private Coroutine currentRollCoroutine;

        private GameObject myContainer;
        public Vector3 firstPersonPos;
        public Vector3 thirdPersonPos;
        [Tooltip("Offset of container when crouching. Only for first person mode")]
        public Vector3 containerCrouchPos;//for first person only
        [HideInInspector] public Vector3 containerDefaultPos = new Vector3(0, 0.75f, 0);
        [Tooltip("Clamp of the camera's pitch in first person")]
        public Vector2 firstPersonClamp;
        [Tooltip("Clamp of the camera's pitch in third person")]
        public Vector2 thirdPersonClamp;
        

        bool canResetPos = false;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            playerController = GetComponentInParent<PlayerController>();
            myContainer = transform.parent.gameObject;
        }

        private void Start()
        {
            if (playerController.mode == PerspectiveMode.ThirdPerson)
            {
                //Debug.Log("Player mode is third person, adjusting camera");
                playerController.thirdPersonVisual.SetActive(true); //Enable player visual
                cam.transform.localPosition = thirdPersonPos;//Change position of camera 
                //set rotation around a pivot point
            }
            else if(playerController.mode == PerspectiveMode.FirstPerson)
            {
                playerController.thirdPersonVisual.SetActive(false);
                cam.transform.localPosition = firstPersonPos;
                //set rotation around a pivot point
            }

        }
        private void Update()
        {
        
            //Checking if there is something blocking the camera in third person
            if (playerController.mode == PerspectiveMode.ThirdPerson)
            {
                Vector3 desiredWorldPos = myContainer.transform.TransformPoint(thirdPersonPos);

                Vector3 origin = myContainer.transform.position;
                Vector3 direction = desiredWorldPos - origin;
                float distance = direction.magnitude;
                float radius = .4f;

                LayerMask layersToIgnore = playerController.playerLayer | playerController.platformingLayer;

                if (Physics.SphereCast(origin, radius, direction.normalized, out RaycastHit hit, distance, ~layersToIgnore, QueryTriggerInteraction.Ignore))
                {
                    Vector3 pos = origin + direction.normalized * hit.distance;
                    cam.transform.position = pos;
                    canResetPos = true;
                }
                else if(canResetPos)
                {
                    cam.transform.position = desiredWorldPos;
                    canResetPos = false;
                }
            }
            //
        }

        public void ChangeFOV(float changeInFOV, float duration)
        {
            if (currentFOVCoroutine != null)
                StopCoroutine(currentFOVCoroutine);

            currentFOVCoroutine = StartCoroutine(SmoothChangeFOV(changeInFOV, duration));
        }

        public void ChangeRoll(float targetRoll, float duration)
        {
            if (currentRollCoroutine != null)
                StopCoroutine(currentRollCoroutine);

            currentRollCoroutine = StartCoroutine(SmoothChangeRoll(targetRoll, duration));
        }

        IEnumerator SmoothChangeFOV(float changeInFOV, float duration)
        {
            float elapsedTime = 0f;
            float targetFov = cam.fieldOfView + changeInFOV;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;

                float currentFov = Mathf.LerpAngle(cam.fieldOfView, targetFov, t);

                cam.fieldOfView = currentFov;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            cam.fieldOfView = targetFov;
        }
        IEnumerator SmoothChangeRoll(float targetRoll, float duration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;

                float currentRoll = Mathf.LerpAngle(transform.rotation.eulerAngles.z, targetRoll, t);

                myContainer.transform.rotation = Quaternion.Euler(myContainer.transform.rotation.eulerAngles.x, myContainer.transform.rotation.eulerAngles.y, currentRoll);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            myContainer.transform.rotation = Quaternion.Euler(myContainer.transform.rotation.eulerAngles.x, myContainer.transform.rotation.eulerAngles.y, targetRoll);
        }
    }
}
