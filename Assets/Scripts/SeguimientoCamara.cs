using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class SeguimientoCamara : MonoBehaviour
{
    [SerializeField] Transform objetivo;
    [SerializeField, Min(0.01f)] float tiempoSuavizado = 0.18f;
    [SerializeField] float limiteIzquierdo = -18f;
    [SerializeField] float limiteDerecho = 18f;

    Camera camara;
    float velocidadHorizontal;

    void Awake()
    {
        camara = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (objetivo == null)
            return;

        float mitadVisible = camara.orthographicSize * camara.aspect;
        float centro = (limiteIzquierdo + limiteDerecho) * 0.5f;
        float recorrido = Mathf.Max(0f, (limiteDerecho - limiteIzquierdo) * 0.5f - mitadVisible);
        float minimo = centro - recorrido;
        float maximo = centro + recorrido;
        float destino = Mathf.Clamp(objetivo.position.x, minimo, maximo);

        Vector3 posicion = transform.position;
        posicion.x = Mathf.Clamp(
            Mathf.SmoothDamp(posicion.x, destino, ref velocidadHorizontal, tiempoSuavizado),
            minimo,
            maximo);
        transform.position = posicion;
    }
}
