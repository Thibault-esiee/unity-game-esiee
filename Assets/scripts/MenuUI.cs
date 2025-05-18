using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI: MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("Floor1 1");
    }

    public void Quit()
    {
        Application.Quit();
    }
}