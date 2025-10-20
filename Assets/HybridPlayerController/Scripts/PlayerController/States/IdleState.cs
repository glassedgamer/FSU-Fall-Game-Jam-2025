using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class IdleState : IState
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
            player.ChangeAnimation("Idle");
            player.justJumped = false;
            player.extraJumpCount = player.extraJumps;
            player.gravityComponent.enabled = false;
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
            else
            {
                player.TransitionToState<RisingState>();
                return;
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
            else
            {
                player.TransitionToState<RisingState>();
                return;
            }
            #endif
        

            if (player.isSprinting && (player.playerControls.BaseMovement.Move.ReadValue<Vector2>() != Vector2.zero) && !player.playerControls.BaseMovement.Crouch.triggered)
            {
                player.TransitionToState<SprintState>();
                return;
            }
            else if ((player.playerControls.BaseMovement.Move.ReadValue<Vector2>() != Vector2.zero) && !player.playerControls.BaseMovement.Crouch.triggered)
            {
                player.TransitionToState<WalkState>();
                return;
            }
            if ((player.playerControls.BaseMovement.Move.ReadValue<Vector2>() != Vector2.zero) && player.playerControls.BaseMovement.Crouch.triggered)
            {
                player.TransitionToState<CrouchWalkState>();
                return;
            }

            if (player.playerControls.BaseMovement.Jump.triggered && player.isGrounded && player.canMove)
            {
                player.TransitionToState<JumpState>();
                return;
            }

            if (player.playerControls.BaseMovement.Crouch.triggered && player.isGrounded)
            {
                player.TransitionToState<CrouchState>();
                return;
            }
        }

        public void FixedUpdateState(PlayerController player) { }
        public void ExitState(PlayerController player)
        {
            player.gravityComponent.enabled = true;
        }
        private string PlayerPrefsKey => "IdleState_Unlocked";
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
