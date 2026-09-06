using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// [ANADIDO: finales de prueba] TEMPORAL, PARA DEPURAR.
///
/// Termina la partida al instante con el final que se pida, igual que si se acabara de
/// contestar la ultima pregunta. Sirve para revisar las laminas y sus textos sin
/// jugarse los tres tramos cada vez.
///
///   G  ->  final minimalista   (todas las decisiones de un lado)
///   H  ->  final mixto         (mezcladas)
///   J  ->  final consumista    (todas del otro lado)
///
/// NO ES UN ATAJO QUE SE SALTE NADA. Llama a GameEndManager.TerminarConFinal(), que
/// coloca las decisiones y deja decidir a la misma logica de siempre. Lo que se ve
/// probando es lo que va a pasar en una partida de verdad.
///
/// ===========================================================================
/// COMO QUITARLO DE LA BUILD
/// ===========================================================================
/// Basta con BORRAR ESTE COMPONENTE de LevelController. Nada mas depende de el: el
/// GameEndManager no lo conoce ni lo busca.
///
/// Si ademas quieres que no quede ni rastro, borra este archivo y el bloque marcado
/// como [ANADIDO: finales de prueba] dentro de GameEndManager.cs, que es lo unico que
/// se anadio alli para esto.
///
/// Mientras tanto, desmarcar UsarTeclasDePrueba ya lo deja inerte sin borrar nada.
/// ===========================================================================
/// </summary>
[DisallowMultipleComponent]
public class FinalesDePrueba : MonoBehaviour
{
    [Header("TEMPORAL: solo para depurar")]
    [Tooltip("Desmarcalo para dejar las teclas inertes sin borrar el componente. Acuerdate de quitarlo antes de una build final.")]
    public bool UsarTeclasDePrueba = true;

    [Tooltip("Quien termina la partida. Si se deja vacio se busca en este mismo GameObject.")]
    public GameEndManager Final;

    [Header("Teclas")]
    [Tooltip("Termina con el final consumista.")]
    public Key TeclaBueno = Key.G;

    [Tooltip("Termina con el final mixto.")]
    public Key TeclaNeutral = Key.H;

    [Tooltip("Termina con el final minimalista.")]
    public Key TeclaMalo = Key.J;

    void Awake()
    {
        if (Final == null)
            Final = GetComponent<GameEndManager>();
    }

    void Update()
    {
        if (!UsarTeclasDePrueba) return;

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (teclado[TeclaBueno].wasPressedThisFrame) Bueno();
        if (teclado[TeclaNeutral].wasPressedThisFrame) Neutral();
        if (teclado[TeclaMalo].wasPressedThisFrame) Malo();
    }
    [ContextMenu("Terminar con final Bueno (Todo Izquierda)")]
    public void Bueno() { Lanzar(GameEndManager.TipoDeFinal.Bueno); }

    [ContextMenu("Terminar con final Neutral")]
    public void Neutral() { Lanzar(GameEndManager.TipoDeFinal.Neutral); }

    [ContextMenu("Terminar con final Malo (Todo Derecha)")]
    public void Malo() { Lanzar(GameEndManager.TipoDeFinal.Malo); }

    void Lanzar(GameEndManager.TipoDeFinal cual)
    {
        if (Final == null)
        {
            Debug.LogError("FinalesDePrueba: no hay GameEndManager asignado.", this);
            return;
        }

        Final.TerminarConFinal(cual);
    }
}
