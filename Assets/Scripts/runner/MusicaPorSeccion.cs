using System.Collections.Generic;
using Patterns.Singleton;
using UnityEngine;

/// <summary>
/// [ANADIDO: musica por seccion]
///
/// Decide que tema suena y que chart de notas se usa en cada tramo de la partida.
/// Va en LevelController.
///
/// La musica y el chart van SIEMPRE emparejados, en la misma ficha del Inspector: un
/// chart solo tiene sentido con la cancion para la que se escribio, asi que se eligen
/// juntos y no se pueden desparejar por descuido.
///
/// COMO ENCAJA CON LO QUE YA HABIA. No hace falta tocar ChartManager: el cambio de pista
/// y el reinicio del chart ya existian y ya estaban en el sitio correcto.
///
///   ChartManager.NextSection(filename) -> MusicManager.ReturnToDefault() -> SwapTrack(defaultClip)
///
/// Eso se dispara justo cuando vuelven las notas (RhythmResumeTrigger, o el temporizador
/// del puente sin transicion). Lo unico que hace este componente es adelantarse y dejar
/// preparados los dos valores que esa llamada va a leer: el defaultClip del MusicManager
/// y el campo filename del ChartManager. Ventaja: musica y notas arrancan en el MISMO
/// instante, porque los reinicia la misma llamada.
///
/// EL RECORRIDO COMPLETO:
///
///   arranque      -> section1 + chart1.json, suena en track01
///   pregunta 1    -> SwapTrack(intermision)  -> intermision pasa a track02
///   decision 1    -> se preparan section2(Malo) y chart2(Malo).json
///   vuelven notas -> ReturnToDefault()       -> la seccion 2 vuelve a track01, con su chart
///   pregunta 2    -> intermision a track02   ... y asi
///   decision 3    -> la partida termina, fundido a blanco y FadeOut de la musica
///
/// SwapTrack alterna solo entre las dos AudioSource, asi que la intermision cae siempre
/// en una y las secciones en la otra, sin tener que elegirlas a mano.
///
/// OJO CON EL SINGLETON. MusicManager es un ASingleton con DontDestroyOnLoad, o sea que
/// SOBREVIVE a la recarga de escena, y changeDefault() le cambia un campo. Sin
/// devolverlo a su sitio, al pulsar R para reiniciar la partida nueva arrancaria con el
/// tema de la seccion 3. Por eso se reinicia al empezar. Ver el CLAUDE.md.
/// </summary>
[DisallowMultipleComponent]
public class MusicaPorSeccion : MonoBehaviour
{
    /// <summary>Una cancion con el chart de notas que le corresponde.</summary>
    [System.Serializable]
    public class Tema
    {
        [Tooltip("Cancion que suena en este tramo.")]
        public AudioClip Musica;

        [Tooltip("Chart de notas escrito para esa cancion. Nombre del json dentro de StreamingAssets, CON extension. Ejemplo: chart2Malo.json")]
        public string Chart = "";
    }

    /// <summary>Los dos temas de una seccion, segun lo que se haya elegido antes.</summary>
    [System.Serializable]
    public class TemasDeSeccion
    {
        [Tooltip("Solo informativo, para leerlo en el Inspector.")]
        public string Nombre = "";

        [Tooltip("Suena si la decision anterior fue Opcion.Derecha, que es la que el GameEndManager cuenta como buena.")]
        public Tema Derecha = new Tema();

        [Tooltip("Suena si la decision anterior fue Opcion.Izquierda, la que cuenta como mala.")]
        public Tema Izquierda = new Tema();
    }

    [Header("Temas")]
    [Tooltip("Con lo que arranca la partida, antes de ninguna decision.")]
    public Tema Seccion1 = new Tema();

    [Tooltip("Tema del puente: suena desde que sale la pregunta hasta que vuelven las notas. No lleva chart porque durante el puente no hay notas.")]
    public AudioClip Intermision;

    [Tooltip("Una entrada por seccion, EN ORDEN. La primera es la seccion 2 (la que sigue a la primera decision), la segunda es la seccion 3, etc.")]
    public List<TemasDeSeccion> SeccionesSiguientes = new List<TemasDeSeccion>();

    [Header("Modo de pruebas")]
    // TEMPORAL: los temas de verdad duran 30-40 s cada uno y la intermision casi un
    // minuto, asi que probar una partida entera son varios minutos de espera. Con esto
    // se juega con los clips cortos de Assets/Audio/Testing y sin intermision.
    [Tooltip("TEMPORAL: usa los temas cortos de prueba en vez de los de arriba. Acuerdate de desmarcarlo antes de subir una build.")]
    public bool ModoDePruebas = false;

    [Tooltip("Sustituye a Seccion1 en modo pruebas. Si se deja vacio no se toca nada y suena lo que hubiera puesto.")]
    public Tema PruebasSeccion1 = new Tema();

