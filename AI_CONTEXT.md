# AI_CONTEXT.md

This file exists to help AI assistants safely modify the Multiply 2048 repository.

Before changing any gameplay code, always read:
- `PROJECT_CONTEXT.md`
- `AI_CONTEXT.md`

These documents define the current project rules and guardrails.

---

## 1. Project type

Multiply 2048 is a **grid-based drag-swap merge puzzle game built in Unity**.

It is **not** slide-based 2048.

Do not introduce:
- whole-board slide mechanics
- swipe-to-shift board logic
- pair-only 2048 assumptions

The player swaps one tile with one orthogonal neighbor.

---

## 2. Primary ownership boundaries

### `BoardController`
Board simulation owner.

Keep these responsibilities here:
- grid data
- input
- swap validation
- merge detection
- merge resolution
- gravity
- refill
- valid-move checks
- undo snapshot export/import
- mode-specific turn visuals
- game-over detection

### `GameManager`
Flow / persistence / UI state owner.

Keep these responsibilities here:
- menu flow
- mode switching
- score routing
- credit economy
- saved-run handling
- rewarded continue handling
- limited-credit popup handling
- HUD and game-over UI

### `CandyTile`
Keep lightweight.

Allowed responsibilities:
- tile value
- tile color refresh
- text display
- label rotation
- movement animation

Do **not** move board logic into `CandyTile`.

---

## 3. Core gameplay rules that must be preserved

### Swap rule
A move is valid only if the adjacent swap creates at least one merge group.
If not, the swap is reverted.

### Merge rule
Merges are based on **3+ in line** and then grouped by connected matching marked cells.
This means cross / T / connected line structures merge as one connected group.

### Merge formula
Preserve:

`newValue = originalValue << (groupSize - 1)`

Do not replace it with pairwise or additive logic unless explicitly instructed.

### Merge center rule
A group resolves into one chosen center tile.
Non-center tiles are removed.

### 2048 milestone rule
When `newValue >= 2048`:
- score still applies if the pass is score-enabled
- milestone SFX/VFX are triggered
- `ThemeManager.NotifyValueCreated(newValue)` is called
- the resulting tile is removed from the board

Do not let 2048 tiles remain on the board unless explicitly asked.

---

## 4. Resolve-loop rule

The current gameplay loop after a successful swap is:
1. find groups
2. apply merges
3. apply gravity
4. refill empty cells
5. repeat until stable

Important nuance:
- only the **first pass** of a player-caused resolve awards score
- later cascades resolve visually but do **not** award score

Do not accidentally convert cascade passes into score-generating passes.

---

## 5. Scoring rule

`GameManager.AddScore` multiplies incoming merge value by **2** before adding it.

Scoring also depends on mode:
- Solo → add to `Score`
- Versus → add to current scoring player based on `BoardController.ScoringPlayer`

If you touch scoring, preserve both:
- the x2 multiplier
- the “first pass only” restriction

---

## 6. Spawn / refill balancing rules

The board does not simply spawn `2` forever.

Current refill system includes:
- spawn presets (`ClassicHard`, `Balanced`, `Rare32`)
- dynamic spawn balancing based on board strength
- danger-helper refill logic when valid moves are low

The current scene uses:
- `Rare32`
- dynamic spawn balancing enabled
- danger-helper enabled

Do not remove or flatten these systems unless explicitly requested.

If you change refill logic, keep in mind:
- large values are intentionally rare
- helper spawn is a difficulty-smoothing tool
- start-board generation also has separate anti-instant-match logic

---

## 7. New-game generation rules

Fresh boards currently:
- use a weighted starting-value distribution
- avoid immediate 3-in-line matches during generation
- normalize and ensure at least one valid move before play begins

Do not change fresh-board behavior into:
- all-2 start
- immediate free merges at spawn
- no-valid-move starts

unless explicitly requested.

---

## 8. Undo rule

Undo is **one snapshot only**.
It is not a move-history stack.

Snapshot includes:
- board values
- dimensions
- current player
- score snapshot fields

Important behavior:
- snapshot is captured before a move is committed
- failed swaps do not overwrite it
- using undo restores that snapshot and then clears it
- shuffle also saves an undo snapshot first

Do not convert this into multi-step history unless explicitly asked.

---

## 9. Shuffle rule

Shuffle is a Solo-facing powerup.

Current behavior:
- requires a full board
- randomizes current tile values
- tries to keep at least 3 valid moves if possible
- runs resolve afterward without scoring
- saves stabilized state afterward

Because shuffle currently interacts with undo and persistence, edits here must be careful.

---

## 10. Game-over / rewarded-continue rule

Current flow:
- board detects no valid moves after a scored resolve
- `GameManager` stores a snapshot and opens a rewarded-ad offer panel
- if reward succeeds, the snapshot is restored
- gameplay resumes
- the board auto-shuffles once
- run state is saved again
- if reward is declined / times out / fails, normal game over completes

Do not break this contract.

Important detail:
- rewarded continue restores a previously saved board snapshot, not a newly generated board

---

## 11. Credits system rule

Two credit pools exist:
- Undo credits
- Shuffle credits

Current behavior:
- regeneration uses UTC time and `PlayerPrefs`
- credits can regenerate while the app is closed
- there is optional max-cap enforcement
- a rewarded ad from the limited-credit panel grants exactly one extra credit of the requested type

Do not replace this with server logic or a different persistence format unless explicitly requested.

