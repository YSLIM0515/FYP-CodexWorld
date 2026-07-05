# Codex World (3D) — FYP Project

Educational puzzle adventure game teaching programming fundamentals (sequences, loops, conditionals) through command-based puzzles, per the FYP proposal. This is the 3D god-view version; the earlier 2D prototype lives in `../CodexWorld2D`.

## First-time setup (run once)

1. Open this project in **Unity 6000.5.1f1** and open `Assets/Scenes/CodexWorld`.
2. In the menu bar, click **Tools > Codex World > Build Scene Content**.
   This generates everything as real, editable objects in the Hierarchy: the 9 level layouts, AXIOM, the UI canvas, the camera controller, and material assets under `Assets/Materials/CodexWorld`.
3. **Save the scene (Ctrl+S).**

After that, just press **Play**. Nothing is created at runtime — what you see in the Hierarchy is the game.

### Main menu (draft)

Open `Assets/Scenes/MainMenu`, then click **Tools > Codex World > Build Main Menu** to
generate a draft menu (title, PLAY/CHALLENGES/LEADERBOARD/CODEX/SETTINGS/EXIT column,
and a top-right profile card) as editable Hierarchy objects. Save the scene afterwards.
It uses placeholder colours/fonts and Unity's built-in font — swap the `Background`
and `Avatar` images and restyle freely once the real art is ready. `PLAY` loads the
`CodexWorld` scene (add both scenes under **File > Build Settings**); the other buttons
are placeholders. Wiring lives on the `MenuCanvas`'s **MainMenuController** component.

### Settings screen (main menu + in-game)

A reusable settings overlay (AUDIO volume sliders, DISPLAY resolution/fullscreen,
LANGUAGE, ACCESSIBILITY, and SAVE / CANCEL / RESTORE DEFAULTS). Build it per scene:

1. In **MainMenu**: run **Build Main Menu** first, then **Tools > Codex World >
   Build Settings Panel** — it auto-wires the panel to the `SETTINGS` button.
2. In **CodexWorld**: run **Build Settings Panel** too — since there's no menu
   controller there, it adds a small ⚙ gear button (top-right) that opens the panel.
3. Save each scene.

Behaviour: **SAVE** persists settings (PlayerPrefs via `GameSettings`), **CANCEL**
closes without saving, **RESTORE DEFAULTS** resets the controls to defaults (75/50/90
volumes) — persisted on the next SAVE. Master volume previews live while dragging.
Logic is on the `SettingsPanel` component; values are stored/applied by `GameSettings`.

### Achievements screen (main menu, display-only draft)

In **MainMenu** (after Build Main Menu): run **Tools > Codex World > Build Achievements
Screen**, then save. It generates the template as editable objects under `MenuCanvas`:
Bronze/Silver/Gold tier rows (horizontal `ScrollRect`s of achievement cards), greyscale
locked cards with a rect-built padlock, an overall progress bar (% + fraction), a
gold-accented "Recently Unlocked" panel with timestamps, and a tip bar with an ESC/Back
button. The menu's **CHALLENGES** button opens it (retargeted by the builder).
All achievements are coding-themed placeholders ("Hello World" — solve your first
puzzle, "Loop Learner", "Logic Master", ...) baked into the scene — pure display, not
linked to gameplay yet. `AchievementsScreen.SetProgress()` is ready for the real system.

### Profile panel (main menu)

In **MainMenu** (after Build Main Menu): run **Tools > Codex World > Build Profile
Panel**, then save. Clicking the **avatar** on the profile card opens the editor over
a dark overlay (draft stand-in for a background blur): edit the **profile name**
(input field), pick a **profile icon** (6 placeholder options), and tick up to 3
**badges** (coding achievements) to display. **SAVE** persists everything
(PlayerPrefs via `GameProfile`) and refreshes the card — name, avatar icon, and a
mini badge strip under the card; **BACK** cancels to the main menu. Logic is on the
`ProfilePanel` component; identity data lives in `GameProfile`.

Note: **Build Main Menu** recreates `MenuCanvas`, so if you re-run it you must re-run
**Build Settings Panel**, **Build Achievements Screen**, and **Build Profile Panel**
afterwards.

## Editing the game visually

- **Levels** (under `Levels/Level1_FirstSteps` … `Level9_NullVirusCore`): the scene is the source of truth.
  - Move/add/delete ground tiles under `Tiles` to change the grid size.
  - Move a `Corrupted_x_y` cube (or duplicate one) to change obstacles — its `CellMarker` follows its position.
  - Move the `Goal` or `Spawn` objects to relocate them; `Spawn`'s CellMarker also sets AXIOM's starting direction.
  - Select the level root to edit its name, story, hint, allowed commands, optimal command count, and camera background in the **LevelDefinition** inspector.
- **AXIOM**: edit its meshes/scale directly; playback speeds are on its **PlayerAvatar** component.
- **Camera**: angles/speeds on the **GodViewCamera** component of Main Camera.
- **UI**: all texts/buttons are under `GameCanvas` — restyle freely; the **GameUI** component just references them.
  Button behaviour lives in each button's **OnClick()** list (Inspector), targeting
  `GameController`'s public handlers. After script changes, **Tools > Codex World >
  Wire Game Buttons (OnClick)** re-fills the lists without rebuilding the scene.
- **GameController**: level order, start level, and XP values.
- **Materials**: `Assets/Materials/CodexWorld` — recolor regions, goal, AXIOM.

Re-running the build tool resets the generated objects to defaults, so only re-run it if you want a fresh start.

## Controls (Play mode)

- Click command buttons to build a sequence; **RUN** executes it; **Undo/Clear** edit the queue.
- **Left-click drag** rotates the god-view camera (horizontal = orbit, vertical = tilt); **right-click drag** moves the view up/down; **mouse scroll** zooms.
- **< / >** (top left) navigate unlocked levels; **Reset Save** wipes progress.
- XP: +100 per level completion, +50 bonus at/below the optimal command count (PlayerPrefs).

## Code map (`Assets/Scripts`)

- `Core/CommandInterpreter.cs` — parses the command queue into a tree and executes it (nested loops/conditionals, bounds checks, goal-halt).
- `Core/InterpreterTests.cs` — optional: attach to any GameObject to run 9 interpreter tests in the Console.
- `Level/LevelDefinition.cs` — per-level info; reads the puzzle grid from the level's child objects.
- `Level/CellMarker.cs` — marks an object's cell as Obstacle / Goal / Spawn.
- `Game/PlayerAvatar.cs` — AXIOM playback/win/fail animations.
- `Game/GodViewCamera.cs` — orbiting top-down camera (new Input System).
- `Game/ProgressManager.cs` — XP, completion, and unlock persistence.
- `UI/GameUI.cs` — references to the scene-authored UI controls.
- `GameController.cs` — game controller: queue, level flow, execution playback.
- `../Editor/CodexWorldSceneBuilder.cs` — the one-time scene generator (Tools menu).
