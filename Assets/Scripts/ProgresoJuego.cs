using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
#if FIREBASE_PROGRESS
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
#endif

public sealed class ProgresoJuego : MonoBehaviour
{
    const string Clave = "Selva.Progreso";
    static ProgresoJuego instancia;

    [Serializable]
    sealed class Partida
    {
        public List<string> monedas = new List<string>();
        public int nivel = 1;
        public bool nivelDosCompletado;
        public long generacion;
        public string reinicioPendiente;
    }

    Partida partida;
    HashSet<string> recogidas;
    string mensaje;
#if FIREBASE_PROGRESS
    DocumentReference documento;
    bool sincronizando;
    bool pendiente = true;
    float siguienteIntento;
    string estado = "Conectando con Firebase...";
#else
    string estado = "Guardado local";
#endif

    public static ProgresoJuego Instancia => instancia;

    void Awake()
    {
        if (instancia != null)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        partida = JsonUtility.FromJson<Partida>(PlayerPrefs.GetString(Clave, "{}")) ?? new Partida();
        partida.nivel = Mathf.Clamp(partida.nivel, 1, 2);
        recogidas = new HashSet<string>(partida.monedas ?? new List<string>());
    }

    void Start()
    {
        if (instancia == this)
            RestaurarNivel();
    }

    public bool TieneMoneda(string id) => recogidas.Contains(id);

    public void Recoger(string id)
    {
        if (recogidas.Add(id))
            Guardar();
    }

    public void LlegarAMeta(int nivel, string[] monedasDelNivel)
    {
        foreach (string id in monedasDelNivel)
        {
            if (!recogidas.Contains(id))
            {
                mensaje = "Recoge las monedas de este nivel para abrir la salida.";
                return;
            }
        }

        if (nivel == 1)
        {
            partida.nivel = 2;
            Guardar();
            mensaje = null;
            SceneManager.LoadScene("Nivel2");
            return;
        }

        partida.nivel = 2;
        partida.nivelDosCompletado = true;
        Guardar();
        mensaje = "¡Completaste los dos niveles!";
    }

    public void CargarPartida()
    {
        mensaje = null;
        SceneManager.LoadScene(partida.nivel == 2 ? "Nivel2" : "SampleScene");
    }

    void RestaurarNivel()
    {
        if (partida.nivel == 2 && SceneManager.GetActiveScene().name == "SampleScene")
        {
            mensaje = null;
            SceneManager.LoadScene("Nivel2");
        }
    }

    void Guardar()
    {
        GuardarLocal();
#if FIREBASE_PROGRESS
        pendiente = true;
        estado = "Sincronización pendiente";
#endif
    }

    void GuardarLocal()
    {
        partida.monedas = new List<string>(recogidas);
        partida.monedas.Sort(StringComparer.Ordinal);
        PlayerPrefs.SetString(Clave, JsonUtility.ToJson(partida));
        PlayerPrefs.Save();
    }

    void NuevaPartida()
    {
        partida = new Partida
        {
            generacion = partida.generacion,
            reinicioPendiente = Guid.NewGuid().ToString("N")
        };
        recogidas.Clear();
        mensaje = null;
        Guardar();
        SceneManager.LoadScene("SampleScene");
    }

    void OnGUI()
    {
        string escena = SceneManager.GetActiveScene().name;
        if (escena == MenuInicio.Escena)
            return;

        int nivelActual = escena == "Nivel2" ? 2 : 1;
        string texto = mensaje;
        if (nivelActual == 2 && partida.nivelDosCompletado)
            texto = "¡Completaste los dos niveles!";

        GUILayout.BeginArea(new Rect(12, 12, 330, 145), GUI.skin.box);
        GUILayout.Label($"Nivel {nivelActual}/2   |   Monedas: {recogidas.Count}");
        GUILayout.Label(estado);
        GUILayout.Label(texto ?? "Recoge las monedas y llega a la bandera.");
        var teclado = Keyboard.current;
        bool mayus = teclado != null && (teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed);
        bool habilitado = GUI.enabled;
        GUI.enabled = habilitado && mayus;
        bool reiniciar = GUILayout.Button("Nueva partida (Mayús + clic)");
        GUI.enabled = habilitado;
        bool volverAlMenu = GUILayout.Button("Volver al menú");
        GUILayout.EndArea();
        if (reiniciar)
            NuevaPartida();
        else if (volverAlMenu)
            SceneManager.LoadScene(MenuInicio.Escena);
    }

#if FIREBASE_PROGRESS
    void Update()
    {
        if (pendiente && !sincronizando && Time.unscaledTime >= siguienteIntento)
            Sincronizar();
    }

