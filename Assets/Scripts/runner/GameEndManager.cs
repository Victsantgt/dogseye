using System.Collections;
using System.Collections.Generic;
using Patterns.Singleton;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Final de partida. Cuenta las decisiones que va tomando el jugador y, al llegar a
/// DecisionesParaFinal, apaga el resto del juego, funde a blanco y deja en pantalla
/// el texto del final correspondiente hasta que se pulsa la tecla de reinicio.
/// </summary>
[DisallowMultipleComponent]
public class GameEndManager : MonoBehaviour
{
    /// <summary>Un final escrito para una secuencia exacta de decisiones.</summary>
    [System.Serializable]
    public class FinalPorSecuencia
    {
        [Tooltip("Secuencia exacta y en orden. I = izquierda, D = derecha.")]
        public string Secuencia = "III";

        [TextArea(2, 6)]
        public string Texto = "";
    }

    [Header("Cuando termina")]
    [Tooltip("Decisiones que se juegan. La partida acaba en cuanto se CONTESTA la ultima: se va directa al fundido en blanco, sin cambio de terreno ni seccion musical extra.")]
    public int DecisionesParaFinal = 3;

    [Header("Fundido a blanco")]
    [Tooltip("Objeto que contiene el blanco y los textos. Se activa al terminar.")]
    public GameObject PanelFinal;

    [Tooltip("Imagen blanca a pantalla completa.")]
    public Image Blanco;

    [Tooltip("Grupo con los textos, para que aparezcan despues del blanco.")]
    public CanvasGroup GrupoTextos;

    [Tooltip("Segundos que tarda la pantalla en ponerse blanca.")]
    public float DuracionFundido = 1.5f;

    [Tooltip("Segundos que tardan los textos en aparecer, ya sobre el blanco.")]
    public float DuracionFundidoTexto = 0.8f;

    // [ANADIDO: imagen de final] Segun lo que se haya elegido durante la partida sale
    // una de tres laminas. Va dentro del grupo de textos a proposito: ese grupo ya se
    // funde despues del blanco, asi que la imagen entra con el mismo fundido y no hace
    // falta ninguna corrutina nueva.
    /// <summary>
    /// [ANADIDO: imagen de final] Un final: su lamina y su texto, juntos.
    ///
    /// Estan en la misma ficha a proposito, para que quien escriba los textos vea al
    /// lado la imagen a la que acompanan y no haya que cruzar dos listas.
    /// </summary>
    [System.Serializable]
    public class Final
    {
        [Tooltip("Solo informativo, para saber cual es cual en el Inspector.")]
        public string Nombre = "";

        [Tooltip("Lamina a pantalla completa de este final.")]
        public Sprite Lamina;

        [Tooltip("Texto de este final. Sale sobre la lamina, en la banda inferior, por encima del 'pulsa R'. Se puede dejar vacio.")]
        [TextArea(3, 8)]
        public string Texto = "";
    }

    [Header("Imagen del final")]
    [Tooltip("Imagen a pantalla completa donde se pone la lamina del final. Cuelga del grupo de textos para heredar su fundido.")]
    public Image ImagenFinal;

    [Tooltip("Todas las decisiones fueron la opcion Derecha (pasillo estrecho).")]
    public Final Minimalista = new Final { Nombre = "Minimalista (todas Derecha)" };

    [Tooltip("Todas las decisiones fueron la opcion Izquierda (pasillo ancho).")]
    public Final Consumista = new Final { Nombre = "Consumista (todas Izquierda)" };

    [Tooltip("Se han mezclado las dos opciones.")]
    public Final Mixto = new Final { Nombre = "Mixto (mezcladas)" };

    // [ANADIDO: legibilidad del texto] Banda que se pone DETRAS del texto del final.
    //
    // Hace falta porque no hay ningun hueco libre comun a las tres laminas: minimalista
    // y mixto son casi todo blanco con el dibujo en el centro, pero consumista esta
    // llena de coches, regalos y bolsas de arriba abajo, y el humo llega al borde
    // superior. Buscar un sitio despejado no vale para las tres, asi que en vez de
    // esquivar el dibujo se le pone un velo debajo al texto y se lee siempre.
    [Tooltip("Banda semitransparente detras del texto del final, para que se lea sobre cualquier lamina. Opcional.")]
    public Image BandaDelTexto;

