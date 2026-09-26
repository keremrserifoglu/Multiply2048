# AI_CONTEXT

Last updated: 2026-09-22

## Purpose

Use this file as the implementation guardrail for **Multiply2048**.

The central truth is:

> Multiply2048 is a full-board, drag-swap merge puzzle. It is not classic swipe-to-collapse 2048.

Code is the gameplay authority. `SampleScene.unity` inspector values can override script defaults. The UI-layout section near the end records the current intended local Editor setup and must be kept in sync with the saved scene.

---

## Non-negotiable gameplay rules

1. A stable board is normally full.
2. Normal input swaps one tile with one orthogonally adjacent tile.
3. A normal swap is accepted only when the resulting board contains a valid merge group.
4. A failed normal swap animates back and awards no score.
5. Merge groups are horizontal/vertical lines of 3+ equal values; connected valid lines of the same value resolve as one group.
6. A successful move resolves merge, gravity, refill, and later cascades until stable.
7. Stable board checkpoints are used for save/resume and rewarded recovery.
8. Undo has been removed and replaced by the one-use **Free Swap** power.
9. There is no normal in-game Restart button or Restart code path. `Play Again` after game over is a separate flow and remains valid.

---

## Free Swap: current exact behavior

Free Swap is not Undo and must never restore an earlier snapshot.

- Available in Solo only.
- Pressing `FreeSwapButton` calls `GameManager.FreeSwapPressed()`.
- The button arms `BoardController.ArmFreeSwap()`; arming does not immediately spend the credit.
- The next real adjacent, orthogonal, in-bounds tile swap attempt completes the power.
- That first swap consumes exactly one Free Swap credit whether it creates a merge or not.
- If it creates no merge, the swapped layout is kept instead of being reverted.
- If it creates a merge, the normal resolve loop runs.
- Free Swap is disarmed immediately when that swap attempt is processed; it does not wait for a non-merging move.
- A Free Swap always resets the existing combo, including when the swapped tiles produce a valid merge.
- A merge created by Free Swap scores normally but cannot increase the combo and receives no combo multiplier.
- If credit consumption unexpectedly fails, the swap is rolled back.
- Out-of-bounds releases, taps below the drag threshold, and missing-neighbor attempts do not reach `CoTrySwap` and therefore do not consume the armed power.
- Starting/importing a board, pausing for menu, shuffle, rewarded recovery, and hard runtime reset clear the armed flag.

Do not reintroduce snapshot/undo behavior under the Free Swap name.

---

## System ownership

### `BoardController`

Owns:

- drag input and adjacency validation
- normal swap and Free Swap execution
- match detection and merge unions
- gravity and refill
- start-board generation
- shuffle candidate validation
- hints
- combo-chain rules and score multipliers
- board import/export
- board-facing solo/versus presentation

### `GameManager`

Owns:

- mode flow and panels
- score routing and score UI
- Free Swap and Shuffle economy
- legacy Undo-credit migration into Free Swap credits
- persistence orchestration
- limited-credit rewarded flow
- game-over rewarded recovery
- 1v1 turn timer

`GameManager.AddScore` receives the final score amount produced by board rules. Do not duplicate combo math there.

### Presentation-only responsibilities

- `CandyTile`: tile value, palette colors, motion, label rotation, hint visuals.
- `ComboBannerUI`: displays `Combo xN` or `Great Combo xN`; it does not calculate score.
- `ThemedGoldButton`: applies button sprites, optional runtime size, and label layout.
- `ThemedModalCard`: prepares overlay/frame visuals and optionally auto-fits its frame parent.
- `SettingsUIController`: settings UI, SFX toggle, theme mask.
- `UIBackgroundController` / `BackgroundController`: theme-family backgrounds.

### Service lifetimes

- `AudioManager`: persistent singleton.
- `MobileAdsManager`: persistent singleton.
- `ThemeManager`: scene-owned singleton-style authority.

