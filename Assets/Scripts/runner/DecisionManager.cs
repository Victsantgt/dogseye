using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lanza la pregunta en pantalla y traduce la respuesta del jugador en un cambio
/// de terreno dentro del SegmentGenerator.
///
/// Flecha izquierda = accion BUENA -> el nivel tiende a Narrow
/// Flecha derecha   = accion MALA  -> el nivel tiende a Wide
/// </summary>
public class DecisionManager : MonoBehaviour
{
    public enum Opcion { Buena, Mala }

    [SerializeField] private InputActionReference leftDecision;
    [SerializeField] private InputActionReference rightDecision;

    public Dialogue dialogo;

    [Tooltip("Objeto que se activa y desactiva al mostrar u ocultar la pregunta.")]
    public GameObject PanelPregunta;

    public PhoneVibration phone;
    public BubbleText bubbleText;
    public DialogueOptionsPopup options;

    [Header("Tiempo para responder")]
    [Tooltip("Segundos que tiene el jugador para elegir. Al llegar a 0 se escoge una opcion al azar.")]
    public float SegundosParaElegir = 5f;

    [Tooltip("Opcional: etiqueta donde se muestra la cuenta atr�s. Puede quedar vac�a.")]
    public TextMeshProUGUI TextoCuentaAtras;

    bool puedeElegir;

    [Header("Texto de la respuesta (durante el puente)")]
    [Tooltip("Texto que sale al elegir la opcion BUENA (flecha izquierda).")]
    [TextArea] public string TextoOpcionBuena = "accion buena";

    [Tooltip("Texto que sale al elegir la opcion MALA (flecha derecha).")]
    [TextArea] public string TextoOpcionMala = "accion mala";

    [Tooltip("Etiqueta donde se escribe el texto de la respuesta.")]
    public TextMeshProUGUI TextoRespuestaUI;

    [Tooltip("Objeto que se activa mientras se muestra la respuesta. Se apaga cuando el jugador frena en el RushStopTrigger.")]
    public GameObject PanelRespuesta;

    [Header("Referencias")]
    [Tooltip("Si se deja vacio se busca el SegmentGenerator de este mismo GameObject.")]
    public SegmentGenerator Generador;

    [Tooltip("Aceleron del jugador durante el puente musical. Opcional: si se deja vacio no hay aceleron.")]
    public TransitionRush Aceleron;

    [Tooltip("Apaga el sistema de notas mientras dura el puente. Opcional: si se deja vacio no se toca.")]
    public RhythmSystemToggle SistemaNotas;

    [Tooltip("Lleva la cuenta de decisiones y remata la partida. Opcional: si se deja vacio el juego no termina nunca.")]
    public GameEndManager Final;

    [Tooltip("Efecto de camara. Se usa para el puente corto de cuando la respuesta NO cambia el terreno.")]
    public DollyZoomEffect Camara;

    [Header("Puente cuando la respuesta NO cambia el terreno")]
    [Tooltip("Segundos que dura el efecto de camara antes de que vuelvan las notas. Es el equivalente al trayecto hasta el segmento de transicion, pero por tiempo.")]
    public float SegundosPuenteSinTransicion = 2f;

    [Tooltip("Si el texto de la respuesta sale tambien en este caso. Aqui si puede salir porque el puente tiene una duracion fija que lo termina.")]
    public bool MostrarTextoSinTransicion = true;

    float tiempoRestante;
    bool preguntaActiva;

    /// <summary>True mientras la pregunta esta en pantalla esperando respuesta.</summary>
    public bool PreguntaActiva { get { return preguntaActiva; } }

    /// <summary>Segundos que le quedan al jugador para responder.</summary>
    public float TiempoRestante { get { return tiempoRestante; } }

    void Awake()
    {
        if (Generador == null)
            Generador = GetComponent<SegmentGenerator>();

        // El texto de la respuesta se quita justo cuando el aceleron empieza a frenar,
        // o sea al cruzar el RushStopTrigger del segmento de transicion.
        if (Aceleron != null)
            Aceleron.AlDetenerse += OcultarRespuesta;

        OcultarSecuenciaTelefono();
        OcultarRespuesta();
    }

