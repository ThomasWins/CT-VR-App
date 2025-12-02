using UnityEngine;

public class ToggleOnOff : MonoBehaviour
{
    [Header("Target Object to Toggle")]
    public GameObject targetObject;

    public void EnableTarget()
    {
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    public void DisableTarget()
    {
        if (targetObject != null)
            targetObject.SetActive(false);
    }

    public void ToggleTarget()
    {
        if (targetObject != null)
            targetObject.SetActive(!targetObject.activeSelf);
    }
}
