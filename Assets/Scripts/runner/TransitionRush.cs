using System.Collections;
using UnityEngine;

/// <summary>
/// Aceleron del jugador para el puente entre secciones.
///
/// Ya no dura un tiempo fijo: sube hasta MultiplicadorMaximo, se mantiene ahi todo
/// lo que haga falta, y solo frena cuando el RushStopTrigger del prefab de transicion
/// llama a Detener(). Asi el aceleron dura exactamente lo que tarde el jugador en
/// llegar al segmento de transicion, sea la distancia que sea.
///
/// Va en el mismo GameObject que BasicMovement (el Player).
/// </summary>
// Dos TransitionRush en el mismo objeto se pisan: los dos escriben
// SetMultiplicador() cada frame y Detener() solo afecta a uno, asi que el otro
// mantiene la velocidad alta para siempre. Unity ya no deja anadir un segundo.
[DisallowMultipleComponent]
public class TransitionRush : MonoBehaviour
{
    [Tooltip("Cuanto se multiplica la velocidad mientras dura el aceleron. 1 = sin efecto.")]
    public float MultiplicadorMaximo = 2.5f;

    [Tooltip("Segundos que tarda en alcanzar el multiplicador maximo.")]
    public float TiempoSubida = 0.6f;

    [Tooltip("Segundos que tarda en volver a la velocidad normal cuando se llama a Detener().")]
    public float TiempoBajada = 0.8f;

    [Tooltip("Forma de la subida. Eje Y: 0 = velocidad normal, 1 = MultiplicadorMaximo.")]
    public AnimationCurve CurvaSubida = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Red de seguridad en segundos por si el jugador nunca llega a cruzar el RushStopTrigger. 0 = sin limite.")]
    public float SegundosDeSeguridad = 20f;

    [Tooltip("Si se deja vacio se busca el BasicMovement de este mismo GameObject.")]
    public BasicMovement Movimiento;

    /// <summary>
    /// Se dispara justo cuando empieza el frenado, sea porque el RushStopTrigger ha
    /// llamado a Detener() o porque ha saltado la red de seguridad. Lo usa el
    /// DecisionManager para quitar de pantalla el texto de la respuesta.
    /// </summary>
    public event System.Action AlDetenerse;

    Coroutine rutina;
    bool pararSolicitado;
    float multiplicadorActual = 1f;

    /// <summary>True mientras el aceleron esta en marcha (subiendo, sostenido o bajando).</summary>
    public bool Activo { get { return rutina != null; } }

    // [ANADIDO: animaciones del carrito] Copia publica de pararSolicitado, que hasta
    // ahora era privada. Sirve para distinguir las dos mitades del aceleron desde fuera:
    // Activo esta en true durante la subida, el sostenido Y la bajada, asi que por si
    // solo no dice si el jugador todavia esta ganando velocidad o ya esta frenando.
    // AnimacionesDelCarrito no la usa (se guia por la velocidad, como el resto de
    // efectos del puente), pero queda expuesta para quien la necesite.
    /// <summary>True desde que alguien pide el frenado hasta que arranca el siguiente aceleron.</summary>
    public bool FrenadoSolicitado { get { return pararSolicitado; } }

    void Awake()
    {
        if (Movimiento == null)
            Movimiento = GetComponent<BasicMovement>();
    }

    /// <summary>
    /// Arranca el aceleron. Se mantiene hasta que alguien llame a Detener()
    /// (normalmente el RushStopTrigger del segmento de transicion).
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

        pararSolicitado = false;
        rutina = StartCoroutine(Acelerar());
    }

    /// <summary>
    /// Pide el frenado suave. Lo llama el RushStopTrigger del prefab de transicion.
    /// Si no hay aceleron en marcha no hace nada.
    /// </summary>
    [ContextMenu("Detener aceleron")]
    public void Detener()
    {
        pararSolicitado = true;
    }

    /// <summary>Corta el aceleron de golpe y devuelve la velocidad a la normal.</summary>
    public void Cancelar()
    {
        if (rutina != null)
        {
            StopCoroutine(rutina);
            rutina = null;
        }

        multiplicadorActual = 1f;
        if (Movimiento != null)
            Movimiento.SetMultiplicador(1f);
    }

    IEnumerator Acelerar()
    {
        // --- subida ---
        float t = 0f;
        while (t < TiempoSubida && !pararSolicitado)
        {
            t += Time.deltaTime;
            float p = TiempoSubida <= 0f ? 1f : Mathf.Clamp01(t / TiempoSubida);
            Aplicar(Mathf.LerpUnclamped(1f, MultiplicadorMaximo, CurvaSubida.Evaluate(p)));
            yield return null;
        }

        // --- sostenido: aqui es donde se espera al RushStopTrigger ---
        float sostenido = 0f;
        while (!pararSolicitado)
        {
            if (SegundosDeSeguridad > 0f && sostenido >= SegundosDeSeguridad)
            {
                Debug.LogWarning("TransitionRush: se ha frenado por la red de seguridad, el jugador no llego a cruzar el RushStopTrigger.", this);
                break;
            }

            sostenido += Time.deltaTime;
            Aplicar(MultiplicadorMaximo);
            yield return null;
        }

        // Salimos del sostenido: empieza el frenado, venga del trigger o de la
        // red de seguridad. Avisamos aqui para que el aviso llegue siempre.
        if (AlDetenerse != null)
            AlDetenerse();

        // --- bajada ---
        float desde = multiplicadorActual;
        float t2 = 0f;
        while (t2 < TiempoBajada)
        {
            t2 += Time.deltaTime;
            float p = TiempoBajada <= 0f ? 1f : Mathf.Clamp01(t2 / TiempoBajada);
            Aplicar(Mathf.Lerp(desde, 1f, p));
            yield return null;
        }

        Aplicar(1f);
        rutina = null;
    }

    void Aplicar(float valor)
    {
        multiplicadorActual = valor;
        Movimiento.SetMultiplicador(valor);
    }
}
