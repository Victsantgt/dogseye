using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [ANADIDO: adornos del menu] Hace que una lagrima resbale y se apague, en bucle, para
/// que se lea como llanto y no como una gota pegada a la cara.
///
/// Trabaja en coordenadas locales del propio RectTransform, asi que da igual que el
/// padre (el sprite triste) este girado: la gota cae siguiendo la inclinacion de la
/// cara, que es justo lo que queremos.
///
/// La posicion de partida se lee en Awake, o sea la primera vez que el objeto se
/// enciende. Como el adorno vive apagado hasta que el raton entra en el boton, lo que
/// se guarda es lo que haya puesto el artista en el editor.
///
/// Usa deltaTime sin escalar para que siga goteando aunque alguien pare el tiempo.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class GoteoDeLagrima : MonoBehaviour
{
    [Tooltip("Cuantos pixeles baja la gota antes de desaparecer.")]
    public float Recorrido = 26f;

    [Tooltip("Lo que tarda una gota en recorrer todo el camino.")]
    public float Duracion = 1.1f;

    [Tooltip("Pausa entre una gota y la siguiente, con la gota ya invisible.")]
    public float EsperaEntreGotas = 0.3f;

    [Tooltip("Desvanece la gota en el ultimo tramo de la caida. Sin esto pega un salto feo al reaparecer arriba.")]
    public bool Desvanecer = true;

    [Tooltip("A partir de que parte del recorrido empieza a desvanecerse (0 = desde el principio, 1 = nunca).")]
    [Range(0f, 1f)]
    public float DondeEmpiezaAApagarse = 0.55f;

    RectTransform caja;
    Graphic grafico;
    Vector2 sitioDeSalida;
    float alfaOriginal = 1f;
    float reloj;

    void Awake()
    {
        caja = GetComponent<RectTransform>();
        grafico = GetComponent<Graphic>();
        sitioDeSalida = caja.anchoredPosition;
        if (grafico != null) alfaOriginal = grafico.color.a;
    }

    void OnEnable()
    {
        reloj = 0f;
        Colocar(0f);
    }

    void OnDisable()
    {
        // Se deja como estaba para que el editor no acabe guardando la gota a mitad de caida.
        Colocar(0f);
    }

    void Update()
    {
        float ciclo = Duracion + Mathf.Max(0f, EsperaEntreGotas);
        if (ciclo <= 0f) return;

        reloj += Time.unscaledDeltaTime;
        float dentroDelCiclo = reloj % ciclo;
        Colocar(Duracion <= 0f ? 1f : Mathf.Clamp01(dentroDelCiclo / Duracion));
    }

    void Colocar(float avance)
    {
        if (caja == null) return;

        // Cuadratico: la gota arranca despacio, como cuando se descuelga, y luego cae.
        caja.anchoredPosition = sitioDeSalida + Vector2.down * (Recorrido * avance * avance);

        if (Desvanecer && grafico != null)
        {
            float apagado = Mathf.InverseLerp(DondeEmpiezaAApagarse, 1f, avance);
            Color color = grafico.color;
            color.a = alfaOriginal * (1f - apagado);
            grafico.color = color;
        }
    }
}
