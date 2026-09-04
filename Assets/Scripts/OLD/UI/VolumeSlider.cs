using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{

    public TextMeshProUGUI uiText;
    public Slider slider;

    void Update()
    {
        int value = (int)(slider.value * 100);

        uiText.text = value.ToString() + "%";
    }
}
