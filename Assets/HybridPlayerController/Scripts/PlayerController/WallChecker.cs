using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class WallChecker : MonoBehaviour
    {
        public PlayerController player;
        [HideInInspector] public int rayCount = 13;        //Number of rays for forward cast
        [HideInInspector] public float heightStep = 0.25f; //Height increment per ray
        [HideInInspector] public float rayDistance = .8f;  //Maximum ray distance

        public RaycastHit? highestHit;
        public RaycastHit? floorHit;
        public RaycastHit? topOfWall;

        //Check if left wall after wall running
        //A nullable raycastHit is set and checked in rising and falling state
        public RaycastHit? wallRunWallHit;//For telling wallRunState weather or not the player left the wall before wall running again 
        [HideInInspector] public Vector3 wallRunForward;//set in wallRunState for wallRunWallHit
        //

        void Update()
        {
            CastRays();
        }

        void FixedUpdate()
        {
            //For telling wallRunState weather or not the player left the wall before wall running again 
            if (player.justWallRanLeft && wallRunForward != Vector3.zero)
            {
                float wallRunRayDistance = .5f + .5f;
                Vector3 dir = Vector3.Cross(wallRunForward, Vector3.up).normalized;//left of wallRunForward
                if (Physics.Raycast(player.transform.position, dir, out RaycastHit hit, wallRunRayDistance, player.worldLayer, QueryTriggerInteraction.Ignore))
                {
                    wallRunWallHit = hit;
                    Debug.DrawRay(player.transform.position, dir * wallRunRayDistance, Color.red, 1);
                }
                else 
                {
                    wallRunWallHit = null;
                }
                
            }
            else if (player.justWallRanRight && wallRunForward != Vector3.zero)
            {
                float wallRunRayDistance = .5f + .5f;
                Vector3 dir = Vector3.Cross(wallRunForward, Vector3.up).normalized;//right of wallRunForward
                if (Physics.Raycast(player.transform.position, dir, out RaycastHit hit, wallRunRayDistance, player.worldLayer, QueryTriggerInteraction.Ignore))
                {
                    wallRunWallHit = hit;
                    Debug.DrawRay(player.transform.position, dir * wallRunRayDistance, Color.red, 1);
                }
                else
                {
                    wallRunWallHit = null;
                }
            }
            else
            {
                wallRunWallHit = null;
            }
            //
        }

        void CastRays()//For ledge detection
        {
            List<RaycastHit> wallHits = new List<RaycastHit>();//All wall hits
            List<RaycastHit> closestHits = new List<RaycastHit>();//Closest wall hits

            Vector3 startPosition = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z);

            for (int i = 0; i < rayCount; i++)
            {
                Vector3 rayOrigin = startPosition + new Vector3(0, i * heightStep, 0);

                if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit arrayHit, rayDistance, player.worldLayer))
                {
                    if (Mathf.Abs(arrayHit.normal.y) < 0.001f)//is a wall //Mathf.Approximately(hit.normal.y, 0)
                    {
                        wallHits.Add(arrayHit);
                        if (player.playerUtils.drawWallChecker)
                            Debug.DrawLine(rayOrigin, arrayHit.point, Color.red);
                    }
                    else
                    {
                        if (player.playerUtils.drawWallChecker)
                            Debug.DrawLine(rayOrigin, arrayHit.point, Color.red);
                    }
                }
                else
                {
                    if (player.playerUtils.drawWallChecker)
                        Debug.DrawRay(rayOrigin, transform.forward * rayDistance, Color.blue);
                }
            }

            //First determine which hits are the closest to the player. THEN out of thoes hit, find the highest
            if (wallHits.Count > 0)
            {
                float closestDist = rayDistance;
                foreach (RaycastHit hit in wallHits)
                {
                    float hitDistance = transform.InverseTransformPoint(hit.point).z;
                    if (hitDistance == closestDist)
                    {
                        closestHits.Add(hit);
                    }
                    else if (hitDistance < closestDist)
                    {
                        closestHits.Clear();
                        closestDist = hitDistance;
                        closestHits.Add(hit);
                    }
                    else
                    {
                        closestHits.Remove(hit);
                    }
                }
            }
            else
            {
                closestHits.Clear();
            }

            //Determine which hit is the highest out of the closest hits
            if (closestHits.Count > 0)
            {
                highestHit = wallHits[0];
                foreach (RaycastHit hit in closestHits)
                {
                    if (hit.point.y > highestHit.Value.point.y)
                    {
                        highestHit = hit;
                    }
                }
            }
            else
            {
                //I made highestHit (a RaycastHit, which is a struct) nullable so that I can avoid using default struct values. If there is no highest hit, then I want highest hit to mean "no hit"
                highestHit = null;
            }

            //For detecting a floor above the wall in front of player. If there is no floor, then the wall is thin and therefore vaultable.
            if (highestHit.HasValue 
                && Physics.Raycast((highestHit.Value.point + transform.up * .30f) + (transform.forward * 0.5f), -transform.up, out RaycastHit flrhit, Vector3.Distance(highestHit.Value.point, highestHit.Value.point + transform.up * .30f) + .1f, player.worldLayer))
            {
                //there is a floor above the wall
                floorHit = flrhit;
                if (player.playerUtils.drawWallChecker)
                    Debug.DrawRay((highestHit.Value.point + transform.up * .30f) + (transform.forward * 0.5f), -transform.up * flrhit.distance, Color.red, .1f);
            }
            else if (highestHit.HasValue)
            {
                //there is no floor above the wall
                floorHit = null;
                if (player.playerUtils.drawWallChecker)
                    Debug.DrawRay((highestHit.Value.point + transform.up * .30f) + (transform.forward * 0.5f), -transform.up * Vector3.Distance(highestHit.Value.point, highestHit.Value.point + transform.up * .30f), Color.blue, .1f);
            }
            else
            {
                floorHit = null;
            }

            //for getting top of wall
            if (highestHit.HasValue)
            { 
                float maxDistance = -1 + (heightStep * rayCount);

                Vector3 origin = (highestHit.Value.point + Vector3.up * .251f) + highestHit.Value.normal * -.01f;//Upward by 0.25 and some change because if it is lower, the origin may be within the wall and the ray will hit the floor
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit topOfWallHit, maxDistance, player.worldLayer))
                {
                    topOfWall = topOfWallHit;
                    if (player.playerUtils.drawWallChecker)
                        Debug.DrawRay(origin, Vector3.down * Vector3.Distance(origin, topOfWall.Value.point), Color.red, 0);

                }
                else
                {
                    topOfWall = null;
                    if (player.playerUtils.drawWallChecker)
                        Debug.DrawRay(origin, Vector3.down * maxDistance, Color.green, 0);
                }
            }
            else
            {
                topOfWall = null;
            }
        }

        public bool HasCeilingAbove()//used in crouchState, crouchWalkState, slideState, and jumpState
        {
            float topOfHeadStand = 1f;//while standing (this is the top of the capsule)
            float topOfHeadCrouch = 0f;//while crouched (this is the center of the capsule)
            
            float rayHeightStand = .1f;//just a little above player's "head"
            float rayHeightCrouch = 1f;//height of standing player (when not crouching)

            if ((player.IsInState<CrouchState>()
                || player.IsInState<CrouchWalkState>()
                || player.IsInState<SlideState>())
                && Physics.Raycast(player.transform.position + Vector3.up * topOfHeadCrouch, Vector3.up, rayHeightCrouch, player.worldLayer, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
            else if (Physics.Raycast(player.transform.position + Vector3.up * topOfHeadStand, Vector3.up, rayHeightStand, player.worldLayer, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
            else
            { 
                return false;
            }
        }
        private void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            if (player.playerUtils.drawWallChecker)
            {
                if (highestHit.HasValue)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(highestHit.Value.point, 0.05f);
                }
                if (topOfWall.HasValue)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(topOfWall.Value.point, 0.05f);
                }

                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 10;
                style.alignment = TextAnchor.MiddleCenter;

                Vector3 startPosition = new Vector3(transform.position.x, transform.position.y - 1, transform.position.z) + transform.forward * (rayDistance + .5f);
                for (int i = 0; i < rayCount; i++)
                {
                    // Adjust the ray's origin based on the height step
                    Vector3 textPos = startPosition + new Vector3(0, i * heightStep, 0);
                    UnityEditor.Handles.Label(textPos, "" + transform.InverseTransformPoint(textPos).y, style);

                }
            }
    #endif
        }
    }
}
