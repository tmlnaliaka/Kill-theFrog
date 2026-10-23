using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controls the game loop: state management, progressive difficulty spawn timers,
/// scoring (+10 base), combo streaks, miss penalties (-5), lives (♥♥♥), 60s countdown,
/// high score persistence, floating combat text, procedural audio triggers, and cross-platform pointer input.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        StartScreen,
        Playing,
        GameOver
    }

    [Header("Game State & Rules")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int maxLives = 3;
    [SerializeField] private float comboWindow = 2.2f;

    [Header("Difficulty Curve")]
    [SerializeField] private float initialSpawnInterval = 1.8f;
    [SerializeField] private float minimumSpawnInterval = 0.75f;
    [SerializeField] private float initialFrogLifetime = 4.2f;
    [SerializeField] private float minimumFrogLifetime = 2.4f;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-5.5f, -3.2f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(5.5f, 0.8f);

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;

    [Header("UI References (Configured by GameBootstrap)")]
    public Text scoreText;
    public Text timerText;
    public Text livesText;
    public Text highScoreText;
    public Text comboText;
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public Text gameOverScoreText;
    public Text gameOverHighScoreText;
    public Text newHighScoreBadge;

    // Runtime state
    private GameState _currentState = GameState.StartScreen;
    private int _score = 0;
    private int _highScore = 0;
    private int _lives = 3;
    private float _timeRemaining = 60f;
    private int _comboCount = 0;
    private float _lastHitTime = -10f;
    private float _nextSpawnTime;
    private Camera _mainCamera;
    private GameObject _frogPrefab;
    private readonly List<Frog> _activeFrogs = new List<Frog>();

    public bool IsGameActive => _currentState == GameState.Playing;
    public int Score => _score;
    public int Lives => _lives;
    public float TimeRemaining => _timeRemaining;

    private const string HighScoreKey = "KillTheFrog_HighScore";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _mainCamera = Camera.main;
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        _highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        UpdateUI();
        ShowStartScreen();
    }

    public void SetFrogPrefab(GameObject prefab)
    {
        _frogPrefab = prefab;
    }

    public void StartGame()
    {
        _score = 0;
        _lives = maxLives;
        _timeRemaining = gameDuration;
        _comboCount = 0;
        _lastHitTime = -10f;
        _currentState = GameState.Playing;

        // Clean any leftover frogs
        ClearAllFrogs();

        // Sound effect
        SoundGenerator.Play(sfxSource, SoundGenerator.StartClip, 0.9f);

        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        _nextSpawnTime = Time.time + 0.5f;
        UpdateUI();
    }

    private void Update()
    {
        if (_currentState != GameState.Playing) return;

        // Countdown timer (clamped to prevent editor focus lag spikes)
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        _timeRemaining -= dt;
        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            TriggerGameOver("Time's Up!");
            return;
        }

        // Combo decay window
        if (_comboCount > 0 && Time.time - _lastHitTime > comboWindow)
        {
            ResetCombo();
        }

        // Progressive difficulty calculation (0 to 1)
        float difficultyProgress = 1f - (_timeRemaining / gameDuration);
        float currentSpawnInterval = Mathf.Lerp(initialSpawnInterval, minimumSpawnInterval, difficultyProgress);

        // Spawn timer
        if (Time.time >= _nextSpawnTime)
        {
            SpawnFrog(difficultyProgress);
            _nextSpawnTime = Time.time + currentSpawnInterval;
        }

        // Pointer click / tap detection
        HandlePointerInput();

        UpdateUI();
    }

    private void HandlePointerInput()
    {
        Vector2 pointerScreenPos = Vector2.zero;
        bool pointerDown = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerScreenPos = Mouse.current.position.ReadValue();
            pointerDown = true;
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pointerScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerDown = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (!pointerDown)
        {
            if (Input.GetMouseButtonDown(0))
            {
                pointerScreenPos = Input.mousePosition;
                pointerDown = true;
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                pointerScreenPos = Input.GetTouch(0).position;
                pointerDown = true;
            }
        }
#endif

        if (pointerDown)
        {
            // EventSystem check: Ignore clicks that hit UI buttons or HUD elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            ProcessClickAtScreenPosition(pointerScreenPos);
        }
    }

    private void ProcessClickAtScreenPosition(Vector2 screenPos)
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

        // 2D Raycast for frogs
        RaycastHit2D hit = Physics2D.Raycast(worldPoint2D, Vector2.zero);

        if (hit.collider != null)
        {
            Frog frog = hit.collider.GetComponentInParent<Frog>();
            if (frog != null && frog.IsAlive)
            {
                HitFrog(frog);
                return;
            }
        }

        // If no frog was hit: Missed click penalty (-5 score)
        OnMissClick(worldPoint2D);
    }

    public void HitFrog(Frog frog)
    {
        if (_currentState != GameState.Playing || frog == null || !frog.IsAlive) return;

        Vector3 frogPos = frog.transform.position;
        frog.Kill();
        _activeFrogs.Remove(frog);

        // Combo streak calculation
        _comboCount++;
        _lastHitTime = Time.time;

        int bonus = Mathf.Min((_comboCount - 1) * 5, 25);
        int pointsEarned = 10 + bonus;
        _score += pointsEarned;

        // Floating combat text
        string text = $"+{pointsEarned}";
        Color textColor = Color.yellow;

        if (_comboCount >= 2)
        {
            text = $"+{pointsEarned}\nCOMBO x{_comboCount}!";
            textColor = new Color(1f, 0.5f, 0.1f);
        }

        SpawnFloatingText(text, frogPos, textColor);

        // Procedural Audio
        SoundGenerator.Play(sfxSource, SoundGenerator.KillClip, 0.95f);

        // Update high score in real-time
        if (_score > _highScore)
        {
            _highScore = _score;
            PlayerPrefs.SetInt(HighScoreKey, _highScore);
        }

        UpdateUI();
    }

    private void OnMissClick(Vector2 clickPos)
    {
        if (_currentState != GameState.Playing) return;

        // Penalize score (-5), floor at 0
        int penalty = 5;
        _score = Mathf.Max(0, _score - penalty);

        // Reset combo streak
        ResetCombo();

        // Sound effect
        SoundGenerator.Play(sfxSource, SoundGenerator.MissClip, 0.8f);

        // Floating text
        SpawnFloatingText($"-{penalty}", clickPos, new Color(1f, 0.3f, 0.3f));

        UpdateUI();
    }

    public void OnFrogEscaped(Frog frog)
    {
        if (_currentState != GameState.Playing) return;

        _activeFrogs.Remove(frog);

        // Reduce lives
        _lives--;
        ResetCombo();

        if (frog != null)
        {
            SpawnFloatingText("ESCAPED!", frog.transform.position, Color.red);
        }

        UpdateUI();

        if (_lives <= 0)
        {
            TriggerGameOver("Out of Lives!");
        }
    }

    private void ResetCombo()
    {
        _comboCount = 0;
        if (comboText != null)
        {
            comboText.text = "";
        }
    }

    private void SpawnFrog(float difficultyProgress)
    {
        if (_frogPrefab == null) return;

        // Spawn position within pond boundary
        float posX = UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float posY = UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(posX, posY, 0f);

        GameObject frogObj = Instantiate(_frogPrefab, spawnPos, Quaternion.identity);
        frogObj.name = "Frog";
        frogObj.SetActive(true);
        Frog frog = frogObj.GetComponent<Frog>();

        if (frog != null)
        {
            float lifetime = Mathf.Lerp(initialFrogLifetime, minimumFrogLifetime, difficultyProgress);
            float hopSpeedMult = Mathf.Lerp(1.0f, 1.8f, difficultyProgress);
            frog.Setup(lifetime, hopSpeedMult);
            _activeFrogs.Add(frog);

            // Procedural croak/ribbit sound on spawn
            SoundGenerator.Play(sfxSource, SoundGenerator.RibbitClip, 0.75f);
        }
    }

    private void TriggerGameOver(string reason)
    {
        _currentState = GameState.GameOver;
        ClearAllFrogs();

        // Play game over procedural melody
        SoundGenerator.Play(sfxSource, SoundGenerator.GameOverClip, 0.95f);

        // Persist high score
        bool isNewRecord = false;
        if (_score > _highScore)
        {
            _highScore = _score;
            PlayerPrefs.SetInt(HighScoreKey, _highScore);
            PlayerPrefs.Save();
            isNewRecord = true;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverScoreText != null) gameOverScoreText.text = $"FINAL SCORE: {_score}";
            if (gameOverHighScoreText != null) gameOverHighScoreText.text = $"BEST: {_highScore}";
            if (newHighScoreBadge != null) newHighScoreBadge.gameObject.SetActive(isNewRecord);
        }
    }

    private void ShowStartScreen()
    {
        _currentState = GameState.StartScreen;
        if (startPanel != null) startPanel.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    private void ClearAllFrogs()
    {
        foreach (Frog f in _activeFrogs)
        {
            if (f != null)
            {
                Destroy(f.gameObject);
            }
        }
        _activeFrogs.Clear();
    }

    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"SCORE: {_score}";
        if (timerText != null) timerText.text = $"TIME: {Mathf.CeilToInt(_timeRemaining)}s";
        if (highScoreText != null) highScoreText.text = $"BEST: {_highScore}";

        if (livesText != null)
        {
            // Display heart icons: ♥♥♥
            string hearts = "";
            for (int i = 0; i < maxLives; i++)
            {
                hearts += (i < _lives) ? "♥ " : "♡ ";
            }
            livesText.text = $"LIVES: {hearts.TrimEnd()}";
        }

        if (comboText != null)
        {
            if (_comboCount >= 2)
            {
                comboText.text = $"COMBO x{_comboCount}!";
            }
            else
            {
                comboText.text = "";
            }
        }
    }

    /// <summary>
    /// Spawns world-space floating combat text that floats upward and fades out.
    /// </summary>
    public void SpawnFloatingText(string message, Vector3 worldPosition, Color color)
    {
        GameObject textObj = new GameObject("FloatingText");
        textObj.transform.position = worldPosition + Vector3.up * 0.4f;

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = message;
        textMesh.characterSize = 0.16f;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = color;

        MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 50;

        textObj.AddComponent<FloatingTextAnimator>();
    }
}

/// <summary>
/// Smooth upward float and alpha fade out for floating combat text.
/// </summary>
public class FloatingTextAnimator : MonoBehaviour
{
    private float _duration = 0.85f;
    private float _elapsed = 0f;
    private TextMesh _textMesh;
    private Color _initialColor;

    private void Start()
    {
        _textMesh = GetComponent<TextMesh>();
        if (_textMesh != null)
        {
            _initialColor = _textMesh.color;
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _duration;

        transform.position += Vector3.up * (Time.deltaTime * 1.5f);

        if (_textMesh != null)
        {
            Color c = _initialColor;
            c.a = Mathf.Lerp(1f, 0f, t * t);
            _textMesh.color = c;
        }

        if (_elapsed >= _duration)
        {
            Destroy(gameObject);
        }
    }
}
