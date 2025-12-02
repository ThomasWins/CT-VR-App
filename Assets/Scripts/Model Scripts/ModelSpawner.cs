using UnityEngine;

public class ModelSpawner : MonoBehaviour
{
    public ModelManager modelManager;
    private Vector3 spawnPosition = new Vector3(0, 1, 0); // the spawn is taken from model manager in inspector

    private GameObject spawnedModel;
    private GameObject lastModel; // track which model instance was active

    public void SpawnCurrentModel()
    {
        if (modelManager == null)
        {
            Debug.LogError("ModelManager reference not set!");
            return;
        }

        GameObject currentModel = modelManager.CurrentModel;

        if (currentModel == null)
        {
            Debug.LogWarning("No model selected to spawn.");
            return;
        }

        // Destroy previous spawned model if it exists
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
        }

        // Instantiate a copy of the currently active model
        spawnedModel = Instantiate(currentModel, spawnPosition, Quaternion.identity);
        SetLayerRecursively(spawnedModel, LayerMask.NameToLayer("Default"));
        spawnedModel.SetActive(true);

        // Remember which model was used
        lastModel = currentModel;

        Debug.Log("Spawned copy of: " + currentModel.name);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }

    private void Update()
    {
        // If the active model in ModelManager changes, clear the spawned copy
        if (spawnedModel != null && modelManager.CurrentModel != lastModel)
        {
            Destroy(spawnedModel);
            spawnedModel = null;
            lastModel = null;
        }
    }

    public void DestroyModel()
    {
        if (spawnedModel != null)
        {
            Destroy(spawnedModel);
            spawnedModel = null;
            lastModel = null;
        }
    }
}
