using UnityEngine;

public class ToggleModelLayer : MonoBehaviour
{
    public ModelManager modelManager;

    public string uiLayerName = "UI3DModel";

    private bool isOnUiLayer = true;

    public void ToggleLayer()
    {
        if (modelManager == null || modelManager.CurrentModel == null)
        {
            Debug.LogWarning("ModelManager or CurrentModel not assigned!");
            return;
        }

        GameObject currentModel = modelManager.CurrentModel;
        int targetLayer = LayerMask.NameToLayer(isOnUiLayer ? "Default" : uiLayerName);

        if (targetLayer == -1)
        {
            Debug.LogError("One of the layer names ('Default' or '" + uiLayerName + "') is not defined in Project Settings > Tags and Layers.");
            return;
        }

        // Change the model and all its children to the new layer
        SetLayerRecursively(currentModel, targetLayer);

        isOnUiLayer = !isOnUiLayer;
    }

    public void ToggleLayerReset()
    {
        if (modelManager == null || modelManager.CurrentModel == null)
        {
            Debug.LogWarning("ModelManager or CurrentModel not assigned!");
            return;
        }

        GameObject currentModel = modelManager.CurrentModel;
        int targetLayer = LayerMask.NameToLayer("UI3DModel");

        // Change the model and all its children to the original layer
        SetLayerRecursively(currentModel, targetLayer);

        isOnUiLayer = !isOnUiLayer;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
