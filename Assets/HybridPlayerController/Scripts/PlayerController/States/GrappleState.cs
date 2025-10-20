using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace HybridPlayerController
{

    public class GrappleState : IState
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
        private SpringJoint joint;
        private Vector3 grapplePoint;
        public void EnterState(PlayerController player)
        {
            player.ChangeAnimation("Grapple");
            player.justJumped = false;
            player.canMove = false;

            joint = player.AddComponent<SpringJoint>();
            grapplePoint = player.grapplePoint;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            float distanceFromPoint = Vector3.Distance(player.transform.position, grapplePoint);
            joint.maxDistance = distanceFromPoint * 0.7f;
            joint.minDistance = distanceFromPoint * 0.25f;
            joint.spring = 4.5f;
            joint.damper = 7;
            joint.massScale = 4.5f;

            player.cam.GetComponent<CamUtils>().ChangeFOV(player.fastMoveFOVChange, .3f);

        }

        public void UpdateState(PlayerController player)
        {
            if (player.mode == PerspectiveMode.FirstPerson)
            {
                player.grappleLine.SetPosition(0, player.firstPersonHandPos.position);

            }
            else if (player.mode == PerspectiveMode.ThirdPerson)
            { 
                player.grappleLine.SetPosition(0, player.thirdPersonHandPos.position);

            }
            player.grappleLine.SetPosition(1, player.grapplePoint);

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
        }

        public void FixedUpdateState(PlayerController player) 
        { 
    
        }
        public void ExitState(PlayerController player)
        {
            Object.Destroy(joint);
            player.canMove = true;
            player.grappleLine.SetPosition(0, player.transform.position);
            player.grappleLine.SetPosition(1, player.transform.position);
            player.cam.GetComponent<CamUtils>().ChangeFOV(-player.fastMoveFOVChange, .3f);
        }
        private string PlayerPrefsKey => "GrappleState_Unlocked";
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
