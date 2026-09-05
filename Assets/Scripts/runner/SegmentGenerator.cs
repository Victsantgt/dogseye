using UnityEngine;

/// <summary>
/// Tipo de terreno que se esta generando ahora mismo.
/// </summary>
public enum TipoSegmento
{
    Narrow,
    Wide
}

public class SegmentGenerator : MonoBehaviour
{
    [Header("Prefabs de segmento")]
    public GameObject SegmentNarrow;
    public GameObject SegmentWide;
    [Tooltip("Transicion que se coloca una sola vez al pasar de Wide a Narrow.")]
    public GameObject SegmentWideToNarrow;
    [Tooltip("Transicion que se coloca una sola vez al pasar de Narrow a Wide.")]
    public GameObject SegmentNarrowToWide;

    [Header("Generacion")]
    [Tooltip("Z donde se coloca el proximo segmento.")]
    [SerializeField] int Zpos = 168;
    [Tooltip("Cambiar segun el largo del suelo de los prefabs.")]
    [SerializeField] int LargoSegmento = 168;
    // Este numero es tambien el colchon en regimen: siempre hay exactamente
    // SegmentosIniciales segmentos por delante del jugador. Cuanto mas alto, mas
    // tarda en llegar el segmento de transicion tras responder la pregunta
    // (a 24 u/s cada segmento son 7 s). Bajarlo solo es seguro si la niebla tapa
    // la distancia a la que aparece el segmento nuevo: SegmentosIniciales * 168.
    [Tooltip("Segmentos que hay siempre por delante del jugador. 2 x 168 = 336 u, tapado por una niebla que acabe antes de esa distancia.")]
    [SerializeField] int SegmentosIniciales = 2;

    [Header("Estado")]
    [Tooltip("Tipo con el que arranca el nivel. Debe coincidir con el StartSegment de la escena.")]
    [SerializeField] TipoSegmento tipoActual = TipoSegmento.Wide;

    /// <summary>Tipo de segmento que se esta generando ahora mismo.</summary>
    public TipoSegmento TipoActual { get { return tipoActual; } }

    void Start()
    {
        // Colchon inicial: varios segmentos seguidos para que el jugador no vea el vacio.
        for (int i = 0; i < SegmentosIniciales; i++)
            ColocarSegmento(PrefabDe(tipoActual));
    }

    /// <summary>
    /// Lo llama el SegmentSpawnTrigger que hay al final de cada segmento cuando el
    /// jugador lo atraviesa. Antes esto lo hacia una corrutina por tiempo.
    /// </summary>
    public void GenerarSiguienteSegmento()
    {
        ColocarSegmento(PrefabDe(tipoActual));
    }

    void ColocarSegmento(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("SegmentGenerator: falta asignar un prefab de segmento en el Inspector.", this);
            return;
        }

        GameObject instancia = Instantiate(prefab, new Vector3(0, 0, Zpos), Quaternion.identity);
        Zpos += LargoSegmento;

        // Le pasamos la referencia al trigger del segmento recien creado
        SegmentSpawnTrigger trigger = instancia.GetComponentInChildren<SegmentSpawnTrigger>(true);
        if (trigger != null)
            trigger.AsignarGenerador(this);
        else
            Debug.LogWarning("SegmentGenerator: el prefab " + prefab.name + " no tiene SegmentSpawnTrigger, la cadena de generacion se cortara aqui.", this);
    }

    GameObject PrefabDe(TipoSegmento tipo)
    {
        return tipo == TipoSegmento.Narrow ? SegmentNarrow : SegmentWide;
    }

    /// <summary>
    /// Punto de entrada para el DecisionManager.
    /// Si el objetivo es el tipo que ya se esta generando no hace nada. Si es distinto,
    /// coloca al instante una unica instancia del segmento de transicion (se encola
    /// delante de los que ya existen) y a partir de ahi todos los segmentos que pida
    /// el trigger son del tipo objetivo.
    /// </summary>
    /// <returns>
    /// true si se ha colocado un segmento de transicion, false si el terreno se queda
    /// como estaba. Lo usa el DecisionManager para saber si hay que lanzar el aceleron
    /// y si hay que esperar a un trigger de transicion.
    /// </returns>
    public bool CambiarTipoDeSegmento(TipoSegmento objetivo)
    {
        if (objetivo == tipoActual)
            return false;

        GameObject transicion = objetivo == TipoSegmento.Narrow ? SegmentWideToNarrow : SegmentNarrowToWide;
        if (transicion == null)
        {
            Debug.LogError("SegmentGenerator: falta el prefab de transicion hacia " + objetivo, this);
            return false;
        }

        tipoActual = objetivo;
        ColocarSegmento(transicion);
        return true;
    }
}