    [Tooltip("Un tema por seccion, en el mismo orden que SeccionesSiguientes. No distinguen buena de mala: en pruebas da igual lo que se elija.")]
    public List<Tema> PruebasSecciones = new List<Tema>();

    [Tooltip("En modo pruebas se salta la intermision, que es el clip mas largo de todos. Desmarcalo si lo que quieres probar es precisamente la intermision.")]
    public bool SaltarIntermisionEnPruebas = true;

    [Header("Referencias")]
    [Tooltip("De donde se detecta que ha salido una pregunta. Si se deja vacio se busca en este mismo GameObject.")]
    public DecisionManager Decisiones;

    [Tooltip("De donde se leen las decisiones tomadas y el final. Si se deja vacio se busca en este mismo GameObject.")]
    public GameEndManager Final;

    [Tooltip("El ChartManager del sistema de notas. Es a quien se le deja preparado el filename. Si se deja vacio se busca en la escena.")]
    public ChartManager Chart;

    [Header("Ajustes")]
    [Tooltip("Al terminar la partida, funde la musica a la vez que el fundido a blanco. Es lo mismo que ya hace PlayerDeathManager al morir.")]
    public bool FundirAlTerminar = true;

    [Tooltip("Deja rastro en consola de cada cambio de tema. Util para comprobar el orden; desmarcalo cuando este ajustado.")]
    public bool LogAlCambiar = true;

    bool preguntaAnterior;
    int decisionesAnteriores;
    bool finalAnterior;

    void Awake()
    {
        if (Decisiones == null) Decisiones = GetComponent<DecisionManager>();
        if (Final == null) Final = GetComponent<GameEndManager>();
        if (Chart == null) Chart = Object.FindFirstObjectByType<ChartManager>(FindObjectsInactive.Include);

        // El filename se deja puesto en Awake, no en Start: el ChartManager lo lee en su
        // propio Start y el orden entre Starts no esta garantizado. Los Awake si van
        // todos antes que cualquier Start, asi que aqui llegamos seguro a tiempo.
        Tema arranque = TemaDeArranque();
        if (arranque != null)
            PonerChart(arranque);
    }

    void Start()
    {
        // La musica va en Start y no en Awake porque necesita al singleton MusicManager,
        // y el orden entre Awakes tampoco esta garantizado.
        //
        // Devolver el defaultClip al tema de arranque es imprescindible: el MusicManager
        // sobrevive a la recarga de escena con el ultimo valor que le dejamos, asi que
        // sin esto una partida reiniciada empezaria por el tema de la ultima seccion.
        Tema arranque = TemaDeArranque();
        MusicManager musica = MusicManager.Instance;
        if (musica != null && arranque != null && arranque.Musica != null)
            musica.changeDefault(arranque.Musica);

        if (ModoDePruebas)
            Debug.LogWarning("MusicaPorSeccion: MODO DE PRUEBAS activo. Suenan los temas cortos"
                + (SaltarIntermisionEnPruebas ? " y se salta la intermision." : ".")
                + " Desmarcalo para la musica de verdad.", this);

        if (Final != null)
        {
            decisionesAnteriores = Final.DecisionesTomadas;
            finalAnterior = Final.Terminado;
        }

        if (Decisiones != null)
            preguntaAnterior = Decisiones.PreguntaActiva;
    }

    void Update()
    {
        VigilarLaPregunta();
        VigilarLaDecision();
        VigilarElFinal();
    }

    Tema TemaDeArranque()
    {
        return ModoDePruebas ? PruebasSeccion1 : Seccion1;
    }

    /// <summary>Al salir la pregunta entra la intermision, en la otra pista.</summary>
    void VigilarLaPregunta()
    {
        if (Decisiones == null) return;

        bool ahora = Decisiones.PreguntaActiva;
        bool acabaDeSalir = ahora && !preguntaAnterior;
        preguntaAnterior = ahora;

        if (!acabaDeSalir) return;

        // En pruebas la intermision se salta: es el clip mas largo y no aporta nada al
        // probar el bucle de juego. Sin swap, la seccion siguiente entrara igual cuando
        // vuelvan las notas, porque de eso se encarga el ReturnToDefault().
        if (ModoDePruebas && SaltarIntermisionEnPruebas)
        {
            if (LogAlCambiar)
                Debug.Log("MusicaPorSeccion: pregunta -> intermision SALTADA (modo pruebas)", this);
            return;
        }

        if (Intermision == null) return;

        MusicManager musica = MusicManager.Instance;
        if (musica == null) return;

        if (!musica.IsMusicPlaying())
        {
            // SwapTrack no hace nada con la musica parada, asi que avisamos en vez de
            // quedarnos en silencio sin saber por que.
            Debug.LogWarning("MusicaPorSeccion: no habia musica sonando al salir la pregunta, la intermision no entra.", this);
            return;
        }

        musica.SwapTrack(Intermision, false, musica.getVolume());

        if (LogAlCambiar)
            Debug.Log("MusicaPorSeccion: pregunta -> intermision", this);
    }

