using UnityEngine;

/// <summary>
/// Trigger invisible situado al final del segmento.
/// Cuando el jugador lo atraviesa le pide al SegmentGenerator que cree el siguiente
/// segmento. Sustituye a la generacion por tiempo: el terreno avanza al ritmo real
/// del jugador, no al del reloj.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SegmentSpawnTrigger : MonoBehaviour
{
    [Tooltip("Tag del jugador. El prefab Character debe tener este tag asignado.")]
    public string PlayerTag = "Player";

    SegmentGenerator generador;
    bool disparado;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        // Los segmentos que crea el generador reciben la referencia por AsignarGenerador().
        // Este Find es solo para el segmento inicial que ya esta colocado a mano en la escena.
        if (generador == null)
            generador = FindFirstObjectByType<SegmentGenerator>();
    }

    /// <summary>Inyecta la referencia al crear el segmento, para no depender de un Find.</summary>
    public void AsignarGenerador(SegmentGenerator gen)
    {
        generador = gen;
    }

    void OnTriggerEnter(Collider other)
    {
        if (disparado) return;
        if (!EsJugador(other)) return;

        disparado = true;

        if (generador != null)
            generador.GenerarSiguienteSegmento();
        else
            Debug.LogError("SegmentSpawnTrigger: no se ha encontrado ningun SegmentGenerator en la escena.", this);
    }

    bool EsJugador(Collider other)
    {
        if (other.CompareTag(PlayerTag)) return true;

        // El collider suele estar en un hijo del Character, asi que miramos tambien la raiz.
        Transform root = other.transform.root;
        return root != null && root.CompareTag(PlayerTag);
    }
}
