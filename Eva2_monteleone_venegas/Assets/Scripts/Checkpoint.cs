using UnityEngine;

/// <summary>
/// Guarda el punto de reaparición del jugador. Collider2D con "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private AudioClip checkpointClip;
    [SerializeField] private string message = "Punto de guardado";
    private bool activated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (!other.TryGetComponent(out PlayerController player)) return;

        activated = true;
        player.SetCheckpoint(transform.position);
        AudioManager.Play(checkpointClip);

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowMessage(message, 2f);
        }
    }
}
