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
    [Tooltip("Termina con el final minimalista.")]
    public Key TeclaMinimalista = Key.G;

    [Tooltip("Termina con el final mixto.")]
    public Key TeclaMixto = Key.H;

    [Tooltip("Termina con el final consumista.")]
    public Key TeclaConsumista = Key.J;

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

        if (teclado[TeclaMinimalista].wasPressedThisFrame) Minimalista();
        if (teclado[TeclaMixto].wasPressedThisFrame) Mixto();
        if (teclado[TeclaConsumista].wasPressedThisFrame) Consumista();
    }

    [ContextMenu("Terminar con final MINIMALISTA")]
    public void Minimalista() { Lanzar(GameEndManager.TipoDeFinal.Minimalista); }

    [ContextMenu("Terminar con final MIXTO")]
    public void Mixto() { Lanzar(GameEndManager.TipoDeFinal.Mixto); }

    [ContextMenu("Terminar con final CONSUMISTA")]
    public void Consumista() { Lanzar(GameEndManager.TipoDeFinal.Consumista); }

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
