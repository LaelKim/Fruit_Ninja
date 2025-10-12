using UnityEngine;

public class FruitController : MonoBehaviour
{
    [Header("Fruit Settings")]
    public int pointsValue = 10;
    public bool isSpecial = false;

    private bool wasSliced = false;

    void Start()
    {
        // S'assurer que le fruit a un Rigidbody pour la physique
        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
        }
    }

    public void OnSliced()
    {
        if (wasSliced) return;
        
        wasSliced = true;
        
        // Notifier le GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFruitSliced(gameObject);
        }

        // Désactiver la physique et ajouter un effet de disparition
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Détruire après un délai
        Destroy(gameObject, 2f);
    }

    void OnBecameInvisible()
    {
        // Si le fruit sort de l'écran sans être coupé
        if (!wasSliced && GameManager.Instance != null)
        {
            GameManager.Instance.OnFruitMissed(gameObject);
            Destroy(gameObject, 1f);
        }
    }
}