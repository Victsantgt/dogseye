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

        SendHit(result != HitResult.Miss, result);
        Debug.Log("DISTANCIA DESTINO NOTA: " + delta + "RESULTADO:" + result);

        // devolver al pool
        Active = false;
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

       

        IPooleableObject poolObj = GetComponent<IPooleableObject>();
        if (poolObj != null) poolObj.Active = false;
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
