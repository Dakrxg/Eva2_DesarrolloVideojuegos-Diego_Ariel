using System.Collections;
using UnityEngine;

/// <summary>
/// Movimiento lateral, salto con fuerza (impulso), gravedad ajustada al caer,
/// daño con invulnerabilidad y reaparición en el último checkpoint.
/// Requiere: Rigidbody2D (Dynamic) + Collider2D + SpriteRenderer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float jumpForce = 13f;          // con masa = 1, equivale a la velocidad inicial
    [SerializeField] private float fallGravityMultiplier = 1.8f;
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Detección de suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Daño")]
    [SerializeField] private float invulnerabilityTime = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip hurtClip;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float baseGravity;
    private float moveInput;
    private float coyoteCounter;
    private bool jumpRequested;
    private bool isInvulnerable;
    private Vector3 respawnPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseGravity = rb.gravityScale;
        respawnPoint = transform.position;
    }

    private void Update()
    {
        bool canControl = GameManager.Instance == null || !GameManager.Instance.IsFinished;

        moveInput = canControl ? Input.GetAxisRaw("Horizontal") : 0f;

        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;

        if (canControl && Input.GetButtonDown("Jump") && coyoteCounter > 0f)
        {
            jumpRequested = true;
        }

        // Salto variable: si suelta la tecla, corta el ascenso
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        if (moveInput != 0f && spriteRenderer != null)
        {
            spriteRenderer.flipX = moveInput < 0f;
        }
    }

    private void FixedUpdate()
    {
        // Aceleración progresiva hacia la velocidad objetivo
        float targetSpeed = moveInput * moveSpeed;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        if (jumpRequested)
        {
            jumpRequested = false;
            coyoteCounter = 0f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            AudioManager.Play(jumpClip);
        }

        // Gravedad más fuerte al caer = salto más "firme"
        rb.gravityScale = rb.linearVelocity.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
    }

    public void SetCheckpoint(Vector3 position)
    {
        respawnPoint = position;
    }

    public void TakeHit()
    {
        if (isInvulnerable) return;
        if (GameManager.Instance == null || GameManager.Instance.IsFinished) return;

        AudioManager.Play(hurtClip);
        GameManager.Instance.LoseLife();

        if (!GameManager.Instance.IsFinished)
        {
            rb.linearVelocity = Vector2.zero;
            transform.position = respawnPoint;
            StartCoroutine(InvulnerabilityRoutine());
        }
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        while (elapsed < invulnerabilityTime)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        isInvulnerable = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
