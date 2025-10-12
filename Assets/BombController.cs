using UnityEngine;
using System.Collections;
using System.Linq;

public class BombController : MonoBehaviour
{
    [Header("Physics")]
    public bool freezeOnExplode = true;

    [Header("Touch Settings")]
    public int touchesToExplode = 3;

    [Header("Shake Settings")]
    public float shakeDuration = 0.8f;
    public float shakeAmplitude = 0.15f;
    public float shakeFrequency = 12f;

    [Header("Explosion Settings")]
    [Tooltip("Peut être nul sur un clone : sera créé dynamiquement.")]
    public GameObject eclipseQuad;
    [Tooltip("Durée du fade out du flash")]
    public float eclipseFadeOutDuration = 0.5f;

    [Header("Audio - Legacy Fallback")]
    public AudioClip touchSound;
    public AudioClip explosionSound;

    [Header("Debug")]
    public bool debugLogs = true;

    private int hitCount = 0;
    private bool isExploding = false;
    private Vector3 originalPosition;
    private AudioSource audioSource;

    // Mat/renderer internes du flash
    private Material _flashMat;
    private MeshRenderer _flashRenderer;

    public System.Action OnBombExploded;

    void Start()
    {
        originalPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        if (eclipseQuad != null) eclipseQuad.SetActive(false);
    }

    void Update()
    {
        // DEBUG (facultatif) : espace = toucher
        if (Input.GetKeyDown(KeyCode.Space)) OnTouch();
    }

    public void OnTouch()
    {
        if (isExploding) return;

        hitCount++;
        
        // NOUVEAU : Notifier le GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBombTouched();
        }
        
        // Son de touche de bombe
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBombTouch();
        }
        else // Fallback à l'ancien système
        {
            if (touchSound && audioSource) 
                audioSource.PlayOneShot(touchSound);
        }
        
        if (debugLogs) Debug.Log($"[Bomb] Touch {hitCount}/{touchesToExplode}", this);

        if (hitCount >= touchesToExplode)
        {
            FreezePhysicsOnExplode();
            StartExplosionSequence();
        }
    }

    private void FreezePhysicsOnExplode()
    {
        if (!freezeOnExplode) return;

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[Bomb] Pas de Rigidbody sur la bombe, rien à figer.", this);
            return;
        }

        Vector3 currentPosition = transform.position;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        transform.position = currentPosition;
    }

    private void StartExplosionSequence()
    {
        isExploding = true;
        originalPosition = transform.position;

        if (debugLogs) Debug.Log("[Bomb] Start explosion sequence", this);
        OnBombExploded?.Invoke();

        FreezePhysicsOnExplode();
        StartCoroutine(ExplosionCoroutine());
    }

    private IEnumerator ExplosionCoroutine()
    {
        yield return ShakeCoroutine();          // 1) shake
        yield return FlashCoroutineFinal();     // 2) flash blanc
        ResetBomb();                            // 3) reset
    }

    private IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float x = Mathf.PerlinNoise(Time.time * shakeFrequency, 0) * 2 - 1;
            float y = Mathf.PerlinNoise(0, Time.time * shakeFrequency) * 2 - 1;
            float z = Mathf.PerlinNoise(Time.time * shakeFrequency, Time.time * shakeFrequency) * 2 - 1;

            transform.position = originalPosition + new Vector3(x, y, z) * shakeAmplitude;
            yield return null;
        }
        transform.position = originalPosition;
    }

    // -------------------- FLASH BLANC --------------------
    private IEnumerator FlashCoroutineFinal()
    {
        // Son d'explosion
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBombExplosion();
        }
        else // Fallback
        {
            if (explosionSound != null && audioSource != null)
                audioSource.PlayOneShot(explosionSound);
        }

        yield return new WaitForSeconds(0.3f);

        EnsureFlashQuad(); // quad + material prêts

        if (eclipseQuad != null && _flashRenderer != null && _flashMat != null)
        {
            // 1) Apparition instantanée : blanc opaque
            eclipseQuad.SetActive(true);
            SetMatAlpha(_flashMat, 1f);

            // Court maintien pour l'effet "bang"
            yield return new WaitForSeconds(0.1f);

            // 2) Fade out → 0
            float t = 0f;
            float fadeOut = Mathf.Max(0.01f, eclipseFadeOutDuration);

            if (debugLogs) Debug.Log($"[Bomb] Starting flash fade out: {fadeOut}s");

            while (t < fadeOut)
            {
                t += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, t / fadeOut);
                SetMatAlpha(_flashMat, a);
                
                if (debugLogs && t % 0.1f < Time.deltaTime)
                    Debug.Log($"[Bomb] Flash alpha: {a:F2}");
                    
                yield return null;
            }
            SetMatAlpha(_flashMat, 0f);

            eclipseQuad.SetActive(false);
            if (debugLogs) Debug.Log("[Bomb] Flash completed");
        }
        else
        {
            if (debugLogs) Debug.LogWarning("[Bomb] Flash quad not properly initialized");
        }

        yield return new WaitForSeconds(0.05f);
    }

    // Crée/Configure le quad + matériau (URP ou Built-in), parenté caméra
    private void EnsureFlashQuad()
    {
        Camera cam = Camera.main;
#if UNITY_2022_2_OR_NEWER
        if (cam == null)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            cam = cams.FirstOrDefault(c => c.isActiveAndEnabled);
        }
