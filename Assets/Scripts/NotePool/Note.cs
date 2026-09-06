using DG.Tweening;
using Patterns.ObjectPool.Interfaces;
using Patterns.Singleton;
using UnityEngine;

public class Note : MonoBehaviour, IPooleableObject
{
    private float duration = 2;
    public string notePosition;

    //OBSERVER
    public float noteTime;     //tiempo en el que la nota debía ser pulsada
    public string lane;        // Carril
    public NoteHitSubject subject; // sujeto del observer
    public AudioSource music;  // referencia a AudioSource
    

    public float ventanaAcierto = 0.2f;

    public Transform perfectMark;

    public bool Active
    {
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    private Tween activeTween;

    // [CAMBIO] Guardamos el padre que tenía la nota al salir del pool para poder
    // devolverla a su sitio cuando se recicla (ver StartMovement / Reset).
    private Transform parentOriginal;
    private bool parentOriginalGuardado;

    private void OnDisable()
    {
        // Cuando vuelve al pool ? parar tween
        if (activeTween != null && activeTween.IsActive())
            activeTween.Kill();
    }

    private void Start()
    {
        if (music == null)
            music = FindFirstObjectByType<AudioSource>();
    }

    // OBSERVER!!!

    // Cuando la nota llega al final sin ser golpeada
    private void OnMiss()
    {
        SendHit(false, HitResult.Miss);

        // devolver al pool
        IPooleableObject poolObj = GetComponent<IPooleableObject>();
        poolObj.Active = false;
    }
    
    public void OnPlayerHit()
    {
        //diferencia de tiempo para saber el rango de la nota
        float delta = Mathf.Abs(perfectMark.transform.position.z - transform.position.z);
        
        HitResult result;

        if (delta <= 0.8f) result = HitResult.Perfect; 
        else if (delta <= 2f) result = HitResult.Good;
        else if (delta <= 2.5f) result = HitResult.Bad;
        else result = HitResult.Miss;

        // [ANADIDO: salida despedida] Se pide ANTES de avisar al observer, porque
        // SendHit devuelve la nota al pool y eso cortaria la animacion en el mismo
        // frame. La puntuacion se manda igual y en el mismo instante: lo unico que se
        // retrasa es el reciclado. Solo lo hace la nota central, que es la unica con
        // MovimientoNotaCentral; las laterales siguen desapareciendo al momento.
        saliendoDespedida = PedirSalidaDespedida(result);

        SendHit(result != HitResult.Miss, result);
        Debug.Log("DISTANCIA DESTINO NOTA: " + delta + "RESULTADO:" + result);

        // devolver al pool, salvo que se este yendo por su cuenta
        if (!saliendoDespedida)
            Active = false;
    }

    // [ANADIDO: salida despedida]
    private bool saliendoDespedida;

    /// <summary>
    /// True si esta nota sabe salir despedida y se le ha pedido que lo haga. Mientras
    /// este a true nadie mas debe devolverla al pool: ya lo hara ella al terminar.
    /// </summary>
    private bool PedirSalidaDespedida(HitResult result)
    {
        if (result == HitResult.Miss) return false;

        MovimientoNotaCentral salida = GetComponent<MovimientoNotaCentral>();
        if (salida != null)
        {
            salida.SalirDespedida();
            return true;
        }

        // [ANADIDO: notas laterales a la cesta] Las laterales no salen despedidas: dan
        // un salto hasta la cesta y se hunden alli. Si por lo que sea no pueden saltar
        // (falta la cesta), devuelve false y la nota se recicla al momento como antes.
        MovimientoNotaLateral salto = GetComponent<MovimientoNotaLateral>();
        if (salto != null)
            return salto.Saltar();

        return false;
    }

    // notifica el resultado
    private void SendHit(bool hit, HitResult result)
    {
        if (subject == null) return;

        NoteHitInfo info = new NoteHitInfo
        {
            lane = lane,
            result = result,
            
        };

       

        // [ANADIDO: salida despedida] Si la nota se esta yendo por su cuenta no se toca
        // aqui: se recicla ella sola cuando termine de salir de pantalla.
        IPooleableObject poolObj = GetComponent<IPooleableObject>();
        if (poolObj != null && !saliendoDespedida) poolObj.Active = false;
        subject.NotifyObservers(info);
    }
    public void RegisterHit()
    {
        float accuracy = 1f;

        NoteHitInfo info = new NoteHitInfo
        {
            lane = notePosition,  
            result = HitResult.Good, 
            
        };

        if (subject != null)
            subject.NotifyObservers(info);

        //liberar nota
        IPooleableObject poolObj = GetComponent<IPooleableObject>();
        if (poolObj != null) poolObj.Active = false;
    }
    public void RegisterMiss()
    {
        NoteHitInfo info = new NoteHitInfo
        {
            lane = notePosition,
            result = HitResult.Miss,
        
        };

        if (subject != null)
            subject.NotifyObservers(info);

        IPooleableObject poolObj = GetComponent<IPooleableObject>();
        if (poolObj != null) poolObj.Active = false;
    }

    // Llamado cuando el pool recicla la nota
    public void Reset()
    {
        // Aquí solo se limpian estados (no iniciar movimiento aquí)
        if (activeTween != null && activeTween.IsActive())
            activeTween.Kill();

        // [ANADIDO: salida despedida] Sin esto, una nota reciclada seguiria creyendo que
        // esta saliendo de pantalla y no se devolveria al pool al siguiente acierto.
        saliendoDespedida = false;

        // [CAMBIO] Al reciclarse la devolvemos al padre que tenía originalmente, para
        // que el pool no acabe acumulando notas colgadas dentro de los carriles.
        if (parentOriginalGuardado && transform.parent != parentOriginal)
            transform.SetParent(parentOriginal, false);
    }

    public IPooleableObject Clone()
    {
        GameObject clone = Instantiate(this.gameObject);
        clone.SetActive(false);
        return clone.GetComponent<Note>();
    }
}
