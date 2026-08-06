# Challenge Mode

Second mode, reached from PLAY > CHALLENGE MODE (`cw_mode = 1`). Five generated
maps per run, different every time. Written up in July 2026, before building it.

## Why it drops in cleanly

`LevelDefinition.BuildGridState()` reads the level root's children at runtime:
ground tiles under "Tiles" set the bounds, `CellMarker` components mark obstacles,
goal and spawn. Everything downstream — interpreter, star scoring, win/fail flow —
works off that scan. So a generator only has to spawn tiles and markers under a
level root, and the existing game logic needs no changes at all.

## Generator

Carve first: random-walk a route from spawn to beacon, biased towards straight runs
and turns, then add dead-end branches and fill obstacles to a target density. BFS
validates solvability as a backstop, with a retry cap.

The part that matters is the budget guarantee. A BFS over (position, facing) gives
two optimal token counts: `fullOptimal` with loops and IFs allowed, and
`sequenceOnlyOptimal` with only moves and turns. A map is accepted only when

    sequenceOnlyOptimal > budget >= fullOptimal

so a flat sequence cannot fit inside the budget. That makes loops and conditionals
mandatory rather than merely more efficient, which is the whole point of the mode.

Every run gets a fresh seed, shown on the HUD, so a run can be reproduced or shared.

## Difficulty

No ramp — all five maps are exam level (9x9 to 11x11, every command available, tight
budget). Variety comes from shuffling shape archetypes instead:

1. Serpentine — repeated S-bends, nested-loop territory
2. Spiral — shrinking repetition, off-by-one traps
3. Minefield — obstacles on the ideal line, where IF beats dodging by hand
4. Liar's maze — plausible dead ends, punishes queue-and-pray
5. Gauntlet — all of the above at once

Pressure levers, pick two or three rather than all: a failed run costs a star; no tip
text on the fail panel, only where the run ended; optional run timer into
`cw_challenge_best`.

Difficulty has to come from reasoning depth, never from hiding information. The grid
stays fully visible and the budget and star rules are stated up front.

Stars: 1 for completing within budget, 2 for optimal+1 or better, 3 for exact optimal
with no failed run.

## Hints

All three tiers come out of solver output the generator already computes.

- Tier 1, spatial — the optimal route glows for about 3 seconds, then fades
- Tier 2, structural — the skeleton, e.g. "Optimal: 11 tokens, 2 loops, 1 IF"
- Tier 3, partial answer — the first three or four optimal commands get filled in

Three hint tokens per five-map run, any tier costs one, so spending them is itself a
decision. Two failed runs on the same map offers a free tier 1 for players who are
properly stuck.

Any hint caps that map at 2 stars, and the button says so before you press it. Hints
cost stars, never completion — finishing is always possible, and 3 stars stays unaided.

## Progress isolation

Challenge runs are roguelike: no `cw_done_*` or `cw_bonus_*` writes, so
`ProgressManager.CompleteLevel` is guarded behind `cw_mode == 0`. Only
`cw_challenge_best` persists (maps cleared, total time). Beginner achievements are
untouched; challenge-specific ones could come later.

## Order of work

1. `ChallengeMapGenerator` — pure seeded C#: carve, decorate, dual-optimal BFS solver
   and validator, returns five map models
2. Runtime builder — level root, `LevelDefinition`, tiles and `CellMarker`s built from
   a model, cloning visuals and materials from an existing level
3. `GameController` branch — generated levels under `cw_mode == 1`, budget enforcement,
   hint ladder and tokens, seed in the top bar, NEXT MAP and CHALLENGE COMPLETE panels
4. Balance harness — editor menu item that generates ~200 maps per archetype and logs
   solvability rate, path length and optimal-token spread, to tune density

Things that could bite: degenerate maps, which the validator and retry cap should
cover, and camera framing at 11x11 — `GodViewCamera` may need a per-map bounds fit.
Generation time is not a concern at this grid size.