---

## 12. Persistence rule

The project currently uses:
- runtime per-mode memory state
- persistent per-mode JSON state in `PlayerPrefs`

Persistent keys:
- `SOLO_BOARD_STATE_JSON`
- `VERSUS_BOARD_STATE_JSON`

Before saving a live run, `BoardController.PrepareBoardForSave()` normalizes the board into a stable state.

Do not save mid-resolution unstable boards unless you intentionally redesign the system.

Do not mix Solo and Versus save data.

---

## 13. Versus-mode rule

Versus mode is not just “Solo with two scores.”

Current behavior includes:
- separate P1 / P2 scores
- board root rotates 180° between turns
- tile labels rotate with the board
- gravity still stays visually downward in the current implementation
- shuffle UI is not used in Versus

Do not assume per-player gravity reversal exists.
If you add versus features, preserve score routing and turn switching.

---

## 14. Theme-system rule

The active theme pipeline is:
- `ThemeManager`
- `TilePaletteDatabase`
- `CandyTile.RefreshColor()`
- UI listeners on `ThemeManager.OnPaletteChanged`

Current behavior:
- settings choose allowed theme families: Dark / Colorful / Light
- a stored selection value of `0` means “all enabled”
- theme resets on game start / settings change
- a new 2048+ milestone attempts to switch to a different eligible palette

Important correction:
- `ThemeManager` is **not** currently `DontDestroyOnLoad`
- it is scene-scoped

Do not document or design around ThemeManager persistence unless you first add it explicitly.

---

## 15. Audio-system rule

`AudioManager` is persistent and uses `DontDestroyOnLoad`.
It owns:
- SFX enabled state
- single or layered SFX playback
- `SFX_ENABLED` persistence

If changing audio settings, keep them compatible with `SettingsUIController` and `AudioManager.SetSfxEnabled`.

---

## 16. Ads rule

`MobileAdsManager` is persistent and uses `DontDestroyOnLoad`.

Current contract:
- `ShowRewarded(Action<bool> onCompleted)`
- callback gets `true` only if reward was actually earned
- rewarded ads auto-reload after close/failure
- bottom banners update the safe-area bottom inset through `SafeAreaFitter`

Do not change the `Action<bool>` reward contract unless all callers are updated.

---

## 17. Safe-area rule

`SafeAreaFitter` is not cosmetic only.
It is part of correct mobile UI layout and also receives ad-banner inset updates.

Do not remove or bypass it casually.

If changing full-screen panels, keep them safe-area aware.

---

## 18. UI theming / settings rule

`SettingsUIController` currently owns:
- opening/closing settings panel
- SFX toggles
- theme-family selection buttons
- PlayerPrefs updates for those settings

`UIBackgroundController` currently owns:
- panel recoloring
- button shadow/outline depth styling
- non-button text recoloring
- forcing button content to black for readability

If you redesign the UI, do not break the `ThemeManager.OnPaletteChanged` listener flow.

---

## 19. Legacy / compatibility note

`ColorThemeManager.cs` exists, but it is not the primary modern color path for tiles.
Primary tile color lookup is through `ThemeManager` + `TilePaletteDatabase`.

Treat `ColorThemeManager` as compatibility / secondary unless you intentionally reintroduce it.

---

## 20. Important code-reading notes

### `targetValue`
`BoardController` exposes `targetValue`, but milestone code currently checks against literal `2048` in merge handling.
If asked to make target fully configurable, update all milestone paths, not just the inspector field.

### `MAX_POWERUPS`
`GameManager` defines `MAX_POWERUPS`, but current gameplay credit flow is specifically undo/shuffle based.
Do not assume a broader active powerup system exists unless you confirm implementation.

### Scene vs script defaults
Inspector values in `SampleScene` override some script defaults.
When making gameplay changes, check both code and scene values.

---

## 21. Safe editing checklist for AI

Before editing, ask yourself:
1. Does this belong in `BoardController` or `GameManager`?
2. Am I preserving swap-based gameplay?
3. Am I preserving 3+ connected-group merge logic?
4. Am I preserving first-pass-only scoring?
5. Am I preserving 2048 milestone tile removal?
6. Am I preserving save compatibility for Solo and Versus?
7. Am I preserving rewarded continue and credit-ad contracts?
8. Am I preserving safe-area and theme listener behavior?

If the answer to any of these is no, the change is likely risky and should be deliberate.

---

## 22. Examples of relatively safe AI tasks

Usually safe:
- add new UI panels
- add new palettes
- add new SFX ids and clips
- add new purely visual VFX
- improve panel styling
- optimize board scanning without changing behavior
- add more spawn presets without breaking existing ones
- add accessibility or quality-of-life UI

---

## 23. Examples of risky AI tasks

High-risk unless explicitly requested:
- converting the game into slide-based 2048
- changing merge math
- changing center selection semantics
- scoring cascades
- keeping 2048 tiles on the board
- removing dynamic refill balancing
- removing danger-helper spawn
- replacing undo with arbitrary full history
- changing rewarded callback semantics
- moving gameplay simulation out of `BoardController`
- moving flow / persistence ownership out of `GameManager`

---

## 24. Final instruction for AI tools

When in doubt:
- preserve existing gameplay behavior
- preserve data compatibility
- preserve ownership boundaries
- prefer additive changes over rewrites

Read `PROJECT_CONTEXT.md` first, then edit with minimal behavioral drift.

