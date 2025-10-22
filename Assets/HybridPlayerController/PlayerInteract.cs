using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{

    [SerializeField] float hitDistance = 4f;

    [SerializeField] GameObject crosshair;
    [SerializeField] GameObject gm;

    private void Start()
    {
        //crosshair.SetActive(false);
    }

    private void FixedUpdate()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if(Physics.Raycast(ray, out hit, hitDistance))
        {
            if (hit.collider.gameObject.name == "Pedestal")
            {
                crosshair.SetActive(true);
            } else
            {
                crosshair.SetActive(false);
            }
        }

    }


    void OnInteract(InputValue value)
    {
        if(value.isPressed)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(ray, out hit, hitDistance))
            {
                if(hit.collider.gameObject.name == "Pedestal")
                {
                    crosshair.SetActive(false);
                    gm.GetComponent<GameManager>().InteractCounter();
                    hit.collider.gameObject.GetComponent<PhonePedestal>().RandomallySpawn();
                    crosshair.SetActive(false);
                }
                
            }
        }
    }
}
