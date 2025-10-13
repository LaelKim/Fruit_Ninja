using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Screens")]
    public GameObject startScreen;
    public GameObject gameScreen;
    public GameObject pauseScreen;
    public GameObject gameOverScreen;

    [Header("Game UI Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI comboText;
    public Slider timeSlider;

    [Header("Game Over Screen")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI fruitsSlicedText;
    public TextMeshProUGUI bombsTouchedText;
    public TextMeshProUGUI highScoreText;

    [Header("Combo System")]
    public GameObject comboProgressPanel;
    public Slider comboProgressSlider;
    public TextMeshProUGUI comboCountText;

    [Header("Effects")]
    public GameObject scoreGainEffect;
    public GameObject fruitSlicedEffect;
    public GameObject bombTouchedEffect;
    public GameObject comboAchievedEffect;
    public AnimationCurve comboScaleCurve;

    [Header("Life Display")]
    public GameObject[] lifeIcons;
    public Color lifeLostColor = Color.red;

    // Variables internes
    private int currentHighScore = 0;
    private Coroutine comboProgressCoroutine;
    private Coroutine timeWarningCoroutine;

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
        LoadHighScore();
    }

    public void InitializeUI()
    {
        // Cacher tous les écrans sauf le start screen
        if (startScreen != null) startScreen.SetActive(true);
        if (gameScreen != null) gameScreen.SetActive(false);
        if (pauseScreen != null) pauseScreen.SetActive(false);
        if (gameOverScreen != null) gameOverScreen.SetActive(false);

        // Initialiser les textes
        UpdateScore(0);
        UpdateTime(120f);
        UpdateLives(3);
        UpdateCombo(0);

        // Cacher les effets
        if (comboProgressPanel != null) comboProgressPanel.SetActive(false);
        if (scoreGainEffect != null) scoreGainEffect.SetActive(false);
        
        Debug.Log("🖥️ UI Manager initialized");
    }

    void LoadHighScore()
    {
        currentHighScore = PlayerPrefs.GetInt("HighScore", 0);
        Debug.Log($"🏆 High score loaded: {currentHighScore}");
    }

    void SaveHighScore()
    {
        if (GameManager.Instance.GetCurrentScore() > currentHighScore)
        {
            currentHighScore = GameManager.Instance.GetCurrentScore();
            PlayerPrefs.SetInt("HighScore", currentHighScore);
            PlayerPrefs.Save();
            Debug.Log($"🏆 New high score saved: {currentHighScore}");
        }
    }

    // ==================== GESTION DES ÉCRANS ====================

    public void ShowStartScreen()
    {
        SetScreenActive(startScreen);
        Debug.Log("🖥️ Showing start screen");
    }

    public void ShowGameScreen()
    {
        SetScreenActive(gameScreen);
        Debug.Log("🖥️ Showing game screen");
    }

    public void ShowPauseScreen()
    {
        SetScreenActive(pauseScreen);
        Debug.Log("🖥️ Showing pause screen");
    }

    public void ShowGameOverScreen(int finalScore, int fruitsSliced, int bombsTouched)
    {
        SetScreenActive(gameOverScreen);
        
        // Mettre à jour les textes de fin de jeu
        if (finalScoreText != null)
            finalScoreText.text = $"Score: {finalScore}";
        
        if (fruitsSlicedText != null)
            fruitsSlicedText.text = $"Fruits Coupés: {fruitsSliced}";
        
        if (bombsTouchedText != null)
            bombsTouchedText.text = $"Bombes Touchées: {bombsTouched}";

        // Vérifier et sauvegarder le high score
        SaveHighScore();
        if (highScoreText != null)
            highScoreText.text = $"Meilleur Score: {currentHighScore}";

        Debug.Log("🖥️ Showing game over screen");
    }

    private void SetScreenActive(GameObject screenToShow)
    {
        GameObject[] allScreens = { startScreen, gameScreen, pauseScreen, gameOverScreen };
        
        foreach (GameObject screen in allScreens)
        {
            if (screen != null)
                screen.SetActive(screen == screenToShow);
        }
    }

    // ==================== MISE À JOUR UI ====================

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void UpdateTime(float timeRemaining)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }

        if (timeSlider != null && GameManager.Instance != null)
        {
            float totalTime = GameManager.Instance.gameDuration;
            timeSlider.value = timeRemaining / totalTime;
        }
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null)
            livesText.text = $"Vies: {lives}";

        // Mettre à jour les icônes de vie
        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                {
                    lifeIcons[i].SetActive(i < lives);
                }
            }
        }
    }

    public void UpdateCombo(int combo)
    {
        if (comboText != null)
        {
            comboText.text = combo > 0 ? $"Combo: x{combo}" : "";
            
            // Animation du texte de combo
            if (combo > 0)
            {
                StartCoroutine(AnimateComboText());
            }
        }
    }

    // ==================== EFFETS VISUELS ====================

    public void ShowScoreGain(int points)
    {
        if (scoreGainEffect != null)
        {
            StartCoroutine(ShowTemporaryEffect(scoreGainEffect, 1f));
            
            // Afficher le texte des points gagnés
            TextMeshProUGUI pointsText = scoreGainEffect.GetComponentInChildren<TextMeshProUGUI>();
            if (pointsText != null)
            {
                pointsText.text = $"+{points}";
            }
        }
    }

    public void ShowFruitSlicedEffect(Vector3 position)
    {
        if (fruitSlicedEffect != null)
        {
            // Convertir la position monde en position écran
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(position);
            fruitSlicedEffect.transform.position = screenPosition;
            
            StartCoroutine(ShowTemporaryEffect(fruitSlicedEffect, 0.5f));
        }
    }

    public void ShowBombTouchedEffect()
    {
        if (bombTouchedEffect != null)
        {
            StartCoroutine(ShowTemporaryEffect(bombTouchedEffect, 1f));
        }
    }

    public void ShowComboProgress(int currentCombo, int maxCombo)
    {
        if (comboProgressPanel != null && comboProgressSlider != null && comboCountText != null)
        {
            comboProgressPanel.SetActive(true);
            comboProgressSlider.value = (float)currentCombo / maxCombo;
            comboCountText.text = $"{currentCombo}/{maxCombo}";

            // Redémarrer le coroutine de masquage
            if (comboProgressCoroutine != null)
                StopCoroutine(comboProgressCoroutine);
            comboProgressCoroutine = StartCoroutine(HideComboProgressAfterDelay(2f));
        }
    }

    public void ShowComboAchieved(int comboLevel)
    {
        if (comboAchievedEffect != null)
        {
            StartCoroutine(ShowTemporaryEffect(comboAchievedEffect, 2f));
            
            // Afficher le niveau du combo
            TextMeshProUGUI comboLevelText = comboAchievedEffect.GetComponentInChildren<TextMeshProUGUI>();
            if (comboLevelText != null)
            {
                comboLevelText.text = $"COMBO x{comboLevel}!";
            }
        }

        // Masquer la barre de progression du combo
        if (comboProgressPanel != null)
        {
            comboProgressPanel.SetActive(false);
        }
    }

    public void ShowLifeLost()
    {
        // Animation des icônes de vie
        if (lifeIcons != null && GameManager.Instance != null)
        {
            int currentLives = GameManager.Instance.GetCurrentLives();
            if (currentLives >= 0 && currentLives < lifeIcons.Length && lifeIcons[currentLives] != null)
            {
                StartCoroutine(AnimateLifeLost(lifeIcons[currentLives]));
            }
        }
    }

    public void ShowTimeWarning(bool show)
    {
        if (timeText != null)
        {
            if (show)
            {
                if (timeWarningCoroutine != null)
                    StopCoroutine(timeWarningCoroutine);
                timeWarningCoroutine = StartCoroutine(AnimateTimeWarning());
            }
            else
            {
                if (timeWarningCoroutine != null)
                    StopCoroutine(timeWarningCoroutine);
                timeText.color = Color.white;
            }
        }
    }

    // ==================== COROUTINES D'ANIMATION ====================

    private IEnumerator ShowTemporaryEffect(GameObject effect, float duration)
    {
        effect.SetActive(true);
        yield return new WaitForSeconds(duration);
        effect.SetActive(false);
    }

    private IEnumerator HideComboProgressAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (comboProgressPanel != null)
        {
            comboProgressPanel.SetActive(false);
        }
    }

    private IEnumerator AnimateComboText()
    {
        if (comboText == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 originalScale = comboText.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float scale = comboScaleCurve.Evaluate(progress);
            comboText.transform.localScale = originalScale * scale;
            yield return null;
        }

        comboText.transform.localScale = originalScale;
    }

    private IEnumerator AnimateLifeLost(GameObject lifeIcon)
    {
        if (lifeIcon == null) yield break;

        Image lifeImage = lifeIcon.GetComponent<Image>();
        if (lifeImage == null) yield break;

        Color originalColor = lifeImage.color;
        lifeImage.color = lifeLostColor;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            lifeImage.color = Color.Lerp(lifeLostColor, originalColor, elapsed / duration);
            yield return null;
        }

        lifeImage.color = originalColor;
    }

    private IEnumerator AnimateTimeWarning()
    {
        if (timeText == null) yield break;

        Color warningColor = Color.red;
        float blinkInterval = 0.3f;

        while (true)
        {
            timeText.color = warningColor;
            yield return new WaitForSeconds(blinkInterval);
            timeText.color = Color.white;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    // ==================== BOUTONS UI ====================

    public void OnStartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }

    public void OnRestartButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }

    public void OnPauseButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
    }

    public void OnResumeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    public void OnQuitButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }

    // ==================== DEBUG ====================

    [ContextMenu("Test Score Update")]
    public void TestScoreUpdate()
    {
        UpdateScore(999);
    }

    [ContextMenu("Test Game Over Screen")]
    public void TestGameOverScreen()
    {
        ShowGameOverScreen(1500, 45, 3);
    }

    [ContextMenu("Test Combo Effect")]
    public void TestComboEffect()
    {
        ShowComboAchieved(5);
    }
}