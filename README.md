# Codex World (3D) — FYP Project

Educational puzzle adventure game teaching programming fundamentals — sequences, loops, and
conditionals — through command-queue puzzles. The player programs a robot to reach a data
beacon by queueing commands, then runs the sequence and watches it execute. This is the 3D
god-view version; the earlier 2D prototype lives in `../CodexWorld2D`.

**Unity 6000.5.1f1**, Universal Render Pipeline.

## Running the project

1. Open the project in Unity **6000.5.1f1**.
2. Open `Assets/Scenes/MainMenu` and press **Play**.

Both scenes are authored and saved — nothing is generated at runtime or by an editor tool.
What you see in the Hierarchy is the game. `MainMenu` and `CodexWorld` are both registered
under **File > Build Settings** (MainMenu at index 0), which is required for scene switching
to work.

### Standalone build

**File > Build Settings > Build**, target Windows x64. The last build was verified to launch
with no runtime errors. Output is git-ignored.

## Game structure

**Beginner mode** — 9 hand-authored levels across 3 regions, each introducing one concept:

| Region | Levels |
| --- | --- |
| Sequence Valley | First Steps, The Turn, The Perimeter |
| Loop Factory | Conveyor Run, Gear Steps, Winding Path |
| Logic Forest | Fork in the Woods, Shifting Maze, Null Virus Core |

**Challenge mode** — unlocked after finishing all 9 beginner levels. Generates 5 procedural
maps per run, different every time. Maps are drawn from five archetypes (Serpentine, Spiral,
Minefield, Maze, Gauntlet) and every one is verified solvable before it is shown: a BFS solver
finds the optimal route, a dynamic-programming pass compresses it into loops, and the map is
only accepted if a flat sequence would exceed the command budget while the compressed solution
fits. This forces the player to actually use loops rather than brute-force a path.

A 3-tier hint system keeps it fair: hints cost tokens (3 per run), escalating from a glowing
path, to a structural description, to the first few solution tokens. A free hint is offered
after two failures on the same map. Using a hint caps that map at 2 stars.

## Progression and feedback

- **XP** — +100 per level completion, +50 bonus at or below the optimal command count.
- **Stars** — 1 for completing, 2 for hitting the optimal token count, 3 for no failed runs.
- **Achievements** — 18 coding-themed unlocks across Bronze/Silver/Gold tiers.
- **Codex** — an 8-entry guidebook (Sequences, Loops, Nested Loops, Conditionals, Boolean
  Logic, Debugging, Optimization, Algorithms), each with a real-world industry example.
  Entries unlock from live gameplay rules rather than being handed out.
- **Tutorial** — three contextual lessons that fire when a feature first appears: basics on
  level 1, loops when loops unlock, conditionals when they unlock.
- **Unlock toasts** — achievements and new codex entries announce themselves the moment they
  unlock instead of waiting to be discovered in a menu.
- **Analytics** — every run is appended to `codexworld_analytics.csv` in
  `Application.persistentDataPath` (mode, level, result, attempt, time, tokens, whether loops
  or conditionals were used, hints, stars) for evaluating the game with real playtest data.

All progress persists through `PlayerPrefs` under `cw_*` keys. **Reset Save** in the in-game
menu clears everything, including tutorial and codex-toast flags, so a fresh run behaves like
a first install.

## Editing the game visually

The scene is the source of truth — edit objects directly in the Unity editor, not through
generator scripts (the old builder tooling has been removed).

- **Levels** (`Levels/Level1_FirstSteps` … `Level9_NullVirusCore`):
  - Add/move/delete ground tiles under `Tiles` to change the grid.
  - Move or duplicate an obstacle to change the puzzle — its `CellMarker` follows its position.
  - Move `Goal` or `Spawn` to relocate them; `Spawn`'s CellMarker also sets the start facing.
  - Select the level root to edit its name, story, hint, allowed commands, optimal command
    count, and camera background in the **LevelDefinition** inspector.
