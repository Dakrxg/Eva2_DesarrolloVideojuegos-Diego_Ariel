using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de historia con texto que se escribe solo (intro y final).
/// Espacio / Enter / clic = avanzar. Al terminar carga la escena indicada.
/// Se usa en las escenas "Intro" y "Ending".
/// </summary>
public class StorySequence : MonoBehaviour
{
    [SerializeField] private TMP_Text storyText;
    [SerializeField, TextArea(3, 6)] private string[] lines;
    [SerializeField] private string nextScene = "Level1";
    [SerializeField] private float charsPerSecond = 40f;
    [SerializeField] private AudioClip advanceClip;

    private int index;
    private bool isTyping;
    private Coroutine typingRoutine;

    private void Start()
    {
        Time.timeScale = 1f;
        ShowLine();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            Advance();
        }
    }

    private void Advance()
    {
        AudioManager.Play(advanceClip);

        if (isTyping)
        {
            // Primer toque: completa la línea al instante
            StopCoroutine(typingRoutine);
            storyText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        index++;
        if (index >= lines.Length)
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            ShowLine();
        }
    }

    private void ShowLine()
    {
        storyText.text = lines[index];
        storyText.maxVisibleCharacters = 0;
        typingRoutine = StartCoroutine(TypeRoutine());
    }

    private IEnumerator TypeRoutine()
    {
        isTyping = true;
        storyText.ForceMeshUpdate();
        int total = storyText.textInfo.characterCount;
        float delay = 1f / charsPerSecond;

        for (int i = 0; i <= total; i++)
        {
            storyText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
        isTyping = false;
    }
}
