using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Métodos para los botones de UI (MainMenu, GameOver, Ending).
/// En el botón: OnClick() -> arrastrar este objeto -> MenuButtons.LoadScene (escribir el nombre de la escena).
/// </summary>
public class MenuButtons : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private float delayBeforeLoad = 0.2f;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadRoutine(sceneName));
    }

    public void QuitGame()
    {
        AudioManager.Play(clickClip);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        AudioManager.Play(clickClip);
        yield return new WaitForSecondsRealtime(delayBeforeLoad);
        SceneManager.LoadScene(sceneName);
    }
}
