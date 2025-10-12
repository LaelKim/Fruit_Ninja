using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FruitSpawner : MonoBehaviour
{
    [Header("Fruit Settings")]
    public GameObject[] fruitPrefabs;
    public Transform[] spawnPoints;

    [Header("Bomb Settings")] 
    [Range(0f, 1f)]
    public float bombSpawnChance = 0.1f; // 10% de chance
    public int bombPrefabIndex = 8;

    [Header("Spawn Settings")]
    public float minSpawnInterval = 0.5f;
    public float maxSpawnInterval = 1.5f;
    public float minForce = 10f;
    public float maxForce = 15f;
    public float destroyYLevel = -2f;
    
    [Header("Game Physics")]
    public float gravityScale = 0.8f;
    public float fruitLinearDamping = 0.1f;
    public float fruitAngularDamping = 0.05f;

    [Header("Rotation Settings")]
    [Range(0f, 50f)]
    public float maxRotationStrength = 20f; 
    public bool randomizeRotationAxis = true;

    [Header("Spawn Patterns")]
    public int[] simultaneousSpawnCounts = { 1, 2, 3 };
    public float[] patternProbabilities = { 0.4f, 0.4f, 0.2f };
    public float simultaneousSpawnDelay = 0.1f;

    private List<GameObject> activeFruits = new List<GameObject>();
    private Dictionary<int, List<GameObject>> activeFruitsByLine = new Dictionary<int, List<GameObject>>();
    private bool[] spawnPointOccupied;
    private Coroutine spawnCoroutine;
    private Coroutine cleanupCoroutine;

    void Start()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            spawnPointOccupied = new bool[spawnPoints.Length];
            InitializeFruitLines();
        }
        else
        {
            Debug.LogError("❌ Aucun spawn point configuré!");
        }
    }

    void Update()
    {
        // Synchroniser avec le GameManager
        if (GameManager.Instance != null)
        {
            // Arrêter le spawn si le jeu n'est pas en cours
            if (!GameManager.Instance.IsGameRunning() && spawnCoroutine != null)
            {
                StopSpawning();
            }
            // Redémarrer le spawn si le jeu commence
            else if (GameManager.Instance.IsGameRunning() && spawnCoroutine == null)
            {
                StartSpawnSystem();
            }
        }
    }

    void InitializeFruitLines()
    {
        activeFruitsByLine.Clear();
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                int lineKey = GetLineKey(spawnPoints[i].position);
                if (!activeFruitsByLine.ContainsKey(lineKey))
                {
                    activeFruitsByLine[lineKey] = new List<GameObject>();
                }
            }
        }
    }

    int GetLineKey(Vector3 position)
    {
        return Mathf.RoundToInt(position.x / 0.5f);
    }

    // ⭐ MÉTHODE MANQUANTE AJOUTÉE : GetRandomRotation
    Quaternion GetRandomRotation()
    {
        return Random.rotation;
    }

    public void StartSpawnSystem()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        
        spawnCoroutine = StartCoroutine(SpawnSystemRoutine());
        cleanupCoroutine = StartCoroutine(CleanupFallenFruitsRoutine());
        
        Debug.Log("🎮 Fruit spawning system started");
    }

    IEnumerator SpawnSystemRoutine()
    {
        yield return new WaitForSeconds(2f);

        while (GameManager.Instance != null && GameManager.Instance.IsGameRunning())
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
            
            if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning())
                yield break;
            
            int spawnCount = GetRandomSpawnCount();
            
            if (spawnCount == 1)
            {
                SpawnSingleFruit();
            }
            else
            {
                yield return StartCoroutine(SpawnMultipleFruits(spawnCount));
            }
        }
    }

    int GetRandomSpawnCount()
    {
        float randomValue = Random.value;
        float cumulative = 0f;

        for (int i = 0; i < patternProbabilities.Length; i++)
        {
            cumulative += patternProbabilities[i];
            if (randomValue <= cumulative)
                return simultaneousSpawnCounts[i];
        }

        return 1;
    }

    void SpawnSingleFruit()
    {
        int availableSpawnPoint = GetAvailableSpawnPoint();
        if (availableSpawnPoint != -1)
        {
            StartCoroutine(SpawnFruitAtPoint(availableSpawnPoint));
        }
    }

    IEnumerator SpawnMultipleFruits(int count)
    {
        List<int> availablePoints = GetAvailableSpawnPoints(count);
        
        if (availablePoints.Count >= count)
        {
            foreach (int spawnIndex in availablePoints)
            {
                if (count <= 0) break;
                
                StartCoroutine(SpawnFruitAtPoint(spawnIndex));
                count--;
                yield return new WaitForSeconds(simultaneousSpawnDelay);
            }
        }
    }

    IEnumerator SpawnFruitAtPoint(int spawnIndex)
    {
        // Vérifications de sécurité
        if (spawnIndex < 0 || spawnIndex >= spawnPointOccupied.Length) yield break;
        if (fruitPrefabs == null || fruitPrefabs.Length == 0) yield break;
        if (spawnPoints == null || spawnIndex >= spawnPoints.Length || spawnPoints[spawnIndex] == null) yield break;

        spawnPointOccupied[spawnIndex] = true;
        yield return new WaitForSeconds(0.05f);

        GameObject fruitPrefab = ChooseFruitOrBomb();
        
        if (fruitPrefab == null)
        {
            StartCoroutine(ReleaseSpawnPoint(spawnIndex, 0.1f));
            yield break;
        }

        Transform spawnPoint = spawnPoints[spawnIndex];
        GameObject fruit = Instantiate(fruitPrefab, spawnPoint.position, GetRandomRotation()); // ✅ Maintenant ça fonctionne!
        
        if (fruit != null)
        {
            EnsureFruitComponents(fruit);
            
            activeFruits.Add(fruit);
            
            int lineKey = GetLineKey(spawnPoint.position);
            if (!activeFruitsByLine.ContainsKey(lineKey))
            {
                activeFruitsByLine[lineKey] = new List<GameObject>();
            }
            activeFruitsByLine[lineKey].Add(fruit);
            
            SetupFruitPhysics(fruit, spawnIndex);
        }
        
        StartCoroutine(ReleaseSpawnPoint(spawnIndex, 1f));
        
        if (fruit != null)
        {
            Destroy(fruit, 10f);
        }
    }

    void EnsureFruitComponents(GameObject fruit)
    {
        if (fruit == null) return;

        if (fruit.GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = fruit.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (fruit.GetComponent<FruitController>() == null && !IsBomb(fruit))
        {
            fruit.AddComponent<FruitController>();
        }

        if (IsBomb(fruit) && fruit.GetComponent<BombController>() == null)
        {
            fruit.AddComponent<BombController>();
        }
    }

    private GameObject ChooseFruitOrBomb()
    {
        if (ShouldSpawnBomb() && IsBombAvailable())
        {
            Debug.Log("💣 Spawning a BOMB!");
            return fruitPrefabs[bombPrefabIndex];
        }
        else
        {
            return GetRandomFruit();
        }
    }

    private bool ShouldSpawnBomb()
    {
        return Random.value < bombSpawnChance;
    }

    private bool IsBombAvailable()
    {
        return bombPrefabIndex >= 0 && 
               bombPrefabIndex < fruitPrefabs.Length && 
               fruitPrefabs[bombPrefabIndex] != null;
    }

    private GameObject GetRandomFruit()
    {
        if (fruitPrefabs == null || fruitPrefabs.Length == 0) return null;
        if (fruitPrefabs.Length <= 1) return fruitPrefabs[0];
        
        List<int> validIndices = new List<int>();
        for (int i = 0; i < fruitPrefabs.Length; i++)
        {
            if (i != bombPrefabIndex && fruitPrefabs[i] != null)
            {
                validIndices.Add(i);
            }
        }
        
        if (validIndices.Count == 0)
        {
            for (int i = 0; i < fruitPrefabs.Length; i++)
            {
                if (fruitPrefabs[i] != null) return fruitPrefabs[i];
            }
            return fruitPrefabs[0];
        }
        
        int randomIndex = validIndices[Random.Range(0, validIndices.Count)];
        return fruitPrefabs[randomIndex];
    }

    private bool IsBomb(GameObject obj)
    {
        if (obj == null) return false;
        BombController bomb = obj.GetComponent<BombController>();
        return bomb != null || obj.name.ToLower().Contains("bomb");
    }

    void SetupFruitPhysics(GameObject fruit, int spawnIndex)
    {
        if (fruit == null) return;

        Rigidbody rb = fruit.GetComponent<Rigidbody>();
        if (rb == null) return;
            
        rb.linearDamping = fruitLinearDamping;
        rb.angularDamping = fruitAngularDamping;
        rb.useGravity = true;
        rb.mass = Random.Range(0.8f, 1.2f);
        
        Vector3 forceDirection = Vector3.up;
        float forceMagnitude = Random.Range(minForce, maxForce);
        
        Vector3 force = forceDirection * forceMagnitude;
        rb.AddForce(force, ForceMode.Impulse);

        Vector3 torque = CalculateTorque();
        rb.AddTorque(torque, ForceMode.Impulse);
        
        StartCoroutine(ApplyCustomGravity(rb));
    }

    Vector3 CalculateTorque()
    {
        if (maxRotationStrength <= 0f)
            return Vector3.zero;

        if (randomizeRotationAxis)
        {
            return new Vector3(
                Random.Range(-maxRotationStrength, maxRotationStrength),
                Random.Range(-maxRotationStrength, maxRotationStrength), 
                Random.Range(-maxRotationStrength, maxRotationStrength)
            );
        }
        else
        {
            return new Vector3(
                Random.Range(-maxRotationStrength * 0.2f, maxRotationStrength * 0.2f),
                Random.Range(-maxRotationStrength, maxRotationStrength), 
                Random.Range(-maxRotationStrength * 0.2f, maxRotationStrength * 0.2f)
            );
        }
    }

    IEnumerator ApplyCustomGravity(Rigidbody rb)
    {
        if (rb == null) yield break;

        while (rb != null && (GameManager.Instance == null || GameManager.Instance.IsGameRunning()))
        {
            if (rb.useGravity)
            {
                rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    int GetAvailableSpawnPoint()
    {
        if (spawnPoints == null || spawnPointOccupied == null) return -1;

        List<int> available = new List<int>();
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && 
                i < spawnPointOccupied.Length && 
                !spawnPointOccupied[i] && 
                IsLineAvailable(spawnPoints[i].position))
            {
                available.Add(i);
            }
        }
        
        return available.Count > 0 ? available[Random.Range(0, available.Count)] : -1;
    }

    List<int> GetAvailableSpawnPoints(int count)
    {
        List<int> available = new List<int>();
        if (spawnPoints == null || spawnPointOccupied == null) return available;
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null && 
                i < spawnPointOccupied.Length && 
                !spawnPointOccupied[i] && 
                IsLineAvailable(spawnPoints[i].position))
            {
                available.Add(i);
            }
        }
        
        for (int i = 0; i < available.Count; i++)
        {
            int temp = available[i];
            int randomIndex = Random.Range(i, available.Count);
            available[i] = available[randomIndex];
            available[randomIndex] = temp;
        }
        
        return available.GetRange(0, Mathf.Min(count, available.Count));
    }

    bool IsLineAvailable(Vector3 position)
    {
        int lineKey = GetLineKey(position);
        
        if (!activeFruitsByLine.ContainsKey(lineKey))
            return true;
        
        activeFruitsByLine[lineKey].RemoveAll(fruit => fruit == null);
        
        return activeFruitsByLine[lineKey].Count == 0;
    }

    IEnumerator ReleaseSpawnPoint(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (index >= 0 && index < spawnPointOccupied.Length)
            spawnPointOccupied[index] = false;
    }

    IEnumerator CleanupFallenFruitsRoutine()
    {
        while (GameManager.Instance == null || GameManager.Instance.IsGameRunning())
        {
            yield return new WaitForSeconds(1f);
            CleanupFallenFruitsOnly();
        }
    }

    void CleanupFallenFruitsOnly()
    {
        for (int i = activeFruits.Count - 1; i >= 0; i--)
        {
            if (activeFruits[i] == null)
            {
                activeFruits.RemoveAt(i);
                continue;
            }
            
            if (activeFruits[i].transform.position.y < destroyYLevel)
            {
                if (!IsBomb(activeFruits[i]))
                {
                    FruitController fruitController = activeFruits[i].GetComponent<FruitController>();
                    if (fruitController != null && GameManager.Instance != null)
                    {
                        GameManager.Instance.OnFruitMissed(activeFruits[i]);
                    }
                }
                
                RemoveFruitFromLine(activeFruits[i]);
                Destroy(activeFruits[i]);
                activeFruits.RemoveAt(i);
            }
        }
    }

    void RemoveFruitFromLine(GameObject fruit)
    {
        foreach (var line in activeFruitsByLine)
        {
            if (line.Value.Contains(fruit))
            {
                line.Value.Remove(fruit);
                break;
            }
        }
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
            cleanupCoroutine = null;
        }
        
        Debug.Log("🛑 Fruit spawning stopped");
    }

    void OnDrawGizmos()
    {
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    bool isOccupied = spawnPointOccupied != null && i < spawnPointOccupied.Length && spawnPointOccupied[i];
                    bool isLineAvailable = IsLineAvailable(spawnPoints[i].position);
                    
                    if (!isLineAvailable)
                        Gizmos.color = Color.red;
                    else if (isOccupied)
                        Gizmos.color = Color.yellow;
                    else
                        Gizmos.color = Color.green;
                    
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 0.3f);
                    
                    Gizmos.color = Color.blue;
                    Vector3 lineStart = spawnPoints[i].position + Vector3.left * 2f;
                    Vector3 lineEnd = spawnPoints[i].position + Vector3.right * 2f;
                    Gizmos.DrawLine(lineStart, lineEnd);
                }
            }
        }
    }
}