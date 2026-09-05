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
    public EnemyManager enemyManager;

    //Para el patr�n observer
    public NoteHitSubject subject; 

    public void Spawn(string position)
    {
        // [ANADIDO: carriles laterales] En un pasillo Narrow no hay carril izquierdo ni
        // derecho, asi que esas notas ni salen del pool. El ChartManager avanza igual su
        // puntero, o sea que la nota se pierde y el chart sigue en hora. Sin el
        // componente CarrilesLaterales en la escena esto devuelve siempre true.
        if (!CarrilesLaterales.Permite(position)) return;

        Note note = pool.GetNote(position);

        //asignamos el subject a cada nota que sale del pool
        note.subject = subject;
        switch (position)
        {
            case "Middle":
                note.transform.position = laneMiddle.position;
                note.perfectMark = laneMiddlePerfect;
                GameObject enemy = enemyManager.Aparecer();
                EnemyRunner enemyRunner = enemy.GetComponent<EnemyRunner>();
                note.enemy = enemyRunner;
                break;
            case "Left":
                note.transform.position = laneLeft.position;
                note.perfectMark = laneLeftPerfect;
                break;
            case "Right":
                note.transform.position = laneRight.position;
                note.perfectMark = laneRightPerfect;
                break;
        }
    }
}
