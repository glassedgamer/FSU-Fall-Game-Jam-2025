using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class SlideState : IState
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
        float speed;
        Vector3 slideDir;
        public void EnterState(PlayerController player)
        {
            //must test for player.canSlide when attempting to enter this state (see walk state and sprint state)
            player.canSlide = false;
            player.ChangeAnimation("Slide", .3f);
            player.justJumped = false;

            speed = player.slideSpeed;
            slideDir = player.transform.forward;

            player.canMove = false;
            player.gameObject.GetComponent<CapsuleCollider>().height = 1f;
            player.gameObject.GetComponent<CapsuleCollider>().center = new Vector3(0, -.5f, 0);
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.camContainer.transform.localPosition = player.cam.GetComponent<CamUtils>().containerCrouchPos;
                player.firstPersonVisualArms.transform.localPosition = new Vector3(0, -1, 0);
                player.cam.GetComponent<CamUtils>().ChangeRoll(-2, .7f);
            }
            else if (player.mode == PerspectiveMode.ThirdPerson)
            {
                player.cam.GetComponent<CamUtils>().ChangeFOV(player.fastMoveFOVChange, .3f);
                player.GetComponent<PlayerRotation>().canRotateRoot = false;
            }
        }

        public void UpdateState(PlayerController player)
        {
            if (player.playerControls.BaseMovement.Jump.triggered && player.isGrounded && !player.HasCeilingAbove)
            {
                player.TransitionToState<JumpState>();
                return;
            }

            if (!player.playerControls.BaseMovement.Crouch.IsPressed() && player.isGrounded && !player.HasCeilingAbove)
            {
                player.TransitionToState<IdleState>();
                return;
            }
            else if (!player.playerControls.BaseMovement.Crouch.IsPressed() && player.isGrounded && player.HasCeilingAbove)
            {
                player.TransitionToState<CrouchState>();
                return;
            }

            if (speed > 0)
            {
                speed -= Time.deltaTime * player.slideReductionRate;
            }
            else if (player.HasCeilingAbove || player.playerControls.BaseMovement.Crouch.IsPressed())
            {
                speed = 0;
                player.TransitionToState<CrouchState>();
                return;
            }
            else
            {
                speed = 0;
                player.TransitionToState<IdleState>();
                return;
            }
        
        }

        IEnumerator DelayNextSlide(PlayerController player)
        {
            yield return new WaitForSeconds(player.slideDelay);
            player.canSlide = true;
        }

        public void FixedUpdateState(PlayerController player) 
        {
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = new Vector3(slideDir.x * speed, player.rb.linearVelocity.y, slideDir.z * speed);
            #else
            player.rb.velocity = new Vector3(slideDir.x * speed, player.rb.velocity.y, slideDir.z * speed);
            #endif
        }
        public void ExitState(PlayerController player)
        {
            player.canMove = true;
            player.gameObject.GetComponent<CapsuleCollider>().height = 2f;
            player.gameObject.GetComponent<CapsuleCollider>().center = Vector3.zero;
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.camContainer.transform.localPosition = player.cam.GetComponent<CamUtils>().containerDefaultPos;
                player.firstPersonVisualArms.transform.localPosition = Vector3.zero;
                player.cam.GetComponent<CamUtils>().ChangeRoll(0, .3f);
            }
            else if (player.mode == PerspectiveMode.ThirdPerson)
            {
                player.cam.GetComponent<CamUtils>().ChangeFOV(-player.fastMoveFOVChange, .3f);
                player.GetComponent<PlayerRotation>().canRotateRoot = true;
            }

            //reset player.canSlide
            player.StartCoroutine(DelayNextSlide(player));
        }
        private string PlayerPrefsKey => "SlideState_Unlocked";
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
