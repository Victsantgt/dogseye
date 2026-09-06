using Patterns.Singleton;
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
    [Header("Acercamiento")]
    [Tooltip("Forma de la frenada. Eje X: 0 recien salida, 1 ya colocada. Eje Y: 0 en el punto de spawn, 1 en la marca de acierto. Por defecto entra rapido y termina casi parada.")]
    public AnimationCurve Frenada = new AnimationCurve(
        new Keyframe(0f, 0f, 2.2f, 2.2f),
        new Keyframe(1f, 1f, 0f, 0f));

    [Header("Salida al acertarla")]
    // [ANADIDO: salida despedida] Al pulsarla bien la nota ya no se desvanece: sale
    // disparada arriba y a la derecha girando, como si la apartaramos de un manotazo.
    // El aviso de puntuacion se manda igual en el momento del acierto; lo unico que se
    // retrasa es la vuelta al pool, para que de tiempo a verla salir.
    [Tooltip("A que velocidad sube al ser expulsada.")]
    public float VelocidadHaciaArriba = 18f;

    [Tooltip("Cuanto se va hacia la derecha al mismo tiempo. Negativo la manda a la izquierda.")]
    public float VelocidadHaciaLaDerecha = 12f;

    [Tooltip("Grados por segundo. Es el giro rapido de 'expulsada de la pantalla'.")]
    public float GradosPorSegundo = 900f;

    [Tooltip("Eje del giro. Por defecto Z, que es el eje de la camara: el sprite gira en el plano de la pantalla y se ve el giro entero. Con X o Y se pondria de canto y desapareceria.")]
    public Vector3 EjeDeGiro = Vector3.forward;

    [Tooltip("Segundos desde que sale despedida hasta que vuelve al pool. Para entonces ya esta fuera de plano.")]
    public float SegundosHastaDesaparecer = 0.8f;

    [Tooltip("Apaga el collider de la nota al apartarla. Sin esto, mientras sale volando se queda atras y cruza la caja de fallo del carril, que la cuenta como Miss y le hace dano al jugador que acaba de acertarla.")]
    public bool QuitarColliderAlSalir = true;

    [Header("Explosion del impacto")]
    [Tooltip("Se suelta en el punto del golpe al apartar la nota. Dejalo vacio para no tener explosion.")]
    public GameObject PrefabExplosion;

    [Tooltip("Segundos hasta destruir la explosion. Tiene que cubrir la duracion entera del clip.")]
    public float SegundosDeLaExplosion = 0.45f;

    Transform jugador;
    Transform marca;

    float xFijo, yFijo;    // la nota no se mueve de lado ni en altura
    float zRelInicial;     // unidades por delante del jugador al salir
    float duracion;
    float transcurrido;
    bool moviendose;

    bool despedida;            // ya la hemos apartado de un manotazo
    float tiempoDespedida;
    Quaternion rotacionOriginal;
    Collider[] colliders;

    public AudioClip blast;

    /// <summary>True mientras todavia se esta acercando.</summary>
    public bool Moviendose { get { return moviendose; } }

    /// <summary>True mientras esta saliendo disparada tras acertarla.</summary>
    public bool Despedida { get { return despedida; } }

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

        // La nota viene del pool, asi que puede llegar girada y sin collider de la vez
        // anterior. Se deja como recien salida de fabrica.
        if (despedida) transform.rotation = rotacionOriginal;
        despedida = false;
        tiempoDespedida = 0f;
        PonerColliders(true);
    }

    /// <summary>
    /// Enciende o apaga los colliders de la nota. Al salir despedida se apagan: la nota
    /// sigue subiendo mientras el jugador avanza, asi que acaba quedandose detras de el
    /// y cruzando la caja de fallo del carril. Con el collider puesto eso se contaba
    /// como Miss y le quitaba vida al jugador justo por haber acertado.
    /// </summary>
    void PonerColliders(bool encendidos)
    {
        if (!QuitarColliderAlSalir) return;

        if (colliders == null)
            colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = encendidos;
    }

    /// <summary>
    /// La llama Note.OnPlayerHit() al acertarla. Deja de acercarse y sale disparada
    /// arriba y a la derecha girando, hasta que se recicla sola.
    /// </summary>
    public void SalirDespedida()
    {
        if (despedida) return;

        rotacionOriginal = transform.rotation;
        moviendose = false;     // se acabo el acercamiento
        despedida = true;
        tiempoDespedida = 0f;

        // Ya esta resuelta: a partir de aqui no debe chocar con nada por el camino.
        PonerColliders(false);

        SoltarExplosion();
    }

    /// <summary>
    /// Deja la explosion en el punto exacto del golpe.
    ///
    /// Se instancia SIN padre, en coordenadas de mundo: asi se queda clavada donde
    /// estaba la nota al pulsarla mientras esta sale disparada. Si colgara de la nota
    /// viajaria con ella y se perderia la lectura de "el impacto ocurre aqui y el
    /// enemigo sale despedido". Como no es hija de nadie, tampoco la arrastra el pool
    /// cuando la nota se recicla: se destruye por su cuenta.
    ///
    /// Solo se llega aqui al acertar. Un Miss no llama a SalirDespedida(), asi que
    /// fallar no enciende ninguna explosion.
    /// </summary>
    void SoltarExplosion()
    {
        if (PrefabExplosion == null) return;

        GameObject boom = Instantiate(PrefabExplosion, transform.position, PrefabExplosion.transform.rotation);
        Destroy(boom, Mathf.Max(0.05f, SegundosDeLaExplosion));

        MusicManager.Instance.Play_SFX(blast, true);
    }

    void OnDisable()
    {
        // Al volver al pool se corta todo, que si no la reutilizaria a medio camino.
        moviendose = false;

        if (despedida)
        {
            transform.rotation = rotacionOriginal;
            despedida = false;
        }

        // Se devuelve el collider por si la nota se reutiliza sin pasar por Lanzar().
        PonerColliders(true);
    }

    // LateUpdate y no Update: BasicMovement mueve al jugador en su Update, asi que
    // leyendo aqui su posicion la nota no va un frame por detras.
    void LateUpdate()
    {
        if (despedida) { SalirDeLaPantalla(); return; }

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

    /// <summary>
    /// Sube y se va hacia la derecha girando, y al cabo de SegundosHastaDesaparecer
    /// vuelve al pool. Se mueve en coordenadas de MUNDO y no respecto al jugador: ya no
    /// le acompana, la hemos echado.
    /// </summary>
    void SalirDeLaPantalla()
    {
        float dt = Time.deltaTime;
        tiempoDespedida += dt;

        transform.position += new Vector3(VelocidadHaciaLaDerecha, VelocidadHaciaArriba, 0f) * dt;

        if (GradosPorSegundo != 0f && EjeDeGiro != Vector3.zero)
            transform.Rotate(EjeDeGiro.normalized * GradosPorSegundo * dt, Space.World);

        if (tiempoDespedida < SegundosHastaDesaparecer) return;

        // Se devuelve al pool. El Active del Note dispara su OnDisable, que es donde se
        // deshace el giro para que la siguiente salga derecha.
        Note nota = GetComponent<Note>();
        if (nota != null) nota.Active = false;
        else gameObject.SetActive(false);
    }
}
