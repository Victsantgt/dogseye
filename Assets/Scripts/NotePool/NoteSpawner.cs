using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public NotePool pool;
    public Transform laneMiddle;
    public Transform laneMiddleDestiny;

    //Para el patrón observer
    public NoteHitSubject subject; 

    public void Spawn(string position)
    {
        Note note = pool.GetNote(position);

        //asignamos el subject a cada nota que sale del pool
        note.subject = subject;
        switch (position)
        {
            case "Middle":
                note.transform.position = laneMiddle.position;
                note.destiny = laneMiddleDestiny;
                break;
        }
        note.StartMovement();
    }
}
