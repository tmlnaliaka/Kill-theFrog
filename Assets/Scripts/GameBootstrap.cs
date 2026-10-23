using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Initializes the entire Kill The Frog game environment at runtime:
/// - 2D Orthographic Camera & Physics Boundaries around screen perimeter.
/// - Environment layers: sky, distant rolling hills, pond with sandy rim, floating lily pads, cattails, drifting clouds.
/// - Frog template prefab configured with Frog, FrogVisual, Rigidbody2D, and CircleCollider2D.
/// - Full UI Canvas with Top HUD header, Start Game Screen, and Game Over Screen.
/// - EventSystem configured for cross-platform New/Legacy input handling.
/// - Audio Sources and GameManager synchronization.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private const float CameraOrthoSize = 5.0f;
    private static Font _cachedFont;

    public static Font DefaultFont
    {
        get
        {
            if (_cachedFont == null)
            {
                _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_cachedFont == null)
                {
                    _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
            return _cachedFont;
        }
    }

    private void Awake()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        Application.runInBackground = true;
        SetupCamera();
        SetupPhysicsBoundaries();
        SetupEnvironment();
        SetupEventSystem();

        GameObject frogPrefab = CreateFrogPrefabTemplate();

        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
        }

        gm.SetFrogPrefab(frogPrefab);

        SetupUI(gm);
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }

        cam.orthographic = true;
        cam.orthographicSize = CameraOrthoSize;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.42f, 0.72f, 0.92f); // Sky blue
    }

    private void SetupPhysicsBoundaries()
    {
        GameObject boundariesObj = new GameObject("ScreenBoundaries");
        boundariesObj.transform.position = Vector3.zero;

        // Camera half dimensions
        float vertExtent = CameraOrthoSize;
        float horzExtent = vertExtent * (16f / 9f); // ~8.88 units

        float wallThickness = 1.0f;

        // Bottom ground barrier
        CreateWall(boundariesObj.transform, "BottomWall", new Vector2(0f, -vertExtent - (wallThickness * 0.5f) + 0.3f), new Vector2(horzExtent * 2.5f, wallThickness));
        // Top barrier
        CreateWall(boundariesObj.transform, "TopWall", new Vector2(0f, vertExtent + (wallThickness * 0.5f) + 0.5f), new Vector2(horzExtent * 2.5f, wallThickness));
        // Left barrier
        CreateWall(boundariesObj.transform, "LeftWall", new Vector2(-horzExtent - (wallThickness * 0.5f), 0f), new Vector2(wallThickness, vertExtent * 2.5f));
        // Right barrier
        CreateWall(boundariesObj.transform, "RightWall", new Vector2(horzExtent + (wallThickness * 0.5f), 0f), new Vector2(wallThickness, vertExtent * 2.5f));
    }

    private void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;

        PhysicsMaterial2D mat = new PhysicsMaterial2D("WallMat")
        {
            bounciness = 0.45f,
            friction = 0.3f
        };
        col.sharedMaterial = mat;
    }

    private void SetupEnvironment()
    {
        GameObject env = new GameObject("Environment");
        Sprite circle = FrogVisual.GetCircleSprite();

        // 1. Distant rolling hills (Layer 1)
        GameObject hills = new GameObject("DistantHills");
        hills.transform.SetParent(env.transform);
        CreateEnvironmentOval(hills.transform, "Hill1", circle, new Vector3(-4.8f, 1.2f, 0f), new Vector3(8.5f, 5.0f, 1f), new Color(0.32f, 0.65f, 0.35f), -20);
        CreateEnvironmentOval(hills.transform, "Hill2", circle, new Vector3(4.5f, 1.0f, 0f), new Vector3(9.2f, 5.2f, 1f), new Color(0.28f, 0.58f, 0.30f), -20);
        CreateEnvironmentOval(hills.transform, "Hill3", circle, new Vector3(0f, 0.7f, 0f), new Vector3(7.0f, 4.0f, 1f), new Color(0.38f, 0.72f, 0.40f), -19);

        // 2. Sandy Shoreline Rim (Layer 2)
        GameObject pondRim = new GameObject("PondShoreRim");
        pondRim.transform.SetParent(env.transform);
        CreateEnvironmentOval(pondRim.transform, "RimOuter", circle, new Vector3(0f, -1.2f, 0f), new Vector3(14.8f, 8.2f, 1f), new Color(0.86f, 0.80f, 0.60f), -15);

        // 3. Pond Water Surface (Layer 3)
        GameObject pondWater = new GameObject("PondWater");
        pondWater.transform.SetParent(env.transform);
        CreateEnvironmentOval(pondWater.transform, "WaterInner", circle, new Vector3(0f, -1.2f, 0f), new Vector3(14.0f, 7.5f, 1f), new Color(0.22f, 0.60f, 0.86f, 0.95f), -10);

        // 4. Floating Lily Pads (Layer 4)
        GameObject lilyPads = new GameObject("LilyPads");
        lilyPads.transform.SetParent(env.transform);
        Vector3[] padPositions = {
            new Vector3(-4.5f, -1.0f, 0f),
            new Vector3(-2.2f, -2.6f, 0f),
            new Vector3(0.0f, -0.6f, 0f),
            new Vector3(2.5f, -2.4f, 0f),
            new Vector3(4.2f, -0.8f, 0f),
            new Vector3(-1.0f, -3.2f, 0f),
            new Vector3(3.2f, 0.2f, 0f),
            new Vector3(-3.5f, 0.4f, 0f)
        };

        for (int i = 0; i < padPositions.Length; i++)
        {
            CreateLilyPad(lilyPads.transform, $"LilyPad_{i}", padPositions[i], circle);
        }

        // 5. Drifting Clouds (Layer 5)
        GameObject clouds = new GameObject("Clouds");
        clouds.transform.SetParent(env.transform);
        CreateCloud(clouds.transform, circle, new Vector3(-7f, 3.8f, 0f), 0.8f);
        CreateCloud(clouds.transform, circle, new Vector3(-1f, 4.3f, 0f), 1.1f);
        CreateCloud(clouds.transform, circle, new Vector3(6f, 3.5f, 0f), 0.9f);
    }

    private void CreateEnvironmentOval(Transform parent, string name, Sprite sprite, Vector3 pos, Vector3 scale, Color color, int sortOrder)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = FrogVisual.GetDefaultSpriteMaterial();
        sr.color = color;
        sr.sortingOrder = sortOrder;
    }

    private void CreateLilyPad(Transform parent, string name, Vector3 pos, Sprite circle)
    {
        GameObject pad = new GameObject(name);
        pad.transform.SetParent(parent);
        pad.transform.position = pos;
        pad.transform.localScale = new Vector3(1.7f, 1.25f, 1f);

        SpriteRenderer sr = pad.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.sharedMaterial = FrogVisual.GetDefaultSpriteMaterial();
        sr.color = new Color(0.18f, 0.65f, 0.28f);
        sr.sortingOrder = -5;

        // Cutout notch simulation on lily pad
        GameObject notch = new GameObject("Notch");
        notch.transform.SetParent(pad.transform);
        notch.transform.localPosition = new Vector3(0.35f, 0.2f, 0f);
        notch.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        SpriteRenderer nSr = notch.AddComponent<SpriteRenderer>();
        nSr.sprite = circle;
        nSr.sharedMaterial = FrogVisual.GetDefaultSpriteMaterial();
        nSr.color = new Color(0.22f, 0.60f, 0.86f, 0.95f);
        nSr.sortingOrder = -4;
    }

    private void CreateCloud(Transform parent, Sprite circle, Vector3 pos, float scale)
    {
        GameObject cloud = new GameObject("Cloud");
        cloud.transform.SetParent(parent);
        cloud.transform.position = pos;

        Color cloudColor = new Color(1f, 1f, 1f, 0.88f);
        CreateEnvironmentOval(cloud.transform, "C1", circle, Vector3.zero, new Vector3(1.6f, 0.9f, 1f) * scale, cloudColor, -18);
        CreateEnvironmentOval(cloud.transform, "C2", circle, new Vector3(-0.6f * scale, -0.1f * scale, 0f), new Vector3(1.2f, 0.75f, 1f) * scale, cloudColor, -18);
        CreateEnvironmentOval(cloud.transform, "C3", circle, new Vector3(0.6f * scale, -0.1f * scale, 0f), new Vector3(1.3f, 0.8f, 1f) * scale, cloudColor, -18);

        cloud.AddComponent<CloudDrifter>().speed = UnityEngine.Random.Range(0.25f, 0.55f);
    }

    private GameObject CreateFrogPrefabTemplate()
    {
        GameObject template = new GameObject("FrogTemplatePrefab");
        template.transform.position = new Vector3(999f, 999f, 0f); // Off-screen

        template.AddComponent<Rigidbody2D>();
        template.AddComponent<CircleCollider2D>();
        template.AddComponent<Frog>();
        template.AddComponent<FrogVisual>();

        template.SetActive(false);
        return template;
    }

    private void SetupEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        esObj.AddComponent<InputSystemUIInputModule>();
