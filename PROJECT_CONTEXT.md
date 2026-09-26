# PROJECT_CONTEXT

Last updated: 2026-09-26

## Project identity

**Multiply2048** is a mobile, portrait-oriented, full-board drag-swap merge puzzle built in Unity. It is not classic swipe-2048.

The player drags a tile toward one orthogonally adjacent neighbor. A normal swap is legal only when the resulting board contains a valid merge. Accepted moves resolve merges, gravity, refill, and later cascades until the board is stable again.

When this document, code, and Inspector values disagree:

1. use code as the gameplay authority
2. inspect `SampleScene.unity` for serialized overrides
3. remember that unsaved local Inspector changes are not represented in GitHub or scene YAML

---

## Current feature summary

- 8x8 full starting board
- adjacent drag-swap input
- horizontal/vertical 3+ line merges with connected-line unions
- repeated resolve loop with gravity and refill
- Solo and local 1v1 modes
- Free Swap and Shuffle credit economy
- idle hint system
- combo, Great Combo, score popups, VFX, SFX, and hit stop
- palette families and themed backgrounds
- persistent run state
- rewarded ads for empty credits and game-over recovery
- safe-area and adaptive banner support

Removed features:

- Undo is removed and replaced by Free Swap.
- The normal in-game Restart button and Restart code are removed.
- `Play Again` on the Game Over panel remains and starts a new run; it is not the removed in-game Restart feature.

---

## Main ownership boundaries

### `BoardController`

- board dimensions and geometry
- drag input
- adjacency and move validation
- normal swaps and Free Swap behavior
- match/group discovery
- merge, gravity, refill, and stable resolution
- shuffle candidate generation
- hints
- combo-chain and score multiplier rules
- board import/export
- versus board rotation and tile-facing presentation coordination

### `GameManager`

- Solo/Versus mode flow
- menu, HUD, limited-credit, game-over-ad, and game-over panels
- score routing
- Free Swap and Shuffle credits
- offline credit regeneration
- saved-run orchestration
- rewarded-ad flows
- 1v1 timer and timeout handoff

### Presentation and support

- `CandyTile`: tile visuals and motion.
- `ThemeManager`: palette authority.
- `TilePaletteDatabase`: palette data.
- `SettingsUIController`: SFX and hint settings UI. Theme-family selection has been removed.
- `UIBackgroundController`: UI background response to palette family.
- `BackgroundController`: camera-fitted scene background.
- `SafeAreaFitter`: safe-area anchors and runtime ad inset.
- `ThemedGoldButton`: button sprite/size/text helper.
- `ThemedModalCard`: modal overlay/frame styling and optional auto-fit.
- `AudioManager` / `SfxLibrary`: persistent SFX service and sound definitions.
- `MobileAdsManager`: persistent banner/rewarded-ad service.
- merge VFX helpers: `MergeFirework`, `MergeSparkle`, `MergeGhost`, `MergeScorePopup`, `BoardMergeShake`. `MergeSparkle` retains its legacy class/prefab name but now renders semi-transparent tile-colored boxes scattering outward instead of an expanding water wave.
- combo UI helpers: `ComboTurnStats`, `FloatingTextPopup`, `ComboBannerUI`.

---

## Current script defaults

Script defaults may be overridden by the scene.

### Board

| Field | Default |
|---|---:|
| width / height | 8 / 8 |
| spacingRatio | 1.06 |
| swapDuration | 0.09 s |
| dragThresholdInCells | 0.35 |
| targetValue | 2048 |
| spawnPreset | Rare32 |
| useSpawnPresets | true |
| useDynamicSpawnBalancer | true |
| useDangerHelperSpawn | true |
| dangerHelperChance | 0.80 |
| dangerHelperTriggerMoves | 5 |
| dynamicSpawnChance | 0.20 |
| dynamicSpawnStrength | 0.35 |
| useEarlyGameTuning | true |
| earlyGameMoveWindow | 8 |
| openingMinValidMoves | 3 |
| generatedSpawnMaxValue | 64 |
| openingForced16Count | 8 |
| openingForced32Count | 4 |

### Combo

