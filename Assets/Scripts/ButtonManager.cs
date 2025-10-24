using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{

    [SerializeField] GameObject howToPlay;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (howToPlay != null)
        {
            howToPlay.SetActive(false);
        }
    }
    public void MainLevel()
    {
        SceneManager.LoadScene("MainLevel");
    }

    public void HowToPlay()
    {
        howToPlay.SetActive(true);
    }

    public void HowToPlayBack()
    {
        howToPlay.SetActive(false);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
