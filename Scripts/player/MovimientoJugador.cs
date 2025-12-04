using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Atributos))]
public class MovimientoJugador : MonoBehaviour
{
    [Header("Velocidad de movimiento")]
    [SerializeField, Tooltip("Velocidad base del jugador.")]
    private float velocidadBase = 3.0f;

    [SerializeField, Tooltip("Velocidad adicional por punto de Velocidad de movimiento.")]
    private float velocidadPorPunto = 0.1f;

    private Rigidbody2D playerRb;
    private Vector2 moveInput;
    public Animator playerAnimator;
    private Vector2 idleDir;

    private Atributos atributos;

    [Header("Debug - Valores en tiempo real")]
    [SerializeField, Tooltip("Debug: última Velocidad de movimiento leída de Atributos.")]
    private int debugVelocidadMovimientoLeida;

    [SerializeField, Tooltip("Debug: velocidad efectiva calculada.")]
    private float debugVelocidadEfectiva;

    void Awake()
    {
        if (!TryGetComponent(out atributos))
            atributos = GetComponentInParent<Atributos>();
    }

    void OnEnable()
    {
        // Suscribirse al evento de cambio de velocidad de movimiento
        if (atributos != null)
        {
            atributos.OnVelocidadMovimientoCambiada += ActualizarVelocidadMovimiento;
        }
    }

    void OnDisable()
    {
        // Desuscribirse para evitar memory leaks
        if (atributos != null)
        {
            atributos.OnVelocidadMovimientoCambiada -= ActualizarVelocidadMovimiento;
        }
    }

    void Start()
    {
        playerRb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        idleDir = Vector2.down;

        if (atributos == null)
            atributos = GetComponentInParent<Atributos>();

        ActualizarDebugInfo();
    }

    private void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("MoveBlendX", moveX);
            playerAnimator.SetFloat("MoveBlendY", moveY);
        }

        Vector2 targetDir = moveInput != Vector2.zero ? moveInput : idleDir;
        idleDir = Vector2.Lerp(idleDir, targetDir, 0.2f);

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("IdleX", idleDir.x);
            playerAnimator.SetFloat("IdleY", idleDir.y);
        }

        if (playerRb != null)
        {
            float effectiveSpeed = GetVelocidadEfectiva();
            debugVelocidadEfectiva = effectiveSpeed;
            playerRb.MovePosition(playerRb.position + moveInput * effectiveSpeed * Time.fixedDeltaTime);
        }
    }

    private float GetVelocidadEfectiva()
    {
        int velocidadMovimiento = (atributos != null) ? atributos.VelocidadMovimientoActual : 0;
        debugVelocidadMovimientoLeida = velocidadMovimiento;
        return velocidadBase + velocidadMovimiento * velocidadPorPunto;
    }

    private void ActualizarDebugInfo()
    {
        debugVelocidadEfectiva = GetVelocidadEfectiva();
    }

    // Callback para evento de atributos
    private void ActualizarVelocidadMovimiento(int nuevaVelocidad)
    {
        Debug.Log($"[MovimientoJugador] Velocidad de movimiento actualizada a: {nuevaVelocidad}");
        ActualizarDebugInfo();
    }

    private void OnValidate()
    {
        velocidadBase = Mathf.Max(0.1f, velocidadBase);
        velocidadPorPunto = Mathf.Max(0f, velocidadPorPunto);

        if (!Application.isPlaying)
        {
            if (atributos == null)
            {
                TryGetComponent(out atributos);
                if (atributos == null) atributos = GetComponentInParent<Atributos>();
            }

            int vel = (atributos != null) ? atributos.VelocidadMovimientoActual : 0;
            debugVelocidadMovimientoLeida = vel;
            debugVelocidadEfectiva = velocidadBase + vel * velocidadPorPunto;
        }
    }
}