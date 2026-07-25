# Requirements Traceability — Codex World

Maps every requirement and design element from the Project 1 report (Chapters 3–4) to the
delivered game, as a moderator would when grading Project II CLO1 ("a working system that
fulfils all the requirements"). Status is one of: **Met**, **Partial**, **Deviated** (built
differently from the spec), **Missing**, or **Added** (in the game, not in the spec).

## Functional Requirements (Table 3.5)

| ID | Requirement | Status | Evidence / Note |
|----|-------------|--------|-----------------|
| FR-01 | Input sequential commands to control a character | **Met** | Command palette builds an ordered queue. |
| FR-02 | Sequences, loops, conditionals | **Met** | LOOP ×2–4, IF_OBSTACLE, IF_CLEAR, nested. |
| FR-03 | Execute and visually simulate in real time | **Met** | Robot animates the queue step by step. |
| FR-04 | Immediate feedback on objective | **Met** | Win/Fail panels + status line. |
| FR-05 | Progressive level structure across regions | **Partial** | Progression present, but **9 levels (3/region)** vs the **12 (4/region)** stated in §4.3.4. |
| FR-06 | Track and display XP and level | **Met** | XP bar, pixel level badge, profile card. |
| FR-07 | Unlimited retries | **Met** | |
| FR-08 | Tutorial during introductory level | **Met** | Contextual lessons (basics/loops/ifs). |
| FR-09 | Auto-save on level completion | **Met** | PlayerPrefs (spec says JSON — see design deviations). |
| FR-10 | Hint after a defined number of attempts | **Partial** | Free hint after 2 fails exists in **Challenge mode only**; beginner levels have no hint. |

**8 of 10 fully met; 2 partial.** The core functional spec is satisfied.

## Non-Functional Requirements (Table 3.6)

| ID | Requirement | Status | Note |
|----|-------------|--------|------|
| NFR-01 | ≥30 FPS | **Met** | Lightweight scene. |
| NFR-02 | Intuitive, no manual | **Met** | Tutorial + clear UI. |
| NFR-03 | Clear simple English | **Met** | |
| NFR-04 | Windows 10+, no extra software | **Met** | Standalone build verified to launch. |
| NFR-05 | Level load < 5s | **Met** | |
| NFR-06 | Persistent local save across restart | **Met** | PlayerPrefs. |
| NFR-07 | Standalone, offline | **Met** | |
| NFR-08 | Assets original / open-licensed | **At risk** | `pixel.ttf` is **non-commercial, no-redistribution** (fine for academic use, but see Asset Licences in README). Confirm the SBS Tiny Texture Pack and Stylized Nature MegaKit licences permit academic use. |

**7 of 8 met; NFR-08 needs a licence check.**

## Chapter 4 design deviations (where a careful marker docks completeness)

These are the visible mismatches between the documented design and the built game. None are
"bugs" — several are the game exceeding the spec — but each should be **acknowledged and
justified in the Project 2 report** under the iterative methodology the project already adopts.

| Design element (report) | Built game | Type |
|---|---|---|
| 2D pixel-art sprites, 32×32 tilemap, Screen-Space-Camera | **3D god-view, URP, 3D models** | **Deviated** — biggest single mismatch |
| 12 levels, 4 per region | 9 levels, 3 per region | Deviated (reduced scope) |
| Regions: report is self-inconsistent (Ch.1 lists 4: Sequence Valley / Loop Factory / Logic Forest / Algorithm Tower; §4 lists 3) | **Sequence Valley / Loop Factory / Logic Forest** (standardised) | **Resolved** — game, README and this doc now agree; use these three names in the Project 2 report and drop the phantom "Algorithm Tower" region |
| 7 commands (Table 4.1) | Exactly those 7 | **Met** |
| Star formula S = C + E + O (efficiency = ≤75% of slots, optimality = ≤50% + optimal count) | 1★ complete / 2★ optimal tokens / 3★ no failed runs | Deviated (different rule) |
| XP = 50 + (S−1)×25, 3★ = 100 | baseXP 100 / bonus 50 | Deviated (different constants) |
| Drag-and-drop command assembly | Click-to-add buttons | Deviated |
| Gameplay Mode + Scenario Mode | Beginner + **Challenge** (procedural) | Partial (2nd mode differs) |
| Student's Journey: + command-usage bar chart + timeline | Region panels only (mastery/levels/stars/XP) | Partial (charts/timeline missing) |
| Settings: English + Mandarin/Malay | Language dropdown non-functional | Partial |
| Colour Blind Mode | Working | **Met** |
| Controls screen + key rebinding | — | Missing |
| World Map screen (region zones, level nodes) | `< >` level navigation | Missing |
| Step Mode + Slow/Normal/Fast speed | — | Missing |
| JSON save system (Class Diagram SaveSystem) | PlayerPrefs only | Deviated |
| Codex guidebook (8 concept entries) | Present | **Added** (not in report) |
| Procedural challenge generator (BFS + loop-compression DP) | Present | **Added** (exceeds spec) |

## How to use this for the Project 2 report

For CLO1 "completeness", the marker compares the game to the requirements. The formal FR/NFR
tables are **largely met**, which is the strong story. The Chapter-4 deviations should be
handled by a short **"Changes from Project 1"** section that states each deviation and its
justification (e.g. 2D→3D chosen to make spatial pathfinding puzzles clearer; scope refined to
9 core levels plus an infinite procedural Challenge mode; Scenario Mode realised as the
procedural Challenge system). Documented, justified evolution is exactly what the iterative
methodology in §3.1 licenses — it reads as critical thinking, not as failure to deliver.
