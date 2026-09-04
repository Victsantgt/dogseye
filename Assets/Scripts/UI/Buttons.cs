using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    [SerializeField] private KeyCode key = KeyCode.None;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor;

    private PointerEventData pointerEventData;
    private Button _button;
    private TextMeshProUGUI _text;

    void Awake()
    {
        _button = GetComponent<Button>();
        pointerEventData = new PointerEventData(EventSystem.current);
        _text = GetComponentInChildren<TextMeshProUGUI>();

    }
    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            _button.OnPointerDown(pointerEventData);
            _button.onClick.Invoke();
            _text.color = pressedColor;
        }
        else if (Input.GetKeyUp(key))
        {
            _button.OnPointerUp(pointerEventData);
            _button.onClick.Invoke();
            _text.color = normalColor;
        }
    }
}
