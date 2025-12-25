using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // ================= PANEL =================
    public GameObject HomePanel;
    public GameObject TutorialPanel;
    public GameObject GamePanel;
    public GameObject WinPanel;
    public GameObject LosePanel;

    // ================= FADE =================
    public Image FadeEffect;
    public float FadeSpeed = 1.5f;
    bool isTransitioning = false;

    // ================= TIME =================
    public float AutoBackToHomeTime = 15f;
    private float nextTime;
    public float GameTime = 20f;
    public float CountDownTime = 3f;
    float currentTime;
    public TextMeshProUGUI CountDownText;
    public Image timeFillImage;

    // ================= SCORE =================
    public Transform scoreObject;
    public float scoreMoveDistance = 625f;
    [SerializeField] private AnimationCurve bonusCurve;
    [SerializeField] private float bonusDistance = 2f;
    Vector3 scoreStartPos;
    int score = 0;
    public AudioClip scoreSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip clickSound;
    public AudioSource scoreAudioSource;

    public Image ScoreEffect;
    // ================= SPRITES SCORE =================
    public List<Sprite> ScoreSprites; // 20 sprites
    public Image ScoreImageA;
    public Image ScoreImageB;
    public float changeScoreImageSpeed = 5f;
    public float fadeStrength = 2f;
    // ================= VIDEO =================
    public RawImage GameImageVideo;
    public RawImage WinImageVideo;
    public RawImage LoseImageVideo;

    public VideoPlayer GameVideoPlayer;
    public VideoPlayer WinVideoPlayer;
    public VideoPlayer LoseVideoPlayer;
    public float videoFadeSpeed = 2f;
    public RawImage VideoFrame;
    public RawImage WinFirstFrame;
    public RawImage WinVideoFrame;

    // ================= SERIAL =================
    SerialPort serialPort;
    public string portName = "COM4";
    public int baudRate = 9600;

    // ================= GAMEPLAY =================
    const int BUTTON_COUNT = 5;
    // HashSet<int> activeTargets = new HashSet<int>();
    float spawnTimer;
    public float spawnInterval = 0.35f;
    // ================= COM PORT =================
    [Header("COM PORT UI")]
    public TMP_InputField comPortInput;
    public TextMeshProUGUI comStatusText;
    // ================= INPUT LOCK =================
    Dictionary<int, bool> buttonHeld = new Dictionary<int, bool>();
    // ================= TARGET STATE =================
    public float targetLifeTime = 0.1f;
    public float targetCooldown = 0.3f;
    private bool isCheckingHits = false;
    public TMP_InputField targetLifeTimeInput;
    public TMP_InputField targetCooldownInput;
    public TMP_InputField spawnIntervalInput;

    bool[] targetActive = new bool[BUTTON_COUNT];
    float[] targetExpireTime = new float[BUTTON_COUNT];
    float[] targetNextSpawnTime = new float[BUTTON_COUNT];
    enum GameState { Home, Tutorial, Countdown, Playing, Result }
    GameState currentState;

    void Start()
    {
        targetCooldownInput.text = targetCooldown.ToString("F2");
        targetLifeTimeInput.text = targetLifeTime.ToString("F2");
        spawnIntervalInput.text = spawnInterval.ToString("F2");
        OpenSerial();

        scoreStartPos = scoreObject.localPosition;
        currentState = GameState.Home;

        ShowOnly(HomePanel);

        for (int i = 0; i < BUTTON_COUNT; i++)
        {
            buttonHeld[i] = false;
            targetActive[i] = false;
            targetExpireTime[i] = 0f;
            targetNextSpawnTime[i] = 0f;
        }


        SendArduino(-1);
        SendArduino(0);
    }

    void Update()
    {
        ReadArduino();

        if (currentState == GameState.Playing)
            UpdateGame();
        else if (currentState == GameState.Result)
        {
            if (Time.time >= nextTime && !isTransitioning)
            {
                StartCoroutine(ChangePanel(HomePanel, GameState.Home));
            }
        }
    }

    // ================= SERIAL =================
    void OpenSerial()
    {
        serialPort = new SerialPort(portName, baudRate);
        serialPort.ReadTimeout = 10;
        serialPort.Open();
    }

    void SendArduino(int value)
    {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.WriteLine(value.ToString());
    }

    // ================= INPUT =================
    void ReadArduino()
    {
        if (serialPort == null || !serialPort.IsOpen || serialPort.BytesToRead <= 0)
            return;

        string data = serialPort.ReadLine().Trim();
        if (!data.StartsWith("HIT")) return;

        int index = int.Parse(data.Split(':')[1]);

        // debounce giữ nút
        if (buttonHeld[index]) return;
        buttonHeld[index] = true;
        StartCoroutine(ReleaseButton(index));

        if (currentState == GameState.Playing)
        {
            CheckHit(index);
        }
        else
        {
            HandlePanelNavigation(index);
        }
    }

    IEnumerator ReleaseButton(int index)
    {
        yield return new WaitForSeconds(0.3f);
        buttonHeld[index] = false;
    }

    // ================= PANEL NAV =================
    public void HandlePanelNavigation(int index)
    {
        if (index != 0 || isTransitioning) return;
        scoreAudioSource.PlayOneShot(clickSound);
        if (currentState == GameState.Home)
            StartCoroutine(ChangePanel(TutorialPanel, GameState.Tutorial));
        else if (currentState == GameState.Tutorial)
            StartCoroutine(StartCountdown());
        else if (currentState == GameState.Result)
            StartCoroutine(ChangePanel(HomePanel, GameState.Home));
    }

    // ================= GAME FLOW =================
    IEnumerator StartCountdown()
    {
        // currentState = GameState.Countdown;
        StartCoroutine(ChangePanel(GamePanel, GameState.Countdown));
        scoreObject.localPosition = scoreStartPos;
        ResetScoreSprite();
        timeFillImage.fillAmount = 1;
        VideoFrame.color = new Color(1, 1, 1, 1f);
        GameImageVideo.color = new Color(1, 1, 1, 1f);
        WinImageVideo.color = new Color(1, 1, 1, 0f);
        LoseImageVideo.color = new Color(1, 1, 1, 0f);
        CountDownText.gameObject.SetActive(true);

        for (int i = 3; i > 0; i--)
        {
            CountDownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        CountDownText.gameObject.SetActive(false);
        SendArduino(-1);
        StartGame();
    }

    void StartGame()
    {
        currentState = GameState.Playing;
        currentTime = GameTime;
        score = 0;

        // FadeVideo(VideoFrame, GameImageVideo);
        // GameVideoPlayer.Play();
    }

    void UpdateGame()
    {
        currentTime -= Time.deltaTime;
        timeFillImage.fillAmount = currentTime / GameTime;
        UpdateTargetsLife();
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0;
            SpawnTargets();
        }
        // if (currentTime <= 5f)
        //     DecideResultVideo();
        if (score >= 20)
            EndGame();
        if (currentTime <= 0)
            EndGame();
    }
    void UpdateTargetsLife()
    {
        if (isCheckingHits)
        {
            return;
        }
        float now = Time.time;

        for (int i = 1; i < BUTTON_COUNT; i++)
        {
            if (!targetActive[i]) continue;
            if (isCheckingHits)
            {
                return;
            }
            if (now >= targetExpireTime[i])
            {
                targetActive[i] = false;
                targetNextSpawnTime[i] = now + targetCooldown;
                SendArduino(i + 100);
            }
        }
    }

    // void SpawnTargets()
    // {
    //     int count = Random.Range(1, 5);
    //     for (int i = 0; i < count; i++)
    //     {
    //         int index = Random.Range(1, BUTTON_COUNT);
    //         activeTargets.Add(index);
    //         SendArduino(index);
    //     }
    // }
    void SpawnTargets()
    {
        float now = Time.time;

        for (int i = 1; i < BUTTON_COUNT; i++)
        {
            // Đang sáng → bỏ qua
            if (targetActive[i]) continue;

            // Đang cooldown → bỏ qua
            if (now < targetNextSpawnTime[i]) continue;

            // Xác suất spawn (tránh bật hết 1 lúc)
            if (Random.value > 0.25f) continue;

            // ===== SPAWN =====
            targetActive[i] = true;
            targetExpireTime[i] = now + targetLifeTime;

            SendArduino(i); // bật LED
        }
    }


    void ResetScoreSprite()
    {
        // StopAllCoroutines();
        ScoreImageA.sprite = ScoreSprites[0];
        ScoreImageA.color = new Color(1, 1, 1, 1f);
        ScoreImageB.sprite = ScoreSprites[0];
        ScoreImageB.color = new Color(1, 1, 1, 0f);
    }
    void CheckHit(int index)
    {
        if (!targetActive[index]) return;
        isCheckingHits = true;
        SendArduino(index + 100);
        scoreAudioSource.PlayOneShot(scoreSound);
        targetActive[index] = false;
        targetNextSpawnTime[index] = Time.time + targetCooldown;
        AddScore(1);
        StartCoroutine(ChangeScoreSpriteBetweenAB());
        StartCoroutine(ScoreEffectFlash());
        isCheckingHits = false;
    }
    public void ChangeScore()
    {
        StartCoroutine(ChangeScoreSpriteBetweenAB());
    }

    // IEnumerator ChangeScoreSpriteBetweenAB()
    // {
    //     int nextScore = Mathf.Clamp(score, 0, ScoreSprites.Count - 1);
    //     Image fadingOutImage, fadingInImage;
    //     if (ScoreImageA.color.a >= 1f)
    //     {
    //         fadingOutImage = ScoreImageA;
    //         fadingInImage = ScoreImageB;
    //     }
    //     else
    //     {
    //         fadingOutImage = ScoreImageB;
    //         fadingInImage = ScoreImageA;
    //     }

    //     fadingInImage.sprite = ScoreSprites[nextScore];
    //     float elapsed = 0f;
    //     while (elapsed < changeScoreImageSpeed)
    //     {
    //         elapsed += Time.deltaTime;
    //         float alpha = Mathf.Clamp01(elapsed / changeScoreImageSpeed * fadeStrength);
    //         fadingOutImage.color = new Color(1, 1, 1, 1f - alpha);
    //         // fadingInImage.color = new Color(1, 1, 1, alpha);
    //         fadingInImage.color = new Color(1, 1, 1, 1f);
    //         yield return null;
    //     }
    //     fadingOutImage.color = new Color(1, 1, 1, 0f);
    //     fadingInImage.color = new Color(1, 1, 1, 1f);
    // }
    IEnumerator ChangeScoreSpriteBetweenAB()
    {
        if (changeScoreImageSpeed <= 0f)
            yield break;

        int nextScore = Mathf.Clamp(score, 0, ScoreSprites.Count - 1);

        Image fadingOut;
        Image fadingIn;

        // Xác định image đang hiển thị
        if (ScoreImageA.color.a >= 0.99f)
        {
            fadingOut = ScoreImageA;
            fadingIn = ScoreImageB;
        }
        else
        {
            fadingOut = ScoreImageB;
            fadingIn = ScoreImageA;
        }

        // Chuẩn bị image mới
        fadingIn.sprite = ScoreSprites[nextScore];
        fadingIn.color = new Color(1, 1, 1, 0f); // bắt đầu từ trong suốt
        fadingIn.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < changeScoreImageSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / changeScoreImageSpeed);

            fadingOut.color = new Color(1, 1, 1, 1f - t);
            fadingIn.color = new Color(1, 1, 1, t);

            yield return null;
        }

        // Kết thúc fade
        fadingOut.color = new Color(1, 1, 1, 0f);
        fadingIn.color = new Color(1, 1, 1, 1f);
    }

    IEnumerator ScoreEffectFlash()
    {
        // rgb 183 99 133
        ScoreEffect.color = new Color(1f, 0f, 0f, 1f);
        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            ScoreEffect.color = new Color(1f, 0f, 0f, alpha);
            yield return null;
        }
        ScoreEffect.color = new Color(1, 1, 1, 0f);
    }
    public void PlayScoreEffect()
    {
        StartCoroutine(ScoreEffectFlash());
    }
    // public void AddScore(int amount)
    // {
    //     score = Mathf.Clamp(score + amount, 0, 20);

    //     float percent = score / 20f;
    //     float targetX = scoreStartPos.x + scoreMoveDistance * percent;

    //     scoreObject.localPosition =
    //         new Vector3(targetX, scoreObject.localPosition.y, scoreObject.localPosition.z);
    // }
    public void AddScore(int amount)
    {
        score = Mathf.Clamp(score + amount, 0, 20);

        float percent = score / 20f;

        // quãng đường chuẩn
        float baseX = scoreStartPos.x + scoreMoveDistance * percent;

        // bonus theo curve
        float bonus = bonusCurve.Evaluate(percent) * bonusDistance;

        float targetX = baseX + bonus;

        scoreObject.localPosition = new Vector3(
            targetX,
            scoreObject.localPosition.y,
            scoreObject.localPosition.z
        );
    }

    IEnumerator MoveScoreObjectToTargetX(float time)
    {
        float elapsed = 0f;
        float startX = scoreObject.localPosition.x;
        float targetX = scoreStartPos.x + scoreMoveDistance;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float newX = Mathf.Lerp(startX, targetX, elapsed / time);
            scoreObject.localPosition =
                new Vector3(newX, scoreObject.localPosition.y, scoreObject.localPosition.z);
            yield return null;
        }
        yield return null;
    }
    void DecideResultVideo()
    {
        if (score >= 14)
        {
            FadeVideo(GameImageVideo, WinImageVideo);
            WinVideoPlayer.Play();
            StartCoroutine(MoveScoreObjectToTargetX(5f));
        }
        else
        {
            FadeVideo(GameImageVideo, LoseImageVideo);
            LoseVideoPlayer.Play();
        }
    }

    void EndGame()
    {
        WinFirstFrame.color = new Color(0, 0, 0, 1f);
        currentState = GameState.Result;
        nextTime = Time.time + AutoBackToHomeTime;
        if (score >= 20)
        {
            // ShowOnly(WinPanel);
            StartCoroutine(ChangePanel(WinPanel, GameState.Result));
            StartCoroutine(FadeVideoCoroutine(WinFirstFrame, WinVideoFrame));
            scoreAudioSource.PlayOneShot(winSound);
        }
        else
        {
            StartCoroutine(ChangePanel(LosePanel, GameState.Result));
            scoreAudioSource.PlayOneShot(loseSound);
        }
        SendArduino(-1);
        SendArduino(0);
    }

    // ================= UI =================
    void ShowOnly(GameObject panel)
    {
        HomePanel.SetActive(false);
        TutorialPanel.SetActive(false);
        GamePanel.SetActive(false);
        WinPanel.SetActive(false);
        LosePanel.SetActive(false);

        panel.SetActive(true);
    }

    IEnumerator ChangePanel(GameObject panel, GameState state)
    {
        isTransitioning = true;
        // if (currentState != GameState.Playing || currentState != GameState.Tutorial)
        SendArduino(-1);
        while (FadeEffect.color.a < 1)
        {
            FadeEffect.color += new Color(0, 0, 0, Time.deltaTime * FadeSpeed);
            yield return null;
        }
        // if (currentState != GameState.Playing || currentState != GameState.Tutorial)
        SendArduino(0);
        ShowOnly(panel);
        currentState = state;

        while (FadeEffect.color.a > 0)
        {
            FadeEffect.color -= new Color(0, 0, 0, Time.deltaTime * FadeSpeed);
            yield return null;
        }
        isTransitioning = false;
    }

    void FadeVideo(RawImage from, RawImage to)
    {
        // from.color = new Color(1, 1, 1, 0.3f);
        // to.color = new Color(1, 1, 1, 1f);
        StartCoroutine(FadeVideoCoroutine(from, to));
    }
    IEnumerator FadeVideoCoroutine(RawImage from, RawImage to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * videoFadeSpeed;
            from.color = new Color(1, 1, 1, Mathf.Lerp(1f, 0f, t));
            to.color = new Color(1, 1, 1, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }
    }

    public void ApplyComPort()
    {
        string newPort = comPortInput.text.Trim();

        if (string.IsNullOrEmpty(newPort))
        {
            SetComStatus("COM port rỗng", Color.red);
            return;
        }

        StartCoroutine(ChangeComPort(newPort));
    }

    IEnumerator ChangeComPort(string newPort)
    {
        // Đóng port cũ
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            yield return new WaitForSeconds(0.1f);
        }

        serialPort = new SerialPort(newPort, baudRate);
        serialPort.ReadTimeout = 10;

        try
        {
            serialPort.Open();
            portName = newPort;
            SetComStatus("Connected: " + newPort, Color.green);
            for (int i = 0; i < BUTTON_COUNT; i++)
            {
                buttonHeld[i] = false;
                targetActive[i] = false;
                targetExpireTime[i] = 0f;
                targetNextSpawnTime[i] = 0f;
            }
            SendArduino(-1);
            SendArduino(0);
            scoreStartPos = scoreObject.localPosition;
            currentState = GameState.Home;
        }
        catch
        {
            SetComStatus("Failed: " + newPort, Color.red);
        }
    }
    void SetComStatus(string message, Color color)
    {
        if (comStatusText == null) return;

        comStatusText.text = message;
        comStatusText.color = color;
    }
    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
            serialPort.Close();
    }
    public void ApplyTargetSettings()
    {
        if (float.TryParse(targetLifeTimeInput.text, out float lifeTime))
        {
            targetLifeTime = lifeTime;
        }
        if (float.TryParse(targetCooldownInput.text, out float cooldown))
        {
            targetCooldown = cooldown;
        }
        if (float.TryParse(spawnIntervalInput.text, out float interval))
        {
            spawnInterval = interval;
        }
    }
}
