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

    [Header("Texto")]
    [Tooltip("Texto que aparece en pantalla al lanzar la pregunta.")]
    [TextArea] public string TextoPregunta = "accion buena, o mala?";

    [Tooltip("Etiqueta donde se escribe TextoPregunta.")]
    public TextMeshProUGUI TextoUI;

    [Tooltip("Objeto que se activa y desactiva al mostrar u ocultar la pregunta.")]
    public GameObject PanelPregunta;

    [Tooltip("Opcional: etiqueta donde se muestra la cuenta atras. Puede quedar vacia.")]
    public TextMeshProUGUI TextoCuentaAtras;

    [Header("Cuando aparece")]
    [Tooltip("Tecla que lanza la pregunta. Solo se escucha cuando no hay ninguna en pantalla.")]
    public Key TeclaPregunta = Key.P;

    [Header("Tiempo para responder")]
    [Tooltip("Segundos que tiene el jugador para elegir. Al llegar a 0 se escoge una opcion al azar.")]
    public float SegundosParaElegir = 5f;

    [Header("Referencias")]
    [Tooltip("Si se deja vacio se busca el SegmentGenerator de este mismo GameObject.")]
    public SegmentGenerator Generador;

    [Tooltip("Aceleron del jugador durante el puente musical. Opcional: si se deja vacio no hay aceleron.")]
    public TransitionRush Aceleron;

    [Tooltip("Apaga el sistema de notas mientras dura el puente. Opcional: si se deja vacio no se toca.")]
    public RhythmSystemToggle SistemaNotas;

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

        Ocultar();
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

        preguntaActiva = true;
        tiempoRestante = SegundosParaElegir;

        if (TextoUI != null)
            TextoUI.text = TextoPregunta;

        if (PanelPregunta != null)
            PanelPregunta.SetActive(true);

        // El ritmo se para mientras se decide. Lo vuelve a encender el
        // RhythmResumeTrigger del segmento de transicion, o el propio Resolver()
        // si la respuesta no cambia el terreno y no va a haber transicion.
        if (SistemaNotas != null)
            SistemaNotas.Desactivar();

        RefrescarCuentaAtras();
    }

    void Update()
    {
        if (!preguntaActiva)
        {
            // Fuera de pregunta lo unico que escuchamos es la tecla que la lanza.
            Keyboard tecladoLibre = Keyboard.current;
            if (tecladoLibre != null && tecladoLibre[TeclaPregunta].wasPressedThisFrame)
                LanzarPregunta();

            return;
        }

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

        if (teclado.leftArrowKey.wasPressedThisFrame)
        {
            elegida = Opcion.Buena;
            return true;
        }

        if (teclado.rightArrowKey.wasPressedThisFrame)
        {
            elegida = Opcion.Mala;
            return true;
        }

        return false;
    }

    void Resolver(Opcion elegida, bool porTiempo)
    {
        preguntaActiva = false;
        Ocultar();

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
            if (Aceleron != null)
                Aceleron.Lanzar();
        }
        else
        {
            // Sin segmento de transicion no habra ningun RhythmResumeTrigger que
            // encienda las notas, asi que las devolvemos aqui mismo.
            if (SistemaNotas != null)
                SistemaNotas.Activar();
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

    void Ocultar()
    {
        if (PanelPregunta != null)
            PanelPregunta.SetActive(false);
    }
}
