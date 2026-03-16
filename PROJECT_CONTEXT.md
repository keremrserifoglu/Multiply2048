# Multiply 2048 — Project Context

This document describes the current gameplay rules, architecture, runtime flow, and scene-level behavior of the **Multiply 2048** project based on the reviewed scripts and scene data.

It is intended for:
- developers joining the project
- AI assistants modifying the repository
- anyone who needs a quick but accurate mental model before changing gameplay code

---

## 1. Project identity

**Project name:** Multiply 2048  
**Engine:** Unity  
**Primary scene:** `Assets/Scenes/SampleScene.unity`  
**Genre:** grid-based drag-swap merge puzzle

This is **not** classic slide-based 2048.

Core identity:
- the board is expected to be full in stable gameplay states
- the player swaps **adjacent tiles** by dragging
- a swap is only accepted if it creates at least one merge group
- merge groups are formed by **3 or more equal values** in horizontal or vertical lines
- cross / T / connected line structures are merged as a single connected group
- after a valid move, the board resolves merges, gravity, refill, and cascades until stable

---

## 2. Current scene configuration

`SampleScene.unity` currently configures the main gameplay board like this:
- board size: **8 x 8**
- spacing ratio: **1.08**
- swap duration: **0.18s**
- drag threshold: **0.35 cells**
- spawn preset: **Rare32**
- dynamic spawn balancer: **enabled**
- danger-helper refill: **enabled**
- danger-helper trigger threshold: **3 valid moves or fewer**
- target value field: **2048**

`GameManager` is currently configured with:
- starting undo credits: **10**
- starting shuffle credits: **10**
- credit regeneration: **every 15 minutes**
- max credit cap: **50**
- unlimited undo testing: **disabled in scene**
- unlimited shuffle testing: **disabled in scene**
- rewarded game-over offer duration: **5 seconds**

Note: the `targetValue` inspector field exists on `BoardController`, but the milestone logic is currently still hard-coded around **2048+** values inside merge resolution and theme switching. Changing the inspector field alone does **not** fully change the milestone system.

---

## 3. Main ownership map

### `BoardController.cs`
Primary gameplay simulation owner.

Responsibilities:
- board grid data
- input handling
- swap validation
- merge detection
- merge resolution
- gravity and refill
- shuffle logic
- undo snapshot export/import
- valid-move detection
- game-over detection
- versus turn rotation visuals
- board normalization before save

### `GameManager.cs`
Primary game-flow and persistence owner.

Responsibilities:
- menu flow
- mode switching
- score handling
- undo / shuffle credits
- rewarded-ad continue flow
- limited-credit ad flow
- per-mode runtime state
- persistent save/load
- game over UI
- HUD updates

### `CandyTile.cs`
Lightweight tile view/data object.

Responsibilities:
- tile value
- tile text display
- tile color refresh
- label rotation
- tile movement animation

### `ThemeManager.cs`
Primary theme and palette owner.

Responsibilities:
- palette selection
- theme-family filtering
- tile colors
- board background color lookup
- UI theme color lookup
- palette switching on 2048+ milestone creation
- refresh notifications through `OnPaletteChanged`

### Other supporting systems
- `AudioManager.cs` → SFX playback and persisted SFX enabled state
- `MobileAdsManager.cs` → Google Mobile Ads integration, banner + rewarded
- `SafeAreaFitter.cs` → safe area + runtime ad inset handling
- `SettingsUIController.cs` → settings popup, SFX toggle, theme-family selection
- `UIBackgroundController.cs` → runtime UI recoloring, button depth styling
- `BackgroundController.cs` → camera background transition helper
- `MergeGhost.cs`, `MergeSparkle.cs`, `MergeFirework.cs` → visual-only merge effects
- `TilePaletteDatabase.cs` → palette asset definition
- `SfxLibrary.cs` → SFX enum + clip database
- `ColorThemeManager.cs` → compatibility / legacy-style color helper, not the primary active theme system

---

## 4. Board model

The active board is stored as:
- `CandyTile[,] grid`

Each tile tracks:
- grid coordinates `x`, `y`
- current numeric value
- sprite renderer
- TMP value label
- owning `BoardController`

The board uses world-space positioning derived from:
- `width`
- `height`
- `spacingRatio`
- prefab sprite size

