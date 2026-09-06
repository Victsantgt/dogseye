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
        // El carril del medio siempre vale, en Narrow y en Wide.
        if (noteMiddle.action.WasPressedThisFrame() && middleNote != null)
            Hit(ref middleNote);

        // [ANADIDO: carriles laterales] En Narrow la A y la D no aciertan nada. Con las
        // notas laterales ya cortadas en el NoteSpawner esto casi nunca llega a hacer
        // falta, pero cubre el caso de que quedara una nota lateral en vuelo al cambiar
        // de terreno. Sin el componente en la escena devuelve siempre true.
        if (!CarrilesLaterales.LateralesActivos) return;

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
