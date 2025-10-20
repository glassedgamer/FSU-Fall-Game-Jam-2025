using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class SwingBar : MonoBehaviour
    {
        public GameObject pivotObj;
        public Transform playerPos;
        public float colliderWidth;
        private const float colliderThickness = .5f;
        public float speed = 1.5f;
        [HideInInspector] public float limit = 0;
        [HideInInspector] public bool started;
        private float time;
        [HideInInspector] public int angleNegate;//set in swing state
        void Start()
        {
            limit = 0;
            //create a BoxCollider centered on the object
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(colliderWidth, colliderThickness, colliderThickness);
        }
        void Update()
        {
            if (started)
            {
                time += Time.deltaTime * speed;
                float angle = (Mathf.Sin(time) * limit) * angleNegate;
                pivotObj.transform.localRotation = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(0, 0, angle);
            }
        }
        public void StartSwing()
        { 
            started = true;
        }

        public void ResetSwing()
        {
            started = false;
            time = 0;
        }
        private void OnDrawGizmos()
        {
    #if UNITY_EDITOR
            Gizmos.color = Color.blue;
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Vector3 size = new Vector3(colliderWidth, colliderThickness, colliderThickness);
            Gizmos.DrawWireCube(Vector3.zero, size);
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
            Gizmos.DrawCube(Vector3.zero, size);
            Gizmos.matrix = originalMatrix;

            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(playerPos.position, .1f);
    #endif
        }
    }
}