| Field | Default/intended value |
|---|---:|
| comboRewardMergedValue | 2048 |
| comboMultiplierStepSize | 10 |
| comboMultiplierBaseValue | 2 |
| comboMultiplierOnlyForPlayerMove | true |
| resetComboWhenHintUsed | true |
| resetComboOnNonComboMove | true |
| showComboBanner | true |

### Economy and flow

| Field | Default |
|---|---:|
| startingFreeSwapCredits | 10 |
| startingShuffleCredits | 10 |
| creditRegenMinutes | 15 |
| maxCreditsCap | 20 |
| unlimitedFreeSwapForTesting | false |
| unlimitedShuffleForTesting | false |
| gameOverAdOfferSeconds | 5 s |
| versusTurnDurationSeconds | 15 s |
| pauseVersusTimerWhileBoardBusy | true |

---

## Board and merge model

The board is stored as `CandyTile[,]`.

A valid group is:

- a horizontal line of at least three equal values
- a vertical line of at least three equal values
- a connected union of same-value tiles that belong to valid lines

This supports rows, columns, L/T shapes, and crosses.

For original value `v` and group size `n`:

`newValue = v << (n - 1)`

Examples:

- three 2 tiles produce 8
- four 4 tiles produce 32
- five 8 tiles produce 128

One center tile survives with the new value; the other group tiles are removed.

Values reaching at least 2048 are scored as configured, trigger milestone presentation, and are then removed from the grid before refill. The milestone threshold is still hard-coded as `>= 2048` in multiple paths; changing only `targetValue` is insufficient to redefine it.

---

## Normal move flow

1. Pointer down selects a tile.
2. Drag must exceed `cellSize * dragThresholdInCells`.
3. Dominant drag axis selects one orthogonal neighbor.
4. The two grid entries swap and animate.
5. The board searches for valid groups.
6. With no group, a normal move swaps back and scores zero.
7. With a group, scoring is enabled and the board resolves until stable.
8. The stable state is saved and versus can hand off the turn.

Diagonal and non-adjacent swaps are rejected.

---

## Free Swap system

Free Swap replaced Undo completely.

### User flow

1. The Solo player presses `FreeSwapButton`.
2. `GameManager.FreeSwapPressed()` verifies mode and available credit.
3. `BoardController.ArmFreeSwap()` arms one use.
4. The next actual adjacent orthogonal tile swap is executed.
5. One Free Swap credit is consumed at the end of that first swap animation.
6. The power immediately disarms.

### Outcome rules

- The credit is consumed whether the first swap creates a merge or not.
- A non-merging Free Swap remains in its new layout and becomes the new stable state.
- A merging Free Swap runs the normal resolve loop.
- Free Swap always resets the previous combo.
- A merge caused by Free Swap scores at raw x1 and cannot increase combo.
- If credit consumption fails unexpectedly, the board swaps back.
- Small taps, out-of-bounds drags, and missing neighbors do not count as the completed swap attempt.
- Free Swap is Solo-only.

### Migration

Former Undo serialized fields and the `UNDO_CREDITS` PlayerPrefs key are retained only for migration through `FormerlySerializedAs` and credit-loading logic. They do not represent an active Undo system.

Free Swap armed state is transient and is cleared by new game, import, menu pause, shuffle, rewarded recovery, and hard reset.

While Free Swap is armed, the button is non-interactable and keeps showing the normal remaining-credit text; it no longer replaces the label with `READY`.

---

## Merge scatter VFX

- Each source tile in a merge emits small copies of its own tile sprite and exact tile color.
- Box size stays between 12% and 25% of the source tile and uses semi-transparent alpha.
- Normal and 2048+ travel distances/lifetimes preserve the approximate radius and duration of the removed wave effect.
- The VFX is fire-and-forget. Resolve logic never waits for VFX completion, so input becomes available as soon as the board itself is stable while particles may still be visible.

Current saved scene values:

| Field | Normal | 2048+ |
|---|---:|---:|
| box count | 6 | 10 |
| per-piece delay | 0.015 s | 0.012 s |
| lifetime | 0.28 s | 0.48 s |
| travel distance in cells | 0.72 | 1.10 |
| alpha | 0.55 | 0.65 |

Shared size range is 0.12–0.25 of the source tile. `MergeSparkle.prefab` uses fade start 0.35, end-size multiplier 0.55, and maximum rotation 220 degrees. The old `mergeApplyDelay` field has been removed.

