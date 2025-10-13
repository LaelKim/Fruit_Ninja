using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int pointsPerFruit = 10;
    public int pointsPerCombo = 50;
    public int bombPenalty = 25;
    public int startLives = 3;
    public float gameDuration = 120f;

    [Header("Combo System")]
    public float comboTimeWindow = 2.0f;
    public int fruitsForCombo = 3;
    
    // Variables de jeu
    private int currentScore = 0;
    private int currentLives;
    private float gameTimeRemaining;
    private bool gameRunning = false;
    private int consecutiveFruits = 0;
    private float lastFruitTime = 0f;
    private int totalFruitsSliced = 0;
    private int totalBombsTouched = 0;

    private Coroutine gameTimerCoroutine;

    // Références
    private FruitSpawner fruitSpawner;
    private UIManager uiManager;

    // Événements
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnLivesChanged;
    public System.Action<float> OnTimeChanged;
    public System.Action<bool> OnGameStateChanged;
    public System.Action<int> OnComboAchieved;

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
        // CORRIGÉ: Utilisation de FindFirstObjectByType au lieu de FindObjectOfType
        fruitSpawner = FindFirstObjectByType<FruitSpawner>();
        uiManager = FindFirstObjectByType<UIManager>();
        
        if (fruitSpawner == null)
        {
            Debug.LogError("FruitSpawner non trouvé! Assurez-vous qu'il est dans la scène.");
        }

        if (uiManager == null)
        {
            Debug.LogError("UIManager non trouvé! Assurez-vous qu'il est dans la scène.");
        }

        // Initialiser l'UI
        if (uiManager != null)
        {
            uiManager.InitializeUI();
        }

        // Démarrer la musique
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBackgroundMusic();
        }

        Debug.Log("🎮 GameManager initialized");
    }

    // ==================== GESTION DU JEU ====================

    public void StartGame()
    {
        if (gameRunning) return;

        // Réinitialiser les variables
        currentScore = 0;
        currentLives = startLives;
        gameTimeRemaining = gameDuration;
        gameRunning = true;
        
        consecutiveFruits = 0;
        totalFruitsSliced = 0;
        totalBombsTouched = 0;

        // Réinitialiser et démarrer le spawner
        if (fruitSpawner != null)
        {
            fruitSpawner.InitializeSpawner();
            fruitSpawner.StartSpawnSystem();
        }

        // Démarrer le timer du jeu
        if (gameTimerCoroutine != null)
            StopCoroutine(gameTimerCoroutine);
        gameTimerCoroutine = StartCoroutine(GameTimerCoroutine());

        // Mettre à jour l'UI
        OnScoreChanged?.Invoke(currentScore);
        OnLivesChanged?.Invoke(currentLives);
        OnTimeChanged?.Invoke(gameTimeRemaining);
        OnGameStateChanged?.Invoke(true);

        // Afficher l'écran de jeu
        if (uiManager != null)
        {
            uiManager.ShowGameScreen();
        }

        // Son de début de jeu
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameStart();
        }

        Debug.Log("🎮 Jeu démarré!");
    }

    public void EndGame()
    {
        if (!gameRunning) return;

        gameRunning = false;

        // Arrêter le timer
        if (gameTimerCoroutine != null) 
        {
            StopCoroutine(gameTimerCoroutine);
            gameTimerCoroutine = null;
        }

        // Arrêter le spawner
        if (fruitSpawner != null)
        {
            fruitSpawner.StopSpawning();
        }

        // Nettoyer les objets restants
        CleanupObjects();

        // Afficher l'écran de fin
        if (uiManager != null)
        {
            uiManager.ShowGameOverScreen(currentScore, totalFruitsSliced, totalBombsTouched);
        }

        // Notifier les événements
        OnGameStateChanged?.Invoke(false);

        // Son de fin de jeu
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }

        Debug.Log($"🏁 Game Over! Score final: {currentScore}, Fruits coupés: {totalFruitsSliced}, Bombes touchées: {totalBombsTouched}");
    }

    public void RestartGame()
    {
        Debug.Log("🔄 Redémarrage du jeu...");
        
        // Nettoyer complètement
        CleanupObjects();
        
        // Redémarrer le jeu
        StartGame();
    }

    public void QuitGame()
    {
        Debug.Log("🚪 Quitting game...");
        
        // Arrêter le jeu
        if (gameRunning)
        {
            EndGame();
        }
        
        // Quitter l'application (ou retourner au menu)
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    // ==================== GESTION SCORE/FRUITS ====================

    public void OnFruitSliced(GameObject fruit)
    {
        if (!gameRunning || fruit == null) return;

        totalFruitsSliced++;
        
        // Gestion du combo
        float currentTime = Time.time;
        if (currentTime - lastFruitTime <= comboTimeWindow)
        {
            consecutiveFruits++;
            
            if (consecutiveFruits >= fruitsForCombo)
            {
                AwardCombo();
            }
            else
            {
                // Feedback visuel pour le combo en cours
                if (uiManager != null)
                {
                    uiManager.ShowComboProgress(consecutiveFruits, fruitsForCombo);
                }
            }
        }
        else
        {
            consecutiveFruits = 1;
        }
        
        lastFruitTime = currentTime;

        // Ajouter les points
        AddScore(pointsPerFruit);

        // Feedback visuel
        if (uiManager != null)
        {
            uiManager.ShowFruitSlicedEffect(fruit.transform.position);
        }

        Debug.Log($"🍉 Fruit coupé! +{pointsPerFruit} points (Combo: {consecutiveFruits})");
    }

    public void OnBombTouched()
    {
        if (!gameRunning) return;

        totalBombsTouched++;
        
        AddScore(-bombPenalty);
        LoseLife();
        consecutiveFruits = 0;

        // Feedback visuel
        if (uiManager != null)
        {
            uiManager.ShowBombTouchedEffect();
        }

        Debug.Log($"💣 Bombe touchée! -{bombPenalty} points, Vies restantes: {currentLives}");
    }

    public void OnFruitMissed(GameObject fruit)
    {
        if (!gameRunning) return;

        AddScore(-5);
        consecutiveFruits = 0;

        Debug.Log("❌ Fruit manqué! -5 points");
    }

    private void AwardCombo()
    {
        AddScore(pointsPerCombo);
        
        // Notifier le combo
        OnComboAchieved?.Invoke(consecutiveFruits);

        // Feedback visuel
        if (uiManager != null)
        {
            uiManager.ShowComboAchieved(consecutiveFruits);
        }

        // Son
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayScoreBonus();
        }

        Debug.Log($"🔥 Combo x{consecutiveFruits}! +{pointsPerCombo} points");
        
        consecutiveFruits = 0;
    }

    private IEnumerator GameTimerCoroutine()
    {
        while (gameTimeRemaining > 0 && gameRunning)
        {
            gameTimeRemaining -= Time.deltaTime;
            OnTimeChanged?.Invoke(gameTimeRemaining);
            
            // Avertissement temps faible
            if (gameTimeRemaining <= 10f)
            {
                if (uiManager != null)
                {
                    uiManager.ShowTimeWarning(true);
                }
                
                if (gameTimeRemaining <= 0)
                {
                    EndGame();
                    yield break;
                }
            }
            
            yield return null;
        }
        
        if (gameTimeRemaining <= 0)
        {
            EndGame();
        }
    }

    private void CleanupObjects()
    {
        // CORRIGÉ: Utilisation de FindObjectsByType avec le nouveau paramètre
        FruitController[] fruits = FindObjectsByType<FruitController>(FindObjectsSortMode.None);
        foreach (FruitController fruit in fruits)
        {
            if (fruit != null && fruit.gameObject != null)
                Destroy(fruit.gameObject);
        }

        BombController[] bombs = FindObjectsByType<BombController>(FindObjectsSortMode.None);
        foreach (BombController bomb in bombs)
        {
            if (bomb != null && bomb.gameObject != null)
                Destroy(bomb.gameObject);
        }

        // Nettoyer les morceaux de fruits coupés
        GameObject[] slices = GameObject.FindGameObjectsWithTag("Slice");
        foreach (GameObject slice in slices)
        {
            if (slice != null)
                Destroy(slice);
        }

        Debug.Log("🧹 All game objects cleaned up");
    }

    private void AddScore(int points)
    {
        int previousScore = currentScore;
        currentScore = Mathf.Max(0, currentScore + points);
        OnScoreChanged?.Invoke(currentScore);

        // Feedback de score
        if (uiManager != null && points > 0)
        {
            uiManager.ShowScoreGain(points);
        }
    }

    private void LoseLife()
    {
        currentLives--;
        OnLivesChanged?.Invoke(currentLives);

        // Feedback visuel de perte de vie
        if (uiManager != null)
        {
            uiManager.ShowLifeLost();
        }

        if (currentLives <= 0)
        {
            EndGame();
        }
    }

    // ==================== METHODES PUBLIQUES ====================

    public bool IsGameRunning() => gameRunning;
    public int GetCurrentScore() => currentScore;
    public int GetCurrentLives() => currentLives;
    public float GetTimeRemaining() => gameTimeRemaining;
    public int GetTotalFruitsSliced() => totalFruitsSliced;
    public int GetTotalBombsTouched() => totalBombsTouched;

    // ==================== PAUSE/RESUME ====================

    public void PauseGame()
    {
        if (!gameRunning) return;

        Time.timeScale = 0f;
        
        if (uiManager != null)
        {
            uiManager.ShowPauseScreen();
        }

        Debug.Log("⏸️ Game paused");
    }

    public void ResumeGame()
    {
        if (!gameRunning) return;

        Time.timeScale = 1f;
        
        if (uiManager != null)
        {
            uiManager.ShowGameScreen();
        }

        Debug.Log("▶️ Game resumed");
    }

    // ==================== DEBUG ====================

    [ContextMenu("Force Start Game")]
    public void DebugStartGame() => StartGame();

    [ContextMenu("Force End Game")]
    public void DebugEndGame() => EndGame();

    [ContextMenu("Add 100 Points")]
    public void DebugAddScore() => AddScore(100);

    [ContextMenu("Lose One Life")]
    public void DebugLoseLife() => LoseLife();

    [ContextMenu("Simulate Fruit Slice")]
    public void DebugSliceFruit()
    {
        OnFruitSliced(null);
    }

    [ContextMenu("Simulate Bomb Touch")]
    public void DebugTouchBomb()
    {
        OnBombTouched();
    }
}