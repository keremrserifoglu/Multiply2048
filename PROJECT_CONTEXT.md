# PROJECT_CONTEXT

## Project identity

**Multiply2048** is a **drag-swap merge puzzle**, not a slide-to-collapse 2048 clone.

Core interaction:

1. The board is normally full in stable states.
2. The player drags one tile toward **one orthogonal neighbor**.
3. A swap is accepted **only if it creates at least one valid merge group**.
4. After a valid swap, the board resolves merges, applies gravity, refills empties, and repeats until stable.
5. Stable snapshots are what matter for undo, save/resume, and rewarded-continue restore.

When documentation and code disagree, prefer the code. Unity inspector values in `SampleScene` may override script defaults.

---

## Ownership

### `BoardController`
Owns board rules and simulation:
- board dimensions and geometry
- drag input and swap validation
- match detection and merge resolution
- gravity + refill
- start-board generation
- hint generation
- dynamic spawn balancing / danger-helper logic
- undo snapshots and board import/export
- solo / versus presentation details on the board

### `GameManager`
Owns game flow and meta systems:
- mode entry (`Solo`, `Versus1v1`)
- menu / HUD / game over panel flow
- score state and score UI
- undo / shuffle credit economy
- offline credit regeneration
- saved-run persistence
- rewarded-ad continue flow
- 1v1 turn timer and timeout turn handoff

### `CandyTile`
Owns per-tile presentation only:
- numeric value text
- tile/text colors from `ThemeManager`
- world movement animation
- label rotation for solo / versus readability
- idle-hint visuals

It is not the source of truth for gameplay rules.

### `ThemeManager`
Owns palette selection and palette-driven refresh:
- loads `TilePaletteDatabase`
- interprets allowed theme families from PlayerPrefs
- picks / rotates palettes
- refreshes all tiles and broadcasts `OnPaletteChanged`

### Persistent service singletons
- `AudioManager`: persistent SFX playback + PlayerPrefs-backed SFX setting
- `MobileAdsManager`: persistent AdMob wrapper for banner and rewarded ads

### Support / presentation scripts
- `SettingsUIController`: settings panel, SFX toggle, theme-family mask
- `UIBackgroundController`: palette-family-driven UI background art/tint and modal overlay
- `BackgroundController`: camera-fitted scene background sprite per palette family
- `SafeAreaFitter`: safe-area anchoring plus runtime ad insets
- `ThemedGoldButton`, `ThemedModalCard`: themed UI helpers
- `MergeFirework`, `MergeSparkle`, `MergeGhost`: merge VFX helpers

---

## Script defaults visible in code

These are script-side defaults, not guaranteed live scene values.

### `BoardController`
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
- `useEarlyGameTuning = true`
- `earlyGameMoveWindow = 8`
- `openingMinValidMoves = 3`
- `dangerHelperUnlockMove = 4`
- `generatedSpawnMaxValue = 64`
- `openingForced16Count = 8`
- `openingForced32Count = 4`

### `GameManager`
- `startingUndoCredits = 10`
- `startingShuffleCredits = 10`
- `creditRegenMinutes = 15`
- `gameOverAdOfferSeconds = 5f`
- `maxCreditsCap = 0`
- `versusTurnDurationSeconds = 15f`
- `pauseVersusTimerWhileBoardBusy = true`
- testing defaults currently leave `unlimitedUndoForTesting` and `unlimitedShuffleForTesting` enabled in code

---

## Board model and stable-state assumptions

The board is a rectangular `CandyTile[,]` grid.

A stable board should satisfy:
- every intended occupied cell contains exactly one tile
- there are no pending gravity/refill steps left to resolve
- the board has at least one valid move unless the run is truly over
- exported/imported states should represent coherent post-resolve checkpoints

`BoardState` currently carries:
- width / height
- flattened tile values
- current player
- `successfulMoves`
- solo score or versus scores
- `versusTurnRemaining`

---

## Input and move validation

Input is drag-based.

Current rules:
- pointer down picks one tile
- drag direction resolves to the dominant orthogonal axis
- drag must exceed `cellSize * dragThresholdInCells`
- only adjacent orthogonal swaps are attempted
- diagonals are invalid

A move is valid only if the post-swap board contains at least one merge group. If not:
- the swap animates back
- board state returns to pre-swap layout
- undo is not committed
- score is not awarded

---

