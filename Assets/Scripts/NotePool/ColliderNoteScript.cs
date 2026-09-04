using UnityEngine;

public class ColliderNoteScript : MonoBehaviour
{
    public NotePool pool;

    private Note middleNote;

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        if (other.CompareTag("NoteMiddle"))
            middleNote = note;
    }

    private void OnTriggerExit(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        if (note == middleNote) middleNote = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown("a") && middleNote != null)
            Hit(ref middleNote);
    }

    private void Hit(ref Note note)
    {
        //pool.Release(note.noteColor, note);

        //observer
        //note.RegisterHit();
        Debug.Log("ON PLAYER HIT");
        note.OnPlayerHit();
        note = null;
    }
}
