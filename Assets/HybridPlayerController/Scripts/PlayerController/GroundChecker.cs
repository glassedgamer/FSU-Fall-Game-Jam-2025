using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
namespace HybridPlayerController
{

    [RequireComponent(typeof(PlayerController))]
    public class GroundChecker : MonoBehaviour
    {
        [Space(7), Header("Settings")]
        public PlayerController player;
        public Vector3 sphereOffset = new Vector3(0, 0, 2);
        public float sphereRadius = .5f;
        public float groundedRayDistance = 1.1f;
        public float slopeRayDistance = 1.6f;
        public Collider playerCollider;
    
        #if UNITY_6000_0_OR_NEWER
        public PhysicsMaterial noFric;
        public PhysicsMaterial highFric;
        #endif

        #if !UNITY_6000_0_OR_NEWER
        public PhysicMaterial noFric;
        public PhysicMaterial highFric;
        #endif

        private LayerMask worldLayer;

        [HideInInspector] public RaycastHit slopeHit;
        [HideInInspector] public RaycastHit checkSlopeHit;

        [Space(7), Header("     Read Only")]
        [ReadOnly] public Vector3 slopeMoveVector;


        private void Awake()
        {
            worldLayer = player.worldLayer;
        }
        private void FixedUpdate()
        {
            Physics.Raycast(transform.position, Vector3.down, out slopeHit, 2.5f, worldLayer);

            //Setting slopMoveVector
            if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, slopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore))
            {
                slopeMoveVector = Vector3.ProjectOnPlane(player.moveVector, slopeHit.normal);//Set slope vector
            }
            //
        }

        public bool IsGrounded()
        {
            Vector3 worldSphereCenter = transform.position + transform.rotation * sphereOffset;

            LayerMask layers = worldLayer | player.platformingLayer;//include platforming layer for the moving platforms
            Collider[] colliders = Physics.OverlapSphere(worldSphereCenter, sphereRadius, layers, QueryTriggerInteraction.Ignore);

            Physics.Raycast(transform.position, Vector3.down, out RaycastHit testAboveSlopeHit, slopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore);
            bool aboveSlope = testAboveSlopeHit.normal != Vector3.up;

            float rayCheckDistance = aboveSlope ? slopeRayDistance : groundedRayDistance;//We change the ray distance when above a slope so that the player can become grounded on slope, otherwise the downward ray check does not reach the ground (the slope)
            if ((player.IsInState<RisingState>() //These are state where extra checks are needed for bring truly grounded
                || player.IsInState<FallingState>()
                || player.IsInState<WallRunState>()
                || player.IsInState<IdleState>()
                || player.IsInState<LedgeState>())
                && colliders.Length > 0
                && Physics.Raycast(transform.position, Vector3.down, rayCheckDistance, layers, QueryTriggerInteraction.Ignore))
                //extra checks (other than colliders) in these  states so that the player is only grounded when intended. Otherwise, isGrounded would be true when jumping into a wall becuase the collider check hits the wall and "thinks" its a floor
            {
                return true;
            }
            else if(colliders.Length > 0
                && !player.IsInState<RisingState>()
                && !player.IsInState<FallingState>()
                && !player.IsInState<WallRunState>()
                && !player.IsInState<IdleState>()//Although being in IdleState is usually on the ground, Idle state is the default state, so most states transition back to this one to effectively reset the player's state. Sometimes this reset happens in a situation where being truly grounded requires extra checks. Otherwise un-intended behavior, like loops, occur.
                && !player.IsInState<LedgeState>()
                && !player.IsInState<JumpState>()/*player should never become grounded in jumpstate, as the state always immediately changes to another (risingState usually). Otherwise un-intended behavior occurs.*/)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsOnSlope()
        {
            //Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore)
            //shoot a ray from each side of the collider so that even if the collider is on an edge, the slope will still register.
            RaycastHit hit;
            float backUpSlopeRayDistance = 1.3f;//.3f out from the bottom of player
            if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore)
                || Physics.Raycast(transform.position + transform.forward * 1, Vector3.down, out hit, backUpSlopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore) 
                || Physics.Raycast(transform.position + transform.forward * -1, Vector3.down, out hit, backUpSlopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore)
                || Physics.Raycast(transform.position + transform.right * 1, Vector3.down, out hit, backUpSlopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore)
                || Physics.Raycast(transform.position + transform.right * -1, Vector3.down, out hit, backUpSlopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger
                    && !player.IsInState<JumpState>()
                    && IsGrounded()
                    && hit.normal != Vector3.up)
                {
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    if (angle >= 40)
                    {
                        player.isOnSteepSlope = true;

                        if (!player.IsInState<SlipState>())
                            player.TransitionToState<SlipState>();

                    }
                    else if (player.IsInState<SlipState>() && angle < 40)
                    {
                        player.isOnSteepSlope = false;
                    }
                    return true;
                }
            }
            player.isOnSteepSlope = false;
            return false;
        }
        void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            if (player.playerUtils.drawMoveVector)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + slopeMoveVector);
            
            }
            bool aboveSlope = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeRayDistance, worldLayer, QueryTriggerInteraction.Ignore);
            float rayCheckDistance = aboveSlope ? slopeRayDistance : groundedRayDistance;
            if (Physics.Raycast(transform.position, Vector3.down, groundedRayDistance, worldLayer, QueryTriggerInteraction.Ignore))
            {
                Debug.DrawRay(transform.position, Vector3.down * rayCheckDistance, Color.red);
            }
            else
            {
                Debug.DrawRay(transform.position, Vector3.down * rayCheckDistance, Color.blue);
            }
            if (IsGrounded() && player.playerUtils.drawGroundChecker)
            {

                Gizmos.color = Color.red;
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireSphere(sphereOffset, sphereRadius);
            }
            else if (player.playerUtils.drawGroundChecker)
            {
                Gizmos.color = Color.blue;
                Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                Gizmos.matrix = rotationMatrix;
                Gizmos.DrawWireSphere(sphereOffset, sphereRadius);
            }
    #endif
        }
    }
}