## Match / merge rule

The merge system is **line-based with connected same-value unions**.

Valid groups:
- horizontal line of 3+ equal values
- vertical line of 3+ equal values
- connected intersections of valid same-value lines

So these can resolve as one group when connected through valid line membership:
- 3 in a row
- 4 or 5 in a row
- L shapes
- T shapes
- plus / cross shapes

### Merge result value
For group size `n` with original value `v`:

`newValue = v << (n - 1)`

Examples:
- `2 + 2 + 2 -> 8`
- four `4`s -> `32`
- five `8`s -> `128`

Only one survivor tile remains. Others are removed.

### Milestone behavior
`>= 2048` is still the live milestone threshold.

Important implications:
- board-side milestone removal is still tied to `>= 2048`
- `ThemeManager.NotifyValueCreated(int value)` also only reacts when `value >= 2048`
- changing `targetValue` alone does **not** fully redefine milestone logic

---

## Resolve loop and scoring

After a successful move, the board resolves repeatedly until no groups remain:

1. detect groups
2. merge them
3. score allowed merges
4. remove milestone survivors when needed
5. apply gravity
6. refill empties
7. repeat until stable

### Current scoring model
This is important and easy to mis-document:

- `GameManager.AddScore` currently applies the **raw incoming amount**. There is **no x2 multiplier** in the current script.
- Score is gated by `GameManager.ScoreCountingEnabled`, not just `PlayerHasMoved`.
- On a fresh run / restart, score counting is disabled.
- On the player’s **first successful move**, `BoardController` enables score counting and resolves with `scoreAllPasses: true`.
- That means all merge passes created by that successful player move currently count.
- Opening-board normalization resolves with **no score**.
- Failed swaps never score.
- Shuffle is not a scoring action.
- Milestone cascade score can still be allowed explicitly in special flows.

Treat `PlayerHasMoved` mainly as a flow/UI flag. Treat `ScoreCountingEnabled` as the real scoring gate.

---

## Start-board generation and pacing systems

### Fresh board creation
`CoStartNewGame` currently:
1. clears runtime state
2. sets player 1 / solo baseline visuals
3. builds a fully populated opening board
4. runs a no-score normalization resolve
5. ensures at least one valid move exists
6. resets the versus timer when needed

### Opening seeded values
Opening boards are not created by a tiny classic 2048 seed. The board starts full.

Current opening generation includes:
- forced counts of `16` and `32`
- remaining cells filled from weighted opening values
- generated values clamped by `generatedSpawnMaxValue`

Current opening-weight fields exposed in code:
- 2
- 4
- 8
- 16
- 32
- internal picker also includes 64 in opening weighting

### Early-game tuning
There is a real early-game pacing layer:
- stronger low-value refill bias early on
- stricter opening move-count target
- danger helper is locked until a configurable move threshold
- dynamic spawn balancing is damped early and ramps toward normal behavior

---

## Refill and spawn rules

Refill is not a single hardcoded random choice.

### Spawn presets
Known presets:
- `ClassicHard`
- `Balanced`
- `Rare32`

Script default is `Rare32`.

### Dynamic spawn balancing
When enabled, refill weights are adjusted using board state. The system estimates board strength from average tile exponent and nudges weights without fully scripting outcomes.

### Danger-helper spawn
When enabled, the board may deliberately inject a value that helps avoid dead states.

Current behavior:
- chance-gated
- optional solo-only restriction
- only considered when valid moves are low
- examines nearby candidate values and evaluates merge potential around the spawn cell

---

## Hint system

The board has an idle hint system.

Key points:
- hinting is board-owned, not UI-owned
- can be disabled at runtime
- defaults to enabled
- can be restricted to solo mode
- waits for idle delay before showing
- respects stable-board revisions so it does not keep recomputing the same board
- highlights all tiles participating in currently legal swaps it finds

`CandyTile` owns the hint pulse / shimmer presentation, but `BoardController` decides when and what to hint.

---

## Undo and shuffle

### Undo
Undo is based on stable checkpoints:
- snapshot is captured before a candidate move
- snapshot is committed only after the move is accepted
- undo restores board plus relevant score state
- undo does not rewind into mid-animation states

