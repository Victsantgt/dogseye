using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public NotePool pool;

    public Transform laneBlue;
    public Transform laneYellow;
    public Transform lanePink;
    public Transform laneGreen;

    //TINKA
    public Transform laneBlueDestiny;
    public Transform laneYellowDestiny;
    public Transform lanePinkDestiny;
    public Transform laneGreenDestiny;

    //Para el patrón observer
    public NoteHitSubject subject; 

    public void Spawn(string color)
    {
        Note note = pool.GetNote(color);

        //asignamos el subject a cada nota que sale del pool
        note.subject = subject;
        switch (color)
        {
            case "Blue":
                note.transform.position = laneBlue.position;
                note.destiny = laneBlueDestiny;
                break;

            case "Yellow":
                note.transform.position = laneYellow.position;
                note.destiny = laneYellowDestiny;
                break;

            case "Pink":
                note.transform.position = lanePink.position;
                note.destiny = lanePinkDestiny;
                break;

            case "Green":
                note.transform.position = laneGreen.position;
                note.destiny = laneGreenDestiny;    
                break;
        }
        note.StartMovement();
    }
}
