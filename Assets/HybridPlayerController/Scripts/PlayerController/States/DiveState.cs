using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class DiveState : IState
    {
        private bool _isUnlocked;
        public bool isUnlocked
        {
            get => _isUnlocked;
            set
            {
                _isUnlocked = value;
                SaveUnlockStatus();
            }
        }
        public void EnterState(PlayerController player)
        {
            player.ChangeAnimation("Dive");
            player.canMove = false;
        
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = new Vector3(player.moveVector.normalized.x * player.diveForce, 0, player.moveVector.normalized.z * player.diveForce);
            #endif

            #if !UNITY_6000_0_OR_NEWER
            player.rb.velocity = new Vector3(player.moveVector.normalized.x * player.diveForce, 0, player.moveVector.normalized.z * player.diveForce);
            #endif
            player.cam.GetComponent<CamUtils>().ChangeFOV(player.fastMoveFOVChange, .3f);
            if (player.mode == PerspectiveMode.ThirdPerson)
            {
                player.GetComponent<PlayerRotation>().canRotateRoot = false;
            }
        }

        public void UpdateState(PlayerController player)
        {
            //The following is for checking for a grabbable ledge
            //Should be in falling and dive state
            if (player.GetComponent<WallChecker>().topOfWall.HasValue)
            {
                float[] targetLedgeValues = { 1.75f, 1.5f, 1.25f, 1, 0.75f, 0.5f };
                float yValue = player.transform.InverseTransformPoint(player.GetComponent<WallChecker>().highestHit.Value.point).y;
                float upNormal = player.GetComponent<WallChecker>().topOfWall.Value.normal.y;

                foreach (float target in targetLedgeValues)
                {
                    if (Mathf.Approximately(yValue, target) && upNormal == 1)//if yValue falls within height range && ledge is a flat floor
                    {
                        player.TransitionToState<LedgeState>();
                        return;
                    }
                }
            }
            //The following is for checking for a swing bar
            //Should be in rising, falling and dive state, and vaultJumpState, and wallrunstate
            Collider[] colliders = Physics.OverlapBox(player.transform.position + player.transform.forward * .5f, new Vector3(.25f, 1, 1), player.transform.rotation, player.platformingLayer);
            if (colliders.Length > 0)
            {
                SwingBar candidateBar = colliders[0].GetComponent<SwingBar>();
                if (candidateBar != null && candidateBar != player.lastSwingBar)
                {
                    player.mySwingBar = candidateBar;
                    player.TransitionToState<SwingBarState>();
                    return;
                }
            }
            //

            // The following is for grapple surface detection.Should be in rising, falling state, and dive state
            Ray ray = new Ray(player.cam.transform.position, player.cam.transform.forward);

            float grappleCheckDistance = 50;

            if (player.playerUtils.drawGrappleAim)
                Debug.DrawRay(ray.origin, player.cam.transform.forward.normalized * grappleCheckDistance, Color.blue);

            if (Physics.Raycast(ray, out RaycastHit grappleHit, grappleCheckDistance))
            {
                if (grappleHit.transform.CompareTag("GrappleSurface"))
                {
                    player.crossHair.SetActive(true);
                    if (player.playerControls.BaseMovement.Fire.triggered)
                    {
                        player.grapplePoint = grappleHit.point;
                        //Play Sound
                        player.TransitionToState<GrappleState>();
                        return;
                    }
                }
                else
                {
                    player.crossHair.SetActive(false);
                }
            }
            //

            if (player.isGrounded && player.playerControls.BaseMovement.Move.ReadValue<Vector2>() == Vector2.zero)
            {
                player.TransitionToState<IdleState>();
                return;
            }
            else if (player.isGrounded)
            {
                player.TransitionToState<WalkState>();
                return;
            }
        }
        public void FixedUpdateState(PlayerController player)
        {
            //player.rb.velocity = new Vector3(player.rb.velocity.x, -player.diveSpeed, player.rb.velocity.z);

        }
        public void ExitState(PlayerController player)
        {
            player.canMove = true;
            player.cam.GetComponent<CamUtils>().ChangeFOV(-player.fastMoveFOVChange, .3f);
            player.crossHair.SetActive(false);
            if (player.mode == PerspectiveMode.ThirdPerson)
            {
                player.GetComponent<PlayerRotation>().canRotateRoot = true;
            }
        }
        private string PlayerPrefsKey => "DiveState_Unlocked";
        public void LoadUnlockStatus()
        {
            _isUnlocked = PlayerPrefs.GetInt(PlayerPrefsKey, 1) == 1;
        }

        private void SaveUnlockStatus()
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, _isUnlocked ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
