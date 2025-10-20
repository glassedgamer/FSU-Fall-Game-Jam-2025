using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class SwingBarState : IState
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
        private SwingBar swingBar;
        private float momentum;
        public void EnterState(PlayerController player)
        {
            momentum = 90;
            player.justJumped = false;
            swingBar = player.mySwingBar;
            player.canMove = false;
            player.gravityComponent.enabled = false;
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = Vector3.zero;
            #else
            player.rb.velocity = Vector3.zero;
            #endif

            player.mySwingBar.StartSwing();
            player.ChangeAnimation("SwingBar");

            Vector3 toPlayer = (player.transform.position - player.mySwingBar.transform.position).normalized;
            Vector3 barFarward = player.mySwingBar.transform.forward;

            float side = Vector3.Dot(barFarward, toPlayer);
            player.firstPersonVisualArms.SetActive(false);

            if (side > 0)
            {
                player.mySwingBar.angleNegate = 1;

                Quaternion lookAtBar = Quaternion.LookRotation(-player.mySwingBar.transform.forward);
                Vector3 euler = lookAtBar.eulerAngles;

                player.GetComponent<PlayerRotation>().SnapRotation(0, euler.y, .1f);
            }
            else if (side < 0)
            {
                player.mySwingBar.angleNegate = -1;

                Quaternion lookAtBar = Quaternion.LookRotation(player.mySwingBar.transform.forward);
                Vector3 euler = lookAtBar.eulerAngles;

                player.GetComponent<PlayerRotation>().SnapRotation(0, euler.y, .1f);
            }

            if (player.mySwingBar != null)
            {
                player.transform.position = player.mySwingBar.playerPos.position;
            }

        }

        public void UpdateState(PlayerController player)
        {
            if (momentum > 0 && player.playerControls.BaseMovement.Move.ReadValue<Vector2>().y == 0)
            {
                momentum -= Time.deltaTime * 20;
            }
            else if (momentum < 90 && player.playerControls.BaseMovement.Move.ReadValue<Vector2>().y != 0)
            {
                momentum += Time.deltaTime * 30;
            }
            if (player.playerControls.BaseMovement.Jump.triggered)
            {
                player.TransitionToState<JumpState>();//with specific momentum from swing?
                return;
            }
            //Swinging
            if (player.mySwingBar != null)
            {
                player.transform.position = player.mySwingBar.playerPos.position;
                float swingLimit = Mathf.Clamp(momentum, 0, 90);
                player.mySwingBar.limit = swingLimit;
            }
            //
        }

        public void FixedUpdateState(PlayerController player) 
        {
        
        }

        public void ExitState(PlayerController player) 
        {
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = Vector3.zero;
            #else
            player.rb.velocity = Vector3.zero;
            #endif
            player.transform.position = player.mySwingBar.playerPos.position;
            player.canMove = true;
            player.gravityComponent.enabled = true;
            player.mySwingBar.ResetSwing();
            player.lastSwingBar = player.mySwingBar;
            player.mySwingBar = null;
            if (player.mode == PerspectiveMode.FirstPerson)
                player.firstPersonVisualArms.SetActive(true);
        }
        private string PlayerPrefsKey => "SwingBarState_Unlocked";
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
