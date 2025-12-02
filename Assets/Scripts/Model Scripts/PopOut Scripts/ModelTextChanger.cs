using UnityEngine;
using TMPro;

public class ModelTextChanger : MonoBehaviour
{
    [Header("Reference to the TextMeshPro field")]
    public TextMeshPro targetText;

    public void ChangeText(string newText)
    {
        if (targetText == null)
        {
            Debug.LogError("No TextMeshProUGUI reference assigned to UITextChanger!");
            return;
        }

        targetText.text = newText;
    }
}
