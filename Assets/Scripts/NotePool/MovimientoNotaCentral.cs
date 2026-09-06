using UnityEngine;

/// <summary>
/// [ANADIDO: nota central] Acercamiento de la nota del carril del medio.
///
/// POR QUE HACE FALTA. Las notas de este juego no se mueven: se dejan quietas en el
/// mundo y es el jugador quien las alcanza corriendo. Por eso la distancia de spawn ES
/// el tiempo de vuelo. Las laterales salen a 70 unidades por delante (2.92 s a 24 u/s)
/// y llegan a tiempo. La central tiene que salir mucho mas lejos, a 113, para no
/// aparecer de la nada dentro del campo de vision, y con eso tardaria 1.79 s de mas.
///
/// La solucion es que esta si se mueva: recorre sus 113 unidades en el MISMO tiempo que
/// las laterales recorren las suyas. Y no a velocidad constante, sino frenando: entra
/// muy rapido y va perdiendo velocidad segun se acerca, que es como se lee un enemigo
/// que se te echa encima y se planta delante.
///
/// COMO SE CALCULA EL TIEMPO. No se escribe a mano: el NoteSpawner lo saca de la
/// geometria real de los carriles, (spawn lateral - marca lateral) / PlayerSpeed. Si
/// alguien mueve un carril o el collider, la central se reajusta sola y sigue cayendo
/// en el mismo momento que las laterales.
///
/// TODO se calcula respecto al jugador, no en coordenadas de mundo: la marca de acierto
/// cuelga de -- RHYTHM SYSTEM --, que va montado en el jugador, asi que su distancia
/// por delante es constante aunque el jugador avance.
///
/// AL LLEGAR deja de moverse. A partir de ahi se queda quieta en el mundo y es el
/// jugador quien la rebasa, o sea que el enemigo se queda sin fuelle y le adelantas.
/// Eso es tambien lo que hace que acabe cruzando el ColliderFinal y vuelva al pool.
/// </summary>
[DisallowMultipleComponent]
public class MovimientoNotaCentral : MonoBehaviour
{
    [Tooltip("Forma de la frenada. Eje X: 0 recien salida, 1 ya colocada. Eje Y: 0 en el punto de spawn, 1 en la marca de acierto. Por defecto entra rapido y termina casi parada.")]
    public AnimationCurve Frenada = new AnimationCurve(
        new Keyframe(0f, 0f, 2.2f, 2.2f),
        new Keyframe(1f, 1f, 0f, 0f));

    Transform jugador;
    Transform marca;

    float xFijo, yFijo;    // la nota no se mueve de lado ni en altura
    float zRelInicial;     // unidades por delante del jugador al salir
    float duracion;
    float transcurrido;
    bool moviendose;

    /// <summary>True mientras todavia se esta acercando.</summary>
    public bool Moviendose { get { return moviendose; } }

    /// <summary>
    /// La llama el NoteSpawner justo despues de colocar la nota en su punto de salida.
    /// </summary>
    /// <param name="objetivo">El Player.</param>
    /// <param name="marcaDeAcierto">La marca donde tiene que quedarse, o sea laneMiddlePerfect.</param>
    /// <param name="segundos">Tiempo de vuelo, el mismo que gastan las laterales.</param>
    public void Lanzar(Transform objetivo, Transform marcaDeAcierto, float segundos)
    {
        if (objetivo == null || marcaDeAcierto == null)
        {
            Debug.LogError("MovimientoNotaCentral: falta el jugador o la marca de acierto, la nota se queda quieta.", this);
            moviendose = false;
            return;
        }

        jugador = objetivo;
        marca = marcaDeAcierto;

        xFijo = transform.position.x;
        yFijo = transform.position.y;
        zRelInicial = transform.position.z - jugador.position.z;

        duracion = Mathf.Max(0.01f, segundos);
        transcurrido = 0f;
        moviendose = true;
    }

    void OnDisable()
    {
        // Al volver al pool se corta, que si no la reutilizaria a medio camino.
        moviendose = false;
    }

    // LateUpdate y no Update: BasicMovement mueve al jugador en su Update, asi que
    // leyendo aqui su posicion la nota no va un frame por detras.
    void LateUpdate()
    {
        if (!moviendose) return;

        if (jugador == null || marca == null)
        {
            moviendose = false;
            return;
        }

        transcurrido += Time.deltaTime;
        float p = Mathf.Clamp01(transcurrido / duracion);

        // La marca se lee cada frame en vez de guardarla al salir: asi, si alguien
        // recoloca el collider central, la nota va al sitio nuevo sin tocar nada aqui.
        float zRelFinal = marca.position.z - jugador.position.z;
        float zRel = Mathf.LerpUnclamped(zRelInicial, zRelFinal, Frenada.Evaluate(p));

        transform.position = new Vector3(xFijo, yFijo, jugador.position.z + zRel);

        if (p >= 1f)
        {
            // Ya esta colocada. A partir de aqui se queda quieta y el jugador la rebasa.
            moviendose = false;
        }
    }
}
