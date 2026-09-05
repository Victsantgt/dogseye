using UnityEngine;
using UnityEngine.InputSystem;

public class ColliderNoteScript : MonoBehaviour
{
    public NotePool pool;

    private Note middleNote;
    private Note leftNote;
    private Note rightNote;

    public InputActionReference noteMiddle;
    public InputActionReference noteLeft;
    public InputActionReference noteRight;

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        if (other.CompareTag("NoteMiddle"))
            middleNote = note;
        if (other.CompareTag("NoteLeft"))
            leftNote = note;
        if (other.CompareTag("NoteRight"))
            rightNote = note;
    }

    private void OnTriggerExit(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        if (note == middleNote) middleNote = null;
        if (note == leftNote) leftNote = null;
        if (note == rightNote) rightNote = null;
    }

    private void Update()
    {
        // El carril del medio siempre vale, en Narrow y en Wide.
        if (noteMiddle.action.WasPressedThisFrame() && middleNote != null)
            Hit(ref middleNote);

        // [ANADIDO: carriles laterales] En Narrow la A y la D no aciertan nada. Con las
        // notas laterales ya cortadas en el NoteSpawner esto casi nunca llega a hacer
        // falta, pero cubre el caso de que quedara una nota lateral en vuelo al cambiar
        // de terreno. Sin el componente en la escena devuelve siempre true.
        if (!CarrilesLaterales.LateralesActivos) return;

        if (noteLeft.action.WasPressedThisFrame() && leftNote != null)
            Hit(ref leftNote);
        if (noteRight.action.WasPressedThisFrame() && rightNote != null)
            Hit(ref rightNote);
    }

    private void Hit(ref Note note)
    {
        note.OnPlayerHit();
        note = null;
    }
}