---

## Combo and scoring guardrails

### Score gate

- `ScoreCountingEnabled` is the real scoring gate.
- `PlayerHasMoved` is not a complete scoring rule.
- Opening normalization scores nothing.
- Failed normal swaps score nothing.
- Shuffle scores nothing.
- Successful player moves can score through the full resolve loop.

### Combo multiplier

The first eligible merge move primes the chain and scores at x1. Visible combo count is `comboChain - 1`.

With intended values:

- `comboMultiplierStepSize = 10`
- `comboMultiplierBaseValue = 2`

Formula for visible combo count greater than zero:

`multiplier = comboMultiplierBaseValue + ((comboCount - 1) / comboMultiplierStepSize)`

Therefore Combo x1..x10 scores at x2, x11..x20 at x3, and so on.

When `comboMultiplierOnlyForPlayerMove = true`, only the first merge pass directly caused by the accepted player swap receives the combo multiplier. Later gravity/refill cascades score at x1.

### Great Combo

- A Great Combo requires at least two separate merge groups in the eligible player merge pass after the chain is already visible.
- Great Combo doubles the already combo-weighted score for that player pass.
- `ComboBannerUI` shows `Great Combo xN`; it does not display the score multiplier.

### Combo-breaking actions

- Free Swap always resets the combo.
- A move using an active hint cannot increase combo.
- Hint activation can reset combo when configured.
- Shuffle normally resets combo.
- Non-combo moves reset combo when `resetComboOnNonComboMove` is enabled.

### 2048 combo reward

An eligible merge reaching `comboRewardMergedValue` (intended 2048) registers a reward. Each reward count grants:

- +1 Shuffle credit
- +1 Free Swap credit

Both grants respect the configured credit cap and can spawn separate floating reward popups.

---

## Credit economy guardrails

Current script defaults:

| Field | Value |
|---|---:|
| `startingFreeSwapCredits` | `10` |
| `startingShuffleCredits` | `10` |
| `creditRegenMinutes` | `15` |
| `maxCreditsCap` | `20` |
| `unlimitedFreeSwapForTesting` | `false` |
| `unlimitedShuffleForTesting` | `false` |

`maxCreditsCap = 0` still means no cap, but it is not the current script default.

Legacy compatibility is intentional:

- `[FormerlySerializedAs("startingUndoCredits")]`
- `[FormerlySerializedAs("undoButton")]`
- `[FormerlySerializedAs("unlimitedUndoForTesting")]`
- legacy PlayerPrefs key `UNDO_CREDITS`

These names exist only to migrate old serialized data. Do not interpret them as an active Undo feature.

---

## Shuffle guardrail

Current normal Shuffle:

- is Solo-only
- consumes one Shuffle credit unless testing override is active
- searches for a permutation with no immediate merge
- targets at least three valid moves
- applies values to existing tiles
- resets combo in the normal player-triggered path
- saves the resulting stable board
- does not run an ordinary post-shuffle scoring resolve

Rewarded game-over recovery may request a shuffle while preserving restored combo state; keep that special path distinct from the normal Shuffle button.

---

## Persistence guardrails

`BoardState` contains board dimensions, flattened values, current player, successful move count, scores, and remaining versus turn time.

- Free Swap armed state is transient and is not persisted.
- Regular board import resets combo and clears armed Free Swap.
- Game-over rewarded recovery snapshots `ComboState` separately so the exact pre-offer combo can be restored before recovery.
- Save coherent stable states only.

There is no Undo snapshot flow in the current product.

---

## Versus guardrails

- Separate P1/P2 scores.
- Current player and remaining turn time persist.
- Default turn duration is 15 seconds.
- Timeout can force a turn handoff.
- Timer may pause while the board is busy.
- Board and labels rotate for readability.
- `ApplyGravityForMode(...)` remains a no-op; this is not true gravity reversal.
- Free Swap and normal Shuffle UI actions are Solo-only.

