using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el puntaje final en las escenas GameOver y Ending.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private string prefix = "Puntaje final: ";

    private void Start()
    {
        GetComponent<TMP_Text>().text = prefix + GameManager.LastScore;
    }
}
