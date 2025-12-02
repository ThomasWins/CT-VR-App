using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Newtonsoft.Json;

[DefaultExecutionOrder(-90)]

[System.Serializable]
public class Answer
{
    public int aid;
    public string letter;
    public string answer;
    public string explanation;
}

[System.Serializable]
public class Question
{
    public int uid;
    public string question;
    public List<Answer> answers;
    public int correctAnswer;
}

[System.Serializable]
public class Quiz
{
    public int qid;
    public string name;
    public List<Question> questions;
}


public class QuizManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject optionsPanel;
    public GameObject quizPanel;
    public GameObject questionPanel; // added 
    public GameObject explanationPanel;
    public GameObject resultsPanel;

    [Header("Exit Button")]
    //public Button exitButton;

    [Header("Options Panel References")]
    public Transform optionParent;
    public GameObject optionPrefab;

    [Header("Quiz Panel References")]
    public TextMeshProUGUI quizTitle;
    public TextMeshProUGUI questionNumber;
    public TextMeshProUGUI questionText;
    public Transform answerParent;
    public GameObject answerPrefab;
    //public Button exitButton;

    [Header("Explanation Panel Reference")]
    public TextMeshProUGUI explanationText;
    public Button continueButton;

    [Header("Results Panel References")]
    public TextMeshProUGUI resultsText;
    public Button restartButton;

    [Header("Audio Options")]
    public AudioSource audioSource;
    public AudioClip correct;
    public AudioClip incorrect;

    private string url = "http://localhost:5295/api/ct/";
    private List<Quiz> loadedQuizzes;
    private int correctAnswerCount;
    private System.DateTime quizStartTime;
    private System.DateTime quizEndTime;
    private bool inProgress = false;
    private bool isDefault = false;


    void Start()
    {
        optionsPanel.SetActive(true);
        quizPanel.SetActive(false);
        explanationPanel.SetActive(false);
        resultsPanel.SetActive(false);

        //exitButton.interactable = true;
        //exitButton.onClick.RemoveAllListeners();
        //exitButton.onClick.AddListener(() =>
        //{
        //    ExitQuiz();
        //});

        StartCoroutine(GetQuizzes());
    }

    IEnumerator GetQuizzes()
    {
        while (true)
        {
            // Connect to backend database
            UnityWebRequest request = UnityWebRequest.Get(url + "quiz");
            yield return request.SendWebRequest();

            // If connection is successful, run the optionsPanel
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log(response);

                List<Quiz> quizzes = JsonConvert.DeserializeObject<List<Quiz>>(response);
                loadedQuizzes = quizzes;

                if (!inProgress)
                {
                    isDefault = false;

                    foreach (Transform child in optionParent)
                    {
                        Destroy(child.gameObject);
                    }

                    foreach (var quiz in quizzes)
                    {
                        GameObject option = Instantiate(optionPrefab, optionParent);
                        option.GetComponentInChildren<TextMeshProUGUI>().text = quiz.name;
                        Button button = option.GetComponent<Button>();
                        button.onClick.AddListener(() => StartQuiz(quiz));
                    }
                }

                yield break;
            }
            // If connection fails, retry connection every 30 minutes
            else
            {
                Debug.LogError("Failed to load quizzes: " + request.error);

                if (!inProgress)
                {
                    isDefault = true;

                    List<Quiz> quizzes = LoadDefaultQuiz();

                    foreach (Transform child in optionParent)
                    {
                        Destroy(child.gameObject);
                    }

                    foreach (var quiz in quizzes)
                    {
                        GameObject option = Instantiate(optionPrefab, optionParent);
                        option.GetComponentInChildren<TextMeshProUGUI>().text = quiz.name;
                        Button button = option.GetComponent<Button>();
                        button.onClick.AddListener(() => StartQuiz(quiz));
                    }
                }

                yield return new WaitForSeconds(30f);
            }
        }
    }

    IEnumerator GetQuestions(Quiz quiz)
    {
        if (isDefault)
        {
            Debug.Log($"Question: {quiz.questions[0]}");
            ShowQuestion(quiz, quiz.questions[0]);
            yield break;
        }

        else
        {
            int attempts = 0;
            while (attempts < 5)
            {
                Debug.Log($"Get Questions: {url + $"question?qid={quiz.qid}"}");
                // Connect to backend database
                UnityWebRequest request = UnityWebRequest.Get(url + $"question?qid={quiz.qid}");
                yield return request.SendWebRequest();

                // If connection is successful, add questions to quiz and retrieve question answers
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    Debug.Log(response);

                    // Add question to quiz
                    List<Question> questions = JsonConvert.DeserializeObject<List<Question>>(response);
                    quiz.questions = questions;

                    // Wait for answers to return for each question before starting quiz
                    if (quiz.questions != null && quiz.questions.Count > 0)
                    {
                        Debug.Log(quiz.questions);

                        foreach (var question in questions)
                        {
                            yield return StartCoroutine(GetAnswers(question));
                        }

                        ShowQuestion(quiz, quiz.questions[0]);
                    }
                    else
                    {
                        Debug.LogWarning("No questions received for quiz: " + quiz.name);
                        ResetQuiz();
                    }

                    yield break;
                }
                // Attempt to retrieve questions again after 5 seconds
                else
                {
                    Debug.LogError("Failed to load questions: " + request.error);
                    attempts++;
                    yield return new WaitForSeconds(5f);
                }
            }

            ResetQuiz();
        }
    }

    IEnumerator GetAnswers(Question question)
    {
        int attempts = 0;
        while (attempts < 5)
        {
            Debug.Log("Get Answers");
            // Connect to backend database
            UnityWebRequest request = UnityWebRequest.Get(url + $"answer?uid={question.uid}");
            yield return request.SendWebRequest();

            // If connection is successful, add answers to question
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log(response);

                // Add answers to question
                List<Answer> answers = JsonConvert.DeserializeObject<List<Answer>>(response);
                question.answers = answers;

                if (question.answers != null && question.answers.Count > 0)
                {
                    Debug.Log(question.answers);
                }
                else
                {
                    Debug.LogWarning("No questions received for question: " + question.question);
                    ResetQuiz();
                }

                yield break;
            }
            // Attempt to retrieve answers again after 5 seconds
            else
            {
                Debug.LogError("Failed to load answers: " + request.error);
                attempts++;
                yield return new WaitForSeconds(5f);
            }
        }

        ResetQuiz();
    }

    public IEnumerator SendResults(object result, Action<bool> onComplete = null)
    {
        string json = JsonConvert.SerializeObject(result);
        byte[] jsonToSend = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url + "quiz-attempt", "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Quiz attempt submitted successfully!");

        }
        else
        {
            Debug.LogError("Failed to submit quiz attempt: " + request.error);
            Debug.LogError(request.downloadHandler.text);
            SessionManager.Instance.AddLocalResult(result);
        }

        onComplete?.Invoke(request.result == UnityWebRequest.Result.Success);
    }

    private List<Quiz> LoadDefaultQuiz()
    {
        TextAsset defaultQuiz = Resources.Load<TextAsset>("DefaultQuiz");

        if (defaultQuiz != null)
        {
            List<Quiz> defaultQuizzes = JsonConvert.DeserializeObject<List<Quiz>>(defaultQuiz.text);
            return defaultQuizzes;
        }
        else
        {
            Debug.LogError("Default quiz not found");
            return new List<Quiz>();
        }
    }

    void StartQuiz(Quiz quiz)
    {
        Debug.Log("Start Quiz");
        correctAnswerCount = 0;
        quizStartTime = System.DateTime.Now;

        optionsPanel.SetActive(false);
        quizPanel.SetActive(true);
        questionPanel.SetActive(true); // added 


        StartCoroutine(GetQuestions(quiz));
    }

    void ShowQuestion(Quiz quiz, Question question)
    {
        questionPanel.SetActive(true); // added
        inProgress = true;

        quizTitle.text = quiz.name;
        questionNumber.text = $"A";
        questionText.text = question.question;
        questionText.ForceMeshUpdate();

        Debug.Log(question.question);

        foreach (Transform child in answerParent)
        {
            if (child.GetComponent<Button>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        // Randomize answer list
        var answers = question.answers.OrderBy(a => UnityEngine.Random.value).ToList();
        for (int i = 0; i < answers.Count(); i++)
        {
            answers[i].letter = ((char)('A' + i)).ToString();
        }

        // Show each answer and treat as a button
        foreach (var answer in answers)
        {
            GameObject answerButton = Instantiate(answerPrefab, answerParent);
            TextMeshProUGUI answerText = answerButton.GetComponentInChildren<TextMeshProUGUI>();
            answerText.text = $"{answer.letter}. {answer.answer}";

            LayoutRebuilder.ForceRebuildLayoutImmediate(answerParent.GetComponent<RectTransform>());

            Button button = answerButton.GetComponent<Button>();
            button.onClick.AddListener(() => AnswerSelected(quiz, question, answer));
        }
    }

    void AnswerSelected(Quiz quiz, Question question, Answer answer)
    {
        // Do not allow answers to be selected again
        foreach (Transform child in answerParent)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }

        Debug.Log($"{answer.aid} - {question.correctAnswer}");
        if (answer.aid == question.correctAnswer)
        {
            correctAnswerCount++;
            audioSource.PlayOneShot(correct);
        }
        else
        {
            audioSource.PlayOneShot(incorrect);
        }

            ShowExplanation(quiz, question, answer);
    }

    void ShowExplanation(Quiz quiz, Question question, Answer answer)
    {
        questionPanel.SetActive(false); // added
        explanationPanel.SetActive(true);
        explanationText.text = answer.explanation;

        continueButton.interactable = true;
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() =>
        {
            explanationPanel.SetActive(false);

            int i = quiz.questions.IndexOf(question) + 1;
            if (i >= quiz.questions.Count)
            {
                ShowResults(quiz);
            }
            else
            {
                ShowQuestion(quiz, quiz.questions[i]);
            }
        });
    }

    void ShowResults(Quiz quiz)
    {
        quizEndTime = System.DateTime.Now;

        quizPanel.SetActive(false);
        resultsPanel.SetActive(true);

        // Added for Stem Day polishing
        resultsText.text = $"Score: {correctAnswerCount}/{quiz.questions.Count}";

        // If offline or not logged in, save to memory
        string userID = SessionManager.SessionID;

        restartButton.interactable = true;
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() =>
        {
            EndQuiz(quiz);
        });

        var result = new
        {
            sid = SessionManager.SessionID,
            qid = quiz.qid,
            amountCorrect = correctAnswerCount,
            amountTotal = quiz.questions.Count,
            timeSpent = (int)(quizEndTime - quizStartTime).TotalSeconds,
            timeTaken = quizEndTime.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        StartCoroutine(SendResults(result));
    }

    void EndQuiz(Quiz quiz)
    {
        ResetQuiz();
    }

    public void ResetQuiz()
    {
        inProgress = false;

        quizPanel.SetActive(false);
        explanationPanel.SetActive(false);
        resultsPanel.SetActive(false);
        optionsPanel.SetActive(true);

        correctAnswerCount = 0;

        if (!isDefault)
        {
            if (loadedQuizzes != null)
            {
                foreach (var quiz in loadedQuizzes)
                {
                    if (quiz?.questions == null)
                    {
                        continue;
                    }

                    foreach (var question in quiz.questions)
                    {
                        question?.answers?.Clear();
                    }

                    quiz.questions.Clear();
                }
            }
        }
    }
}
