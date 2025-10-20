using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HybridPlayerController
{

    public class MovingPlatform : MonoBehaviour
    {
        public Transform targetPos;
        private Vector3 startPos;
        public float delay;
        public float duration;
        public bool bounce;
        public bool moveOnStart;

        void Start()
        {
            startPos = transform.position;
            targetPos.transform.parent = null;
        }

        private void OnEnable()
        {
            if (moveOnStart)
                StartMove(duration);
        }

        IEnumerator Move(float duration)
        {
            yield return new WaitForSeconds(delay);
            float elapsedTime = 0f;
            Vector3 initialPos = transform.position;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                transform.position = Vector3.Lerp(initialPos, targetPos.position, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos.position;

            if (bounce)
            {
                StartCoroutine(BounceMove(duration));
            }
        }

        IEnumerator BounceMove(float duration)
        {
            yield return new WaitForSeconds(delay);
            float elapsedTime = 0f;
            Vector3 initialPos = transform.position;

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                transform.position = Vector3.Lerp(initialPos, startPos, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = startPos;

            if (bounce)
                StartCoroutine(Move(duration));
        }

        public void StartMove(float duration)
        {
            StartCoroutine(Move(duration));
        }
    }
}
