using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class CrouchWalkState : IState
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
            player.ChangeAnimation("CrouchWalk");

            player.gameObject.GetComponent<CapsuleCollider>().height = 1f;
            player.gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, -.5f, 0);
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.camContainer.transform.localPosition = player.cam.GetComponent<CamUtils>().containerCrouchPos;
                player.firstPersonVisualArms.transform.localPosition = new Vector3(0, -1.095f, 0.228f);
            }
        }

        public void UpdateState(PlayerController player)
        {
            //MOVE
            player.SetMoveSpeed(player.crouchWalkSpeed);
            //play sound
            //MOVE

            if (player.playerControls.BaseMovement.Move.ReadValue<Vector2>() == Vector2.zero     //if NOT getting a move input
                && !player.playerControls.BaseMovement.Crouch.IsPressed()                        //&& NOT getting crouch input
                && !player.HasCeilingAbove)                                                                   //&& there is NOT a ceiling above the player
            {
                player.TransitionToState<IdleState>();
                return;
            }

            if (player.playerControls.BaseMovement.Move.ReadValue<Vector2>() == Vector2.zero     //if NOT getting a move input
                && player.playerControls.BaseMovement.Crouch.IsPressed())                        //&& IS getting crouch input
            {
                player.TransitionToState<CrouchState>();
                return;
            }
            else if (player.playerControls.BaseMovement.Move.ReadValue<Vector2>() == Vector2.zero//if NOT getting a move input
                && !player.playerControls.BaseMovement.Crouch.IsPressed()                        //&& NOT getting crouch input
                && player.HasCeilingAbove)                                                                    //&& there IS a ceiling above the player
            {
                player.TransitionToState<CrouchState>();
                return;
            }
            else if (!player.playerControls.BaseMovement.Crouch.IsPressed()                      //if NOT getting crouch input
                && !player.HasCeilingAbove)                                                                   //&& there is NOT a ceiling above the player
            {
                player.TransitionToState<IdleState>();
                return;
            }

            if (!player.isGrounded)
            {
                #if UNITY_6000_0_OR_NEWER
                if (player.rb.linearVelocity.y >= 0)
                {
                    player.TransitionToState<RisingState>();
                    return;
                }
                if (player.rb.linearVelocity.y <= 0)
                {
                    player.TransitionToState<FallingState>();
                    return;
                }
                #endif

                #if !UNITY_6000_0_OR_NEWER
                if (player.rb.velocity.y >= 0)
                {
                    player.TransitionToState<RisingState>();
                    return;
                }
                if (player.rb.velocity.y <= 0)
                {
                    player.TransitionToState<FallingState>();
                    return;
                }
                #endif
            
            }
        }

        public void FixedUpdateState(PlayerController player) { }

        public void ExitState(PlayerController player) 
        {
            //MOVE
            player.SetMoveSpeed(0);
            //MOVE
            player.gameObject.GetComponent<CapsuleCollider>().height = 2f;
            player.gameObject.GetComponent<CapsuleCollider>().center = Vector3.zero;
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.camContainer.transform.localPosition = player.cam.GetComponent<CamUtils>().containerDefaultPos;
                player.firstPersonVisualArms.transform.localPosition = new Vector3(0, -0.09500003f, 0.228f);
            }
        }

        private string PlayerPrefsKey => "CrouchWalkState_Unlocked";
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
