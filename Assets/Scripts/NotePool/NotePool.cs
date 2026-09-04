using Patterns.ObjectPool;
using System.Collections.Generic;
using UnityEngine;

public class NotePool : MonoBehaviour
{
    [System.Serializable]
    public class NotePoolEntry
    {
        public string key;
        public Note prefab;
        public int initialSize = 10;
    }

    public NotePoolEntry[] entries;

    private Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();

    private void Awake()
    {
        foreach (var entry in entries)
        {
            ObjectPool pool = new ObjectPool(entry.prefab, entry.initialSize, true);
            pools.Add(entry.key, pool);
        }
    }

    public Note GetNote(string key)
    {
        return (Note)pools[key].Get();
    }

    public void Release(string key, Note note)
    {
        pools[key].Release(note);
    }
}