---

## Shuffle system

Normal Shuffle is a Solo recovery action:

- verifies/consumes one Shuffle credit
- looks for a value permutation with no immediate merge
- requires at least three valid moves
- applies values to existing tiles
- clears armed Free Swap
- resets combo in the normal player-triggered path
- saves the stable result
- does not award score

Rewarded game-over recovery has a special shuffle path that can preserve the restored combo snapshot.

---

## Scoring and combo

`GameManager.AddScore` applies the incoming amount directly. Combo math lives in `BoardController`.

Scoring is controlled by `ScoreCountingEnabled`. Opening normalization, failed swaps, and Shuffle do not score.

### Combo chain

The first eligible merge move primes the chain. The visible combo is `comboChain - 1`.

For visible combo greater than zero:

`multiplier = comboMultiplierBaseValue + ((comboCount - 1) / comboMultiplierStepSize)`

With the intended 2/10 values:

- Combo x1..x10 => score x2
- Combo x11..x20 => score x3
- Combo x21..x30 => score x4

With `comboMultiplierOnlyForPlayerMove = true`, only the first player-caused merge pass is multiplied. Cascades after gravity/refill still score, but at x1.

### Great Combo

A visible-chain move with at least two separate merge groups in its player pass becomes Great Combo. Its eligible player-pass score receives an additional x2 bonus.

`ComboBannerUI` currently displays either:

- `Combo xN`
- `Great Combo xN`

It receives a multiplier argument but does not include that multiplier in its current text.

### Combo rewards

Each registered 2048+ combo reward grants one Free Swap and one Shuffle credit, capped by `maxCreditsCap`, and can display separate reward popups.

---

## Start, refill, and pacing

A fresh board starts full. It is built, normalized without scoring, and checked for legal moves.

Opening generation uses weighted 2/4/8/16/32 values plus forced 16 and 32 counts. Refill can use spawn presets, dynamic balancing, early-game tuning, and a chance-gated danger helper.

The danger helper is optional, can be Solo-only, and considers low-move board states. Generated values are clamped by `generatedSpawnMaxValue`.

---

## Hint system

`BoardController` owns hint selection and timing. `CandyTile` owns hint animation.

- default idle delay: 10 seconds
- intended Solo-only behavior
- stale hints are invalidated by board revisions
- interaction clears/resets hint timing
- hint use can reset combo and blocks that move from increasing combo

---

## Solo and versus

### Solo

- one score
- Free Swap and Shuffle buttons available
- separate persistent Solo board state

### Versus1v1

- separate player scores
- current player persists
- 15-second turn timer by default
- timeout can hand off the turn
- timer may pause during board resolution
- remaining time persists in `BoardState`
- board and labels rotate for active-player readability

`ApplyGravityForMode(...)` is currently a no-op. Rotation is presentation only; versus does not reverse gravity.

---

## Persistence

Persistent meta state includes:

- total score data, plus weekly max score and weekly max combo
- local-week marker used to reset both weekly records at Monday 00:00 device time
- Free Swap and Shuffle credits
- last credit-regeneration timestamp
- migration version

Separate JSON keys store Solo and Versus board states.

Weekly record reset is fully offline and uses the device's local clock. The first run after installing the feature preserves existing records and records the current Monday. A reset occurs only when a newer Monday is observed, so moving the clock backwards does not repeatedly clear records. Active runs, board saves, total score, credits, and versus scores are not reset.

Player-facing labels are `Weekly Max Score` and `Weekly Max Combo`, including the game-over max-score label.

`BoardState` includes:

- dimensions
- flattened tile values
- current player
- successful move count
- Solo or P1/P2 scores
- remaining versus turn time

Free Swap armed state is not saved. Regular import clears combo and the armed power. Game-over rewarded flow stores `ComboState` separately for exact restoration before rescue logic.

---

## Ads and game-over recovery

Reward flows are explicit:

- `LimitedCredits`
- `GameOverShuffle`

Game-over recovery flow:

1. snapshot stable board, scores, move state, and combo state
2. display timed rewarded offer
3. on reward success, restore exact snapshot
4. perform guaranteed recovery/shuffle logic
5. save the resumed stable state

`MobileAdsManager` can reserve bottom banner space through `SafeAreaFitter`.

