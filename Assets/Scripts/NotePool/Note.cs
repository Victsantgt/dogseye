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

    public Transform destiny;

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

    public void StartMovement()
    {
        if (activeTween != null && activeTween.IsActive())
            activeTween.Kill();

        // [CAMBIO] --- La nota ahora viaja "enganchada" al jugador ---
        // PROBLEMA: el pool instancia las notas con Instantiate() sin padre, así que
        // vivían sueltas en la raíz de la escena. Los carriles (Lanes/*Lane, sus
        // ColliderFinal y perfectMark) cuelgan de Player, o sea que avanzan en Z con él.
        // El tween sólo leía destiny.position UNA vez, al lanzarse, así que la nota
        // caía hacia el punto donde estaba el carril en ese instante; para cuando
        // terminaba de caer el jugador ya había avanzado y la nota quedaba detrás.
        // SOLUCIÓN: colgamos la nota del mismo padre que su destino (el carril). Así
        // hereda automáticamente el avance del jugador y la caída se calcula en local.
        if (destiny != null && destiny.parent != null && transform.parent != destiny.parent)
        {
            if (!parentOriginalGuardado)
            {
                parentOriginal = transform.parent;
                parentOriginalGuardado = true;
            }

            // true = mantiene la posición de mundo actual, la nota no "salta" al reparentar.
            transform.SetParent(destiny.parent, true);
        }

        Sequence seq = DOTween.Sequence();

        // [CAMBIO] DOLocalMove espera coordenadas LOCALES del padre, pero antes se le
        // pasaba destiny.position (mundo). Sólo coincidían porque la nota no tenía padre.
        // Convertimos el destino al espacio local del padre: como destiny es hijo de ese
        // mismo padre, este valor es constante aunque el jugador se mueva.
        Vector3 destinoLocal = transform.parent != null
            ? transform.parent.InverseTransformPoint(destiny.position)
            : destiny.position;

        seq.Append(transform.DOLocalMove(destinoLocal, duration).SetEase(Ease.Linear));

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
        float delta = Vector3.Distance(perfectMark.transform.position, transform.position);
        
        HitResult result;

        if (delta <= 0.1f) result = HitResult.Perfect;
        else if (delta <= 0.3f) result = HitResult.Good;
        else if (delta <= 0.5f) result = HitResult.Bad;
        else result = HitResult.Miss;

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
