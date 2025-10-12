using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas References")]
    public GameObject startMenuCanvas;
    public GameObject gameUICanvas;
    public GameObject gameOverCanvas;

    [Header("Start Menu UI")]
    public Button startButton;
    public Button quitButton;
    public TMP_Text highScoreText;

    [Header("Game UI")]
    public TMP_Text scoreText;
    public TMP_Text bombTouchCountText;
    public TMP_Text timerText;
    public TMP_Text livesText;
    public TMP_Text comboText;

    [Header("Game Over UI")]
    public TMP_Text finalScoreText;
    public TMP_Text bestScoreText;
    public TMP_Text fruitsSlicedText;
    public TMP_Text bombsTouchedText;
    public Button restartButton;
    public Button menuButton;

    private int currentScore = 0;
    private int bombTouchCount = 0;
    private int currentLives = 3;
    private float gameTime = 0f;
    private bool gameRunning = false;

    private const string HIGH_SCORE_KEY = "HighScore";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeUI();
        SetupButtonListeners();
        ShowStartMenu();
        
        // S'abonner aux événements du GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += OnScoreUpdated;
            GameManager.Instance.OnLivesChanged += OnLivesUpdated;
            GameManager.Instance.OnTimeChanged += OnTimeUpdated;
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            GameManager.Instance.OnComboAchieved += OnComboAchieved;
        }
    }

    void OnDestroy()
    {
        // Se désabonner des événements
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= OnScoreUpdated;
            GameManager.Instance.OnLivesChanged -= OnLivesUpdated;
            GameManager.Instance.OnTimeChanged -= OnTimeUpdated;
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            GameManager.Instance.OnComboAchieved -= OnComboAchieved;
        }
    }

    void Update()
    {
        if (gameRunning)
        {
            UpdateGameTimer();
        }
    }

    private void InitializeUI()
    {
        startMenuCanvas.SetActive(false);
        gameUICanvas.SetActive(false);
        gameOverCanvas.SetActive(false);
        comboText.gameObject.SetActive(false);
    }

    private void SetupButtonListeners()
    {
        startButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(QuitGame);
        restartButton.onClick.AddListener(RestartGame);
        menuButton.onClick.AddListener(ShowStartMenu);
    }

    // ==================== GESTION DES ÉVÉNEMENTS ====================

    private void OnScoreUpdated(int newScore)
    {
        currentScore = newScore;
        UpdateScoreUI();
    }

    private void OnLivesUpdated(int newLives)
    {
        currentLives = newLives;
        UpdateLivesUI();
    }

    private void OnTimeUpdated(float timeRemaining)
    {
        gameTime = timeRemaining;
        UpdateTimerUI();
    }

    private void OnGameStateChanged(bool isRunning)
    {
        gameRunning = isRunning;
        if (!isRunning)
        {
            ShowGameOver();
        }
    }

    private void OnComboAchieved(int comboCount)
    {
        ShowComboText(comboCount);
    }

    // ==================== ÉTATS DU JEU ====================

    public void ShowStartMenu()
    {
        startMenuCanvas.SetActive(true);
        gameUICanvas.SetActive(false);
        gameOverCanvas.SetActive(false);

        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        highScoreText.text = $"MEILLEUR SCORE: {highScore}";

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBackgroundMusic();
        }
    }

    public void StartGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        startMenuCanvas.SetActive(false);
        gameUICanvas.SetActive(true);
        gameOverCanvas.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    public void ShowGameOver()
    {
        gameRunning = false;
        gameUICanvas.SetActive(false);
        gameOverCanvas.SetActive(true);

        // Mettre à jour l'UI de fin de jeu
        finalScoreText.text = $"SCORE: {currentScore}";
        
        int highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
        }
        bestScoreText.text = $"MEILLEUR SCORE: {highScore}";

        // Afficher les statistiques
        if (GameManager.Instance != null)
        {
            fruitsSlicedText.text = $"FRUITS COUPÉS: {GameManager.Instance.GetTotalFruitsSliced()}";
            bombsTouchedText.text = $"BOMBES TOUCHÉES: {GameManager.Instance.GetTotalBombsTouched()}";
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }
    }

    // ==================== MISE À JOUR UI ====================

    public void AddBombTouch()
    {
        bombTouchCount++;
        UpdateBombTouchUI();
    }

    private void UpdateScoreUI()
    {
        scoreText.text = $"SCORE: {currentScore}";
    }

    private void UpdateBombTouchUI()
    {
        bombTouchCountText.text = $"BOMBES: {bombTouchCount}";
    }

    private void UpdateLivesUI()
    {
        livesText.text = $"VIES: {currentLives}";
        livesText.color = currentLives <= 1 ? Color.red : Color.white;
    }

    private void UpdateGameTimer()
    {
        gameTime += Time.deltaTime;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        timerText.text = $"TEMPS: {minutes:00}:{seconds:00}";
        
        // Changer la couleur quand le temps est faible
        if (gameTime < 30f)
        {
            timerText.color = Color.red;
        }
    }

    private void ShowComboText(int comboCount)
    {
        comboText.text = $"COMBO x{comboCount}!";
        comboText.gameObject.SetActive(true);
        
        StartCoroutine(HideComboTextAfterDelay(2f));
    }

    private System.Collections.IEnumerator HideComboTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        comboText.gameObject.SetActive(false);
    }

    // ==================== BOUTONS ====================

    private void RestartGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
        StartGame();
    }

    private void QuitGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // ==================== METHODES PUBLIQUES ====================

    public bool IsGameRunning()
    {
        return gameRunning;
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public int GetBombTouchCount()
    {
        return bombTouchCount;
    }
}