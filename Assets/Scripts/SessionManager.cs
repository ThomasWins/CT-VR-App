using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;


public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public static string SessionID { get; private set; }
    private static string SESSION_KEY = "SESSION";

    private enum SessionState { Inactive, Active, Guest }

    [Header("Settings")]
    [SerializeField] private float sessionLife = 5f; // in minutes
    //[SerializeField] private Transform player;
    //[SerializeField] private Transform startingPoint;

    private SessionState currentState = SessionState.Inactive;
    private DateTime lastAction = DateTime.MinValue;
    private TimeSpan sessionTime;

    private InputDevice input;
    private QuizManager quizManager;
    private LoginManager loginManager;
    private List<object> localResults = new List<object>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sessionTime = TimeSpan.FromMinutes(sessionLife);
        InitializeInput();
    }

    private IEnumerator Start()
    {
        // Wait one frame so all Awake/Start calls finish
        yield return null;

        loginManager = FindFirstObjectByType<LoginManager>();
        quizManager = FindFirstObjectByType<QuizManager>();

    }


    private void InitializeInput()
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice, devices);

        if (devices.Count > 0)
            input = devices[0];
    }

    private void Update()
    {
        if (currentState == SessionState.Inactive)
            return;

        if (!input.isValid)
        {
            InitializeInput();
            return;
        }

        // Check for inactivity timeout
        if (DateTime.Now > lastAction.Add(sessionTime))
        {
            Debug.Log("Session expired due to inactivity.");
            EndSession();
        }

        // Detect headset presence
        if (input.TryGetFeatureValue(CommonUsages.userPresence, out bool isPresent))
        {
            if (isPresent)
                lastAction = DateTime.Now;
        }

        
    }


    public void StartSession(string sid)
    {
        SessionID = sid;
        PlayerPrefs.SetString(SESSION_KEY, sid);
        currentState = SessionState.Active;
        lastAction = DateTime.Now;
        Debug.Log($"[Session] User session started. SID: {sid}");
    }

    public void StartGuestSession()
    {
        SessionID = Guid.NewGuid().ToString();
        PlayerPrefs.SetString(SESSION_KEY, SessionID);
        currentState = SessionState.Guest;
        lastAction = DateTime.Now;
        Debug.Log($"[Session] Guest session started. ID: {SessionID}");
    }

    private void EndSession()
    {
        currentState = SessionState.Inactive;

        //if (player && startingPoint)
        //{
        //    player.position = startingPoint.position;
        //    player.rotation = startingPoint.rotation;
        //}

        if (quizManager)
            quizManager.ResetQuiz();

        ReactivateLogin();
    }

    private void ReactivateLogin()
    {
        SceneManager.LoadScene("Login Scene");
    }


    public bool AddLocalResult(object result)
    {
        if (localResults.Contains(result))
        {
            return false;
        }

        localResults.Add(result);
        return true;
    }

    public IEnumerator TrySyncLocalResults()
    {
        var pending = new List<object>(localResults);

        foreach (var result in pending)
        {
            bool sendComplete = false;
            bool sendSuccess = false;

            yield return quizManager.SendResults(result, success =>
            {
                sendSuccess = success;
                sendComplete = true;
            });

            yield return new WaitUntil(() => sendComplete);

            if (sendSuccess)
            {
                localResults.Remove(result);
            }
            else
            {
                bool added = AddLocalResult(result);
                if (!added)
                {
                    yield break;
                }
            }
        }
    }

    // ---------------------- HELPERS ----------------------

    public bool IsSessionActive() => currentState != SessionState.Inactive;
    public bool IsGuest() => currentState == SessionState.Guest;
}
