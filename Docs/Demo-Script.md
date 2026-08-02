# Codex World — Demo Recording Script

Target length **6–7 min**. Record `Build/Windows/FYP-CodexWorld.exe` (not the editor —
the editor's Game view does not honour the resolution setting and the toolbar looks unpolished).

## Before recording

- [ ] Run the exe once, set **Fullscreen OFF** and resolution **1920 x 1080**, SAVE. Restart it.
- [ ] Decide whether you want a clean save (tutorial + unlock toasts fire) or an existing
      profile (XP bar and badges look populated). Restore Defaults resets *settings*, not progress.
- [ ] Close Discord / notifications. Game Bar records the focused window only.
- [ ] `Win+Alt+R` to start, same again to stop. Output lands in `Videos/Captures`.

## Shot list

### 1. Main menu (0:00–0:35)
Land on the menu, let the pixel title and profile card breathe ~3 s.
Say: game name, that it teaches programming logic (sequences, loops, conditionals) through
command-queue puzzles, built in Unity 6 URP.
Point at the profile card: level badge, XP bar, region progress.

### 2. Beginner mode — sequences (0:35–1:45)
PLAY > Beginner. Take a **Sequence Valley** level.
- Show the command palette; name the 7 commands.
- Build a short queue with MOVE_FORWARD / TURN_LEFT / TURN_RIGHT.
- RUN. Narrate the robot stepping through the queue one command at a time.
- Reach the goal: win panel, stars, XP awarded.

Point to make: the queue *is* a program; running it is executing that program.

### 3. Loops (1:45–3:00)
A **Loop Factory** level with a repeating path.
- First build it the long way (MOVE_FORWARD repeated) — show the token count.
- Rebuild with `LOOP xN ... END`. Show the token count drop.
- RUN, clear it, show the **optimal-token bonus star**.

Strongest teaching moment: the loop is visibly *cheaper*, not just shorter.

### 4. Conditionals (3:00–4:00)
A **Logic Forest** level with an obstacle.
- Use `IF_OBSTACLE ... IF_END` (and/or `IF_CLEAR`).
- RUN, narrate the branch being taken or skipped.
- Optional but worth it: show a **fail** first (crash into a block) so the fail message and
  retry are on camera. Unlimited retries is a stated requirement — show it deliberately.

### 5. Challenge mode (4:00–5:15)
Menu > Challenge.
- Say maps are **procedurally generated**: a BFS solver guarantees every map is solvable, and
  loop-compression DP sets the optimal token target. This exceeds the Project 1 spec.
- Clear 2–3 maps.
- Land on the **run summary**: maps cleared, stars, hints used, time, XP earned.

### 6. Progression + extras (5:15–6:15)
- Return to menu: XP bar **animates filling up**; level badge pulses on level-up.
- Open **Codex** — concept entries unlocked through play.
- Open **Achievements** — show unlocked ones.
- Mention unlock toasts slide in during play when these trigger.

### 7. Settings / accessibility (6:15–6:50)
- Audio: Master / Music / SFX are independent AudioMixer groups — drag one, it's audible.
- Display: 3 resolutions + fullscreen. With fullscreen off, switch 1280x720 -> 1920x1080 so the
  **window visibly resizes on camera**.
- **Colour Blind Mode** ON — board repaints to blue/amber, status glyphs `[OK]` / `[X]` appear.
  Call this out as an accessibility requirement, not decoration.

### 8. Close (6:50–7:00)
Back to menu. One line: what's built, what's next.

## Notes

- Don't narrate menus you're only passing through. Silence beats "now I click here".
- If a take breaks, keep rolling and redo that segment — trimming later is faster than
  restarting the whole recording.
- Language dropdown is non-functional — **don't open it on camera**.
- Fail messages no longer print raw grid coordinates, so they read fine on camera.