---

## Milestone and theme guardrails

- Multiple paths still treat `>= 2048` as the live milestone threshold.
- Changing `targetValue` alone does not redefine every milestone behavior.
- `ThemeManager` owns palette selection and tile refresh.
- Theme mask `0 / None` means all theme families enabled.
- Do not hardcode gameplay colors when a palette-driven path exists.

---

## UI and layout guardrails

### Custom panel sprite

Current intended panel sprite:

- name: `GoldPanel_Blank_1024x1024.png`
- size: 1024x1024
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Mesh Type: Full Rect
- PPU: 100
- mipmaps off
- Wrap Mode: Clamp
- Image Type on each panel `Frame`: Sliced
- Frame color: `#FFFFFFFF`
- Preserve Aspect: off
- Frame Raycast Target: off
- current recommended Sprite border: Left 170, Right 170, Top 230, Bottom 170

Assign this sprite to the child `Frame` Image, never to the full-screen panel root or modal overlay.

### Fixed modal sizes

Current intended local scene setup:

- `GameOverPanel > Card`: 1000x980, centered; root `ThemedModalCard.autoFitToContent = false`.
- `GameOverAdPanel > Card`: 1000x980, centered; root `ThemedModalCard.autoFitToContent = false`.
- `LimitedCreditsPanel > Dialog`: 900x1000, centered; root auto-fit disabled.

`LimitedCreditsPanel > Dialog` also contains layout components:

- `ContentSizeFitter`: Horizontal and Vertical Fit must be `Unconstrained`; `Preferred Size` forces the height back to 420.
- `VerticalLayoutGroup`: may remain enabled for automatic centering.
- recommended layout: padding L/R 40, T/B 180, spacing 40, Middle Center, Control Child Size W/H on, Child Force Expand W/H off.
- `Frame` and `Inner` must have `LayoutElement.ignoreLayout = true`.

For GameOverAd content in the 1000x980 Card:

- `AdTitleText`: top-center, Y -130, 700x70.
- `AdDescText`: top-center, Y -230, 700x80.
- `AdTimerGroup`: center, Y -20, 660x70.
- `AdCloseButton`: bottom-center, X -180, Y 170, 320x100.
- `AdWatchButton`: bottom-center, X 180, Y 170, 320x100.

### Settings modal

The intended Settings configuration uses its `Window > Frame` child for the sprite. The current tuning target is:

- auto-fit enabled
- padding 120x110
- min size 820x900
- max size 960x1000
- parent width ratio 0.94
- parent height ratio 0.82
- title top-center Y -100
- close button bottom-center Y 170

### Button sprite runtime behavior

`ThemedGoldButton` overwrites its target Image sprite at runtime from `normalSprite` / `pressedSprite`. Changing only the Image `Source Image` is not enough for themed buttons. Update the component sprite fields or replace the referenced PNG while preserving its `.meta` file.

---

## Required verification after edits

### Free Swap

1. Arming alone spends no credit.
2. First actual adjacent swap spends exactly one credit.
3. A non-merging Free Swap stays swapped.
4. A merging Free Swap resolves but gets no combo growth/multiplier.
5. Both forms reset the previous combo.
6. The power disarms after that first swap.
7. Free Swap is unavailable in versus.

### Layout

1. Changes are made outside Play Mode and the scene is saved.
2. Modal root overlays remain transparent/full-screen.
3. Art is assigned only to each `Frame` child.
4. No Content Size Fitter is set to Preferred Size on a manually sized modal.
5. Test portrait aspect ratios and safe areas with the ad banner visible.

### General

1. Failed normal swaps still restore and score zero.
2. Opening normalization still scores zero.
3. Solo and versus score routing remains correct.
4. Save/resume restores a stable board.
5. Rewarded recovery restores the snapshot before rescue logic.
6. Restart and Undo UI/callbacks are not reintroduced.

