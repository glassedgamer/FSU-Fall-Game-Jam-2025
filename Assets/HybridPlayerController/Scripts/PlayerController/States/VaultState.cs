using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{
    public class VaultState : IState
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
        private float vaultDuration;//in PlayerController
        private float vaultDistance = 2f;
        private float vaultHeight = 1f;

        private bool canVault = false;

        private bool didJump; //for fov, see exit state

        public void EnterState(PlayerController player)
        {
            player.cam.GetComponent<CamUtils>().ChangeFOV(player.fastMoveFOVChange, .1f);
            didJump = false;
            player.ChangeAnimation("Vault");

            vaultDuration = player.vaultTime;
            player.StartCoroutine(VaultOverWall(player));
        }

        private IEnumerator VaultOverWall(PlayerController player)
        {
            canVault = true;

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
                //simple parabola: f(t)=t(1-t)
                float arc = 4 * arcHeight * t * (1 - t);
                currentPos.y += arc;

                player.transform.position = currentPos;

                //if at (a little after) peak of arc
                if (Mathf.Abs(t - 0.625f) < Time.deltaTime / vaultDuration)
                {
                    //if jump button is being held && there is no platform above the wall
                    if (player.playerControls.BaseMovement.Jump.IsPressed() && !floor)
                    {
                        canVault = false;
                        didJump = true;
                        player.TransitionToState<VaultJumpState>();
                        yield break;
                    }
                }
                yield return null;
            }
            player.transform.position = endPos;

            player.TransitionToState<IdleState>();
            yield break;
        }

        public void UpdateState(PlayerController player)
        {
        
        }

        public void FixedUpdateState(PlayerController player)
        {
        
        }

        public void ExitState(PlayerController player)
        {
            if (!didJump)
            {
                player.cam.GetComponent<CamUtils>().ChangeFOV(-player.fastMoveFOVChange, .3f);
            }
        }
        private string PlayerPrefsKey => "VaultState_Unlocked";
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
