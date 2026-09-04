using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SliderScript : MonoBehaviour
{

    public TextMeshProUGUI uiText;
    public Slider slider;

    void Update()
    {
        if (gameObject.CompareTag("NoteSlider")) 
        { 
            uiText.text = (slider.value / 10).ToString() + " s";
            return;
        }

        int value = (int)(slider.value * 100);

        uiText.text = value.ToString() + "%";
    }
}
