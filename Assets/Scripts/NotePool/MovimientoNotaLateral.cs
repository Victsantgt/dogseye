using UnityEngine;

/// <summary>
/// [ANADIDO: notas laterales a la cesta] Salto de la nota lateral al acertarla.
///
/// En vez de desaparecer de golpe, la nota salta desde donde estaba y describe un arco
/// hasta el centro de la cesta (Tubo.001), girando por el camino. Al llegar se hunde y
/// vuelve al pool.
///
/// EL DESTINO SE LEE CADA FRAME, NO AL SALIR. Es lo importante de este script. La cesta
/// cuelga del Player y avanza con el a 24 u/s, asi que si se guardara su posicion al
/// empezar el salto, para cuando la nota llegara alli la cesta ya se habria ido y el
/// objeto caeria detras del jugador. Leyendo el centro de la cesta en cada frame, el
/// destino viaja con ella y el arco siempre acaba dentro.
///
/// LA LLEGADA NO USA FISICA. El arco termina exactamente en el centro de la cesta, o
/// sea que el momento de llegada ya es un dato exacto: cuando el recorrido llega a 1, se
/// acabo. Por eso durante el vuelo se apaga el collider, que ademas evita el problema
/// que tuvimos con la nota central: quedarse atras y cruzar la caja de fallo, que la
/// contaba como Miss y quitaba vida justo por haber acertado.
///
/// EL GIRO es de una cantidad al azar entre 0 y 360 grados, sorteada en cada salto, y
/// termina justo cuando el objeto toca la cesta. Asi unas veces da la vuelta entera y
/// otras se queda a medias, y nunca se ve el corte de una rotacion a medio hacer.
///
///
/// ============================================================================
/// AVISO IMPORTANTE, PARA CUANDO ALGO DE LAS NOTAS LATERALES FALLE
/// ============================================================================
///
/// EN LAS NOTAS LATERALES, EL COLLIDER Y EL DIBUJO NO ESTAN EN EL MISMO SITIO.
///
/// Los clips 'teddy' (Animaciones2d y Animaciones2d 1) no mueven la nota: animan la
/// POSICION LOCAL del sprite hijo. Al terminar el clip lo dejan en local (-5.86, 2.22)
/// en NoteLeft y (6.20, 2.22) en NoteRight, o sea a unas 6 unidades de la raiz.
///
/// La raiz es la que lleva el Collider, el Rigidbody y la que se compara con el
/// perfectMark. O sea que la DETECCION va por la raiz y lo que el jugador VE va seis
/// unidades mas alla. El ritmo sale bien porque el perfectMark esta calibrado sobre la
/// raiz, pero el jugador acierta en un momento en el que el muneco no esta donde la caja.
///
/// Sintomas de que esto es la causa de algo:
///   - "acierto y el objeto no estaba ahi" o la ventana de acierto se siente descolocada
///   - algo que se mueve o gira respecto a la nota parece orbitar en vez de girar
///     (fue justo este bug: al girar la raiz, el dibujo a 6 u barria un circulo)
///   - un objeto lanzado hacia un destino llega con la raiz pero el dibujo cae al lado
///
/// Por eso este script, antes de saltar, congela ese Animator y hace coincidir raiz y
/// dibujo (ver CongelarYCentrarElSprite). Eso arregla el salto, PERO NO la caida previa:
/// durante todo el descenso el desfase sigue ahi.
///
/// El arreglo de raiz seria que el clip 'teddy' no tocara m_LocalPosition y que el
/// desplazamiento lo llevara el objeto entero. Mientras no se haga, tenerlo presente
/// antes de dar por buena cualquier medida sobre las notas laterales.
/// ============================================================================
/// </summary>
[DisallowMultipleComponent]
public class MovimientoNotaLateral : MonoBehaviour
{
    [Header("Destino")]
    // [CAMBIO] Antes esto apuntaba a Tubo.001 y se usaba el centro del bounds de su
    // Renderer. Mala idea: Tubo.001 es un hueso de un rig animado, y bounds es la caja
    // ALINEADA A EJES DE MUNDO, que se deforma y desplaza su centro cada vez que el
    // hueso gira con la animacion. De ahi que las notas salieran hacia sitios distintos
    // en cada salto. Ahora se apunta a un objeto vacio colocado a mano dentro de la
    // cesta y se usa su posicion tal cual, que es un punto estable.
    [Tooltip("Punto exacto al que van las notas. Es el objeto vacio que hay dentro de Tubo.001. Si se deja vacio se busca por nombre dentro del jugador.")]
    public Transform Cesta;

