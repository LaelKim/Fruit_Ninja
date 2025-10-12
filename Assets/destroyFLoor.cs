using UnityEngine;

public class DestroyFloor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string[] tagsToDestroy = { "Fruit" };
    
    void OnTriggerEnter(Collider other)
    {
        foreach (string tag in tagsToDestroy)
        {
            if (other.CompareTag(tag))
            {
                Destroy(other.gameObject);
                return;
            }
        }
        
        // Alternative: vérifier par le nom (si vous n'utilisez pas de tags)
        if (other.gameObject.name.Contains("Slice") || other.gameObject.name.Contains("(Conc)"))
        {
            Destroy(other.gameObject);
        }
    }
}