`BoardController` also auto-fits the orthographic camera to the board when enabled.

---

## 5. Input model

Input is drag-based.

Flow:
1. player presses a tile
2. press position is stored in tile-root local space
3. on release, a dominant drag direction is chosen
4. the board selects the neighbor in that direction
5. only orthogonally adjacent swaps are allowed
6. if the swap creates no merge group, the swap is animated back

Important parameter:
- `dragThresholdInCells`

The game never performs a full-board slide.

---

## 6. Start-board generation

A fresh board is not filled with only 2s.

Current start behavior:
- starting values are chosen from a weighted pool: `2, 4, 8, 16, 32, 64, 128, 256`
- heavier weight is given to smaller values
- start generation tries to avoid immediate horizontal or vertical lines of 3 equal values
- after generation, the board is normalized and checked for at least one valid move
- if needed, it is reshuffled until a valid move exists

This means a fresh board is designed to feel populated and playable immediately without free auto-merges.

---

## 7. Merge detection rules

Merges are **line-based + connected-group-based**.

The board first marks every tile that belongs to:
- a horizontal line of length **3 or more**, or
- a vertical line of length **3 or more**

Then it groups connected matching marked tiles using flood fill / BFS.

As a result, these all count as one merge group when connected by equal-value tiles:
- straight lines
- crosses
- T-shapes
- larger connected structures made from valid horizontal/vertical match cells

A group only resolves if it contains at least 3 tiles.

---

## 8. Merge center selection

Each group resolves into a single **center tile**.

Center selection priority:
1. if a tile belongs to both a horizontal and vertical line, that intersection tile becomes center
2. if the group is horizontal-only, the horizontal midpoint tile is used
3. if the group is vertical-only, the vertical midpoint tile is used
4. otherwise, the tile nearest the group centroid is chosen

All non-center tiles in the group are removed.

---

## 9. Merge math

Merge formula:

`newValue = originalValue << (groupSize - 1)`

Examples:
- `2 + 2 + 2` → `8`
- `2 + 2 + 2 + 2` → `16`
- `4 + 4 + 4` → `16`

This is exponential growth based on the number of merged tiles.

The system is **not pairwise** and must not be treated like repeated pair merges.

---

## 10. Score rules

Important scoring behavior:
- `GameManager.AddScore` doubles the incoming merged value before adding it
- only the **first resolve pass caused directly by the player move** awards score
- cascades caused by gravity/refill after that resolve visually, but do **not** award additional score

So the scoring rule is effectively:
- `score gain = merged value x 2`
- only for groups created directly by the accepted swap

Mode routing:
- Solo → score goes to `Score`
- Versus → score goes to the current scoring player (`BoardController.ScoringPlayer`)

This applies to milestone merges too. A `2048+` creation still scores before the resulting tile is removed.

---

## 11. Resolve loop

After every successful player swap, the board runs this loop until stable:
1. detect groups
2. apply merges
3. apply gravity
4. refill empty cells
5. repeat

Important details:
- gravity is downward in the grid
- refill creates new tiles for empty cells
- the loop has a safety cap to avoid infinite resolution
- after the final stable state of a scored move, the board checks for game over

The current implementation distinguishes between:
- **directly scored pass**
- **non-scoring cascade passes**

Do not collapse that distinction accidentally.

---

## 12. Refill / spawn behavior during gameplay

Refills do more than random 2-spawning.

### Spawn presets
`BoardController` supports three preset families:
- `ClassicHard`
- `Balanced`
- `Rare32`

Current scene uses **Rare32**.

### Dynamic spawn balancer
When enabled, refill weights can shift based on average board strength:
- weak board → slightly more generous higher values
- strong board → slightly more generous lower values
- very large values stay rare

### Danger-helper spawn
When enabled, the board can try to place a helpful refill tile if the board is close to running out of moves.

Current behavior:
- mostly intended for Solo mode
- checks valid move count
- if moves are low enough, it evaluates nearby candidate values
- it prefers values that are likely to create or support future 3-in-line matches

This helper is part of current gameplay balance and should not be removed casually.

---

## 13. Shuffle system

Shuffle is **Solo-only** from the UI.

Behavior:
- requires a full board
- stores an undo snapshot before shuffling
- randomizes the values of current tiles
- tries up to 40 attempts to reach at least 3 valid moves
- runs the resolve loop afterward without score
- saves the stabilized board

