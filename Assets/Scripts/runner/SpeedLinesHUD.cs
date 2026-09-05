using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lineas de velocidad radiales, estilo Sonic Frontiers: nacen cerca del centro de la
/// pantalla y huyen hacia los bordes. Todo procedural, sin sprites: cada linea es una
/// Image blanca fina que se coloca, se estira y se desvanece.
///
/// La intensidad NO se enciende y apaga con un evento: se lee cada frame de
/// BasicMovement.VelocidadActual / PlayerSpeed. Asi el efecto sigue por si solo las
/// rampas de TiempoSubida y TiempoBajada del TransitionRush, y si cambias esos valores
/// o el MultiplicadorMaximo no hay que tocar nada aqui.
///
/// Va en un Canvas propio, NO dentro de -- RHYTHM SYSTEM --: ese objeto se apaga
/// justo cuando sale la pregunta, o sea durante todo el aceleron.
/// </summary>
[DisallowMultipleComponent]
public class SpeedLinesHUD : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("De aqui sale la velocidad. Si se deja vacio se busca en la escena.")]
    public BasicMovement Movimiento;

    [Tooltip("De aqui sale el multiplicador maximo, para saber que ratio cuenta como intensidad 1. Si se deja vacio se busca en la escena.")]
    public TransitionRush Aceleron;

    [Tooltip("Donde se crean las lineas. Si se deja vacio se usa el RectTransform de este objeto.")]
    public RectTransform Contenedor;

    [Header("Cantidad")]
    [Tooltip("Lineas por segundo cuando la intensidad es maxima.")]
    public float LineasPorSegundo = 55f;

    [Tooltip("Tope de lineas vivas a la vez.")]
    public int MaxLineas = 60;

    [Header("Forma")]
    [Tooltip("Grosor de cada linea en pixeles de UI (minimo, maximo).")]
    public Vector2 Grosor = new Vector2(2f, 6f);

    [Tooltip("Largo inicial de cada linea en pixeles de UI (minimo, maximo).")]
    public Vector2 Largo = new Vector2(70f, 190f);

    [Tooltip("Cuanto se alarga la linea segun se aleja del centro. 1 = no se alarga.")]
    public float EstiramientoAlAlejarse = 2.2f;

    [Header("Movimiento")]
    [Tooltip("Radio desde el centro donde nacen las lineas (minimo, maximo). Subelo para dejar mas despejado el centro.")]
    public Vector2 RadioDeNacimiento = new Vector2(160f, 380f);

    [Tooltip("Velocidad de alejamiento en pixeles por segundo (minimo, maximo).")]
    public Vector2 VelocidadLinea = new Vector2(1100f, 2000f);

    [Tooltip("Margen extra mas alla de la esquina antes de reciclar la linea.")]
    public float MargenFuera = 250f;

    [Header("Color")]
    public Color ColorLinea = Color.white;

    [Tooltip("Opacidad maxima de una linea cuando la intensidad es 1.")]
    [Range(0f, 1f)] public float OpacidadMaxima = 0.85f;

    [Header("Respuesta a la velocidad")]
    [Tooltip("Por debajo de esta intensidad (0-1) no sale ninguna linea. Evita que un cambio minimo de velocidad las dispare.")]
    [Range(0f, 1f)] public float UmbralMinimo = 0.05f;

    [Tooltip("Suaviza la subida y bajada de la intensidad.")]
    public bool SuavizarIntensidad = true;

    class Linea
    {
        public RectTransform rt;
        public Image img;
        public Vector2 dir;
        public float radio;
        public float radioInicial;
        public float velocidad;
        public float largo;
        public bool viva;
    }

    readonly List<Linea> lineas = new List<Linea>();
    float acumulado;
    float intensidad;

    void Awake()
    {
        if (Movimiento == null) Movimiento = FindFirstObjectByType<BasicMovement>();
        if (Aceleron == null) Aceleron = FindFirstObjectByType<TransitionRush>();
        if (Contenedor == null) Contenedor = GetComponent<RectTransform>();
    }

    void OnDisable()
    {
        // Al apagar el componente (por ejemplo al terminar la partida) no queremos
        // dejar lineas congeladas en pantalla.
        for (int i = 0; i < lineas.Count; i++)
            Recoger(lineas[i]);

        acumulado = 0f;
        intensidad = 0f;
    }

    void Update()
    {
        intensidad = CalcularIntensidad();

        Generar();
        Mover();
    }

    float CalcularIntensidad()
    {
        if (Movimiento == null || Movimiento.PlayerSpeed <= 0.0001f)
            return 0f;

        float maximo = Aceleron != null ? Aceleron.MultiplicadorMaximo : 2.5f;
        if (maximo <= 1.0001f) return 0f;

        // ratio va de 1 (velocidad normal) a MultiplicadorMaximo (aceleron a tope)
        float ratio = Movimiento.VelocidadActual / Movimiento.PlayerSpeed;
        float bruto = Mathf.InverseLerp(1f, maximo, ratio);

        if (bruto < UmbralMinimo) return 0f;

        return SuavizarIntensidad ? Mathf.SmoothStep(0f, 1f, bruto) : bruto;
    }

    void Generar()
    {
        if (intensidad <= 0f) return;

        acumulado += LineasPorSegundo * intensidad * Time.deltaTime;

        while (acumulado >= 1f)
        {
            acumulado -= 1f;
            Lanzar();
        }
    }

    void Lanzar()
    {
        Linea l = Libre();
        if (l == null) return;

        float angulo = Random.Range(0f, Mathf.PI * 2f);
        l.dir = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
        l.radioInicial = Random.Range(RadioDeNacimiento.x, RadioDeNacimiento.y);
        l.radio = l.radioInicial;
        l.velocidad = Random.Range(VelocidadLinea.x, VelocidadLinea.y);
        l.largo = Random.Range(Largo.x, Largo.y);

        float grosor = Random.Range(Grosor.x, Grosor.y);
        l.rt.sizeDelta = new Vector2(l.largo, grosor);
        l.rt.localRotation = Quaternion.Euler(0f, 0f, angulo * Mathf.Rad2Deg);

        l.viva = true;
        l.rt.gameObject.SetActive(true);
    }

    void Mover()
    {
        float radioMax = RadioMaximo();

        for (int i = 0; i < lineas.Count; i++)
        {
            Linea l = lineas[i];
            if (!l.viva) continue;

            // mas rapidas cuanto mayor es la intensidad, pero nunca se paran de golpe
            l.radio += l.velocidad * Mathf.Lerp(0.55f, 1f, intensidad) * Time.deltaTime;

            float recorrido = radioMax - l.radioInicial;
            float t = recorrido <= 0f ? 1f : Mathf.Clamp01((l.radio - l.radioInicial) / recorrido);

            if (t >= 1f)
            {
                Recoger(l);
                continue;
            }

            float largoActual = l.largo * Mathf.Lerp(1f, EstiramientoAlAlejarse, t);
            l.rt.sizeDelta = new Vector2(largoActual, l.rt.sizeDelta.y);
            l.rt.anchoredPosition = l.dir * (l.radio + largoActual * 0.5f);

            Color c = ColorLinea;
            c.a = OpacidadMaxima * intensidad * Desvanecido(t);
            l.img.color = c;
        }
    }

    // Entra rapido y se apaga en el ultimo tramo, para que no desaparezcan de golpe.
    static float Desvanecido(float t)
    {
        if (t < 0.12f) return t / 0.12f;
        if (t > 0.65f) return Mathf.Clamp01((1f - t) / 0.35f);
        return 1f;
    }

    float RadioMaximo()
    {
        Vector2 tam = Contenedor != null ? Contenedor.rect.size : new Vector2(1920f, 1080f);
        return tam.magnitude * 0.5f + MargenFuera;
    }

    void Recoger(Linea l)
    {
        l.viva = false;
        if (l.rt != null)
            l.rt.gameObject.SetActive(false);
    }

    Linea Libre()
    {
        for (int i = 0; i < lineas.Count; i++)
            if (!lineas[i].viva) return lineas[i];

        if (lineas.Count >= MaxLineas) return null;

        return Crear();
    }

    Linea Crear()
    {
        GameObject go = new GameObject("Linea", typeof(RectTransform));
        go.transform.SetParent(Contenedor, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = go.AddComponent<Image>();
        img.color = ColorLinea;
        img.raycastTarget = false;

        go.SetActive(false);

        Linea l = new Linea();
        l.rt = rt;
        l.img = img;
        l.viva = false;
        lineas.Add(l);
        return l;
    }
}
