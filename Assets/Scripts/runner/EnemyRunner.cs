using UnityEngine;

/// <summary>
/// En que punto de su vida esta el enemigo.
/// </summary>
public enum EstadoEnemigo
{
    Inactivo,       // recien instanciado, todavia no le han dicho de donde viene
    Aproximandose,  // sale de la niebla y se echa encima del jugador
    Acompanando,    // ya esta al lado (o delante) y va a la misma velocidad
    Vencido,        // le hemos apartado: se va hacia arriba
    Retirandose     // nos ha ganado: se vuelve por donde vino
}

/// <summary>
/// Comportamiento del enemigo que persigue al jugador durante la fase de notas.
///
/// TODO el movimiento se calcula como un DESPLAZAMIENTO RESPECTO AL JUGADOR, no en
/// coordenadas de mundo. Esa es la idea importante del script:
///
///   - "Igualar la velocidad para acompanarlo" sale gratis: basta con dejar de tocar
///     el desplazamiento y recolocarse cada frame sobre la posicion del jugador.
///   - El aceleron del puente (TransitionRush) multiplica la velocidad del jugador,
///     pero como aqui todo es relativo el enemigo no se descuelga ni se adelanta.
///   - Las velocidades del Inspector son de ACERCAMIENTO, no de mundo: con el jugador
///     a 24 u/s y VelocidadDeAcercamiento a 140, el enemigo va a 116 u/s hacia atras
///     en coordenadas de mundo, o sea se echa encima de verdad.
///
/// Quien lo crea y decide cuando aparece es EnemyManager. Este componente solo sabe
/// moverse y no consulta a nadie mas: se le dice de que lado viene y si el pasillo es
/// estrecho, y ya.
/// </summary>
[DisallowMultipleComponent]
public class EnemyRunner : MonoBehaviour
{
    [Header("Aparicion")]
    [Tooltip("A cuantas unidades por delante del jugador aparece. La niebla de la escena tapa a partir de ~250, asi que por debajo de eso se le vera aparecer de la nada.")]
    public float DistanciaDeAparicion = 250f;

    [Tooltip("Altura a la que vuela, respecto a la del jugador. 0 = a la misma altura.")]
    public float AlturaSobreElJugador = 0f;

    [Header("Anchura del pasillo")]
    // MEDIDO sobre la escena, no estimado. Lo que mas se mete hacia el centro no son
    // las estanterias sino las 'column' que cuelgan de ellas: dejan el hueco libre en
    // x [-9.01, 8.62]. Con el cubo (1.5 de medio ancho) el centro del enemigo puede ir
    // como mucho a [-7.51, 7.12], y como el jugador esta en x=0.20 eso son -7.71 / 6.92
    // de separacion. 6.5 deja margen por los dos lados.
    [Tooltip("Separacion lateral respecto al jugador en un pasillo Wide. El hueco libre medido deja como maximo 6.9 hacia la derecha; por encima de eso el enemigo se mete en las columnas de las estanterias.")]
    public float LateralWide = 6.5f;

    [Header("Acompanar")]
    [Tooltip("Unidades por delante del jugador a las que se queda cuando le alcanza.")]
    public float DistanciaAlAcompanar = 8f;

    [Tooltip("A cuantas unidades del sitio final empieza a frenar. Por debajo de esto ya no va a velocidad de acercamiento.")]
    public float DistanciaDeFrenado = 45f;

    [Tooltip("Segundos que tarda en asentarse en el sitio una vez ha frenado.")]
    public float SuavizadoDeFrenado = 0.35f;

    [Header("Velocidades (relativas al jugador, no de mundo)")]
    [Tooltip("A que ritmo recorta la distancia mientras sale de la niebla.")]
    public float VelocidadDeAcercamiento = 140f;

    [Tooltip("A que ritmo se aleja hacia delante cuando nos gana.")]
    public float VelocidadDeRetirada = 170f;

    [Tooltip("A que ritmo sube cuando le vencemos.")]
    public float VelocidadAlSerVencido = 55f;

    [Tooltip("Cuanto se aparta ademas hacia su lado al ser vencido. 0 = sube recto.")]
    public float ApartadoAlSerVencido = 22f;

    [Tooltip("Grados por segundo que gira al ser apartado. Solo es adorno.")]
    public float GiroAlSerVencido = 320f;

    [Header("Despawn")]
    [Tooltip("Segundos desde que empieza la accion (vencido o retirada) hasta que se destruye. Para entonces ya esta fuera de plano en los dos casos.")]
    public float SegundosHastaDespawn = 2f;

