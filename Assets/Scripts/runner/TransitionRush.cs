using System.Collections;
using UnityEngine;

/// <summary>
/// Aceleron temporal del jugador para el puente musical entre secciones.
/// Se lanza al resolverse la pregunta del DecisionManager: recorta la distancia
/// que queda hasta el siguiente segmento y ademas se lee como algo intencionado
/// en vez de como una espera muerta.
///
/// Va en el mismo GameObject que BasicMovement (el Player).
/// </summary>
public class TransitionRush : MonoBehaviour
{
    [Tooltip("Cuanto se multiplica la velocidad en el pico del aceleron. 1 = sin efecto.")]
    public float MultiplicadorMaximo = 2.5f;

    [Tooltip("Duracion total del aceleron en segundos. Ajustalo al largo del puente musical.")]
    public float Duracion = 3f;

    [Tooltip("Forma del aceleron a lo largo de la Duracion. Eje Y: 0 = velocidad normal, 1 = MultiplicadorMaximo.")]
    public AnimationCurve Curva = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.25f, 1f),
        new Keyframe(0.75f, 1f),
        new Keyframe(1f, 0f));

    [Tooltip("Si se deja vacio se busca el BasicMovement de este mismo GameObject.")]
    public BasicMovement Movimiento;

    Coroutine rutina;

    void Awake()
    {
        if (Movimiento == null)
            Movimiento = GetComponent<BasicMovement>();
    }

    /// <summary>
    /// Arranca el aceleron. Si ya habia uno en marcha lo reinicia.
    /// </summary>
    [ContextMenu("Lanzar aceleron")]
    public void Lanzar()
    {
        if (Movimiento == null)
        {
            Debug.LogError("TransitionRush: no hay BasicMovement asignado.", this);
            return;
        }

        if (rutina != null)
            StopCoroutine(rutina);

        rutina = StartCoroutine(Acelerar());
    }

    /// <summary>Corta el aceleron y devuelve la velocidad a la normal.</summary>
    public void Cancelar()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }

        if (Movimiento != null)
            Movimiento.SetMultiplicador(1f);
    }

    IEnumerator Acelerar()
    {
        float t = 0f;

        while (t < Duracion)
        {
            t += Time.deltaTime;
            float progreso = Duracion <= 0f ? 1f : Mathf.Clamp01(t / Duracion);
            float mezcla = Curva.Evaluate(progreso);
            Movimiento.SetMultiplicador(Mathf.LerpUnclamped(1f, MultiplicadorMaximo, mezcla));
            yield return null;
        }

        Movimiento.SetMultiplicador(1f);
        rutina = null;
    }
}