    [Tooltip("Opacidad de esa banda. Las laminas son sobre blanco, asi que un blanco a media opacidad apaga el dibujo sin que se note un recuadro duro.")]
    [Range(0f, 1f)] public float OpacidadDeLaBanda = 0.75f;

    [Tooltip("Que opcion lleva al final minimalista. Si algun dia se le da la vuelta al significado de Derecha e Izquierda, esto es lo unico que hay que cambiar.")]
    public DecisionManager.Opcion OpcionMinimalista = DecisionManager.Opcion.Derecha;

    [Header("Texto principal: uno por cada final posible")]
    [Tooltip("Muestra el resumen de conteo sobre la lamina. Desmarcado por defecto: la lamina ya cuenta la historia y el texto la ensucia.")]
    public bool MostrarResumen = false;

    public TextMeshProUGUI TextoFinalUI;

    [Tooltip("Un texto por cada combinacion de decisiones. Usa el menu contextual del componente para generarlas todas vacias.")]
    public List<FinalPorSecuencia> FinalesPorSecuencia = new List<FinalPorSecuencia>();

    [Tooltip("Se usa si la secuencia jugada no tiene texto escrito en la lista de arriba.")]
    [TextArea(2, 6)]
    public string TextoFinalPorDefecto = "";

    [Header("Resumen de conteo")]
    public TextMeshProUGUI TextoResumenUI;

    [Tooltip("Plantilla del resumen. Admite {irte}, {quedarte} y {total}.")]
    [TextArea(2, 4)]
    public string PlantillaResumen = "Has elegido {irte} y {quedarte}.";

    [Tooltip("Como se escribe el recuento de buenas cuando es exactamente 1.")]
    public string FraseDerechaSingular = "irte 1 vez";

    [Tooltip("Como se escribe el recuento de buenas cuando es 0 o mas de 1. {n} es el numero.")]
    public string FraseDerechaPlural = "irte {n} veces";

    [Tooltip("Como se escribe el recuento de malas cuando es exactamente 1.")]
    public string FraseIzquierdaSingular = "quedarte 1 vez";

    [Tooltip("Como se escribe el recuento de malas cuando es 0 o mas de 1. {n} es el numero.")]
    public string FraseIzquierdaPlural = "quedarte {n} veces";

    [Header("Reinicio")]
    public TextMeshProUGUI TextoReinicioUI;

    [TextArea]
    public string TextoReinicio = "pulsa R para reiniciar el juego";

    public Key TeclaReinicio = Key.R;

    [Tooltip("Para la musica antes de recargar. Hace falta porque MusicManager es un singleton con DontDestroyOnLoad y sobrevive al cambio de escena.")]
    public bool PararLaMusicaAlReiniciar = true;

    [Header("Que se apaga al terminar la partida")]
    [Tooltip("Componentes que se desactivan (movimiento, generador, decisiones...).")]
    public List<Behaviour> ComponentesADesactivar = new List<Behaviour>();

    [Tooltip("Objetos enteros que se desactivan (sistema de notas, paneles de UI...).")]
    public List<GameObject> ObjetosADesactivar = new List<GameObject>();

    readonly List<DecisionManager.Opcion> decisiones = new List<DecisionManager.Opcion>();
    bool terminado;
    bool aceptaReinicio;

    /// <summary>True cuando la partida ya ha terminado y esta la pantalla final.</summary>
    public bool Terminado { get { return terminado; } }

    /// <summary>Decisiones tomadas hasta ahora.</summary>
    public int DecisionesTomadas { get { return decisiones.Count; } }

    // [ANADIDO: musica por seccion] MusicaPorSeccion necesita saber que se acaba de
    // elegir. Antes lo sacaba de la ultima letra de SecuenciaActual(), pero eso se rompe
    // en silencio cada vez que alguien renombra el enum Opcion (ya paso al pasar de
    // Buena/Mala a Derecha/Izquierda: la letra cambio de 'B' a 'D' y la musica empezo a
    // elegir siempre la variante contraria). Comparando el enum, un renombrado futuro
    // da error de compilacion en vez de un fallo mudo.
    /// <summary>
    /// La ultima decision tomada. Si todavia no hay ninguna devuelve Derecha, que es la
    /// que cuenta como buena; comprueba DecisionesTomadas antes si eso te importa.
    /// </summary>
    public DecisionManager.Opcion UltimaDecision
    {
        get { return decisiones.Count > 0 ? decisiones[decisiones.Count - 1] : DecisionManager.Opcion.Derecha; }
    }

