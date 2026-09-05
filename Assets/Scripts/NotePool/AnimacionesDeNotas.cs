using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Un hueco de animacion: lo que pasa cuando se pulsa una de las tres teclas.
///
/// Hay dos formas de engancharse, y se pueden usar las dos a la vez:
///   - NombreDelBool: el parametro Bool del Animator que se enciende al pulsar.
///   - AlPulsar: un UnityEvent normal, para cualquier otra cosa que quieras colgar
///     desde el Inspector (particulas, sonido, un script propio...).
/// Los dos son opcionales. Si se dejan vacios no pasa nada y no se queja.
/// </summary>
[System.Serializable]
public class HuecoDeAnimacion
{
    [Tooltip("Solo para leerlo en el Inspector: que gesto va aqui. No hace nada.")]
    public string Gesto = "";

    [Tooltip("Nombre del parametro Bool que se pasa a Animator.SetBool al pulsar. Distingue mayusculas y tiene que existir en el controller. Dejalo vacio si el gesto no va por Animator.")]
    public string NombreDelBool = "";

    [Tooltip("Se invoca al pulsar la tecla. Para todo lo que no sea el bool del Animator.")]
    public UnityEvent AlPulsar;

    // Frames que le quedan encendido. 0 = apagado.
    int framesRestantes;
    bool encendido;

    /// <summary>True mientras el bool de este gesto esta puesto.</summary>
    public bool Encendido { get { return encendido; } }

    /// <summary>Enciende el bool y arranca la cuenta de frames. La llama AnimacionesDeNotas.</summary>
    public void Encender(Animator animator, int frames)
    {
        // Se reinicia la cuenta aunque ya estuviera encendido: dos pulsaciones seguidas
        // no lo apagan antes de tiempo.
        framesRestantes = Mathf.Max(1, frames);

        if (!encendido)
        {
            Escribir(animator, true);
            encendido = true;
        }

        if (AlPulsar != null)
            AlPulsar.Invoke();
    }

    /// <summary>
    /// Descuenta un frame y apaga el bool cuando toca. Se llama una vez por frame.
    /// </summary>
    public void Avanzar(Animator animator)
    {
        if (!encendido) return;

        framesRestantes--;
        if (framesRestantes > 0) return;

        Escribir(animator, false);
        encendido = false;
    }

    /// <summary>Apaga el bool ya, sin esperar a la cuenta.</summary>
    public void Apagar(Animator animator)
    {
        if (!encendido) return;

        Escribir(animator, false);
        encendido = false;
        framesRestantes = 0;
    }

    void Escribir(Animator animator, bool valor)
    {
        // Solo se comprueba que haya a quien llamar y con que nombre. Un hueco sin
        // Animator o sin nombre es el estado valido de "todavia sin montar".
        // Si el parametro no existe en el controller, avisa el propio Unity.
        if (animator == null || string.IsNullOrEmpty(NombreDelBool)) return;

        animator.SetBool(NombreDelBool, valor);
    }
}

/// <summary>
/// Huecos de animacion para las tres teclas del juego de ritmo, sobre UN SOLO Animator
/// manejado con tres parametros Bool, uno por gesto.
///
/// Va en el mismo GameObject que el ColliderNoteScript (ColliderNotas) y lee las MISMAS
/// InputActionReference que el, para que no puedan desincronizarse. El mapeo del asset
/// de input es:
///
///   A (accion Left)   -> agarrar a la izquierda
///   S (accion Middle) -> empujar carrito
///   D (accion Right)  -> agarrar a la derecha
///
/// COMO FUNCIONA EL BOOL. Al pulsar se pone a true y se mantiene FramesEncendido frames,
/// luego se apaga solo. Ese margen es lo importante: el Animator evalua una vez por
/// frame y en su propio momento del ciclo, asi que encender y apagar dentro del mismo
/// frame se lo podria perder entero. Con dos frames lo ve seguro, sea cual sea el
/// updateMode del Animator.
///
/// La ventaja sobre un Trigger es que el bool NO se puede quedar cargado. Un Trigger que
/// ninguna transicion consume espera indefinidamente y suelta su gesto mucho despues (el
/// gesto fantasma clasico al pulsar dos teclas a la vez). Aqui, se tome la transicion o
/// no, a los pocos frames el bool esta apagado: en el peor caso se pierde un gesto, que
/// es un fallo mucho mas limpio.
///
/// IMPORTANTE: un Animator solo puede animar transforms de SU PROPIA jerarquia. Para que
/// los tres gestos salgan de este Animator unico, los tres clips tienen que estar hechos
/// sobre el mismo armature. Un clip autorizado sobre otro rig no movera nada aunque el
/// bool se encienda bien.
///
/// Este componente cuelga de -- RHYTHM SYSTEM --, que esta apagado durante todo el puente
/// entre secciones, asi que ahi no se dispara nada. Es lo correcto: en el puente no hay
/// notas.
/// </summary>
[DisallowMultipleComponent]
public class AnimacionesDeNotas : MonoBehaviour
{
    [Header("Input (las mismas acciones que usa ColliderNoteScript)")]
    [Tooltip("Accion Left del asset. En teclado es la A.")]
    public InputActionReference TeclaIzquierda;