    [Header("Fisica")]
    [Tooltip("Pone el collider en trigger al arrancar. El prefab lo trae solido y el Player tiene Rigidbody, asi que sin esto el enemigo empujaria al jugador. Solo afecta a la instancia, no al prefab.")]
    public bool PonerColliderEnTrigger = true;

    /// <summary>Se dispara justo antes de destruirse, sea por la via que sea.</summary>
    public event System.Action AlDesaparecer;

    Transform jugador;
    EstadoEnemigo estado = EstadoEnemigo.Inactivo;

    // Desplazamiento respecto al jugador. Es el estado real del enemigo: la posicion
    // de mundo se recalcula a partir de el en cada LateUpdate.
    Vector3 desplazamiento;

    float lateralDeEntrada;   // carril por el que va, con signo. En Narrow es 0.
    float signoDeEntrada;     // lado que le toco en el sorteo. En Narrow no mueve el carril, pero marca hacia donde le apartamos al vencerle.
    float velocidadFrenado;   // memoria del SmoothDamp
    bool frenando;            // ya ha entrado en el tramo de frenado
    bool despawnProgramado;

    /// <summary>En que punto de su vida esta.</summary>
    public EstadoEnemigo Estado { get { return estado; } }

    /// <summary>
    /// True mientras el enemigo sigue vivo y no se esta yendo todavia, o sea
    /// acercandose o acompanando.
    /// </summary>
    public bool EnJuego { get { return estado == EstadoEnemigo.Aproximandose || estado == EstadoEnemigo.Acompanando; } }

    /// <summary>
    /// True solo cuando ya esta colocado al lado (o delante) del jugador. Es la unica
    /// ventana en la que se le puede vencer o puede ganar: mientras viene de lejos no
    /// hay duelo todavia, asi que no puede salir volando ni darse la vuelta.
    /// </summary>
    public bool SePuedeResolver { get { return estado == EstadoEnemigo.Acompanando; } }

