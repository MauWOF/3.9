using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class MetaNivel : MonoBehaviour
{
    [SerializeField] int nivel;
    [SerializeField] string[] monedasDelNivel;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponent<MovimientoJohn>() != null && ProgresoJuego.Instancia != null)
            ProgresoJuego.Instancia.LlegarAMeta(nivel, monedasDelNivel);
    }
}
