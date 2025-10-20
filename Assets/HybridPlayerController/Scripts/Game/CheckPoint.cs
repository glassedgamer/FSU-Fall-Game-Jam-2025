using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    [RequireComponent(typeof(BoxCollider))]
    public class CheckPoint : MonoBehaviour
    {
        public GameObject spawnPos;
        public BoxCollider boxCollider;//set in inspector
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                other.GetComponent<PlayerController>().checkpoint = spawnPos.transform;
            }
        }

        private void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            Matrix4x4 originalMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Color transparentBlue = new Color(0, 0, 1, 0.7f);
            Gizmos.color = transparentBlue;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);


            Gizmos.color = Color.blue;

            Gizmos.matrix = originalMatrix;

            Color capsuleColor = Color.blue;
            Gizmos.color = capsuleColor;
            DrawGizmoCapsule(spawnPos.transform.position, 0.5f, 2.0f);

            Vector3 forwardEnd = spawnPos.transform.position + spawnPos.transform.forward * 1f;
            Gizmos.DrawLine(spawnPos.transform.position, forwardEnd);
            Gizmos.DrawWireSphere(forwardEnd, 0.1f);
            Debug.DrawLine(transform.position, spawnPos.transform.position);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            UnityEditor.Handles.Label(spawnPos.transform.position, "Spawn Position", style);
            UnityEditor.Handles.Label(transform.position, "Check Point", style);
    #endif
        }
        private void DrawGizmoCapsule(Vector3 position, float radius, float height)
        {
            Vector3 up = Vector3.up * (height / 2 - radius);
            Gizmos.DrawWireSphere(position + up, radius);
            Gizmos.DrawWireSphere(position - up, radius);
            Gizmos.DrawLine(position + up + Vector3.forward * radius, position - up + Vector3.forward * radius);
            Gizmos.DrawLine(position + up - Vector3.forward * radius, position - up - Vector3.forward * radius);
            Gizmos.DrawLine(position + up + Vector3.right * radius, position - up + Vector3.right * radius);
            Gizmos.DrawLine(position + up - Vector3.right * radius, position - up - Vector3.right * radius);
        }
    }
}
