# Requirements Traceability

Checks the requirements and design from the Project 1 report (Chapters 3-4) against
what actually got built. Status is one of Met, Partial, Deviated (built differently
from the spec), Missing, or Added (in the game but not in the spec).

## Functional requirements (Table 3.5)

| ID | Requirement | Status | Note |
|----|-------------|--------|------|
| FR-01 | Input sequential commands to control a character | Met | Command palette builds an ordered queue. |
| FR-02 | Sequences, loops, conditionals | Met | LOOP x2-4, IF_OBSTACLE, IF_CLEAR, nested. |
| FR-03 | Execute and visually simulate in real time | Met | Robot animates the queue step by step. |
| FR-04 | Immediate feedback on objective | Met | Win/fail panels plus the status line. |
| FR-05 | Progressive level structure across regions | Partial | Progression is there, but 9 levels (3 per region) against the 12 (4 per region) in §4.3.4. |
| FR-06 | Track and display XP and level | Met | XP bar, level badge, profile card. |
| FR-07 | Unlimited retries | Met | |
| FR-08 | Tutorial during introductory level | Met | Contextual lessons for basics, loops, ifs. |
| FR-09 | Auto-save on level completion | Met | PlayerPrefs; the spec said JSON, see deviations below. |
| FR-10 | Hint after a defined number of attempts | Partial | The free hint after 2 fails exists in Challenge mode only. Beginner levels have no hint. |

8 of 10 fully met, 2 partial.

## Non-functional requirements (Table 3.6)

| ID | Requirement | Status | Note |
|----|-------------|--------|------|
| NFR-01 | >=30 FPS | Met | Lightweight scene. |
| NFR-02 | Intuitive, no manual needed | Met | Tutorial plus clear UI. |
| NFR-03 | Clear simple English | Met | |
| NFR-04 | Windows 10+, no extra software | Met | Standalone build verified to launch. |
| NFR-05 | Level load under 5s | Met | |
| NFR-06 | Persistent local save across restart | Met | PlayerPrefs. |
| NFR-07 | Standalone, offline | Met | |
| NFR-08 | Assets original or open-licensed | At risk | `pixel.ttf` is non-commercial and no-redistribution. Fine for academic use, a problem for anything public. Still need to confirm the SBS Tiny Texture Pack and Stylized Nature MegaKit licences. |

7 of 8 met. NFR-08 needs the licence check finishing.

## Where the build differs from Chapter 4

| Design in the report | What got built | Type |
|---|---|---|
| 2D pixel-art sprites, 32x32 tilemap, Screen-Space-Camera | 3D god-view, URP, 3D models | Deviated — the biggest single mismatch |
| 12 levels, 4 per region | 9 levels, 3 per region | Deviated, reduced scope |
| Regions: Ch.1 lists 4, §4 lists 3 | Sequence Valley / Loop Factory / Logic Forest | Resolved — game, README and this doc now agree. Drop "Algorithm Tower". |
| 7 commands (Table 4.1) | Exactly those 7 | Met |
| Star formula S = C + E + O | 1* complete, 2* optimal tokens, 3* no failed runs | Deviated, different rule |
| XP = 50 + (S-1)x25, 3* = 100 | baseXP 100, bonus 50 | Deviated, different constants |
| Drag-and-drop command assembly | Click-to-add buttons | Deviated |
| Gameplay Mode + Scenario Mode | Beginner + Challenge (procedural) | Partial, the second mode differs |
| Student's Journey with usage chart and timeline | Region panels only | Partial, charts and timeline missing |
| Settings: English plus Mandarin/Malay | Language dropdown non-functional | Partial |
| Colour Blind Mode | Working | Met |
| Controls screen with key rebinding | — | Missing |
| World Map screen | `< >` level navigation | Missing |
| Step Mode, Slow/Normal/Fast speed | — | Missing |
| JSON save system | PlayerPrefs only | Deviated |
| Codex guidebook, 8 entries | Present | Added |
| Procedural challenge generator | Present | Added |

## For the Project 2 report

The FR and NFR tables are mostly met, so completeness is the strong part. The Chapter 4
mismatches need a short "changes from Project 1" section covering the reasoning: 2D to 3D
because spatial pathfinding puzzles read more clearly in 3D; scope narrowed to 9 core
levels plus an unlimited procedural mode; Scenario Mode realised as that Challenge system.
