using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    [SerializeField] private InputActionReference key;
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
        if (key.action.WasPressedThisFrame())
        {
            _button.OnPointerDown(pointerEventData);
            _button.onClick.Invoke();
            _text.color = pressedColor;
        }
        else if (key.action.WasReleasedThisFrame())
        {
            _button.OnPointerUp(pointerEventData);
            _button.onClick.Invoke();
            _text.color = normalColor;
        }
    }
}
