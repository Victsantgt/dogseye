using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UItabScript : MonoBehaviour
{
    [SerializeField] private Transform m_transform;

    private static int screenIndex = 0; // Tab activa compartida por todas
    private static event Action OnTabChanged; // Evento para notificar a todas las tabs

    private Image img;
    private int tabIndex;
    private float pos0x;
    private float transitionTime = 0.2f;
    private float tabMovement = 24f;

    private Color orange;
    private Color pink;

    void Start()
    {
        pos0x = m_transform.position.y;
        tabIndex = Int32.Parse(gameObject.tag); // Asigna el tag como índice del tab

        ColorUtility.TryParseHtmlString("#FFF6B6", out orange);
        ColorUtility.TryParseHtmlString("#FFB6F6", out pink);
        img = GetComponent<Image>();

        // Suscribirse al evento para refrescar la posición cuando cambie el tab
        OnTabChanged += Movement;

        // Ajustar la posición inicial
        Movement();
    }

    void OnDestroy()
    {
        OnTabChanged -= Movement;
    }

    void MovementDown()
    {
        m_transform.DOMoveY(pos0x - tabMovement, transitionTime);
        if (tabIndex == 0) img.color = orange;
        if (tabIndex == 1) img.color = pink;
    }

    void MovementUp()
    {
        m_transform.DOMoveY(pos0x, transitionTime);
        img.color = Color.white;
    }

    void Movement()
    {
        if (tabIndex == screenIndex)
            MovementDown();
        else
            MovementUp();
    }

    void Update()
    {
        // Navegación con teclado
        if (Input.GetKeyDown("e") && screenIndex == 0)
        {
            screenIndex++;
            OnTabChanged?.Invoke();
        }
        if (Input.GetKeyDown("q") && screenIndex == 1)
        {
            screenIndex--;
            OnTabChanged?.Invoke();
        }
    }

    // Botones
    public void ClickJuego()
    {
        screenIndex = 0;
        OnTabChanged?.Invoke();
    }
    public void ClickSonido()
    {
        screenIndex = 1;
        OnTabChanged?.Invoke();
    }
}
