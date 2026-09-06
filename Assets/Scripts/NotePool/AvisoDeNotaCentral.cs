using UnityEngine;

/// <summary>
/// [ANADIDO: aviso de la nota central] Cambia el dibujo de la nota del medio mientras
/// pulsarla cuenta como acierto, y lo devuelve a su animacion normal al salir de ahi.
///
/// QUE HACE. La nota del medio corre su animacion de siempre (A2 &lt;-&gt; A3, el clip
/// PersonaA). Cuando entra en la ventana de acierto se congela y se ensena A4, que es el
/// mismo personaje mirando hacia atras preocupado. Al salir de la ventana se vuelve a
/// soltar la animacion. Es un aviso de "ahora", no un adorno: si aparece, es que pulsar
/// puntua.
///
/// DE DONDE SALE LA VENTANA. Es la misma cuenta que hace Note.OnPlayerHit para puntuar:
/// la distancia en Z entre la nota y su perfectMark. Ahi, 2.5 unidades es el limite de
/// Bad, o sea el ultimo resultado que todavia cuenta como acierto; mas alla ya es Miss.
/// Por eso DistanciaDeAviso vale 2.5 por defecto.
///
/// OJO, LOS DOS NUMEROS VAN APAREJADOS. El 2.5 esta escrito a mano en Note.OnPlayerHit y
/// aqui: no hay forma de que uno lea al otro sin tocar Note.cs, que es un archivo que se
/// reescribe a menudo. Si alguien cambia los rangos de puntuacion, hay que cambiar
/// tambien este campo o el aviso mentira (se veria la cara preocupada cuando pulsar ya
/// no puntua, o al reves).
///
/// NO SE USA LA CAJA DEL DetectorNotaCentral a proposito, aunque seria comodo. Esa caja
/// es hoy de 6 unidades de fondo, o sea +-3 alrededor de la marca, medio unidad mas
/// ancha por cada lado que la ventana que puntua. Sirve para registrar la nota, pero si
/// el aviso fuera por ella se encenderia en un tramo donde pulsar todavia da Miss.
///
/// COMO SE CONGELA. Apagando el Animator, no con un estado nuevo en el controlador. El
/// clip escribe m_Sprite cada frame, asi que sin apagarlo el cambio duraria un frame. Al
/// volver a encenderlo la animacion arranca de cero (el prefab tiene
/// KeepAnimatorStateOnDisable a 0), que para un bucle de dos fotogramas da igual.
///
/// PARA QUITARLO: borra este componente del prefab NoteMiddle. Nada mas depende de el.
/// </summary>
[DisallowMultipleComponent]
public class AvisoDeNotaCentral : MonoBehaviour
{
    [Header("A quien se le cambia el dibujo")]
    [Tooltip("SpriteRenderer del muneco de la nota. Si se deja vacio se busca en los hijos.")]
    public SpriteRenderer Dibujo;

    [Tooltip("Animator que lleva el bucle A2-A3. Si se deja vacio se busca en los hijos. Se apaga mientras dura el aviso.")]
    public Animator Animacion;

    [Header("Que se ensena")]
    [Tooltip("Sprite del aviso: el personaje mirando hacia atras. Sin esto el componente no hace nada.")]
    public Sprite SpriteDeAviso;

    [Header("Cuando se ensena")]
    [Tooltip("Distancia en Z a la marca de acierto dentro de la cual pulsar puntua. TIENE QUE VALER LO MISMO que el limite de Bad de Note.OnPlayerHit, hoy 2.5.")]
    public float DistanciaDeAviso = 2.5f;

    [Tooltip("Deja rastro en consola cada vez que se enciende o se apaga el aviso. Para ajustar la ventana; desmarcalo despues.")]
    public bool LogAlCambiar = false;

    Note nota;
    Sprite spriteNormal;
    bool avisando;

    void Reset()
    {
        Dibujo = GetComponentInChildren<SpriteRenderer>(true);
        Animacion = GetComponentInChildren<Animator>(true);
    }

    void Awake()
    {
        nota = GetComponent<Note>();
        if (Dibujo == null) Dibujo = GetComponentInChildren<SpriteRenderer>(true);
        if (Animacion == null) Animacion = GetComponentInChildren<Animator>(true);
    }

    void OnEnable()
    {
        // La nota sale del pool reutilizada. Si la anterior murio con el aviso puesto,
        // esta arrancaria congelada en A4 y sin animacion.
        avisando = false;
        if (Animacion != null) Animacion.enabled = true;
    }

    void OnDisable()
    {
        // Se deja el muneco como estaba antes de guardarla en el pool.
        if (avisando) Poner(false);
    }

    void Update()
    {
        if (Dibujo == null || SpriteDeAviso == null) return;

        // El perfectMark no se puede cachear en OnEnable: el NoteSpawner lo asigna
        // DESPUES de sacar la nota del pool, o sea despues de que este OnEnable corra.
        Transform marca = nota != null ? nota.perfectMark : null;
        if (marca == null) return;

        float distancia = Mathf.Abs(marca.position.z - transform.position.z);
        bool deberia = distancia <= DistanciaDeAviso;

        if (deberia != avisando) Poner(deberia);
    }

    void Poner(bool aviso)
    {
        avisando = aviso;

        if (aviso)
        {
            // Se guarda el fotograma que estuviera puesto para poder devolverlo tal cual
            // y que no parpadee en el frame que va entre soltar el sprite y el primer
            // fotograma que escriba el Animator al reactivarse.
            spriteNormal = Dibujo.sprite;
            if (Animacion != null) Animacion.enabled = false;
            Dibujo.sprite = SpriteDeAviso;
        }
        else
        {
            if (spriteNormal != null) Dibujo.sprite = spriteNormal;
            if (Animacion != null) Animacion.enabled = true;
        }

        if (LogAlCambiar)
            Debug.Log("AvisoDeNotaCentral: " + (aviso ? "ENCENDIDO" : "apagado"), this);
    }
}
