using UnityEngine;

/// <summary>
/// Trigger invisible que llevan SOLO los prefabs de transicion.
/// Va colocado un poco ANTES de la entrada del segmento (sobresale hacia atras,
/// sobre el suelo del segmento anterior), de manera que el jugador lo cruza justo
/// antes de llegar a la transicion y el aceleron frena a tiempo en vez de durar
/// un numero fijo de segundos.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RushStopTrigger : MonoBehaviour
{
    [Tooltip("Tag del jugador. El prefab Character debe tener este tag asignado.")]
    public string PlayerTag = "Player";

    bool disparado;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (disparado) return;
        if (!EsJugador(other)) return;

        disparado = true;

        // El TransitionRush vive en el propio jugador, asi que lo sacamos de el
        // en vez de buscar por toda la escena.
        TransitionRush rush = other.transform.root.GetComponentInChildren<TransitionRush>(true);
        if (rush == null)
            rush = FindFirstObjectByType<TransitionRush>();

        if (rush != null)
            rush.Detener();
        else
            Debug.LogError("RushStopTrigger: no se ha encontrado ningun TransitionRush.", this);
    }

    bool EsJugador(Collider other)
    {
        if (other.CompareTag(PlayerTag)) return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(PlayerTag);
    }
}
