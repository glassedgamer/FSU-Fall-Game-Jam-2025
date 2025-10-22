using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public int interactCounterVar = 0;
    public int maxInteractCounter = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(interactCounterVar >= maxInteractCounter)
        {
            SceneManager.LoadScene("WinScreen");
        }
    }

    public void InteractCounter()
    {
        interactCounterVar++;
    }
}
