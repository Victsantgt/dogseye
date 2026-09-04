using System.Collections;
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
    [Tooltip("Segundos de gameplay antes de lanzar la primera pregunta.")]
    public float SegundosHastaPrimeraPregunta = 20f;

    [Tooltip("Desactivalo si prefieres lanzar la pregunta solo a mano con LanzarPregunta().")]
    public bool LanzarPrimeraAutomaticamente = true;

    [Tooltip("Si esta activo la pregunta vuelve a salir sola cada SegundosEntrePreguntas.")]
    public bool RepetirAutomaticamente = false;

    [Tooltip("Segundos entre una pregunta y la siguiente cuando RepetirAutomaticamente esta activo.")]
    public float SegundosEntrePreguntas = 30f;

    [Header("Tiempo para responder")]
    [Tooltip("Segundos que tiene el jugador para elegir. Al llegar a 0 se escoge una opcion al azar.")]
    public float SegundosParaElegir = 5f;

    [Header("Referencias")]
    [Tooltip("Si se deja vacio se busca el SegmentGenerator de este mismo GameObject.")]
    public SegmentGenerator Generador;

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

    void Start()
    {
        if (LanzarPrimeraAutomaticamente)
            StartCoroutine(CicloAutomatico());
    }

    IEnumerator CicloAutomatico()
    {
        yield return new WaitForSeconds(SegundosHastaPrimeraPregunta);
        LanzarPregunta();

        while (RepetirAutomaticamente)
        {
            // esperamos a que se resuelva la pregunta actual antes de contar el intervalo
            while (preguntaActiva)
                yield return null;

            yield return new WaitForSeconds(SegundosEntrePreguntas);
            LanzarPregunta();
        }
    }

    /// <summary>
    /// Muestra la pregunta en pantalla y arranca la cuenta atras.
    /// Llamalo desde donde quieras (otro script, un UnityEvent, un boton de UI...)
    /// para decidir tu el momento exacto. Si ya hay una pregunta en pantalla se ignora.
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

        RefrescarCuentaAtras();
    }

    void Update()
    {
        if (!preguntaActiva)
            return;

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

        if (Generador != null)
            Generador.CambiarTipoDeSegmento(objetivo);
        else
            Debug.LogError("DecisionManager: no hay SegmentGenerator asignado.", this);

        Debug.Log("Decision: " + elegida + (porTiempo ? " (al azar, se acabo el tiempo)" : "") + " -> terreno " + objetivo);
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