    void Awake()
    {
        if (PanelFinal != null)
            PanelFinal.SetActive(false);
    }

    /// <summary>
    /// La llama el DecisionManager cada vez que se resuelve una pregunta.
    /// Solo apunta la decision. Quien remata es IntentarTerminar(), que el
    /// DecisionManager llama justo despues de esta.
    /// </summary>
    public void RegistrarDecision(DecisionManager.Opcion elegida)
    {
        if (terminado) return;

        decisiones.Add(elegida);
    }

    /// <summary>
    /// Arranca el final si ya se han jugado todas las decisiones. Se llama desde dos
    /// sitios del DecisionManager:
    ///
    ///   - En Resolver(), justo despues de apuntar la decision. Es el camino normal:
    ///     al contestar la ultima pregunta la partida termina ahi mismo.
    ///   - En LanzarPregunta(), antes de sacar una pregunta. Es una red de seguridad
    ///     para que nadie (por ejemplo la tecla P) saque una pregunta con la partida
    ///     ya terminada.
    /// </summary>
    /// <returns>true si el final ya esta en marcha y por tanto hay que cortar lo que se estuviera haciendo.</returns>

    private bool terminando = false;
    public bool IntentarTerminar()
    {
        if (terminado) return true;
        if (decisiones.Count < DecisionesParaFinal) return false;

        if (!terminando)
        {
            terminando = true;
            Terminar();
        }

        return true;
    }

    [ContextMenu("Terminar partida (prueba)")]
    void TerminarDesdeMenu()
    {
        Terminar();
    }

    // ===================== [ANADIDO: finales de prueba] =====================
    // TEMPORAL. Lo usa el componente FinalesDePrueba para poder ver cada final sin
    // jugarse la partida entera. Para quitarlo de la build: borra el componente
    // FinalesDePrueba de LevelController y, si quieres, este bloque entero.
    // =======================================================================

    /// <summary>Cual de los tres finales se quiere forzar.</summary>
    public enum TipoDeFinal { Minimalista, Consumista, Mixto }

    /// <summary>
    /// [ANADIDO: finales de prueba] Coloca las decisiones necesarias para que salga el
    /// final pedido y termina la partida.
    ///
    /// NO se salta la logica normal: rellena la lista de decisiones y deja que decida
    /// FinalElegido(), el mismo que corre en una partida de verdad. Asi lo que ves
    /// probando es exactamente lo que va a pasar jugando, y no una version paralela que
    /// se pueda desincronizar.
    /// </summary>
    public void TerminarConFinal(TipoDeFinal cual)
    {
        if (terminado) return;

        DecisionManager.Opcion mini = OpcionMinimalista;
        DecisionManager.Opcion cons = mini == DecisionManager.Opcion.Derecha
            ? DecisionManager.Opcion.Izquierda
            : DecisionManager.Opcion.Derecha;

        int total = Mathf.Max(1, DecisionesParaFinal);
        if (cual == TipoDeFinal.Mixto && total < 2)
            Debug.LogWarning("GameEndManager: con " + total + " decision no se puede montar un final mixto.", this);

        decisiones.Clear();
        for (int i = 0; i < total; i++)
        {
            if (cual == TipoDeFinal.Minimalista) decisiones.Add(mini);
            else if (cual == TipoDeFinal.Consumista) decisiones.Add(cons);
            else decisiones.Add(i == 0 ? cons : mini);   // mezcla: al menos una de cada
        }

        Debug.Log("GameEndManager: final de PRUEBA '" + cual + "' con la secuencia " + SecuenciaActual(), this);
        Terminar();
    }

    void Terminar()
    {
        if (terminado) return;
        terminado = true;

        ApagarElJuego();
        RellenarTextos();

        if (PanelFinal != null)
            PanelFinal.SetActive(true);

        StartCoroutine(Fundir());
    }

