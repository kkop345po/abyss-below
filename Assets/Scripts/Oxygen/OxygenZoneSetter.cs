using UnityEngine;
using UnityEngine.SceneManagement;

public class OxygenZoneSetter : MonoBehaviour
{
    void Start()
    {
        OxygenManager oxygenManager = FindObjectOfType<OxygenManager>();
        if (oxygenManager == null) return;

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Shallow" )
        {
            oxygenManager.SetUnderwaterState(true, DepthZone.Shallow);
        }
        else if (currentScene == "Mid")
        {
            oxygenManager.SetUnderwaterState(true, DepthZone.Mid);
        }
        else if(currentScene=="Deep")
        {
            oxygenManager.SetUnderwaterState(true, DepthZone.Deep);
        }
        else { 
            oxygenManager.SetUnderwaterState(false, DepthZone.Shallow); 
        }

            Debug.Log($"OxygenZoneSetter 設定為水中: {oxygenManager}");
    }
}
