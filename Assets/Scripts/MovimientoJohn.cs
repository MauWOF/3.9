using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D), typeof(Animator))]
public sealed class MovimientoJohn : MonoBehaviour
{
    [SerializeField, Min(0)] float velocidad = 3.5f;
    [SerializeField, Min(0)] float velocidadSalto = 6.2f;
    [SerializeField] LayerMask capasSuelo;
    [SerializeField] SpriteRenderer dibujo;
    [SerializeField] Transform puntoDisparo;
    [SerializeField] Bala prefabBala;
    [SerializeField, Min(0.01f)] float intervaloDisparo = 0.15f;

    static readonly int Corriendo = Animator.StringToHash("Corriendo");
    static readonly int EnSuelo = Animator.StringToHash("EnSuelo");
    static readonly int Disparar = Animator.StringToHash("Disparar");
    readonly ContactPoint2D[] contactos = new ContactPoint2D[8];
    Rigidbody2D cuerpo;
    Animator animador;
    ContactFilter2D filtroSuelo;
    float horizontal;
    bool saltoSolicitado;
    float proximoDisparo;

    void Awake()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        animador = GetComponent<Animator>();
        filtroSuelo.SetLayerMask(capasSuelo);
        filtroSuelo.useTriggers = false;
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null)
        {
            horizontal = 0f;
            return;
        }

        bool izquierda = teclado.aKey.isPressed || teclado.leftArrowKey.isPressed;
        bool derecha = teclado.dKey.isPressed || teclado.rightArrowKey.isPressed;
        horizontal = (derecha ? 1f : 0f) - (izquierda ? 1f : 0f);
        saltoSolicitado |= teclado.wKey.wasPressedThisFrame;

        if (horizontal != 0f)
        {
            dibujo.flipX = horizontal < 0f;
            Vector3 posicion = puntoDisparo.localPosition;
            posicion.x = Mathf.Abs(posicion.x) * (dibujo.flipX ? -1f : 1f);
            puntoDisparo.localPosition = posicion;
        }

        if (teclado.spaceKey.wasPressedThisFrame && Time.time >= proximoDisparo)
        {
            Quaternion rotacion = dibujo.flipX ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
            Instantiate(prefabBala, puntoDisparo.position, rotacion);
            animador.SetTrigger(Disparar);
            proximoDisparo = Time.time + intervaloDisparo;
        }
    }

    void FixedUpdate()
    {
        bool enSuelo = false;
        int cantidad = cuerpo.GetContacts(filtroSuelo, contactos);
        for (int i = 0; i < cantidad; i++)
            enSuelo |= contactos[i].normal.y > 0.6f;

        float vertical = cuerpo.linearVelocity.y;
        if (saltoSolicitado && enSuelo)
        {
            vertical = velocidadSalto;
            enSuelo = false;
        }

        saltoSolicitado = false;
        cuerpo.linearVelocity = new Vector2(horizontal * velocidad, vertical);
        animador.SetBool(Corriendo, horizontal != 0f);
        animador.SetBool(EnSuelo, enSuelo);
    }
}
