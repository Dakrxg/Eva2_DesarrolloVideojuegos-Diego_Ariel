using UnityEngine;

/// <summary>
/// Fragmento de luz recolectable. Collider2D con "Is Trigger" activado.
/// Convertir en PREFAB y reutilizar las 5 veces.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour
{
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private string pickupMessage = "Fragmento de luz recuperado";
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        // Flotación suave para llamar la atención
        float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + Vector3.up * offset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerController _)) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.AddFragment();
        AudioManager.Play(pickupClip);

        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowMessage(
                $"{pickupMessage} ({GameManager.Instance.Fragments}/{GameManager.Instance.FragmentsToWin})", 2f);
        }

        Destroy(gameObject);
    }
}
