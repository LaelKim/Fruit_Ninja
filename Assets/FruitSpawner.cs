using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("Fruit Settings")]
    public GameObject[] fruitPrefabs;
    public Transform[] spawnPoints;

    [Header("Spawn Settings")]
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 3f;
    public float minForce = 8f;
    public float maxForce = 12f;
    public float destroyYLevel = -2f;

    private List<GameObject> activeFruits = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnFruitsRoutine());
        StartCoroutine(CleanupFruitsRoutine());
    }

    IEnumerator SpawnFruitsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
            SpawnRandomFruit();
        }
    }
    IEnumerator CleanupFruitsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            CleanupFallenFruits();
        
        }
    }

    void SpawnRandomFruit()
    {
        if (fruitPrefabs.Length == 0 || spawnPoints.Length == 0) return;
        
       
        GameObject fruitPrefab = fruitPrefabs[Random.Range(0, fruitPrefabs.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        GameObject fruit = Instantiate(fruitPrefab, spawnPoint.position, Random.rotation);
        activeFruits.Add(fruit);
        
        Rigidbody rb = fruit.GetComponent<Rigidbody>();
        if (rb == null)
            rb = fruit.AddComponent<Rigidbody>();
            
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    
        Vector3 force = Vector3.up * Random.Range(minForce, maxForce);
        rb.AddForce(force, ForceMode.Impulse);
        
        Vector3 torque = new Vector3(
            Random.Range(-5f, 5f),
            Random.Range(-5f, 5f), 
            Random.Range(-5f, 5f)
        );
        rb.AddTorque(torque, ForceMode.Impulse);
        
        Destroy(fruit, 10f);
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
                Destroy(activeFruits[i]);
                activeFruits.RemoveAt(i);
            }
        }
    }
}
