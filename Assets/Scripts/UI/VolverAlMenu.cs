using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// [ANADIDO: pantalla de derrota] Devuelve al menu principal al pulsar una tecla.
/// Va en la escena de derrota.
///
/// Se lee el teclado directamente y no por InputAction a proposito: esta escena no
/// monta el sistema de input del juego, asi que una accion del asset podria llegar
/// deshabilitada y dejar al jugador encerrado sin forma de salir.
/// </summary>
[DisallowMultipleComponent]
public class VolverAlMenu : MonoBehaviour
{
    [Tooltip("Tecla que devuelve al menu.")]
    public Key Tecla = Key.R;

    [Tooltip("Escena a la que se vuelve. Tiene que estar en Build Settings.")]
    public string Escena = "MainMenu";

    [Tooltip("Para la musica antes de cambiar. Hace falta porque MusicManager es un singleton con DontDestroyOnLoad y sobrevive al cambio de escena.")]
    public bool PararLaMusica = true;

    bool yendo;

    void Update()
    {
        if (yendo) return;

        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        if (teclado[Tecla].wasPressedThisFrame)
            Volver();
    }

    /// <summary>Publica por si se quiere colgar tambien de un boton de UI.</summary>
    public void Volver()
    {
        if (yendo) return;
        yendo = true;

        if (PararLaMusica)
        {
            // Se busca por tipo en vez de por el singleton para no depender de que
            // MusicManager.Instance siga vivo tras los cambios de escena.
            var musica = Object.FindFirstObjectByType<Patterns.Singleton.MusicManager>(FindObjectsInactive.Include);
            if (musica != null) musica.StopMusic();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(Escena);
    }
}