    async void Sincronizar()
    {
        sincronizando = true;
        pendiente = false;
        estado = "Sincronizando...";
        string[] monedas = new List<string>(recogidas).ToArray();
        int nivel = partida.nivel;
        bool nivelDosCompletado = partida.nivelDosCompletado;
        long generacion = partida.generacion;
        string reinicio = partida.reinicioPendiente;

        try
        {
            if (documento == null)
            {
                var dependencias = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencias != DependencyStatus.Available)
                    throw new InvalidOperationException(dependencias.ToString());

                var auth = FirebaseAuth.DefaultInstance;
                var usuario = auth.CurrentUser ?? (await auth.SignInAnonymouslyAsync()).User;
                documento = FirebaseFirestore.DefaultInstance.Collection("progreso").Document(usuario.UserId);
            }

            var resultado = await FirebaseFirestore.DefaultInstance.RunTransactionAsync(async transaccion =>
            {
                var anterior = await transaccion.GetSnapshotAsync(documento);
                long generacionRemota = 0;
                string ultimoReinicio = "";
                if (anterior.Exists)
                {
                    anterior.TryGetValue("generacion", out generacionRemota);
                    anterior.TryGetValue("ultimoReinicio", out ultimoReinicio);
                }

                bool hayReinicio = !string.IsNullOrEmpty(reinicio);
                bool reinicioNuevo = hayReinicio && reinicio != ultimoReinicio;
                bool conservarLocal = hayReinicio || generacion >= generacionRemota;
                var total = new HashSet<string>();
                int nivelFusionado = 1;
                bool terminado = false;

                if (conservarLocal)
                {
                    total.UnionWith(monedas);
                    nivelFusionado = nivel;
                    terminado = nivelDosCompletado;
                }

                if (anterior.Exists && !reinicioNuevo)
                {
                    if (anterior.TryGetValue("monedas", out List<string> guardadas))
                        total.UnionWith(guardadas);
                    if (anterior.TryGetValue("nivel", out int nivelGuardado))
                        nivelFusionado = Math.Max(nivelFusionado, Math.Min(2, nivelGuardado));
                    terminado |= anterior.TryGetValue("nivelDosCompletado", out bool completado) && completado;
                }

                var datos = new Partida
                {
                    monedas = new List<string>(total),
                    nivel = nivelFusionado,
                    nivelDosCompletado = terminado,
                    generacion = reinicioNuevo ? Math.Max(generacion, generacionRemota) + 1 : Math.Max(generacion, generacionRemota)
                };
                transaccion.Set(documento, new Dictionary<string, object>
                {
                    { "monedas", datos.monedas },
                    { "totalMonedas", datos.monedas.Count },
                    { "nivel", datos.nivel },
                    { "nivelDosCompletado", datos.nivelDosCompletado },
                    { "generacion", datos.generacion },
                    { "ultimoReinicio", reinicioNuevo ? reinicio : ultimoReinicio ?? "" }
                }, SetOptions.MergeAll);
                return datos;
            });

            if (this == null)
                return;
            if (partida.reinicioPendiente != reinicio)
                return;

            bool reinicioRemoto = string.IsNullOrEmpty(reinicio) && resultado.generacion > partida.generacion;
            if (reinicioRemoto)
            {
                recogidas.Clear();
                partida.nivel = 1;
                partida.nivelDosCompletado = false;
            }

            partida.generacion = resultado.generacion;
            partida.reinicioPendiente = null;
            recogidas.UnionWith(resultado.monedas);
            partida.nivel = Math.Max(partida.nivel, resultado.nivel);
            partida.nivelDosCompletado |= resultado.nivelDosCompletado;
            GuardarLocal();
            estado = pendiente ? "Sincronización pendiente" : "Firestore sincronizado";
            if (SceneManager.GetActiveScene().name == MenuInicio.Escena)
                return;
            if (reinicioRemoto)
                SceneManager.LoadScene(partida.nivel == 2 ? "Nivel2" : "SampleScene");
            else
                RestaurarNivel();
        }
        catch (Exception error)
        {
            if (this == null)
                return;
            Debug.LogWarning($"Firestore: {error.Message}");
            estado = "Guardado local | Reintentando sincronizar";
            pendiente = true;
            siguienteIntento = Time.unscaledTime + 5f;
        }
        finally
        {
            sincronizando = false;
        }
    }
#endif
}
