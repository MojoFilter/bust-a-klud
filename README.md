# Bust-A-Klud

A whimsical, spooky bubble-shooter in the spirit of Snood and Bust-A-Move, built in **Unity 6000.6.7f1**
(built-in render pipeline, uGUI, legacy Input Manager). It plays in the editor, as a Windows build, and in the
browser as WebGL.

Aim the cauldron cannon, match 3 or more bats, pumpkins, skulls, ghosts or spiders, and cut bubbles loose from the
ceiling for bonus points. Every few shots, hit or miss, the ceiling creeps down. Blunderthor heckles from the side.

## Controls

| Action | Mouse | Keyboard | Touch |
|---|---|---|---|
| Aim | move | A / D or Left / Right | drag |
| Fire | click | Space, Enter, Up, W | release |
| Swap with the waiting bubble | right-click, or click the waiting bubble | S, Tab, Down, Shift | n/a |
| Pause | Pause button | Esc or P | Pause button |

## Opening and running

1. Open the folder in Unity Hub with editor **6000.6.7f1** (packages resolve from the editor install, no network needed).
2. Open `Assets/Scenes/Main.unity` and press Play. The scene holds just a camera and a `GameBootstrap`; the whole
   game is assembled from code at startup.
3. If the scene is ever missing, use **Bust-A-Klud > Setup Project** to recreate it and apply the player settings.

## Building

Menu: **Bust-A-Klud > Build > WebGL** or **Windows 64-bit**. Headless:

```
Unity.exe -batchmode -nographics -quit -projectPath <this folder> -buildTarget WebGL -executeMethod BustAKlud.Editor.ProjectSetup.BuildWebGL
Unity.exe -batchmode -nographics -quit -projectPath <this folder> -buildTarget StandaloneWindows64 -executeMethod BustAKlud.Editor.ProjectSetup.BuildWindows
```

Output goes to `WebBuild/` and `WinBuild/`. The WebGL build is uncompressed, so `WebBuild/` can be dropped on any
static file host (it must serve `.wasm` as `application/wasm`). It uses a custom full-window page
(`Assets/WebGLTemplates/BustAKlud`) that also stops the browser's right-click menu so swap works.

## Tests

EditMode tests cover the hex grid (including a check that every logical neighbour is exactly one bubble-diameter
away in world space), cluster and orphan detection, landing cells, level generation, and the shot-path simulator.
Run them from **Window > General > Test Runner**, or headless:

```
Unity.exe -batchmode -nographics -projectPath <this folder> -runTests -testPlatform EditMode -testResults results.xml
```

## Dev bot

Development builds include a harness that plays the game by itself and saves screenshots (it is compiled out of
release builds):

```
Bust-A-Klud.exe -bak-autoplay [-bak-shots 120] [-bak-out <folder>] [-bak-lose]
```

Build it with `BustAKlud.Editor.ProjectSetup.BuildWindowsDevelopment`. The bot simulates every aim angle and fires
the one that pops the biggest group; `-bak-lose` makes it avoid matches to reach Game Over.

## Layout

```
Assets/
  Resources/Art, Audio      the supplied art and sound effects (loaded by name at runtime)
  Resources/Shaders         additive sprite shader for glows
  Scripts/Core              pure rules, no scene dependencies: HexGrid, LevelGenerator, ShotPath, config
  Scripts/Gameplay          BoardView, BubbleView, Cannon, AimGuide, input
  Scripts/Effects           pooled FX, camera rig, backdrop, procedural sprites
  Scripts/Audio             AudioManager and the synthesised music and effects
  Scripts/UI                title, HUD, modals, text effects (all built from code)
  Scripts/App               GameBootstrap, GameController (the game flow), dev harness
  Editor/                   import rules, scene and build tooling
  Tests/EditMode/           unit tests
  WebGLTemplates/           the web page
Resources/                  the original source assets, untouched (outside Assets, so Unity ignores it)
```

## Where to tweak things

- **Board and tuning:** `GameConfig` (bubble size, rows before losing, shot speed, aim limits).
- **Levels:** `LevelGenerator` (shapes, colour counts, rows, and how many shots between ceiling drops; every shot counts, matched or not).
- **Scoring and flow:** `GameController`.
- **Fonts:** headings, buttons, scores and speech use the spooky display font at `Assets/Resources/Fonts/Title.ttf`
  (Creepster, SIL Open Font License; the license text sits beside it). Swap in any TTF with that name to restyle
  them; `UIKit.DisplayScale` compensates for fonts with a small x-height. Paragraphs and small print stay in Unity's
  built-in font for readability.
- **Look of the title and menus:** `UIKit` holds the palette; `TitleView` and `HudView` hold the layouts.

## How the sounds are used

The supplied recordings: `cannon5` fires, `bounce` is a wall hit, `thud` is a bubble sticking (and the ceiling
landing, pitched down), `bang` backs big pops. Pops, falls, UI clicks, level-clear and game-over sounds, and the
background waltz are synthesised in code (`ProceduralAudio`), so there is no extra audio to ship.
