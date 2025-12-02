using Unity.XR.CoreUtils;
using UnityEngine;

public class PageSwapper : MonoBehaviour
{
    public bool swap {get; set;} = false;
    private bool previous = false;
    private GameObject primary;
    private GameObject alt;
    void OnEnable()
    {
        primary = gameObject.transform.GetChild(0).gameObject;
        alt = gameObject.transform.GetChild(1).gameObject;
        if (swap == false){primary.SetActive(true);alt.SetActive(false);}
        else { primary.SetActive(false); alt.SetActive(true);}
        previous = swap;
    }

    // Update is called once per frame
    void Update()
    {
        if(swap == true && previous != true)
        {
            primary.SetActive(false);
            alt.SetActive(true);
        }
        else if(swap == false && previous != false)
        {
            primary.SetActive(true);
            alt.SetActive(false);
        }
        previous = swap;
    }
}
