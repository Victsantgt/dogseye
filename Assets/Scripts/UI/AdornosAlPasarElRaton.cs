using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [ANADIDO: adornos del menu] Ensena unos objetos mientras el raton esta encima de este
/// boton y los esconde al salir, y suelta un sonido al entrar.
///
/// El componente no crea ni coloca nada: solo enciende y apaga lo que se le pase en la
/// lista. Los adornos son hijos normales del boton, asi que se mueven, se escalan y se
/// giran desde el editor como cualquier otra imagen. Si manana el adorno del boton de
/// jugar tiene que ser otro dibujo, se cambia el Sprite del hijo y ya esta, aqui no se
/// toca nada.
///
/// Se apagan tambien en OnDisable porque si el boton se desactiva con el raton encima
/// nunca llega el OnPointerExit, y el adorno se quedaria colgado la proxima vez que el
/// boton vuelva a aparecer.
///
/// [ANADIDO: sonido al pasar el raton] El clip suena SOLO al entrar, no al salir ni
/// mientras se esta encima: OnPointerEnter llega una vez por entrada, asi que lo unico
/// que puede repetirlo es barrer el raton por el borde, y ahi repetirlo es lo que se
/// espera.
///
/// El altavoz se monta solo en el propio boton si no se le da uno. Se hace asi en vez de
/// tirar del AudioSource de la musica del menu, para no dejarlo a merced de como este
/// configurado ese, y en vez de usar MusicManager, que es un singleton que vive en la
/// escena del juego y aqui no existe. Va con spatialBlend a 0, o sea sonido 2D: si no,
/// Unity lo colocaria en el mundo y el volumen dependeria de donde caiga el boton
/// respecto a la camara.
///
/// [ANADIDO: corte del clip de hover] SegundosMaximos existe porque los clips del
/// proyecto no estan cortados para UI: ding.ogg dura 2.73 s, de los cuales el golpe se
/// acaba antes de medio segundo y el resto es cola cada vez mas floja. Como aviso de
/// hover eso se hace eterno y, si barres el raton por varios botones, se apilan colas.
/// El corte se hace AQUI y no recortando el .ogg a proposito: el audio es un asset
/// compartido que puede estar usandose en otro sitio, y ademas asi el punto de corte se
/// prueba moviendo un numero en el inspector.
///
/// PARA QUITARLO: borra este componente del boton y sus hijos adorno. Nada mas depende
/// de el.
/// </summary>
[DisallowMultipleComponent]
public class AdornosAlPasarElRaton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Objetos que se encienden al pasar el raton por encima del boton y se apagan al salir.")]
    public List<GameObject> Adornos = new List<GameObject>();

    [Tooltip("Los apaga al arrancar la escena, por si se han dejado visibles en el editor para colocarlos.")]
    public bool OcultarAlEmpezar = true;

    // [ANADIDO: sonido al pasar el raton]
    [Header("Sonido")]
    [Tooltip("Clip que suena al entrar el raton en el boton. Dejalo vacio y el boton no suena.")]
    public AudioClip SonidoAlEntrar;

    [Tooltip("Volumen del clip del hover.")]
    [Range(0f, 1f)]
    public float Volumen = 1f;

    // [ANADIDO: corte del clip de hover]
    [Tooltip("Segundos que se deja sonar el clip antes de empezar a apagarlo. En 0 suena entero. Ponlo para clips largos que como aviso de hover se hacen pesados.")]
    public float SegundosMaximos = 0f;

    [Tooltip("Fundido con el que se apaga al llegar al corte. Cortar en seco a mitad de la onda hace 'clic'.")]
    public float SegundosDeFundido = 0.15f;

    [Tooltip("Altavoz por el que sale el clip. Si se deja vacio se anade uno a este mismo boton al arrancar.")]
    public AudioSource Altavoz;

    Coroutine corte;

    void Awake()
    {
        if (OcultarAlEmpezar) Mostrar(false);

        // [ANADIDO: sonido al pasar el raton] Solo se monta el altavoz si este boton
        // tiene algo que sonar, para no dejar AudioSources sueltos en botones mudos.
        if (SonidoAlEntrar != null && Altavoz == null)
        {
            Altavoz = gameObject.AddComponent<AudioSource>();
            Altavoz.playOnAwake = false;
            Altavoz.loop = false;
            Altavoz.spatialBlend = 0f;
        }
    }

    void OnDisable()
    {
        Mostrar(false);

        // [ANADIDO: corte del clip de hover] Si el boton se apaga a mitad del fundido, el
        // altavoz se quedaria guardado con el volumen a medias para la proxima vez.
        corte = null;
        if (Altavoz != null)
        {
            Altavoz.Stop();
            Altavoz.volume = Volumen;
        }
    }

    public void OnPointerEnter(PointerEventData evento)
    {
        Mostrar(true);
        Sonar();
    }

    public void OnPointerExit(PointerEventData evento)
    {
        Mostrar(false);
    }

    /// <summary>
    /// [ANADIDO: sonido al pasar el raton] Suelta el clip del hover.
    ///
    /// Hay dos caminos a proposito. Sin corte se usa PlayOneShot, que deja que dos
    /// entradas seguidas se solapen. Con corte hace falta poder pararlo, y PlayOneShot no
    /// da forma de parar una instancia suelta, asi que se toca el AudioSource entero: ahi
    /// una entrada nueva reinicia la anterior en vez de sumarse, que para un aviso corto
    /// es justo lo que se quiere.
    /// </summary>
    public void Sonar()
    {
        if (SonidoAlEntrar == null || Altavoz == null) return;

        if (SegundosMaximos <= 0f)
        {
            Altavoz.volume = 1f;
            Altavoz.PlayOneShot(SonidoAlEntrar, Volumen);
            return;
        }

        if (corte != null) StopCoroutine(corte);

        Altavoz.Stop();
        Altavoz.clip = SonidoAlEntrar;
        Altavoz.volume = Volumen;
        Altavoz.Play();
        corte = StartCoroutine(CortarElClip());
    }

    IEnumerator CortarElClip()
    {
        // Sin escalar: el menu no toca el timeScale, pero si algun dia se pausa el juego
        // desde aqui el sonido de la interfaz no tiene por que congelarse con el.
        float t = 0f;
        while (t < SegundosMaximos)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        float fundido = Mathf.Max(0.01f, SegundosDeFundido);
        float u = 0f;
        while (u < fundido)
        {
            u += Time.unscaledDeltaTime;
            Altavoz.volume = Mathf.Lerp(Volumen, 0f, u / fundido);
            yield return null;
        }

        Altavoz.Stop();
        Altavoz.volume = Volumen;
        corte = null;
    }

    /// <summary>Publica por si alguien quiere ensenarlos desde otro sitio (mando, teclado...).</summary>
    public void Mostrar(bool visible)
    {
        for (int i = 0; i < Adornos.Count; i++)
        {
            GameObject adorno = Adornos[i];
            if (adorno != null) adorno.SetActive(visible);
        }
    }
}
