using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MixerManager : MonoBehaviour
{

    [SerializeField] private AudioMixer audioMixer;

    public void SetMasterVolume(float level)
    {
        audioMixer.SetFloat("masterVolume", Mathf.Log10(level) * 20f);
    }

    public void SetBGMVolume(float level)
    {
        audioMixer.SetFloat("bgmVolume", Mathf.Log10(level) * 20f);
    }

    public void SetSFXVolume(float level)
    {
        audioMixer.SetFloat("sfxVolume", Mathf.Log10(level) * 20f);
    }

    public void SetEffectValue(string parameterName, float value)
    {
        audioMixer.SetFloat(parameterName, value);
    }

    public IEnumerator TransitionEffect(string parameterName, float startValue, float targetValue, float duration)
    {
        float elapsedTime = 0f;
        SetEffectValue(parameterName, startValue);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentValue = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);
            SetEffectValue(parameterName, currentValue);
            yield return null;
        }
        SetEffectValue(parameterName, targetValue);
    }

    public void StartTransition(string parameterName, float targetValue, float duration)
    {
        audioMixer.GetFloat(parameterName, out float currentValue);
        StartCoroutine(TransitionEffect(parameterName, currentValue, targetValue, duration));
    }
    public void PauseFilterStart()
    {
        StartTransition("lowpass", 300, 0.5f);
    }

    public void PauseFilterEnd()
    {
        StartTransition("lowpass", 5000, 1f);
    }
}
