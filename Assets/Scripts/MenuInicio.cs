using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MenuInicio : MonoBehaviour
{
    public const string Escena = "MenuInicio";

    [SerializeField] Texture2D cielo;
    [SerializeField] Texture2D selvaLejana;
    [SerializeField] Texture2D selvaCercana;

    GUIStyle titulo;
    GUIStyle boton;

    void OnGUI()
    {
        var pantalla = new Rect(0, 0, Screen.width, Screen.height);
        GUI.DrawTexture(pantalla, cielo, ScaleMode.ScaleAndCrop);
        GUI.DrawTexture(pantalla, selvaLejana, ScaleMode.ScaleAndCrop);
        GUI.DrawTexture(pantalla, selvaCercana, ScaleMode.ScaleAndCrop);

        titulo ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        boton ??= new GUIStyle(GUI.skin.button) { fontSize = 20 };

        float escala = Mathf.Max(0.1f, Mathf.Min(Screen.width / 480f, Screen.height / 360f));
        Matrix4x4 matriz = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(
            new Vector3(Screen.width / 2f, Screen.height / 2f, 0), Quaternion.identity,
            new Vector3(escala, escala, 1));
        var color = GUI.color;
        GUI.color = new Color(0.035f, 0.09f, 0.07f, 0.94f);
        GUI.DrawTexture(new Rect(-145, -115, 290, 230), Texture2D.whiteTexture);
        GUI.color = color;

        GUI.Label(new Rect(-125, -92, 250, 56), "SELVA", titulo);
        bool jugar = GUI.Button(new Rect(-106, -18, 212, 44), "Jugar", boton);
        bool salir = GUI.Button(new Rect(-106, 38, 212, 44), "Salir", boton);
        GUI.matrix = matriz;

        if (jugar)
        {
            if (ProgresoJuego.Instancia != null)
                ProgresoJuego.Instancia.CargarPartida();
            else
                SceneManager.LoadScene("SampleScene");
        }

        if (salir)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
