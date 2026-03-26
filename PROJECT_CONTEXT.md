# PROJECT_CONTEXT

## Project identity

**Multiply2048** is a **drag-swap merge puzzle**, not a slide-to-collapse 2048 clone.

The core interaction is:

1. The board is normally full.
2. The player drags a tile toward one orthogonal neighbor.
3. The swap is accepted **only if it creates at least one valid merge group**.
4. After a valid swap, the board resolves merges, refills empty cells, and continues resolving until stable.

This document reflects the codebase as currently implemented in the reviewed scripts.  
When in doubt, prefer **script behavior** over old assumptions, and remember that **Unity inspector values in `SampleScene` can override script defaults**.

---

## High-level ownership

### `BoardController`
Owns the board simulation and most game rules:

- Board dimensions and cell layout
- Input hit-testing and drag-to-swap behavior
- Swap validation
- Group detection and merge resolution
- Refill / spawn rules
- Undo snapshots
- Board import / export
- Solo / versus board presentation details

### `GameManager`
Owns game flow, meta systems, and persistence:

- Main menu vs in-game flow
- Solo and 1v1 mode entry
- Score state and UI text
- Undo / shuffle credit economy
- Offline credit regeneration
- Saved-run persistence
- Game-over flow
- Rewarded continue flow integration
- Buttons / modal orchestration

### `CandyTile`
Owns per-tile visual state:

- Grid coordinates (`x`, `y`)
- Numeric value display
- Theme-driven tile color / text color refresh
- Position animation
- Versus label rotation support

It should stay lightweight. It is not the source of truth for board rules.

### `ThemeManager`
Owns runtime theme selection and palette switching:

- Loads a `TilePaletteDatabase`
- Tracks allowed theme families from settings
- Chooses / rotates palettes
- Refreshes tiles and broadcasts `OnPaletteChanged`

### `AudioManager`
Persistent SFX singleton:

- Stores SFX enabled flag in PlayerPrefs
- Plays one-shot and layered merge/game/menu sounds
- Uses `SfxLibrary` entries for clip pools, volume, and pitch jitter

### `MobileAdsManager`
Persistent ad singleton:

- Banner lifecycle
- Rewarded-ad lifecycle
- Reward flow dispatch (`LimitedCredits`, `GameOverShuffle`)
- Safe-area-aware bottom banner layout

### Presentation / support scripts
- `SettingsUIController`: settings UI, SFX toggle, theme family toggle mask
- `UIBackgroundController`: menu/HUD/modal/button visual theming
- `BackgroundController`: visual background sprite + camera fitting
- `SafeAreaFitter`: safe-area anchoring and extra inset support
- `ThemedGoldButton`, `ThemedModalCard`: UI skinning helpers
- `MergeFirework`, `MergeSparkle`, `MergeGhost`: merge VFX helpers
- `ColorThemeManager`: legacy / compatibility visual helper unless proven otherwise by a new scene workflow

---

## Script defaults currently visible in code

These are **script-side defaults**, not guaranteed live scene values:

### Board defaults (`BoardController`)
- `width = 8`
- `height = 8`
- `spacingRatio = 1.06f`
- `swapDuration = 0.18f`
- `dragThresholdInCells = 0.35f`
- `targetValue = 2048`
- `spawnPreset = Rare32`
- `useSpawnPresets = true`
- `useDynamicSpawnBalancer = true`
- `useDangerHelperSpawn = true`
- `dangerHelperChance = 0.80f`
- `dangerHelperTriggerMoves = 5`
- `helperSpawnSoloOnly = true`
- `dynamicSpawnChance = 0.20f`
- `dynamicSpawnStrength = 0.35f`

### Meta defaults (`GameManager`)
- `startingUndoCredits = 10`
- `startingShuffleCredits = 10`
- `creditRegenMinutes = 15`
- `gameOverAdOfferSeconds = 5f`
- `maxCreditsCap = 0` (script default; treat as scene-overridable)
- internal hard clamp constant: `MAX_POWERUPS = 50`

---

## Board model

The board is represented as a full rectangular grid of `CandyTile` references indexed by `[x, y]`.

Stable states should follow this expectation:

- Every occupied cell contains exactly one `CandyTile`
- No unresolved empties remain after refill/resolve completes
- There is at least one valid move available unless the run is truly over

Coordinates live in board space; tile GameObjects are the visual carriers of that state.

---

## Input model

Input is drag-based, not tap-then-tap.

Current behavior:

- Pointer down selects a tile if the board is interactive
- Drag direction is resolved to the dominant orthogonal axis
- A drag must exceed `cellSize * dragThresholdInCells`
- Only adjacent orthogonal swaps are attempted
- Diagonal swaps are never valid

A swap animation can happen visually, but the move is only committed if the post-swap board forms at least one valid merge group involving the affected area.

---

## Valid move rule

A move is valid only if swapping two adjacent tiles produces one or more merge groups.

If no group is created:

- the tiles animate back
- the board returns to its pre-swap state
- the move is rejected
- the pending undo snapshot is not committed as a successful move state

