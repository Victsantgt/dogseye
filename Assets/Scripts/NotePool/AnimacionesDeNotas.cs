using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Un hueco de animacion: lo que pasa cuando se pulsa una de las tres teclas.
///
/// Hay dos formas de engancharse, y se pueden usar las dos a la vez:
///   - Animator + NombreDelTrigger: lo mas directo si el gesto es un estado del
///     Animator Controller del personaje.
///   - AlPulsar: un UnityEvent normal, para cablear cualquier otra cosa desde el
///     Inspector (particulas, sonido, un script propio...).
/// Los dos son opcionales. Si se dejan vacios no pasa nada y no se queja.
/// </summary>
[System.Serializable]
public class HuecoDeAnimacion
{
    [Tooltip("Solo para leerlo en el Inspector: que gesto va aqui. No hace nada.")]
    public string Gesto = "";

    [Tooltip("Animator que recibe el trigger. Opcional.")]
    public Animator Animator;

    [Tooltip("Nombre del parametro Trigger que se dispara en ese Animator. Dejalo vacio si el gesto no va por Animator.")]
    public string NombreDelTrigger = "";

    [Tooltip("Se invoca al pulsar la tecla. Para todo lo que no sea un trigger de Animator.")]
    public UnityEvent AlPulsar;

    bool avisadoDelTriggerQueFalta;

    /// <summary>Lanza el gesto. La llama AnimacionesDeNotas al detectar la pulsacion.</summary>
    public void Disparar()
    {
        if (Animator != null && !string.IsNullOrEmpty(NombreDelTrigger))
        {
            if (TieneElTrigger())
                Animator.SetTrigger(NombreDelTrigger);
            else if (!avisadoDelTriggerQueFalta)
            {
                // Un solo aviso: si no, con una tecla de ritmo llenaria la consola.
                avisadoDelTriggerQueFalta = true;
                Debug.LogWarning("AnimacionesDeNotas: el Animator '" + Animator.name + "' no tiene ningun Trigger llamado '"
                    + NombreDelTrigger + "'. El gesto '" + Gesto + "' no se disparara hasta que exista.", Animator);
            }
        }

        if (AlPulsar != null)
            AlPulsar.Invoke();
    }

    bool TieneElTrigger()
    {
        if (Animator.runtimeAnimatorController == null) return false;

        AnimatorControllerParameter[] ps = Animator.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].type == AnimatorControllerParameterType.Trigger && ps[i].name == NombreDelTrigger)
                return true;

        return false;
    }
}

/// <summary>
/// Huecos de animacion para las tres teclas del juego de ritmo.
///
/// Va en el mismo GameObject que el ColliderNoteScript (ColliderNotas) y lee las MISMAS
/// InputActionReference que el, para que no puedan desincronizarse. El mapeo del asset
/// de input es:
///
///   A (accion Left)   -> agarrar a la izquierda
///   S (accion Middle) -> empujar carrito
///   D (accion Right)  -> agarrar a la derecha
///
/// Las mismas acciones estan tambien en las flechas y en el mando, asi que el gesto
/// sale igual jugando con cualquiera de los tres.
///
/// IMPORTANTE PARA EL ANIMADOR: el gesto se dispara SIEMPRE que se pulsa, haya nota o
/// no. Es a proposito: el personaje responde a cada pulsacion y no se traga ninguna.
/// Si hiciera falta un gesto distinto solo cuando se acierta, es otro hueco aparte y se
/// puede anadir.
///
/// Este componente cuelga de -- RHYTHM SYSTEM --, que esta apagado durante todo el
/// puente entre secciones, asi que ahi no se dispara nada. Es lo correcto: en el puente
/// no hay notas.
/// </summary>
[DisallowMultipleComponent]
public class AnimacionesDeNotas : MonoBehaviour
{
    [Header("Input (las mismas acciones que usa ColliderNoteScript)")]
    [Tooltip("Accion Left del asset. En teclado es la A.")]
    public InputActionReference TeclaIzquierda;

    [Tooltip("Accion Middle del asset. En teclado es la S.")]
    public InputActionReference TeclaCentro;

    [Tooltip("Accion Right del asset. En teclado es la D.")]
    public InputActionReference TeclaDerecha;

    [Header("Huecos de animacion")]
    public HuecoDeAnimacion AgarrarIzquierda = new HuecoDeAnimacion { Gesto = "A - agarrar a la izquierda" };
    public HuecoDeAnimacion EmpujarCarrito = new HuecoDeAnimacion { Gesto = "S - empujar carrito" };
    public HuecoDeAnimacion AgarrarDerecha = new HuecoDeAnimacion { Gesto = "D - agarrar a la derecha" };

    [Header("Ajustes")]
    [Tooltip("Habilita las acciones de input si llegan apagadas. Sin esto los gestos no saldrian si nadie mas las ha encendido.")]
    public bool HabilitarLasAccionesSiHaceFalta = true;

    [Tooltip("Deja en consola cada gesto disparado. Util mientras se montan las animaciones; desmarcalo despues.")]
    public bool LogAlDisparar = false;

    void OnEnable()
    {
        if (!HabilitarLasAccionesSiHaceFalta) return;

        Habilitar(TeclaIzquierda);
        Habilitar(TeclaCentro);
        Habilitar(TeclaDerecha);
    }

    void Update()
    {
        if (Pulsada(TeclaIzquierda)) DispararIzquierda();
        if (Pulsada(TeclaCentro)) DispararCentro();
        if (Pulsada(TeclaDerecha)) DispararDerecha();
    }

    /// <summary>A: agarrar a la izquierda. Publica por si hay que lanzarla desde otro sitio.</summary>
    [ContextMenu("Disparar A (agarrar a la izquierda)")]
    public void DispararIzquierda() { Lanzar(AgarrarIzquierda); }

    /// <summary>S: empujar carrito.</summary>
    [ContextMenu("Disparar S (empujar carrito)")]
    public void DispararCentro() { Lanzar(EmpujarCarrito); }

    /// <summary>D: agarrar a la derecha.</summary>
    [ContextMenu("Disparar D (agarrar a la derecha)")]
    public void DispararDerecha() { Lanzar(AgarrarDerecha); }

    void Lanzar(HuecoDeAnimacion hueco)
    {
        if (hueco == null) return;

        if (LogAlDisparar)
            Debug.Log("AnimacionesDeNotas: " + hueco.Gesto, this);

        hueco.Disparar();
    }

    static bool Pulsada(InputActionReference referencia)
    {
        return referencia != null && referencia.action != null && referencia.action.WasPressedThisFrame();
    }

    void Habilitar(InputActionReference referencia)
    {
        if (referencia == null || referencia.action == null) return;
        if (referencia.action.enabled) return;

        referencia.action.Enable();
        Debug.Log("AnimacionesDeNotas: la accion '" + referencia.action.name + "' estaba apagada y se ha encendido.", this);
    }
}
