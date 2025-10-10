using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FruitSpawner : MonoBehaviour
{
    [Header("Fruit Settings")]
    public GameObject[] fruitPrefabs;
    public Transform[] spawnPoints;

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

    [Header("Spawn Patterns")]
    public int[] simultaneousSpawnCounts = { 1, 2, 3 };
    public float[] patternProbabilities = { 0.4f, 0.4f, 0.2f };
    public float simultaneousSpawnDelay = 0.1f;

    private List<GameObject> activeFruits = new List<GameObject>();
    private Dictionary<int, List<GameObject>> activeFruitsByLine = new Dictionary<int, List<GameObject>>();
    private bool[] spawnPointOccupied;
    private Coroutine spawnCoroutine;

    void Start()
    {
        spawnPointOccupied = new bool[spawnPoints.Length];
        InitializeFruitLines();
        StartSpawnSystem();
    }

    void InitializeFruitLines()
    {
        activeFruitsByLine.Clear();
        // Créer une entrée pour chaque ligne possible (basée sur la position X)
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int lineKey = GetLineKey(spawnPoints[i].position);
            if (!activeFruitsByLine.ContainsKey(lineKey))
            {
                activeFruitsByLine[lineKey] = new List<GameObject>();
            }
        }
    }

    int GetLineKey(Vector3 position)
    {
        // Arrondir la position X pour grouper par ligne
        // Ajustez la précision selon vos besoins (0.5f = lignes espacées de 0.5 unité)
        return Mathf.RoundToInt(position.x / 0.5f);
    }

    void StartSpawnSystem()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        
        spawnCoroutine = StartCoroutine(SpawnSystemRoutine());
        StartCoroutine(CleanupFruitsRoutine());
    }

    IEnumerator SpawnSystemRoutine()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
            
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
        spawnPointOccupied[spawnIndex] = true;

        yield return new WaitForSeconds(0.05f);

        if (fruitPrefabs.Length == 0) yield break;

        GameObject fruitPrefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];
        Transform spawnPoint = spawnPoints[spawnIndex];
        
        GameObject fruit = Instantiate(fruitPrefab, spawnPoint.position, GetRandomRotation());
        activeFruits.Add(fruit);
        
        // Enregistrer le fruit dans sa ligne
        int lineKey = GetLineKey(spawnPoint.position);
        if (!activeFruitsByLine.ContainsKey(lineKey))
        {
            activeFruitsByLine[lineKey] = new List<GameObject>();
        }
        activeFruitsByLine[lineKey].Add(fruit);
        
        SetupFruitPhysics(fruit, spawnIndex);
        
        StartCoroutine(ReleaseSpawnPoint(spawnIndex, 1f));
        
        Destroy(fruit, 10f);
    }

    void SetupFruitPhysics(GameObject fruit, int spawnIndex)
    {
        Rigidbody rb = fruit.GetComponent<Rigidbody>();
        if (rb == null)
            rb = fruit.AddComponent<Rigidbody>();
            
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = fruitLinearDamping;
        rb.angularDamping = fruitAngularDamping;
        rb.useGravity = true;
        rb.mass = Random.Range(0.8f, 1.2f);
        
        Vector3 forceDirection = GetForceDirection(spawnIndex);
        float forceMagnitude = Random.Range(minForce, maxForce);
        
        Vector3 force = forceDirection * forceMagnitude;
        rb.AddForce(force, ForceMode.Impulse);
        
        Vector3 torque = new Vector3(
            Random.Range(-20f, 20f),
            Random.Range(-20f, 20f), 
            Random.Range(-20f, 20f)
        );
        rb.AddTorque(torque, ForceMode.Impulse);
        
        StartCoroutine(ApplyCustomGravity(rb));
    }

    IEnumerator ApplyCustomGravity(Rigidbody rb)
    {
        while (rb != null)
        {
            if (rb.useGravity)
            {
                rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    Vector3 GetForceDirection(int spawnIndex)
    {
        return Vector3.up;
    }

    Quaternion GetRandomRotation()
    {
        return Random.rotation;
    }

    int GetAvailableSpawnPoint()
    {
        List<int> available = new List<int>();
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnPointOccupied[i] && IsLineAvailable(spawnPoints[i].position))
            {
                available.Add(i);
            }
        }
        
        return available.Count > 0 ? available[Random.Range(0, available.Count)] : -1;
    }

    List<int> GetAvailableSpawnPoints(int count)
    {
        List<int> available = new List<int>();
        
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!spawnPointOccupied[i] && IsLineAvailable(spawnPoints[i].position))
            {
                available.Add(i);
            }
        }
        
        // Mélanger les points disponibles
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
        
        // Vérifier si la ligne existe dans le dictionnaire
        if (!activeFruitsByLine.ContainsKey(lineKey))
            return true;
        
        // Nettoyer les fruits null de la ligne
        activeFruitsByLine[lineKey].RemoveAll(fruit => fruit == null);
        
        // La ligne est disponible s'il n'y a plus de fruits actifs
        return activeFruitsByLine[lineKey].Count == 0;
    }

    IEnumerator ReleaseSpawnPoint(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (index < spawnPointOccupied.Length)
            spawnPointOccupied[index] = false;
    }

    IEnumerator CleanupFruitsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            CleanupFallenFruits();
        }
    }

    void CleanupFallenFruits()
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
                // Retirer le fruit de sa ligne avant de le détruire
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
                    
                    // Couleur selon la disponibilité
                    if (!isLineAvailable)
                        Gizmos.color = Color.red;    // Ligne occupée
                    else if (isOccupied)
                        Gizmos.color = Color.yellow; // Point occupé mais ligne libre
                    else
                        Gizmos.color = Color.green;  // Complètement disponible
                    
                    Gizmos.DrawWireSphere(spawnPoints[i].position, 0.3f);
                    
                    // Afficher la ligne
                    Gizmos.color = Color.blue;
                    Vector3 lineStart = spawnPoints[i].position + Vector3.left * 2f;
                    Vector3 lineEnd = spawnPoints[i].position + Vector3.right * 2f;
                    Gizmos.DrawLine(lineStart, lineEnd);
                }
            }
        }
    }
}