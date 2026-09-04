using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public NotePool pool;
    public Transform laneMiddle;
    public Transform laneMiddleDestiny;
    public Transform laneMiddlePerfect;
    public Transform laneLeft;
    public Transform laneLeftDestiny;
    public Transform laneLeftPerfect;
    public Transform laneRight;
    public Transform laneRightDestiny;
    public Transform laneRightPerfect;

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
                note.perfectMark = laneMiddlePerfect;
                break;
            case "Left":
                note.transform.position = laneLeft.position;
                note.destiny = laneLeftDestiny;
                note.perfectMark = laneLeftPerfect;
                break;
            case "Right":
                note.transform.position = laneRight.position;
                note.destiny = laneRightDestiny;
                note.perfectMark = laneRightPerfect;
                break;
        }
        note.StartMovement();
    }
}
