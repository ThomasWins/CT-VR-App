using UnityEngine;

public class LaptopInteraction : MonoBehaviour
{
    [SerializeField] private GameObject quizBoard; 
    private bool quizActive = false;

    public void OnLaptopClicked()
    {
        if (!quizActive && quizBoard != null)
        {
            quizBoard.SetActive(true);
            quizActive = true;
        }
        else if (quizActive)
        {
            Debug.Log("Quiz is already active on the board.");
        }
    }
}
