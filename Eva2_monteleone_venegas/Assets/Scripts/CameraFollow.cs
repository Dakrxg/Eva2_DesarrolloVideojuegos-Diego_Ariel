using UnityEngine;

/// <summary>
/// Cámara que sigue al jugador con suavizado y límites del mapa.
/// Va en la Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Límites (opcional)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(0f, 0f);
    [SerializeField] private Vector2 maxBounds = new Vector2(40f, 10f);

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        if (useBounds)
        {
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
