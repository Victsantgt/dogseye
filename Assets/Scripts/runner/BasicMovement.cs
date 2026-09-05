using UnityEngine;

public class BasicMovement : MonoBehaviour
{
    // [CAMBIO] Antes era: transform.Translate(Vector3.forward * PlayerSpeed)
    // sin Time.deltaTime, o sea unidades por FRAME. La velocidad real dependia de
    // los FPS (24 u/s a 60 Hz, 57.6 u/s a 144 Hz), asi que el terreno se desincronizaba
    // de la musica segun la maquina. Ahora es en unidades por SEGUNDO.
    // El valor 24 es el equivalente exacto al 0.4 por frame que habia a 60 fps.
    [Tooltip("Unidades por SEGUNDO. Antes este valor era por frame; 24 equivale al 0.4 anterior a 60 fps.")]
    public float PlayerSpeed = 24f;

    // Multiplicador temporal usado por TransitionRush durante el puente musical.
    float multiplicador = 1f;

    /// <summary>Velocidad efectiva de este frame, ya con el multiplicador aplicado.</summary>
    public float VelocidadActual { get { return PlayerSpeed * multiplicador; } }

    void Update()
    {
        transform.Translate(Vector3.forward * PlayerSpeed * multiplicador * Time.deltaTime);
    }

    /// <summary>Lo usa TransitionRush. 1 = velocidad normal.</summary>
    public void SetMultiplicador(float valor)
    {
        multiplicador = valor;
    }
}
