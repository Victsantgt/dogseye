using System.Collections;
using System.Collections.Generic;
using Patterns.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [ANADIDO: secuencia de derrota] Lo que se ve al quedarse sin vida, antes de cambiar
/// a la escena de derrota.
///
/// La llama LifeManager cuando la vida llega a 0. Antes eso saltaba directo a la escena
/// con un simple cierre de imagen; ahora primero se ve el tropiezo:
///
///   1. La camara se desengancha del jugador y se queda CLAVADA en el mundo.
///   2. Las manos desaparecen, como si el carrito se le hubiera escapado.
///   3. El conjunto (personaje y carrito) sigue avanzando solo unos instantes, frenando.
///   4. Se va de lado hacia la izquierda de la camara.
///   5. Fundido a negro y cambio de escena.
///
/// POR QUE CAE EL CONJUNTO ENTERO. El carrito no es un objeto suelto: es un hueso
/// (Tubo) dentro del rig del personaje, igual que los brazos. Sacarlo de ahi lo dejaria
/// sin animar y al personaje en una pose rara con las manos vacias. Cae todo junto, que
/// ademas se lee igual de bien.
///
/// LA IZQUIERDA ES LA DE LA CAMARA. La camara mira hacia +Z, asi que su izquierda es -X.
/// Girar sobre el eje Z de mundo inclina la vertical del objeto hacia -X, o sea que cae
/// hacia la izquierda de lo que se ve. Por eso el eje por defecto es Z y no otro.
/// </summary>
[DisallowMultipleComponent]
public class SecuenciaDeDerrota : MonoBehaviour
{
    [Header("Piezas de la escena")]
    [Tooltip("La camara. Se saca de la jerarquia del jugador para que se quede quieta.")]
    public Transform Camara;

    [Tooltip("Lo que se cae: el personaje con su carrito. Normalmente CuboPequeno, que es quien los lleva a los dos.")]
    public Transform ConjuntoQueCae;

    [Tooltip("Las manos, que se ocultan al perder el control. Es el objeto rigbrazos.")]
    public GameObject Manos;

    [Header("Que se apaga al morir")]
    [Tooltip("Componentes que se desactivan nada mas morir. BasicMovement NO va aqui: lo apaga el propio script para tomar el control del avance.")]
    public List<Behaviour> ComponentesADesactivar = new List<Behaviour>();

    [Tooltip("Objetos enteros que se desactivan: el sistema de notas, paneles de UI de juego...")]
    public List<GameObject> ObjetosADesactivar = new List<GameObject>();

    [Header("Tropiezo")]
    [Tooltip("Segundos que el conjunto sigue avanzando solo antes de empezar a caer.")]
    public float SegundosAvanzando = 0.8f;