    void OnEnable()
    {
        if (dialogo != null)
            dialogo.OnDialogueFinished += HabilitarEleccion;
    }

    void OnDisable()
    {
        if (dialogo != null)
            dialogo.OnDialogueFinished -= HabilitarEleccion;
    }

    void HabilitarEleccion()
    {
        TextoCuentaAtras.gameObject.SetActive(true);
        puedeElegir = true;
        tiempoRestante = SegundosParaElegir;
        RefrescarCuentaAtras();
    }

    void OnDestroy()
    {
        if (Aceleron != null)
            Aceleron.AlDetenerse -= OcultarRespuesta;
    }

    /// <summary>
    /// Muestra la pregunta en pantalla y arranca la cuenta atras.
    /// La lanza TeclaPregunta (P por defecto), pero sigue siendo publica para poder
    /// llamarla desde otro script, un UnityEvent o un boton de UI.
    /// Si ya hay una pregunta en pantalla se ignora.
    /// </summary>
    [ContextMenu("Lanzar pregunta")]
    public void LanzarPregunta()
    {
        if (preguntaActiva)
            return;

        // La pregunta que vendria despues de la ultima decision no llega a salir:
        // en su lugar arranca el final. Da igual quien la pida, la tecla o el
        // Transitions.NextTransition() del final de seccion musical.
        if (Final != null && Final.IntentarTerminar())
            return;

        preguntaActiva = true;

        puedeElegir = false;

        tiempoRestante = SegundosParaElegir;
        if (TextoCuentaAtras != null)
            TextoCuentaAtras.text = "";

        // Por si quedara visible el texto del puente anterior
        OcultarRespuesta();

        // El ritmo se para mientras se decide. Lo vuelve a encender el
        // RhythmResumeTrigger del segmento de transicion, o el propio Resolver()
        // si la respuesta no cambia el terreno y no va a haber transicion.
        if (SistemaNotas != null)
            SistemaNotas.Desactivar();

        RefrescarCuentaAtras();

        if (PanelPregunta != null)
            PanelPregunta.SetActive(true);

        if (phone != null) phone.PlayShake();
    }

    void Update()
    {
        if (!preguntaActiva) return;

        if (!puedeElegir) return;

        Opcion elegida;
        if (LeerTecla(out elegida))
        {
            Resolver(elegida, false);
            return;
        }

        tiempoRestante -= Time.deltaTime;
        RefrescarCuentaAtras();

        if (tiempoRestante <= 0f)
        {
            // Se acabo el tiempo: elegimos al azar entre las dos opciones
            Opcion azar = Random.value < 0.5f ? Opcion.Buena : Opcion.Mala;
            Resolver(azar, true);
        }
    }

    bool LeerTecla(out Opcion elegida)
    {
        elegida = Opcion.Buena;

        Keyboard teclado = Keyboard.current;
        if (teclado == null)
            return false;

        if (leftDecision.action.WasPressedThisFrame())
        {
            elegida = Opcion.Buena;
            return true;
        }

        if (rightDecision.action.WasPressedThisFrame())
        {
            elegida = Opcion.Mala;
            return true;
        }

        return false;
    }

