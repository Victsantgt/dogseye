using UnityEngine;

public class ColliderNoteScript : MonoBehaviour
{
    public NotePool pool;
    public MenuPause pauseMenu;

    private Note blueNote;
    private Note yellowNote;
    private Note pinkNote;
    private Note greenNote;

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        if (other.CompareTag("NoteBlue"))
            blueNote = note;
        else if (other.CompareTag("NoteYellow"))
            yellowNote = note;
        else if (other.CompareTag("NotePink"))
            pinkNote = note;
        else if (other.CompareTag("NoteGreen"))
            greenNote = note;
    }

    private void OnTriggerExit(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        if (note == blueNote) blueNote = null;
        else if (note == yellowNote) yellowNote = null;
        else if (note == pinkNote) pinkNote = null;
        else if (note == greenNote) greenNote = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown("a") && blueNote != null && !pauseMenu.musicPaused)
            Hit(ref blueNote);

        if (Input.GetKeyDown("s") && yellowNote != null && !pauseMenu.musicPaused)
            Hit(ref yellowNote);

        if (Input.GetKeyDown("k") && pinkNote != null && !pauseMenu.musicPaused)
            Hit(ref pinkNote);

        if (Input.GetKeyDown("l") && greenNote != null && !pauseMenu.musicPaused)
            Hit(ref greenNote);
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
