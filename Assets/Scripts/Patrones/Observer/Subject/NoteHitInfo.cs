using UnityEngine;

//Los tipos de resultados que puede dar una nota
public enum HitResult
{
    Miss,
    Bad,
    Good,
    Perfect,

    // [ANADIDO: antispam] Pulsar la tecla de un carril sin que hubiera ninguna nota en
    // el. Va al FINAL de la lista a proposito: los valores anteriores conservan su
    // indice, asi que nada de lo que ya estuviera serializado cambia de significado.
    //
    // Existe como resultado propio, y no reusando Miss o Bad, porque no es lo mismo
    // fallar una nota que pulsar al aire: se penaliza distinto y conviene poder
    // distinguirlo en las estadisticas.
    Vacio
}

public struct NoteHitInfo
{
    public string lane;        // carril de la nota
    public HitResult result;   // Perfect, good...
  
}