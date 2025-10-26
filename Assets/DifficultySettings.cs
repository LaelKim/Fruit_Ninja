using UnityEngine;

[System.Serializable]
public class DifficultySettings
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Extreme
    }

    [Header("Current Difficulty")]
    public Difficulty currentDifficulty = Difficulty.Normal;

    [Header("Easy Settings")]
    public int easyLives = 5;
    public float easyMinSpawnInterval = 1.0f;
    public float easyMaxSpawnInterval = 2.5f;
    public float easyBombChance = 0.05f;

    [Header("Normal Settings")]
    public int normalLives = 3;
    public float normalMinSpawnInterval = 0.5f;
    public float normalMaxSpawnInterval = 1.5f;
    public float normalBombChance = 0.1f;

    [Header("Hard Settings")]
    public int hardLives = 2;
    public float hardMinSpawnInterval = 0.3f;
    public float hardMaxSpawnInterval = 1.0f;
    public float hardBombChance = 0.15f;

    [Header("Extreme Settings")]
    public int extremeLives = 1;
    public float extremeMinSpawnInterval = 0.2f;
    public float extremeMaxSpawnInterval = 0.7f;
    public float extremeBombChance = 0.2f;

    // Méthodes pour récupérer les paramètres selon la difficulté
    public int GetLives()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return easyLives;
            case Difficulty.Normal: return normalLives;
            case Difficulty.Hard: return hardLives;
            case Difficulty.Extreme: return extremeLives;
            default: return normalLives;
        }
    }

    public float GetMinSpawnInterval()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return easyMinSpawnInterval;
            case Difficulty.Normal: return normalMinSpawnInterval;
            case Difficulty.Hard: return hardMinSpawnInterval;
            case Difficulty.Extreme: return extremeMinSpawnInterval;
            default: return normalMinSpawnInterval;
        }
    }

    public float GetMaxSpawnInterval()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return easyMaxSpawnInterval;
            case Difficulty.Normal: return normalMaxSpawnInterval;
            case Difficulty.Hard: return hardMaxSpawnInterval;
            case Difficulty.Extreme: return extremeMaxSpawnInterval;
            default: return normalMaxSpawnInterval;
        }
    }

    public float GetBombChance()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return easyBombChance;
            case Difficulty.Normal: return normalBombChance;
            case Difficulty.Hard: return hardBombChance;
            case Difficulty.Extreme: return extremeBombChance;
            default: return normalBombChance;
        }
    }

    public string GetDifficultyName()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return "Facile";
            case Difficulty.Normal: return "Normal";
            case Difficulty.Hard: return "Difficile";
            case Difficulty.Extreme: return "Extrême";
            default: return "Normal";
        }
    }
}