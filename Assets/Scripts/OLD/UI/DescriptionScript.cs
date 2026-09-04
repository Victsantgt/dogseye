using UnityEngine;
using TMPro;

public class DescriptionScript : MonoBehaviour
{
    public TextMeshProUGUI uiText;


    private int fileIndex = 0;
    private int screenIndex = 0;
    private float[][] pos;
    private int[] files;
    private int screens = 2;
    private Vector3 lastMousePos;
    private string[][] text;

    void Start()
    {
        pos = new float[4][];
        pos[0] = new float[] { 665f, 528f };
        pos[1] = new float[] { 700f, 560f, 425f };

        files = new int[] { 2, 3 };

        text = new string[4][];
        text[0] = new string[] { "Cambia la dificultad del juego. Se puede alternar entre la dificultad estándar, o un modo ayuda en el que no se puede perder.",
            "Ajusta la velocidad con la que descienden las notas. Representa los segundos que tarda una nota en llegar al objetivo. Recomendado: 4 s" };
        text[1] = new string[] { "Cambia el volumen general",
            "Cambia el volumen de la música de fondo",
            "Cambia el volumen de los efectos de sonido" };
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
        uiText.text = text[screenIndex][fileIndex];
    }

    // Update is called once per frame
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
