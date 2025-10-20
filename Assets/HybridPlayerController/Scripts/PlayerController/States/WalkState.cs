using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class WalkState : IState
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
            player.ChangeAnimation("Walk");
            player.justJumped = false;
        }

        public void UpdateState(PlayerController player)
        {
            //MOVE
            player.SetMoveSpeed(player.walkSpeed);
            //play sound footsteps
            //MOVE

            if (player.isSprinting)
            {
                player.TransitionToState<SprintState>();
                return;
            }

            if (player.playerControls.BaseMovement.Move.ReadValue<Vector2>() == Vector2.zero)
            {
                player.TransitionToState<IdleState>();
                return;
            }

            if (!player.isGrounded)
            {
                #if UNITY_6000_0_OR_NEWER
                if (player.rb.linearVelocity.y >= 0)
                #else
                if (player.rb.velocity.y >= 0)
                #endif
                {
                    player.TransitionToState<RisingState>();
                    return;
                }
                #if UNITY_6000_0_OR_NEWER
                if (player.rb.linearVelocity.y <= 0)
                #else
                if (player.rb.velocity.y <= 0)
                #endif
                {
                    player.TransitionToState<FallingState>();
                    return;
                }
            }

            //Stepping and transitioning to vault/jump:
            //Should be in WalkState and SprintState
            //This will be partially shared in rising and falling sate for ledge detection

            WallChecker wallChecker = player.GetComponent<WallChecker>();

            //stepping
            if (wallChecker.highestHit.HasValue && (player.moveInput.y > 0f || player.moveInput.x != 0))
            {
                float y = player.transform.InverseTransformPoint(wallChecker.highestHit.Value.point).y;
                float yR = Mathf.Round(y * 100f) / 100f;//Rounding yValue to two decimal places.
                if ((Mathf.Approximately(yR, -1) || Mathf.Approximately(yR, -0.75f)) && wallChecker.topOfWall.HasValue)//Checking if the heihght of the hit is in our desired step height range (between -1 and -0.75 relative to the player)
                {
                    if (Vector3.Distance(wallChecker.topOfWall.Value.point, new Vector3(player.transform.position.x, wallChecker.topOfWall.Value.point.y, player.transform.position.z)) <= .51f)
                    {
                        float posDif = wallChecker.topOfWall.Value.point.y - (player.transform.position.y - 1);
                        Vector3 targetPos = new Vector3(player.transform.position.x, player.transform.position.y + posDif, player.transform.position.z);
                        float stepSmoothSpeed = 10;
                        player.rb.MovePosition(Vector3.MoveTowards(player.rb.position, targetPos, stepSmoothSpeed * Time.fixedDeltaTime));
                        //player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + finalPosDif, player.transform.position.z);
                    }
                }
            }
            //

            //Vaulting (and fallbackt to jump)
            if (player.playerControls.BaseMovement.Jump.triggered)
            {

                if (player.isGrounded && wallChecker.highestHit.HasValue)
                {
                    //float[] targetVaultValues = { 0.75f, 0.5f, 0.25f, 0f, -0.25f, -0.5f, -0.75f };
                    float[] targetVaultValues = { 0.75f, 0.5f, 0.25f, 0f, -0.25f, -0.5f }; //if -0.75 step, if atleast -0.5 then vault (-0.75 is not included for this reason)
                    float[] targetLedgeValues = { 1, 1.25f, 1.5f, 1.75f };//not higher than highest possible (2 is not included because we want to do a normal jump when above 1.75f)
                    float yValue = player.transform.InverseTransformPoint(wallChecker.highestHit.Value.point).y;
                    float yValueRounded = Mathf.Round(yValue * 100f) / 100f;//Rounding yValue to two decimal places.

                    foreach (float target in targetVaultValues)
                    {
                        float playerHeight = 2;
                        if (Mathf.Approximately(yValueRounded, target)
                            && !Physics.Raycast(wallChecker.topOfWall.Value.point, Vector3.up, playerHeight, player.worldLayer, QueryTriggerInteraction.Ignore))//There is not a ceiling blocking the potential vault
                        {
                            player.TransitionToState<VaultState>();
                            return;
                        }
                    }

                    foreach (float target in targetLedgeValues)
                    {
                        if (Mathf.Approximately(yValueRounded, target))
                        {
                            player.TransitionToState<LedgeState>();
                            return;
                        }
                    }

                    player.TransitionToState<JumpState>();
                    return;
                }
                else
                {
                    player.TransitionToState<JumpState>();
                    return;
                }
            }
            //
        }

        public void FixedUpdateState(PlayerController player) {}

        public void ExitState(PlayerController player)
        {
            
        }
        private string PlayerPrefsKey => "WalkState_Unlocked";
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