    [Tooltip("Nombre con el que buscar el punto de destino si no esta asignado arriba.")]
    public string NombreDeLaCesta = "item reference";

    [Tooltip("Ajuste fino sobre el centro de la cesta, por si el objeto tiene que entrar un poco mas arriba o mas al fondo.")]
    public Vector3 AjusteDelDestino = Vector3.zero;

    [Header("Salto")]
    [Tooltip("Segundos que dura el salto entero, desde que se acierta hasta que se hunde en la cesta.")]
    public float Duracion = 0.55f;

    [Tooltip("Altura del arco por encima de la linea recta entre el punto de salida y la cesta.")]
    public float AlturaDelSalto = 3.5f;

    [Tooltip("Forma del avance hacia la cesta. Por defecto sale rapido y llega frenando.")]
    public AnimationCurve Avance = new AnimationCurve(
        new Keyframe(0f, 0f, 1.6f, 1.6f),
        new Keyframe(1f, 1f, 0.4f, 0.4f));

    [Header("Giro")]
    [Tooltip("Grados minimos que gira durante el salto.")]
    public float GradosMinimos = 0f;

    [Tooltip("Grados maximos. Con 360 puede llegar a dar una vuelta completa.")]
    public float GradosMaximos = 360f;

    [Tooltip("Eje del giro. Z es el eje de la camara: el sprite gira en el plano de la pantalla y se ve la vuelta entera.")]
    public Vector3 EjeDeGiro = Vector3.forward;

    [Header("Fisica")]
    [Tooltip("Apaga el collider mientras vuela. Sin esto la nota puede cruzar la caja de fallo por el camino y contarse como Miss, quitando vida al jugador que acaba de acertarla.")]
    public bool QuitarColliderAlSaltar = true;

    Transform jugador;
    Vector3 salidaRelativa;    // punto de salida, en coordenadas relativas al jugador

    // [ANADIDO: congelar la animacion del sprite] Los clips 'teddy' de las notas
    // laterales animan la POSICION LOCAL del sprite hijo: lo desplazan entre 6 y 12
    // unidades respecto a la raiz. Mientras eso corra, mover y girar la raiz no sirve de
    // nada, porque el dibujo esta en otro sitio: el giro se ve como una orbita enorme y
    // el objeto nunca parece llegar a la cesta aunque la raiz si llegue.
    Animator[] animadores;
    Transform[] hijosAnimados;
    Vector3[] localesOriginales;
    bool congelado;
    float transcurrido;
    float gradosTotales;
    float gradosAplicados;
    bool saltando;
    Quaternion rotacionOriginal;
    Collider[] colliders;

    /// <summary>True mientras esta en el aire camino de la cesta.</summary>
    public bool Saltando { get { return saltando; } }

    /// <summary>
    /// La llama Note.OnPlayerHit() al acertar la nota. Si devuelve false es que no ha
    /// podido saltar y quien llama debe reciclarla como siempre.
    /// </summary>
    public bool Saltar()
    {
        if (saltando) return true;

        if (!Preparar()) return false;

        rotacionOriginal = transform.rotation;

        // Antes de nada, hacer coincidir la raiz con lo que se esta viendo. Si no, el
        // arco moveria la raiz y el dibujo seguiria su propio camino varios metros aparte.
        CongelarYCentrarElSprite();

        salidaRelativa = transform.position - jugador.position;
        transcurrido = 0f;
        gradosAplicados = 0f;

        // Un sorteo por salto: a veces da la vuelta entera y a veces se queda a medias.
        gradosTotales = Random.Range(GradosMinimos, GradosMaximos);

        saltando = true;
        PonerColliders(false);
        return true;
    }

    /// <summary>
    /// Apaga la animacion propia del sprite y lo centra en la raiz, SIN que se note un
    /// salto: primero se anota donde se esta viendo el dibujo, luego se pega el hijo a la
    /// raiz y por ultimo se lleva la raiz a ese punto. El resultado es que el sprite se
    /// queda exactamente donde estaba, pero a partir de ahora raiz y dibujo son lo mismo
    /// y el arco puede llevarlo al sitio exacto.
    /// </summary>
    void CongelarYCentrarElSprite()
    {
        if (congelado) return;

        if (animadores == null)
        {
            animadores = GetComponentsInChildren<Animator>(true);
            var srs = GetComponentsInChildren<SpriteRenderer>(true);
            hijosAnimados = new Transform[srs.Length];
            localesOriginales = new Vector3[srs.Length];
            for (int i = 0; i < srs.Length; i++)
            {
                hijosAnimados[i] = srs[i].transform;
                localesOriginales[i] = srs[i].transform.localPosition;
            }
        }

        // Donde se ve el dibujo ahora mismo
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
        Vector3 dondeSeVe = sr != null ? sr.bounds.center : transform.position;

        for (int i = 0; i < animadores.Length; i++)
            if (animadores[i] != null) animadores[i].enabled = false;

        for (int i = 0; i < hijosAnimados.Length; i++)
            if (hijosAnimados[i] != null && hijosAnimados[i] != transform)
                hijosAnimados[i].localPosition = Vector3.zero;

        transform.position = dondeSeVe;
        congelado = true;
    }

