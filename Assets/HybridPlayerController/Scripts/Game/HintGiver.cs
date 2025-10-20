using UnityEngine;
using TMPro;
namespace HybridPlayerController
{

    [RequireComponent(typeof(BoxCollider))]
    public class HintGiver : MonoBehaviour
    {
        public BoxCollider boxCollider;//set in inspector
        public TMP_Text hintText;//set in inspector
        public Animator textAnimator;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                textAnimator.Play("HintTextAppear");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                textAnimator.Play("HintTextDisappear");
            }
        }
        private void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            Matrix4x4 originalMatrix = Gizmos.matrix;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Color transparentYellow = new Color(1f, 1f, 0f, 0.7f);
            Gizmos.color = transparentYellow;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            UnityEditor.Handles.Label(transform.position, "Hint", style);
    #endif
        }
    }
}
