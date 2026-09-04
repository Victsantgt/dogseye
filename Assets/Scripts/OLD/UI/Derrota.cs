using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class Derrota : MonoBehaviour
{
   public void Reiniciar()
    {
        SceneManager.LoadScene("escenaCombateTest");
    }

    public void Salir()
    {
        Application.Quit();
    }
}