    void Awake()
    {
        if (PonerColliderEnTrigger)
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }
    }

    /// <summary>
    /// Le dice al enemigo que empiece a salir de la niebla. Lo llama EnemyManager
    /// nada mas instanciarlo.
    /// </summary>
    /// <param name="objetivo">El Player. Todo el movimiento se mide respecto a el.</param>
    /// <param name="pasilloEstrecho">
    /// True si el terreno es Narrow. Ahi no hay carril lateral que valga: viene ya
    /// centrado delante del jugador. En Wide viene por su lado, en diagonal.
    /// </param>
    /// <param name="ladoDerecho">
    /// De que lado sale. Lo sortea el manager. En Narrow no cambia por donde viene,
    /// solo hacia donde sale despedido si le vencemos.
    /// </param>
    public void Lanzar(Transform objetivo, bool pasilloEstrecho, bool ladoDerecho)
    {
        if (objetivo == null)
        {
            Debug.LogError("EnemyRunner: no hay jugador al que perseguir.", this);
            Destroy(gameObject);
            return;
        }

        jugador = objetivo;
        signoDeEntrada = ladoDerecho ? 1f : -1f;

        // En Wide entra por un lado y se queda ahi: la diagonal es su sitio final.
        // En Narrow el hueco libre son 9 unidades y el enemigo mide 3, asi que no hay
        // carril lateral en el que quepa: sale ya centrado delante del jugador y viene
        // recto. Si entrara de lado se comeria las estanterias por el camino.
        lateralDeEntrada = pasilloEstrecho ? 0f : signoDeEntrada * LateralWide;

        desplazamiento = new Vector3(lateralDeEntrada, AlturaSobreElJugador, DistanciaDeAparicion);
        velocidadFrenado = 0f;
        frenando = false;
        estado = EstadoEnemigo.Aproximandose;

        Recolocar();
    }

    /// <summary>
    /// Le hemos vencido: sale despedido hacia arriba y hacia su lado, como si le
    /// hubieramos apartado de un manotazo. Se destruye solo pasados
    /// SegundosHastaDespawn.
    /// Solo vale mientras acompana: si todavia viene de lejos no hay duelo que resolver
    /// y se ignora.
    /// </summary>
    [ContextMenu("Vencer (prueba)")]
    public void Vencer()
    {
        if (!SePuedeResolver) return;

        estado = EstadoEnemigo.Vencido;
        ProgramarDespawn();
    }

    /// <summary>
    /// Nos ha ganado: se vuelve por donde vino, hacia delante, y se pierde en la
    /// niebla. Se destruye solo pasados SegundosHastaDespawn.
    /// Solo vale mientras acompana, igual que Vencer().
    /// </summary>
    [ContextMenu("Ganar (prueba)")]
    public void Ganar()
    {
        if (!SePuedeResolver) return;

        Retirarse();
    }

    /// <summary>
    /// Misma huida que Ganar(), pero valida tambien si todavia venia de lejos. No es
    /// el resultado de un duelo: es la salida forzada de cuando empieza el puente y no
    /// puede quedar ningun enemigo en pantalla. Por eso se salta el filtro de
    /// SePuedeResolver, que si no dejaria al que venia acercandose colgado a media
    /// niebla durante toda la transicion.
    /// </summary>
    public void RetirarseYa()
    {
        if (!EnJuego) return;

        Retirarse();
    }

    void Retirarse()
    {
        estado = EstadoEnemigo.Retirandose;
        ProgramarDespawn();
    }

    /// <summary>
    /// Lo quita de en medio ya, sin animacion. Para cortes secos: muerte del jugador,
    /// final de partida o recarga de escena.
    /// </summary>
    public void Desaparecer()
    {
        Avisar();
        Destroy(gameObject);
    }

    // LateUpdate y no Update: BasicMovement mueve al jugador en su Update, asi que
    // leyendo aqui su posicion nos ahorramos un frame de retraso y el enemigo no
    // tiembla respecto a el cuando entra el aceleron.
    void LateUpdate()
    {
        if (estado == EstadoEnemigo.Inactivo)
            return;

        // El jugador se destruye al recargar la escena; sin ancla no hay nada que hacer.
        if (jugador == null)
        {
            Desaparecer();
            return;
        }

        float dt = Time.deltaTime;

        switch (estado)
        {
            case EstadoEnemigo.Aproximandose: Aproximarse(dt); break;
            case EstadoEnemigo.Acompanando:   /* el desplazamiento se queda quieto */ break;
            case EstadoEnemigo.Vencido:       SalirPorArriba(dt); break;
            case EstadoEnemigo.Retirandose:   VolverPorDondeVino(dt); break;
        }

        Recolocar();
    }

    void Aproximarse(float dt)
    {
        float restante = desplazamiento.z - DistanciaAlAcompanar;

        if (restante > DistanciaDeFrenado)
        {
            desplazamiento.z -= VelocidadDeAcercamiento * dt;
        }
        else
        {
            // El SmoothDamp arranca desde parado si no se le dice a que velocidad
            // veniamos, y el enemigo pasaria de 140 u/s a frenar de golpe. Le pasamos
            // la velocidad de acercamiento (negativa: la distancia va bajando) para
            // que la deceleracion sea continua.
            if (!frenando)
            {
                frenando = true;
                velocidadFrenado = -VelocidadDeAcercamiento;
            }

            desplazamiento.z = Mathf.SmoothDamp(desplazamiento.z, DistanciaAlAcompanar,
                ref velocidadFrenado, SuavizadoDeFrenado);

            // Ya esta practicamente en su sitio: a partir de aqui solo acompana.
            if (Mathf.Abs(desplazamiento.z - DistanciaAlAcompanar) < 0.15f)
            {
                desplazamiento.z = DistanciaAlAcompanar;
                estado = EstadoEnemigo.Acompanando;
            }
        }

        // El carril no se toca durante el acercamiento: viene recto por el suyo, sea
        // el lateral de un Wide o el centro de un Narrow.
    }

    void SalirPorArriba(float dt)
    {
        desplazamiento.y += VelocidadAlSerVencido * dt;

        // Hacia el lado que le toco en el sorteo. En Narrow viene centrado, asi que
        // este signo es lo unico que decide por donde sale despedido.
        desplazamiento.x += signoDeEntrada * ApartadoAlSerVencido * dt;

        if (GiroAlSerVencido != 0f)
            transform.Rotate(Vector3.right * GiroAlSerVencido * dt, Space.Self);
    }

    void VolverPorDondeVino(float dt)
    {
        // Se va por su carril, que es el mismo por el que vino: durante el acercamiento
        // y el acompanar la X no se ha movido.
        desplazamiento.z += VelocidadDeRetirada * dt;
    }

    void Recolocar()
    {
        transform.position = jugador.position + desplazamiento;
    }

    void ProgramarDespawn()
    {
        if (despawnProgramado) return;

        despawnProgramado = true;
        Invoke("Desaparecer", SegundosHastaDespawn);
    }

    void Avisar()
    {
        // Se vacia antes de avisar: el manager reacciona pidiendo el siguiente enemigo
        // y no queremos que un OnDestroy tardio vuelva a entrar por aqui.
        System.Action aviso = AlDesaparecer;
        AlDesaparecer = null;

        if (aviso != null)
            aviso();
    }

    void OnDestroy()
    {
        // Red de seguridad: si alguien lo destruye por fuera (recarga de escena,
        // Destroy a mano) el manager se entera igual y no se queda esperando.
        Avisar();
    }
}