    [Tooltip("Accion Middle del asset. En teclado es la S.")]
    public InputActionReference TeclaCentro;

    [Tooltip("Accion Right del asset. En teclado es la D.")]
    public InputActionReference TeclaDerecha;

    [Header("Animator")]
    [Tooltip("El Animator unico que lleva los tres gestos. Tiene que ser el objeto de la ESCENA, no el FBX del proyecto.")]
    public Animator Animator;

    [Tooltip("Frames que el bool se queda encendido antes de apagarse solo. 1 puede perderse segun el updateMode del Animator; 2 es seguro. Subelo solo si algun gesto no llega a entrar.")]
    public int FramesEncendido = 2;

    [Header("Huecos de animacion")]
    public HuecoDeAnimacion AgarrarIzquierda = new HuecoDeAnimacion { Gesto = "A - agarrar a la izquierda" };
    public HuecoDeAnimacion EmpujarCarrito = new HuecoDeAnimacion { Gesto = "S - empujar carrito" };
    public HuecoDeAnimacion AgarrarDerecha = new HuecoDeAnimacion { Gesto = "D - agarrar a la derecha" };

    [Header("Ajustes")]
    [Tooltip("Habilita las acciones de input si llegan apagadas. Sin esto los gestos no saldrian si nadie mas las ha encendido.")]
    public bool HabilitarLasAccionesSiHaceFalta = true;

    [Tooltip("Deja en consola cada gesto disparado. Util mientras se montan las animaciones; desmarcalo despues.")]
    public bool LogAlDisparar = false;

    void OnEnable()
    {
        if (!HabilitarLasAccionesSiHaceFalta) return;

        Habilitar(TeclaIzquierda);
        Habilitar(TeclaCentro);
        Habilitar(TeclaDerecha);
    }

    void OnDisable()
    {
        // Si el puente empieza justo con un bool encendido, el componente se apaga a
        // media cuenta y el bool se quedaria a true. Al volver las notas el Animator lo
        // veria puesto y soltaria el gesto de la nada. Los apagamos al salir.
        AgarrarIzquierda.Apagar(Animator);
        EmpujarCarrito.Apagar(Animator);
        AgarrarDerecha.Apagar(Animator);
    }

    void Update()
    {
        if (Pulsada(TeclaIzquierda)) DispararIzquierda();
        if (Pulsada(TeclaCentro)) DispararCentro();
        if (Pulsada(TeclaDerecha)) DispararDerecha();
    }

    void LateUpdate()
    {
        // El descuento va en LateUpdate para que el frame en el que se pulsa cuente
        // entero: en Update acabamos de encenderlo y aqui ya solo restamos.
        AgarrarIzquierda.Avanzar(Animator);
        EmpujarCarrito.Avanzar(Animator);
        AgarrarDerecha.Avanzar(Animator);
    }

    /// <summary>A: agarrar a la izquierda. Publica por si hay que lanzarla desde otro sitio.</summary>
    [ContextMenu("Disparar A (agarrar a la izquierda)")]
    public void DispararIzquierda() { Lanzar(AgarrarIzquierda); }

    /// <summary>S: empujar carrito.</summary>
    [ContextMenu("Disparar S (empujar carrito)")]
    public void DispararCentro() { Lanzar(EmpujarCarrito); }

    /// <summary>D: agarrar a la derecha.</summary>
    [ContextMenu("Disparar D (agarrar a la derecha)")]
    public void DispararDerecha() { Lanzar(AgarrarDerecha); }

    void Lanzar(HuecoDeAnimacion hueco)
    {
        if (hueco == null) return;

        if (LogAlDisparar)
            Debug.Log("AnimacionesDeNotas: " + hueco.Gesto + " -> bool '" + hueco.NombreDelBool + "' encendido", this);

        hueco.Encender(Animator, FramesEncendido);
    }

    static bool Pulsada(InputActionReference referencia)
    {
        return referencia != null && referencia.action != null && referencia.action.WasPressedThisFrame();
    }

    void Habilitar(InputActionReference referencia)
    {
        if (referencia == null || referencia.action == null) return;
        if (referencia.action.enabled) return;

        referencia.action.Enable();
        Debug.Log("AnimacionesDeNotas: la accion '" + referencia.action.name + "' estaba apagada y se ha encendido.", this);
    }
}
