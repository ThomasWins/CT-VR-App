using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class tutorialPageManager : MonoBehaviour
{
    public GameObject panel;
    public GameObject previous;
    public GameObject next;
    private int page { get; set; } = 1;
    private int currentPage = 1;
    private int pages;
    public Material c1;
    public Material c2;
    private GameObject highlight1, highlight2;
    void Start()
    {
        pages = panel.transform.childCount;
        panel.transform.GetChild(currentPage - 1).gameObject.SetActive(true);
    }

    private void changePage()
    {
        if (page < 1) page = 1;
        if (page > pages) page = pages;
        if (currentPage != page)
        {
            panel.transform.GetChild(currentPage - 1).gameObject.SetActive(false);
            panel.transform.GetChild(page - 1).gameObject.SetActive(true);
            if (page == pages){next.SetActive(false);}
            else { next.SetActive(true); }
            if (page == 1) { previous.SetActive(false);}
            else {  previous.SetActive(true); }
            currentPage = page;
            ShowControls();
        }
    }

    public void ShowControls()
    {
        StopAllCoroutines();
        if (highlight1 != null)
        {
            highlight1.GetComponent<Renderer>().material = c1;
        }
        if (highlight2 != null)
        {
            highlight2.GetComponent<Renderer>().material = c1;
        }
        highlight1 = panel.transform.GetChild(currentPage - 1).gameObject.GetComponent<Show_Controls_Function>().highlight1;
        if (highlight1 != null) { Blink(highlight1); }
        highlight2 = panel.transform.GetChild(currentPage - 1).gameObject.GetComponent<Show_Controls_Function>().highlight2;
        if (highlight2 != null) { Blink(highlight2); }
    }

    private void Blink(GameObject target)
    {
        StartCoroutine(Illuminate(target.GetComponent<Renderer>()));
    }

    IEnumerator Illuminate(Renderer target)
    {
        for (int i = 0; i < 7; i++)
        {
            target.material = c2;
            yield return new WaitForSeconds(0.6f);
            target.material = c1;
            yield return new WaitForSeconds(0.4f);
        }

    }
    public void Next()
    {
        page = currentPage + 1;
        changePage();
    }
    public void Previous() { 
        page = currentPage - 1;
        changePage();
    }
}
