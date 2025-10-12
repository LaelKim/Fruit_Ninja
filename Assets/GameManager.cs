using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    // Référence à votre FruitSpawner existant
    private FruitSpawner fruitSpawner;

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
        // Trouver votre FruitSpawner existant
        fruitSpawner = Object.FindFirstObjectByType<FruitSpawner>();
        
        if (fruitSpawner == null)
        {
            Debug.LogError("FruitSpawner non trouvé! Assurez-vous qu'il est dans la scène.");
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager non trouvé! Assurez-vous qu'il est dans la scène.");
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBackgroundMusic();
        }
    }

    // ==================== GESTION DU JEU ====================

    public void StartGame()
    {
        if (gameRunning) return;

        currentScore = 0;
        currentLives = startLives;
        gameTimeRemaining = gameDuration;
        gameRunning = true;
        
        consecutiveFruits = 0;
        totalFruitsSliced = 0;
        totalBombsTouched = 0;

        // Démarrer le timer du jeu
        gameTimerCoroutine = StartCoroutine(GameTimerCoroutine());

        // Activer le FruitSpawner (s'il a un système d'activation)
        if (fruitSpawner != null)
        {
            // Si votre FruitSpawner a une méthode StartSpawning, appelez-la
            // Sinon, il devrait déjà fonctionner automatiquement
            fruitSpawner.enabled = true;
        }

        // Notifier les événements
        OnScoreChanged?.Invoke(currentScore);
        OnLivesChanged?.Invoke(currentLives);
        OnTimeChanged?.Invoke(gameTimeRemaining);
        OnGameStateChanged?.Invoke(true);

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

        // Désactiver le FruitSpawner
        if (fruitSpawner != null)
        {
            fruitSpawner.enabled = false;
        }

        // Nettoyer les objets restants
        CleanupObjects();

        OnGameStateChanged?.Invoke(false);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOver();
        }

        Debug.Log($"🏁 Game Over! Score final: {currentScore}, Fruits coupés: {totalFruitsSliced}, Bombes touchées: {totalBombsTouched}");
    }

    // ==================== GESTION SCORE/FRUITS ====================

    public void OnFruitSliced(GameObject fruit)
    {
        if (!gameRunning || fruit == null) return;

        totalFruitsSliced++;
        
        float currentTime = Time.time;
        if (currentTime - lastFruitTime <= comboTimeWindow)
        {
            consecutiveFruits++;
            
            if (consecutiveFruits >= fruitsForCombo)
            {
                AwardCombo();
            }
        }
        else
        {
            consecutiveFruits = 1;
        }
        
        lastFruitTime = currentTime;

        AddScore(pointsPerFruit);

        Debug.Log($"🍉 Fruit coupé! +{pointsPerFruit} points (Combo: {consecutiveFruits})");
    }

    public void OnBombTouched()
    {
        if (!gameRunning) return;

        totalBombsTouched++;
        
        AddScore(-bombPenalty);
        LoseLife();
        consecutiveFruits = 0;

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
        OnComboAchieved?.Invoke(consecutiveFruits);

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
            
            if (gameTimeRemaining <= 0)
            {
                EndGame();
                yield break;
            }
            
            yield return null;
        }
    }

    private void CleanupObjects()
    {
        // Nettoyer les fruits et bombes restants
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
    }

    private void AddScore(int points)
    {
        currentScore = Mathf.Max(0, currentScore + points);
        OnScoreChanged?.Invoke(currentScore);
    }

    private void LoseLife()
    {
        currentLives--;
        OnLivesChanged?.Invoke(currentLives);

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

    // ==================== DEBUG ====================

    [ContextMenu("Force End Game")]
    public void ForceEndGame() => EndGame();

    [ContextMenu("Add Test Score")]
    public void AddTestScore() => AddScore(100);

    [ContextMenu("Lose Test Life")]
    public void LoseTestLife() => LoseLife();
}