#else
        if (cam == null && Camera.allCamerasCount > 0)
            cam = Camera.allCameras[0];
#endif
        if (cam == null)
        {
            if (debugLogs) Debug.LogWarning("[Bomb] Aucune caméra trouvée pour le flash.", this);
            return;
        }

        if (eclipseQuad == null)
        {
            eclipseQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            eclipseQuad.name = "DynamicFlashQuad";
            var collider = eclipseQuad.GetComponent<Collider>();
            if (collider != null) 
            {
                DestroyImmediate(collider);
            }
        }

        eclipseQuad.transform.SetParent(cam.transform, worldPositionStays: false);
        eclipseQuad.transform.localPosition = new Vector3(0f, 0f, 0.3f);
        eclipseQuad.transform.localRotation = Quaternion.identity;
        eclipseQuad.transform.localScale = new Vector3(2f, 2f, 1f);

        _flashRenderer = eclipseQuad.GetComponent<MeshRenderer>();
        if (_flashRenderer == null)
        {
            if (debugLogs) Debug.LogError("[Bomb] Failed to get MeshRenderer from flash quad");
            return;
        }

        if (_flashMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit"); // URP
            if (shader == null) shader = Shader.Find("Unlit/Transparent");  // Built-in transparent
            if (shader == null) shader = Shader.Find("Unlit/Color");        // Built-in
            if (shader == null) shader = Shader.Find("Standard");           // fallback

            if (shader == null)
            {
                if (debugLogs) Debug.LogError("[Bomb] No suitable shader found for flash!");
                return;
            }

            _flashMat = new Material(shader);
            if (debugLogs) Debug.Log($"[Bomb] Created flash material with shader: {shader.name}");

            // Couleur BLANCHE (alpha 0 au repos)
            SetMatColor(_flashMat, new Color(1f, 1f, 1f, 0f));

            // Configuration blending/transparence
            _flashMat.SetOverrideTag("RenderType", "Transparent");
            _flashMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _flashMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _flashMat.DisableKeyword("_ALPHATEST_ON");
            _flashMat.EnableKeyword("_ALPHABLEND_ON");
            _flashMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            _flashMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // URP : Transparent + Alpha
            if (_flashMat.HasProperty("_Surface")) _flashMat.SetFloat("_Surface", 1f);
            if (_flashMat.HasProperty("_Blend"))   _flashMat.SetFloat("_Blend", 0f);
            if (_flashMat.HasProperty("_QueueControl")) _flashMat.SetFloat("_QueueControl", 1f);

            // Affichage "au-dessus de tout" si dispo
            if (_flashMat.HasProperty("_ZWrite")) _flashMat.SetInt("_ZWrite", 0);
            if (_flashMat.HasProperty("_ZTest"))  _flashMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

            // S'assurer que la couleur est bien blanche
            SetMatColor(_flashMat, new Color(1f, 1f, 1f, 0f));
        }

        _flashRenderer.material = _flashMat;
        eclipseQuad.SetActive(false);
        
        if (debugLogs) Debug.Log("[Bomb] Flash quad initialized successfully");
    }

    // Helpers matériaux (gère _BaseColor ou _Color)
    private static void SetMatColor(Material m, Color c)
    {
        if (m == null) return;
        
        if (m.HasProperty("_BaseColor")) 
        {
            m.SetColor("_BaseColor", c);
        }
        else if (m.HasProperty("_Color"))     
        {
            m.SetColor("_Color", c);
        }
        else
        {
            Debug.LogWarning("[Bomb] Material has no _BaseColor or _Color property");
        }
    }

    private static void SetMatAlpha(Material m, float a)
    {
        if (m == null) return;
        
        Color c = Color.white;
        
        if (m.HasProperty("_BaseColor")) 
        {
            c = m.GetColor("_BaseColor");
        }
        else if (m.HasProperty("_Color")) 
        {
            c = m.GetColor("_Color");
        }
        
        c.a = a;
        SetMatColor(m, c);
    }

    private void ResetBomb()
    {
        hitCount = 0;
        isExploding = false;
        
        if (debugLogs) Debug.Log("[Bomb] Reset", this);
    }

    // Méthode pour forcer l'explosion (utile pour le debug)
    public void ForceExplode()
    {
        if (!isExploding)
        {
            hitCount = touchesToExplode;
            FreezePhysicsOnExplode();
            StartExplosionSequence();
        }
    }

    // Nettoyage
    void OnDestroy()
    {
        if (_flashMat != null)
        {
            DestroyImmediate(_flashMat);
        }
    }
}