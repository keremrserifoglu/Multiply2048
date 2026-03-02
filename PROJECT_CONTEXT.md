# PROJECT CONTEXT — PowerCandyDuel2 (kod_analizi)

## 1) Project Summary
**Engine:** Unity  
**Main Scene:** `SampleScene.unity`  
**Game Type:** Grid-based merge game (swap + match groups + gravity + refill), with **Solo** and **Versus 1v1** modes.

Core idea:
- Board is an `width x height` grid (default 8x8).
- Player swaps adjacent tiles via drag.
- After swap, the board resolves by:
  1) Finding match groups (including cross/cluster logic)
  2) Merging groups into a center tile (value grows)
  3) Applying gravity (tiles fall)
  4) Refilling empty cells with new tiles

Special rule:
- When a merged value reaches **>= 2048**, special VFX/SFX plays and the tile is removed; theme/palette progression can be triggered.

---

## 2) Gameplay Loop (High Level)
1. Player presses a tile and drags to swap (`BoardController` input & swap).
2. Swap resolves if valid, then a resolve loop runs:
   - Find groups
   - Merge -> score (if scoring resolve)
   - Gravity
   - Refill
3. If there are no valid moves, game ends.

---

## 3) Main Runtime Controllers

### GameManager (`GameManager.cs`)
Responsibilities:
- Singleton: `GameManager.I`
- Game flow + UI panels:
  - `mainMenuPanel`, `hudPanel`, `gameOverPanel`
  - Texts (score, max score, total score, winner, etc.)
- Two play modes:
  - `Solo`
  - `Versus1v1`
- Handles:
  - Start/Resume/New game
  - Shuffle (Solo only)
  - Undo system (credits + UI)
  - Saving/loading meta scores and persistent board state (PlayerPrefs JSON)

Key properties:
- `Score`, `TotalScore`, `MaxScore`, `UndoCredits`
- `PlayerHasMoved`

---

### BoardController (`BoardController.cs`)
Responsibilities:
- Owns the grid: `CandyTile[,] grid`
- Board config:
  - `width`, `height`, `spacingRatio`, `targetValue`
  - spawn presets (`SpawnPreset`)
- Tile spawning and movement:
  - `tilePrefab` (CandyTile prefab)
  - gravity + refill animations
- Player input:
  - press/drag to swap
  - drag threshold: `dragThresholdInCells`
- Resolve loop:
  - Find groups, merge, score, gravity, refill
  - Validate moves / game over
- Undo snapshot:
  - `ExportState()` / `ImportState()`
  - `TryUndoLastMove()`
- Camera fit (optional):
  - `autoFitCameraToBoard`, `targetCamera`, `cameraPadding`

Merge VFX hooks (instantiated prefabs):
- `mergeGhostPrefab` -> `MergeGhost`
- `mergeSparklePrefab` -> `MergeSparkle`
- `mergeFireworkPrefab` -> `MergeFirework`

GameManager compatibility APIs:
- `ResetBoardForMenu()`
- `NewGame(playType)`
- `ResumeGame(playType)`
- `TryShuffle()`
- `TryUndoLastMove()`
- `ExportState()`, `ImportState()`

---

## 4) Tiles & Visuals

### CandyTile (`CandyTile.cs`)
Responsibilities:
- Represents a single tile on the grid.
- Holds:
  - grid position: `x`, `y`
  - value: `Value`
  - references: `TMP_Text valueText`, `SpriteRenderer spriteRenderer`
- Functions:
  - `Init(board, gx, gy, value)`
  - `SetValue(v)` + number sizing (`ApplyNumberSizing`)
  - `RefreshColor()` via `ThemeManager`
  - Movement:
    - `SetWorldPosInstant(pos)`
    - `MoveToWorld(pos, duration)`
  - Label rotation support: `SetLabelRotation(rotation)`

Note:
- Tile scaling “pop” effects are disabled intentionally (layout stability).

---

## 5) Theme / Palette System

### ThemeManager (`ThemeManager.cs`)
Responsibilities:
- Singleton: `ThemeManager.I` (DontDestroyOnLoad)
- Uses `TilePaletteDatabase` ScriptableObject:
  - `db: TilePaletteDatabase`
- Provides:
  - `GetTileColor(value)`
  - `GetBackgroundColor()`
  - `GetTextColorForTile(tileColor)`
- Palette switching:
  - `NextPalette()`
  - `NotifyValueCreated(value)` triggers palette change when `value >= 2048`
- Utility:
  - `RefreshAllTiles()`
  - `ResetTheme()`

### TilePaletteDatabase (`TilePaletteDatabase.cs` + `TilePaletteDatabase.asset`)
ScriptableObject contains:
- Multiple `Palette` entries:
  - tile colors for powers (2,4,8,16,...)
  - board tint / background color
  - text colors for contrast (dark/light)

---

## 6) Audio System

### AudioManager (`AudioManager.cs`)
Responsibilities:
- Singleton: `AudioManager.I` (DontDestroyOnLoad)
- Uses `SfxLibrary` ScriptableObject:
  - `sfx: SfxLibrary`
- Plays:
  - `Play(SfxId)`
  - `PlayLayered(front, back)`
- Respects persisted setting:
  - PlayerPrefs key: `SFX_ENABLED`
- Has 2 AudioSources for layering:
  - `sfxSource`, `sfxSource2`

### SfxLibrary (`SfxLibrary.cs`)
ScriptableObject mapping:
- Enum `SfxId` includes merge/gameover/menu events
- Each entry:
  - multiple clips, volume, pitch jitter

---

## 7) UI / Safe Area / Background

### SettingsUIController (`SettingsUIController.cs`)
Responsibilities:
- Settings panel open/close
- SFX toggle:
  - writes PlayerPrefs `SFX_ENABLED`
  - updates AudioManager

### SafeAreaFitter (`SafeAreaFitter.cs`)
Responsibilities:
- Applies `Screen.safeArea` to a RectTransform (for mobile notch/safe area)

### Background/Theme-related scripts
Present in project (scene usage varies):
- `GradientBackgroundController.cs`
- `BackgroundController.cs`
- `UIBackgroundController.cs`
- `CameraPaletteBackground.cs`
- `ColorThemeManager.cs`

(These generally relate to background visuals and palette-based coloring.)

---

## 8) Prefabs Included (from uploaded package)
- `CandyTile.prefab` (SpriteRenderer + TMP text; uses `CandyTile`)
- `MergeGhost.prefab` (used by `BoardController` -> ghost burst)
- `MergeSparkle.prefab` (used by `BoardController` -> sparkle burst)
- `MergeFirework.prefab` (used by `BoardController` -> firework burst)

---

## 9) Scene Notes (SampleScene.unity)
Scene contains (not exhaustive, key objects):
- `GameManager`
- `Board`
- UI: `MainMenuPanel`, `HudPanel`, `GameOverPanel`, buttons (Solo/1v1/Undo/Shuffle/Menu)
- `ThemeManager`
- `AudioManager`
- `EventSystem`
- Background objects (Gradient/background controllers)
- `Global Volume` (URP volume profile asset present)

---

## 10) Persistence Keys (PlayerPrefs)
- `TOTAL_SCORE_STR`
- `MAX_SCORE_STR`
- `UNDO_CREDITS`
- `SOLO_BOARD_STATE_JSON`
- `VERSUS_BOARD_STATE_JSON`
- `SFX_ENABLED`

---

## 11) AI Collaboration Rules (project-level)
- Code comments must be in English.
- Do not use emojis in code comments.
- Prefer minimal, readable changes consistent with existing style.

END