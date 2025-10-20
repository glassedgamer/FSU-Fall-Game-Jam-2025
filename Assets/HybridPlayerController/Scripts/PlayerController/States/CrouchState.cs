using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class CrouchState : IState
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
            player.ChangeAnimation("Crouch");
            player.justJumped = false;

            player.gameObject.GetComponent<CapsuleCollider>().height = 1f;
            player.gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, -.5f, 0);
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.camContainer.transform.localPosition = player.cam.GetComponent<CamUtils>().containerCrouchPos;
                player.firstPersonVisualArms.transform.localPosition = new Vector3(0, -1, 0);
            }
        }

        public void UpdateState(PlayerController player)
        {
            #if UNITY_6000_0_OR_NEWER
            if (player.groundChecker.IsOnSlope() && player.groundChecker.IsGrounded())
            {
                player.rb.linearVelocity = new Vector3(0, 0, 0);
            }
            else if (player.groundChecker.IsGrounded())
            {
                player.rb.linearVelocity = new Vector3(0, player.rb.linearVelocity.y, 0);
            }
            #endif

            #if !UNITY_6000_0_OR_NEWER
            if (player.groundChecker.IsOnSlope() && player.groundChecker.IsGrounded())
            {
                player.rb.velocity = new Vector3(0, 0, 0);
            }
            else if (player.groundChecker.IsGrounded())
            {
                player.rb.velocity = new Vector3(0, player.rb.velocity.y, 0);
            }
            #endif

            if (player.playerControls.BaseMovement.Move.ReadValue<Vector2>() != Vector2.zero)
            {
                player.TransitionToState<CrouchWalkState>();
                return;
            }

            /*
            if (Input.GetButtonDown("Jump") && player.isGrounded && player.canMove)
            {
                player.TransitionToState<JumpState>();
            }
            */

            if (!player.playerControls.BaseMovement.Crouch.IsPressed() && !player.HasCeilingAbove)//if crouch is not being press && there is no ceiling above player
            {
                player.TransitionToState<IdleState>();
                return;
            }
        }

        public void FixedUpdateState(PlayerController player) { }
        public void ExitState(PlayerController player)
        {
            player.gameObject.GetComponent<CapsuleCollider>().height = 2f;
            player.gameObject.GetComponent<CapsuleCollider>().center = Vector3.zero;
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.camContainer.transform.localPosition = player.cam.GetComponent<CamUtils>().containerDefaultPos;
                player.firstPersonVisualArms.transform.localPosition = Vector3.zero;
            }
        }
        private string PlayerPrefsKey => "CrouchState_Unlocked";
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
