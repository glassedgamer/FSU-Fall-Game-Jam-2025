using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace HybridPlayerController
{
    [Serializable]
    public struct StateToggle
    {
        public string stateName;  //e.g. "WalkState"
        public bool isUnlocked; //shown and edited in Inspector
    }

    [ExecuteAlways]  //OnValidate will run in the editor
    [RequireComponent(typeof(PlayerController))]
    public class PlayerUtils : MonoBehaviour
    {
        [Header("Debug")]
        public bool drawMoveVector;
        public bool drawGrappleAim;
        public bool drawGroundChecker;
        public bool drawWallChecker;
        public bool drawWallRunCheck;
        public bool drawBarCheck;
        public bool drawCurrentState;
        public bool logStateChanges;

        [Header("Lock/Unlock States")]
        public List<StateToggle> stateToggles = new List<StateToggle>();

        private PlayerController _player;

        //1 in edit-mode, keep the list in sync with all IState types
        private void OnValidate()
        {
            //find all types that implement IState
            var stateTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    typeof(IState).IsAssignableFrom(t) &&
                    !t.IsInterface &&
                    !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToArray();

            //build a new list, preserving any existing unlock flags
            var newList = new List<StateToggle>(stateTypes.Length);
            foreach (var t in stateTypes)
            {
                var existing = stateToggles.FirstOrDefault(x => x.stateName == t.Name);
                newList.Add(new StateToggle
                {
                    stateName = t.Name,
                    isUnlocked = (existing.stateName == t.Name)
                                   ? existing.isUnlocked
                                   : true //default new states to unlocked
                });
            }

            stateToggles = newList;
        }

        //2. at runtime, apply the Inspector settings to the actual states
        private void Start()
        {
            _player = GetComponent<PlayerController>();
            if (_player == null)
                return;

            if (!Application.isPlaying)
                return;

            foreach (var toggle in stateToggles)
            {
                var fullName = "HybridPlayerController." + toggle.stateName;
                var type = Type.GetType(fullName);
                if (type == null)
                {
                    Debug.LogWarning("State type not found: " + fullName);
                    continue;
                }

                try
                {
                    _player.GetState(type).isUnlocked = toggle.isUnlocked;
                }
                catch (KeyNotFoundException)
                {
                    Debug.LogWarning("No runtime state for type: " + fullName);
                }
            }
        }
    }
}
