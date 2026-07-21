# Challenge Mode — Design & Implementation Plan

Codex World's second game mode (entered from PLAY > CHALLENGE MODE, `cw_mode = 1`).
Five procedurally generated maps per run — different on every click — that test
everything learned in Beginner Mode. Planned 2026-07; see phases at the bottom.

## Why it fits the architecture

`LevelDefinition.BuildGridState()` scans the level root's children at runtime:
ground tiles under "Tiles" define bounds, `CellMarker` components define
obstacles / goal / spawn. The interpreter, star scoring and win/fail flow all
consume that scan. **A generator therefore only has to spawn tiles + markers
under a level root — all existing game logic works unchanged.**

## Generator design

- **Carve-first:** random-walk a guaranteed route from spawn to beacon (biased
  to produce straight runs and turns), add dead-end branches, fill with
  obstacles to a target density, BFS-validate as a safety net (retry cap).
- **Dual-optimal validation (the "combined" guarantee):** BFS over
  (position, facing) states computes two optimal token counts —
  `fullOptimal` (loops + IFs allowed) and `sequenceOnlyOptimal` (moves/turns only).
  A map is accepted only if `sequenceOnlyOptimal > budget >= fullOptimal`,
  so a plain sequence cannot fit the command budget: loops/conditionals are
  mandatory, not optional.
- **Seed per run:** new seed on every PLAY click; shown on the HUD
  ("MAP SEED: 4F2A9") so runs can be shared/reproduced.

## Difficulty: hard and combined (no ramp)

All five maps are exam-level (9x9–11x11, all commands, tight budget). Variety
comes from shuffled shape archetypes, not difficulty tiers:

1. **Serpentine** — repeated S-bends; nested-loop territory.
2. **Spiral** — shrinking repetition; loop off-by-one traps.
3. **Minefield** — obstacles on the ideal line; IF-branching beats hand dodging.
4. **Liar's maze** — plausible dead ends; punishes queue-and-pray.
5. **Gauntlet** — all motifs combined.

Pressure levers (use 2–3, not all): failed run costs a star; no TIP text on the
fail panel (only where the run ended); optional run timer -> `cw_challenge_best`.
HCI rule: difficulty must come from reasoning depth, never information hiding —
grid fully visible, budget and star rules stated up front.

**Stars per map:** 1* complete within budget · 2* <= optimal+1 tokens ·
3* exact optimal with no failed run.

## Hint system (fairness)

All hints derive from the solver output the generator already computes.

1. **Three-tier ladder per map** (progressive disclosure):
   - Tier 1 *spatial* — optimal route tiles glow ~3 s, then fade.
   - Tier 2 *structural* — reveal the skeleton: "Optimal: 11 tokens — 2 loops, 1 IF."
   - Tier 3 *partial answer* — auto-fill the first 3–4 optimal commands.
2. **3 hint tokens per 5-map run** — any tier costs one token (scarcity = strategy).
3. **Adaptive free hint** — 2 failed runs on the same map offers a free Tier 1
   ("Struggling? First hint is free"). Rescues stuck players only.
4. **Cost = stars, never completion** — any hint caps that map at 2*, stated on
   the button before use. Completion is always possible; 3* stays unaided mastery.

UI: HINT button + token badge in the command panel; tier picker with costs;
fail panel gains a USE HINT shortcut when the free hint triggers.

## Progress & scoring isolation

- Challenge runs are roguelike: no `cw_done_*` / `cw_bonus_*` writes
  (guard `ProgressManager.CompleteLevel` behind `cw_mode == 0`).
- Persist only `cw_challenge_best` (maps cleared, total time).
- Beginner achievements unaffected; challenge achievements possible later.

## Build phases

1. **Generator core** — `ChallengeMapGenerator` (pure C#, seeded): carve +
   decorate + dual-optimal BFS solver/validator; returns 5 map models.
2. **Runtime level builder** — instantiate level root + `LevelDefinition` +
   tiles + `CellMarker`s from a model, cloning visuals/materials (R2_/R3_) from
   an existing level.
3. **GameController branch + hints + UI** — `cw_mode == 1` swaps in generated
   levels; command budget enforcement; hint ladder/tokens/free-hint/star cap;
   "CHALLENGE n / 5 | SEED xxxx" top bar; NEXT MAP / CHALLENGE COMPLETE panels.
4. **Balance harness** — editor menu item generating ~200 maps per archetype,
   logging solvability %, path length and optimal-token spread to tune density.

Risks: degenerate maps (validator + retry cap), camera framing on 11x11
(GodViewCamera may need per-map bounds fit), generation time (negligible at
this grid size).
