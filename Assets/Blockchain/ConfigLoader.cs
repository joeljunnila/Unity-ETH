using UnityEngine;

public class ConfigLoader : MonoBehaviour
{
    public static ConfigKeys config;

    void Awake()
    {
        LoadConfig();
    }

    private void LoadConfig()
    {
        TextAsset configText = Resources.Load<TextAsset>("configKeys");
        if (configText != null)
        {
            config = JsonUtility.FromJson<ConfigKeys>(configText.text);
        }
        else
        {
            Debug.LogError("Failed to load configKeys.json from Resources.");
        }
    }
}