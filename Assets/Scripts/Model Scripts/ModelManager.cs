using UnityEngine;
using System.Collections.Generic;

public class ModelManager : MonoBehaviour
{
    [Tooltip("List of model prefabs to choose from.")]
    public List<GameObject> modelPrefabs;

    [Tooltip("The GameObject whose position will be used to spawn models.")]
    public GameObject spawnTarget;

    [Header("Teleport Settings")]
    [Tooltip("Target position to teleport the model and spawn point to.")]
    public Vector3 teleportPositionOffset = new Vector3(0, 2, 0);
    [Tooltip("Set true if you want to use absolute position instead of offset.")]
    public bool useAbsoluteTeleportPosition = false;
    [Tooltip("Optional: reference object whose position will be used as the teleport destination.")]
    public GameObject teleportTargetObject;

    private List<GameObject> spawnedModels = new List<GameObject>();
    public int currentModelIndex = -1; // Referenced for DistanceMeasure script

    // Internal storage for teleport toggle
    private bool isTeleported = false;
    private Vector3 originalModelPosition;
    private Quaternion originalModelRotation;
    private Vector3 originalSpawnPosition;
    private Quaternion originalSpawnRotation;

    // Public property for accessing the currently active model
    public GameObject CurrentModel
    {
        get
        {
            if (currentModelIndex >= 0 && currentModelIndex < spawnedModels.Count)
                return spawnedModels[currentModelIndex];
            return null;
        }
    }

    private void Start()
    {
        if (spawnTarget == null)
        {
            Debug.LogError("Spawn target not assigned in ModelManager!");
            return;
        }

        // Manual Adjustments to make it fit in the view
        Vector3 spawnPosition = spawnTarget.transform.position;
        spawnPosition.y += 1.15f;
        spawnPosition.z += -0.3f;
        Quaternion spawnRotation = spawnTarget.transform.rotation;

        // Spawn all models at startup at the spawnTarget's position
        for (int i = 0; i < modelPrefabs.Count; i++)
        {
            GameObject instance = Instantiate(modelPrefabs[i], spawnPosition, spawnRotation);
            instance.SetActive(false);
            spawnedModels.Add(instance);
        }

        if (spawnedModels.Count > 0)
        {
            ChooseModel(16);
        }

        // Record original spawn location
        ResetModel();
        originalSpawnPosition = spawnTarget.transform.position;
        originalSpawnRotation = spawnTarget.transform.rotation;
    }

    public void ChooseModel(int modelIndex)
    {
        if (modelIndex < 0 || modelIndex >= spawnedModels.Count)
        {
            Debug.LogError("Model index is out of range! Check your button configuration.");
            return;
        }

        Vector3 currentPos;
        Quaternion currentRot;

        // If a model is currently active, get its transform
        if (CurrentModel != null)
        {
            currentPos = CurrentModel.transform.position;
            currentRot = CurrentModel.transform.rotation;
        }
        else
        {
            // Default to spawnTarget
            currentPos = spawnTarget.transform.position;
            currentRot = spawnTarget.transform.rotation;
        }

        // Disable all models
        for (int i = 0; i < spawnedModels.Count; i++)
            spawnedModels[i].SetActive(false);

        // Activate the chosen model and set its position/rotation
        currentModelIndex = modelIndex;
        GameObject chosen = spawnedModels[modelIndex];
        chosen.SetActive(true);
        ResetModel();
        chosen.transform.position = currentPos;
        chosen.transform.rotation = currentRot;

        // Reset CTPlaneControllers if they exist
        foreach (CTPlaneController controller in FindObjectsByType<CTPlaneController>(FindObjectsSortMode.None))
        {
            controller.ResetSlider();
        }

        // If the model is in CT view, have the layer be in UI3DModel
        // This is so newly spawned models are visible in CT view
        if (isTeleported)
        {
            CurrentModel.layer = LayerMask.NameToLayer("Default");
        } else
        {
            CurrentModel.layer = LayerMask.NameToLayer("UI3DModel");
        }
    }

    public void NextModel()
    {
        if (spawnedModels.Count == 0) return;
        int nextIndex = (currentModelIndex + 1) % spawnedModels.Count;
        ChooseModel(nextIndex);
    }

    public void PreviousModel()
    {
        if (spawnedModels.Count == 0) return;
        int previousIndex = (currentModelIndex - 1 + spawnedModels.Count) % spawnedModels.Count;
        ChooseModel(previousIndex);
    }

    public void ResetModel()
    {
        if (CurrentModel != null && spawnTarget != null)
        {
            Vector3 pos = spawnTarget.transform.position;
            pos.y += 1.15f;
            pos.z += -0.3f;
            CurrentModel.transform.position = pos;
            CurrentModel.transform.rotation = spawnTarget.transform.rotation;
            CurrentModel.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // Reset CTPlaneControllers if they exist
            foreach (CTPlaneController controller in FindObjectsByType<CTPlaneController>(FindObjectsSortMode.None))
            {
                controller.ResetSlider();
            }
        }
    }

    public void ToggleTeleport()
    {
        if (CurrentModel == null || spawnTarget == null)
        {
            Debug.LogWarning("No model or spawn target available to teleport!");
            return;
        }

        if (!isTeleported)
        {
            // Save original positions
            originalModelPosition = CurrentModel.transform.position;
            originalModelRotation = CurrentModel.transform.rotation;
            originalSpawnPosition = spawnTarget.transform.position;
            originalSpawnRotation = spawnTarget.transform.rotation;

            // Determine teleport destination
            Vector3 targetPosition;
            if (teleportTargetObject != null)
                targetPosition = teleportTargetObject.transform.position;
            else if (useAbsoluteTeleportPosition)
                targetPosition = teleportPositionOffset;
            else
                targetPosition = spawnTarget.transform.position + teleportPositionOffset;

            // Move both model and spawn target
            spawnTarget.transform.position = targetPosition;
            CurrentModel.transform.position = targetPosition;

            isTeleported = true;
            ResetModel();
        }
        else
        {
            // Return to original
            spawnTarget.transform.position = originalSpawnPosition;
            spawnTarget.transform.rotation = originalSpawnRotation;
            CurrentModel.transform.position = originalModelPosition;
            CurrentModel.transform.rotation = originalModelRotation;

            isTeleported = false;
            ResetModel();
        }
        // Reset CTPlaneControllers if they exist
        foreach (CTPlaneController controller in FindObjectsByType<CTPlaneController>(FindObjectsSortMode.None))
        {
            controller.ResetSlider();
        }
    }
}
