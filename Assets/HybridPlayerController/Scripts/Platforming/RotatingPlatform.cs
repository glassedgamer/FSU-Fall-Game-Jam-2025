using UnityEngine;

public class RotatingPlatform : MonoBehaviour
{
    public float rotateSpeed = 100;
    void Update()
    {
        transform.Rotate(new Vector3(0, 1, 0) * (rotateSpeed * Time.deltaTime));
    }
}
