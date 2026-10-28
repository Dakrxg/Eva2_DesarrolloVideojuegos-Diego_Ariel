using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Interfaz del nivel: puntaje, vidas, fragmentos y mensajes de historia.
/// Va en el Canvas de Level1.
/// </summary>
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [SerializeField] private TMP_Text fragmentsText;
    [SerializeField] private TMP_Text messageText;

    private Coroutine messageRoutine;

    private void Awake()
    {
        Instance = this;
        if (messageText != null) messageText.text = "";
    }

    private void OnEnable()
    {
        GameManager.OnStatsChanged += Refresh;
    }

    private void OnDisable()
    {
        GameManager.OnStatsChanged -= Refresh;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Refresh();
    }

    private void Refresh()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (scoreText != null) scoreText.text = $"Puntaje: {gm.Score}";
        if (livesText != null) livesText.text = $"Vidas: {gm.Lives}";
        if (fragmentsText != null) fragmentsText.text = $"Fragmentos: {gm.Fragments}/{gm.FragmentsToWin}";
    }

    public void ShowMessage(string text, float seconds = 4f)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    private IEnumerator MessageRoutine(string text, float seconds)
    {
        messageText.text = text;
        yield return new WaitForSeconds(seconds);
        messageText.text = "";
    }
}
