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
        if (noteMiddle.action.WasPressedThisFrame() && middleNote != null)
            Hit(ref middleNote);
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
