using UnityEngine;

/// <summary>
/// Zona invisible que muestra un texto de historia al pasar el jugador (una sola vez).
/// Collider2D con "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StoryTrigger : MonoBehaviour
{
    [SerializeField, TextArea(2, 4)] private string message;
    [SerializeField] private float duration = 5f;

    private bool shown;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (shown) return;
        if (!other.TryGetComponent(out PlayerController _)) return;

        shown = true;
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowMessage(message, duration);
        }
    }
}
