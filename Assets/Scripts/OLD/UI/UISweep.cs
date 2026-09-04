using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UISweep : MonoBehaviour
{
    public float duracion = 1f;
    public float offset = 500f;
    public string dir;

    public void Sweep()
    {
        RectTransform pos = GetComponent<RectTransform>();
        Vector2 posOriginal = pos.anchoredPosition;
        if (dir == "up") pos.anchoredPosition = new Vector2(posOriginal.x, posOriginal.y + offset);
        if (dir == "down") pos.anchoredPosition = new Vector2(posOriginal.x, posOriginal.y - offset);
        if (dir == "left") pos.anchoredPosition = new Vector2(posOriginal.x - offset, posOriginal.y);
        if (dir == "right") pos.anchoredPosition = new Vector2(posOriginal.x + offset, posOriginal.y);
        pos.DOAnchorPos(posOriginal, duracion).SetEase(Ease.OutCubic);

    }
}
