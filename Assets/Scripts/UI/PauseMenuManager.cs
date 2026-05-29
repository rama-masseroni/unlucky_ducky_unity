using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject container;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Pause();
        }
    }

    public void ResumeButton()
    {
        if (container != null)
        {
            container.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void Pause()
    {
        if (container != null)
        {
            container.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void ReturnToMainMenuButton()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }
}