    void ApagarElJuego()
    {
        for (int i = 0; i < ComponentesADesactivar.Count; i++)
            if (ComponentesADesactivar[i] != null)
                ComponentesADesactivar[i].enabled = false;

        for (int i = 0; i < ObjetosADesactivar.Count; i++)
            if (ObjetosADesactivar[i] != null)
                ObjetosADesactivar[i].SetActive(false);
    }

    void RellenarTextos()
    {
        // [CAMBIO: imagen de final] El texto principal ya no sale de FinalesPorSecuencia
        // sino del propio final elegido, que es donde lo escribe el equipo.
        Final elegido = FinalElegido();

        if (TextoFinalUI != null)
            TextoFinalUI.text = elegido != null ? elegido.Texto : "";

        if (ImagenFinal != null)
        {
            Sprite lamina = elegido != null ? elegido.Lamina : null;
            ImagenFinal.sprite = lamina;
            ImagenFinal.enabled = lamina != null;
        }

        // La banda solo se ve si hay texto que arropar. Sin texto seria un recuadro
        // gris flotando en medio de la lamina sin motivo.
        if (BandaDelTexto != null)
        {
            bool hayTexto = elegido != null && !string.IsNullOrEmpty(elegido.Texto);
            BandaDelTexto.enabled = hayTexto;

            Color c = BandaDelTexto.color;
            c.a = OpacidadDeLaBanda;
            BandaDelTexto.color = c;
        }

        if (TextoResumenUI != null)
            TextoResumenUI.text = MostrarResumen ? TextoDelResumen() : "";

        if (TextoReinicioUI != null)
            TextoReinicioUI.text = TextoReinicio;
    }

    /// <summary>
    /// [ANADIDO: imagen de final] Que final toca segun lo que se haya elegido.
    ///
    /// Se mira si TODAS las decisiones fueron iguales, en vez de comparar contra un 3
    /// fijo: asi sigue valiendo si algun dia se cambia DecisionesParaFinal.
    /// </summary>
    public Final FinalElegido()
    {
        int minimalistas = 0, consumistas = 0;
        for (int i = 0; i < decisiones.Count; i++)
        {
            if (decisiones[i] == OpcionMinimalista) minimalistas++;
            else consumistas++;
        }

        if (decisiones.Count == 0) return Mixto;
        if (consumistas == 0) return Minimalista;
        if (minimalistas == 0) return Consumista;
        return Mixto;
    }

