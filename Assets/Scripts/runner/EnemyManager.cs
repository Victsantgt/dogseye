using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Decide cuando aparece el enemigo, de que lado viene y como se va.
/// Va en LevelController, junto al SegmentGenerator y al RhythmSystemToggle.
///
/// El enemigo solo existe durante la fase de notas. Para saber en cual estamos NO se
/// mira ni al DecisionManager ni al terreno: se mira si el objeto -- RHYTHM SYSTEM --
/// esta encendido, que es justo lo que ya distingue las dos fases en este proyecto.
/// El DecisionManager lo apaga al lanzar la pregunta y el RhythmResumeTrigger (o el
/// temporizador del puente corto) lo vuelve a encender. Asi esto no acopla el reloj
/// del ritmo con el del terreno: solo lee un bool que ya existia.
///
///   notas ON  -> aparece un enemigo
///   notas OFF -> el que hubiera se va como si nos hubiera ganado, y no aparece
///                ninguno hasta que vuelvan las notas
///
/// Vencer() y Ganar() son las dos funciones que hay que llamar desde el sistema de
/// notas cuando eso este listo. De momento estan tambien en las teclas B y N, y la
/// aparicion a mano en la V.
/// </summary>
[DisallowMultipleComponent]
public class EnemyManager : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Prefab del enemigo. Assets/Prefabs/Runner/Enemy.prefab")]
    public GameObject PrefabEnemigo;

    [Tooltip("El Player. Si se deja vacio se busca por el tag TagDelJugador.")]
    public Transform Jugador;

    [Tooltip("De donde se saca si estamos en fase de notas o en el puente. Si se deja vacio se busca en este mismo GameObject.")]
    public RhythmSystemToggle SistemaNotas;

    [Tooltip("Tag con el que se busca al jugador si no esta asignado arriba.")]
    public string TagDelJugador = "Player";

    [Header("Cuando aparece")]
    [Tooltip("Si se engancha solo a la fase de notas. Desmarcalo si prefieres controlar la aparicion tu, llamando a Aparecer() desde el sistema de notas.")]
    public bool AparicionAutomatica = false;

    [Tooltip("Segundos que tarda en volver otro enemigo despues de que le vencieran o de que nos ganara.")]
    public float SegundosParaReaparecer = 5f;

    [Tooltip("Margen antes del primer enemigo de cada fase de notas. 0 = aparece en cuanto arrancan las notas.")]
    public float RetrasoPrimeraAparicion = 0f;

    [Header("Teclas de prueba")]
    [Tooltip("TEMPORAL: permite disparar las tres acciones a mano mientras el sistema de notas no las llame. Ponlo en false cuando ya lo hagan las notas.")]
    public bool UsarTeclasDePrueba = true;

    [Tooltip("Hace aparecer un enemigo.")]
    public Key TeclaAparecer = Key.V;

    [Tooltip("Le vencemos: sale despedido hacia arriba.")]
    public Key TeclaVencer = Key.B;

    [Tooltip("Nos gana: se vuelve por donde vino.")]
    public Key TeclaGanar = Key.N;

    EnemyRunner enemigoActual;
    float cuentaAtras = -1f;   // negativo = no hay ninguna aparicion pendiente
    bool faseAnterior;

    /// <summary>True mientras corre la fase de notas, o sea fuera del puente.</summary>
    public bool EnFaseDeNotas
    {
        // Sin toggle asignado damos por hecho que siempre estamos en notas, para que
        // las teclas de prueba sigan funcionando en una escena a medio montar.
        get { return SistemaNotas == null || SistemaNotas.Activo; }
    }

    /// <summary>True si hay un enemigo vivo que todavia no se esta yendo.</summary>
    public bool HayEnemigo { get { return enemigoActual != null && enemigoActual.EnJuego; } }

    /// <summary>
    /// True solo cuando el enemigo ya ha llegado y va acompanando al jugador. Es la
    /// ventana en la que Vencer() y Ganar() hacen algo: mientras viene de lejos las
    /// dos se ignoran, para que no salga volando ni se de la vuelta en la niebla.
    /// </summary>
    public bool SePuedeResolver { get { return enemigoActual != null && enemigoActual.SePuedeResolver; } }

    /// <summary>
    /// True mientras quede algun enemigo en pantalla, incluidos los que ya estan
    /// yendose. Solo puede haber uno a la vez, asi que esto es lo que bloquea la
    /// aparicion del siguiente.
    /// </summary>
    public bool HayEnemigoEnPantalla { get { return enemigoActual != null; } }

    void Awake()
    {
        if (SistemaNotas == null)
            SistemaNotas = GetComponent<RhythmSystemToggle>();

        if (Jugador == null)
        {
            GameObject encontrado = GameObject.FindGameObjectWithTag(TagDelJugador);
            if (encontrado != null)
                Jugador = encontrado.transform;
        }
    }

    void Start()
    {
        faseAnterior = EnFaseDeNotas;

        // La partida arranca ya en fase de notas, asi que no habra ningun flanco de
        // subida que lo dispare: el primero lo pedimos aqui.
        if (AparicionAutomatica && faseAnterior)
            ProgramarAparicion(RetrasoPrimeraAparicion);
    }

    void Update()
    {
        VigilarLaFase();
        AvanzarCuentaAtras();
        LeerTeclas();
    }

    void VigilarLaFase()
    {
        bool fase = EnFaseDeNotas;
        if (fase == faseAnterior)
            return;

        faseAnterior = fase;

        if (fase)
        {
            // Vuelven las notas: vuelve el enemigo.
            if (AparicionAutomatica)
                ProgramarAparicion(RetrasoPrimeraAparicion);
        }
        else
        {
            // Empieza el puente. Durante la pregunta y la transicion no puede haber
            // enemigos, asi que el que quedara se va como si nos hubiera ganado.
            CancelarAparicion();

            // RetirarseYa() y no Ganar(): aqui no estamos resolviendo un duelo, estamos
            // vaciando la pantalla. Si el enemigo todavia venia de lejos, Ganar() lo
            // ignoraria y se quedaria acercandose durante todo el puente.
            if (HayEnemigo)
                enemigoActual.RetirarseYa();
        }
    }

    void AvanzarCuentaAtras()
    {
        if (cuentaAtras < 0f)
            return;

        cuentaAtras -= Time.deltaTime;
        if (cuentaAtras > 0f)
            return;

        cuentaAtras = -1f;

        // Solo puede haber uno en pantalla. Si el anterior todavia se esta yendo
        // (SegundosParaReaparecer por debajo de su SegundosHastaDespawn), no perdemos
        // la aparicion: la reintentamos en cuanto haya desaparecido.
        if (HayEnemigoEnPantalla)
        {
            ProgramarAparicion(0.25f);
            return;
        }

        Aparecer();
    }

    void LeerTeclas()
    {
        if (!UsarTeclasDePrueba)
            return;

        Keyboard teclado = Keyboard.current;
        if (teclado == null)
            return;

        if (teclado[TeclaAparecer].wasPressedThisFrame) Aparecer();
        if (teclado[TeclaVencer].wasPressedThisFrame) Vencer();
        if (teclado[TeclaGanar].wasPressedThisFrame) Ganar();
    }

    /// <summary>
    /// Saca un enemigo de la niebla. Viene siempre por el centro, justo delante del
    /// jugador, sea cual sea el segmento; el sorteo del lado solo decide por donde
    /// saldra despedido si le vencemos.
    /// No hace nada si estamos en el puente, ni si queda alguno en pantalla: solo
    /// puede haber uno a la vez, y cuenta tambien el que se este yendo.
    /// </summary>
    [ContextMenu("Aparecer enemigo (prueba)")]
    public GameObject Aparecer()
    {
        if (!EnFaseDeNotas)
        {
            Debug.Log("EnemyManager: no salen enemigos durante el puente, se ha ignorado la peticion.", this);
            return null;
        }

        if (PrefabEnemigo == null)
        {
            Debug.LogError("EnemyManager: falta asignar el prefab del enemigo en el Inspector.", this);
            return null;
        }

        if (Jugador == null)
        {
            Debug.LogError("EnemyManager: no se ha encontrado al jugador (tag '" + TagDelJugador + "').", this);
            return null;
        }

        GameObject instancia = Instantiate(PrefabEnemigo);
        instancia.name = PrefabEnemigo.name;

        EnemyRunner enemigo = instancia.GetComponent<EnemyRunner>();
        if (enemigo == null)
            enemigo = instancia.AddComponent<EnemyRunner>();

        enemigoActual = enemigo;
        enemigo.AlDesaparecer += OlvidarEnemigo;

        enemigo.Lanzar(Jugador, Random.value < 0.5f);

        return instancia;
    }

    /// <summary>
    /// Le hemos vencido. Es la funcion que tiene que llamar el sistema de notas cuando
    /// el jugador acierte lo que haga falta. Programa el siguiente enemigo.
    /// Se ignora si el enemigo todavia viene de lejos: hasta que no esta acompanando
    /// no hay duelo.
    /// </summary>
    [ContextMenu("Vencer enemigo (prueba)")]
    public void Vencer()
    {
        if (!SePuedeResolver)
            return;

        enemigoActual.Vencer();
        //ProgramarAparicion(SegundosParaReaparecer);
    }

    /// <summary>
    /// Nos ha ganado. Es la funcion que tiene que llamar el sistema de notas cuando el
    /// jugador falle. Programa el siguiente enemigo.
    /// Se ignora mientras viene de lejos, igual que Vencer().
    /// </summary>
    [ContextMenu("Ganar enemigo (prueba)")]
    public void Ganar()
    {
        if (!SePuedeResolver)
            return;

        enemigoActual.Ganar();
        //ProgramarAparicion(SegundosParaReaparecer);
    }

    /// <summary>Quita el enemigo de en medio al instante, sin animacion ni reaparicion.</summary>
    public void LimpiarEnemigo()
    {
        CancelarAparicion();

        if (enemigoActual != null)
            enemigoActual.Desaparecer();
    }

    void ProgramarAparicion(float segundos)
    {
        cuentaAtras = segundos <= 0f ? 0.0001f : segundos;
    }

    void CancelarAparicion()
    {
        cuentaAtras = -1f;
    }

    void OlvidarEnemigo()
    {
        enemigoActual = null;
    }
}
