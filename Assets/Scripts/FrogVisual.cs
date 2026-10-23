using System.IO;
using UnityEngine;

/// <summary>
/// Generates and manages the 2D visual representation of the frog.
/// Supports both the uploaded pink glitter unicorn frog sprite and procedural vector-art generation
/// (with eyelids, specular eye reflections, blush cheeks, bellies, and feet).
/// Handles visual state changes: warning blink, jitter, death flash, and squash/stretch visuals.
/// </summary>
public class FrogVisual : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private bool useUploadedSprite = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color deathFlashColor = new Color(1f, 0.15f, 0.15f, 1f);

    private SpriteRenderer _mainRenderer;
    private Transform _visualRoot;
    private static Sprite _cachedUploadedSprite;
    private static Texture2D _circleTex;
    private static Material _unlitSpriteMat;

    public SpriteRenderer MainRenderer => _mainRenderer;
    public Transform VisualRoot => _visualRoot;

    public static Material GetDefaultSpriteMaterial()
    {
        if (_unlitSpriteMat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s != null)
            {
                _unlitSpriteMat = new Material(s);
            }
        }
        return _unlitSpriteMat;
    }

    private void Awake()
    {
        BuildVisualHierarchy();
    }

    public void BuildVisualHierarchy()
    {
        if (_visualRoot != null) return;

        // Visual container to allow separate squash/stretch and jitter independent of physics body
        GameObject rootObj = new GameObject("VisualRoot");
        rootObj.transform.SetParent(transform, false);
        _visualRoot = rootObj.transform;

        // Attempt to load the uploaded frog sprite first
        Sprite uploadedSprite = GetUploadedFrogSprite();

        if (useUploadedSprite && uploadedSprite != null)
        {
            GameObject spriteObj = new GameObject("FrogSprite");
            spriteObj.transform.SetParent(_visualRoot, false);
            _mainRenderer = spriteObj.AddComponent<SpriteRenderer>();
            _mainRenderer.sprite = uploadedSprite;
            _mainRenderer.sharedMaterial = GetDefaultSpriteMaterial();
            _mainRenderer.sortingOrder = 10;

            // Normalize size to approx 1.6 units in world space
            float maxDim = Mathf.Max(uploadedSprite.bounds.size.x, uploadedSprite.bounds.size.y);
            if (maxDim > 0.001f)
            {
                float targetScale = 1.6f / maxDim;
                spriteObj.transform.localScale = Vector3.one * targetScale;
            }
        }
        else
        {
            BuildProceduralVectorFrog();
        }
    }

    /// <summary>
    /// Loads the uploaded frog sprite from Resources or disk.
    /// </summary>
    public static Sprite GetUploadedFrogSprite()
    {
        if (_cachedUploadedSprite != null) return _cachedUploadedSprite;

        // Try Resources.Load
        Sprite resSprite = Resources.Load<Sprite>("Frog");
        if (resSprite != null)
        {
            _cachedUploadedSprite = resSprite;
            return _cachedUploadedSprite;
        }

        // Fallback: Read PNG bytes directly from Assets folder in Editor/Runtime
        string[] searchPaths = {
            Path.Combine(Application.dataPath, "Sprites", "Frog.png"),
            Path.Combine(Application.dataPath, "Resources", "Frog.png")
        };

        foreach (string path in searchPaths)
        {
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    _cachedUploadedSprite = Sprite.Create(
                        tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.45f),
                        100f
                    );
                    _cachedUploadedSprite.name = "UploadedFrogSprite";
                    return _cachedUploadedSprite;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Fallback: Procedurally generates a cute vector-art frog with eyelids, specular eyes, cheeks, belly, and feet.
    /// </summary>
    private void BuildProceduralVectorFrog()
    {
        Sprite circle = GetCircleSprite();

        // 1. Feet (Left and Right) - Rich Violet Purple
        CreatePart("LeftFoot", circle, new Vector3(-0.55f, -0.45f, 0), new Vector3(0.5f, 0.35f, 1), new Color(0.58f, 0.22f, 0.82f), 8);
        CreatePart("RightFoot", circle, new Vector3(0.55f, -0.45f, 0), new Vector3(0.5f, 0.35f, 1), new Color(0.58f, 0.22f, 0.82f), 8);

        // 2. Main Body - Vibrant Royal Purple
        GameObject bodyObj = CreatePart("Body", circle, Vector3.zero, new Vector3(1.4f, 1.15f, 1), new Color(0.72f, 0.36f, 0.96f), 10);
        _mainRenderer = bodyObj.GetComponent<SpriteRenderer>();

        // 3. Glitter / Light Belly - Soft Lavender
        CreatePart("Belly", circle, new Vector3(0f, -0.15f, 0), new Vector3(1.0f, 0.75f, 1), new Color(0.90f, 0.82f, 0.98f), 11);

        // 4. Blush Cheeks - Rosy Orchid
        CreatePart("LeftCheek", circle, new Vector3(-0.45f, 0.05f, 0), new Vector3(0.28f, 0.22f, 1), new Color(0.85f, 0.45f, 0.88f, 0.75f), 12);
        CreatePart("RightCheek", circle, new Vector3(0.45f, 0.05f, 0), new Vector3(0.28f, 0.22f, 1), new Color(0.85f, 0.45f, 0.88f, 0.75f), 12);

        // 5. Eye Sockets / Eyelids - Purple Outer
        CreatePart("LeftEyeSocket", circle, new Vector3(-0.45f, 0.45f, 0), new Vector3(0.55f, 0.55f, 1), new Color(0.72f, 0.36f, 0.96f), 12);
        CreatePart("RightEyeSocket", circle, new Vector3(0.45f, 0.45f, 0), new Vector3(0.55f, 0.55f, 1), new Color(0.72f, 0.36f, 0.96f), 12);

        // 6. Eye Pupils (Deep glossy navy/black)
        CreatePart("LeftEyePupil", circle, new Vector3(-0.45f, 0.45f, 0), new Vector3(0.42f, 0.42f, 1), new Color(0.1f, 0.12f, 0.25f), 13);
        CreatePart("RightEyePupil", circle, new Vector3(0.45f, 0.45f, 0), new Vector3(0.42f, 0.42f, 1), new Color(0.1f, 0.12f, 0.25f), 13);

        // 7. Specular Reflections (Cute anime eye shine)
        CreatePart("LeftEyeSpecular", circle, new Vector3(-0.50f, 0.52f, 0), new Vector3(0.16f, 0.16f, 1), Color.white, 14);
        CreatePart("RightEyeSpecular", circle, new Vector3(0.40f, 0.52f, 0), new Vector3(0.16f, 0.16f, 1), Color.white, 14);
        CreatePart("LeftEyeSubSpecular", circle, new Vector3(-0.40f, 0.38f, 0), new Vector3(0.08f, 0.08f, 1), Color.white, 14);
        CreatePart("RightEyeSubSpecular", circle, new Vector3(0.50f, 0.38f, 0), new Vector3(0.08f, 0.08f, 1), Color.white, 14);

        // 8. Unicorn Horn (Luminous lavender/gold iridescent horn)
        CreatePart("UnicornHorn", circle, new Vector3(0f, 0.75f, 0), new Vector3(0.24f, 0.6f, 1), new Color(0.96f, 0.92f, 1.0f), 15);
    }

    private GameObject CreatePart(string partName, Sprite sprite, Vector3 localPos, Vector3 localScale, Color color, int sortingOrder)
    {
        GameObject part = new GameObject(partName);
        part.transform.SetParent(_visualRoot, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        SpriteRenderer sr = part.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = GetDefaultSpriteMaterial();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return part;
    }

    /// <summary>
    /// Generates a smooth antialiased circle sprite.
    /// </summary>
    public static Sprite GetCircleSprite()
    {
        if (_circleTex != null)
        {
            return Sprite.Create(_circleTex, new Rect(0, 0, _circleTex.width, _circleTex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        int size = 128;
        _circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _circleTex.filterMode = FilterMode.Bilinear;
        _circleTex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radius = center - 2f;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Smooth antialiased border
                float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        _circleTex.SetPixels(pixels);
        _circleTex.Apply();

        return Sprite.Create(_circleTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// Sets warning blink tint and jitter position when frog is close to expiring.
    /// </summary>
    public void SetWarningState(float warningIntensity, float jitterAmount)
    {
        Color targetColor = Color.Lerp(normalColor, warningColor, warningIntensity);
        SetTint(targetColor);

        if (jitterAmount > 0.001f && _visualRoot != null)
        {
            Vector3 jitter = new Vector3(
                (UnityEngine.Random.value * 2f - 1f) * jitterAmount,
                (UnityEngine.Random.value * 2f - 1f) * jitterAmount,
                0f
            );
            _visualRoot.localPosition = jitter;
        }
        else if (_visualRoot != null)
        {
            _visualRoot.localPosition = Vector3.zero;
        }
    }

    public void FlashRed()
    {
        SetTint(deathFlashColor);
    }

    public void SetTint(Color color)
    {
        if (_mainRenderer != null)
        {
            _mainRenderer.color = color;
        }
    }

    public void SetAlpha(float alpha)
    {
        if (_mainRenderer != null)
        {
            Color c = _mainRenderer.color;
            c.a = alpha;
            _mainRenderer.color = c;
        }
    }

    public void ResetVisuals()
    {
        SetTint(normalColor);
        if (_visualRoot != null)
        {
            _visualRoot.localPosition = Vector3.zero;
            _visualRoot.localRotation = Quaternion.identity;
        }
    }
}
