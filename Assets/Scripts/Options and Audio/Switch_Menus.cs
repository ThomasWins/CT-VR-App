using UnityEngine;

public class Switch_Menus : MonoBehaviour
{
    public GameObject mainSettingsPanel;
    public GameObject accessibilityPanel;
    public GameObject visualsPanel;
    public GameObject audioPanel;

    private void OnEnable()
    {
        ShowMainPanel();
    }
    public void ShowAccessibilityPanel()
    {
        mainSettingsPanel.SetActive(false);
        accessibilityPanel.SetActive(true);
    }

    public void ShowVisualsPanel()
    {
        mainSettingsPanel.SetActive(false);
        visualsPanel.SetActive(true);
    }

    public void ShowAudioPanel()
    {
        mainSettingsPanel.SetActive(false);
        audioPanel.SetActive(true);
    }

    public void ShowMainPanel()
    {
        accessibilityPanel.SetActive(false);
        visualsPanel.SetActive(false);
        audioPanel.SetActive(false);
        mainSettingsPanel.SetActive(true);
    }
}
