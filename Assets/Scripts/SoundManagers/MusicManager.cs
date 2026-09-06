using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace Patterns.Singleton
{
    public class MusicManager : ASingleton<MusicManager>
    {
        [SerializeField] private AudioClip defaultClip;

        [SerializeField] private AudioSource track01;
        [SerializeField] private AudioSource track02;

        [SerializeField] private AudioSource SFX_Object;

        private bool isPlayingTrack01;
        private bool isMusicPlaying;
        private bool isPreviewing;

        private Coroutine previewCoroutine;

        private Tween previewTween;
        private Tween backgroundTween;

        private AudioSource previewSource;
        private AudioSource backgroundSource;

        private float backgroundBaseVolume = 1f;
        private bool backgroundVolumeStored;

        private float timeSinceBegin;

        private void Start()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            string sceneName = currentScene.name;

            if (isMusicPlaying || sceneName != "mainMenu") return;
            //StopAllCoroutines();
            StartCoroutine(FadeIn(1));
        }

        public void musicStart()
        {
            isPlayingTrack01 = true;
            track01.clip = defaultClip;
            track01.timeSamples = 0;
            track01.Play();
            track01.volume = 1f;
            isMusicPlaying = true;
        }
        public float getVolume()
        {
            float volume;
            if (isPlayingTrack01)
            {
                volume = track01.volume;
            }
            else
            {
                volume = track02.volume;
            }
            return volume;
        }

        public bool IsMusicPlaying() { return isMusicPlaying; }
        public void SwapTrack(AudioClip newClip, bool dynamic = false, float volume = 1)
        {
            if (isMusicPlaying)
            {
                StopAllCoroutines();
                StartCoroutine(FadeTrack(newClip, dynamic, volume));
                isPlayingTrack01 = !isPlayingTrack01;
            }
        }
        public void changeDefault(AudioClip newDefaultClip)
        {
            defaultClip = newDefaultClip;
        }

        public void ReturnToDefault(bool dynamic = false)
        {
            StartCoroutine(ReturnDefaultCoroutine());
        }

        // [ANADIDO: fundido de la intermision] Segundos que baja la musica antes de que
        // entre el tema nuevo. Es tambien lo que se le suma al cero del chart para que
        // las notas no se adelanten a la cancion; ver RetardoAlVolverAlDefault.
        [Header("Fundido al volver al tema por defecto")]
        [SerializeField] private float segundosDeFundidoDeSalida = 1.5f;

        /// <summary>
        /// Cuanto tardara en sonar la musica si AHORA MISMO se llama a ReturnToDefault().
        /// Con musica sonando hay que esperar al fundido de salida; sin nada sonando el
        /// tema entra al momento. Lo lee el ChartManager para colocar el cero del chart.
        /// </summary>
        public float RetardoAlVolverAlDefault
        {
            get { return isMusicPlaying ? segundosDeFundidoDeSalida : 0f; }
        }

        public IEnumerator ReturnDefaultCoroutine()
        {
            // [CAMBIO] Antes esto era "FadeOut();" a secas. FadeOut es un IEnumerator, y
            // llamarlo sin StartCoroutine NO ejecuta nada: solo crea el iterador y lo
            // tira. De ahi los dos sintomas: la intermision no bajaba de volumen, y
            // ademas se quedaba sonando POR DEBAJO del tema nuevo para siempre, porque
            // el Stop() del final de FadeOut tampoco llegaba a correr nunca.
            //
            // Con el yield return esperamos a que el fundido termine de verdad y solo
            // entonces entra el tema siguiente, que es el orden que se buscaba.
            yield return StartCoroutine(FadeOut(segundosDeFundidoDeSalida));

            musicStart();
        }

        public float GetTime() { return track01.time; }

        public bool GetTrack1Playing() { return isPlayingTrack01; }

        public void SetTimeSinceBegin(float time) { timeSinceBegin = time;  }

        public float GetLength() { return timeSinceBegin + defaultClip.length; }

        private IEnumerator FadeTrack(AudioClip newClip, bool dynamic, float volume)
        {
            float timeToFade = 1f;
            float timeElapsed = 0;
            if (isPlayingTrack01)
            {
                track02.clip = newClip;
                if (dynamic) track02.time += track01.time;
                track02.Play();
                float originalVolume = track01.volume;
                while (timeElapsed < timeToFade)
                {
                    track02.volume = Mathf.Lerp(0, volume, timeElapsed / timeToFade);
                    track01.volume = Mathf.Lerp(originalVolume, 0, timeElapsed / timeToFade);
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
                track01.Stop();
            }
            else
            {
                track01.clip = newClip;
                if (dynamic) track01.time = track02.time;
                track01.Play();
                float originalVolume = track02.volume;
                while (timeElapsed < timeToFade)
                {
                    track01.volume = Mathf.Lerp(0, volume, timeElapsed / timeToFade);
                    track02.volume = Mathf.Lerp(originalVolume, 0, timeElapsed / timeToFade);
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
                track02.Stop();
            }
        }

        public IEnumerator FadeIn(float volume = 1)
        {
            if (!isMusicPlaying)
            {
                isMusicPlaying = true;
                float timeToFade = 2f;
                float timeElapsed = 0;

                isPlayingTrack01 = true;
                track01.clip = defaultClip;
                track01.timeSamples = 0;
                track01.Play();
                while (timeElapsed < timeToFade)
                {
                    track01.volume = Mathf.Lerp(0, volume, timeElapsed / timeToFade);
                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // [CAMBIO] Se le ha anadido una duracion opcional para poder reutilizarla desde
        // ReturnDefaultCoroutine con el valor del Inspector. Sin argumento se comporta
        // igual que antes, asi que las llamadas de PlayerDeathManager y MusicaPorSeccion
        // siguen valiendo tal cual.
        public IEnumerator FadeOut(float duracion = -1f)
        {
            if (isMusicPlaying)
            {
                float timeToFade = duracion > 0f ? duracion : segundosDeFundidoDeSalida;
                float timeElapsed = 0;

                if (isPlayingTrack01)
                {
                    float originalVolume = track01.volume;
                    while (timeElapsed < timeToFade)
                    {

                        track01.volume = Mathf.Lerp(originalVolume, 0, timeElapsed / timeToFade);
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    track01.Stop();
                }
                else
                {
                    float originalVolume = track02.volume;
                    while (timeElapsed < timeToFade)
                    {
                        track02.volume = Mathf.Lerp(originalVolume, 0, timeElapsed / timeToFade);
                        timeElapsed += Time.deltaTime;
                        yield return null;
                    }
                    track02.Stop();
                }
                isMusicPlaying = false;
            }
        }

        public void StopLoop()
        {
            track01.loop = false;
        }

        public void Loop()
        {
            track01.loop = true;
        }

        public void StopMusic()
        {
            if (isMusicPlaying)
            {
                if (isPlayingTrack01) { track01.Stop(); }
                else { track02.Stop(); }
                isMusicPlaying = false;
                isPlayingTrack01 = false;
            }
        }

        public void PauseMusic()
        {
            if (isMusicPlaying)
            {
                if (isPlayingTrack01) { track01.Pause(); }
                else { track02.Pause(); }
                isMusicPlaying = false;
            }
        }

        public void ResumeMusic()
        {
            if (!isMusicPlaying)
            {
                if (isPlayingTrack01) { track01.UnPause(); }
                else { track02.UnPause(); }
                isMusicPlaying = true;
            }
        }

        public void PlayPreview(AudioClip clip, float startTime, float maxDuration = 15f)
        {
            if (!backgroundVolumeStored)
            {
                backgroundSource = isPlayingTrack01 ? track01 : track02;
                backgroundBaseVolume = backgroundSource.volume;
                backgroundVolumeStored = true;
            }

            StopPreviewImmediate();
            previewCoroutine = StartCoroutine(PreviewRoutine(clip, startTime, maxDuration));
        }

        public void StopPreview()
        {
            StopPreviewImmediate();
            RestoreBackground();
        }

        private void StopPreviewImmediate()
        {
            previewTween?.Kill();
            backgroundTween?.Kill();

            if (previewCoroutine != null)
            {
                StopCoroutine(previewCoroutine);
                previewCoroutine = null;
            }

            if (previewSource != null)
            {
                previewSource.Stop();
                previewSource.volume = 0f;
            }
        }

        private IEnumerator PreviewRoutine(AudioClip clip, float startTime, float maxDuration)
        {
            backgroundSource = isPlayingTrack01 ? track01 : track02;
            previewSource = isPlayingTrack01 ? track02 : track01;

            // Fade out fondo
            backgroundTween?.Kill();
            backgroundTween = backgroundSource.DOFade(0f, 0.3f).SetEase(Ease.InOutSine);
            yield return backgroundTween.WaitForCompletion();
            backgroundSource.Pause();

            // Configurar preview
            previewSource.clip = clip;
            previewSource.time = Mathf.Clamp(startTime, 0f, clip.length - 0.01f);
            previewSource.volume = 0f;
            previewSource.loop = false;
            previewSource.Play();
            yield return null;

            // Fade in preview
            previewTween?.Kill();
            previewTween = previewSource.DOFade(1f, 0.3f).SetEase(Ease.InOutSine);

            // Esperar duraci�n del preview
            float previewLength = Mathf.Min(maxDuration, clip.length - previewSource.time);
            yield return new WaitForSeconds(previewLength);

            // Fade out preview antes de detener
            previewTween?.Kill();
            previewTween = previewSource.DOFade(0f, 0.45f).SetEase(Ease.InOutSine);
            yield return previewTween.WaitForCompletion();
            previewSource.Stop();

            // Restaurar fondo
            RestoreBackground();
        }

        private void RestoreBackground()
        {
            if (backgroundSource == null) return;

            backgroundSource.UnPause();
            backgroundSource.volume = 0f;

            backgroundTween?.Kill();
            backgroundTween = backgroundSource
                .DOFade(backgroundBaseVolume, 0.6f)
                .SetEase(Ease.InOutSine);
        }

        public AudioSource Play_SFX(AudioClip audioClip, bool randomize = false, float volume = 1f)
        {
            AudioSource audioSource = Instantiate(SFX_Object, new Vector3(0, 0, 0), Quaternion.identity);

            audioSource.clip = audioClip;

            if (randomize) audioSource.pitch = (float)Random.Range(8, 12) / 10;

            audioSource.volume = volume;

            audioSource.Play();

            float clipLength = audioSource.clip.length;

            Destroy(audioSource.gameObject, clipLength);

            return audioSource;
        }

        public AudioSource Play_Array(AudioClip[] audioClip, bool randomize = false, float volume = 1f)
        {
            AudioSource audioSource = Instantiate(SFX_Object, new Vector3(0, 0, 0), Quaternion.identity);

            audioSource.clip = audioClip[Random.Range(0, audioClip.Length)];

            if (randomize) audioSource.pitch = (float)Random.Range(8, 12) / 10;

            audioSource.volume = volume;

            audioSource.Play();

            float clipLength = audioSource.clip.length;

            Destroy(audioSource.gameObject, clipLength);

            return audioSource;
        }
    }
}