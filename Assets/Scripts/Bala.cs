using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class Bala : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float velocidad = 11f;
    [SerializeField, Min(0.1f)] float tiempoDeVida = 2f;
    [SerializeField] LayerMask capasImpacto = 1 << 8;

    void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = (Vector2)transform.right * velocidad;
        Destroy(gameObject, tiempoDeVida);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if ((capasImpacto.value & (1 << otro.gameObject.layer)) != 0)
            Destroy(gameObject);
    }
}
