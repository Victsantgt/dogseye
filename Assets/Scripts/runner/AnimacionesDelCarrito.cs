using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [ANADIDO: animaciones del carrito]
///
/// Enciende y apaga los bools del Animator del carrito (controller 'Untitled'). Mismo
/// sistema de bools que los brazos, con dos formas distintas de usarlos:
///
///   CORRER (clip CABALLITO, en bucle) -> bool SOSTENIDO.
///     Se mantiene encendido todo el rato que el jugador va mas rapido de lo normal, o
///     sea durante el aceleron del puente entre secciones. Como el clip es un bucle, el
///     bool no puede ser un pulso: la transicion de vuelta necesita verlo en false.
///
///   EMPUJAR (clip Empujar, una sola vez) -> bool de PULSO.
///     Se enciende al pulsar la tecla del carril del medio (S) y se apaga solo a los
///     FramesEncendido frames, igual que los gestos de los brazos.
///
/// DE DONDE SALE LO DE CORRER. No se lee TransitionRush.Activo ni su FrenadoSolicitado,
/// sino la velocidad, con el mismo driver que el CLAUDE.md fija para todos los efectos
/// del puente:
///
///     InverseLerp(1, MultiplicadorMaximo, VelocidadActual / PlayerSpeed)
///
/// Asi el caballito arranca y para exactamente a la vez que las lineas de velocidad y el
/// dolly zoom, sin sincronizar nada entre ellos, y dura toda la rampa de frenado en vez
/// de cortarse en seco al cruzar el RushStopTrigger. Y por el mismo motivo NO sale en el
/// puente sin transicion: ahi la velocidad no cambia, asi que el driver da 0.
///
/// DONDE VA ESTE COMPONENTE. En el Player, NO dentro de -- RHYTHM SYSTEM --. Ese objeto
/// se apaga durante todo el puente, que es justo cuando el caballito tiene que verse.
///
/// El DANO todavia no esta: el estado existe en el controller pero sin clip, y su
/// transicion esta silenciada. Ver el comentario de mas abajo.
/// </summary>
[DisallowMultipleComponent]
public class AnimacionesDelCarrito : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Animator del carrito, el que lleva el controller 'Untitled'. Tiene que ser el objeto de la ESCENA.")]
    public Animator Animator;

    [Tooltip("De donde se lee la velocidad. Si se deja vacio se busca el BasicMovement de este mismo GameObject.")]
    public BasicMovement Movimiento;

    [Tooltip("Solo para leer MultiplicadorMaximo, que es el tope del driver de velocidad. Si se deja vacio se busca aqui.")]
    public TransitionRush Aceleron;

    [Tooltip("Accion Middle del asset de input. En teclado es la S.")]
    public InputActionReference TeclaEmpujar;

    [Header("Nombres de los parametros del Animator")]
    [Tooltip("Bool sostenido mientras el jugador corre. Distingue mayusculas.")]
    public string BoolCorrer = "Correr";

    [Tooltip("Bool de pulso al empujar el carrito. Distingue mayusculas.")]
    public string BoolEmpujar = "Empujar";

    [Header("Ajustes")]
    [Tooltip("Por encima de que valor del driver de velocidad se considera que esta corriendo. 0 = en cuanto pase de la velocidad normal. Un poco por encima de 0 evita que parpadee por redondeo.")]
    [Range(0f, 0.5f)] public float UmbralDeCarrera = 0.02f;

    [Tooltip("Frames que el bool de empujar se queda encendido antes de apagarse solo. 2 es seguro para que el Animator lo vea.")]
    public int FramesEncendido = 2;

    [Tooltip("Habilita la accion de input si llega apagada.")]
    public bool HabilitarLaAccionSiHaceFalta = true;

    [Tooltip("Deja rastro en consola al empezar y terminar de correr, y en cada empujon.")]
    public bool LogAlCambiar = false;

    bool corriendo;
    bool empujando;
    int framesRestantes;

    /// <summary>True mientras el caballito esta sonando.</summary>
    public bool Corriendo { get { return corriendo; } }

    void Awake()
    {
        if (Movimiento == null) Movimiento = GetComponent<BasicMovement>();
        if (Aceleron == null) Aceleron = GetComponent<TransitionRush>();
    }

    void OnEnable()
    {
        if (HabilitarLaAccionSiHaceFalta && TeclaEmpujar != null && TeclaEmpujar.action != null && !TeclaEmpujar.action.enabled)
        {
            TeclaEmpujar.action.Enable();
            Debug.Log("AnimacionesDelCarrito: la accion '" + TeclaEmpujar.action.name + "' estaba apagada y se ha encendido.", this);
        }
    }

    void OnDisable()
    {
        // Que no se quede ningun bool encendido si nos apagan a media animacion.
        Escribir(BoolCorrer, false);
        Escribir(BoolEmpujar, false);
        corriendo = false;
        empujando = false;
        framesRestantes = 0;
    }

    void Update()
    {
        ActualizarCorrer();
        LeerEmpujar();
    }

    void LateUpdate()
    {
        // El pulso de empujar se descuenta aqui para que el frame de la pulsacion cuente
        // entero, igual que en AnimacionesDeNotas.
        if (!empujando) return;

        framesRestantes--;
        if (framesRestantes > 0) return;

        Escribir(BoolEmpujar, false);
        empujando = false;
    }

    void ActualizarCorrer()
    {
        bool deberia = CalcularSiCorre();
        if (deberia == corriendo) return;

        corriendo = deberia;
        Escribir(BoolCorrer, corriendo);

        if (LogAlCambiar)
            Debug.Log("AnimacionesDelCarrito: correr " + (corriendo ? "ON" : "OFF"), this);
    }

    /// <summary>
    /// El driver de velocidad del proyecto. Devuelve true mientras el jugador va por
    /// encima de su velocidad normal, sea cual sea el motivo.
    /// </summary>
    bool CalcularSiCorre()
    {
        if (Movimiento == null || Movimiento.PlayerSpeed <= 0f) return false;

        float tope = Aceleron != null ? Aceleron.MultiplicadorMaximo : 2.5f;
        if (tope <= 1f) return false;

        float driver = Mathf.InverseLerp(1f, tope, Movimiento.VelocidadActual / Movimiento.PlayerSpeed);
        return driver > UmbralDeCarrera;
    }

    void LeerEmpujar()
    {
        if (TeclaEmpujar == null || TeclaEmpujar.action == null) return;
        if (!TeclaEmpujar.action.WasPressedThisFrame()) return;

        // Mientras corre no se empuja: el estado Correr no tiene salida hacia Empujar,
        // asi que encender el bool solo dejaria un valor colgado que saltaria al volver
        // al idle. Es mas limpio ignorarlo aqui.
        if (corriendo) return;

        Empujar();
    }

    /// <summary>
    /// Lanza el empujon del carrito. Publica y en el menu contextual para poder probarla
    /// sin jugar, y por si mas adelante interesa dispararla desde otro sitio.
    /// </summary>
    [ContextMenu("Empujar (prueba)")]
    public void Empujar()
    {
        framesRestantes = Mathf.Max(1, FramesEncendido);

        if (!empujando)
        {
            Escribir(BoolEmpujar, true);
            empujando = true;
        }

        if (LogAlCambiar)
            Debug.Log("AnimacionesDelCarrito: empujar", this);
    }

    void Escribir(string nombre, bool valor)
    {
        if (Animator == null || string.IsNullOrEmpty(nombre)) return;

        Animator.SetBool(nombre, valor);
    }
}