    /// <summary>
    /// Al contestar se deja preparado el tema de la seccion que viene, musica y chart.
    /// No suena aun: lo lanzara el NextSection() del ChartManager cuando vuelvan las notas.
    /// </summary>
    void VigilarLaDecision()
    {
        if (Final == null) return;

        int ahora = Final.DecisionesTomadas;
        if (ahora == decisionesAnteriores) return;

        decisionesAnteriores = ahora;

        // La decision numero N elige el tema de la seccion N+1, que es la entrada N-1
        // de la lista (la primera entrada es la seccion 2).
        // [CAMBIO] Antes esto miraba la ultima letra de SecuenciaActual(). Se rompio en
        // silencio al renombrar el enum Opcion de Buena/Mala a Derecha/Izquierda: la
        // letra paso de 'B' a 'D' y la comparacion daba siempre false, o sea que todas
        // las secciones sonaban con la variante Malo. Ahora se compara el enum, que si
        // alguien lo renombra da error de compilacion en vez de fallar mudo.
        int indice = ahora - 1;
        bool derecha = Final.UltimaDecision == DecisionManager.Opcion.Derecha;
        Tema tema = TemaDeLaSeccion(indice, derecha);

        if (tema == null) return;   // los avisos los da TemaDeLaSeccion

        MusicManager musica = MusicManager.Instance;
        if (musica != null && tema.Musica != null)
            musica.changeDefault(tema.Musica);

        PonerChart(tema);

        if (LogAlCambiar)
            Debug.Log("MusicaPorSeccion: decision " + ahora + " (" + (derecha ? "derecha" : "izquierda")
                + ") -> seccion " + (ahora + 1) + " preparada con '"
                + (tema.Musica != null ? tema.Musica.name : "sin musica") + "' y chart '" + tema.Chart + "'"
                + (ModoDePruebas ? " [pruebas]" : ""), this);
    }

    /// <summary>
    /// Deja el nombre del chart en el ChartManager. Quien lo usa es su NextSection(),
    /// tanto al arrancar el nivel como al reanudar las notas tras cada puente.
    /// </summary>
    void PonerChart(Tema tema)
    {
        if (Chart == null || tema == null || string.IsNullOrEmpty(tema.Chart)) return;

        Chart.filename = tema.Chart;
    }

    /// <summary>
    /// Que tema toca para la seccion cuyo indice se pasa. En pruebas se tira de la lista
    /// corta y se ignora si la decision fue buena o mala, que ahi da igual.
    /// Devuelve null si no hay nada que poner, y en ese caso todo se queda como este.
    /// </summary>
    Tema TemaDeLaSeccion(int indice, bool derecha)
    {
        if (ModoDePruebas)
        {
            if (indice < 0 || indice >= PruebasSecciones.Count)
            {
                if (LogAlCambiar)
                    Debug.Log("MusicaPorSeccion: sin tema de pruebas para el indice " + indice + ", todo sigue como esta.", this);
                return null;
            }

            return PruebasSecciones[indice];
        }

        if (indice < 0 || indice >= SeccionesSiguientes.Count)
        {
            // Pasa en la ultima decision: la partida termina ahi y no hay seccion nueva.
            if (LogAlCambiar)
                Debug.Log("MusicaPorSeccion: decision sin seccion siguiente configurada, no se cambia nada.", this);
            return null;
        }

        TemasDeSeccion seccion = SeccionesSiguientes[indice];
        Tema tema = derecha ? seccion.Derecha : seccion.Izquierda;

        if (tema == null || tema.Musica == null)
            Debug.LogError("MusicaPorSeccion: falta la musica de la opcion " + (derecha ? "Derecha" : "Izquierda") + " en '" + seccion.Nombre + "'.", this);

        return tema;
    }

    void VigilarElFinal()
    {
        if (Final == null || !FundirAlTerminar) return;

        bool ahora = Final.Terminado;
        bool acabaDeTerminar = ahora && !finalAnterior;
        finalAnterior = ahora;

        if (!acabaDeTerminar) return;

        MusicManager musica = MusicManager.Instance;
        if (musica == null) return;

        // La corrutina tiene que correr en el propio MusicManager: es un singleton con
        // DontDestroyOnLoad y el GameEndManager apaga medio nivel al terminar, asi que
        // lanzada desde aqui podria morir a medias. Mismo motivo que en PlayerDeathManager.
        musica.StartCoroutine(musica.FadeOut());

        if (LogAlCambiar)
            Debug.Log("MusicaPorSeccion: final de partida -> fundido de salida de la musica", this);
    }
}
