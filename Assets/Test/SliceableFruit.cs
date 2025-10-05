// SliceableFruit.cs
using UnityEngine;

public class SliceableFruit : MonoBehaviour
{
    [Header("Fruit Properties")]
    public int points = 10;
    public bool isSliced = false;
    
    void Start()
    {
        SetupPhysics();
    }
    
    void SetupPhysics()
    {
        // S'assurer d'avoir un Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        
        // Pas de gravité avant d'être coupé
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // S'assurer d'avoir un collider
        if (GetComponent<Collider>() == null)
        {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            collider.convex = true;
        }
    }
    
    // Optionnel: Faire flotter le fruit
    void Update()
    {
        if (!isSliced)
        {
            // Légère animation de flottement
            transform.Rotate(0, 30 * Time.deltaTime, 0);
            transform.position += new Vector3(0, Mathf.Sin(Time.time) * 0.001f, 0);
        }
    }
}