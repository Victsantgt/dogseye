using UnityEngine;

/// <summary>
/// Trigger invisible colocado a la entrada del segmento.
/// Cuando el jugador lo atraviesa, programa la destruccion del segmento
/// pasados DespawnSeconds segundos.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SegmentDespawner : MonoBehaviour
{
    [Tooltip("Segundos que tarda el segmento en destruirse despues de que el jugador toque el trigger.")]
    public float DespawnSeconds = 10f;

    [Tooltip("Tag del jugador. El prefab Character debe tener este tag asignado.")]
    public string PlayerTag = "Player";

    [Tooltip("Objeto que se destruye. Si se deja vacio se destruye la raiz del segmento.")]
    public GameObject TargetToDestroy;

    bool triggered = false;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!IsPlayer(other)) return;

        triggered = true;
        GameObject target = TargetToDestroy != null ? TargetToDestroy : transform.root.gameObject;
        Destroy(target, DespawnSeconds);
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag(PlayerTag)) return true;

        // El collider suele estar en un hijo del Character, asi que miramos tambien la raiz.
        Transform root = other.transform.root;
        return root != null && root.CompareTag(PlayerTag);
    }
}
