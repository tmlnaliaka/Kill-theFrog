# 🐸 Kill The Frog

[![Unity Version](https://img.shields.io/badge/Unity-6000.5.2f1%20(Unity%206)-blue.svg?logo=unity)](https://unity.com)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Cross--Platform-brightgreen.svg)](https://unity.com)
[![Pipeline](https://img.shields.io/badge/Render%20Pipeline-Universal%202D%20(URP)-purple.svg)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest)
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](LICENSE)

A dynamic, high-adrenaline 2D arcade reaction game created in **Unity 6**. Tap and click hopping purple unicorn frogs across a lush pond before their timers expire. Features procedural animations, dynamic physics hops, warning alerts, combo systems, and an integrated audio synthesizer.

---

## 🎮 Gameplay & Mechanics

- **Physics-Driven Hopping**: Frogs utilize `Rigidbody2D` with dynamic physics impulses, random trajectory arcs, and realistic 2D bounce physics across lily pads.
- **Procedural Squash & Stretch**: Jump impulses trigger elastic vertical stretching, while ground landings produce juicy squash deformations.
- **Urgency Warning System**: When a frog's lifetime runs low (< 1.6s remaining), it enters a high-urgency state with rapid color blinking and procedural coordinate jitter before escaping.
- **Splatter & Spin Death**: Clicking or tapping a frog triggers a 720° spin compression, flash, and sparkling 8-way lavender & violet particle burst.
- **Dynamic Combo Multipliers**: Swift consecutive frog eliminations build combo chains that multiply score points.
- **Escapes & Strikes System**: Missed frogs escape into the sky; 3 escapes trigger a Game Over screen with high score tracking.
- **Progressive Difficulty Scaling**: Frog lifetimes decrease, spawn intervals tighten, and jump frequencies accelerate as your score increases.

---

## 🎨 Visual Aesthetics & Design

- **Vibrant Purple Unicorn Frog**: Refactored sprite palette featuring rich royal purple hues (Hue 270°–280°), soft lavender accents, rosy orchid cheeks, and a luminous horn.
- **Procedural Fallback Vector Engine**: Includes an in-engine procedural rendering pipeline that generates vector-accurate frogs using smooth antialiased procedural circle primitives with eyelids, pupils, and specular highlights.
- **Multi-Layered Environment**:
  - Sky blue gradient backdrop with drifting clouds.
  - Layered distant rolling hills and pond banks.
  - Deep blue water basin with organic shoreline.
  - Floating green lily pads with realistic cutout notches.
- **Integrated Synthesizer Audio**: Procedurally generated audio waveforms (`SoundGenerator.cs`) for clicks, squashes, pop-ins, escapes, and game over stings—zero missing audio asset dependencies.

---

## 📂 Project Architecture

```
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
│   │   └── SoundGenerator.cs    # Procedural audio waveform synthesis
│   ├── Settings/                # URP 2D Graphics Settings and pipeline assets
│   └── Sprites/
│       └── Frog.png             # Purple unicorn frog sprite asset
├── Packages/
│   └── manifest.json            # Unity Package Manager manifest
└── ProjectSettings/             # Physics2D, Tag, Input, and Player configuration
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
| **Start / Retry** | Click Start Game / Try Again Button |
| **Pause / Resume** | In-game UI or Escape |

---

## 🛠️ Requirements & Tech Stack

- **Engine**: Unity 6 (6000.5.2f1)
- **Render Pipeline**: Universal Render Pipeline 2D (URP 2D)
- **Scripting Runtime**: .NET Standard 2.1 / C# 9.0+
- **Physics**: 2D Physics Engine (PhysX / Box2D 2D Subsystem)
