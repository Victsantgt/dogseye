using Patterns.Singleton;
using UnityEngine;


public class DirtyFlag : MonoBehaviour
{
    public bool infoflag = false;
    [SerializeField] ComboManager comboManager;
    public string PlayerPrefsKeyName;

    private void Awake()
    {
        PlayerPrefsKeyName = GameConfig.Instance.GetplayerPrefsKey();
    }

    void Update()
    {
        if (infoflag)
        {
            PlayerInfo playerinfo = new PlayerInfo();
            playerinfo.highscore = comboManager.score;

            string info = JsonUtility.ToJson(playerinfo);

            PlayerPrefs.SetString(PlayerPrefsKeyName, info);
            PlayerPrefs.Save();

            infoflag = false;
        }
    }
}