#else
        esObj.AddComponent<StandaloneInputModule>();
#endif
    }

    private void SetupUI(GameManager gm)
    {
        // 1. Root Canvas
        GameObject canvasObj = new GameObject("HUDCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 2f;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Top Header HUD Bar
        GameObject topBar = CreateUIPanel(canvasObj.transform, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(1860, 90), new Color(0.08f, 0.12f, 0.18f, 0.82f));

        // Horizontal Layout in TopBar
        HorizontalLayoutGroup hlg = topBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 80;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        gm.scoreText = CreateUIText(topBar.transform, "ScoreText", "SCORE: 0", 38, Color.yellow);
        gm.timerText = CreateUIText(topBar.transform, "TimerText", "TIME: 60s", 38, Color.white);
        gm.livesText = CreateUIText(topBar.transform, "LivesText", "LIVES: ♥ ♥ ♥", 38, new Color(1f, 0.35f, 0.45f));
        gm.highScoreText = CreateUIText(topBar.transform, "HighScoreText", "BEST: 0", 34, new Color(0.6f, 0.9f, 1f));
        gm.comboText = CreateUIText(topBar.transform, "ComboText", "", 40, new Color(1f, 0.55f, 0.1f));

        // 3. Start Game Screen
        GameObject startPanel = CreateUIPanel(canvasObj.transform, "StartPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 620), new Color(0.06f, 0.10f, 0.16f, 0.92f));
        gm.startPanel = startPanel;

        VerticalLayoutGroup vlgStart = startPanel.AddComponent<VerticalLayoutGroup>();
        vlgStart.childAlignment = TextAnchor.MiddleCenter;
        vlgStart.spacing = 25;
        vlgStart.padding = new RectOffset(40, 40, 40, 40);

        CreateUIText(startPanel.transform, "Title", "KILL THE FROG", 58, new Color(0.35f, 1.0f, 0.45f), FontStyle.Bold);
        CreateUIText(startPanel.transform, "SubTitle", "2D Arcade Physics Simulation", 28, new Color(0.8f, 0.9f, 1f));

        string instructions = "HOW TO PLAY:\n\n" +
                              "• Tap or click frogs before they leap away or expire!\n" +
                              "• Hit frogs to earn +10 points & build COMBO multipliers!\n" +
                              "• Clicking empty water or land penalizes score (-5)!\n" +
                              "• Escaped frogs cost 1 Life (♥♥♥). Don't let them run!\n" +
                              "• Survive 60 seconds and set the new High Score!";
        CreateUIText(startPanel.transform, "Instructions", instructions, 24, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);

        CreateUIButton(startPanel.transform, "StartButton", "START GAME", new Color(0.18f, 0.72f, 0.32f), () => gm.StartGame());

        // 4. Game Over Screen
        GameObject gameOverPanel = CreateUIPanel(canvasObj.transform, "GameOverPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 520), new Color(0.12f, 0.06f, 0.08f, 0.94f));
        gm.gameOverPanel = gameOverPanel;

        VerticalLayoutGroup vlgOver = gameOverPanel.AddComponent<VerticalLayoutGroup>();
        vlgOver.childAlignment = TextAnchor.MiddleCenter;
        vlgOver.spacing = 25;
        vlgOver.padding = new RectOffset(40, 40, 40, 40);

        CreateUIText(gameOverPanel.transform, "GameOverTitle", "GAME OVER", 62, new Color(1f, 0.25f, 0.35f), FontStyle.Bold);
        gm.newHighScoreBadge = CreateUIText(gameOverPanel.transform, "NewRecordBadge", "★ NEW HIGH SCORE! ★", 36, Color.yellow, FontStyle.Bold);
        gm.gameOverScoreText = CreateUIText(gameOverPanel.transform, "FinalScore", "FINAL SCORE: 0", 44, Color.white, FontStyle.Bold);
        gm.gameOverHighScoreText = CreateUIText(gameOverPanel.transform, "BestScore", "BEST: 0", 32, new Color(0.7f, 0.85f, 1f));

        CreateUIButton(gameOverPanel.transform, "RestartButton", "PLAY AGAIN", new Color(0.2f, 0.65f, 0.95f), () => gm.StartGame());

        gameOverPanel.SetActive(false);
    }

    private GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        // Rounded outline aesthetic
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.25f);
        outline.effectDistance = new Vector2(2, -2);

        return panel;
    }

    private Text CreateUIText(Transform parent, string name, string content, int fontSize, Color color, FontStyle style = FontStyle.Bold, TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        Text text = textObj.AddComponent<Text>();
        text.font = DefaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;

        ContentSizeFitter csf = textObj.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(2, -2);

        return text;
    }

    private Button CreateUIButton(Transform parent, string name, string label, Color btnColor, Action onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320, 75);

        Image img = btnObj.AddComponent<Image>();
        img.color = btnColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = btnColor;
        cb.highlightedColor = Color.Lerp(btnColor, Color.white, 0.3f);
        cb.pressedColor = Color.Lerp(btnColor, Color.black, 0.25f);
        btn.colors = cb;

        btn.onClick.AddListener(() => onClick?.Invoke());

        GameObject textObj = new GameObject("BtnText");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.font = DefaultFont;
        text.text = label;
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;

        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(2, -2);

        return btn;
    }
}

/// <summary>
/// Drifts sky clouds horizontally and wraps around screen bounds.
/// </summary>
public class CloudDrifter : MonoBehaviour
{
    public float speed = 0.4f;
    private const float LeftBound = -12f;
    private const float RightBound = 12f;

    private void Update()
    {
        transform.position += Vector3.right * (speed * Time.deltaTime);
        if (transform.position.x > RightBound)
        {
            Vector3 pos = transform.position;
            pos.x = LeftBound;
            transform.position = pos;
        }
    }
}
