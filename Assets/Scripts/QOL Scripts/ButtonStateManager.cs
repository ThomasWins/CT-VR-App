using UnityEngine;
using UnityEngine.UI; // Required for UI components

public class ButtonStateManager : MonoBehaviour
{
    private Toggle toggle;

    private void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        ColorBlock cb = toggle.colors;
        if (isOn)
        {
            cb.normalColor = new Color32(47, 0, 89, 255);
            cb.highlightedColor = new Color32(47, 0, 89, 255);
        }
        else
        {
            cb.normalColor = new Color32(47, 47, 47, 255);
            cb.highlightedColor = new Color32(120, 200, 201, 255);
        }
        toggle.colors = cb;
    }
}