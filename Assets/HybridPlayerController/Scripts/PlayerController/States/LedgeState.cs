using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{
    public class LedgeState : IState
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

        Vector3 rightDir;
        Vector3 targetPosition;
        Vector3 forwardDir;

        //see vault state
        private float vaultDuration;//in PlayerController
        private float vaultDistance = 2f;
        private float vaultHeight = 1f;
        private bool canVault;
        //

        WallChecker w;
        RaycastHit wallHit;

        bool canMoveRight = true;
        bool canMoveLeft = true;

        private float dist;

        //
        Vector3 offset = new Vector3(0, -0.47f, 0.3f);
        //

        public void EnterState(PlayerController player) 
        {
            player.canMove = false;
            player.justJumped = false;
            player.gravityComponent.enabled = false;
            if (player.mode == PerspectiveMode.ThirdPerson)
            { 
                player.GetComponent<PlayerRotation>().canRotateVisual = false;
            }
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = Vector3.zero;
            #else
            player.rb.velocity = Vector3.zero;
            #endif
            vaultDuration = player.vaultTime;

            w = player.GetComponent<WallChecker>();
            if (w == null || !w.highestHit.HasValue || !w.topOfWall.HasValue)
            {
                player.TransitionToState<IdleState>();
                return;
            }
            wallHit = w.highestHit.Value;
            Vector3 upDir = player.transform.up;
            forwardDir = -wallHit.normal;
            rightDir = Vector3.Cross(upDir, forwardDir).normalized;
            forwardDir = Vector3.Cross(rightDir, upDir).normalized;
            Quaternion rot = Quaternion.LookRotation(forwardDir, upDir);

            //For checking for end of ledge
            
            targetPosition = (w.topOfWall.Value.point + -upDir * 1) + (-forwardDir * 0.5f);
            dist = .51f;

            player.StartCoroutine(LerpPosition(player));

            Vector3 euler = rot.eulerAngles;
            player.GetComponent<PlayerRotation>().SnapRotation(euler.x, euler.y, .1f);
            player.ChangeAnimation("LedgeIdle");
        
        }
        IEnumerator LerpPosition(PlayerController player)
        {
            float duration = .05f;
            float time = 0;
            Vector3 startPosition = player.transform.position;

            while (time < duration)
            {
                player.transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
                time += Time.deltaTime;
                yield return null;
            }

            player.transform.position = targetPosition;//ensure it reaches the exact position

            //First Person hands. Done here to ensure it is after the player position is exact
            if(player.mode == PerspectiveMode.FirstPerson)
            {
                player.firstPersonVisualArms.transform.rotation = player.transform.rotation;
                player.firstPersonVisualArms.transform.localPosition = offset;
                player.firstPersonVisualArms.transform.parent = null;//for rotation
            }
            //

            //yield return new WaitUntil(() => w.topOfWall.HasValue && w.topOfWall.HasValue);
            //upYoffset = player.transform.InverseTransformPoint(w.topOfWall.Value.point).y;
        }
        private IEnumerator ClimbLedge(PlayerController player)
        {
            canVault = true;

            //Hands
            if (player.mode == PerspectiveMode.FirstPerson)
                player.firstPersonVisualArms.transform.parent = player.transform;
            //

            Vector3 startPos = player.transform.position;
            Vector3 vaultDirection = player.transform.forward;//dir of vault
            Vector3 endPos;
            float arcHeight;
            if (player.GetComponent<WallChecker>().floorHit.HasValue) //floor in front of player
            {
                endPos = player.transform.up * 1 + player.GetComponent<WallChecker>().floorHit.Value.point;
                arcHeight = player.transform.InverseTransformPoint(player.GetComponent<WallChecker>().highestHit.Value.point).y;
                vaultDuration /= 2;
            }
            else
            {
                endPos = startPos + vaultDirection.normalized * vaultDistance;
                arcHeight = player.transform.InverseTransformPoint(player.GetComponent<WallChecker>().highestHit.Value.point).y + vaultHeight;
            }

            bool floor = player.GetComponent<WallChecker>().floorHit.HasValue;

            float elapsedTime = 0f;

            while (elapsedTime < vaultDuration && canVault)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / vaultDuration;

                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                //calculate a vertical offset to form a parabolic arc.
                float arc = 4 * arcHeight * t * (1 - t);
                currentPos.y += arc;

                player.transform.position = currentPos;

                //Hands
                if (player.mode == PerspectiveMode.FirstPerson)
                {
                    player.firstPersonVisualArms.transform.rotation = player.transform.rotation;
                    player.firstPersonVisualArms.transform.localPosition = Vector3.zero;
                }
                //

                yield return null;
            }
            player.transform.position = endPos;

            yield break;
        }
        public void UpdateState(PlayerController player)
        {
            if (player.isGrounded)
            {
                player.TransitionToState<IdleState>();
                return;
            }

            //Determining if the player can climb a ledge (otherwise jump) by...
            //Checking if the player is looking towards or away from the ledge 
            //Checking if there is a ledge to jump up onto
            //Checking if there is not a ceiling above the desired climb 
            float side = Vector3.Dot(forwardDir, player.transform.forward);
            float upYoffset = 1;//Top of player's "head"
            float playerHeight = 2;
            Vector3 middleRayOrigin = player.transform.position + (player.transform.up * upYoffset);
            Debug.DrawRay(middleRayOrigin + player.transform.up * .01f, forwardDir * 1, Color.blue);
            if (player.playerControls.BaseMovement.Jump.triggered)
            {
                if (side > 0)
                {
                    if (player.moveInput.y >= 0)//if not pressing backward. If the player is pressing backwards we want the player to jump off a ledge not climb it
                    {
                        if (Physics.Raycast(middleRayOrigin + player.transform.up * .01f, forwardDir, out RaycastHit middleRayHit, 1, player.worldLayer, QueryTriggerInteraction.Ignore))//Make sure there is not a wall in front of the player
                        {
                            player.TransitionToState<JumpState>();
                            return;
                        }
                        else if (w.topOfWall.HasValue && !Physics.Raycast(w.topOfWall.Value.point, Vector3.up, playerHeight, player.worldLayer, QueryTriggerInteraction.Ignore))//that there is actually a ledge directly in front of the player to jump up onto && there is not ceiling blocking the climb
                        {
                            player.StartCoroutine(ClimbLedge(player));
                        }
                        else
                        {
                            player.TransitionToState<JumpState>();
                            return;
                        }
                    }
                    else
                    {
                        player.TransitionToState<JumpState>();
                        return;
                    }
                }
                else if (side < 0)
                {
                    player.TransitionToState<JumpState>();
                    return;
                }
            }

            //Checking for end of ledge to stop movement:

            Vector3 rightRayOrigin = player.transform.position + player.transform.up * -.01f + rightDir * 0.5f + (player.transform.up * upYoffset);
            Vector3 leftRayOrigin = player.transform.position + player.transform.up * -.01f + -rightDir * 0.5f + (player.transform.up * upYoffset);


            // ------------------ Right side checks ------------------ //

            bool hasLedgeRight = Physics.Raycast(rightRayOrigin, forwardDir, out RaycastHit ledgeHitRight, dist, player.worldLayer);
            bool hasBlockerRight = Physics.Raycast(rightRayOrigin + player.transform.up * .02f,
                                                    forwardDir,
                                                    out RaycastHit blockerHitRight,
                                                    dist,
                                                    player.worldLayer);

            if (hasLedgeRight)
            {
                Debug.DrawRay(rightRayOrigin, forwardDir * ledgeHitRight.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(rightRayOrigin, forwardDir * dist, Color.red);
            }

            if (hasBlockerRight)
            {
                Debug.DrawRay(rightRayOrigin + player.transform.up * .01f,
                              forwardDir * blockerHitRight.distance,
                              Color.red);
            }
            else
            {
                Debug.DrawRay(rightRayOrigin + player.transform.up * .01f,
                              forwardDir * dist,
                              Color.green);
            }

            canMoveRight = hasLedgeRight && !hasBlockerRight;


            // ------------------ Left side checks ------------------ //

            bool hasLedgeLeft = Physics.Raycast(leftRayOrigin, forwardDir, out RaycastHit ledgeHitLeft, dist, player.worldLayer);
            bool hasBlockerLeft = Physics.Raycast(leftRayOrigin + player.transform.up * .02f,
                                                  forwardDir,
                                                  out RaycastHit blockerHitLeft,
                                                  dist,
                                                  player.worldLayer);

            if (hasLedgeLeft)
            {
                Debug.DrawRay(leftRayOrigin, forwardDir * ledgeHitLeft.distance, Color.green);
            }
            else
            {
                Debug.DrawRay(leftRayOrigin, forwardDir * dist, Color.red);
            }

            if (hasBlockerLeft)
            {
                Debug.DrawRay(leftRayOrigin + player.transform.up * .01f,
                              forwardDir * blockerHitLeft.distance,
                              Color.red);
            }
            else
            {
                Debug.DrawRay(leftRayOrigin + player.transform.up * .01f,
                              forwardDir * dist,
                              Color.green);
            }

            canMoveLeft = hasLedgeLeft && !hasBlockerLeft;

        }

        public void FixedUpdateState(PlayerController player) 
        {
            float horizontal = player.playerControls.BaseMovement.Move.ReadValue<Vector2>().x;
            Vector3 moveDir = rightDir * horizontal;
            if (horizontal > 0 && canMoveRight)
            {
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = moveDir * player.ledgeMoveSpeed;
                #else
                player.rb.velocity = moveDir * player.ledgeMoveSpeed;
                #endif

                if (player.mode == PerspectiveMode.FirstPerson)
                    player.firstPersonVisualArms.transform.position = player.transform.TransformPoint(offset);

                if (!player.animator.GetCurrentAnimatorStateInfo(0).IsName("LedgeRight"))
                {
                    player.ChangeAnimation("LedgeRight");
                }
            }
            else if (horizontal < 0 && canMoveLeft)
            {
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = moveDir * player.ledgeMoveSpeed;
                #else
                player.rb.velocity = moveDir * player.ledgeMoveSpeed;
                #endif

                if (player.mode == PerspectiveMode.FirstPerson)
                    player.firstPersonVisualArms.transform.position = player.transform.TransformPoint(offset);

                if (!player.animator.GetCurrentAnimatorStateInfo(0).IsName("LedgeLeft"))
                {
                    player.ChangeAnimation("LedgeLeft");
                }
            }
            else
            {
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = Vector3.zero;
                #else
                player.rb.velocity = Vector3.zero;
                #endif
                if (!player.animator.GetCurrentAnimatorStateInfo(0).IsName("LedgeIdle"))
                {
                    player.ChangeAnimation("LedgeIdle");
                }

            }
        }

        public void ExitState(PlayerController player) 
        {
            player.canMove = true;
            player.gravityComponent.enabled = true;

            //Hands
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.firstPersonVisualArms.transform.parent = player.transform;
                player.firstPersonVisualArms.transform.rotation = player.transform.rotation;
                player.firstPersonVisualArms.transform.localPosition = Vector3.zero;
            }
            else if (player.mode == PerspectiveMode.ThirdPerson)
            { 
                player.GetComponent<PlayerRotation>().canRotateVisual = true;
            }
            //
        }
        private string PlayerPrefsKey => "LedgeState_Unlocked";
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
