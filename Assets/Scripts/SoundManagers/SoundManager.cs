using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource SFX_2DObject;
    [SerializeField] private AudioSource SFX_3DObject;

    //SFX 3D (Atenuación con distancia)
    public void Play3D_SFX(AudioClip[] audioClip, Transform spawnTransform, bool randomize = false, float volume = 1f, float spatial = 1f)
    {
        AudioSource audioSource = Instantiate(SFX_3DObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip[Random.Range(0, audioClip.Length)];

        if(randomize) audioSource.pitch = (float)Random.Range(8, 12) / 10;

        audioSource.spatialBlend = spatial;

        audioSource.volume = volume;

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    //SFX 2D (Global)
    public AudioSource Play2D_SFX(AudioClip[] audioClip, bool randomize = false, float volume = 1f)
    {
        AudioSource audioSource = Instantiate(SFX_2DObject, new Vector3(0,0,0), Quaternion.identity);

        audioSource.clip = audioClip[Random.Range(0, audioClip.Length)];

        if(randomize) audioSource.pitch = (float)Random.Range(8, 12) / 10;

        audioSource.volume = volume;

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);

        return audioSource;
    }
}
