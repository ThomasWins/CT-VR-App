using UnityEngine;

public class RenderManager : MonoBehaviour
{
    [Header("Parent Folder")]
    public Transform parentFolder;

    private int currentIndex = 0;
    private Transform[] children;

    void Start()
    {
        if (parentFolder == null)
        {
            Debug.LogError("Parent folder not assigned!");
            return;
        }

        // Cache children
        int count = parentFolder.childCount;
        children = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            children[i] = parentFolder.GetChild(i);
            children[i].gameObject.SetActive(false); // start disabled
        }

        if (children.Length > 0)
        {
            children[0].gameObject.SetActive(true); // show first one
        }
    }

    public void NextChild()
    {
        if (children == null || children.Length == 0) return;

        // Disable current
        children[currentIndex].gameObject.SetActive(false);

        // Move index forward
        currentIndex = (currentIndex + 1) % children.Length;

        // Enable next
        children[currentIndex].gameObject.SetActive(true);
    }
}
