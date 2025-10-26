using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("ROOT PANELS")]
    public GameObject StartMenuPanel;
    public GameObject DifficultyPanel;
    public GameObject GameUIPanel;
    public GameObject GameOverPanel;

    [Header("StartMenuPanel children")]
    public TextMeshProUGUI TitleText;
    public Button StartButton;
    public Button QuitButton;
    public TextMeshProUGUI HighScoreText;

    [Header("DifficultyPanel children")]
    public TextMeshProUGUI DifficultyTitleText;
    public Button EasyButton;
    public Button NormalButton;
    public Button HardButton;
    public Button ExtremeButton;
    public Button BackButton;
    public TextMeshProUGUI EasyDescriptionText;
    public TextMeshProUGUI NormalDescriptionText;
    public TextMeshProUGUI HardDescriptionText;
    public TextMeshProUGUI ExtremeDescriptionText;

    [Header("GameUIPanel children")]
    public TextMeshProUGUI ScoreText;
    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI LivesText;
    public TextMeshProUGUI ComboText;
    public TextMeshProUGUI DifficultyIndicatorText;

    [Header("GameOverPanel children")]
    public TextMeshProUGUI GameOverTitle;
    public TextMeshProUGUI FinalScoreText;
    public TextMeshProUGUI BestScoreText;
    public TextMeshProUGUI FruitsSlicedText;
    public TextMeshProUGUI BombsTouchedText;
    public Button RestartButton;
    public Button MenuButton;

    // internes
    private int highScore;
    private Coroutine timeWarningCo;
    private GameManager gm;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public void InitializeUI(GameManager gameManager)
    {
        gm = gameManager;

        highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (HighScoreText) HighScoreText.text = $"Best: {highScore}";

        SetOnlyActive(StartMenuPanel);

        // Boutons Start Menu
        if (StartButton)
        {
            StartButton.onClick.RemoveAllListeners();
            StartButton.onClick.AddListener(() => ShowDifficultyPanel());
        }
        if (QuitButton)
        {
            QuitButton.onClick.RemoveAllListeners();
            QuitButton.onClick.AddListener(() => gm.QuitGame());
        }

        // Boutons Difficulté
        if (EasyButton)
        {
            EasyButton.onClick.RemoveAllListeners();
            EasyButton.onClick.AddListener(() => SelectDifficulty(DifficultySettings.Difficulty.Easy));
        }
        if (NormalButton)
        {
            NormalButton.onClick.RemoveAllListeners();
            NormalButton.onClick.AddListener(() => SelectDifficulty(DifficultySettings.Difficulty.Normal));
        }
        if (HardButton)
        {
            HardButton.onClick.RemoveAllListeners();
            HardButton.onClick.AddListener(() => SelectDifficulty(DifficultySettings.Difficulty.Hard));
        }
        if (ExtremeButton)
        {
            ExtremeButton.onClick.RemoveAllListeners();
            ExtremeButton.onClick.AddListener(() => SelectDifficulty(DifficultySettings.Difficulty.Extreme));
        }
        if (BackButton)
        {
            BackButton.onClick.RemoveAllListeners();
            BackButton.onClick.AddListener(() => ShowStartMenu());
        }

        // Boutons Game Over
        if (RestartButton)
        {
            RestartButton.onClick.RemoveAllListeners();
            RestartButton.onClick.AddListener(() => gm.RestartGame());
        }
        if (MenuButton)
        {
            MenuButton.onClick.RemoveAllListeners();
            MenuButton.onClick.AddListener(() => gm.GoToMenu());
        }

        // Descriptions des difficultés
        UpdateDifficultyDescriptions();

        // Valeurs par défaut
        UpdateScore(0);
        UpdateTime(gm.gameDuration);
        UpdateLives(gm.startLives);
        UpdateCombo(0);
    }

    private void UpdateDifficultyDescriptions()
    {
        if (EasyDescriptionText)
            EasyDescriptionText.text = "5 vies • Spawn lent • Peu de bombes";
        
        if (NormalDescriptionText)
            NormalDescriptionText.text = "3 vies • Spawn normal • Bombes modérées";
        
        if (HardDescriptionText)
            HardDescriptionText.text = "2 vies • Spawn rapide • Plus de bombes";
        
        if (ExtremeDescriptionText)
            ExtremeDescriptionText.text = "1 vie • Spawn très rapide • Beaucoup de bombes";
    }

    // ==================== SCREENS ====================
    private void SetOnlyActive(GameObject panelToShow)
    {
        GameObject[] all = { StartMenuPanel, DifficultyPanel, GameUIPanel, GameOverPanel };
        foreach (var go in all)
            if (go) go.SetActive(go == panelToShow);
    }

    public void ShowDifficultyPanel()
    {
        SetOnlyActive(DifficultyPanel);
        if (DifficultyTitleText) DifficultyTitleText.text = "Choisissez la difficulté";
    }

    private void SelectDifficulty(DifficultySettings.Difficulty difficulty)
    {
        gm.SetDifficulty(difficulty);
        gm.StartGame();
    }

    public void ShowGameUI(int score, float timeRemaining, int lives)
    {
        SetOnlyActive(GameUIPanel);
        UpdateScore(score);
        UpdateTime(timeRemaining);
        UpdateLives(lives);
        UpdateCombo(0);
        ShowTimeWarning(false);
        
        // Afficher la difficulté actuelle
        if (DifficultyIndicatorText)
            DifficultyIndicatorText.text = $"Difficulté: {gm.difficultySettings.GetDifficultyName()}";
    }

    public void ShowGameOverPanel(int finalScore, int fruits, int bombs)
    {
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        SetOnlyActive(GameOverPanel);

        if (GameOverTitle)    GameOverTitle.text = "Game Over";
        if (FinalScoreText)   FinalScoreText.text = $"Score: {finalScore}";
        if (BestScoreText)    BestScoreText.text = $"Best: {highScore}";
        if (FruitsSlicedText) FruitsSlicedText.text = $"Fruits Sliced: {fruits}";
        if (BombsTouchedText) BombsTouchedText.text = $"Bombs Touched: {bombs}";

        if (HighScoreText) HighScoreText.text = $"Best: {highScore}";
    }

    public void ShowStartMenu()
    {
        SetOnlyActive(StartMenuPanel);
        if (HighScoreText) HighScoreText.text = $"Best: {highScore}";
    }

    // ==================== UPDATES ====================
    public void UpdateScore(int score)
    {
        if (ScoreText) ScoreText.text = $"Score: {score}";
    }

    public void UpdateTime(float timeRemaining)
    {
        if (TimerText)
        {
            int m = Mathf.FloorToInt(timeRemaining / 60f);
            int s = Mathf.FloorToInt(timeRemaining % 60f);
            TimerText.text = $"{m:00}:{s:00}";
        }
    }

    public void UpdateLives(int lives)
    {
        if (LivesText) LivesText.text = $"Lives: {lives}";
    }

    public void UpdateCombo(int combo)
    {
        if (ComboText)
        {
            ComboText.text = combo > 0 ? $"Combo: x{combo}" : "";
        }
    }

    public void ShowTimeWarning(bool on)
    {
        if (!TimerText) return;

        if (!on)
        {
            if (timeWarningCo != null) StopCoroutine(timeWarningCo);
            TimerText.color = Color.white;
            return;
        }

        if (timeWarningCo != null) StopCoroutine(timeWarningCo);
        timeWarningCo = StartCoroutine(AnimateTimeWarning());
    }

    // ==================== COROUTINES ====================
    private IEnumerator AnimateTimeWarning()
    {
        Color warn = Color.red;
        float blink = 0.3f;
        while (true)
        {
            TimerText.color = warn;  yield return new WaitForSeconds(blink);
            TimerText.color = Color.white; yield return new WaitForSeconds(blink);
        }
    }
}