    /// <summary>La secuencia jugada como cadena, por ejemplo "BBM".</summary>
    public string SecuenciaActual()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(decisiones.Count);
        for (int i = 0; i < decisiones.Count; i++)
            sb.Append(decisiones[i] == DecisionManager.Opcion.Derecha ? 'D' : 'I');
        return sb.ToString();
    }

    string TextoDeLaSecuencia()
    {
        string secuencia = SecuenciaActual();

        for (int i = 0; i < FinalesPorSecuencia.Count; i++)
        {
            FinalPorSecuencia f = FinalesPorSecuencia[i];
            if (f == null || string.IsNullOrEmpty(f.Secuencia)) continue;

            if (f.Secuencia.Trim().ToUpperInvariant() == secuencia && !string.IsNullOrEmpty(f.Texto))
                return f.Texto;
        }

        return TextoFinalPorDefecto;
    }

    string TextoDelResumen()
    {
        int buenas = 0;
        for (int i = 0; i < decisiones.Count; i++)
            if (decisiones[i] == DecisionManager.Opcion.Derecha) buenas++;

        int malas = decisiones.Count - buenas;

        return PlantillaResumen
            .Replace("{irte}", Frase(buenas, FraseDerechaSingular, FraseDerechaPlural))
            .Replace("{quedarte}", Frase(malas, FraseIzquierdaSingular, FraseIzquierdaPlural))
            .Replace("{total}", decisiones.Count.ToString());
    }

    // En castellano el 0 lleva plural ("0 opciones buenas"), asi que solo el 1 es singular.
    static string Frase(int n, string singular, string plural)
    {
        string f = n == 1 ? singular : plural;
        return f.Replace("{n}", n.ToString());
    }

    IEnumerator Fundir()
    {
        if (GrupoTextos != null) GrupoTextos.alpha = 0f;

        if (Blanco != null)
        {
            Color c = Blanco.color;
            c.a = 0f;
            Blanco.color = c;

            float t = 0f;
            while (t < DuracionFundido)
            {
                t += Time.deltaTime;
                c.a = DuracionFundido <= 0f ? 1f : Mathf.Clamp01(t / DuracionFundido);
                Blanco.color = c;
                yield return null;
            }

            c.a = 1f;
            Blanco.color = c;
        }

        if (GrupoTextos != null)
        {
            float t2 = 0f;
            while (t2 < DuracionFundidoTexto)
            {
                t2 += Time.deltaTime;
                GrupoTextos.alpha = DuracionFundidoTexto <= 0f ? 1f : Mathf.Clamp01(t2 / DuracionFundidoTexto);
                yield return null;
            }

            GrupoTextos.alpha = 1f;
        }

        aceptaReinicio = true;
    }

    void Update()
    {
        if (!aceptaReinicio) return;

        Keyboard teclado = Keyboard.current;
        if (teclado != null && teclado[TeclaReinicio].wasPressedThisFrame)
            Reiniciar();
    }

    // [ANADIDO: volver al menu] A donde lleva la tecla al terminar la partida. Antes
    // esto recargaba siempre la escena actual; ahora, tras ver el final, se vuelve al
    // menu principal. Dejandolo vacio se recupera el comportamiento anterior.
    [Tooltip("Escena a la que lleva la tecla de reinicio. Vacio = recargar la escena actual, que era lo de antes.")]
    public string EscenaAlPulsarReinicio = "MainMenu";

    /// <summary>
    /// Lleva a EscenaAlPulsarReinicio, o recarga la escena actual si esta vacio.
    /// Publica por si quieres un boton de UI. La usa tambien PlayerDeathManager.
    /// </summary>
    public void Reiniciar()
    {
        // MusicManager es un ASingleton con DontDestroyOnLoad: sobrevive a la recarga
        // y seguiria sonando por encima de la partida nueva si no la paramos aqui.
        if (PararLaMusicaAlReiniciar && MusicManager.Instance != null)
            MusicManager.Instance.StopMusic();

        Time.timeScale = 1f;

        // [CAMBIO: volver al menu]
        SceneManager.LoadScene(string.IsNullOrEmpty(EscenaAlPulsarReinicio)
            ? SceneManager.GetActiveScene().name
            : EscenaAlPulsarReinicio);
    }

    /// <summary>
    /// Rellena FinalesPorSecuencia con TODAS las combinaciones posibles para el
    /// numero de decisiones configurado, con el texto vacio para que lo escribas.
    /// Respeta los textos que ya hubiera escritos para una secuencia.
    /// </summary>
    [ContextMenu("Generar todas las combinaciones")]
    void GenerarCombinaciones()
    {
        if (DecisionesParaFinal < 1 || DecisionesParaFinal > 12)
        {
            Debug.LogError("GameEndManager: DecisionesParaFinal debe estar entre 1 y 12 para generar combinaciones (son 2^n).", this);
            return;
        }

        // nos quedamos con lo ya escrito
        Dictionary<string, string> yaEscritos = new Dictionary<string, string>();
        foreach (FinalPorSecuencia f in FinalesPorSecuencia)
        {
            if (f == null || string.IsNullOrEmpty(f.Secuencia)) continue;
            yaEscritos[f.Secuencia.Trim().ToUpperInvariant()] = f.Texto;
        }

        List<FinalPorSecuencia> nuevas = new List<FinalPorSecuencia>();
        int total = 1 << DecisionesParaFinal;

        for (int i = 0; i < total; i++)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(DecisionesParaFinal);
            for (int bit = DecisionesParaFinal - 1; bit >= 0; bit--)
                sb.Append(((i >> bit) & 1) == 0 ? 'I' : 'D');

            string sec = sb.ToString();
            FinalPorSecuencia f = new FinalPorSecuencia();
            f.Secuencia = sec;
            f.Texto = yaEscritos.ContainsKey(sec) ? yaEscritos[sec] : "";
            nuevas.Add(f);
        }

        FinalesPorSecuencia = nuevas;
        Debug.Log("GameEndManager: generadas " + total + " combinaciones para " + DecisionesParaFinal + " decisiones.", this);
    }
}
