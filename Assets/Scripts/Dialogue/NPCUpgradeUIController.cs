using UnityEngine;

public class NPCUpgradeUIController : MonoBehaviour
{
    [Header("�������O")]
    public GameObject characterUpgradePanel;
    public GameObject weaponUpgradePanel;

    [Header("�������s���G�]�i��^")]
    public GameObject characterTabHighlight;
    public GameObject weaponTabHighlight;

    public void OpenCharacterTab()
    {
        characterUpgradePanel.SetActive(true);
        // weaponUpgradePanel.SetActive(false);

        if (characterTabHighlight) characterTabHighlight.SetActive(true);
        // if (weaponTabHighlight) weaponTabHighlight.SetActive(false);
    }

    // public void OpenWeaponTab()
    // {
    //     characterUpgradePanel.SetActive(false);
    //     weaponUpgradePanel.SetActive(true);
    //
    //     if (characterTabHighlight) characterTabHighlight.SetActive(false);
    //     if (weaponTabHighlight) weaponTabHighlight.SetActive(true);
    // }

    public void CloseAll()
    {
        characterUpgradePanel.SetActive(false);
        weaponUpgradePanel.SetActive(false);
        gameObject.SetActive(false);
    }
}
