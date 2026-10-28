using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla el estado global del nivel: vidas, puntaje, fragmentos y cambio de escena.
/// Va UNA vez en la escena Level1 (objeto vacío "GameManager").
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Se guarda al terminar para mostrarlo en las escenas GameOver / Ending
    public static int LastScore { get; private set; }

    // Evento para que la interfaz se actualice sola
    public static event Action OnStatsChanged;

    [Header("Reglas del juego")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int fragmentsToWin = 5;
    [SerializeField] private int pointsPerFragment = 100;
    [SerializeField] private int bonusPerRemainingLife = 200;

    [Header("Escenas")]
    [SerializeField] private string gameOverScene = "GameOver";
    [SerializeField] private string endingScene = "Ending";
    [SerializeField] private float sceneChangeDelay = 1f;

    public int Lives { get; private set; }
    public int Score { get; private set; }
    public int Fragments { get; private set; }
    public int FragmentsToWin => fragmentsToWin;
    public bool HasAllFragments => Fragments >= fragmentsToWin;
    public bool IsFinished { get; private set; }

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Lives = startingLives;
        Score = 0;
        Fragments = 0;
        IsFinished = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddFragment()
    {
        if (IsFinished) return;
        Fragments++;
        Score += pointsPerFragment;
        OnStatsChanged?.Invoke();
    }

    public void LoseLife()
    {
        if (IsFinished) return;
        Lives--;
        OnStatsChanged?.Invoke();

        if (Lives <= 0)
        {
            IsFinished = true;
            LastScore = Score;
            StartCoroutine(LoadSceneAfterDelay(gameOverScene));
        }
    }

    public void WinGame()
    {
        if (IsFinished) return;
        IsFinished = true;
        Score += Lives * bonusPerRemainingLife;
        LastScore = Score;
        OnStatsChanged?.Invoke();
        StartCoroutine(LoadSceneAfterDelay(endingScene));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        yield return new WaitForSeconds(sceneChangeDelay);
        SceneManager.LoadScene(sceneName);
    }
}