This rule is critical to the feel of the game and should not be loosened casually.

---

## Group detection and merge rule

The merge logic is **line-based**, not flood-fill-only:

- Horizontal lines of length **3 or more** of the same value form a group
- Vertical lines of length **3 or more** of the same value form a group
- Intersecting / connected valid lines of the same value resolve as one combined group

This means shapes like:

- 3 in a row
- 4 or 5 in a row
- T-shapes
- L-shapes
- plus/cross shapes

can resolve as a single same-value merge group when connected through valid horizontal/vertical line membership.

### Merge output value
For a resolved group of size `n` with original value `v`, the resulting tile value is:

`v << (n - 1)`

Examples:
- `2 + 2 + 2` (group size 3) -> `8`
- four `4`s (group size 4) -> `32`
- five `8`s (group size 5) -> `128`

Only one survivor tile remains for that group; the rest are removed.

---

## Resolve loop

After a successful move, the board enters a resolve cycle until it becomes stable.

Conceptually:

1. Detect all valid groups
2. Merge them
3. Award score when scoring is enabled for that resolve pass
4. Remove milestone tiles if applicable
5. Refill empties
6. Check again for new groups
7. Repeat until no groups remain

### Important scoring nuance
The project’s established rule is that the **player-triggered resolution is the scoring event**.  
Cascades created by refill or cleanup should not turn the game into a free infinite combo machine.

When changing resolve behavior, preserve the intended distinction between:
- **player-earned first-pass scoring**
- non-abusive follow-up stabilization / cleanup

### Shuffle nuance
The current shuffle flow explicitly resolves with:

- `scoreThisResolve: false`
- `animate: true`
- `allowMilestoneCascadeScore: true`

So shuffle cleanup is not a normal scoring move, but milestone-related cascade handling is still allowed by the current implementation. Preserve this unless intentionally redesigning shuffle rewards.

---

## Start board generation

A fresh run is not created by blindly dropping random tiles once.

`BuildFreshStartBoard()` currently:

1. Clears board state
2. Fills the grid with weighted start values
3. Runs a no-score, no-animation normalization resolve
4. Ensures at least one valid move exists
5. Retries up to a capped number of attempts if needed

### Weighted start values
The helper for weighted starting cells currently uses values below the 2048 milestone, including:

- 2
- 4
- 8
- 16
- 32
- 64
- 128
- 256

with higher weights on lower values.

This means the opening board is intentionally richer than classic 2048 starts.

---

## Refill / spawn rules

The code currently supports spawn presets and helper logic rather than a single hardcoded random rule.

### Spawn presets
Known presets:

- `ClassicHard`
- `Balanced`
- `Rare32`

Current script default is `Rare32`.

### Preset intent
- `ClassicHard`: low-value focused
- `Balanced`: broader but still conservative
- `Rare32`: mostly low values with rare higher-value spice

For `Rare32`, the reviewed code currently biases strongly toward:
- mostly `2`
- some `4`
- fewer `8`
- rare `16`
- extremely rare `32`

### Dynamic spawn balancer
When enabled, the board can bias spawns based on current state rather than using only a static preset distribution.

### Danger-helper spawn
When enabled, the board can deliberately help avoid dead states.

Current code behavior:
- only considered when the board is low on valid moves
- uses a chance gate
- can be restricted to solo mode
- evaluates nearby board values and potential horizontal/vertical opportunities

This system is meant to improve survivability without making the board feel scripted every turn.

---

## Undo system

Undo is designed around **stable board checkpoints**, not arbitrary mid-animation rewinds.

Key intent:
- Save the board before a candidate move
- Commit undo only when the move is actually accepted
- After a successful resolve, save the resulting stable state for persistence
- Undo should return to a coherent playable board, not a half-resolved state

`GameManager` exposes undo credits and the public UI flow, but `BoardController` owns the board snapshot data.

After undo, `PlayerHasMoved` is reset so the restored state behaves like a proper earlier turn.

---

## Shuffle system

Shuffle is a board-level recovery tool, not a free scoring exploit.

Current intent:
- consumes a shuffle credit through `GameManager`
- saves an undo snapshot before changing the board
- attempts to permute the board into a playable configuration
- tries to guarantee a minimum number of valid moves
- performs a cleanup resolve without normal move scoring
- saves the new stable state

The current implementation targets **at least 3 valid moves** and caps reshuffle attempts.

---

## Milestone / 2048 rule

The current project logic still treats **2048 and above** as a milestone threshold.

Important implications:

- `BoardController` exposes a `targetValue` field, but milestone-related behavior is still effectively tied to `>= 2048` in the reviewed flow
- `ThemeManager.NotifyValueCreated(int value)` also changes theme only when `value >= 2048`

So changing `targetValue` alone does **not** fully redefine the game’s milestone logic.

If milestone behavior is ever generalized, update both board logic and theme logic together.

---

## Solo vs 1v1 (versus) mode

`GameManager.PlayType` currently supports:

- `Solo`
- `Versus1v1`

