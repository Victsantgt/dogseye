using UnityEngine;

/// <summary>
/// [ANADIDO: nota central] Segunda zona de deteccion, solo para el carril del medio.
///
/// POR QUE VA SEPARADA. El ColliderNotas original va de -5 a 0 respecto al jugador, o
/// sea ENTERO POR DETRAS de el. Para las laterales eso funciona porque van desplazadas a
/// los lados y nunca se le meten dentro. La central llega de frente, asi que con esa
/// caja tenia que alcanzarle y atravesarle el modelo antes de poder pulsarla.
///
/// Esta caja va por delante (por defecto de +2 a +8, con la marca de acierto en +5), de
/// forma que el duelo del medio se resuelve delante del personaje y no encima.
///
/// No repite la logica de input: cuando entra o sale una nota del medio se lo cuenta al
/// ColliderNoteScript de siempre, que sigue siendo el unico que lee las teclas. Asi no
/// hay dos sitios leyendo el mismo boton.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class DetectorNotaCentral : MonoBehaviour
{
    [Tooltip("Quien lee las teclas y resuelve el acierto. Si se deja vacio se busca en la escena.")]
    public ColliderNoteScript Notas;

    [Tooltip("Tag de las notas del carril del medio.")]
    public string TagDeLaNota = "NoteMiddle";

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (Notas == null)
            Notas = Object.FindFirstObjectByType<ColliderNoteScript>(FindObjectsInactive.Include);

        if (Notas == null)
            Debug.LogError("DetectorNotaCentral: no hay ningun ColliderNoteScript al que avisar.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (Notas == null || !other.CompareTag(TagDeLaNota)) return;

        Note nota = other.GetComponent<Note>();
        if (nota != null) Notas.RegistrarMedio(nota);
    }

    void OnTriggerExit(Collider other)
    {
        if (Notas == null || !other.CompareTag(TagDeLaNota)) return;

        Note nota = other.GetComponent<Note>();
        if (nota != null) Notas.SoltarMedio(nota);
    }
}
