# Demo Recording Plan

Target 6-7 minutes. Record the build in `Build/Windows/`, not the editor — the
editor Game view ignores the resolution setting and the toolbar is in shot.

## Before recording

Run the exe once, set fullscreen off and 1920x1080, save, then restart it. Decide
whether you want a clean save, so the tutorial and unlock toasts fire, or an
existing profile, so the XP bar and badges look populated. Restore Defaults resets
settings, not progress.

Close Discord and anything else that pops notifications. `Win+Alt+R` starts and
stops Game Bar; output lands in `Videos/Captures`.

## Shots

**1. Main menu, 0:00-0:35.** Let the title and profile card sit for a few seconds.
Say the name, that it teaches programming logic through command-queue puzzles, and
that it is Unity 6 URP. Point at the level badge, XP bar and region progress.

**2. Sequences, 0:35-1:45.** PLAY > Beginner, take a Sequence Valley level. Show the
command palette and name the seven commands, build a short queue, then RUN and
narrate the robot stepping through it one command at a time. Finish on the win panel
with stars and XP. The point to land: the queue is a program, and running it is
executing that program.

**3. Loops, 1:45-3:00.** A Loop Factory level with a repeating path. Build it the long
way first with repeated MOVE and read out the block count, then rebuild it as
`LOOP xN ... END` and show the count drop. Run it and show the optimal-token bonus
star. This is the strongest teaching moment in the demo — the loop is visibly cheaper,
not just shorter — so give it room.

**4. Conditionals, 3:00-4:00.** A Logic Forest level with an obstacle. Use
`IF_OBSTACLE ... IF_END` and narrate the branch being taken or skipped. Worth failing
on purpose first by crashing into a block, so the fail message and retry are on
camera. Unlimited retries is a stated requirement, so show it deliberately.

**5. Challenge mode, 4:00-5:15.** Say the maps are procedurally generated, that a BFS
solver guarantees every map is solvable, and that loop-compression DP sets the optimal
token target. That part exceeds the Project 1 spec. Clear two or three maps and land
on the run summary.

**6. Progression, 5:15-6:15.** Back to the menu so the XP bar animates and the level
badge pulses. Open the Codex, then Achievements. Mention that unlock toasts slide in
during play.

**7. Settings, 6:15-6:50.** Drag a volume slider so it is audibly independent. With
fullscreen off, switch 1280x720 to 1920x1080 so the window visibly resizes on camera.
Turn on Colour Blind Mode and show the board repaint plus the `[OK]` / `[X]` glyphs —
call it out as an accessibility requirement, not decoration.

**8. Close, 6:50-7:00.** Back to the menu. One line on what is built and what is next.

## Notes

Don't narrate menus you are only passing through; silence beats "now I click here".
If a take breaks, keep rolling and redo that segment — trimming later is faster than
starting over. The language dropdown is non-functional, so don't open it on camera.
Fail messages no longer print raw grid coordinates, so they read fine.
