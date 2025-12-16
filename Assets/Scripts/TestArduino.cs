using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TestArduino : MonoBehaviour
{
    // ================= SERIAL =================
    SerialPort serialPort;
    public string portName = "COM4";
    public int baudRate = 9600;

    // ================= UI =================
    public Image led1;
    public Image led2;

    public GameObject startGamePanel;
    public GameObject gamePanel;
    public GameObject endGamePanel;

    public Image fadeImage;

    // Time UI (Radial)
    public Image timeFillImage;

    // Score Movement
    public Transform scoreObject;
    public float scoreMoveDistance = 625f; // = 100 điểm
    Vector3 scoreStartPos;

    public TextMeshProUGUI finalScoreText;

    // ================= GAME =================
    enum GameState { Start, Game, End }
    GameState currentState;

    int currentIndex = -1;
    int score = 0;

    float gameTime = 20f;
    float maxGameTime = 20f;

    float randomTimer = 0f;
    float randomInterval = 1f;

    // ================= FADE =================
    public float fadeSpeed = 2f;
    bool isTransitioning = false;

    // ================= UNITY =================
    void Start()
    {
        OpenSerial();
        scoreStartPos = scoreObject.localPosition;
        SetState(GameState.Start);
        StartCoroutine(FadeIn());
    }

    void Update()
    {
        ReadArduino();

        if (currentState != GameState.Game)
            return;

        GameUpdate();
    }

    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }

    // ================= SERIAL =================
    void OpenSerial()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 10;
        serialPort.Open();
    }

    void SendToArduino(int index)
    {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.WriteLine(index.ToString());
    }

    // ================= STATE =================
    void SetState(GameState newState)
    {
        currentState = newState;

        startGamePanel.SetActive(newState == GameState.Start);
        gamePanel.SetActive(newState == GameState.Game);
        endGamePanel.SetActive(newState == GameState.End);

        if (newState == GameState.Start || newState == GameState.End)
        {
            currentIndex = 0;
            SendToArduino(0);
            led1.color = Color.green;
            led2.color = Color.gray;
        }

        if (newState == GameState.Game)
        {
            ResetGame();
        }
    }

    // ================= GAME =================
    void GameUpdate()
    {
        // TIME
        gameTime -= Time.deltaTime;
        timeFillImage.fillAmount = gameTime / maxGameTime;

        if (gameTime <= 0)
        {
            finalScoreText.text = score >= 100 ? "WIN" : "LOSE";
            StartCoroutine(ChangeStateWithFade(GameState.End));
            return;
        }

        // RANDOM LED
        randomTimer += Time.deltaTime;
        if (randomTimer >= randomInterval)
        {
            randomTimer = 0;
            currentIndex = Random.Range(0, 2);
            SendToArduino(currentIndex);
            UpdateLEDUI();
        }
    }

    void CheckAnswer(int pressedIndex)
    {
        if (pressedIndex != currentIndex)
            return;

        AddScore(10);
    }

    void AddScore(int amount)
    {
        score += amount;
        score = Mathf.Clamp(score, 0, 100);

        float percent = score / 100f;
        float targetX = scoreStartPos.x + scoreMoveDistance * percent;

        Vector3 targetPos = scoreObject.localPosition;
        targetPos.x = targetX;

        StopAllCoroutines();
        StartCoroutine(MoveScoreObject(targetPos));
    }

    IEnumerator MoveScoreObject(Vector3 target)
    {
        float t = 0;
        Vector3 start = scoreObject.localPosition;

        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            scoreObject.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }

    void ResetGame()
    {
        score = 0;
        gameTime = maxGameTime;
        randomTimer = 0;
        currentIndex = -1;

        timeFillImage.fillAmount = 1;
        scoreObject.localPosition = scoreStartPos;

        led1.color = Color.gray;
        led2.color = Color.gray;
    }

    // ================= ARDUINO =================
    void ReadArduino()
    {
        if (isTransitioning)
            return;

        if (serialPort == null || !serialPort.IsOpen || serialPort.BytesToRead <= 0)
            return;

        string data = serialPort.ReadLine().Trim();

        if (!data.StartsWith("HIT"))
            return;

        int pressedIndex = int.Parse(data.Split(':')[1]);

        if (currentState == GameState.Game)
        {
            CheckAnswer(pressedIndex);
        }
        else
        {
            GameState next = currentState == GameState.Start ? GameState.Game : GameState.Start;
            StartCoroutine(ChangeStateWithFade(next));
        }
    }

    // ================= LED =================
    void UpdateLEDUI()
    {
        if (currentState != GameState.Game)
            return;

        led1.color = Color.gray;
        led2.color = Color.gray;

        if (currentIndex == 0) led1.color = Color.green;
        if (currentIndex == 1) led2.color = Color.red;
    }

    // ================= FADE =================
    IEnumerator FadeIn()
    {
        fadeImage.color = new Color(0, 0, 0, 1);

        while (fadeImage.color.a > 0)
        {
            fadeImage.color -= new Color(0, 0, 0, Time.deltaTime * fadeSpeed);
            yield return null;
        }
    }

    IEnumerator ChangeStateWithFade(GameState nextState)
    {
        if (isTransitioning)
            yield break;

        isTransitioning = true;

        // Fade Out
        while (fadeImage.color.a < 1)
        {
            fadeImage.color += new Color(0, 0, 0, Time.deltaTime * fadeSpeed);
            yield return null;
        }

        SetState(nextState);

        // Fade In
        while (fadeImage.color.a > 0)
        {
            fadeImage.color -= new Color(0, 0, 0, Time.deltaTime * fadeSpeed);
            yield return null;
        }

        isTransitioning = false;
    }
}
