# AI_CONTEXT

## Purpose

Use this file as the implementation guardrail for **Multiply2048**.

The most important truth:

> This is **not** classic swipe-2048. It is a **drag-swap merge puzzle** built around legal adjacent swaps, controlled resolve loops, stable checkpoints, and a full-board start.

---

## Non-negotiable gameplay truths

1. Stable boards are normally full.
2. The player swaps with **one adjacent orthogonal neighbor**.
3. A move is legal **only if the swap creates at least one merge group**.
4. Merge groups come from horizontal/vertical lines of 3+ equal values, with connected same-value valid lines resolving as one group.
5. After a successful move, the board resolves until stable.
6. Undo, save/resume, and rewarded continue must operate on **stable checkpoints**, not mid-animation states.

---

## Put changes in the right place

### `BoardController`
Use for:
- gameplay rules
- swap validation
- match detection / merge logic
- gravity / refill
- hints
- start-board generation
- undo snapshot data
- board import/export
- board-facing versus presentation

### `GameManager`
Use for:
- mode flow
- menu / HUD / game over panels
- score routing
- undo / shuffle economy
- persistence orchestration
- rewarded continue orchestration
- versus turn timer

### `CandyTile`
Use only for tile presentation:
- text
- color refresh
- movement animation
- label rotation
- idle-hint visuals

### `ThemeManager`
Use for palette selection and palette refresh.

### `AudioManager` / `MobileAdsManager`
Treat as persistent service singletons.

Do not move core gameplay authority into UI helper scripts.

---

## High-risk current truths to preserve

### 1) Scoring is **not** x2 right now
Older assumptions about a `GameManager.AddScore` x2 multiplier are stale.

Current behavior:
- `AddScore` applies the incoming amount directly
- score is gated by `ScoreCountingEnabled`
- versus routes score to the current scoring player

If you change scoring, document it explicitly.

### 2) `PlayerHasMoved` is not the full scoring rule
Do not treat `PlayerHasMoved` as the real score gate.

Current scoring gate is `ScoreCountingEnabled`.
That distinction matters for:
- fresh runs
- restart
- restore/import
- undo
- rewarded continue

### 3) Successful player moves currently score **all resolve passes**
A successful swap enables score counting and calls resolve with `scoreAllPasses: true`.

So in the current codebase:
- opening normalization does not score
- failed swaps do not score
- player-triggered successful move chains currently do score across the full resolve loop
- special milestone-cascade scoring can still be enabled separately in special flows

### 4) Shuffle is currently a permutation recovery tool
Do not describe current shuffle as “shuffle then cleanup resolve” unless you change the code.

Current shuffle:
- finds a value permutation with no immediate merge already present
- targets at least 3 valid moves
- applies values directly to existing tiles
- saves immediately as a stable state

### 5) Versus now has a real turn timer
Current code includes:
- per-turn countdown
- default 15 seconds
- timeout-based forced turn advance
- optional pause while board is busy
- persistence of remaining turn time in board state

If you touch versus turn flow, account for the timer.

### 6) Versus is **not** true gravity reversal
Board view rotates for readability, but `ApplyGravityForMode(...)` is still a no-op.

Do not invent separate gravity logic in documentation or AI edits unless you actually implement it.

### 7) `targetValue` does not fully redefine the milestone threshold
Milestone-sensitive code still checks `>= 2048` in multiple places.
If you generalize the milestone, update every dependent path together.

### 8) Theme selection `None / 0` means “all enabled”
Do not break this semantic.

---

## Scene values vs script defaults

Always distinguish between:
- script defaults visible in code
- live inspector values in `SampleScene`

This matters especially for:
- economy values
- timer values
- spawn tuning
- helper thresholds
- testing overrides

When writing docs or making changes, state which one you mean.

---

## Stable-state checklist

Before changing anything, ask:

1. Does this affect what a stable board means?
2. Does it change what gets exported/imported?
3. Does undo still restore a coherent board and score state?
4. Does save/resume still rebuild safely after import?
5. Does rewarded continue still restore the exact earned snapshot before rescue logic?

If yes, update all affected paths together.

---

## Score-change checklist

If you touch scoring, verify all of these:

1. Failed swaps still score `0`
2. Opening-board normalization still behaves as intended
3. Shuffle still behaves as intended
4. Solo and versus both route score correctly
5. Timeout turn-advance in versus does not accidentally score
6. Undo / resume / rewarded-continue restore score eligibility correctly
7. Any new board-side score source respects `ScoreCountingEnabled`

---

## Versus-change checklist

If you touch 1v1 flow, verify all of these:

1. `currentPlayer` stays consistent across export/import
2. `versusTurnRemaining` is saved and restored
3. turn reset happens on normal turn switch
4. timeout handoff does not leave input stuck
5. board rotation and tile label rotation still match the active player
6. HUD timer texts and score texts stay in sync with actual state

---

## Spawn / pacing checklist

If you touch refill or opening generation, verify all of these:

1. The board still starts full
2. Opening normalization still prevents accidental free score
3. The opening still guarantees at least one legal move
4. Early-game tuning still ramps naturally
5. Danger-helper spawn remains subtle and not obviously scripted
6. Generated values still respect `generatedSpawnMaxValue`

---

## Hint-system checklist

If you touch hints:
- keep the authority in `BoardController`
- keep visuals in `CandyTile`
- avoid showing stale hints after board revisions
- respect solo-only behavior if that remains desired
- clear hints when board is busy, over, or being manipulated

---

## Theme / UI guardrails

- `ThemeManager` is the palette authority
- `SettingsUIController` owns theme-family mask editing
- `UIBackgroundController` and `BackgroundController` react to theme family
- `SafeAreaFitter` owns safe-area anchoring and runtime ad inset application

Do not hardcode colors in gameplay scripts unless there is no palette-driven path for the effect.

---

## Service lifetime guardrails

### Persistent
- `AudioManager`
- `MobileAdsManager`

### Scene-owned singleton-style
- `ThemeManager`

Do not assume every manager shares the same lifecycle.

---

## Ads / reward-flow guardrails

Current rewarded paths are explicit and should stay deterministic:
- `LimitedCredits`
- `GameOverShuffle`

Do not couple ad success directly to random gameplay side effects.
Restore exact state first, then apply the intended reward flow.

---

## When you edit code, ask these five questions first

1. Is this a board-rule change, a meta-flow change, or a visual-only change?
2. Does it affect stable checkpoints or persistence?
3. Does it affect solo and versus differently?
4. Does it affect score gating or turn timing?
5. Does documentation need to be updated because an old assumption is now false?

If the answer to the last question is yes, update `PROJECT_CONTEXT` and `AI_CONTEXT` in the same change.
