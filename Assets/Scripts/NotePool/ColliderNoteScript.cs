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

    // [ANADIDO: nota central] Esta caja va de -5 a 0 respecto al jugador, o sea por
    // detras de el. Vale para las laterales, que van desplazadas a los lados, pero la
    // central llega de frente y tendria que atravesar el modelo antes de poder pulsarse.
    // Por eso el carril del medio lo vigila una caja aparte, colocada por delante, que
    // avisa por RegistrarMedio/SoltarMedio. Desmarca esto si algun dia se quiere volver
    // a detectar el medio desde aqui.
    [Header("Nota central")]
    [Tooltip("Deja el carril del medio en manos del DetectorNotaCentral, que va por delante del jugador.")]
    public bool ElMedioLoLlevaOtroCollider = true;

    // [ANADIDO: antispam] Antes, pulsar un carril sin nota no costaba NADA: la
    // pulsacion se descartaba en silencio. Con eso, machacar las teclas era
    // estrictamente mejor que llevar el ritmo, porque cubrias toda la ventana sin
    // arriesgar nada. Ahora una pulsacion al aire cuenta como fallo propio (HitResult
    // .Vacio), y ademas cada carril se bloquea un rato tras cada pulsacion para que la
    // penalizacion no se acumule quince veces por segundo.
    [Header("Antispam")]
    [Tooltip("Pulsar un carril sin nota cuenta como fallo. Es lo que hace que machacar las teclas deje de compensar.")]
    public bool PenalizarPulsacionEnVacio = true;

    [Tooltip("Segundos que cada carril ignora nuevas pulsaciones despues de una. Evita que un doble toque nervioso cobre dos veces. Cada carril lleva su propio bloqueo.")]
    public float SegundosDeBloqueo = 0.15f;

    [Tooltip("A quien se le avisa de la pulsacion en vacio. Es el mismo NoteHitSubject que usan las notas; si se deja vacio no se penaliza.")]
    public NoteHitSubject subject;

    [Tooltip("Deja rastro en consola de cada pulsacion al aire. Util para ajustar la penalizacion; desmarcalo despues.")]
    public bool LogAlPenalizar = false;

    float bloqueoMedio;
    float bloqueoIzquierda;
    float bloqueoDerecha;

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) return;

        if (!ElMedioLoLlevaOtroCollider && other.CompareTag("NoteMiddle"))
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

    /// <summary>
    /// [ANADIDO: nota central] Lo llama el DetectorNotaCentral cuando una nota del medio
    /// entra en su caja. La pulsacion la sigue leyendo este script, que es el unico que
    /// tiene las acciones de input.
    /// </summary>
    public void RegistrarMedio(Note nota)
    {
        middleNote = nota;
    }

    /// <summary>La nota del medio ha salido de la caja sin que nadie la pulse.</summary>
    public void SoltarMedio(Note nota)
    {
        if (middleNote == nota) middleNote = null;
    }

    private void Update()
    {
        // [ANADIDO: antispam] Los bloqueos se descuentan SIEMPRE, antes de cualquier
        // return, para que un carril no se quede bloqueado al entrar en un Narrow.
        float dt = Time.deltaTime;
        bloqueoMedio -= dt;
        bloqueoIzquierda -= dt;
        bloqueoDerecha -= dt;

        // El carril del medio siempre vale, en Narrow y en Wide.
        if (Pulsada(noteMiddle) && bloqueoMedio <= 0f)
        {
            bloqueoMedio = SegundosDeBloqueo;
            if (middleNote != null) Hit(ref middleNote);
            else Penalizar("Middle");
        }

        // [ANADIDO: carriles laterales] En Narrow la A y la D no aciertan nada. Con las
        // notas laterales ya cortadas en el NoteSpawner esto casi nunca llega a hacer
        // falta, pero cubre el caso de que quedara una nota lateral en vuelo al cambiar
        // de terreno. Sin el componente en la escena devuelve siempre true.
        //
        // Ojo: aqui tambien se corta la penalizacion, y es lo correcto. En Narrow los
        // laterales estan deshabilitados a proposito, asi que pulsarlos no es un fallo
        // del jugador y no debe quitarle vida.
        if (!CarrilesLaterales.LateralesActivos) return;

        if (Pulsada(noteLeft) && bloqueoIzquierda <= 0f)
        {
            bloqueoIzquierda = SegundosDeBloqueo;
            if (leftNote != null) Hit(ref leftNote);
            else Penalizar("Left");
        }

        if (Pulsada(noteRight) && bloqueoDerecha <= 0f)
        {
            bloqueoDerecha = SegundosDeBloqueo;
            if (rightNote != null) Hit(ref rightNote);
            else Penalizar("Right");
        }
    }

    static bool Pulsada(InputActionReference referencia)
    {
        return referencia != null && referencia.action != null && referencia.action.WasPressedThisFrame();
    }

    /// <summary>
    /// [ANADIDO: antispam] Se ha pulsado un carril sin nota dentro. Se avisa al mismo
    /// observer que las notas, con su propio resultado para poder distinguirlo de un
    /// Miss de verdad y penalizarlo mas barato.
    /// </summary>
    private void Penalizar(string carril)
    {
        if (!PenalizarPulsacionEnVacio || subject == null) return;

        NoteHitInfo info = new NoteHitInfo { lane = carril, result = HitResult.Vacio };
        subject.NotifyObservers(info);

        if (LogAlPenalizar)
            Debug.Log("ColliderNoteScript: pulsacion al aire en '" + carril + "'", this);
    }

    private void Hit(ref Note note)
    {
        note.OnPlayerHit();
        note = null;
    }
}
