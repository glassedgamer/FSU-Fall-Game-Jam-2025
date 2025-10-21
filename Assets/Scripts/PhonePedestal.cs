using UnityEngine;
using UnityEngine.AI;

public class PhonePedestal : MonoBehaviour
{

    public NavMeshAgent agent;
    public Bounds bndFloor;
    public Vector3 moveTo;

    private void Start()
    {
        bndFloor = GameObject.Find("Ground").GetComponent<Renderer>().bounds;
    }

    public void RandomallySpawn()
    {
        float rx = Random.Range(bndFloor.min.x, bndFloor.max.x);
        float rz = Random.Range(bndFloor.min.z, bndFloor.max.z);

        moveTo = new Vector3(rx, this.transform.position.y, rz);

        agent.SetDestination(moveTo);
    }
}
