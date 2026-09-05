using UnityEngine;

/// <summary>
/// Trigger invisible que llevan SOLO los prefabs de transicion
/// (SegmentWideToNarrow y SegmentNarrowToWide).
/// Cuando el jugador entra en el segmento de transicion, vuelve a encender el
/// sistema de notas que el DecisionManager apago al lanzar la pregunta.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RhythmResumeTrigger : MonoBehaviour
{
    [Tooltip("Tag del jugador. El prefab Character debe tener este tag asignado.")]
    public string PlayerTag = "Player";

    RhythmSystemToggle toggle;
    bool disparado;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        // El toggle vive en LevelController, que no se destruye nunca, asi que
        // basta con buscarlo una vez al crearse el segmento.
        toggle = FindFirstObjectByType<RhythmSystemToggle>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (disparado) return;
        if (!EsJugador(other)) return;

        disparado = true;

        if (toggle != null)
            toggle.Activar();
        else
            Debug.LogError("RhythmResumeTrigger: no se ha encontrado ningun RhythmSystemToggle en la escena.", this);
    }

    bool EsJugador(Collider other)
    {
        if (other.CompareTag(PlayerTag)) return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(PlayerTag);
    }
}