Because shuffle stores an undo snapshot, the player can undo a shuffle if undo is allowed and a snapshot exists.

---

## 14. Undo system

Undo is **one stored snapshot**, not a history stack.

Snapshot contents:
- board dimensions
- tile values
- current player
- solo score or versus scores

Behavior:
- snapshot is captured before a move is committed
- failed swaps do not consume or overwrite the undo snapshot
- successful swaps commit the pending snapshot
- using undo restores the snapshot and then clears it
- after undo, `PlayerHasMoved` is reset to `false`

Undo is currently exposed only in Solo mode.

---

## 15. Versus mode behavior

Play types:
- `Solo`
- `Versus1v1`

Versus specifics:
- two separate scores are tracked
- shuffle button is hidden / unavailable
- the board rotates **180 degrees visually** between turns
- tile labels are also rotated for readability
- gravity itself is still effectively downward in the current implementation
- after a successful resolve, the turn switches

Important nuance:
- the board view rotates for turn presentation
- gravity does **not** currently reverse per player turn

---

## 16. 2048+ milestone rule

When a merge creates a value **greater than or equal to 2048**:
- 2048 milestone SFX plays
- firework VFX spawns
- sparkle / ghost VFX also participate
- `ThemeManager.NotifyValueCreated(newValue)` is called
- the resulting tile is **removed from the board**

So `2048+` values are milestone events, not persistent board tiles.

This is one of the most important design rules in the project.

---

## 17. Game over + rewarded continue flow

Game over is triggered when, after a player-scored resolve, the stabilized board has no valid moves.

Flow:
1. `BoardController` calls `GameManager.GameOver()`
2. `GameManager` captures a snapshot of the board and score state
3. the game shows a rewarded-ad decision panel for a limited time
4. if the player closes the panel or the timer expires, normal game over is confirmed
5. if the rewarded ad completes successfully, the board snapshot is restored
6. gameplay resumes
7. the board auto-shuffles once after the rewarded continue
8. the run is saved again

Important details:
- current offer duration in scene: **5 seconds**
- if rewarded is still loading, the countdown temporarily waits
- Solo updates total/max score on confirmed game over
- confirmed game over clears the saved run for that mode

---

## 18. Credits system

There are two player-facing credit types:
- Undo credits
- Shuffle credits

Current behavior:
- both start at configurable values
- both regenerate using UTC timestamps in `PlayerPrefs`
- regeneration works while the app is closed
- current scene cap is **50**
- a limited-credits popup can offer a rewarded ad to grant **one extra credit** of the requested type

Key persistence keys:
- `UNDO_CREDITS`
- `SHUFFLE_CREDITS`
- `CREDITS_LAST_GRANT_UTC`

The code also contains corruption recovery for legacy timestamp/credit bugs.

---

## 19. Persistence model

The game uses both:
- **runtime in-memory state per mode**, and
- **persistent PlayerPrefs JSON state per mode**

Persistent keys:
- `SOLO_BOARD_STATE_JSON`
- `VERSUS_BOARD_STATE_JSON`

Saved board state includes:
- width
- height
- tile values
- current player
- score snapshot fields

Important save behavior:
- before saving, `BoardController.PrepareBoardForSave()` normalizes the board into a stable no-empty, no-pending-merge state
- saves occur on stable checkpoints, pause, quit, and menu return when the run is still alive
- Solo and Versus saves are fully separate

---

## 20. Theme system

Primary theme pipeline:
- `ThemeManager`
- `TilePaletteDatabase`
- `CandyTile.RefreshColor()`
- UI listeners using `ThemeManager.OnPaletteChanged`

### Theme families
The project supports theme-family filtering through settings:
- Dark
- Colorful
- Light

`SettingsUIController` stores the selection mask in `SETTINGS_THEME_SELECTION`.
A stored mask of `0` means “all enabled”.

### Palette selection behavior
- on game start or settings change, `ThemeManager` resets to a random eligible palette
- when a 2048+ merge happens, it attempts to switch to a different eligible palette
- tile colors come from palette index by power-of-two value
- text color is chosen mainly from theme family rules
- UI colors are generated from theme family defaults

