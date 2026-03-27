# AI_CONTEXT

## Purpose

Use this file as an implementation guardrail when editing **Multiply2048**.

The most important idea:

> This project is **not** classic swipe-2048. It is a **drag-swap merge puzzle** built around a full board, legal-swap validation, controlled resolution, and stable-state persistence.

---

## Non-negotiable gameplay truths

1. The board is normally **full** in stable states.
2. The player swaps with **one adjacent orthogonal neighbor**.
3. A swap is legal **only if it creates at least one valid merge group**.
4. Merge groups are based on **horizontal/vertical lines of 3+ equal values**, and connected line-members of the same value can resolve together as one group.
5. The board resolves to a **stable** state after each successful move.
6. Undo / save / resume should target **stable checkpoints**, not mid-animation or half-resolved states.

---

## Ownership guardrails

### Put logic in the right place
- **`BoardController`**: board rules, swap validation, merge resolution, refill, board import/export, undo snapshots
- **`GameManager`**: mode flow, score UI, meta score, undo/shuffle credits, persistence, game over, rewarded continue, menu/panel orchestration
- **`CandyTile`**: visual tile state only
- **`ThemeManager`**: palette selection and theme refresh
- **`AudioManager` / `MobileAdsManager`**: persistent service singletons
- **UI/background helper scripts**: presentation, not gameplay authority

Do not move core board rules into UI scripts.

---

## Scene values vs script defaults

Be careful:

- Script field defaults are visible in code
- Unity inspector values in `SampleScene` may override them

So when documenting or changing behavior:
- call out whether you mean **script default** or **scene-configured live value**
- do not silently replace one with the other

This matters especially for economy, spawn tuning, spacing, and helper thresholds.

---

## Current implementation facts worth preserving

### Board defaults visible in code
- 8x8 board
- `spacingRatio = 1.06f`
- `swapDuration = 0.18f`
- `dragThresholdInCells = 0.35f`
- default spawn preset = `Rare32`
- dynamic spawn balancer enabled by default
- danger-helper spawn enabled by default
- `dangerHelperTriggerMoves = 5`
- `targetValue = 2048`

### Meta defaults visible in code
- starting undo credits = 10
- starting shuffle credits = 10
- credit regen = 15 minutes
- game-over ad offer window = 5 seconds
- script default `maxCreditsCap = 0` (scene may override)

---

## Scoring guardrails

Preserve the distinction between:
- **raw board score generation**
- **GameManager score routing**
- **current x2 multiplier in `GameManager.AddScore`**

Right now `GameManager.AddScore(...)` doubles the incoming amount before applying it.

Do not accidentally:
- remove the x2 multiplier
- score failed swaps
- turn shuffle into a normal scoring move
- create runaway cascade abuse without explicitly redesigning the scoring model

---

## Shuffle / resolve nuance

Current shuffle flow matters:

- it saves an undo snapshot first
- it attempts to create a playable board with multiple valid moves
- it resolves with:
  - `scoreThisResolve: false`
  - `animate: true`
  - `allowMilestoneCascadeScore: true`

So shuffle cleanup is intentionally **not** a normal player-scored move.

Preserve that intent unless the design is deliberately changing.

---

## Milestone / targetValue caution

Do **not** assume `targetValue` fully defines the milestone threshold yet.

Current reviewed behavior still contains logic tied to **`>= 2048`**:
- board milestone/removal paths
- theme change trigger in `ThemeManager.NotifyValueCreated`

If you generalize the target threshold, update every dependent path together.

---

## Versus-mode caution

Do **not** invent gravity reversal behavior.

`ApplyGravityForMode(GameManager.PlayType playType)` is currently a **no-op**.  
The board is effectively “always down” in the reviewed implementation.

Versus currently means:
- separate score routing
- current-player tracking
- turn switching after successful resolve
- presentation differences like label rotation

Not a fully separate gravity simulation.

---

## Theme-system caution

`SettingsUIController` stores a theme-family bitmask.

Important:
- stored `None` / `0` means **all theme families enabled**, not “disable themes”

`ThemeManager`:
- is scene-scoped singleton-style (`ThemeManager.I`)
- refreshes tiles/UI through palette changes
- changes palette when a created tile reaches the milestone threshold (`>= 2048`)

Do not break the mask semantics when editing settings.

---

## Service lifetime caution

### Persistent across scene reloads
- `AudioManager`
- `MobileAdsManager`

### Not guaranteed persistent service
- `ThemeManager` is singleton-style but should be treated as scene-owned unless you explicitly add persistence

Do not assume all managers share the same lifetime model.

---

## Tile-visual caution

`CandyTile` currently keeps some pop / merge-flash behavior intentionally minimal or effectively disabled.

Do not “fix” that automatically unless there is an explicit visual-design request.  
Board readability is more important than adding noisy animation.

---

## Persistence guardrails

Preserve these rules:

1. Save solo and versus runs separately
2. Save coherent board states only
3. Undo should restore a real playable checkpoint
4. Resume should normalize safely after import
5. Rewarded continue must restore the exact saved dead-end context before rescue logic runs

If you change save data shape, update:
- export
- import
- migration / fallback handling
- solo and versus code paths
- game-over continue snapshot restore

---

## Ads / reward-flow guardrails

Current reward flows:
- `LimitedCredits`
- `GameOverShuffle`

Do not couple ad success directly to arbitrary gameplay side effects.  
Route rewards through explicit flow handling so failure, retry, and restore behavior remain deterministic.

---

## When making code changes, use this checklist

Before editing, ask:

1. **Who should own this change?**
   - Board rule?
   - Meta flow?
   - Visual-only?
   - Theme?
   - Ads/audio service?

2. **Does it affect stable-state assumptions?**
   - save/load
   - undo
   - game over
   - resume
   - rewarded continue

3. **Does it affect legal-move validation?**
   - only merge-producing swaps should remain legal unless explicitly redesigned

4. **Does it affect scoring?**
   - keep the first-pass / non-exploit intent intact

5. **Does it affect milestone behavior?**
   - remember 2048 is still effectively hardwired in more than one place

6. **Could scene inspector values override this?**
   - document whether you changed code defaults or live scene configuration

---

## Preferred response style for future AI edits

When proposing or applying changes:

- state assumptions clearly
- separate **verified code behavior** from **likely scene override**
- avoid broad refactors when a local fix is enough
- preserve existing architecture unless there is a strong reason to restructure
- mention cross-file follow-ups when a change touches milestone logic, persistence, or scoring

---

## One-line reminder

> Treat Multiply2048 as a stable-state, legal-swap, board-first puzzle system wrapped by GameManager-driven economy, persistence, UI flow, and rewarded recovery.
