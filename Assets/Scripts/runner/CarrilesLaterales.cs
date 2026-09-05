using UnityEngine;

/// <summary>
/// [ANADIDO: carriles laterales]
///
/// Unico sitio que decide si los carriles izquierdo y derecho estan disponibles.
/// En un pasillo Narrow no hay sitio a los lados, asi que mientras el terreno sea
/// Narrow pasan tres cosas, y las tres salen de aqui:
///
///   1. Se apagan los botones A y D del HUD (SetActive false).
///   2. Las pulsaciones de A y D no aciertan notas   -> lo consulta ColliderNoteScript.
///      Tampoco lanzan gesto de animacion            -> lo consulta AnimacionesDeNotas.
///   3. El chart no llega a lanzar notas Left ni Right -> lo consulta NoteSpawner.
///
/// En cuanto el terreno vuelve a ser Wide se reactiva TODO: botones, input y notas.
/// La reactivacion no depende de nada externo, se recalcula cada frame comparando con
/// el tipo actual, asi que no hay forma de quedarse apagado por un aviso perdido.
///
/// DE DONDE SALE EL TIPO. Se lee SegmentGenerator.TipoActual, que es el tipo que se
/// esta GENERANDO por delante, no el suelo que el jugador pisa ahora mismo. Aqui eso
/// es correcto, y no por casualidad: TipoActual cambia en el instante en que el jugador
/// responde la pregunta, o sea durante el puente, y los botones cuelgan de
/// -- RHYTHM SYSTEM --, que esta apagado durante todo el puente. Cuando el HUD vuelve a
/// verse, el jugador ya ha entrado en el segmento de transicion hacia el terreno nuevo.
/// Si algun dia el HUD pasara a verse durante el puente, habria que cambiar esto por una
/// deteccion del segmento realmente pisado.
///
/// Los demas scripts preguntan por los metodos estaticos. Si este componente no esta en
/// la escena, los dos devuelven true y el juego se comporta exactamente como antes de
/// anadirlo.
/// </summary>
[DisallowMultipleComponent]
public class CarrilesLaterales : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("De donde se lee el tipo de terreno. Si se deja vacio se busca en este mismo GameObject.")]
    public SegmentGenerator Generador;

    [Tooltip("Boton A del HUD (Button_L). Cuelga de Player/-- RHYTHM SYSTEM --/Canvas/")]
    public GameObject BotonIzquierda;

    [Tooltip("Boton D del HUD (Button_R). Cuelga de Player/-- RHYTHM SYSTEM --/Canvas/")]
    public GameObject BotonDerecha;

    [Header("Ajustes")]
    [Tooltip("Deja rastro en consola cada vez que los laterales se apagan o se encienden. Util para comprobar el momento; desmarcalo cuando este ajustado.")]
    public bool LogAlCambiar = true;

    // La instancia viva. NO se usa ASingleton a proposito: esos son DontDestroyOnLoad y
    // no limpian Instance en OnDestroy, y este componente tiene que morir con la escena.
    static CarrilesLaterales instancia;

    bool activos = true;
    bool inicializado;

    /// <summary>
    /// True si los carriles laterales cuentan ahora mismo. Sin componente en la escena
    /// devuelve true, para que quitarlo deje el juego como estaba.
    /// </summary>
    public static bool LateralesActivos
    {
        get { return instancia == null || instancia.activos; }
    }

    /// <summary>
    /// True si el carril indicado se puede usar ahora. Los nombres son los del chart:
    /// "Left", "Middle" y "Right". El del medio siempre vale.
    /// </summary>
    public static bool Permite(string carril)
    {
        if (LateralesActivos) return true;

        return carril != "Left" && carril != "Right";
    }

    void Awake()
    {
        if (Generador == null)
            Generador = GetComponent<SegmentGenerator>();
    }

    void OnEnable()
    {
        instancia = this;
        inicializado = false;   // fuerza a aplicar el estado en el primer Refrescar
        Refrescar();
    }

    void OnDisable()
    {
        if (instancia == this)
            instancia = null;

        // Al quitarnos de en medio dejamos el HUD como lo encontramos. Si no, los
        // botones se quedarian apagados para siempre mientras el input ya volveria a
        // funcionar, que es la peor combinacion posible.
        Aplicar(true);
    }

    void Update()
    {
        Refrescar();
    }

    void Refrescar()
    {
        if (Generador == null)
        {
            Debug.LogError("CarrilesLaterales: no hay SegmentGenerator asignado, los laterales se quedan siempre activos.", this);
            enabled = false;
            return;
        }

        // Narrow -> sin lados. Cualquier otro tipo -> con lados.
        bool deberian = Generador.TipoActual != TipoSegmento.Narrow;

        if (inicializado && deberian == activos)
            return;

        inicializado = true;
        activos = deberian;
        Aplicar(activos);

        if (LogAlCambiar)
            Debug.Log("CarrilesLaterales: terreno " + Generador.TipoActual
                + " -> carriles laterales " + (activos ? "ACTIVOS" : "apagados"), this);
    }

    void Aplicar(bool encendidos)
    {
        if (BotonIzquierda != null && BotonIzquierda.activeSelf != encendidos)
            BotonIzquierda.SetActive(encendidos);

        if (BotonDerecha != null && BotonDerecha.activeSelf != encendidos)
            BotonDerecha.SetActive(encendidos);
    }
}