    /// <summary>Devuelve el sprite a su animacion normal. Se llama al reciclar la nota.</summary>
    void DescongelarElSprite()
    {
        if (!congelado) return;

        for (int i = 0; i < hijosAnimados.Length; i++)
            if (hijosAnimados[i] != null && hijosAnimados[i] != transform)
                hijosAnimados[i].localPosition = localesOriginales[i];

        for (int i = 0; i < animadores.Length; i++)
            if (animadores[i] != null) animadores[i].enabled = true;

        congelado = false;
    }

    bool Preparar()
    {
        if (Cesta == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                Transform[] todos = p.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < todos.Length; i++)
                    if (todos[i].name == NombreDeLaCesta) { Cesta = todos[i]; break; }
            }
        }

        if (Cesta == null)
        {
            Debug.LogWarning("MovimientoNotaLateral: no se encuentra la cesta '" + NombreDeLaCesta + "', la nota desaparece como antes.", this);
            return false;
        }

        jugador = Cesta.root;
        return jugador != null;
    }

    /// <summary>
    /// Donde tiene que caer la nota, en coordenadas relativas al jugador.
    ///
    /// Se lee la posicion del transform tal cual, NO el bounds de ningun Renderer: la
    /// cesta es un hueso animado y su caja de bounds se mueve sola al girar. Este punto
    /// si sigue a la cesta de verdad, incluida su animacion, porque cuelga de ella.
    /// </summary>
    Vector3 DestinoRelativo()
    {
        return (Cesta.position - jugador.position) + AjusteDelDestino;
    }

    // LateUpdate: el jugador se mueve en su Update, asi que leyendo aqui su posicion la
    // nota no va un frame por detras de la cesta.
    void LateUpdate()
    {
        if (!saltando) return;

        if (jugador == null || Cesta == null) { Terminar(); return; }

        transcurrido += Time.deltaTime;
        float p = Mathf.Clamp01(transcurrido / Mathf.Max(0.01f, Duracion));
        float avance = Avance.Evaluate(p);

        // Todo en coordenadas relativas al jugador: el destino se relee cada frame, asi
        // que la cesta no puede dejar atras al objeto por mucho que avance.
        Vector3 rel = Vector3.LerpUnclamped(salidaRelativa, DestinoRelativo(), avance);

        // Parabola de salto: 0 en los extremos y maximo en la mitad del recorrido.
        rel.y += AlturaDelSalto * 4f * p * (1f - p);

        transform.position = jugador.position + rel;

        // El giro se reparte por el recorrido y llega justo a su total al tocar la cesta.
        float objetivo = gradosTotales * avance;
        float delta = objetivo - gradosAplicados;
        gradosAplicados = objetivo;
        if (delta != 0f && EjeDeGiro != Vector3.zero)
            transform.Rotate(EjeDeGiro.normalized * delta, Space.World);

        if (p >= 1f) Terminar();
    }

    /// <summary>Ha tocado la cesta: se hunde y vuelve al pool.</summary>
    void Terminar()
    {
        saltando = false;

        Note nota = GetComponent<Note>();
        if (nota != null) nota.Active = false;
        else gameObject.SetActive(false);
    }

    void PonerColliders(bool encendidos)
    {
        if (!QuitarColliderAlSaltar) return;

        if (colliders == null)
            colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = encendidos;
    }

    void OnDisable()
    {
        // Al volver al pool se deja como recien salida de fabrica: sin giro acumulado y
        // con el collider puesto, que si no la siguiente no se podria ni pulsar.
        if (saltando || gradosAplicados != 0f)
        {
            transform.rotation = rotacionOriginal;
            gradosAplicados = 0f;
        }

        saltando = false;
        PonerColliders(true);

        // Y el sprite vuelve a su animacion propia, que si no la siguiente nota saldria
        // del pool congelada y pegada a la raiz.
        DescongelarElSprite();
    }
}
