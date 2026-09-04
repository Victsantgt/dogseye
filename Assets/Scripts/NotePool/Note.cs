using DG.Tweening;
using Patterns.ObjectPool.Interfaces;
using Patterns.Singleton;
using UnityEngine;

public class Note : MonoBehaviour, IPooleableObject
{
    private float firstDuration = GameConfig.Instance.GetNoteSpeed();
    private float colliderPosZ = -5.5495f;
    private float colliderPosY = -0.584f;
    private float endPosZ = -10f;
    private float endPosY = -1.2f;

    public string notePosition;

    //OBSERVER
    public float noteTime;     //tiempo en el que la nota debía ser pulsada
    public string lane;        // Carril
    public NoteHitSubject subject; // sujeto del observer
    public AudioSource music;  // referencia a AudioSource

    public float ventanaAcierto = 0.2f;

    public Transform destiny; //de cada carril

    public bool Active
    {
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    private Tween activeTween;

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

    public void StartMovement()
    {
        if (activeTween != null && activeTween.IsActive())
            activeTween.Kill();

        Vector3 startPos = transform.position;
        Vector3 midPos = new Vector3(startPos.x, colliderPosY, colliderPosZ);
        Vector3 endPos = new Vector3(startPos.x, endPosY, endPosZ);

        // Calcular la velocidad para que sea constante
        float firstDistance = Vector3.Distance(startPos, midPos);
        float speed = firstDistance / firstDuration;

        // Calcular el tiempo que tarda en hacer el segundo tramo
        float secondDistance = Vector3.Distance(midPos, endPos);
        float secondDuration = secondDistance / speed;

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOMove(midPos, firstDuration).SetEase(Ease.Linear));
        seq.Append(transform.DOMove(endPos, secondDuration).SetEase(Ease.Linear));

        activeTween = seq;
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
        float delta = Vector3.Distance(destiny.transform.position, transform.position);
        
        HitResult result;

        if (delta <= 0.7f) result = HitResult.Perfect;
        else if (delta <= 1.4f) result = HitResult.Good;
        else if (delta <= 2f) result = HitResult.Bad;
        else result = HitResult.Miss;
        Debug.Log("DISTANCIA DESTINO NOTA: " + delta + "RESULTADO:" + result);

        SendHit(result != HitResult.Miss, result);

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

    }

    public IPooleableObject Clone()
    {
        GameObject clone = Instantiate(this.gameObject);
        clone.SetActive(false);
        return clone.GetComponent<Note>();
    }
}
