using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    [RequireComponent(typeof(BoxCollider))]
    public class DeathZone : MonoBehaviour
    {
        public BoxCollider boxCollider;//set in inspector
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponent<PlayerController>().Die();
            }
        }

        private void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            Matrix4x4 originalMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Color transparentRed = new Color(1, 0, 0, 0.7f);
            Gizmos.color = transparentRed;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
    #endif
        }
    }
}
