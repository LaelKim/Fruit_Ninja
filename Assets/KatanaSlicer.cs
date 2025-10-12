using UnityEngine;

public class KatanaSlicer : MonoBehaviour
{
    [Header("Slicing Settings")]
    public float minSliceVelocity = 2.0f;
    public LayerMask fruitLayer = 1;
    
    [Header("Audio")]
    public bool enableSwipeSounds = true;
    public float minSwipeSpeedForSound = 1.0f;
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    
    private Slicer slicer;
    private Vector3[] velocityBuffer = new Vector3[10];
    private int bufferIndex = 0;
    private Vector3 lastPosition;
    private float currentSpeed;
    
    void Start()
    {
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
        
        for (int i = 0; i < velocityBuffer.Length; i++)
        {
            velocityBuffer[i] = Vector3.zero;
        }
    }

    void Update()
    {
        Vector3 currentVelocity = CalculateInstantVelocity();
        velocityBuffer[bufferIndex] = currentVelocity;
        bufferIndex = (bufferIndex + 1) % velocityBuffer.Length;
        
        currentSpeed = currentVelocity.magnitude;
        
        // Jouer le son de whoosh pendant le mouvement rapide
        if (enableSwipeSounds && currentSpeed > minSwipeSpeedForSound)
        {
            PlaySwipeSound();
        }
        
        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (slicer == null) return;
        
        // Détection des FRUITS
        if (IsSliceableFruit(other.gameObject))
        {
            TrySliceFruit(other.gameObject);
        }
        // Détection des BOMBES
        else if (IsBomb(other.gameObject))
        {
            TryTouchBomb(other.gameObject);
        }
    }

    private bool IsBomb(GameObject obj)
    {
        if (obj == null) return false;
        
        // Méthode 1: Vérifier par le tag (RECOMMANDÉE)
        if (obj.CompareTag("Bomb")) 
            return true;
        
        // Méthode 2: Vérifier par le composant
        BombController bomb = obj.GetComponent<BombController>();
        if (bomb != null) 
            return true;
        
        // Méthode 3: Vérifier par le nom
        if (obj.name.Contains("Bomb") || obj.name.Contains("bomb"))
            return true;
        
        return false;
    }

    private void TryTouchBomb(GameObject bombObject)
    {
        Vector3 averageVelocity = CalculateAverageVelocity();
        float speed = averageVelocity.magnitude;
        
        if (speed > minSliceVelocity)
        {
            BombController bomb = bombObject.GetComponent<BombController>();
            if (bomb != null)
            {
                bomb.OnTouch();
                Debug.Log($"💣 Katana touched bomb! Speed: {speed:F2}");
                
                // Son de touche de bombe
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayBombTouch();
                }
            }
        }
        else
        {
            Debug.Log($"❌ Too slow for bomb: {speed:F2} < {minSliceVelocity}");
        }
    }

    private bool IsSliceableFruit(GameObject obj)
    {
        if (fruitLayer != 0 && ((1 << obj.layer) & fruitLayer) == 0)
            return false;
            
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
            
            // NOUVEAU : Notifier le GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFruitSliced(fruit);
            }
            
            // Son de découpe de fruit
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayFruitSlice();
            }
        }
        else
        {
            Debug.Log($"❌ Too slow: {fruit.name} | Speed: {speed:F2} < {minSliceVelocity}");
        }
    }

    private void PlaySwipeSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayKatanaWhoosh();
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
        Vector3 bladeDirection = transform.forward;
        Vector3 sliceNormal = Vector3.Cross(velocity.normalized, bladeDirection).normalized;
        
        if (sliceNormal.sqrMagnitude < 0.1f)
        {
            sliceNormal = transform.right;
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
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;
        
        Vector3 averageVelocity = CalculateAverageVelocity();
        float speed = averageVelocity.magnitude;
        
        Gizmos.color = speed > minSliceVelocity ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, averageVelocity.normalized * 0.3f);
        
        Gizmos.color = speed > minSliceVelocity ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, speed * 0.1f);
        
        if (speed > 0.1f)
        {
            Vector3 sliceNormal = CalculateSliceNormal(averageVelocity);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, sliceNormal * 0.2f);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.2f);
    }
}