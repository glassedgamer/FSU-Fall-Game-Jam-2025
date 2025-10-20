using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class VaultJumpState : IState
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
        float upForce;
        float forwardForce;

        float timer;
        public void EnterState(PlayerController player) 
        {
            player.cam.GetComponent<CamUtils>().ChangeFOV(player.fastMoveFOVChange, .1f);
            upForce = player.vaultJumpUpForce;
            forwardForce = player.vaultJumpForwardForce;
            timer = player.vaultTime;
            player.canMove = false;
            //Jump
            player.justJumped = true;
            player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + .08f, player.transform.position.z);//setting height up just enough so that the ground checker does not hit ground. Rising state checks for is grounded, so breaks immediately otherwise
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = new Vector3(0, upForce, 0) + player.transform.forward * forwardForce;
            #else
            player.rb.velocity = new Vector3(0, upForce, 0) + player.transform.forward * forwardForce;
            #endif
            player.ChangeAnimation("VaultJump");
        }
        public void UpdateState(PlayerController player) 
        {
            if(timer > 0)
                timer -= Time.deltaTime;


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
            Debug.DrawRay(ray.origin, player.cam.transform.forward.normalized * 50, Color.yellow);
            if (Physics.Raycast(ray, out RaycastHit hit, 50))
            {
                if (hit.transform.CompareTag("GrappleSurface"))
                {
                    if (player.playerControls.BaseMovement.Fire.triggered)
                    {
                        player.grapplePoint = hit.point;
                        player.TransitionToState<GrappleState>();
                        return;
                    }
                }
            }
            //


            if (player.isGrounded && timer <= 0)
            {
                player.TransitionToState<IdleState>();
                return;
            }

        }

        public void FixedUpdateState(PlayerController player) { }
        public void ExitState(PlayerController player) 
        {
            player.canMove = true;
            player.cam.GetComponent<CamUtils>().ChangeFOV(-player.fastMoveFOVChange, .3f);
        }
        private string PlayerPrefsKey => "VaultJumpState_Unlocked";
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
