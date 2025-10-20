using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class JumpState : IState
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
            player.ChangeAnimation("Jump");
        }
        public void UpdateState(PlayerController player) 
        {
            //Jump
            if (player.justWallRanRight)
            {
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, player.jumpForce, player.rb.linearVelocity.z);
                #else
                player.rb.velocity = new Vector3(player.rb.velocity.x, player.jumpForce, player.rb.velocity.z);
                #endif
                player.justJumped = true;
            }
            else if (player.justWallRanLeft)
            {
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, player.jumpForce, player.rb.linearVelocity.z);
                #else
                player.rb.velocity = new Vector3(player.rb.velocity.x, player.jumpForce, player.rb.velocity.z);
                #endif
                player.justJumped = true;
            }
            else if(!player.justJumped && !player.HasCeilingAbove)//normal jump if there is no ceiling above player
            {
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + .08f, player.transform.position.z);//setting height up just enough so that the ground checker does not hit ground. Rising state checks for is grounded, so breaks immediately otherwise
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, player.jumpForce, player.rb.linearVelocity.z);
                #else
                player.rb.velocity = new Vector3(player.rb.velocity.x, player.jumpForce, player.rb.velocity.z);
                #endif
                player.justJumped = true;
            }
            else if(player.justJumped && player.extraJumps > 0 && !player.HasCeilingAbove)//extra jump
            {
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + .08f, player.transform.position.z);//setting height up just enough so that the ground checker does not hit ground. Rising state checks for is grounded, so breaks immediately otherwise
                #if UNITY_6000_0_OR_NEWER
                player.rb.linearVelocity = new Vector3(player.rb.linearVelocity.x, player.jumpForce, player.rb.linearVelocity.z);
#else
                player.rb.velocity = new Vector3(player.rb.velocity.x, player.jumpForce, player.rb.velocity.z);
#endif
                player.extraJumpCount--;
            }
            else
            {
                player.TransitionToState<IdleState>();
                return;
            }

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

        public void FixedUpdateState(PlayerController player) { }
        public void ExitState(PlayerController player) 
        {

        }
        private string PlayerPrefsKey => "JumpState_Unlocked";
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
