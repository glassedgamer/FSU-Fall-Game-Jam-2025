using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManager : MonoBehaviour
{

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void MainLevel()
    {
        SceneManager.LoadScene("MainLevel");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
