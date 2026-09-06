using DG.Tweening;
using UnityEngine;

public class SideNotesFeedback : MonoBehaviour
{
    public Transform feedback;

    private void Start()
    {
        feedback.DOScale(0, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ColliderNotas"))
            feedback.DOScale(1, 0.1f).OnComplete(() => feedback.DOScale(1, 0.2f).OnComplete(() => feedback.DOScale(0, 0)));
    }

}
