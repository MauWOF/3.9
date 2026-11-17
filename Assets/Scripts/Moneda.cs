using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Moneda : MonoBehaviour
{
    [SerializeField] string identificador;

    void Update()
    {
        var progreso = ProgresoJuego.Instancia;
        if (progreso != null && progreso.TieneMoneda(identificador))
            gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<MovimientoJohn>() == null || ProgresoJuego.Instancia == null)
            return;

        ProgresoJuego.Instancia.Recoger(identificador);
        gameObject.SetActive(false);
    }
}
