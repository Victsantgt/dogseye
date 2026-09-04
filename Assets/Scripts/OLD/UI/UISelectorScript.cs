using System;
using DG.Tweening;
using UnityEngine;

public class UISelectorScript : MonoBehaviour
{
    [SerializeField] private Transform m_transform;

    private int fileIndex = 0;
    private int screenIndex = 0;
    private float transitionTime = 0.2f;
    private float[][] pos;
    private int[] files;
    private int screens = 2;
    private Vector3 lastMousePos;

    void Start()
    {
        pos = new float[4][];
        pos[0] = new float[] { 665f, 528f };
        pos[1] = new float[] { 700f, 560f, 425f };

        files = new int[] { 2, 3 };
    }

    int GetClosestIndex(float[] arr, float value)
    {
        int closest = 0;
        float minDist = Mathf.Abs(arr[0] - value);

        for (int i = 1; i < arr.Length; i++)
        {
            float dist = Mathf.Abs(arr[i] - value);
            if (dist < minDist)
            {
                minDist = dist;
                closest = i;
            }
        }

        return closest;
    }

    void mouseFunc()
    {
        Vector3 mousePos = Input.mousePosition;

        // only update if mouse moved vertically
        if (Mathf.Abs(mousePos.y - lastMousePos.y) > 0.1f)
        {
            fileIndex = GetClosestIndex(pos[screenIndex], mousePos.y);
        }

        lastMousePos = mousePos;
    }

    void MovementDown()
    {
        if (fileIndex < files[screenIndex] - 1)
            fileIndex++;
    }

    void MovementUp()
    {
        if (fileIndex > 0)
            fileIndex--;
    }

    void UpdateMovement()
    {
        //m_transform.DOKill();
        m_transform.DOMoveY(pos[screenIndex][fileIndex], transitionTime);
    }

    void Update()
    {
        mouseFunc();

        if (Input.GetKeyDown("q"))
        {
            if (screenIndex > 0)
            {
                screenIndex--;
                fileIndex = 0;
            }
        }

        if (Input.GetKeyDown("e"))
        {
            if (screenIndex < screens - 1)
            {
                screenIndex++;
                fileIndex = 0;
            }
        }

        if (Input.GetKeyDown("w") || Input.GetKeyDown("up"))
            MovementUp();

        if (Input.GetKeyDown("s") || Input.GetKeyDown("down"))
            MovementDown();

        UpdateMovement();
    }

    public void ClickJuego()
    {
        screenIndex = 0;
        fileIndex = 0;
        UpdateMovement();
    }

    public void ClickSonido()
    {
        screenIndex = 1;
        fileIndex = 0;
        UpdateMovement();
    }
}
