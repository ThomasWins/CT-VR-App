using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;


[DefaultExecutionOrder(-70)]

public class LoginManager : MonoBehaviour
{
    [Header("Intro Panel")]
    public GameObject introPanel;
    public Button loginButton;

    [Header("Login Panel")]
    public GameObject loginPanel;
    public TMP_InputField userInput;
    public TMP_InputField passInput;
    public TMP_Text errorMessage;
    public Button submitButton;
    public Button guestButton;

    private SessionManager sessionManager;
    private string url = "https://15.204.249.183:5152/api/ct/";


    private void Start() 
    { 
        introPanel.SetActive(true); 
        loginPanel.SetActive(false); // just in case 
        loginButton.onClick.AddListener(StartLogin);
        //StartLogin(); // now start login process
    }

    IEnumerator ValidateLogin(string sid, string password)
    {
        if (string.IsNullOrEmpty(sid) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("One or more fields are empty");
            errorMessage.text = "One or more fields are empty";
            yield break;
        }

        var login = new
        {
            sid = sid,
            password = password
        };

        string json = JsonConvert.SerializeObject(login);
        byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url + "login", "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Login successful!");
            errorMessage.text = "";
            SessionManager.Instance.StartSession(sid);
            GoToMasterScene();
            //loginPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Failed to login: " + request.error);
            errorMessage.text = "Failed to login - please try again with different credentials or continue as guest.";
            Debug.LogError(request.downloadHandler.text);
        }
    }

    public void StartLogin()
    {
        introPanel.SetActive(false);
        loginPanel.SetActive(true);

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(LoginButton);

        guestButton.onClick.RemoveAllListeners();
        guestButton.onClick.AddListener(GuestButton);
    }

void LoginButton()
    {

        string username = userInput.text;
        string password = passInput.text;

        Debug.Log($"Login attempt with user: {username}, pass: {password}");

        StartCoroutine(ValidateLogin(username, password));
    }

    void GuestButton()
    {

        Debug.Log("Continuing as Guest");
        errorMessage.text = "";
        loginPanel.SetActive(false);

        SessionManager.Instance.StartGuestSession();
        GoToMasterScene();
    }

    private void GoToMasterScene()
    {
        SceneManager.LoadScene("Master Scene");
        Debug.Log("Move to Master");
    }

}