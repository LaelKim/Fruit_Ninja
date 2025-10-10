// KatanaSlicer.cs - Version sans aucune obsolescence
using UnityEngine;

public class KatanaSlicer : MonoBehaviour
{
    [Header("Slicing Settings")]
    public float minSliceVelocity = 2.0f;
    public LayerMask fruitLayer = 1;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    
    private Slicer slicer;
    private Vector3[] velocityBuffer = new Vector3[10];
    private int bufferIndex = 0;
    private Vector3 lastPosition;
    
    void Start()
    {
        // ✅ CORRIGÉ: Utilisation de FindAnyObjectByType au lieu de FindObjectOfType
        slicer = FindAnyObjectByType<Slicer>();
        if (slicer == null)
        {
            Debug.LogError("No Slicer found in scene! Add a Slicer component to a GameObject.");
        }
        else
        {
            Debug.Log("KatanaSlicer initialized successfully on: " + gameObject.name);
        }
        
        lastPosition = transform.position;
        
        // Initialiser le buffer de vélocité
        for (int i = 0; i < velocityBuffer.Length; i++)
        {
            velocityBuffer[i] = Vector3.zero;
        }
    }

    void Update()
    {
        // Calculer et stocker la vélocité
        Vector3 currentVelocity = CalculateInstantVelocity();
        velocityBuffer[bufferIndex] = currentVelocity;
        bufferIndex = (bufferIndex + 1) % velocityBuffer.Length;
        
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (slicer == null) return;
        
        if (IsSliceableFruit(other.gameObject))
        {
            TrySliceFruit(other.gameObject);
        }
    }

    private bool IsSliceableFruit(GameObject obj)
    {
        // Vérifier le layer
        if (fruitLayer != 0 && ((1 << obj.layer) & fruitLayer) == 0)
            return false;
            
        // Vérifier que c'est un mesh et pas déjà un morceau coupé
        bool hasMesh = obj.GetComponent<MeshFilter>() != null;
        bool isNotSlice = !obj.name.Contains("Slice") && !obj.name.Contains("_Slice_");
        bool isNotKatana = !obj.CompareTag("Katana");
        
        return hasMesh && isNotSlice && isNotKatana;
    }

    private void TrySliceFruit(GameObject fruit)
    {
        Vector3 averageVelocity = CalculateAverageVelocity();
        float speed = averageVelocity.magnitude;
        
        if (speed > minSliceVelocity)
        {
            Vector3 slicePoint = GetSlicePoint(fruit);
            Vector3 sliceNormal = CalculateSliceNormal(averageVelocity);
            
            slicer.Slice(fruit, slicePoint, sliceNormal);
            Debug.Log($"✅ Sliced {fruit.name} | Speed: {speed:F2}");
        }
        else
        {
            Debug.Log($"❌ Too slow: {fruit.name} | Speed: {speed:F2} < {minSliceVelocity}");
        }
    }

    private Vector3 GetSlicePoint(GameObject fruit)
    {
        Collider fruitCollider = fruit.GetComponent<Collider>();
        if (fruitCollider != null)
        {
            return fruitCollider.ClosestPoint(transform.position);
        }
        return fruit.transform.position;
    }

    private Vector3 CalculateSliceNormal(Vector3 velocity)
    {
        // Utiliser la direction de la lame comme référence principale
        Vector3 bladeDirection = transform.forward;
        
        // Calculer une normale perpendiculaire au mouvement et à la lame
        Vector3 sliceNormal = Vector3.Cross(velocity.normalized, bladeDirection).normalized;
        
        // Fallback si le calcul échoue
        if (sliceNormal.sqrMagnitude < 0.1f)
        {
            sliceNormal = transform.right; // Côté de la lame
        }
        
        return sliceNormal;
    }

    private Vector3 CalculateInstantVelocity()
    {
        if (Time.deltaTime < 0.0001f) return Vector3.zero;
        return (transform.position - lastPosition) / Time.deltaTime;
    }

    private Vector3 CalculateAverageVelocity()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        for (int i = 0; i < velocityBuffer.Length; i++)
        {
            if (velocityBuffer[i] != Vector3.zero)
            {
                sum += velocityBuffer[i];
                count++;
            }
        }
        
        return count > 0 ? sum / count : Vector3.zero;
    }
    
    // Visualisation debug améliorée
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;
        
        Vector3 averageVelocity = CalculateAverageVelocity();
        float speed = averageVelocity.magnitude;
        
        // Couleur basée sur la vitesse
        Gizmos.color = speed > minSliceVelocity ? Color.green : Color.red;
        
        // Direction et intensité du mouvement
        Gizmos.DrawRay(transform.position, averageVelocity.normalized * 0.3f);
        
        // Sphère indiquant la vitesse actuelle
        Gizmos.color = speed > minSliceVelocity ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, speed * 0.1f);
        
        // Normale de coupe
        if (speed > 0.1f)
        {
            Vector3 sliceNormal = CalculateSliceNormal(averageVelocity);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, sliceNormal * 0.2f);
        }
        
        // Direction de la lame
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.2f);
    }
}