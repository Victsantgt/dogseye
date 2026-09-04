using UnityEngine;

//Los tipos de resultados que puede dar una nota
public enum HitResult
{
    Miss,
    Bad,
    Good,
    Perfect
}

public struct NoteHitInfo
{
    public string lane;        // carril de la nota
    public HitResult result;   // Perfect, good...
  
}