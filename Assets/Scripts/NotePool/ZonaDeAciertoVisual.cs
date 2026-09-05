using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Rectangulo semitransparente que marca la zona donde una nota se puede pulsar.
///
/// Va en el mismo GameObject que el ColliderNoteScript (ColliderNotas) y se dibuja
/// copiando su BoxCollider, que es EXACTAMENTE lo que decide si una pulsacion cuenta:
/// el ColliderNoteScript guarda la nota en OnTriggerEnter y la suelta en OnTriggerExit,
/// asi que mientras la nota este dentro de esa caja la tecla la acierta, y fuera no.
/// Al leerlo del collider, si alguien reajusta la ventana el rectangulo la sigue sin
/// tener que tocar nada aqui.
///
/// El rectangulo se planta en el plano XY (el ancho de la zona por su alto), que es
/// por donde cae la nota. La caja tiene ademas 3 unidades de fondo en Z, que no se
/// dibujan porque la nota no se mueve en ese eje.
///
/// Cuelga de -- RHYTHM SYSTEM --, o sea que se apaga durante todo el puente. Eso es lo
/// que queremos: en el puente no hay notas que pulsar, asi que la guia sobra.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public class ZonaDeAciertoVisual : MonoBehaviour
{
    [Header("Aspecto")]
    [Tooltip("Color y transparencia del rectangulo. El canal alfa es lo que lo hace translucido: 0 invisible, 1 opaco del todo.")]
    public Color Color = new Color(1f, 0.85f, 0.3f, 0.35f);

    [Tooltip("Opcional. Si lo dejas vacio se crea uno transparente de URP en tiempo de ejecucion.")]
    public Material MaterialPersonalizado;

    [Tooltip("Cuanto se encoge o crece el rectangulo respecto al collider, en unidades. Negativo lo mete hacia dentro. Solo es estetico: la zona que cuenta sigue siendo la del collider.")]
    public Vector2 Margen = Vector2.zero;

    [Header("Visibilidad")]
    [Tooltip("Enciende y apaga la guia. Se puede cambiar en marcha.")]
    public bool Visible = true;

    [Tooltip("Dibuja tambien el volumen entero de la caja en la vista de escena, con los 3 de fondo incluidos. No se ve en el juego.")]
    public bool GizmoEnLaEscena = true;

    BoxCollider caja;
    Transform rectangulo;
    Material material;

    void Awake()
    {
        caja = GetComponent<BoxCollider>();
        Construir();
    }

    void LateUpdate()
    {
        if (rectangulo == null) return;

        // Se recalcula cada frame en vez de una sola vez para que se pueda ajustar el
        // collider o el color con el juego corriendo y verlo al momento.
        Ajustar();

        if (rectangulo.gameObject.activeSelf != Visible)
            rectangulo.gameObject.SetActive(Visible);
    }

    /// <summary>Enciende o apaga la guia desde codigo o desde un UnityEvent.</summary>
    public void Mostrar(bool visible)
    {
        Visible = visible;

        if (rectangulo != null)
            rectangulo.gameObject.SetActive(visible);
    }

    void Construir()
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "ZonaDeAcierto (generado)";

        // CreatePrimitive trae un MeshCollider de regalo. Aqui estorbaria: la nota
        // entraria en el y no queremos que la guia participe en la fisica.
        Collider sobra = quad.GetComponent<Collider>();
        if (sobra != null) Destroy(sobra);

        quad.transform.SetParent(transform, false);
        rectangulo = quad.transform;

        MeshRenderer mr = quad.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.material = MaterialPersonalizado != null ? MaterialPersonalizado : CrearMaterialTransparente();
        material = mr.material;

        Ajustar();
        quad.SetActive(Visible);
    }

    void Ajustar()
    {
        if (caja == null) return;

        // Se trabaja en MUNDO, no en local, porque ColliderNotas esta rotado: su size
        // local es (3, 5.1, 20.6) y el ancho de la zona cae en su eje Z, no en el X.
        // Usando bounds nos da igual como este girado el objeto.
        //
        // bounds es la caja alineada a ejes de mundo. Con la rotacion actual (multiplo
        // de 90 grados) coincide exactamente con la caja real; si algun dia se gira en
        // diagonal, saldria algo mas grande que la zona de verdad.
        Bounds b = caja.bounds;

        rectangulo.position = b.center;

        // Rotacion de mundo a identidad: el rectangulo queda plantado en el plano XY,
        // de cara a la camara, que va por detras del jugador mirando hacia +Z. El
        // material va sin culling, asi que se ve por los dos lados.
        rectangulo.rotation = Quaternion.identity;

        // El padre no tiene escala, asi que la rotacion local que compensa la del padre
        // deja los ejes de escala alineados con el mundo y no hay cizalla.
        rectangulo.localScale = new Vector3(
            Mathf.Max(0f, b.size.x + Margen.x),
            Mathf.Max(0f, b.size.y + Margen.y),
            1f);

        if (material != null && MaterialPersonalizado == null)
            AplicarColor(material, Color);
    }

    Material CrearMaterialTransparente()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("ZonaDeAciertoVisual: no se ha encontrado ningun shader para el rectangulo. Asigna un material a mano en MaterialPersonalizado.", this);
            return null;
        }

        Material m = new Material(shader);
        m.name = "ZonaDeAcierto (generado)";

        // Configuracion de transparencia de URP/Unlit. Sin esto el shader sale opaco
        // aunque el color lleve alfa, porque el modo de superficie es una propiedad
        // del material, no del color.
        m.SetOverrideTag("RenderType", "Transparent");
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);   // 1 = Transparent
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);       // 0 = Alpha
        if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = (int)RenderQueue.Transparent;

        AplicarColor(m, Color);
        return m;
    }

    static void AplicarColor(Material m, Color c)
    {
        if (m == null) return;

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    void OnDrawGizmos()
    {
        if (!GizmoEnLaEscena) return;

        BoxCollider c = caja != null ? caja : GetComponent<BoxCollider>();
        if (c == null) return;

        // El rectangulo de verdad se crea en Awake, o sea que en modo edicion no hay
        // nada que ver. El gizmo cubre ese hueco y ademas ensena el fondo en Z, que el
        // rectangulo no dibuja.
        Gizmos.matrix = transform.localToWorldMatrix;

        Color relleno = Color;
        relleno.a *= 0.5f;
        Gizmos.color = relleno;
        Gizmos.DrawCube(c.center, c.size);

        Color borde = Color;
        borde.a = 1f;
        Gizmos.color = borde;
        Gizmos.DrawWireCube(c.center, c.size);
    }
}
