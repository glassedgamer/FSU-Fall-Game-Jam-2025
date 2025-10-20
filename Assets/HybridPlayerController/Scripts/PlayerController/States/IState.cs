using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{
    public interface IState
    {
        public bool isUnlocked { get; set; }//implemented as a property because interfaced cant use fields
        void EnterState(PlayerController player);
        void UpdateState(PlayerController player);
        void FixedUpdateState(PlayerController player);
        void ExitState(PlayerController player);

        public void LoadUnlockStatus();
    }
}
