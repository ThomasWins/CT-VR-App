using System.Collections;
using UnityEngine;

public class Show_Controls_Function : MonoBehaviour
{
    public bool Show_Controls;
    public GameObject SCButton;
    public GameObject highlight1;
    public GameObject highlight2;

    void OnEnable()
    {
        if (Show_Controls == false)
        {
            SCButton.SetActive(false);
        }
        else
        {
            SCButton.SetActive(true);
        }
    }

    
}
