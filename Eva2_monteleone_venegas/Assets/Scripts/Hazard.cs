using UnityEngine;

/// <summary>
/// Peligro (pinchos, agua, zona de caída). Collider2D con "Is Trigger" activado.
/// Para la zona de caída: un trigger muy ancho debajo del mapa.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerController player))
        {
            player.TakeHit();
        }
    }
}
