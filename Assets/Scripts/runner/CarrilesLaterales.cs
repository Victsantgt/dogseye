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

    // [ANADIDO: sin laterales en la primera seccion] De donde se saca en que seccion
    // vamos. Se reutiliza el contador del GameEndManager, que es el mismo que usa
    // MusicaPorSeccion para elegir tema: cero decisiones = seccion 1.
    [Tooltip("De donde se lee cuantas decisiones se han tomado. Si se deja vacio se busca en este mismo GameObject.")]
    public GameEndManager Final;

    [Header("Primera seccion")]
    [Tooltip("En la seccion 1, antes de la primera decision, los laterales se apagan aunque el pasillo sea Wide. El jugador empieza solo con la tecla del medio y los lados se desbloquean al llegar a la seccion 2.")]
    public bool SinLateralesEnLaPrimeraSeccion = true;

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

        // [ANADIDO: sin laterales en la primera seccion]
        if (Final == null)
            Final = GetComponent<GameEndManager>();
    }

    /// <summary>
    /// [ANADIDO: sin laterales en la primera seccion]
    /// True mientras no se haya contestado ninguna pregunta, o sea durante la seccion 1.
    /// Sin GameEndManager asignado se da por hecho que no, para no apagar el HUD entero
    /// por una referencia que falte.
    /// </summary>
    bool EsLaPrimeraSeccion
    {
        get { return Final != null && Final.DecisionesTomadas == 0; }
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

        // [ANADIDO: sin laterales en la primera seccion] La seccion 1 va siempre sin
        // lados, sea Wide o no: se arranca solo con la tecla del medio y los laterales
        // se desbloquean al entrar en la seccion 2. El motivo se guarda aparte para
        // poder decirlo en el log, que si no parece que el Wide no se este respetando.
        bool porSerLaPrimera = SinLateralesEnLaPrimeraSeccion && EsLaPrimeraSeccion;
        if (porSerLaPrimera)
            deberian = false;

        if (inicializado && deberian == activos)
            return;

        inicializado = true;
        activos = deberian;
        Aplicar(activos);

        if (LogAlCambiar)
            Debug.Log("CarrilesLaterales: terreno " + Generador.TipoActual
                + (porSerLaPrimera ? " pero seccion 1" : "")
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
