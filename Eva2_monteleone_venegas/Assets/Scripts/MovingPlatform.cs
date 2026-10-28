using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plataforma móvil. Requiere Rigidbody2D en modo KINEMATIC + Collider2D.
/// Lleva consigo a los cuerpos que están encima.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 travel = new Vector2(4f, 0f);   // desplazamiento total
    [SerializeField] private float speed = 2f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private readonly List<Rigidbody2D> riders = new List<Rigidbody2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;
    }

    private void FixedUpdate()
    {
        float length = Mathf.Max(travel.magnitude, 0.01f);
        float t = Mathf.PingPong(Time.time * speed / length, 1f);
        Vector2 target = startPosition + travel * t;
        Vector2 delta = target - rb.position;

        rb.MovePosition(target);

        foreach (Rigidbody2D rider in riders)
        {
            if (rider != null) rider.position += delta;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D body = collision.rigidbody;
        if (body != null && collision.transform.position.y > transform.position.y && !riders.Contains(body))
        {
            riders.Add(body);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null) riders.Remove(collision.rigidbody);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Gizmos.DrawLine(origin, origin + (Vector3)travel);
    }
}
