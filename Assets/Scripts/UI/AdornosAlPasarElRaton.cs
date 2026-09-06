using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// [ANADIDO: adornos del menu] Ensena unos objetos mientras el raton esta encima de este
/// boton y los esconde al salir.
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

    void Awake()
    {
        if (OcultarAlEmpezar) Mostrar(false);
    }

    void OnDisable()
    {
        Mostrar(false);
    }

    public void OnPointerEnter(PointerEventData evento)
    {
        Mostrar(true);
    }

    public void OnPointerExit(PointerEventData evento)
    {
        Mostrar(false);
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
