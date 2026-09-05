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

    [Header("Texto principal: uno por cada final posible")]
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

    private float EsperaAntesDeTerminar = 5f;

    private bool terminando = false;
    public bool IntentarTerminar()
    {
        if (terminado) return true;
        if (decisiones.Count < DecisionesParaFinal) return false;

        if (!terminando)
        {
            terminando = true;
            StartCoroutine(TerminarConEspera(EsperaAntesDeTerminar));
        }

        return true;
    }

    private IEnumerator TerminarConEspera(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        Terminar();
    }

    [ContextMenu("Terminar partida (prueba)")]
    void TerminarDesdeMenu()
    {
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
        if (TextoFinalUI != null)
            TextoFinalUI.text = TextoDeLaSecuencia();

        if (TextoResumenUI != null)
            TextoResumenUI.text = TextoDelResumen();

        if (TextoReinicioUI != null)
            TextoReinicioUI.text = TextoReinicio;
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

    /// <summary>Recarga la escena actual. Publica por si quieres un boton de UI.</summary>
    public void Reiniciar()
    {
        // MusicManager es un ASingleton con DontDestroyOnLoad: sobrevive a la recarga
        // y seguiria sonando por encima de la partida nueva si no la paramos aqui.
        if (PararLaMusicaAlReiniciar && MusicManager.Instance != null)
            MusicManager.Instance.StopMusic();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
