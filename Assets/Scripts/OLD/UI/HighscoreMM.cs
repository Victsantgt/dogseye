using Patterns.Singleton;
using TMPro;
using UnityEngine;

public class HighscoreMM : MonoBehaviour
{
    private const string playerPrefsKey = "PlayerPrefsKeyName";
    [SerializeField] public TextMeshProUGUI hs;

    private void Start()
    {
        hs.text = "HIGHSCORE: 0";
    
        Debug.Log(GameConfig.Instance.GetplayerPrefsKey());

        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            string info = PlayerPrefs.GetString(playerPrefsKey);

            PlayerInfo playerinfo = JsonUtility.FromJson<PlayerInfo>(info);

            hs.text = "HIGHSCORE: " + playerinfo.highscore;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
