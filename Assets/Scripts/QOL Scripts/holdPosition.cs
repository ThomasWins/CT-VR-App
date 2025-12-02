using UnityEngine;

public class holdPosition : MonoBehaviour
{
    public GameObject menu;
    public Transform Parent;
    private void OnEnable()
    {
        Vector3 forward = Parent.transform.forward;
        forward = new Vector3(forward.x, 0, forward.z);
        menu.transform.position = Parent.transform.position + forward.normalized * 1.5f;
        Vector3 newEulerAngles = new Vector3(0, Parent.transform.eulerAngles.y, 0);

        transform.eulerAngles = newEulerAngles;
        
    }

    private void OnDisable()
    {
        
    }
}
