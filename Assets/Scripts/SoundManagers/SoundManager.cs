using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource SFX_Object;

    public AudioSource Play_SFX(AudioClip[] audioClip, bool randomize = false, float volume = 1f)
    {
        AudioSource audioSource = Instantiate(SFX_Object, new Vector3(0,0,0), Quaternion.identity);

        audioSource.clip = audioClip[Random.Range(0, audioClip.Length)];

        if(randomize) audioSource.pitch = (float)Random.Range(8, 12) / 10;

        audioSource.volume = volume;

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);

        return audioSource;
    }
}
