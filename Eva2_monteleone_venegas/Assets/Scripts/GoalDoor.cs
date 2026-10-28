using UnityEngine;

/// <summary>
/// Puerta del faro: solo deja ganar si se reunieron todos los fragmentos.
/// Collider2D con "Is Trigger".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GoalDoor : MonoBehaviour
{
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip lockedClip;
    [SerializeField] private string lockedMessage = "El faro necesita más fragmentos de luz...";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerController _)) return;
        if (GameManager.Instance == null || GameManager.Instance.IsFinished) return;

        if (GameManager.Instance.HasAllFragments)
        {
            AudioManager.Play(openClip);
            GameManager.Instance.WinGame();
        }
        else
        {
            AudioManager.Play(lockedClip);
            if (HUDController.Instance != null)
            {
                int missing = GameManager.Instance.FragmentsToWin - GameManager.Instance.Fragments;
                HUDController.Instance.ShowMessage($"{lockedMessage} (faltan {missing})", 3f);
            }
        }
    }
}
