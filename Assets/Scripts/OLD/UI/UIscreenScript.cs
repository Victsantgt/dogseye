using DG.Tweening;
using UnityEngine;

public class UIscreenScript : MonoBehaviour
{

    [SerializeField] private Transform m_transform;

    private int screenIndex = 0;
    private float transitionTime = 0.2f;
    private float pos0x;
    private int screens = 2;
    private int screenWidth = 1933;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pos0x = m_transform.position.x;
    }

    void Update()
    {
        if (Input.GetKeyDown("q"))
        {
            MovementLeft();
        }

        if (Input.GetKeyDown("e"))
        {
            MovementRight();
        }
    }
    void MovementRight()
    {
        if (screenIndex < screens-1)
        {
            screenIndex++;
            m_transform.DOMoveX(pos0x - (screenIndex) * screenWidth, transitionTime);

        }
    }

    void MovementLeft()
    {
        if (screenIndex > 0)
        {
            screenIndex--;
            m_transform.DOMoveX(pos0x - (screenIndex) * screenWidth, transitionTime);
        }
    }

    public void ClickJuego()
    {
        if (screenIndex == 1)
        {
            MovementLeft();
        }
    }

    public void ClickSonido()
    {
        if (screenIndex == 0)
        {
            MovementRight();
        }
    }
}
