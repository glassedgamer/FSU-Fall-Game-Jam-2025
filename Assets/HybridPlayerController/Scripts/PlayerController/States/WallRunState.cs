using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{
    public class WallRunState : IState
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
        bool justWallRanRight;
        bool justWallRanLeft;

        Vector3 wallForward;
        Vector3 wallNormal;

        //for exiting wallRunState in third person 
        bool entrenceInputIsX;
        bool entrenceInputIsY;
        bool canExitState;
        Vector3 snapDir;
        //

        public void EnterState(PlayerController player)
        {
            player.canMove = false;
            player.gravityComponent.enabled = false;
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = Vector3.zero;
            #else
            player.rb.velocity = Vector3.zero;
            #endif

            if(player.mode == PerspectiveMode.ThirdPerson)
            {
                entrenceInputIsX = false;
                entrenceInputIsY = false;
                canExitState = false;
                if (player.moveInput.x != 0)
                {
                    entrenceInputIsX = true;
                }
                if (player.moveInput.y != 0)
                {
                    entrenceInputIsY = true;

                }
            }

            //use the stashed hit instead of raycasting again:
            var hit = player.nextWallRunHit;
            wallNormal = hit.normal;
            wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

            //For wallChecker portion of determining weather or not the player left the wall before wall running again. A nullable raycastHit is set in wall checker and checked in rising and falling state
            player.GetComponent<WallChecker>().wallRunForward = wallForward;

            //set player position to be right against the wall
            Vector3 targetPos = hit.point + wallNormal * 0.5f + Vector3.down * 1; //have to offset the target pos down 1 becuase the starting hit here is from a ray shooting from 1 unit above the player, not from the players center (See code for entering this state in rising and faling states)
            player.StartCoroutine(MoveToWall(player, targetPos));//smoothly move to where the wall is

            //roll + snap
            snapDir = player.nextWallRunIsRight ? -wallForward : wallForward;
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                float rollAngle = player.nextWallRunIsRight ? 5f : -5f;
                player.cam.GetComponent<CamUtils>().ChangeRoll(rollAngle, .7f);
                player.GetComponent<PlayerRotation>().SnapRotation(0, snapDir, .1f);
            }
            else if(player.mode == PerspectiveMode.ThirdPerson)
            {
                player.GetComponent<PlayerRotation>().canRotateRoot = false;
                player.transform.rotation = Quaternion.LookRotation(snapDir, player.transform.up);//Instant rotation to maintain exit functionality
            }

            //animation
            player.ChangeAnimation(player.nextWallRunIsRight ? "WallRunRight" : "WallRunLeft");
        }

        private IEnumerator MoveToWall(PlayerController player, Vector3 targetPos)
        {
            Vector3 startPos = player.transform.position;
            float elapsedTime = 0f;
            float moveDuration = .1f;

            while (elapsedTime < moveDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveDuration;
                player.transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            player.transform.position = targetPos;
            canExitState = true;//for bailing when no wall
        }

        public void UpdateState(PlayerController player)
        {
            if (player.playerControls.BaseMovement.Jump.triggered)
            {
                player.TransitionToState<JumpState>();
                return;
            }

            if (player.isGrounded)
            {
                player.TransitionToState<IdleState>();
                return;
            }

            if (player.mode == PerspectiveMode.FirstPerson && player.moveInput.y <= 0.1f)
            {
                player.TransitionToState<IdleState>();
                return;
            }
            else if (player.mode == PerspectiveMode.ThirdPerson)
            {
                if (entrenceInputIsX && !entrenceInputIsY && player.moveInput.x == 0f)
                {
                    player.TransitionToState<IdleState>();
                    return;
                }
                else if (entrenceInputIsY && !entrenceInputIsX && player.moveInput.y == 0f)
                {
                    player.TransitionToState<IdleState>();
                    return;
                }
                else if ((entrenceInputIsX && entrenceInputIsY) && (player.moveInput.y == 0f && player.moveInput.x == 0f))
                {
                    player.TransitionToState<IdleState>();
                    return;
                }
            }
            
        }

        public void FixedUpdateState(PlayerController player)
        {
            //if we've lost the wall, bail
            //Have to have "canExitState" here becuase this is being called before/at the same time as the player parent being rotated in enterState(). Without, it would exit immediately
            if (canExitState && (!Physics.Raycast(player.transform.position, player.nextWallRunIsRight ? player.transform.right : -player.transform.right, out var onHit, 1f, player.worldLayer) || Vector3.Distance(onHit.normal, wallNormal) > 0.01f))
            {
                player.TransitionToState<IdleState>();
                return;
            }

            //Run along that wall-forward
            Vector3 runDir = player.nextWallRunIsRight ? -wallForward : wallForward;
            float wallRunSpeed = player.wallRunSpeedSprint;
            //transition state if player hits wall in front
            if (Physics.Raycast(player.transform.position, runDir, .6f, player.worldLayer, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(player.transform.position, runDir * .6f, Color.red, 1);
                Debug.DrawRay(player.transform.position + runDir * .5f, Vector3.up * .1f, Color.red, 1);
                player.TransitionToState<IdleState>();
                return;
            }
            else
            { 
            #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = runDir * wallRunSpeed + Vector3.down * player.wallRunDownForce;
            #else
                player.rb.velocity = runDir * wallRunSpeed + Vector3.down * player.wallRunDownForce;
            #endif
            }
            

            // The following is for checking for a swing bar
            // Should be in rising, falling and dive state, and vaultJumpState, and wallrunstate
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
        }

        public void ExitState(PlayerController player)
        {
            player.justWallRanRight = player.nextWallRunIsRight;
            player.justWallRanLeft  = !player.nextWallRunIsRight;

            player.canMove = true;
            player.gravityComponent.enabled = true;
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.cam.GetComponent<CamUtils>().ChangeRoll(0, .3f);
            }
            else if (player.mode == PerspectiveMode.ThirdPerson)
            {
                player.GetComponent<PlayerRotation>().canRotateRoot = true;
            }
        }

        private string PlayerPrefsKey => "WallRunState_Unlocked";
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
