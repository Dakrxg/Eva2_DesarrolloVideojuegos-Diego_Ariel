using UnityEngine;

/// <summary>
/// Trampolín: aplica una fuerza (impulso) hacia arriba al jugador.
/// Collider2D NO trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    [SerializeField] private float bounceForce = 20f;
    [SerializeField] private AudioClip bounceClip;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D body = collision.rigidbody;
        if (body == null) return;

        // Solo si el jugador cae desde arriba
        if (collision.transform.position.y <= transform.position.y) return;

        body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
        body.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
        AudioManager.Play(bounceClip);
    }
}
