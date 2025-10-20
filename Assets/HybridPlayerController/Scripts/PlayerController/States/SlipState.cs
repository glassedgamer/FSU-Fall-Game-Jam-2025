using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class SlipState : IState
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
            player.canMove = false;
            player.groundChecker.playerCollider.material = player.groundChecker.noFric;
            player.gravityComponent.enabled = true;
            #if UNITY_6000_0_OR_NEWER
            player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, -10, player.rb.linearVelocity.z);
            #else
            player.rb.velocity = new Vector3(player.rb.velocity.x, -10, player.rb.velocity.z);
            #endif
            player.ChangeAnimation("Slip");
        }

        public void UpdateState(PlayerController player)
        {
            if (player.isGrounded && player.isOnSteepSlope)
            {
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, -10, player.rb.linearVelocity.z);
                #else
                player.rb.velocity = new Vector3(player.rb.velocity.x, -10, player.rb.velocity.z);
                #endif
            }
            else
            {
                player.TransitionToState<IdleState>();
                return;
            }
        }

        public void FixedUpdateState(PlayerController player) { }
        public void ExitState(PlayerController player)
        {
            player.canMove = true;
        }
        private string PlayerPrefsKey => "SlipState_Unlocked";
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