---

## Theme and audio

Theme families:

- Dark
- Colorful
- Light

Theme-family selection and its settings mask have been removed. `ThemeManager` automatically selects from every palette remaining in `TilePaletteDatabase` and refreshes tiles. Palette families remain presentation metadata so background helpers can react to the selected palette. `AudioManager` is persistent and stores the SFX toggle under `SFX_ENABLED`.

---

## Current modal and art configuration

This section records the intended current local Editor setup. Save `SampleScene` before pushing so these values become part of the repository.

### Shared panel sprite

`GoldPanel_Blank_1024x1024.png`:

- 1024x1024 RGBA PNG
- Sprite (2D and UI), Single, Full Rect
- PPU 100, mipmaps off, Wrap Clamp
- recommended 9-slice border: L170, R170, T230, B170

On every modal, assign it to the child `Frame > Image`, not the root or overlay. Use white `#FFFFFFFF`, Sliced, Preserve Aspect off, Raycast Target off, PPU Multiplier 1.

### GameOverPanel

- `Card`: centered, 1000x980
- root `ThemedModalCard.autoFitToContent`: false
- `Frame`: full stretch, zero offsets

### GameOverAdPanel

- `Card`: centered, 1000x980
- root auto-fit: false
- `AdTitleText`: top-center, Y -130, 700x70
- `AdDescText`: top-center, Y -230, 700x80
- `AdTimerGroup`: center, Y -20, 660x70
- `AdCloseButton`: bottom-center, X -180, Y 170, 320x100
- `AdWatchButton`: bottom-center, X 180, Y 170, 320x100

### LimitedCreditsPanel

- `Dialog`: centered, 900x1000
- root auto-fit: false
- `ContentSizeFitter`: H/V Unconstrained; Preferred Size previously forced height back to 420
- `VerticalLayoutGroup`: enabled if automatic centering is desired
- recommended group settings: padding L/R40, T/B180; spacing40; Middle Center; Control Child Size W/H on; Force Expand W/H off
- `Frame` and `Inner`: Layout Element Ignore Layout on
- `InfoText`: preferred 600x120
- Watch/Close buttons: preferred 360x110

### SettingsPanel

- sprite is assigned at `Window > Frame`
- auto-fit target: padding 120x110, min 820x900, max 960x1000
- parent ratios: width 0.94, height 0.82
- `TitleText`: top-center Y -100
- `CloseButton`: bottom-center Y 170

### Button art

`ThemedGoldButton` can replace the target Image sprite during `OnEnable`. To change a themed button permanently, set its `normalSprite` and `pressedSprite` fields, or replace the referenced source PNG while preserving the `.meta` file.

---

## Orientation and safe-area target

The game is designed for portrait presentation. Recommended Android Player settings:

- Default Orientation: Portrait, or Auto Rotation with only Portrait enabled
- Landscape Left/Right disabled
- Resizable Activity disabled for strict portrait behavior
- Render Outside Safe Area may remain enabled because `SafeAreaFitter` constrains UI content

Canvas reference design is 1080x1920 with balanced width/height matching. Validate at multiple portrait aspect ratios and with the adaptive banner visible.

---

## Regression checklist

1. Normal invalid swap returns and scores zero.
2. Free Swap’s first actual adjacent swap consumes one credit whether it merges or not.
3. Non-merging Free Swap remains swapped.
4. Merging Free Swap resolves at x1 and resets combo.
5. Shuffle produces no immediate group and at least three moves.
6. Opening normalization awards no score.
7. Combo step multiplier and Great Combo bonus are applied only in intended passes.
8. 2048 combo reward grants both credit types within cap.
9. Solo/Versus scores and turn timer save/restore correctly.
10. Rewarded continue restores its snapshot before recovery.
11. Modal dimensions survive Play Mode; no Content Size Fitter overrides fixed sizes.
12. Merge scatter boxes use the source tile sprite/color, remain at or below 25% size, and do not delay input after the board stabilizes.
13. Armed Free Swap keeps its numeric label and shows the button's disabled state without displaying `READY`.
14. Weekly Max Score and Weekly Max Combo reset on the first check after a newer local Monday.
12. Panel art is on `Frame`, with white tint and Sliced mode.
13. Restart and Undo UI/callbacks do not exist.

