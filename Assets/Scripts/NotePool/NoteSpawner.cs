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

    //Para el patr�n observer
    public NoteHitSubject subject;

    // [ANADIDO: nota central] Hacen falta para calcular cuanto tiene que tardar la nota
    // del medio en llegar. Si se dejan vacios se buscan solos.
    [Header("Nota central")]
    [Tooltip("El Player. Si se deja vacio se busca por el tag Player.")]
    public Transform Jugador;

    [Tooltip("De donde se lee PlayerSpeed. Si se deja vacio se busca en el Player.")]
    public BasicMovement Movimiento;

    void Awake()
    {
        if (Jugador == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) Jugador = p.transform;
        }

        if (Movimiento == null && Jugador != null)
            Movimiento = Jugador.GetComponent<BasicMovement>();
    }

    /// <summary>
    /// Cuanto tarda una nota lateral desde que sale hasta que llega a su marca de
    /// acierto. Como las laterales estan quietas, ese tiempo es solo su distancia
    /// dividida entre la velocidad del jugador, que es quien las alcanza.
    ///
    /// Se calcula de la geometria en vez de escribirlo a mano para que mover un carril o
    /// el collider no descoloque a la central en silencio.
    /// </summary>
    public float SegundosDeVueloLateral()
    {
        float velocidad = Movimiento != null ? Movimiento.PlayerSpeed : 24f;
        if (velocidad <= 0f) velocidad = 24f;

        if (laneLeft == null || laneLeftPerfect == null)
        {
            Debug.LogWarning("NoteSpawner: faltan los carriles laterales, la nota central usa 3 s por defecto.", this);
            return 3f;
        }

        float recorrido = laneLeft.position.z - laneLeftPerfect.position.z;
        return Mathf.Max(0.01f, recorrido / velocidad);
    }

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

                // [ANADIDO: nota central] La central sale mucho mas lejos que las
                // laterales para no aparecer de la nada dentro del campo de vision, asi
                // que no puede quedarse quieta como ellas: llegaria tarde. Se le dice
                // que se acerque frenando y que tarde EXACTAMENTE lo mismo que gasta una
                // lateral en su recorrido, para que las dos caigan en el mismo momento
                // del compas.
                MovimientoNotaCentral movimiento = note.GetComponent<MovimientoNotaCentral>();
                if (movimiento != null)
                    movimiento.Lanzar(Jugador, laneMiddlePerfect, SegundosDeVueloLateral());
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
