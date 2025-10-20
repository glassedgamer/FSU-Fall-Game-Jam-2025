using System;
using UnityEngine;

namespace HybridPlayerController
{
    [RequireComponent(typeof(BoxCollider))]
    public class UnlockStateTrigger : MonoBehaviour
    {
        [Tooltip("Exact class name of the state, e.g. \"WallRunState\"")]
        public string stateToUnlockName;

        PlayerController _player;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _player = other.GetComponent<PlayerController>();
            if (_player == null) return;

            var t = Type.GetType($"HybridPlayerController.{stateToUnlockName}");
            if (t == null)
            {
                Debug.LogWarning($"No state class named {stateToUnlockName}");
                return;
            }

            var st = _player.GetState(t);
            StartCoroutine(DelayUnlock(st));
        }

        System.Collections.IEnumerator DelayUnlock(IState st)
        {
            yield return null;
            st.isUnlocked = true;
            Destroy(gameObject);
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var box = GetComponent<BoxCollider>();
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            var col = new Color(1, 1, 0, 0.7f);
            Gizmos.color = col;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);

            var style = new GUIStyle
            {
                normal = { textColor = Color.white },
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            UnityEditor.Handles.Label(transform.position, $"Unlock {stateToUnlockName}", style);
#endif
        }
    }
}
