using UnityEngine;

/// <summary>
/// Efecto dolly zoom (efecto Vertigo) durante el aceleron: el FOV se abre mientras la
/// camara se acerca, de forma que el jugador conserva el mismo tamano en pantalla pero
/// el fondo se deforma y parece huir. Es el mismo truco de Vertigo y Tiburon.
///
/// La intensidad se lee cada frame de BasicMovement.VelocidadActual / PlayerSpeed,
/// igual que SpeedLinesHUD, asi que dura exactamente lo mismo que el aceleron y las
/// lineas y sigue sus rampas de subida y bajada sin sincronizar nada a mano.
///
/// Va en la propia camara. No hay nada mas que dependa de este componente: para
/// quitarlo basta con borrarlo (ver la nota al final del fichero).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class DollyZoomEffect : MonoBehaviour
{
    [Header("Activacion")]
    [Tooltip("Desmarcalo para anular el efecto sin quitar el componente.")]
    public bool Activo = true;

    [Header("Referencias")]
    [Tooltip("De aqui sale la velocidad. Si se deja vacio se busca en la escena.")]
    public BasicMovement Movimiento;

    [Tooltip("De aqui sale el multiplicador maximo. Si se deja vacio se busca en la escena.")]
    public TransitionRush Aceleron;

    [Header("Dolly zoom")]
    [Tooltip("Cuanto se abre el FOV en el pico del aceleron. Positivo = gran angular, el fondo se aleja.")]
    public float DeltaFOV = 25f;

    [Tooltip("Cuanto se compensa el zoom acercando la camara. 1 = dolly zoom real (el jugador no cambia de tamano). 0 = solo zoom, sin mover la camara.")]
    [Range(0f, 1f)] public float CompensacionDolly = 1f;

    [Header("Alejamiento")]
    [Tooltip("Unidades que la camara se echa hacia atras en el pico del aceleron, siguiendo la misma direccion en la que ya esta colocada. Asi se aleja sin cambiar el angulo desde el que mira al jugador. Vuelve sola a 0 al bajar la intensidad.")]
    public float RetrocesoMaximo = 6f;

    [Tooltip("Desplazamiento extra en espacio local que se suma en el pico, por si quieres ademas subirla, bajarla o abrirla de lado. Se queda en cero si no lo tocas.")]
    public Vector3 DesplazamientoExtra = Vector3.zero;

    [Header("Respuesta a la velocidad")]
    [Tooltip("Por debajo de esta intensidad (0-1) no se aplica nada.")]
    [Range(0f, 1f)] public float UmbralMinimo = 0.05f;

    [Tooltip("Suaviza la subida y bajada, igual que las lineas de velocidad.")]
    public bool SuavizarIntensidad = true;

    [Header("Modo manual (sin aceleron)")]
    [Tooltip("Intensidad maxima que alcanza el efecto cuando lo lanza LanzarManual(). Bajalo si quieres que la version sin aceleron sea mas suave que la del puente.")]
    [Range(0f, 1f)] public float IntensidadManualMaxima = 1f;

    [Tooltip("Segundos que tarda en llegar al maximo en modo manual.")]
    public float TiempoSubidaManual = 0.4f;

    [Tooltip("Segundos que tarda en volver a la normalidad en modo manual.")]
    public float TiempoBajadaManual = 0.6f;

    [Header("Punto de fuga")]
    [Tooltip("Desplaza el punto de fuga sin girar la camara, usando el lens shift de camara fisica. Asi las verticales siguen rectas, cosa que no pasa si inclinas la camara.")]
    public bool DesplazarPuntoDeFuga = false;

    [Tooltip("Positivo sube el punto de fuga por encima del jugador. Si lo ves invertido, ponlo en negativo. Se mide en fracciones del sensor, valores utiles entre 0.05 y 0.25.")]
    public float AlturaPuntoDeFuga = 0.1f;

    [Tooltip("Si esta marcado el punto de fuga solo se desplaza durante el aceleron. Si no, se queda desplazado siempre.")]
    public bool PuntoDeFugaSoloEnAceleron = false;

    Camera cam;

    Coroutine rutinaManual;
    float intensidadManual;

    // Estado original, para poder devolver la camara a como estaba
    float fovBase;
    Vector3 posicionBase;
    bool fisicaBase;
    Vector2 lensShiftBase;
    Camera.GateFitMode gateFitBase;

    void Awake()
    {
        cam = GetComponent<Camera>();

        fovBase = cam.fieldOfView;
        posicionBase = transform.localPosition;
        fisicaBase = cam.usePhysicalProperties;
        lensShiftBase = cam.lensShift;
        gateFitBase = cam.gateFit;

        if (Movimiento == null) Movimiento = FindFirstObjectByType<BasicMovement>();
        if (Aceleron == null) Aceleron = FindFirstObjectByType<TransitionRush>();
    }

    void OnDisable()
    {
        CancelarManual();
        Restaurar();
    }

    void LateUpdate()
    {
        if (!Activo)
        {
            Restaurar();
            return;
        }

        // Dos fuentes: la velocidad (durante el aceleron) y la envolvente manual por
        // tiempo (cuando la respuesta no genera transicion y no hay cambio de velocidad).
        // Nos quedamos con la mayor, asi nunca se pisan.
        float intensidad = Mathf.Max(CalcularIntensidad(), intensidadManual);

        AplicarDollyZoom(intensidad);
        AplicarPuntoDeFuga(intensidad);
    }

    /// <summary>
    /// Lanza el efecto de camara por tiempo, sin tocar la velocidad del jugador.
    /// Lo usa el DecisionManager cuando la opcion elegida no genera segmento de
    /// transicion: mismo dolly zoom, pero durando unos segundos fijos en vez de
    /// hasta cruzar un trigger.
    /// </summary>
    public void LanzarManual(float duracion)
    {
        if (rutinaManual != null)
            StopCoroutine(rutinaManual);

        rutinaManual = StartCoroutine(EnvolventeManual(duracion));
    }

    /// <summary>Corta el efecto manual de golpe.</summary>
    public void CancelarManual()
    {
        if (rutinaManual != null)
        {
            StopCoroutine(rutinaManual);
            rutinaManual = null;
        }

        intensidadManual = 0f;
    }

    System.Collections.IEnumerator EnvolventeManual(float duracion)
    {
        float subida = Mathf.Max(0f, TiempoSubidaManual);
        float bajada = Mathf.Max(0f, TiempoBajadaManual);

        // Si la duracion pedida no da para subir y bajar, encogemos las rampas
        // proporcionalmente en vez de pasarnos del tiempo pedido.
        if (duracion > 0f && subida + bajada > duracion)
        {
            float k = duracion / (subida + bajada);
            subida *= k;
            bajada *= k;
        }

        float sostenido = Mathf.Max(0f, duracion - subida - bajada);
        float tope = Mathf.Clamp01(IntensidadManualMaxima);

        float t = 0f;
        while (t < subida)
        {
            t += Time.deltaTime;
            intensidadManual = tope * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / subida));
            yield return null;
        }

        intensidadManual = tope;

        if (sostenido > 0f)
            yield return new WaitForSeconds(sostenido);

        t = 0f;
        while (t < bajada)
        {
            t += Time.deltaTime;
            intensidadManual = tope * Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(t / bajada));
            yield return null;
        }

        intensidadManual = 0f;
        rutinaManual = null;
    }

    float CalcularIntensidad()
    {
        if (Movimiento == null || Movimiento.PlayerSpeed <= 0.0001f)
            return 0f;

        float maximo = Aceleron != null ? Aceleron.MultiplicadorMaximo : 2.5f;
        if (maximo <= 1.0001f) return 0f;

        float ratio = Movimiento.VelocidadActual / Movimiento.PlayerSpeed;
        float bruto = Mathf.InverseLerp(1f, maximo, ratio);

        if (bruto < UmbralMinimo) return 0f;

        return SuavizarIntensidad ? Mathf.SmoothStep(0f, 1f, bruto) : bruto;
    }

    void AplicarDollyZoom(float intensidad)
    {
        float fov = Mathf.Lerp(fovBase, fovBase + DeltaFOV, intensidad);
        cam.fieldOfView = fov;

        // Para que el jugador conserve el mismo tamano en pantalla, la distancia tiene
        // que cumplir  d * tan(fov/2) = constante. Escalamos el offset local entero,
        // asi el jugador se queda encuadrado donde estaba y solo cambia la perspectiva.
        float mitadBase = fovBase * 0.5f * Mathf.Deg2Rad;
        float mitadAhora = fov * 0.5f * Mathf.Deg2Rad;

        float factorReal = Mathf.Tan(mitadBase) / Mathf.Tan(mitadAhora);
        float factor = Mathf.Lerp(1f, factorReal, CompensacionDolly);

        Vector3 pos = posicionBase * factor;

        // Alejamiento: se suma en la misma direccion en la que ya esta la camara, o sea
        // que se retira sin cambiar el angulo desde el que encuadra al jugador.
        // Al bajar la intensidad a 0 el termino desaparece y la camara vuelve sola.
        if (posicionBase.sqrMagnitude > 0.0001f)
            pos += posicionBase.normalized * (RetrocesoMaximo * intensidad);

        pos += DesplazamientoExtra * intensidad;

        transform.localPosition = pos;
    }

    void AplicarPuntoDeFuga(float intensidad)
    {
        if (!DesplazarPuntoDeFuga)
        {
            if (cam.usePhysicalProperties != fisicaBase)
            {
                cam.usePhysicalProperties = fisicaBase;
                cam.lensShift = lensShiftBase;
                cam.gateFit = gateFitBase;
            }
            return;
        }

        cam.usePhysicalProperties = true;
        // Vertical mantiene el FOV vertical que ya tenia la camara, asi activar la
        // camara fisica no reencuadra por si solo.
        cam.gateFit = Camera.GateFitMode.Vertical;

        float altura = PuntoDeFugaSoloEnAceleron ? AlturaPuntoDeFuga * intensidad : AlturaPuntoDeFuga;

        // Signo negativo: bajar el frustum sube el punto de fuga en la imagen.
        cam.lensShift = new Vector2(lensShiftBase.x, lensShiftBase.y - altura);
    }

    void Restaurar()
    {
        if (cam == null) return;

        cam.fieldOfView = fovBase;
        transform.localPosition = posicionBase;
        cam.usePhysicalProperties = fisicaBase;
        cam.lensShift = lensShiftBase;
        cam.gateFit = gateFitBase;
    }
}

// PARA QUITAR EL EFECTO:
//   1. Quita el componente DollyZoomEffect de la Main Camera (o desmarca Activo).
//   2. Borra este fichero.
// No hay nada mas: ningun otro script llama a esta clase ni la referencia.