### Shuffle
Current shuffle behavior is simpler than some older docs imply:
- solo-only via `GameManager`
- consumes a shuffle credit unless testing override is active
- does **not** perform a normal resolve pass after shuffling
- searches for a shuffled value arrangement with **no immediate merge already present** and at least **3 valid moves**
- applies the chosen value permutation directly to existing tiles
- saves the new stable state immediately

So current shuffle is a board recovery permutation, not a “resolve cleanup” action.

---

## Solo vs 1v1 mode

### Solo
- one score
- undo and shuffle exposed
- solo save slot used

### Versus (`Versus1v1`)
- separate player 1 / player 2 scores
- current player is persisted in board state
- board view rotates 180° between turns for readability
- tile labels rotate with the board view
- successful resolving move switches turn
- there is a **15-second turn timer by default**
- if timer reaches zero, turn is force-advanced without a move
- timer can be paused while the board is busy resolving
- versus timer state is persisted in `BoardState`

### Gravity note
`ApplyGravityForMode(GameManager.PlayType playType)` is currently a **no-op**.
The board still resolves “downward” visually for all modes.

This is presentation rotation, not true gravity reversal.

---

## Persistence and save model

`GameManager` persists both meta progress and resumable run state.

### Meta / economy keys
- total score
- max score
- undo credits
- shuffle credits
- last credit grant time
- one-time score reset migration version

### Run-state persistence
Separate save keys exist for:
- solo board state JSON
- versus board state JSON

### Save checkpoints
Current intent:
- save stable board states only
- save when leaving to menu, pausing, quitting, or after successful stable board updates
- clear persistent state when a run is truly over
- rewarded continue restores an exact snapshot first, then runs recovery logic

---

## Game over and rewarded continue

`GameManager` supports a short rewarded-ad offer before final game over.

Current flow:
1. board reports game over
2. manager snapshots board + relevant score state + move state
3. ad-offer panel can appear for a limited window
4. if rewarded ad succeeds, snapshot is restored
5. board resumes and recovery logic runs
6. stable resumed state is saved again

Rewarded recovery currently prefers shuffle-based rescue, with fallback board rebuilding if needed.

---

## Credit economy

Undo and shuffle are credit-gated unless testing overrides are enabled.

Current behavior includes:
- starting credits from script defaults or inspector overrides
- offline time-based regeneration using UTC timestamps
- optional cap through `maxCreditsCap`
- rewarded-ad credit grant when empty
- corrupted-value sanity reset path

Because `maxCreditsCap = 0` means “no cap”, inspector values should always be checked before making economy assumptions.

---

## Theme system

Themes are palette-driven.

### Theme families
Supported families:
- Dark
- Colorful
- Light

### Selection semantics
`SettingsUIController` stores a bitmask in PlayerPrefs.

Important rule:
- stored `0` / `None` means **all theme families enabled**, not “disable all themes”

### ThemeManager behavior
- loads `TilePaletteDatabase` from `Resources` if missing
- chooses a palette from allowed families
- can force a different palette on milestone creation
- refreshes all `CandyTile` colors on palette change
- broadcasts `OnPaletteChanged` so UI/background helpers can react

`UIBackgroundController` and `BackgroundController` both derive their visuals from the current palette family.

---

## Audio system

`AudioManager` is a persistent singleton.

Current characteristics:
- `DontDestroyOnLoad`
- stores SFX enabled state under `SFX_ENABLED`
- supports one-shot and layered playback
- uses `SfxLibrary` entries with clip arrays, volume, and pitch jitter
- does not play when SFX is disabled

---

## Ads and safe area

`MobileAdsManager` is a persistent AdMob wrapper.

Current characteristics:
- initializes the SDK on start by default
- loads bottom banner and rewarded ads
- exposes reward flows for `LimitedCredits` and `GameOverShuffle`
- can reserve banner space by pushing extra bottom inset into `SafeAreaFitter`

`SafeAreaFitter` is the runtime authority for safe-area anchoring and extra ad insets.

---

## Implementation cautions worth preserving

1. Do not move board rules into UI scripts.
2. Do not assume `targetValue` fully controls milestone behavior.
3. Do not reintroduce an old “x2 score multiplier” assumption; current code does not apply one.
4. Do not describe shuffle as a post-shuffle resolve unless the code is changed back to that behavior.
5. Do not describe versus as true gravity reversal.
6. When adding new board-side features, update export/import if the feature affects resumable run state.
7. When adding turn-based versus features, account for timer reset, timeout handoff, and persisted timer state.