Important correction versus older assumptions:
- `ThemeManager` does **not** currently use `DontDestroyOnLoad`
- it is a scene object, not a persistent singleton object across scenes

`AudioManager` and `MobileAdsManager` are the singleton-style `DontDestroyOnLoad` systems, not `ThemeManager`.

---

## 21. Audio system

`AudioManager` responsibilities:
- persisted SFX enabled state via `SFX_ENABLED`
- single-shot playback
- layered dual-source playback
- random clip + pitch jitter from `SfxLibrary`

Current SFX ids:
- `MergeCrack`
- `MergeBody`
- `Merge2048Sparkle`
- `Merge2048Air`
- `GameOverClose`
- `GameOverHope`
- `MenuModeSelect`

---

## 22. Ads + safe area

### Mobile ads
`MobileAdsManager` currently handles:
- SDK initialization
- bottom adaptive banner
- rewarded ad load/show/reload
- safe callback contract through `ShowRewarded(Action<bool>)`
- automatic reloading after rewarded close/failure

The current ad unit ids are Google test IDs.

### Safe area
`SafeAreaFitter`:
- applies `Screen.safeArea` to a UI `RectTransform`
- supports extra runtime inset values in pixels
- is used by `MobileAdsManager` to reserve banner height at the bottom
- re-applies automatically on safe-area / size / orientation changes

---

## 23. UI notes

### Settings UI
`SettingsUIController` manages:
- settings panel open/close
- SFX toggle + button state
- theme-family selection buttons
- saving settings to `PlayerPrefs`
- reapplying visuals on palette changes

### Runtime UI theming
`UIBackgroundController` manages:
- panel recoloring by scene object names
- button face/shadow/outline depth styling
- non-button text recoloring
- forcing button content to black for readability

### Camera background
`BackgroundController` is a camera background transition helper that listens to palette changes.
Treat it as visual-only.

---

## 24. VFX system

VFX prefabs reviewed:
- `MergeGhost.prefab`
- `MergeSparkle.prefab`
- `MergeFirework.prefab`

Roles:
- `MergeGhost` → floating/fading ghost of removed tiles
- `MergeSparkle` → short-lived burst particles on merges
- `MergeFirework` → milestone radial firework burst for 2048+

These effects are purely visual and do not modify board logic.

---

## 25. Important implementation constraints

When modifying this project, assume all of the following are intentional unless explicitly changing design:
- swap-based gameplay, not slide-based gameplay
- line-of-3+ / connected-group merges
- exponential merge formula based on group size
- score only on the first resolve pass of the player move
- 2048+ milestone tiles are removed from the board
- dynamic refill balancing exists on purpose
- danger-helper refill exists on purpose
- undo is one snapshot, not full history
- shuffle is Solo-only from the UI
- rewarded continue restores snapshot and auto-shuffles
- Solo and Versus save data are separate
- board simulation lives in `BoardController`
- flow / persistence / score routing live in `GameManager`

---

## 26. Repository structure (reviewed files)

```text
Assets/
├─ Scenes/
│  ├─ SampleScene.unity
│  └─ Prefabs/
│     ├─ CandyTile.prefab
│     ├─ MergeFirework.prefab
│     ├─ MergeGhost.prefab
│     └─ MergeSparkle.prefab
├─ Scripts/
│  ├─ AudioManager.cs
│  ├─ BackgroundController.cs
│  ├─ BoardController.cs
│  ├─ CandyTile.cs
│  ├─ ColorThemeManager.cs
│  ├─ GameManager.cs
│  ├─ MergeFirework.cs
│  ├─ MergeGhost.cs
│  ├─ MergeSparkle.cs
│  ├─ MobileAdsManager.cs
│  ├─ SafeAreaFitter.cs
│  ├─ SettingsUIController.cs
│  ├─ SfxLibrary.cs
│  ├─ UIBackgroundController.cs
│  └─ Theme/
│     ├─ ThemeManager.cs
│     └─ TilePaletteDatabase.cs
```

---

## 27. Purpose of this document

Use this file to understand the real current behavior of the project before changing:
- gameplay systems
- scoring
- spawn balancing
- persistence
- UI flow
- theme logic
- ad continue flow

If you are editing gameplay, start with:
1. `BoardController.cs`
2. `GameManager.cs`
3. `CandyTile.cs`
4. `ThemeManager.cs`

