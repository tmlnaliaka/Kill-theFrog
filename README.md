# 🐸 Kill The Frog

[![Unity Version](https://img.shields.io/badge/Unity-6000.5.2f1%20(Unity%206)-blue.svg?logo=unity)](https://unity.com)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Cross--Platform-brightgreen.svg)](https://unity.com)
[![Pipeline](https://img.shields.io/badge/Render%20Pipeline-Universal%202D%20(URP)-purple.svg)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](LICENSE)

A dynamic, high-adrenaline 2D arcade reaction game built in **Unity 6**. Tap and click hopping purple unicorn frogs across a lush pond before their timers expire. Engineered with a **100% code-driven runtime architecture** featuring 2D physics simulation, procedural animations, a real-time PCM audio synthesizer, and a procedural vector-art fallback renderer.

---

## 🧠 Key Concepts & System Architecture

### 1. Singleton Game Loop & State Machine ([`GameManager.cs`](Assets/Scripts/GameManager.cs))
- **Finite State Machine (`GameState`)**: Cleanly transitions between `StartScreen`, `Playing`, and `GameOver` states, ensuring pointer inputs, spawn timers, and countdown clocks only run during active gameplay.
- **Delta-Time Clamping**: Frame delta time is clamped (`Mathf.Min(Time.deltaTime, 0.1f)`) to prevent sudden timer jumps when switching Editor focus or experiencing OS window lag.
- **Scoring, Combo Streaks & Accuracy Penalties**:
  - **Base Hit**: `+10` points per frog eliminated.
  - **Combo Window (`2.2s`)**: Consecutive hits within `2.2` seconds increment `_comboCount`, granting `+5` bonus points per chain level (capped at `+25` bonus / `+35` total per hit).
  - **Miss Click Penalty (`-5`)**: Clicking empty water or terrain deducts `5` points (floored at `0`) and resets the active combo streak.
- **Lives (`♥♥♥`) & High Score Persistence**: Escaped frogs deduct 1 life out of 3 (`maxLives = 3`). Losing all lives or reaching `0s` on the `60s` clock triggers Game Over and persists the best score via Unity `PlayerPrefs` (`KillTheFrog_HighScore`).
- **Cross-Platform Pointer Raycasting**: Supports both Unity's **New Input System** (`Mouse.current` / `Touchscreen.current`) and **Legacy Input Manager** via conditional compilation (`#if ENABLE_INPUT_SYSTEM`), converting screen coordinates to world space for `Physics2D.Raycast` hit detection while ignoring UI clicks via `EventSystem.current.IsPointerOverGameObject()`.

### 2. Progressive Difficulty Scaling ([`GameManager.cs`](Assets/Scripts/GameManager.cs))
As the 60-second round progresses, `difficultyProgress` scales linearly from `0.0` to `1.0` (`1f - (_timeRemaining / gameDuration)`), driving three simultaneous difficulty curves via `Mathf.Lerp`:
| Parameter | Initial (`0s`) | Maximum Difficulty (`60s`) | Effect |
|---|---|---|---|
| **Spawn Interval** | `1.80s` | `0.75s` | Frogs spawn more than twice as fast |
| **Frog Lifetime** | `4.20s` | `2.40s` | Reaction window tightens significantly |
| **Hop Speed Multiplier** | `1.00x` | `1.80x` | Interval between physics hops shrinks |

### 3. 2D Physics & Procedural Animation ([`Frog.cs`](Assets/Scripts/Frog.cs))
- **Bouncy 2D Physics (`Rigidbody2D` & `CircleCollider2D`)**: Configured at runtime with `gravityScale = 2.2`, `CollisionDetectionMode2D.Continuous`, frozen Z-rotation, and a custom `PhysicsMaterial2D` (`bounciness = 0.42`, `friction = 0.35`).
- **Impulse Hopping & Squash-and-Stretch**: Periodic jump impulses apply randomized horizontal (`[-3.5, 3.5]`) and vertical (`6.5 ± 15%`) velocities paired with elastic vertical stretch (`0.8x` width, `1.3x` height) on launch and horizontal squash (`1.3x` width, `0.75x` height) upon ground collision (`relativeVelocity.y > 1.2`).
- **Elastic Overshoot Pop-In**: Newly spawned frogs animate from scale `0` to `1` using a cubic back-out overshoot curve (`s = 1.7`).
- **Urgency Warning System**: During the final `1.6s` of a frog's lifetime, urgency scales from `0` to `1`, accelerating a sinusoidal red-tint blink (`8 rad/s` to `25 rad/s`) and applying random local-position coordinate jitter (`0.02` to `0.09` world units) on the visual root without disturbing the physics collider.
- **Splatter Burst & 720° Death Spin**: Eliminating a frog disables physics simulation, flashes red, spawns an 8-particle radial burst (`SplashParticleMover`) in purple/lavender/violet hues, and spins the frog `720°` while shrinking to zero.

### 4. Dual-Mode Visual Pipeline ([`FrogVisual.cs`](Assets/Scripts/FrogVisual.cs))
- **Decoupled Visual Root**: Separates `VisualRoot` transforms (used for warning jitter and tinting) from the root `Rigidbody2D` physics object.
- **Primary Sprite & Procedural Vector Fallback**: Loads `Assets/Resources/Frog.png` (or reads PNG bytes from disk) and normalizes bounds to `1.6` world units. If no external sprite is present, `BuildProceduralVectorFrog()` procedurally generates a `128x128` antialiased circle texture and composites a multi-part layered frog (violet feet, royal purple body, soft lavender belly, rosy cheeks, eyelids, pupils, dual anime specular highlights, and unicorn horn).

### 5. Zero-Asset Procedural Audio Synthesizer ([`SoundGenerator.cs`](Assets/Scripts/SoundGenerator.cs))
Generates 44.1 kHz mono PCM waveforms in memory at runtime using `AudioClip.Create`—requiring zero external `.wav` or `.mp3` files:
- **`SFX_Kill`**: `0.18s` upward frequency sweep (`320 Hz → 1250 Hz`) with a 2nd harmonic and fast exponential decay.
- **`SFX_Miss`**: `0.24s` downward pitch drop (`120 Hz → 40 Hz`) blended with exponentially decaying white noise for a water-thud splash.
- **`SFX_Ribbit`**: `0.42s` two-syllable frog croak using `38 Hz` sinusoidal frequency modulation (`195 Hz ± 65 Hz`) and a `2.3x` formant resonance harmonic.
- **`SFX_Start`**:Ascending 5-note C-major arpeggio chime (`C4, E4, G4, C5, E5`).
- **`SFX_GameOver`**: Descending 4-note minor sting (`A4, G4, F4, D4`).

### 6. Code-First Runtime Scene Bootstrapper ([`GameBootstrap.cs`](Assets/Scripts/GameBootstrap.cs))
- Automatically constructs the entire playable scene on `Awake()`: orthographic camera, 4 bouncy perimeter collider walls (`ScreenBoundaries`), 5-layer procedural pond environment (rolling hills, sandy shoreline rim, pond water, 8 notched lily pads, and wrapping `CloudDrifter` sky clouds), off-screen `FrogTemplatePrefab`, `EventSystem`, and a `1920x1080` scaled `HUDCanvas` (Top Bar, Start Screen, and Game Over Screen).

---

## 🔄 Recent Updates & Changelog

- **Documentation & Architecture Presentation**:
  - Added [`Kill_The_Frog_GameManager_Presentation.docx`](Kill_The_Frog_GameManager_Presentation.docx) detailing the `GameManager` state machine, difficulty scaling formulas, and event lifecycle.
  - Expanded `README.md` with comprehensive technical breakdowns of all six core runtime subsystems.
- **Unity MCP & Tooling Upgrade**:
  - Upgraded `com.ivanmurzak.unity.mcp` package from `1.3.0` to `1.4.0` (`UNITY_MCP_DEPS_15`) and updated NuGet `McpPlugin` / `ReflectorNet` assemblies and RQL skill definitions.
- **Repository Hygiene**:
  - Updated `.gitignore` to exclude Unity automatic crash recovery directories (`Assets/_Recovery/`).

---

## 📂 Project Architecture

```text
Kill theFrog/
├── Assets/
│   ├── Resources/
│   │   └── Frog.png             # Purple unicorn frog sprite asset (Resources)
│   ├── Scenes/
│   │   └── SampleScene.unity    # Primary playable scene with GameBootstrap
│   ├── Scripts/
│   │   ├── Frog.cs              # Physics, jumping, lifetime, death & escape routines
│   │   ├── FrogVisual.cs        # 2D sprite rendering, procedural fallback, warning tints
│   │   ├── GameBootstrap.cs     # Scene generation, environment layers, UI canvas setup
│   │   ├── GameManager.cs       # Score, combo tracking, strike counter, game loop
│   │   └── SoundGenerator.cs    # Procedural audio waveform synthesis (44.1 kHz PCM)
│   ├── Settings/                # URP 2D Graphics Settings and pipeline assets
│   └── Sprites/
│       └── Frog.png             # Purple unicorn frog sprite asset
├── Packages/
│   └── manifest.json            # Unity Package Manager manifest
├── ProjectSettings/             # Physics2D, Tag, Input, and Player configuration
└── Kill_The_Frog_GameManager_Presentation.docx # GameManager technical presentation
```

---

## 🚀 How to Run

### In the Unity Editor
1. Open **Unity Hub**.
2. Add and open this project directory (`Kill theFrog`) using **Unity 6 (6000.5.2f1)**.
3. Open `Assets/Scenes/SampleScene.unity`.
4. Press **Play** (▶) in the top toolbar to start playing immediately in the Game View.

### Building Standalone Executable
You can build a standalone Windows executable directly from the command line:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.2f1\Editor\Unity.exe" -batchmode -quit -projectPath "." -buildWindows64Player "Builds\KillTheFrog.exe"
```
Then run `Builds\KillTheFrog.exe` to launch the game window.

---

## ⌨️ Controls

| Action | Control |
|---|---|
| **Whack / Kill Frog** | Left Click / Touch Tap |
| **Start / Retry** | Click `START GAME` / `PLAY AGAIN` Button |

---

## 🛠️ Requirements & Tech Stack

- **Engine**: Unity 6 (`6000.5.2f1`)
- **Render Pipeline**: Universal Render Pipeline 2D (URP 2D)
- **Scripting Runtime**: .NET Standard 2.1 / C# 9.0+
- **Physics**: 2D Physics Engine (Box2D Subsystem)
