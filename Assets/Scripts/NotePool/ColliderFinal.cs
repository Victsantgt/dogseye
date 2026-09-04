using UnityEngine;

public class ColliderFinal : MonoBehaviour
{
    public NotePool pool;

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        // Devuelve la nota al pool
        //pool.Release(note.noteColor, note);

        //patrón observer
        note.RegisterMiss();

    }
}
