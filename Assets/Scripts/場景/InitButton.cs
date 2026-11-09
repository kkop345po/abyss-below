using UnityEngine;
using UnityEngine.EventSystems;
public class InitButton : MonoBehaviour
{
    private GameObject lastSelect;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastSelect = new GameObject();
    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelect);
        }
        else
        {
            lastSelect = EventSystem.current.currentSelectedGameObject;
        }
    }
}
