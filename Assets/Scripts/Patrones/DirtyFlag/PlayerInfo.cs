using Patterns.Singleton;
using UnityEngine;
[System.Serializable]
public class PlayerInfo
{
    public int highscore;

    public PlayerInfo() { 
        this.highscore = 0;
    }

    public static PlayerInfo CreateFromJSON(string json)
    {
        return JsonUtility.FromJson<PlayerInfo>(json);
    }
}