### Solo
- one score
- one persistent solo board save slot
- undo and shuffle buttons are exposed through the solo flow

### Versus
- separate per-player scores
- separate versus save slot
- current player is tracked in exported/imported board state
- label rotation / presentation can change for readability
- turn switching happens after a successful resolving move

### Gravity note
`ApplyGravityForMode(GameManager.PlayType playType)` is currently a **no-op** with a comment indicating the board is visually “always down” for now.

That means there is **not** currently a real per-player gravity reversal system in the reviewed code, even if the presentation suggests player perspective differences.

---

## Scoring

`GameManager.AddScore(long amount, bool ignorePlayerMovedCheck = false)` currently multiplies incoming board score by **2** before applying it.

That multiplier is part of the current game economy and should not be accidentally removed.

When maintaining scoring, keep these layers distinct:

- board-side raw merge score calculation
- GameManager-side score routing
- current x2 multiplier
- solo vs current-player routing in versus

---

## Persistence

`GameManager` persists both meta progress and resumable runs.

### Meta / economy keys
- total score
- max score
- undo credits
- shuffle credits
- last credit grant timestamp

### Run state keys
- solo board state JSON
- versus board state JSON

### Board export/import
`BoardController.ExportState()` includes:
- width
- height
- current player
- flattened tile values
- relevant score fields

`ImportState()` rebuilds the board, snaps visuals, then normalizes the board into a coherent playable state.

Never assume a partially resolved board is safe to persist.

---

## Credit economy

Undo and shuffle are credit-gated unless testing overrides are enabled in scene configuration.

Current behavior includes:
- starting credits on first boot / recovery
- offline time-based regeneration
- limited-credits modal when the player is empty
- rewarded ad flow to regain or use power resources
- a cap path that can be scene-configured

Because script default `maxCreditsCap` is `0`, always verify the live inspector value before making economy assumptions based on a running build.

---

## Rewarded continue / ads

`MobileAdsManager.RewardFlow` currently defines:
- `LimitedCredits`
- `GameOverShuffle`

The intended game-over continue contract is:

1. Snapshot the dead-end board and score state
2. Offer rewarded continuation for a short window
3. If accepted and ad succeeds, restore the saved state
4. Perform recovery logic (currently tied to shuffle-style rescue)
5. Save the resumed stable run again

This flow must stay deterministic and restore the exact run context the player earned before death.

---

## Theme system

Themes are palette-driven, not just color-swaps on a few UI elements.

### Theme families
The reviewed code supports:
- Dark
- Colorful
- Light

### Selection mask
`SettingsUIController` stores a bitmask in PlayerPrefs.

Important behavior:
- stored `None / 0` does **not** mean “no themes”
- it is treated as **all themes enabled**

### ThemeManager behavior
- loads `TilePaletteDatabase` if missing
- resets / picks a palette on start and on settings changes
- refreshes all tiles on palette changes
- switches to a different palette when a tile value reaches the milestone threshold (`>= 2048`)

### Tile colors
`CandyTile.RefreshColor()` pulls tile and text colors from the active theme path, not from hardcoded per-tile colors only.

---

## Audio system

`AudioManager` is a persistent singleton and uses PlayerPrefs key `SFX_ENABLED`.

Key points:
- SFX can be toggled from settings
- merge sounds are layered / varied through `SfxLibrary`
- game-over and menu-select sounds are part of the current library
- audio state should remain stable across scene reloads

---

## Safe area and responsive layout

`SafeAreaFitter` watches:
- `Screen.safeArea`
- screen size
- orientation

It can also add extra inset pixels on each edge before applying anchors.

Use this instead of ad-hoc anchor hacks when touching mobile UI layout, especially for:
- banner placement
- bottom controls
- notches / rounded-corner phones

---

## Visual systems that are intentionally lightweight

### `CandyTile` animation caveat
The currently reviewed `CandyTile` code keeps some pop / flash behavior intentionally muted or effectively disabled.

Do not assume missing scale-bounce or heavy flash behavior is a bug.  
It may be an intentional choice to keep the board readable and calm.

### Background controllers
`BackgroundController` and `UIBackgroundController` are presentation-only helpers.  
They should not accumulate gameplay logic.

---

## Practical maintenance rules

When editing the project, prefer these constraints:

1. Keep **board rules** in `BoardController`
2. Keep **meta flow / credits / persistence / panels** in `GameManager`
3. Keep `CandyTile` mostly visual
4. Do not make shuffle a scoring exploit
5. Do not weaken the “swap must create a merge” rule without an explicit redesign
6. Do not assume `targetValue` already fully owns milestone behavior
7. Do not invent gravity-flip mechanics unless the board logic is truly updated to support them
8. Treat script defaults and scene inspector overrides as separate sources of truth
9. Save and restore only coherent board states
10. Preserve solo and versus persistence separation

---

## Fast mental model

If you need one sentence:

> **Multiply2048 is a full-board drag-swap merge puzzle where only merge-producing swaps are legal, the board resolves into stable playable states, and `GameManager` wraps that simulation with persistence, credits, ads, themes, and UI flow.**
