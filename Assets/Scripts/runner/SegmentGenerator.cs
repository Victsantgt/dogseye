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
    [Tooltip("Segmentos que se crean de golpe al arrancar la partida, para que no se vea el vacio delante del jugador.")]
    [SerializeField] int SegmentosIniciales = 3;

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
    public void CambiarTipoDeSegmento(TipoSegmento objetivo)
    {
        if (objetivo == tipoActual)
            return;

        GameObject transicion = objetivo == TipoSegmento.Narrow ? SegmentWideToNarrow : SegmentNarrowToWide;
        if (transicion == null)
        {
            Debug.LogError("SegmentGenerator: falta el prefab de transicion hacia " + objetivo, this);
            return;
        }

        tipoActual = objetivo;
        ColocarSegmento(transicion);
    }
}