- **Player robot** — edit meshes/scale directly; playback speeds are on **PlayerAvatar**.
- **Obstacles** — the virus crystals use a procedural mesh; glow and animation are tunable on
  the **CorruptedBlock** component, and the material is
  `Assets/Materials/CodexWorld/Obstacle_Corrupted.mat` (Smoothness / Emission).
- **Camera** — angles and speeds on **GodViewCamera** (Main Camera).
- **UI** — everything lives under `GameCanvas` / `MenuCanvas`; restyle freely. Button behaviour
  is in each button's **OnClick()** list in the Inspector, targeting public handler methods.
- **Audio** — `GameAudioMixer` (`Assets/Design/Resources`) has Master → Music / SFX groups with
  exposed `MusicVol` / `SFXVol` parameters, so the settings sliders attenuate independently.
  Music, click sounds, win/fail stingers and slider ticks are all wired through Inspector
  components — no audio code to maintain.
- **Animation** — `Assets/Animations` holds the screen fade and level-badge pulse clips; timings
  are editable in the Animation window.

## Controls (Play mode)

- Click command buttons to build a sequence; **RUN** executes it; **Undo / Clear** edit the queue.
- **Left-drag** orbits the god-view camera, **right-drag** pans, **scroll** zooms.
- **&lt; / &gt;** navigate unlocked levels; **ESC** opens the pause menu.

## Code map (`Assets/Scripts`)

**Core**
- `Core/CommandInterpreter.cs` — parses the command queue into a tree and executes it
  (nested loops/conditionals, bounds checks, goal-halt).
- `GameController.cs` — queue building, level flow, execution playback, win/fail handling,
  challenge state, hints, achievement evaluation.

**Level**
- `Level/LevelDefinition.cs` — per-level data; reads the puzzle grid from child objects.
- `Level/CellMarker.cs` — marks a cell as Obstacle / Goal / Spawn.
- `Level/ChallengeGenerator.cs` — procedural map generation, BFS solver, loop-compression DP.
- `Level/ChallengeLevelBuilder.cs` — turns a generated map into runtime level objects.
- `Level/CorruptedBlock.cs` — obstacle visuals (pulsing emission, per-instance variation).

**Game**
- `Game/PlayerAvatar.cs` — robot movement, turn and win/fail animation.
- `Game/GodViewCamera.cs` — orbiting top-down camera (new Input System).
- `Game/ProgressManager.cs` — XP, completion, stars, unlock persistence.
- `Game/AchievementManager.cs` — 18 achievement definitions and unlock state.
- `Game/AnalyticsLogger.cs` — CSV playtest logging.

**UI**
- `UI/GameUI.cs`, `UI/PauseMenu.cs`, `UI/MainMenuController.cs` — scene UI references and menus.
- `UI/GameModeScreen.cs` — beginner / challenge mode select with lock state.
- `UI/TutorialGuide.cs` — contextual lessons.
- `UI/CodexLibrary.cs`, `UI/CodexScreen.cs` — guidebook content and reader UI.
- `UI/AchievementsScreen.cs`, `UI/AchievementCard.cs` — achievements screen.
- `UI/StudentJourneyScreen.cs` — per-region mastery dashboard.
- `UI/ProfilePanel.cs`, `UI/GameProfile.cs`, `UI/LevelNumberDisplay.cs` — player profile.
- `UI/SettingsPanel.cs`, `UI/GameSettings.cs` — settings UI and persistence.
- `UI/UnlockToast.cs`, `UI/ScreenFader.cs` — unlock banners and scene transitions.

## Known gaps

- The **colour-blind toggle** and **language dropdown** in Settings are saved but not yet
  applied to anything.
- Challenge mode ends without a run-summary screen.

## Documentation

- `Docs/ChallengeMode-Plan.md` — challenge mode design: difficulty model, archetypes, the
  dual-optimal budget rule, and the hint system.