    [Tooltip("Como pierde velocidad mientras se aleja. Eje Y: 1 = la velocidad que llevaba, 0 = parado.")]
    public AnimationCurve Frenada = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(1f, 0.15f, -0.8f, -0.8f));

    [Header("Caida")]
    [Tooltip("Segundos que tarda en irse de lado del todo.")]
    public float SegundosCayendo = 0.7f;

    [Tooltip("Grados que se inclina. 90 es tumbado del todo.")]
    public float GradosDeCaida = 90f;

    [Tooltip("Eje del giro, en coordenadas de MUNDO. Z tumba hacia la izquierda de la camara; ponlo en -Z para que caiga a la derecha.")]
    public Vector3 EjeDeCaida = Vector3.forward;

    [Tooltip("Forma de la caida. Por defecto arranca despacio y se desploma al final, como algo que pierde el equilibrio.")]
    public AnimationCurve CurvaDeCaida = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2.2f, 2.2f));

    [Header("Fundido a negro")]
    [Tooltip("Objeto que contiene el negro. Se activa al empezar el fundido.")]
    public GameObject PanelNegro;

    [Tooltip("Imagen negra a pantalla completa.")]
    public Image Negro;

    [Tooltip("Si el panel del negro trae textos, se fuerzan a invisibles: aqui no queremos texto, el texto va en la escena de derrota.")]
    public CanvasGroup TextosAOcultar;

    [Tooltip("Segundos que tarda la pantalla en ponerse negra. Empieza cuando el conjunto ya se ha caido.")]
    public float DuracionFundido = 1f;

    [Tooltip("Segundos de negro completo antes de cambiar de escena.")]
    public float EsperaEnNegro = 0.35f;

    [Header("Escena de derrota")]
    [Tooltip("Nombre de la escena a la que se va. Tiene que estar en Build Settings.")]
    public string EscenaDeDerrota = "Derrota";

    [Header("Notas en pantalla")]
    [Tooltip("Devuelve al pool las notas CENTRALES que quedaran en pantalla al perder. Las laterales se dejan como estan a proposito.")]
    public bool LimpiarNotasCentrales = true;

    [Header("Musica")]
    [Tooltip("Funde la musica mientras dura el tropiezo, en vez de cortarla de golpe.")]
    public bool FundirLaMusica = true;

    bool enMarcha;

    /// <summary>True desde que arranca la secuencia. Evita que se dispare dos veces.</summary>
    public bool EnMarcha { get { return enMarcha; } }

    /// <summary>
    /// Arranca la secuencia. La llama LifeManager al llegar la vida a 0.
    ///
    /// Es reentrante a proposito: con el antispam, al morir siguen llegando pulsaciones
    /// y notas durante los frames siguientes, y cada una volveria a llamar aqui. La
    /// guarda de abajo hace que solo cuente la primera.
    /// </summary>
    [ContextMenu("Lanzar derrota (prueba)")]
    public void Lanzar()
    {
        if (enMarcha) return;
        enMarcha = true;

        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        LimpiarLasCentrales();
        ApagarElJuego();
        SoltarLaCamara();
        OcultarLasManos();

        yield return Avanzar();
        yield return Caer();
        yield return FundirANegro();

        yield return new WaitForSeconds(EsperaEnNegro);

        if (string.IsNullOrEmpty(EscenaDeDerrota))
        {
            Debug.LogError("SecuenciaDeDerrota: no hay nombre de escena de derrota, la partida se queda en negro.", this);
            yield break;
        }

        SceneManager.LoadScene(EscenaDeDerrota);
    }

    /// <summary>
    /// Quita de en medio las notas CENTRALES que quedaran en pantalla al morir. Las
    /// laterales se dejan a proposito: molestan menos y se van solas.
    ///
    /// Hay que buscarlas por toda la escena, no dentro de -- RHYTHM SYSTEM --. El pool
    /// las crea con Note.Clone(), que hace Instantiate SIN PADRE, asi que viven en la
    /// raiz de la escena. Por eso apagar el sistema de notas no las oculta, y por eso el
    /// Desactivar() del RhythmSystemToggle tampoco las encuentra: el busca por
    /// GetComponentsInChildren y ahi no cuelga ninguna.
    ///
    /// Se identifican por llevar MovimientoNotaCentral, que es lo unico que distingue a
    /// la central de las laterales.
    ///
    /// Va lo PRIMERO de la secuencia, antes de apagar nada: si se desactivara antes algo
    /// de lo que colgaran, dejarian de encontrarse.
    /// </summary>
    void LimpiarLasCentrales()
    {
        if (!LimpiarNotasCentrales) return;

        var centrales = Object.FindObjectsByType<MovimientoNotaCentral>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int limpiadas = 0;

        for (int i = 0; i < centrales.Length; i++)
        {
            if (centrales[i] == null) continue;

            Note nota = centrales[i].GetComponent<Note>();
            if (nota == null) continue;

            // Mismo orden que usa RhythmSystemToggle: primero fuera de pantalla y luego
            // el Reset, que es quien mata el tween y devuelve el estado del pool.
            nota.Active = false;
            nota.Reset();
            limpiadas++;
        }

        if (limpiadas > 0)
            Debug.Log("SecuenciaDeDerrota: " + limpiadas + " nota(s) central(es) devuelta(s) al pool al perder.", this);
    }

    void ApagarElJuego()
    {
        // El movimiento del jugador lo apagamos aqui, no por la lista: a partir de ahora
        // el avance lo lleva esta secuencia y dos cosas escribiendo la posicion se pisan.
        BasicMovement movimiento = GetComponent<BasicMovement>();
        if (movimiento != null) movimiento.enabled = false;

        TransitionRush aceleron = GetComponent<TransitionRush>();
        if (aceleron != null) { aceleron.Cancelar(); aceleron.enabled = false; }

        for (int i = 0; i < ComponentesADesactivar.Count; i++)
            if (ComponentesADesactivar[i] != null) ComponentesADesactivar[i].enabled = false;

        for (int i = 0; i < ObjetosADesactivar.Count; i++)
            if (ObjetosADesactivar[i] != null) ObjetosADesactivar[i].SetActive(false);

        if (FundirLaMusica && MusicManager.Instance != null)
        {
            // La corrutina va en el propio MusicManager: es un singleton con
            // DontDestroyOnLoad y aqui vamos a cambiar de escena a media secuencia.
            MusicManager musica = MusicManager.Instance;
            musica.StartCoroutine(musica.FadeOut());
        }
    }

    /// <summary>
    /// Saca la camara de la jerarquia del jugador. Con true conserva su posicion de
    /// mundo, asi que se queda exactamente donde estaba y deja de seguir a nadie.
    /// </summary>
    void SoltarLaCamara()
    {
        if (Camara == null) return;

        // [CAMBIO] El ORDEN es lo importante y antes estaba al reves.
        //
        // DollyZoomEffect.OnDisable() llama a Restaurar(), que hace
        // transform.localPosition = posicionBase, y esa base la capturo en su Awake como
        // posicion LOCAL bajo CuboPequeno: (0, 4.83, -9.13). Si primero desenganchamos y
        // luego lo desactivamos, esa local se aplica sin padre y se convierte en posicion
        // de MUNDO: la camara se teletransportaba a z = -9 y dejaba el carrito a lo lejos.
        //
        // Apagandolo mientras sigue colgado del jugador, restaura su sitio de siempre, y
        // solo entonces la sacamos con worldPositionStays para que se quede ahi clavada.
        DollyZoomEffect dolly = Camara.GetComponent<DollyZoomEffect>();
        if (dolly != null) { dolly.CancelarManual(); dolly.enabled = false; }

        Camara.SetParent(null, true);
    }

    void OcultarLasManos()
    {
        if (Manos != null) Manos.SetActive(false);
    }

    /// <summary>El conjunto sigue solo hacia delante, perdiendo fuelle.</summary>
    IEnumerator Avanzar()
    {
        if (ConjuntoQueCae == null) yield break;

        float velocidad = VelocidadDePartida();
        float t = 0f;

        while (t < SegundosAvanzando)
        {
            t += Time.deltaTime;
            float p = SegundosAvanzando <= 0f ? 1f : Mathf.Clamp01(t / SegundosAvanzando);
            ConjuntoQueCae.position += Vector3.forward * velocidad * Frenada.Evaluate(p) * Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>Se va de lado, sin dejar de deslizarse un poco.</summary>
    IEnumerator Caer()
    {
        if (ConjuntoQueCae == null) yield break;

        Quaternion desde = ConjuntoQueCae.rotation;
        Quaternion hasta = Quaternion.AngleAxis(GradosDeCaida, EjeDeCaida.normalized) * desde;

        float restante = VelocidadDePartida() * Frenada.Evaluate(1f);
        float t = 0f;

        while (t < SegundosCayendo)
        {
            t += Time.deltaTime;
            float p = SegundosCayendo <= 0f ? 1f : Mathf.Clamp01(t / SegundosCayendo);

            ConjuntoQueCae.rotation = Quaternion.SlerpUnclamped(desde, hasta, CurvaDeCaida.Evaluate(p));

            // Sigue deslizandose mientras cae, y se para justo al tocar el suelo.
            ConjuntoQueCae.position += Vector3.forward * restante * (1f - p) * Time.deltaTime;
            yield return null;
        }

        ConjuntoQueCae.rotation = hasta;
    }

    float VelocidadDePartida()
    {
        BasicMovement m = GetComponent<BasicMovement>();
        return m != null ? m.PlayerSpeed : 24f;
    }

    IEnumerator FundirANegro()
    {
        if (TextosAOcultar != null) TextosAOcultar.alpha = 0f;
        if (PanelNegro != null) PanelNegro.SetActive(true);
        if (Negro == null) yield break;

        Color c = Negro.color;
        c.a = 0f;
        Negro.color = c;

        float t = 0f;
        while (t < DuracionFundido)
        {
            t += Time.deltaTime;
            c.a = DuracionFundido <= 0f ? 1f : Mathf.Clamp01(t / DuracionFundido);
            Negro.color = c;
            yield return null;
        }

        c.a = 1f;
        Negro.color = c;
    }
}
