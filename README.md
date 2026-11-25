# Ninja Run – 3D Endless Runner (Unity 6, C#)

A mobile-friendly 3D endless runner set in a Japanese-themed world. The ninja runs automatically along a stone road. You switch between **two lanes** to dodge obstacles and collect coins, while the game keeps getting faster.

> Built with **Unity 6 (6000.4)**, **Universal Render Pipeline**, **C#**, **TextMeshPro** and the **new Input System**.

---

## Features

- **Automatic forward running** with smooth, physics-safe movement (kinematic Rigidbody + `MovePosition`)
- **Exactly two lanes**, with smooth lane switching (SmoothDamp) and a lean animation
- **Keyboard and touch controls**: A/D or arrow keys, plus on-screen Left/Right buttons that react on finger-down
- **4 obstacle types** (wooden crates, stone barrier, roadblock, spike trap). One lane is always left free, so the run is always fair
- **Coin rows**, some switching lanes part-way through, never placed inside obstacles
- **Score** = distance in metres + 10 points per coin, with a **saved high score** (PlayerPrefs)
- **Rising difficulty**: speed increases over time, and obstacles get closer together (gaps are measured in seconds, so they stay fair at any speed)
- **Endless world** that recycles road segments, with randomised torii gates, pagodas, cherry blossom trees and stone lanterns
- **Object pooling** (Unity's `ObjectPool<T>`) for coins and obstacles, so nothing is created or destroyed during play
- **Responsive UI** that fits portrait phones and landscape PCs
- **Full UI flow**: Start screen, HUD (coins top-left, score, pause top-right, lane buttons), Pause screen and Game Over screen with Restart
- **Audio**: koto-style looping music, plus coin, lane-switch, crash and click sound effects
- **Polish**: procedural run animation, camera shake on crash, fog, a sunset skybox, and bloom/colour-grading post-processing

## Controls

| Action | Keyboard | Mobile |
|---|---|---|
| Move to left lane | **A** / **←** | **<** button (bottom-left) |
| Move to right lane | **D** / **→** | **>** button (bottom-right) |
| Pause / Resume | **Esc** / **P** | **II** button (top-right) |
| Start / Restart | **Space** / **Enter** | **PLAY** / **RESTART** buttons |

## Architecture

The scripts are decoupled with **C# events** (Observer pattern). `GameManager` owns the game state, and the UI, audio and camera only *listen* to it.

```
GameManager (state machine: Menu → Playing ⇄ Paused → GameOver)
   │  events: StateChanged, CoinsChanged, ScoreChanged
   ├──► UIManager      – shows screens, updates counters, button handlers
   ├──► AudioManager   – music + SFX (also listens to Player & CoinCollector)
   └──► CameraFollow   – shake on crash

PlayerController ──(hits Obstacle)──► GameManager.GameOver()
CoinCollector    ──(touches Coin)───► GameManager.AddCoin()

RoadSpawner      – recycles RoadSegment prefabs ahead of the player
ObstacleSpawner  – pooled obstacles, gap shrinks with difficulty
CoinSpawner      – pooled coin rows, asks ObstacleSpawner.IsBlocked() to avoid overlaps
```

| Script | Responsibility |
|---|---|
| `Core/GameManager.cs` | Game state machine, speed and difficulty, distance, coins, score, high score, restart |
| `Core/InputHelper.cs` | Keyboard input that works with both the new and old input systems |
| `Player/PlayerController.cs` | Auto-run, two-lane switching, lean, obstacle collision |
| `Player/CoinCollector.cs` | Detects coins and reports them to the GameManager |
| `Player/NinjaAnimator.cs` | Procedural run, idle and fall animation |
| `Camera/CameraFollow.cs` | Third-person follow camera with crash shake |
| `Spawning/RoadSpawner.cs` | Endless road by recycling segments |
| `Spawning/ObstacleSpawner.cs` | Spawns and pools obstacles, increasing difficulty |
| `Spawning/CoinSpawner.cs` | Spawns and pools coin rows |
| `Spawning/PrefabPool.cs` | Generic wrapper around `UnityEngine.Pool.ObjectPool<T>` |
| `Spawning/Coin.cs`, `Obstacle.cs` | Coin spin/collect, obstacle data |
| `Environment/RoadSegment.cs` | Randomises scenery on each reuse |
| `Environment/BackgroundFollower.cs` | Keeps mountains and clouds on the horizon |
| `UI/UIManager.cs` | Screens, HUD, button handlers |
| `UI/ResponsiveCanvasScaler.cs` | Keeps the UI on screen in both portrait (phone) and landscape (PC) |
| `Audio/AudioManager.cs` | Music and sound effects |
| `Editor/NinjaRunnerBuilder.cs` | Editor tool that generates the scene, prefabs, materials and UI |

## Project Structure

```
Assets/_Project/
  Audio/        music + sound effects (.wav)
  Editor/       NinjaRunnerBuilder (menu: Ninja Runner > Build Game Scene)
  Materials/    URP Lit materials
  Meshes/       generated low-poly cone / pyramid
  Prefabs/      Environment/RoadSegment, Collectibles/Coin, Obstacles/*
  Scenes/       NinjaRunner.unity
  Scripts/      Core, Player, Camera, Spawning, Environment, UI, Audio
  Settings/     post-processing profile
```

## How to Run

1. Open the project in **Unity 6000.4.x** (Unity Hub → Add → this folder).
2. Open `Assets/_Project/Scenes/NinjaRunner.unity`.
3. Press **Play**.

To regenerate the scene from scratch, use **Ninja Runner → 2. Build Game Scene** in the menu bar.

## Building for Android

1. In Unity Hub, go to **Installs → (your Unity version) → Add modules → Android Build Support**, and include OpenJDK and the Android SDK & NDK.
2. In Unity, go to **File → Build Profiles → Android → Switch Platform**.
3. The game is already set to portrait mode with the package name `com.akhil.ninjarun`.
4. Connect your phone (with USB debugging on) and click **Build And Run**, or click **Build** to get an `.apk`.

## Possible Next Steps

- Replace the placeholder ninja with a rigged model and an Animator (e.g. a free Mixamo character)
- Swipe gestures, power-ups (magnet, shield) and a coin shop
- Floating-origin reset for very long runs
- Leaderboards with Unity Gaming Services
