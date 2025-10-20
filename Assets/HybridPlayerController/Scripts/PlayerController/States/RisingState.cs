using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class RisingState : IState
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

        private float wallRunDelayTimer;

        public void EnterState(PlayerController player)
        {
            wallRunDelayTimer = .1f;
            player.ChangeAnimation("Rising");
            //MOVE for rising and falling states
            if (player.isSprinting)
            {
                player.SetMoveSpeed(player.risingMoveSpeed + (player.sprintSpeed - player.walkSpeed));//Move speed is set to the risingMoveSpeed in PlayerController PLUS the difference between sprint speed and walk speed
            }
            else
            {
                player.SetMoveSpeed(player.risingMoveSpeed);//Normal
            }
            //MOVE
        }
        public void UpdateState(PlayerController player) 
        {
            //the following should be in, RaisingState, and FallingState
            if (player.mode == PerspectiveMode.FirstPerson && player.playerControls.BaseMovement.Crouch.triggered && player.playerControls.BaseMovement.Move.ReadValue<Vector2>().y > 0)//Dive forward only
            {
                player.TransitionToState<DiveState>();
                return;
            }
            else if (player.mode == PerspectiveMode.ThirdPerson && player.playerControls.BaseMovement.Crouch.triggered && player.playerControls.BaseMovement.Move.ReadValue<Vector2>() != Vector2.zero)//Dive in any direction
            {
                player.TransitionToState<DiveState>();
                return;
            }
            //

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

            #if UNITY_6000_0_OR_NEWER
            if (player.rb.linearVelocity.y <= 0)
            {
                player.TransitionToState<FallingState>();
                return;
            }
            #else
            if (player.rb.velocity.y <= 0)
            {
                player.TransitionToState<FallingState>();
                return;
            }
            #endif
            
        
            if (player.isGrounded)
            {
                player.TransitionToState<IdleState>();
                return;
            }
            
            if (player.playerControls.BaseMovement.Jump.triggered && player.extraJumpCount > 0)//this is for extra jumps (double-jumping)
            {
                player.TransitionToState<JumpState>();//wether or not it is a normal jump is logic ran in jumpState
            }

            if (wallRunDelayTimer <= 0)//slight delay after jumping so player does not get stuck on a wall when trying to jump close to a wall
            {
                //The following is for checking for a wall to run on
                //should be in rising and falling state
                //tuning parameters
                float forwardCheckHeight = 1.0f;
                float forwardCheckDist = 1.5f;
                float angleThresholdFirstPerson = 70f;
                float angleThresholdThirdPerson = 80f;

                //forward cast & angle check
                Vector3 rayOrigin = player.transform.position + Vector3.up * forwardCheckHeight;
                RaycastHit hit;

                if (Physics.Raycast(rayOrigin, player.transform.forward, out hit, forwardCheckDist, player.worldLayer) && Mathf.Abs(hit.normal.y) < 0.1f)
                {
                    Vector3 wallFwd = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    float angleToFwd = Vector3.Angle(player.transform.forward, wallFwd);
                    float angleToBwd = Vector3.Angle(player.transform.forward, -wallFwd);

                    float capsuleRadius = .5f;
                    float offsetFromWall = .51f;
                    Vector3 capsuleCheckStart = new Vector3(hit.point.x, hit.point.y - forwardCheckHeight + .5f, hit.point.z) + hit.normal * offsetFromWall;
                    Vector3 capsuleCheckEnd = new Vector3(hit.point.x, hit.point.y - forwardCheckHeight - .5f, hit.point.z) + hit.normal * offsetFromWall;

                    if (player.mode == PerspectiveMode.FirstPerson && (angleToFwd < angleThresholdFirstPerson || angleToBwd < angleThresholdFirstPerson)
                        && player.justJumped
                        && player.isSprinting
                        && player.moveInput.y > 0f
                        && !Physics.CheckCapsule(capsuleCheckStart, capsuleCheckEnd, capsuleRadius, player.worldLayer, QueryTriggerInteraction.Ignore))//Check if there is a perpendicular wall in front of the player
                    {
                        bool wallOnRight = Vector3.Dot(hit.normal, player.transform.right) < 0f;//if the dot product is negative, then true
                        if (wallOnRight && !player.justWallRanRight && !player.GetComponent<WallChecker>().wallRunWallHit.HasValue)
                        {
                            player.nextWallRunHit = hit;
                            player.nextWallRunIsRight = true;
                            player.TransitionToState<WallRunState>();
                            return;
                        }
                        else if (!wallOnRight && !player.justWallRanLeft && !player.GetComponent<WallChecker>().wallRunWallHit.HasValue)
                        {
                            player.nextWallRunHit = hit;
                            player.nextWallRunIsRight = false;
                            player.TransitionToState<WallRunState>();
                            return;
                        }
                    }
                    else if (player.mode == PerspectiveMode.ThirdPerson && (angleToFwd < angleThresholdThirdPerson || angleToBwd < angleThresholdThirdPerson) 
                        && player.justJumped
                        && player.isSprinting 
                        && player.moveInput.magnitude != 0f
                        && !player.GetComponent<WallChecker>().wallRunWallHit.HasValue
                        && !Physics.CheckCapsule(capsuleCheckStart, capsuleCheckEnd, capsuleRadius, player.worldLayer, QueryTriggerInteraction.Ignore))//Check if there is a perpendicular wall in front of the player
                    {
                        bool wallOnRight = Vector3.Dot(hit.normal, player.transform.right) < 0f;//if the dot product is negative, then true
                        if (wallOnRight && !player.justWallRanRight && !player.GetComponent<WallChecker>().wallRunWallHit.HasValue)
                        {
                            player.nextWallRunHit = hit;
                            player.nextWallRunIsRight = true;
                            player.TransitionToState<WallRunState>();
                            return;
                        }
                        else if (!wallOnRight && !player.justWallRanLeft && !player.GetComponent<WallChecker>().wallRunWallHit.HasValue)
                        {
                            player.nextWallRunHit = hit;
                            player.nextWallRunIsRight = false;
                            player.TransitionToState<WallRunState>();
                            return;
                        }
                    }
                }
                else
                {
                    if (player.playerUtils.drawWallRunCheck)
                        Debug.DrawRay(rayOrigin, player.transform.forward * forwardCheckDist, Color.blue, 100);
                }

                //fallback to side-rays (for firtst person)
                if(player.mode == PerspectiveMode.FirstPerson)
                {
                    bool didSideHit = false;

                    if (!player.justWallRanRight && Physics.Raycast(rayOrigin, player.transform.right, out hit, 0.53f, player.worldLayer) && Mathf.Abs(hit.normal.y) < 0.1f)
                    {
                        player.nextWallRunHit = hit;
                        player.nextWallRunIsRight = true;
                        didSideHit = true;
                    }
                    else if (!player.justWallRanLeft && Physics.Raycast(rayOrigin, -player.transform.right, out hit, 0.53f, player.worldLayer) && Mathf.Abs(hit.normal.y) < 0.1f)
                    {
                        player.nextWallRunHit = hit;
                        player.nextWallRunIsRight = false;
                        didSideHit = true;
                    }

                    if (didSideHit && player.justJumped && player.isSprinting && player.moveInput.y > 0f)
                    {
                        player.TransitionToState<WallRunState>();
                        return;
                    }
                }
                //
            }
            else
            {
                wallRunDelayTimer -= Time.deltaTime;
                //Debug.Log(timer);
            }

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
                    return;
                }
            }
            //
        }
        public void FixedUpdateState(PlayerController player) 
        {
            //For ensuring the player does not fly up in the air after going up a slope
            if (!player.isGrounded && !player.justJumped && Physics.Raycast(player.transform.position, Vector3.down, out RaycastHit groundHit, 1.5f, player.worldLayer) && !groundHit.collider.isTrigger)
            {
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, 0, player.rb.linearVelocity.z);
                #else
                player.rb.velocity = new Vector3(player.rb.velocity.x, 0, player.rb.velocity.z);
                #endif
                player.transform.position = new Vector3(player.transform.position.x, groundHit.point.y + 1, player.transform.position.z);

                if (player.playerUtils.drawGroundChecker)
                    Debug.DrawRay(player.transform.position, Vector3.down * 1.5f, Color.white, 5);
            }
            //
        }
        public void ExitState(PlayerController player) 
        {
            //MOVE
            //Don't set so that the move speed carries over to the falling state
            //player.SetMoveSpeed(0);
            //MOVE
            player.crossHair.SetActive(false);
        }
        private string PlayerPrefsKey => "RisingState_Unlocked";
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
