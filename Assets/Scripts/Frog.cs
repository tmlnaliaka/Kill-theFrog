using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Controls frog simulation: physics impulse hopping, squash-and-stretch procedural jump animations,
/// warning blink & jitter before expiring, bouncy pop-in spawn, death animations, particle bursts, and escape handling.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Frog : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private FrogVisual frogVisual;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CircleCollider2D circleCol;

    [Header("Gameplay & Lifetime")]
    [SerializeField] private float lifetime = 4.2f;
    [SerializeField] private float warningThreshold = 1.6f;
    [SerializeField] private float hopIntervalMin = 1.2f;
    [SerializeField] private float hopIntervalMax = 2.4f;
    [SerializeField] private float hopForceY = 6.5f;
    [SerializeField] private float hopForceX = 3.5f;

    private float _timeRemaining;
    private bool _isAlive = true;
    private bool _isDying = false;
    private bool _isEscaping = false;
    private float _nextHopTime;
    private Vector3 _baseScale = Vector3.one;
    private Coroutine _squashStretchRoutine;

    public bool IsAlive => _isAlive && !_isDying && !_isEscaping;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (circleCol == null) circleCol = GetComponent<CircleCollider2D>();
        if (frogVisual == null) frogVisual = GetComponentInChildren<FrogVisual>();

        ConfigurePhysics();
    }

    private void ConfigurePhysics()
    {
        rb.gravityScale = 2.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        circleCol.radius = 0.75f;
        circleCol.isTrigger = false;

        // Bouncy physics material
        PhysicsMaterial2D mat = new PhysicsMaterial2D("FrogBounceMat")
        {
            bounciness = 0.42f,
            friction = 0.35f
        };
        circleCol.sharedMaterial = mat;
    }

    private void Start()
    {
        _timeRemaining = lifetime;
        _nextHopTime = Time.time + UnityEngine.Random.Range(0.4f, 1.0f);
        _baseScale = transform.localScale;
        if (_baseScale.x < 0.1f || _baseScale.y < 0.1f) _baseScale = Vector3.one;

        // Pop-in bouncy spawn animation
        StartCoroutine(PopInRoutine());
    }

    public void Setup(float customLifetime, float hopFrequencyMultiplier)
    {
        lifetime = Mathf.Max(2.2f, customLifetime);
        _timeRemaining = lifetime;
        hopIntervalMin = Mathf.Max(0.7f, hopIntervalMin / hopFrequencyMultiplier);
        hopIntervalMax = Mathf.Max(1.2f, hopIntervalMax / hopFrequencyMultiplier);
    }

    private void Update()
    {
        if (!_isAlive || _isDying || _isEscaping) return;

        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        _timeRemaining -= dt;

        // Dynamic warning blink + jitter when frog is about to expire
        if (_timeRemaining <= warningThreshold)
        {
            float urgency = 1f - (_timeRemaining / warningThreshold);
            float blinkSpeed = Mathf.Lerp(8f, 25f, urgency);
            float blink = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f;
            float jitterAmount = Mathf.Lerp(0.02f, 0.09f, urgency);

            if (frogVisual != null)
            {
                frogVisual.SetWarningState(blink * urgency, jitterAmount);
            }
        }
        else
        {
            if (frogVisual != null)
            {
                frogVisual.SetWarningState(0f, 0f);
            }
        }

        // Periodic impulse hops
        if (Time.time >= _nextHopTime)
        {
            DoHop();
            _nextHopTime = Time.time + UnityEngine.Random.Range(hopIntervalMin, hopIntervalMax);
        }

        // Expiration -> Frog Escaped
        if (_timeRemaining <= 0f)
        {
            StartCoroutine(EscapeRoutine());
        }
    }

    private void DoHop()
    {
        if (!_isAlive || _isDying || _isEscaping) return;

        // Random horizontal jump impulse and strong vertical hop
        float dirX = UnityEngine.Random.Range(-hopForceX, hopForceX);
        float forceY = UnityEngine.Random.Range(hopForceY * 0.85f, hopForceY * 1.15f);

        rb.linearVelocity = new Vector2(dirX, forceY);

        // Procedural squash & stretch
        TriggerSquashAndStretch(0.8f, 1.3f, 0.35f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // When landing on ground or lily pad, squash outward briefly
        if (_isAlive && collision.relativeVelocity.y > 1.2f)
        {
            TriggerSquashAndStretch(1.3f, 0.75f, 0.22f);
        }
    }

    private void TriggerSquashAndStretch(float targetScaleX, float targetScaleY, float duration)
    {
        if (_squashStretchRoutine != null)
        {
            StopCoroutine(_squashStretchRoutine);
        }
        _squashStretchRoutine = StartCoroutine(SquashStretchRoutine(targetScaleX, targetScaleY, duration));
    }

    private IEnumerator SquashStretchRoutine(float targetScaleX, float targetScaleY, float duration)
    {
        Vector3 initial = transform.localScale;
        Vector3 target = new Vector3(_baseScale.x * targetScaleX, _baseScale.y * targetScaleY, _baseScale.z);

        float half = duration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Mathf.Min(Time.deltaTime, 0.05f);
            transform.localScale = Vector3.Lerp(initial, target, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Mathf.Min(Time.deltaTime, 0.05f);
            transform.localScale = Vector3.Lerp(target, _baseScale, elapsed / half);
            yield return null;
        }

        transform.localScale = _baseScale;
    }

    private IEnumerator PopInRoutine()
    {
        transform.localScale = Vector3.zero;
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float dt = Time.timeScale > 0.001f ? Time.deltaTime : Time.unscaledDeltaTime;
            elapsed += Mathf.Min(dt, 0.05f);
            float t = elapsed / duration;
            // Overshoot elastic curve: t^2 * ((s+1)*t - s)
            float s = 1.7f;
            float scale = Mathf.Clamp01(t);
            float overshoot = (scale - 1f) * (scale - 1f) * ((s + 1f) * (scale - 1f) + s) + 1f;
            transform.localScale = _baseScale * overshoot;
            yield return null;
        }

        transform.localScale = _baseScale;
    }

    /// <summary>
    /// Called when the player clicks or taps on the frog.
    /// </summary>
    public void Kill()
    {
        if (!_isAlive || _isDying) return;

        _isAlive = false;
        _isDying = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        if (circleCol != null)
        {
            circleCol.enabled = false;
        }

        StartCoroutine(DeathAnimationRoutine());
    }

    private IEnumerator DeathAnimationRoutine()
    {
        // 1. Red flash & particle burst
        if (frogVisual != null)
        {
            frogVisual.FlashRed();
        }

        SpawnSplashBurst(transform.position);

        // 2. 720 degree spin, squash, and shrink to 0
        float duration = 0.38f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Quaternion startRot = transform.rotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 720 degree spin
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, t * 720f);

            // Squash down and shrink
            float scaleX = Mathf.Lerp(startScale.x * 1.25f, 0f, t);
            float scaleY = Mathf.Lerp(startScale.y * 0.6f, 0f, t);
            transform.localScale = new Vector3(scaleX, scaleY, startScale.z);

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator EscapeRoutine()
    {
        _isAlive = false;
        _isEscaping = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFrogEscaped(this);
        }

        // Quick hop upward and fade/shrink away
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(UnityEngine.Random.Range(-1.5f, 1.5f), 7.5f);
            rb.gravityScale = 1.0f;
        }

        float duration = 0.45f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (frogVisual != null)
            {
                frogVisual.SetAlpha(1f - t);
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Spawns an 8-particle sparkling splash burst on death.
    /// </summary>
    private void SpawnSplashBurst(Vector3 center)
    {
        int particleCount = 8;
        Sprite circleSprite = FrogVisual.GetCircleSprite();
        Color[] splashColors = {
            new Color(0.72f, 0.35f, 0.98f), // Purple
            new Color(0.88f, 0.70f, 1.0f),  // Soft Lavender
            new Color(0.55f, 0.20f, 0.88f), // Deep Violet
            Color.white
        };

        for (int i = 0; i < particleCount; i++)
        {
            float angle = (i / (float)particleCount) * 360f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            GameObject p = new GameObject("SplashParticle_" + i);
            p.transform.position = center + (Vector3)(dir * 0.15f);
            p.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.2f, 0.35f);

            SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = circleSprite;
            sr.sharedMaterial = FrogVisual.GetDefaultSpriteMaterial();
            sr.color = splashColors[i % splashColors.Length];
            sr.sortingOrder = 25;

            // Simple self-destroying particle mover
            p.AddComponent<SplashParticleMover>().Init(dir * UnityEngine.Random.Range(4.5f, 7.5f));
        }
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameActive)
        {
            GameManager.Instance.HitFrog(this);
        }
    }
}

/// <summary>
/// Helper component for procedural sparkling burst particles.
/// </summary>
public class SplashParticleMover : MonoBehaviour
{
    private Vector2 _velocity;
    private float _lifetime = 0.45f;
    private float _elapsed = 0f;
    private SpriteRenderer _sr;
    private Vector3 _startScale;

    public void Init(Vector2 velocity)
    {
        _velocity = velocity;
        _sr = GetComponent<SpriteRenderer>();
        _startScale = transform.localScale;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _lifetime;

        transform.position += (Vector3)(_velocity * Time.deltaTime);
        _velocity = Vector2.Lerp(_velocity, Vector2.zero, Time.deltaTime * 6f);

        transform.localScale = Vector3.Lerp(_startScale, Vector3.zero, t);
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = 1f - t;
            _sr.color = c;
        }

        if (_elapsed >= _lifetime)
        {
            Destroy(gameObject);
        }
    }
}