    void Resolver(Opcion elegida, bool porTiempo)
    {
        preguntaActiva = false;
        OcultarSecuenciaTelefono();

        if (Final != null)
            Final.RegistrarDecision(elegida);

        // [CAMBIO: el final vuelve a saltar al contestar]
        // Antes esto solo apuntaba la decision y la partida seguia: el final llegaba en
        // la pregunta SIGUIENTE, o sea que el jugador se jugaba entera una seccion
        // musical mas despues de la ultima respuesta. Ahora, en cuanto se contesta la
        // decision numero DecisionesParaFinal, se va directo al fundido en blanco.
        //
        // El return corta aqui a proposito: nos saltamos el cambio de terreno, el
        // aceleron y el puente, que no tendrian a donde llevar. La pregunta ya se ha
        // ocultado arriba, en OcultarSecuenciaTelefono().
        if (Final != null && Final.IntentarTerminar())
            return;

        // Buena tiende a Narrow, Mala tiende a Wide.
        // Si el generador ya esta en ese tipo no hace nada y sigue su ritmo normal;
        // si no, mete la transicion al instante y cambia el tipo a partir de ahi.
        TipoSegmento objetivo = elegida == Opcion.Buena ? TipoSegmento.Narrow : TipoSegmento.Wide;

        bool hayTransicion = false;
        if (Generador != null)
            hayTransicion = Generador.CambiarTipoDeSegmento(objetivo);
        else
            Debug.LogError("DecisionManager: no hay SegmentGenerator asignado.", this);

        if (hayTransicion)
        {
            // Solo hay puente si el terreno cambia de verdad. Si se sigue fabricando
            // el mismo tipo no hay nada a lo que llegar, asi que ni aceleron ni pausa.
            MostrarRespuesta(elegida);

            if (Aceleron != null)
                Aceleron.Lanzar();
        }
        else
        {
            // Sin segmento de transicion no hay ningun RhythmResumeTrigger que encienda
            // las notas, asi que el puente lo marca un temporizador: mismo efecto de
            // camara, pero sin aceleron ni lineas (esas dos leen la velocidad, que aqui
            // no cambia), y las notas esperan a que termine.
            StartCoroutine(PuenteSinTransicion(elegida));
        }

        Debug.Log("Decision: " + elegida + (porTiempo ? " (al azar, se acabo el tiempo)" : "")
            + " -> terreno " + objetivo + (hayTransicion ? " (con transicion)" : " (sin cambio)"));
    }

    void RefrescarCuentaAtras()
    {
        if (TextoCuentaAtras == null)
            return;

        float t = tiempoRestante < 0f ? 0f : tiempoRestante;
        TextoCuentaAtras.text = t.ToString("0.0");
    }


    /// <summary>
    /// Saca el texto del puente segun lo elegido. Se queda en pantalla hasta que el
    /// aceleron empieza a frenar, que es cuando el jugador cruza el RushStopTrigger.
    /// </summary>
    void MostrarRespuesta(Opcion elegida)
    {
        if (TextoRespuestaUI != null)
            TextoRespuestaUI.text = elegida == Opcion.Buena ? TextoOpcionBuena : TextoOpcionMala;

        if (PanelRespuesta != null)
            PanelRespuesta.SetActive(true);
    }
    void OcultarSecuenciaTelefono()
    {
        if (PanelPregunta != null) PanelPregunta.SetActive(false);
        if (phone != null) phone.Hide();
        if (bubbleText != null) bubbleText.Hide();
        if (options != null) options.HideOptions();
        TextoCuentaAtras.gameObject.SetActive(false);


    }

    void OcultarRespuesta()
    {
        if (PanelRespuesta != null)
            PanelRespuesta.SetActive(false);
    }

    /// <summary>
    /// Puente para la respuesta que no genera segmento de transicion. Hace el mismo
    /// dolly zoom que el puente largo, pero durando SegundosPuenteSinTransicion en vez
    /// de hasta cruzar un trigger, y sin tocar la velocidad del jugador: por eso no
    /// salen ni el aceleron ni las lineas, que se alimentan de la velocidad.
    /// Las notas no vuelven hasta que termina.
    /// </summary>
    System.Collections.IEnumerator PuenteSinTransicion(Opcion elegida)
    {
        if (MostrarTextoSinTransicion)
            MostrarRespuesta(elegida);

        if (Camara != null)
            Camara.LanzarManual(SegundosPuenteSinTransicion);

        yield return new WaitForSeconds(SegundosPuenteSinTransicion);

        OcultarRespuesta();

        if (SistemaNotas != null)
            SistemaNotas.Activar();
    }
}
