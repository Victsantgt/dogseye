using UnityEngine;

/// <summary>
/// Apaga y enciende el sistema de notas durante el puente entre secciones.
///
/// Lo apaga el DecisionManager al mostrar la pregunta, y lo vuelve a encender el
/// RhythmResumeTrigger que llevan los prefabs de transicion cuando el jugador entra
/// en ellos.
/// </summary>
public class RhythmSystemToggle : MonoBehaviour
{
    [Tooltip("El objeto -- RHYTHM SYSTEM -- que cuelga del Player.")]
    public GameObject SistemaDeNotas;

    [Tooltip("Al reactivar, salta las notas del chart cuyo momento haya pasado durante la pausa. Sin esto saldrian todas de golpe.")]
    public bool SaltarNotasPerdidas = true;

    /// <summary>True si el sistema de notas esta encendido ahora mismo.</summary>
    public bool Activo { get { return SistemaDeNotas != null && SistemaDeNotas.activeSelf; } }

    [ContextMenu("Desactivar sistema de notas")]
    public void Desactivar()
    {
        if (SistemaDeNotas == null)
        {
            Debug.LogError("RhythmSystemToggle: falta asignar el objeto del sistema de notas.", this);
            return;
        }

        if (!SistemaDeNotas.activeSelf)
            return;

        // Las notas que estuvieran cayendo cuelgan de los carriles, asi que se
        // apagarian congeladas a media caida. Las devolvemos al pool antes.
        Note[] enVuelo = SistemaDeNotas.GetComponentsInChildren<Note>(true);
        for (int i = 0; i < enVuelo.Length; i++)
        {
            enVuelo[i].Active = false;
            enVuelo[i].Reset();   // mata el tween y las saca del carril
        }

        // Ademas del objeto, paramos el chart. Con el objeto apagado su Update no
        // corre igualmente, pero dejarlo marcado evita sorpresas si alguien reactiva
        // el objeto por otro camino.
        ChartManager chartOff = SistemaDeNotas.GetComponentInChildren<ChartManager>(true);
        if (chartOff != null)
            chartOff.chartActive = false;

        SistemaDeNotas.SetActive(false);
    }

    [ContextMenu("Activar sistema de notas")]
    public void Activar()
    {
        if (SistemaDeNotas == null)
        {
            Debug.LogError("RhythmSystemToggle: falta asignar el objeto del sistema de notas.", this);
            return;
        }

        if (SistemaDeNotas.activeSelf)
            return;

        SistemaDeNotas.SetActive(true);

        ChartManager chart = SistemaDeNotas.GetComponentInChildren<ChartManager>(true);
        if (chart != null)
        {
            if (SaltarNotasPerdidas)
                chart.SaltarNotasPasadas();

            chart.chartActive = true;
            chart.NextSection("test2.json");
        }
    }
}
