using UnityEngine;

public class PlaneToggle : MonoBehaviour
{
    public GameObject redPlane;
    public GameObject greenPlane;
    public GameObject yellowPlane;

    public void ToggleRedPlane()
    {
        if (redPlane == null)
        {
            Debug.LogWarning("Red plane reference not set!");
            return;
        }

        redPlane.SetActive(!redPlane.activeSelf);

        Debug.Log($"Red Plane {(redPlane.activeSelf ? "Enabled" : "Disabled")}");
    }
    public void ToggleGreenPlane()
    {
        if (greenPlane == null)
        {
            Debug.LogWarning("Green plane reference not set!");
            return;
        }

        greenPlane.SetActive(!greenPlane.activeSelf);

        Debug.Log($"Green Plane {(greenPlane.activeSelf ? "Enabled" : "Disabled")}");
    }
    public void ToggleYellowPlane()
    {
        if (yellowPlane == null)
        {
            Debug.LogWarning("Yellow plane reference not set!");
            return;
        }

        yellowPlane.SetActive(!yellowPlane.activeSelf);

        Debug.Log($"Yellow Plane {(yellowPlane.activeSelf ? "Enabled" : "Disabled")}");
    }

    public void ToggleAll()
    {
        if (redPlane != null && !redPlane.activeSelf)
        {
            redPlane.SetActive(true);
        }
        if (greenPlane != null && !greenPlane.activeSelf)
        {
            greenPlane.SetActive(true);
        }
        if (yellowPlane != null && !yellowPlane.activeSelf)
        {
            yellowPlane.SetActive(true);
        }
        Debug.Log($"Toggled planes to enabled.");
    }
    public void DisableAll()
    {
        if (redPlane != null && redPlane.activeSelf)
        {
            redPlane.SetActive(false);
        }
        if (greenPlane != null && greenPlane.activeSelf)
        {
            greenPlane.SetActive(false);
        }
        if (yellowPlane != null && yellowPlane.activeSelf)
        {
            yellowPlane.SetActive(false);
        }
        Debug.Log($"Toggled planes to disabled.");
    }
}