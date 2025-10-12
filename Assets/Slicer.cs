using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Slicer : MonoBehaviour
{
    [Header("Slicing Settings")]
    public Material capMaterial;
    public bool enableDebug = false;
    public bool debugLogCapColor = false;
    public float gizmoNormalScale = 0.25f;

    [Header("Audio")]
    public bool enableSliceSounds = true;

    [Tooltip("Tolérance géométrique pour la détection d'intersections avec le plan")]
    public float epsilon = 1e-5f;

    // -------------------- structs internes --------------------
    private struct VertexData
    {
        public Vector3 pos;
        public Vector3 normal;
        public Vector2 uv;
        public VertexData(Vector3 p, Vector3 n, Vector2 u) { pos = p; normal = n; uv = u; }
    }

    private struct Triangle
    {
        public VertexData a, b, c;
        public Triangle(VertexData v1, VertexData v2, VertexData v3) { a = v1; b = v2; c = v3; }
        public Vector3 FaceNormal() => Vector3.Cross(b.pos - a.pos, c.pos - a.pos).normalized;
    }

    private class Segment
    {
        public Vector3 p0, p1;
        public Segment(Vector3 a, Vector3 b) { p0 = a; p1 = b; }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SliceAtMousePosition();
        }
    }

    public void SliceFruitVR(GameObject fruit, Vector3 slicePoint, Vector3 sliceNormal)
    {
        if (fruit == null) return;

        // NOUVEAU : Son de découpe (optionnel - déjà joué par KatanaSlicer)
        if (enableSliceSounds && AudioManager.Instance != null)
        {
            // Petit son supplémentaire pour le slice réussi
            AudioManager.Instance.PlayFruitSlice();
        }

        Color capColor = TrySampleCapColor(fruit);
        Slice(fruit, slicePoint, sliceNormal, capColor);
    }

    // API publique pour le slicing
    public void Slice(GameObject objectToSlice, Vector3 slicePoint, Vector3 sliceNormal, Color? capColorOverride = null)
    {
        GameObject a, b;
        SliceObject(objectToSlice, sliceNormal.normalized, slicePoint, out a, out b, capColorOverride);
    }

    private void SliceAtMousePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        if (hit.collider.GetComponent<MeshFilter>() == null && hit.collider.transform.childCount == 0)
            return;

        Vector3 planeNormal = Vector3.Cross(ray.direction, Vector3.up).normalized;
        if (planeNormal.sqrMagnitude < 1e-6f) planeNormal = Vector3.right;

        Slice(hit.collider.gameObject, hit.point, planeNormal, TrySampleCapColor(hit.collider.gameObject));
    }

    // ... [Le reste de votre code Slicer reste EXACTEMENT le même] ...
    // Toutes les méthodes de slicing, triangulation, etc. restent identiques
    
    private Color TrySampleCapColor(GameObject go)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return Color.gray;
        return CapColorFromRenderer(rend);
    }

    private Color CapColorFromRenderer(Renderer rend)
    {
        Color sampledColor = GetColorFromMaterial(rend.sharedMaterial);
        if (IsWhiteColor(sampledColor))
        {
            Color nameColor = GetFruitColorByName(rend.gameObject.name);
            if (enableDebug) 
                Debug.Log($"[Slicer] Using name-based color for {rend.gameObject.name}: #{ColorUtility.ToHtmlStringRGB(nameColor)}");
            return nameColor;
        }
        return sampledColor;
    }

    private Color GetColorFromMaterial(Material mat)
    {
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return Color.yellow;
    }

    private Color GetFruitColorByName(string fruitName)
    {
        if (string.IsNullOrEmpty(fruitName)) return new Color(0.9f, 0.6f, 0.3f);

        string name = fruitName.ToLower();
        if (name.Contains("banana")) return new Color(1.0f, 0.95f, 0.8f);
        else if (name.Contains("coconut")) return new Color(0.98f, 0.92f, 0.84f);
        else if (name.Contains("greenapple")) return new Color(0.8f, 0.95f, 0.7f);
        else if (name.Contains("orange")) return new Color(1.0f, 0.65f, 0.3f);
        else if (name.Contains("pear")) return new Color(0.98f, 0.95f, 0.8f);
        else if (name.Contains("redapple")) return new Color(0.95f, 0.8f, 0.8f);
        else if (name.Contains("tomato")) return new Color(0.95f, 0.7f, 0.6f);
        else if (name.Contains("watermelon")) return new Color(0.95f, 0.6f, 0.6f);
        else if (name.Contains("apple")) return new Color(0.9f, 0.8f, 0.8f);
        else return new Color(0.9f, 0.7f, 0.5f);
    }

    private bool IsWhiteColor(Color color)
    {
        return color.r > 0.9f && color.g > 0.9f && color.b > 0.9f;
    }

    private bool HasTexture(Material mat)
    {
        return (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null) ||
               (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null);
    }

    private Color SampleTextureColor(Material mat)
    {
        try
        {
            Texture2D tex = null;

            if (mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex") as Texture2D;
            if (tex == null && mat.HasProperty("_BaseMap")) tex = mat.GetTexture("_BaseMap") as Texture2D;

            if (tex != null && tex.isReadable)
            {
                // Sample le centre de la texture
                Color centerColor = tex.GetPixel(tex.width / 2, tex.height / 2);
                Debug.Log($"[Slicer] Sampled texture center: #{ColorUtility.ToHtmlStringRGB(centerColor)}");
                return centerColor;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Slicer] Texture sampling failed: {e.Message}");
        }

        return Color.white;
    }


    private bool TryAverageTextureColor(Texture tex, out Color avg)
    {
        avg = Color.gray;
        if (tex == null) return false;

        // Texture2D readable ?
        if (tex is Texture2D t2d)
        {
            try { avg = AverageColor(t2d, 8); return true; } catch { /* non-readable */ }
        }

        // Fallback GPU : blit → RT 32x32 → ReadPixels
        try
        {
            int W = 32, H = 32;
            RenderTexture rt = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32);
            RenderTexture active = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;

            Texture2D tmp = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tmp.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tmp.Apply(false, true);

            var px = tmp.GetPixels();
            float r = 0, g = 0, b = 0, a = 0;
            for (int i = 0; i < px.Length; i++) { var c = px[i]; r += c.r; g += c.g; b += c.b; a += c.a; }
            avg = new Color(r / px.Length, g / px.Length, b / px.Length, a / px.Length);

            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            return true;
        }
        catch { return false; }
    }

    private Color AverageColor(Texture2D tex, int step)
    {
        float r = 0, g = 0, b = 0, a = 0; int n = 0;
        int w = tex.width, h = tex.height;
        for (int y = 0; y < h; y += step)
            for (int x = 0; x < w; x += step)
            { var c = tex.GetPixel(x, y); r += c.r; g += c.g; b += c.b; a += c.a; n++; }
        if (n == 0) return Color.gray;
        return new Color(r / n, g / n, b / n, a / n);
    }

    // -------------------- API publique --------------------
    

    public void SliceObject(GameObject objectToSlice, Vector3 planeNormal, Vector3 planePoint,
                            out GameObject sliceA, out GameObject sliceB, Color? capColorOverride = null)
    {
        sliceA = null; sliceB = null;

        // Parent → regroupe les enfants meshés
        if (objectToSlice.GetComponent<MeshFilter>() == null && objectToSlice.transform.childCount > 0)
        {
            SliceParentObject(objectToSlice, planeNormal, planePoint, out sliceA, out sliceB);
            return;
        }

        SliceSingleObject(objectToSlice, planeNormal, planePoint, out sliceA, out sliceB, capColorOverride);
    }

    private void SliceParentObject(GameObject parent, Vector3 planeNormal, Vector3 planePoint,
                                   out GameObject sliceA, out GameObject sliceB)
    {
        sliceA = new GameObject(parent.name + "_Slice_A");
        sliceB = new GameObject(parent.name + "_Slice_B");

        foreach (Transform child in parent.transform)
        {
            if (child.GetComponent<MeshFilter>() == null) continue;
            Color childColor = TrySampleCapColor(child.gameObject);
            SliceSingleObject(child.gameObject, planeNormal, planePoint,
                              out GameObject a, out GameObject b, childColor);
            if (a != null) a.transform.SetParent(sliceA.transform, true);
            if (b != null) b.transform.SetParent(sliceB.transform, true);
        }

        if (sliceA.transform.childCount == 0) { Destroy(sliceA); sliceA = null; }
        if (sliceB.transform.childCount == 0) { Destroy(sliceB); sliceB = null; }
        Destroy(parent);
    }

    // -------------------- cœur de découpe --------------------
    private void SliceSingleObject(GameObject go, Vector3 planeNormal, Vector3 planePoint,
                                   out GameObject sliceA, out GameObject sliceB, Color? capColorOverride)
    {
        sliceA = null; sliceB = null;

        var mf = go.GetComponent<MeshFilter>();
        var renderer = go.GetComponent<Renderer>();
        if (mf == null || renderer == null || mf.sharedMesh == null) return;

        Mesh source = mf.sharedMesh;
        if (!source.isReadable) { Debug.LogError($"Mesh '{source.name}' is not readable! Activez Read/Write."); return; }

        Plane plane = new Plane(planeNormal, planePoint);

        var verts = source.vertices;
        var norms = (source.normals != null && source.normals.Length == verts.Length) ? source.normals : new Vector3[verts.Length];
        var uvs = (source.uv != null && source.uv.Length == verts.Length) ? source.uv : Enumerable.Repeat(Vector2.zero, verts.Length).ToArray();

        // Concatène tous les submeshes
        List<int> allTris = new List<int>();
        for (int sm = 0; sm < source.subMeshCount; sm++) allTris.AddRange(source.GetTriangles(sm));

        Material surfaceMat = renderer.sharedMaterial;
        Material capMatInstance = MakeCapMaterial(capColorOverride ?? CapColorFromRenderer(renderer), renderer.sharedMaterial);

        List<Triangle> A_surface = new List<Triangle>();
        List<Triangle> B_surface = new List<Triangle>();
        List<Segment> cutSegments = new List<Segment>();

        for (int i = 0; i < allTris.Count; i += 3)
        {
            int i0 = allTris[i]; int i1 = allTris[i + 1]; int i2 = allTris[i + 2];

            VertexData v0 = new VertexData(go.transform.TransformPoint(verts[i0]), go.transform.TransformDirection(norms[i0]).normalized, uvs[i0]);
            VertexData v1 = new VertexData(go.transform.TransformPoint(verts[i1]), go.transform.TransformDirection(norms[i1]).normalized, uvs[i1]);
            VertexData v2 = new VertexData(go.transform.TransformPoint(verts[i2]), go.transform.TransformDirection(norms[i2]).normalized, uvs[i2]);

            Vector3 originalN = Vector3.Cross(v1.pos - v0.pos, v2.pos - v0.pos).normalized;

            ClassifyAndSliceTriangle(plane, v0, v1, v2, originalN, A_surface, B_surface, cutSegments);
        }

        // Caps (boucles soudées → éventail)
        BuildCapsFromSegments(cutSegments, planeNormal, planePoint, out List<Triangle> A_caps, out List<Triangle> B_caps);

        // Meshes
        sliceA = BuildMesh(go, A_surface, A_caps, surfaceMat, capMatInstance, go.name + "_Slice_A");
        sliceB = BuildMesh(go, B_surface, B_caps, surfaceMat, capMatInstance, go.name + "_Slice_B");

        // Physique
        SetupSlicedPhysics(sliceA, go, planeNormal);
        SetupSlicedPhysics(sliceB, go, -planeNormal);

        Destroy(go);
    }

    private void AddTriangleAligned(List<Triangle> dst, Triangle t, Vector3 referenceNormal)
    {
        Vector3 n = t.FaceNormal();
        if (Vector3.Dot(n, referenceNormal) < 0f) dst.Add(new Triangle(t.a, t.c, t.b));
        else dst.Add(t);
    }

    private void ClassifyAndSliceTriangle(Plane plane, VertexData v0, VertexData v1, VertexData v2, Vector3 originalN,
                                          List<Triangle> sideA, List<Triangle> sideB, List<Segment> cutSegments)
    {
        float d0 = plane.GetDistanceToPoint(v0.pos);
        float d1 = plane.GetDistanceToPoint(v1.pos);
        float d2 = plane.GetDistanceToPoint(v2.pos);

        int s0 = SignWithEps(d0), s1 = SignWithEps(d1), s2 = SignWithEps(d2);

        if (s0 >= 0 && s1 >= 0 && s2 >= 0) { AddTriangleAligned(sideA, new Triangle(v0, v1, v2), originalN); return; }
        if (s0 <= 0 && s1 <= 0 && s2 <= 0) { AddTriangleAligned(sideB, new Triangle(v0, v1, v2), originalN); return; }

        List<VertexData> pos = new List<VertexData>(); List<float> posD = new List<float>();
        List<VertexData> neg = new List<VertexData>(); List<float> negD = new List<float>();

        void push(VertexData v, float d) { if (d >= 0) { pos.Add(v); posD.Add(d); } else { neg.Add(v); negD.Add(d); } }
        push(v0, d0); push(v1, d1); push(v2, d2);

        if (pos.Count == 2 && neg.Count == 1)
        {
            VertexData va = pos[0]; float da = posD[0];
            VertexData vb = pos[1]; float db = posD[1];
            VertexData vc = neg[0]; float dc = negD[0];

            VertexData i1 = LerpEdge(vc, va, dc, da);
            VertexData i2 = LerpEdge(vc, vb, dc, db);

            AddTriangleAligned(sideA, new Triangle(va, vb, i2), originalN);
            AddTriangleAligned(sideA, new Triangle(va, i2, i1), originalN);
            AddTriangleAligned(sideB, new Triangle(vc, i1, i2), originalN);

            cutSegments.Add(new Segment(i1.pos, i2.pos));
        }
        else if (pos.Count == 1 && neg.Count == 2)
        {
            VertexData va = neg[0]; float da = negD[0];
            VertexData vb = neg[1]; float db = negD[1];
            VertexData vc = pos[0]; float dc = posD[0];

            VertexData i1 = LerpEdge(vc, va, dc, da);
            VertexData i2 = LerpEdge(vc, vb, dc, db);

            AddTriangleAligned(sideB, new Triangle(va, i1, vb), originalN);
            AddTriangleAligned(sideB, new Triangle(vb, i1, i2), originalN);
            AddTriangleAligned(sideA, new Triangle(vc, i2, i1), originalN);

            cutSegments.Add(new Segment(i1.pos, i2.pos));
        }
        else
        {
            if (s0 >= 0 || s1 >= 0 || s2 >= 0) AddTriangleAligned(sideA, new Triangle(v0, v1, v2), originalN);
            else AddTriangleAligned(sideB, new Triangle(v0, v1, v2), originalN);
        }
    }

    private int SignWithEps(float d) => d > epsilon ? 1 : (d < -epsilon ? -1 : 0);

    private VertexData LerpEdge(VertexData vStart, VertexData vEnd, float dStart, float dEnd)
    {
        float t = dStart / (dStart - dEnd);
        Vector3 p = Vector3.Lerp(vStart.pos, vEnd.pos, t);
        Vector3 n = Vector3.Slerp(vStart.normal, vEnd.normal, t).normalized;
        Vector2 uv = Vector2.Lerp(vStart.uv, vEnd.uv, t);
        return new VertexData(p, n, uv);
    }

    // -------------------- reconstruction des caps --------------------
    private void BuildCapsFromSegments(List<Segment> segments, Vector3 planeNormal, Vector3 planePoint,
                                   out List<Triangle> capA, out List<Triangle> capB)
    {
        capA = new List<Triangle>();
        capB = new List<Triangle>();
        if (segments == null || segments.Count == 0) return;

        // Base (u,v) du plan pour projeter en 2D
        Vector3 u = Vector3.Cross(planeNormal, Vector3.up);
        if (u.sqrMagnitude < 1e-6f) u = Vector3.Cross(planeNormal, Vector3.right);
        u.Normalize();
        Vector3 v = Vector3.Cross(planeNormal, u);

        // Taille de tolérance adaptée à l’échelle (0.05% du rayon)
        float scaleRef = 0.0f;
        foreach (var s in segments) { scaleRef = Mathf.Max(scaleRef, (s.p0 - planePoint).magnitude, (s.p1 - planePoint).magnitude); }
        float tol = Mathf.Max(1e-4f, scaleRef * 5e-4f);     // monde
        float tol2 = tol * tol;

        // Quantization pour souder (on projette en 2D puis on arrondit)
        float q = tol * 2f; // taille de cellule de quantization
        Dictionary<(int, int), int> gridToIndex = new Dictionary<(int, int), int>();
        List<Vector3> welded = new List<Vector3>();         // points soudés (monde)
        List<Vector2> welded2D = new List<Vector2>();       // leurs coords projetées (2D)

        int Weld(Vector3 p)
        {
            Vector3 d = p - planePoint;
            float px = Vector3.Dot(d, u);
            float py = Vector3.Dot(d, v);
            int gx = Mathf.RoundToInt(px / q);
            int gy = Mathf.RoundToInt(py / q);
            var key = (gx, gy);

            if (gridToIndex.TryGetValue(key, out int idx))
            {
                // si proche, réutilise
                if ((welded[idx] - p).sqrMagnitude <= tol2) return idx;
            }

            int ni = welded.Count;
            welded.Add(p);
            welded2D.Add(new Vector2(px, py));
            gridToIndex[key] = ni;
            return ni;
        }

        // Graphe non orienté : pour chaque point, la liste de ses voisins
        List<HashSet<int>> adj = new List<HashSet<int>>();

        void EnsureAdjSize(int n)
        {
            while (adj.Count < n) adj.Add(new HashSet<int>());
        }

        foreach (var s in segments)
        {
            int a = Weld(s.p0);
            int b = Weld(s.p1);
            if (a == b) continue;
            EnsureAdjSize(Mathf.Max(a, b) + 1);
            adj[a].Add(b);
            adj[b].Add(a);
        }

        // Extraire tous les cycles fermés
        bool[] used = new bool[welded.Count];

        for (int start = 0; start < welded.Count; start++)
        {
            if (adj.Count <= start || adj[start].Count == 0) continue;
            if (used[start]) continue;

            // Suivi d’un cycle en choisissant à chaque fois le voisin le "plus angulairement proche"
            List<int> loop = new List<int>();
            int current = start;
            int prev = -1;

            // On tente d’avancer jusqu’à ce qu’on revienne au point de départ
            for (int safety = 0; safety < 4096; safety++)
            {
                loop.Add(current);
                used[current] = true;

                // Choisir prochain voisin
                int next = -1;
                if (adj[current].Count == 0) break;

                if (prev < 0)
                {
                    // 1er pas : prends n'importe quel voisin
                    next = adj[current].First();
                }
                else
                {
                    // Choisir le voisin le plus "dans la continuité" (max cos angle en 2D)
                    Vector2 a = welded2D[current] - welded2D[prev];
                    float bestDot = -999f;
                    foreach (var nb in adj[current])
                    {
                        if (nb == prev) continue;
                        Vector2 b = welded2D[nb] - welded2D[current];
                        float dot = Vector2.Dot(a.normalized, b.normalized);
                        if (dot > bestDot) { bestDot = dot; next = nb; }
                    }
                    if (next < 0) next = prev; // cul-de-sac
                }

                // Si on se ferme (proche du départ), on a un cycle
                if (next >= 0 && (welded[next] - welded[start]).sqrMagnitude <= tol2 && loop.Count >= 3)
                {
                    // Construire le contour final en monde
                    List<Vector3> ring = new List<Vector3>();
                    foreach (var id in loop) ring.Add(welded[id]);
                    TriangulateCapLoop(ring, planeNormal, planePoint, capA, capB);
                    break;
                }

                // Avancer
                if (next < 0 || adj[current].Count == 1) break; // pas de boucle
                int tmp = current; current = next; prev = tmp;
            }
        }
    }


    private void TriangulateCapLoop(List<Vector3> loopWorld, Vector3 planeNormal, Vector3 planePoint,
                                List<Triangle> capA, List<Triangle> capB)
    {
        if (loopWorld == null || loopWorld.Count < 3) return;

        // Base 2D du plan
        Vector3 u = Vector3.Cross(planeNormal, Vector3.up);
        if (u.sqrMagnitude < 1e-6f) u = Vector3.Cross(planeNormal, Vector3.right);
        u.Normalize();
        Vector3 v = Vector3.Cross(planeNormal, u);

        // Centre géométrique
        Vector3 center = Vector3.zero;
        foreach (var p in loopWorld) center += p;
        center /= loopWorld.Count;

        // Ordonner par angle en 2D autour du centre
        var ordered = loopWorld
            .Select(p => new { p, ang = Mathf.Atan2(Vector3.Dot(p - center, v), Vector3.Dot(p - center, u)) })
            .OrderBy(t => t.ang)
            .Select(t => t.p)
            .ToList();

        VertexData c = new VertexData(center, planeNormal, Vector2.zero);
        for (int i = 0; i < ordered.Count; i++)
        {
            var a = new VertexData(ordered[i], planeNormal, Vector2.zero);
            var b = new VertexData(ordered[(i + 1) % ordered.Count], planeNormal, Vector2.zero);
            capA.Add(new Triangle(c, a, b));   // face 1
            capB.Add(new Triangle(c, b, a));   // face 2 (inversée)
        }
    }

    // -------------------- construction mesh --------------------
    private GameObject BuildMesh(GameObject original, List<Triangle> surface, List<Triangle> caps,
                                 Material surfaceMat, Material capMat, string name)
    {
        if ((surface == null || surface.Count == 0) && (caps == null || caps.Count == 0)) return null;

        GameObject go = new GameObject(name);
        go.tag = original.tag;
        go.layer = original.layer;

        go.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
        go.transform.localScale = original.transform.localScale;

        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv0 = new List<Vector2>();
        List<int> idxSurface = new List<int>();
        List<int> idxCaps = new List<int>();

        bool negativeScale = (go.transform.lossyScale.x * go.transform.lossyScale.y * go.transform.lossyScale.z) < 0f;

        void PushTriangle(Triangle t, List<int> idxCollector)
        {
            int baseIndex = vertices.Count;

            vertices.Add(go.transform.InverseTransformPoint(t.a.pos));
            vertices.Add(go.transform.InverseTransformPoint(t.b.pos));
            vertices.Add(go.transform.InverseTransformPoint(t.c.pos));

            normals.Add(go.transform.InverseTransformDirection(t.a.normal).normalized);
            normals.Add(go.transform.InverseTransformDirection(t.b.normal).normalized);
            normals.Add(go.transform.InverseTransformDirection(t.c.normal).normalized);

            uv0.Add(t.a.uv); uv0.Add(t.b.uv); uv0.Add(t.c.uv);

            if (negativeScale)
            { idxCollector.Add(baseIndex); idxCollector.Add(baseIndex + 2); idxCollector.Add(baseIndex + 1); }
            else
            { idxCollector.Add(baseIndex); idxCollector.Add(baseIndex + 1); idxCollector.Add(baseIndex + 2); }
        }

        if (surface != null) foreach (var t in surface) PushTriangle(t, idxSurface);
        if (caps != null) foreach (var t in caps) PushTriangle(t, idxCaps);

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uv0);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(idxSurface, 0, true);
        mesh.SetTriangles(idxCaps, 1, true);
        mesh.RecalculateBounds();

        mf.sharedMesh = mesh;
        mr.sharedMaterials = new Material[] { surfaceMat, capMat };
        return go;
    }

    // -------------------- matériau cap --------------------
    private Material MakeCapMaterial(Color? capColor, Material fruitMat)
    {
        // Shader prioritaire URP Unlit
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);

        // ⭐ FORCER la couleur calculée du fruit - ignorer capMaterial de l'inspecteur
        Color finalColor = capColor ?? Color.gray;

        // Appliquer la couleur
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", finalColor);
        if (m.HasProperty("_Color")) m.SetColor("_Color", finalColor);

        // Supprimer les textures
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);

        // Rendre double-face
        if (m.HasProperty("_Cull")) m.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        if (m.HasProperty("_CullMode")) m.SetInt("_CullMode", (int)UnityEngine.Rendering.CullMode.Off);

        // Debug
        if (debugLogCapColor)
            Debug.Log($"[Slicer] Final cap color: #{ColorUtility.ToHtmlStringRGB(finalColor)}");

        return m;
    }
    // -------------------- physique --------------------
    private void SetupSlicedPhysics(GameObject slice, GameObject original, Vector3 forceDirection)
    {
        if (slice == null) return;

        var oldMc = slice.GetComponent<MeshCollider>();
        if (oldMc != null) DestroyImmediate(oldMc);

        AddOptimalCollider(slice);

        var rb = slice.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.AddForce(forceDirection.normalized * 2f, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere, ForceMode.Impulse);
    }

    private Collider AddOptimalCollider(GameObject slice)
    {
        var mf = slice.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return slice.AddComponent<SphereCollider>();

        Bounds b = mf.sharedMesh.bounds;
        Vector3 size = b.size;

        if (Mathf.Abs(size.x - size.y) < 0.1f && Mathf.Abs(size.x - size.z) < 0.1f)
        { var s = slice.AddComponent<SphereCollider>(); s.center = b.center; s.radius = Mathf.Max(size.x, size.y, size.z) * 0.5f; return s; }
        else if (size.y > size.x * 1.5f && size.y > size.z * 1.5f)
        { var c = slice.AddComponent<CapsuleCollider>(); c.center = b.center; c.height = size.y; c.radius = Mathf.Max(size.x, size.z) * 0.5f; c.direction = 1; return c; }
        else
        { var bx = slice.AddComponent<BoxCollider>(); bx.center = b.center; bx.size = size; return bx; }
    }
    
}
