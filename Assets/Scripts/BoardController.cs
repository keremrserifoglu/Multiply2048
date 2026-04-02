using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    [SerializeField] private Transform boardRoot;

    [Header("Camera Fit")]
    [SerializeField] private bool autoFitCameraToBoard = true;
    [SerializeField] private Camera targetCamera;
    [Tooltip("Extra margin around the board in world units")]
    [SerializeField] private float cameraPadding = 0.15f;

    [SerializeField] private GameObject mergeGhostPrefab;
    [SerializeField] private int mergeGhostBurstCount = 3;
    [SerializeField] private float mergeGhostSpawnRadius = 0.12f;
    [SerializeField] private int mergeGhostBurstCap = 6;

    private bool isPlayer1Turn = true;

    public enum SpawnPreset
    {
        ClassicHard,
        Balanced,
        Rare32
    }

    [Header("Spawn Presets")]
    public bool useSpawnPresets = true;
    public SpawnPreset spawnPreset = SpawnPreset.Rare32;

    [Header("Dynamic Spawn Balancer")]
    [Tooltip("If enabled, sometimes adjusts spawn probabilities based on current board strength.")]
    public bool useDynamicSpawnBalancer = true;
    public bool useDangerHelperSpawn = true;

    [Range(0f, 1f)]
    public float dangerHelperChance = 0.80f;

    [Range(1, 6)]
    public int dangerHelperTriggerMoves = 5;

    public bool helperSpawnSoloOnly = true;

    [Range(0f, 1f)]
    [Tooltip("How often dynamic balancing is applied. 0 = never, 1 = always.")]
    public float dynamicSpawnChance = 0.20f;

    [Range(0f, 1f)]
    [Tooltip("How strong the adjustment is. Keep low for natural feel.")]
    public float dynamicSpawnStrength = 0.35f;

    [Header("Early Game Tuning")]
    public bool useEarlyGameTuning = true;

    [Min(0)]
    public int earlyGameMoveWindow = 8;

    [Min(1)]
    public int openingMinValidMoves = 3;

    [Min(0)]
    public int dangerHelperUnlockMove = 4;

    [Range(0f, 1f)]
    public float earlyDangerHelperChanceMultiplier = 0.35f;

    [Range(1, 6)]
    public int earlyDangerHelperMaxTriggerMoves = 2;

    [Range(0f, 1f)]
    public float earlyDynamicChanceMultiplier = 0.35f;

    [Range(0f, 1f)]
    public float earlyDynamicStrengthMultiplier = 0.50f;

    [Header("Opening Board Distribution")]
    [Min(0)] public int openingWeight2 = 460;
    [Min(0)] public int openingWeight4 = 300;
    [Min(0)] public int openingWeight8 = 120;
    [Min(0)] public int openingWeight16 = 24;
    [Min(0)] public int openingWeight32 = 4;

    [Header("Early Refill Distribution")]
    [Min(0)] public int earlyRefillWeight2 = 860;
    [Min(0)] public int earlyRefillWeight4 = 120;
    [Min(0)] public int earlyRefillWeight8 = 18;
    [Min(0)] public int earlyRefillWeight16 = 2;
    [Min(0)] public int earlyRefillWeight32 = 0;

    [Header("Spawn Limits")]
    [Min(2)] public int generatedSpawnMaxValue = 64;

    [Header("Opening Forced Seeds")]
    [Min(0)] public int openingForced16Count = 8;
    [Min(0)] public int openingForced32Count = 4;

    [Header("Board")]
    public int width = 8;
    public int height = 8;

    [Tooltip("1.00 tight, 1.04-1.06 small gaps")]
    public float spacingRatio = 1.06f;

    [Header("Tile")]
    public CandyTile tilePrefab;
    public Transform tilesRoot;

    [Header("Swap")]
    [Tooltip("Swap animation duration")]
    public float swapDuration = 0.18f;

    [Tooltip("Drag distance needed to trigger swap (in cell units)")]
    public float dragThresholdInCells = 0.35f;

    [Header("Target / Win")]
    public int targetValue = 2048;

    [Header("Hint System")]
    [SerializeField] private bool useIdleHints = true;
    [SerializeField] private bool hintsEnabledByDefault = true;
    [SerializeField] private bool hintSoloOnly = true;
    [SerializeField, Min(0f)] private float hintIdleDelay = 10f;
    [SerializeField, Min(0f)] private float hintExtraDelayBeforeFirstMove = 0f;
    [SerializeField, Min(0f)] private float hintRepulseInterval = 0f;
    [SerializeField, Range(0.02f, 0.35f)] private float hintHighlightStrength = 0.22f;
    [SerializeField, Range(1f, 1.10f)] private float hintPulseScale = 1.05f;
    [SerializeField, Min(0.10f)] private float hintPulseDuration = 0.90f;
    [SerializeField, Range(1, 3)] private int hintPulseCount = 2;

    // GameManager compatibility
    public bool IsGameOver => gameOver;
    public bool IsBusy => busy;
    public int ScoringPlayer => currentPlayer;

    private bool busy;
    private bool gameOver;
    private int currentPlayer = 1;

    private CandyTile[,] grid;

    // Geometry
    private Vector3 originWorld;
    private float cellSize = 1f;
    private Vector3 originLocal;
    private float tileWorldSize = 1f;
    private int lastScreenW = -1;
    private int lastScreenH = -1;

    // Undo
    private BoardState lastUndoSnap;
    private bool hasUndoSnap;

    // Input
    private CandyTile pressedTile;
    private Vector3 pressLocal;
    private bool pressing;

    // Run pacing
    private int successfulMovesThisRun;

    // Hint runtime
    private struct HintMove
    {
        public int ax;
        public int ay;
        public int bx;
        public int by;
    }

    private float lastPlayerInteractionTime;
    private float lastHintPulseTime = float.NegativeInfinity;
    private int stableBoardRevision;
    private int lastHintBoardRevision = -1;
    private bool hasActiveHint;
    private readonly List<CandyTile> activeHintTiles = new List<CandyTile>();
    private bool hintsRuntimeEnabled = true;

    // --------------------------
    // Lifecycle
    // --------------------------
    private void Awake()
    {
        hintsRuntimeEnabled = hintsEnabledByDefault;
    }

    private void OnEnable()
    {
        SubscribeThemeEvents();
        ResetHintTimer(clearHint: false);
    }

    private void OnDisable()
    {
        UnsubscribeThemeEvents();
        ClearActiveHint();
    }

    private void SubscribeThemeEvents()
    {
        if (ThemeManager.I == null)
            return;

        ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;
        ThemeManager.I.OnPaletteChanged += HandlePaletteChanged;
    }

    private void UnsubscribeThemeEvents()
    {
        if (ThemeManager.I == null)
            return;

        ThemeManager.I.OnPaletteChanged -= HandlePaletteChanged;
    }

    private void HandlePaletteChanged()
    {
        if (!hasActiveHint)
            return;

        ReapplyActiveHintVisuals();
    }

    // --------------------------
    // GameManager expected methods
    // --------------------------
    public void ResetBoardForMenu()
    {
        ClearActiveHint();
        HardResetRuntimeState();

        ClearBoardImmediate();
        grid = new CandyTile[width, height];
        ComputeGeometry();

        // Always leave menu in a clean Solo-like visual state
        currentPlayer = 1;
        isPlayer1Turn = true;
        if (boardRoot != null) boardRoot.rotation = Quaternion.identity;
    }

    public void PauseForMenu()
    {
        ClearActiveHint();

        // Stop any running resolve/refill coroutines so the board can't mutate while menu is open
        StopAllCoroutines();

        busy = false;
        pressedTile = null;
        pressing = false;

        // Ensure every tile is snapped to its correct grid position instantly
        SnapAllTilesToGridInstant();
    }

    public void ResumeGame()
    {
        gameOver = false;
        busy = false;
        ResetHintTimer();
    }

    public void ResumeGame(GameManager.PlayType playType)
    {
        ApplyGravityForMode(playType);
        ResumeGame();
        ApplyModeVisuals(playType);
    }

    public void ApplyGravityForMode(GameManager.PlayType playType)
    {
        // Always down visually (no-op for now)
    }

    public void NewGame(GameManager.PlayType playType)
    {
        ClearActiveHint();
        StopAllCoroutines();
        StartCoroutine(CoStartNewGame(playType));
    }

    public bool TryShuffle()
    {
        if (busy || gameOver) return false;

        RegisterPlayerInteraction();
        if (grid == null) return false;

        int nonNullCount = 0;
        var values = new List<int>(width * height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null) nonNullCount++;
                values.Add(grid[x, y] ? grid[x, y].Value : 0);
            }
        }

        if (nonNullCount != width * height)
        {
            Debug.LogError($"TryShuffle aborted: board has missing tiles. nonNullCount={nonNullCount}, expected={width * height}");
            return false;
        }

        SaveUndoSnapshot();

        const int minValidMovesAfterShuffle = 3;
        const int maxShuffleAttempts = 40;

        bool foundGoodShuffle = false;

        for (int attempt = 0; attempt < maxShuffleAttempts; attempt++)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            int kCheck = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] == null)
                    {
                        Debug.LogError($"TryShuffle failed: grid[{x}, {y}] is null.");
                        return false;
                    }

                    int v = values[kCheck++];
                    if (v <= 0) v = 2;

                    grid[x, y].SetValue(v);
                    grid[x, y].RefreshColor();
                }
            }

            if (CountValidMovesFast(minValidMovesAfterShuffle) >= minValidMovesAfterShuffle)
            {
                foundGoodShuffle = true;
                break;
            }
        }

        if (!foundGoodShuffle)
        {
            Debug.LogWarning("Shuffle could not guarantee at least 3 valid moves.");
        }

        int k = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] == null)
                {
                    Debug.LogError($"TryShuffle failed: grid[{x}, {y}] is null.");
                    return false;
                }

                int v = values[k++];
                if (v <= 0) v = 2;

                grid[x, y].SetValue(v);
                grid[x, y].RefreshColor();
                grid[x, y].SetWorldPosInstant(GridToWorld(x, y));
            }
        }

        StartCoroutine(CoShuffleResolveAndSave());
        return true;
    }

    private IEnumerator CoShuffleResolveAndSave()
    {
        yield return ResolveLoop(
            scoreThisResolve: false,
            animate: true,
            allowMilestoneCascadeScore: true
        );

        NotifyStableBoardChanged();
        GameManager.I?.SaveCurrentRunStable();
    }

    public bool TryUndoLastMove()
    {
        if (busy || !hasUndoSnap || lastUndoSnap == null)
            return false;

        RegisterPlayerInteraction();

        BoardState undoState = lastUndoSnap;

        ImportState(undoState);

        if (GameManager.I != null)
        {
            if (GameManager.I.CurrentPlayType == GameManager.PlayType.Solo)
            {
                GameManager.I.SetScore(undoState.soloScore);
            }
            else
            {
                GameManager.I.SetVersusScores(undoState.p1Score, undoState.p2Score);
            }
        }

        hasUndoSnap = false;
        lastUndoSnap = null;

        return true;
    }

    public BoardState ExportState()
    {
        if (grid == null) return null;

        var s = new BoardState();
        s.w = width;
        s.h = height;
        s.currentPlayer = currentPlayer;
        s.successfulMoves = successfulMovesThisRun;
        s.values = new int[width * height];

        int i = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                s.values[i++] = grid[x, y] ? grid[x, y].Value : 0;

        // Save scores into snapshot for Undo
        if (GameManager.I != null)
        {
            if (GameManager.I.CurrentPlayType == GameManager.PlayType.Solo)
            {
                s.soloScore = GameManager.I.Score;
            }
            else
            {
                s.p1Score = GameManager.I.GetPlayer1Score();
                s.p2Score = GameManager.I.GetPlayer2Score();
            }
        }

        return s;
    }

    public void ImportState(BoardState s)
    {
        if (s == null)
        {
            Debug.LogError("ImportState failed: state is null.");
            return;
        }

        if (s.values == null)
        {
            Debug.LogError("ImportState failed: state values are null.");
            return;
        }

        if (s.w <= 0 || s.h <= 0)
        {
            Debug.LogError($"ImportState failed: invalid board size {s.w}x{s.h}.");
            return;
        }

        if (s.values.Length < s.w * s.h)
        {
            Debug.LogError($"ImportState failed: state length is {s.values.Length}, expected at least {s.w * s.h}.");
            return;
        }

        ClearActiveHint();
        StopAllCoroutines();

        width = s.w;
        height = s.h;
        currentPlayer = s.currentPlayer;
        successfulMovesThisRun = Mathf.Max(0, s.successfulMoves);

        ClearBoardImmediate();
        grid = new CandyTile[width, height];
        ComputeGeometry();

        int i = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int v = s.values[i++];

                if (v > 0)
                {
                    SpawnAt(x, y, v, true);
                }
                else
                {
                    grid[x, y] = null;
                }
            }
        }

        busy = false;
        gameOver = false;
        pressedTile = null;
        pressing = false;

        bool gridValid = true;
        i = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int expectedValue = s.values[i++];
                bool shouldHaveTile = expectedValue > 0;

                if (shouldHaveTile && grid[x, y] == null)
                {
                    Debug.LogError($"ImportState error: grid[{x}, {y}] should contain value {expectedValue} but is null after restore.");
                    gridValid = false;
                }
                else if (!shouldHaveTile && grid[x, y] != null)
                {
                    Debug.LogError($"ImportState error: grid[{x}, {y}] should be empty but contains value {grid[x, y].Value} after restore.");
                    gridValid = false;
                }
            }
        }

        if (!gridValid)
        {
            Debug.LogError("ImportState produced an invalid board. Rebuilding from snapshot values.");

            ClearBoardImmediate();
            grid = new CandyTile[width, height];
            ComputeGeometry();

            i = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int v = s.values[i++];
                    if (v > 0)
                    {
                        SpawnAt(x, y, v, true);
                    }
                }
            }
        }

        SnapAllTilesToGridInstant();
        NormalizeBoardInstantNoScore();
        SnapAllTilesToGridInstant();
        NotifyStableBoardChanged();
    }

    // --------------------------
    // Unity loop (INPUT)
    // --------------------------
    private void Update()
    {
        if (gameOver || grid == null)
        {
            ClearActiveHint();
            return;
        }

        if (busy)
        {
            ClearActiveHint();
            return;
        }

        if (Input.GetMouseButtonDown(0))
            BeginPress(Input.mousePosition);

        if (Input.GetMouseButtonUp(0))
            EndPress(Input.mousePosition);

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) BeginPress(t.position);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) EndPress(t.position);
        }

        HandleIdleHintTick();
    }

    private void BeginPress(Vector2 screenPos)
    {
        RegisterPlayerInteraction();

        pressedTile = PickTile(screenPos);
        pressing = (pressedTile != null);
        if (!pressing) return;

        pressLocal = ScreenToLocalOnTilesRoot(screenPos);
    }

    private void EndPress(Vector2 screenPos)
    {
        RegisterPlayerInteraction();

        if (!pressing || pressedTile == null) { pressing = false; pressedTile = null; return; }
        if (busy) { pressing = false; pressedTile = null; return; }

        Vector3 releaseLocal = ScreenToLocalOnTilesRoot(screenPos);
        Vector3 delta = releaseLocal - pressLocal;

        float threshold = Mathf.Max(0.001f, cellSize * dragThresholdInCells);
        if (delta.magnitude < threshold)
        {
            pressing = false;
            pressedTile = null;
            return;
        }

        int dx = 0, dy = 0;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dx = (delta.x > 0f) ? 1 : -1;
        else
            dy = (delta.y > 0f) ? 1 : -1;

        int tx = pressedTile.x + dx;
        int ty = pressedTile.y + dy;

        pressing = false;

        if (!InBounds(tx, ty))
        {
            pressedTile = null;
            return;
        }

        var other = grid[tx, ty];
        if (other == null)
        {
            pressedTile = null;
            return;
        }

        StartCoroutine(CoTrySwap(pressedTile, other));
        pressedTile = null;
    }

    private CandyTile PickTile(Vector2 screenPos)
    {
        Vector3 w = ScreenToWorldOnZ0(screenPos);
        Collider2D col = Physics2D.OverlapPoint(w);
        if (!col) return null;
        return col.GetComponent<CandyTile>();
    }

    private Camera GetCam()
    {
        return targetCamera != null ? targetCamera : Camera.main;
    }

    private Vector3 ScreenToWorldOnZ0(Vector2 screenPos)
    {
        var cam = GetCam();
        if (!cam) return Vector3.zero;

        Vector3 w = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        w.z = 0f;
        return w;
    }

    private Vector3 ScreenToLocalOnTilesRoot(Vector2 screenPos)
    {
        var cam = GetCam();
        if (!cam) return Vector3.zero;

        Vector3 w = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        w.z = 0f;

        if (tilesRoot == null) tilesRoot = transform;
        return tilesRoot.InverseTransformPoint(w);
    }

    private bool InBounds(int x, int y)
        => x >= 0 && x < width && y >= 0 && y < height;

    // --------------------------
    // Swap logic
    // --------------------------
    private IEnumerator CoTrySwap(CandyTile a, CandyTile b)
    {
        if (a == null || b == null) yield break;
        if (busy || gameOver) yield break;

        int md = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        if (md != 1) yield break;

        busy = true;

        // Take a snapshot BEFORE attempting the move, but don't commit yet
        var pendingUndoSnap = ExportState();

        // Swap in grid
        SwapInGrid(a, b);

        // Animate swap
        Vector3 aw = GridToWorld(a.x, a.y);
        Vector3 bw = GridToWorld(b.x, b.y);

        a.MoveToWorld(aw, swapDuration);
        b.MoveToWorld(bw, swapDuration);

        yield return new WaitForSeconds(swapDuration);

        // Check if swap created any valid match
        var groups = FindGroupsIncludingCross();
        if (groups.Count == 0)
        {
            // Swap back (failed move) - DO NOT overwrite undo snapshot
            SwapInGrid(a, b);

            aw = GridToWorld(a.x, a.y);
            bw = GridToWorld(b.x, b.y);

            a.MoveToWorld(aw, swapDuration);
            b.MoveToWorld(bw, swapDuration);

            yield return new WaitForSeconds(swapDuration);

            busy = false;
            yield break;
        }

        // Successful move => commit undo snapshot NOW
        lastUndoSnap = pendingUndoSnap;
        hasUndoSnap = (lastUndoSnap != null);

        ClearActiveHint();

        GameManager.I?.SetPlayerHasMoved(true);
        yield return ResolveLoop(
            scoreThisResolve: true,
            animate: true,
            allowMilestoneCascadeScore: false
        );
        successfulMovesThisRun++;
        NotifyStableBoardChanged();
        GameManager.I?.SaveCurrentRunStable();

        if (GameManager.I != null && GameManager.I.CurrentPlayType == GameManager.PlayType.Versus1v1)
            SwitchTurn();

        busy = false;
    }

    private void SwapInGrid(CandyTile a, CandyTile b)
    {
        int ax = a.x, ay = a.y;
        int bx = b.x, by = b.y;

        grid[ax, ay] = b;
        grid[bx, by] = a;

        a.x = bx; a.y = by;
        b.x = ax; b.y = ay;
    }

    // --------------------------
    // Start game
    // --------------------------
    private IEnumerator CoStartNewGame(GameManager.PlayType playType)
    {
        ClearActiveHint();
        ResetHintTimer();

        busy = true;
        gameOver = false;

        currentPlayer = 1;
        isPlayer1Turn = true;

        ApplyGravityForMode(playType);

        hasUndoSnap = false;
        lastUndoSnap = null;
        successfulMovesThisRun = 0;

        GameManager.I?.SetPlayerHasMoved(false);

        const int maxAttempts = 40;
        int requiredOpeningMoves = GetOpeningMinimumValidMoves();

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            BuildFreshStartBoard();

            // Apply mode visuals after grid is created (labels exist now)
            ApplyModeVisuals(playType);

            yield return ResolveLoop(
                scoreThisResolve: false,
                animate: false,
                allowMilestoneCascadeScore: false
            );

            if (CountValidMovesFast(requiredOpeningMoves) >= requiredOpeningMoves)
                break;
        }

        EnsureMinimumValidMoves(1);
        busy = false;
        NotifyStableBoardChanged();
    }

    private void BuildFreshStartBoard()
    {
        if (tilesRoot == null) tilesRoot = transform;

        ClearBoardImmediate();
        grid = new CandyTile[width, height];
        ComputeGeometry();

        List<int> openingValues = BuildOpeningSeededValues(width * height);
        int index = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SpawnAt(x, y, openingValues[index++], instant: true);
            }
        }

        RefreshAllTileColors();
        RepositionAllTilesInstant();
        SnapAllTilesToGridInstant();
    }

    // --------------------------
    // Geometry (WORLD)
    // --------------------------
    private void ComputeGeometry()
    {
        if (tilesRoot == null) tilesRoot = transform;

        float baseSize = 1f;

        if (tilePrefab != null)
        {
            CandyTile temp = Instantiate(tilePrefab, Vector3.zero, Quaternion.identity);
            var sr = temp.spriteRenderer != null ? temp.spriteRenderer : temp.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                baseSize = sr.bounds.size.x;
                if (baseSize <= 0.0001f) baseSize = 1f;
            }

            Destroy(temp.gameObject);
        }

        cellSize = baseSize * spacingRatio;
        tileWorldSize = baseSize;

        float w = (width - 1) * cellSize;
        float h = (height - 1) * cellSize;

        originLocal = new Vector3(-w * 0.5f, -h * 0.5f, 0f);
        originWorld = tilesRoot.TransformPoint(originLocal);

        if (autoFitCameraToBoard)
            FitCameraToBoard();
    }

    private Vector3 GridToWorld(int x, int y)
    {
        Vector3 local = originLocal + new Vector3(x * cellSize, y * cellSize, 0f);
        return tilesRoot.TransformPoint(local);
    }

    private void RepositionAllTilesInstant()
    {
        if (grid == null) return;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (grid[x, y] != null)
                    grid[x, y].SetWorldPosInstant(GridToWorld(x, y));
    }

    private void RefreshAllTileColors()
    {
        if (grid == null) return;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (grid[x, y] != null)
                    grid[x, y].RefreshColor();
    }

    private void ClearBoardImmediate()
    {
        if (grid != null)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
                for (int x = 0; x < grid.GetLength(0); x++)
                    if (grid[x, y] != null)
                        Destroy(grid[x, y].gameObject);
        }

        if (tilesRoot != null)
        {
            for (int i = tilesRoot.childCount - 1; i >= 0; i--)
                Destroy(tilesRoot.GetChild(i).gameObject);
        }
    }

    // --------------------------
    // Spawn / refill
    // --------------------------
    private int GetGeneratedSpawnCap()
    {
        int rawCap = Mathf.Max(2, generatedSpawnMaxValue);
        int powerOfTwoCap = 2;

        while (powerOfTwoCap * 2 <= rawCap)
        {
            powerOfTwoCap *= 2;
        }

        return powerOfTwoCap;
    }

    private int ClampGeneratedSpawnValue(int value)
    {
        return Mathf.Min(value, GetGeneratedSpawnCap());
    }

    private int PickOpeningWeightedValue()
    {
        int[] values = { 2, 4, 8, 16, 32, 64 };
        float[] weights =
        {
        openingWeight2,
        openingWeight4,
        openingWeight8,
        openingWeight16,
        openingWeight32,
        0f
    };

        int picked = WeightedPick(values, weights);
        return ClampGeneratedSpawnValue(picked);
    }

    private List<int> BuildOpeningSeededValues(int cellCount)
    {
        List<int> values = new List<int>(cellCount);

        int forced32 = Mathf.Clamp(openingForced32Count, 0, cellCount);
        int forced16 = Mathf.Clamp(openingForced16Count, 0, cellCount - forced32);

        for (int i = 0; i < forced32; i++)
        {
            values.Add(ClampGeneratedSpawnValue(32));
        }

        for (int i = 0; i < forced16; i++)
        {
            values.Add(ClampGeneratedSpawnValue(16));
        }

        while (values.Count < cellCount)
        {
            values.Add(PickOpeningWeightedValue());
        }

        ShuffleOpeningValues(values);
        return values;
    }

    private void ShuffleOpeningValues(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private int RandomSpawnValue()
    {
        if (!useSpawnPresets)
        {
            return ClampGeneratedSpawnValue(2);
        }

        int[] values = { 2, 4, 8, 16, 32 };
        float[] weights = GetCurrentRefillWeights();

        float effectiveDynamicChance = dynamicSpawnChance;
        float effectiveDynamicStrength = dynamicSpawnStrength;

        if (IsEarlyGameActive())
        {
            float progress = GetEarlyGameProgress01();
            effectiveDynamicChance *= Mathf.Lerp(earlyDynamicChanceMultiplier, 1f, progress);
            effectiveDynamicStrength *= Mathf.Lerp(earlyDynamicStrengthMultiplier, 1f, progress);
        }

        if (!useDynamicSpawnBalancer || grid == null || UnityEngine.Random.value > effectiveDynamicChance)
        {
            return ClampGeneratedSpawnValue(WeightedPick(values, weights));
        }

        float avgExp = GetAverageTileExponent();
        float t = Mathf.InverseLerp(2.0f, 6.0f, avgExp);

        float highMul = Mathf.Lerp(1.0f + effectiveDynamicStrength, 1.0f - effectiveDynamicStrength, t);
        float lowMul = Mathf.Lerp(1.0f - effectiveDynamicStrength, 1.0f + effectiveDynamicStrength, t);

        float[] adjusted = new float[weights.Length];

        for (int i = 0; i < weights.Length; i++)
        {
            int v = values[i];
            float m = (v <= 4) ? lowMul : highMul;

            if (v >= 32)
            {
                m *= 0.75f;
            }

            adjusted[i] = weights[i] * m;
        }

        return ClampGeneratedSpawnValue(WeightedPick(values, adjusted));
    }

    private bool ShouldUseDangerHelperSpawn()
    {
        if (!useDangerHelperSpawn || grid == null)
            return false;

        if (helperSpawnSoloOnly && GameManager.I != null &&
            GameManager.I.CurrentPlayType != GameManager.PlayType.Solo)
            return false;

        float helperChance = dangerHelperChance;
        int helperTriggerMoves = dangerHelperTriggerMoves;

        if (useEarlyGameTuning)
        {
            if (successfulMovesThisRun < dangerHelperUnlockMove)
                return false;

            float progress = GetEarlyGameProgress01();
            helperChance *= Mathf.Lerp(earlyDangerHelperChanceMultiplier, 1f, progress);
            helperTriggerMoves = Mathf.Min(helperTriggerMoves, earlyDangerHelperMaxTriggerMoves);
        }

        if (UnityEngine.Random.value > helperChance)
            return false;

        int validMoves = CountValidMovesFast(helperTriggerMoves + 1);
        return validMoves <= helperTriggerMoves;
    }

    private int CountValidMovesFast(int stopAfter)
    {
        if (grid == null) return 0;

        int count = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x + 1 < width && SwapWouldCreateMerge(x, y, x + 1, y))
                {
                    count++;
                    if (count >= stopAfter) return count;
                }

                if (y + 1 < height && SwapWouldCreateMerge(x, y, x, y + 1))
                {
                    count++;
                    if (count >= stopAfter) return count;
                }
            }
        }

        return count;
    }

    private bool SwapWouldCreateMerge(int ax, int ay, int bx, int by)
    {
        CandyTile a = grid[ax, ay];
        CandyTile b = grid[bx, by];

        if (a == null || b == null)
            return false;

        grid[ax, ay] = b;
        grid[bx, by] = a;

        int oldAx = a.x;
        int oldAy = a.y;
        int oldBx = b.x;
        int oldBy = b.y;

        a.x = bx;
        a.y = by;
        b.x = ax;
        b.y = ay;

        bool createsMerge = FindGroupsIncludingCross().Count > 0;

        a.x = oldAx;
        a.y = oldAy;
        b.x = oldBx;
        b.y = oldBy;

        grid[ax, ay] = a;
        grid[bx, by] = b;

        return createsMerge;
    }

    private bool GetHintsEnabled()
    {
        return hintsRuntimeEnabled;
    }

    public void SetHintsEnabled(bool enabled)
    {
        hintsRuntimeEnabled = enabled;

        if (enabled)
        {
            lastHintBoardRevision = -1;
            ResetHintTimer();
        }
        else
        {
            ClearActiveHint();
        }
    }

    public bool AreHintsEnabled()
    {
        return GetHintsEnabled();
    }

    private void RegisterPlayerInteraction()
    {
        lastPlayerInteractionTime = Time.unscaledTime;
        lastHintPulseTime = float.NegativeInfinity;

        if (hasActiveHint)
            ClearActiveHint();
    }

    private void ResetHintTimer(bool clearHint = true)
    {
        lastPlayerInteractionTime = Time.unscaledTime;
        lastHintPulseTime = float.NegativeInfinity;

        if (clearHint)
            ClearActiveHint();
    }

    private void NotifyStableBoardChanged()
    {
        stableBoardRevision++;
        lastHintBoardRevision = -1;
        ResetHintTimer();
    }

    private float GetCurrentHintDelay()
    {
        bool playerHasMoved = GameManager.I != null && GameManager.I.PlayerHasMoved;
        return hintIdleDelay + (playerHasMoved ? 0f : hintExtraDelayBeforeFirstMove);
    }

    private bool CanShowIdleHint()
    {
        if (!useIdleHints || !GetHintsEnabled())
            return false;

        if (gameOver || grid == null)
            return false;

        if (hintSoloOnly && GameManager.I != null && GameManager.I.CurrentPlayType != GameManager.PlayType.Solo)
            return false;

        return true;
    }

    private void HandleIdleHintTick()
    {
        if (!CanShowIdleHint())
        {
            ClearActiveHint();
            return;
        }

        if (busy)
            return;

        float now = Time.unscaledTime;

        if (hasActiveHint)
        {
            if (CountValidMovesFast(1) <= 0)
            {
                ClearActiveHint();
                lastHintBoardRevision = -1;
                return;
            }

            if (hintRepulseInterval > 0f && now - lastHintPulseTime >= hintRepulseInterval)
            {
                ReapplyActiveHintVisuals();
                lastHintPulseTime = now;
            }

            return;
        }

        if (pressing)
            return;

        if (now - lastPlayerInteractionTime < GetCurrentHintDelay())
            return;

        if (lastHintBoardRevision == stableBoardRevision)
            return;

        List<CandyTile> hintTiles = BuildAllHintTiles();
        if (hintTiles.Count == 0)
        {
            lastHintBoardRevision = stableBoardRevision;
            return;
        }

        ShowHintTiles(hintTiles);
        lastHintBoardRevision = stableBoardRevision;
        lastHintPulseTime = now;
    }

    private List<CandyTile> BuildAllHintTiles()
    {
        List<CandyTile> tiles = new List<CandyTile>();

        if (grid == null)
            return tiles;

        int[,] vals = BuildValueGrid();
        HashSet<CandyTile> uniqueTiles = new HashSet<CandyTile>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x + 1 < width && SwapCreatesMatch(vals, x, y, x + 1, y))
                {
                    AddHintTile(uniqueTiles, tiles, grid[x, y]);
                    AddHintTile(uniqueTiles, tiles, grid[x + 1, y]);
                }

                if (y + 1 < height && SwapCreatesMatch(vals, x, y, x, y + 1))
                {
                    AddHintTile(uniqueTiles, tiles, grid[x, y]);
                    AddHintTile(uniqueTiles, tiles, grid[x, y + 1]);
                }
            }
        }

        return tiles;
    }

    private void AddHintTile(HashSet<CandyTile> uniqueTiles, List<CandyTile> tiles, CandyTile tile)
    {
        if (tile == null)
            return;

        if (uniqueTiles.Add(tile))
            tiles.Add(tile);
    }

    private int[,] BuildValueGrid()
    {
        int[,] vals = new int[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                vals[x, y] = grid[x, y] != null ? grid[x, y].Value : 0;
            }
        }

        return vals;
    }

    private void ShowHintTiles(List<CandyTile> tiles)
    {
        ClearActiveHint();

        if (tiles == null || tiles.Count == 0)
            return;

        for (int i = 0; i < tiles.Count; i++)
        {
            CandyTile tile = tiles[i];
            if (tile == null)
                continue;

            activeHintTiles.Add(tile);
            tile.ShowIdleHint(hintHighlightStrength, hintPulseScale, hintPulseDuration, hintPulseCount);
        }

        hasActiveHint = activeHintTiles.Count > 0;
    }

    private void ReapplyActiveHintVisuals()
    {
        if (!hasActiveHint)
            return;

        for (int i = activeHintTiles.Count - 1; i >= 0; i--)
        {
            CandyTile tile = activeHintTiles[i];
            if (tile == null)
            {
                activeHintTiles.RemoveAt(i);
                continue;
            }

            tile.ShowIdleHint(hintHighlightStrength, hintPulseScale, hintPulseDuration, hintPulseCount);
        }

        hasActiveHint = activeHintTiles.Count > 0;
    }

    private void ClearHintVisualsFromEntireBoard()
    {
        if (grid == null)
            return;

        HashSet<CandyTile> cleared = new HashSet<CandyTile>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                CandyTile tile = grid[x, y];
                if (tile == null)
                    continue;

                if (cleared.Add(tile))
                    tile.ClearIdleHint();
            }
        }
    }

    private void ClearActiveHint()
    {
        for (int i = 0; i < activeHintTiles.Count; i++)
        {
            if (activeHintTiles[i] != null)
                activeHintTiles[i].ClearIdleHint();
        }

        ClearHintVisualsFromEntireBoard();

        activeHintTiles.Clear();
        hasActiveHint = false;
    }

    private int PickRefillValue(int x, int y, ref bool helperAvailable)
    {
        if (helperAvailable)
        {
            int helperValue = TryPickDangerHelperValue(x, y);
            if (helperValue > 0)
            {
                helperAvailable = false;
                return ClampGeneratedSpawnValue(helperValue);
            }
        }

        return ClampGeneratedSpawnValue(RandomSpawnValue());
    }

    private int TryPickDangerHelperValue(int x, int y)
    {
        HashSet<int> candidates = new HashSet<int>();

        CollectCandidateValue(candidates, x - 1, y);
        CollectCandidateValue(candidates, x + 1, y);
        CollectCandidateValue(candidates, x, y - 1);
        CollectCandidateValue(candidates, x, y - 2);
        CollectCandidateValue(candidates, x, y + 1);

        int bestValue = 0;
        int bestScore = 0;

        foreach (int value in candidates)
        {
            int score = EvaluateDangerHelperValue(x, y, value);

            if (score > bestScore || (score == bestScore && score > 0 && UnityEngine.Random.value < 0.5f))
            {
                bestScore = score;
                bestValue = value;
            }
        }

        return bestScore > 0 ? bestValue : 0;
    }

    private void CollectCandidateValue(HashSet<int> set, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return;

        CandyTile t = grid[x, y];
        if (t == null || t.Value <= 0)
            return;

        set.Add(t.Value);
    }

    private int EvaluateDangerHelperValue(int x, int y, int value)
    {
        int score = 0;

        int horizontal = 1 + CountSameInDirection(x, y, -1, 0, value) + CountSameInDirection(x, y, 1, 0, value);
        int vertical = 1 + CountSameInDirection(x, y, 0, -1, value) + CountSameInDirection(x, y, 0, 1, value);

        if (horizontal >= 3) score += 100;
        if (vertical >= 3) score += 100;

        if (GetTileValue(x - 1, y) == value) score += 12;
        if (GetTileValue(x + 1, y) == value) score += 12;
        if (GetTileValue(x, y - 1) == value) score += 18;
        if (GetTileValue(x, y - 2) == value) score += 10;
        if (GetTileValue(x, y + 1) == value) score += 6;

        return score;
    }

    private int CountSameInDirection(int x, int y, int dx, int dy, int value)
    {
        int count = 0;

        int cx = x + dx;
        int cy = y + dy;

        while (cx >= 0 && cx < width && cy >= 0 && cy < height)
        {
            CandyTile t = grid[cx, cy];
            if (t == null || t.Value != value)
                break;

            count++;
            cx += dx;
            cy += dy;
        }

        return count;
    }

    private int GetTileValue(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return 0;

        CandyTile t = grid[x, y];
        return t != null ? t.Value : 0;
    }

    private int RandomStartValueWeighted()
    {
        if (!useEarlyGameTuning)
        {
            // Weighted start distribution (values are < 2048)
            int[] values = { 2, 4, 8, 16, 32, 64, 128, 256 };
            float[] weights = { 360f, 260f, 180f, 110f, 60f, 25f, 10f, 5f };
            return WeightedPick(values, weights);
        }

        int[] openingValues = { 2, 4, 8, 16, 32 };
        float[] openingWeights = GetOpeningStartWeights();
        return WeightedPick(openingValues, openingWeights);
    }

    private int PickStartValueForCell(int x, int y)
    {
        // Prevent easy same-value runs on game start (reduces huge adjacent clusters at spawn)
        const int attempts = 12;
        int last = 2;

        for (int i = 0; i < attempts; i++)
        {
            int v = RandomStartValueWeighted();
            last = v;

            if (!WouldCreateMatchLineOf3(x, y, v))
                return v;
        }

        return last;
    }

    private bool WouldCreateMatchLineOf3(int x, int y, int v)
    {
        // Horizontal check: [x-2][x-1][x] all v
        if (x >= 2)
        {
            CandyTile a = grid[x - 1, y];
            CandyTile b = grid[x - 2, y];
            if (a != null && b != null && a.Value == v && b.Value == v)
                return true;
        }

        // Vertical check: [y-2][y-1][y] all v
        if (y >= 2)
        {
            CandyTile a = grid[x, y - 1];
            CandyTile b = grid[x, y - 2];
            if (a != null && b != null && a.Value == v && b.Value == v)
                return true;
        }

        return false;
    }

    private int WeightedPick(int[] values, float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += Mathf.Max(0f, weights[i]);

        float r = UnityEngine.Random.value * total;
        float acc = 0f;

        for (int i = 0; i < values.Length; i++)
        {
            acc += Mathf.Max(0f, weights[i]);
            if (r <= acc) return values[i];
        }

        return values[values.Length - 1];
    }

    private float[] GetOpeningStartWeights()
    {
        return new float[]
        {
            Mathf.Max(0, openingWeight2),
            Mathf.Max(0, openingWeight4),
            Mathf.Max(0, openingWeight8),
            Mathf.Max(0, openingWeight16),
            Mathf.Max(0, openingWeight32)
        };
    }

    private float[] GetCurrentRefillWeights()
    {
        float[] baseWeights = GetBaseSpawnWeights();
        if (!IsEarlyGameActive())
            return baseWeights;

        float[] earlyWeights = new float[]
        {
            Mathf.Max(0, earlyRefillWeight2),
            Mathf.Max(0, earlyRefillWeight4),
            Mathf.Max(0, earlyRefillWeight8),
            Mathf.Max(0, earlyRefillWeight16),
            Mathf.Max(0, earlyRefillWeight32)
        };

        return LerpWeights(earlyWeights, baseWeights, GetEarlyGameProgress01());
    }

    private float[] GetBaseSpawnWeights()
    {
        switch (spawnPreset)
        {
            case SpawnPreset.ClassicHard:
                return new float[] { 0.90f, 0.10f, 0f, 0f, 0f };

            case SpawnPreset.Balanced:
                return new float[] { 0.80f, 0.15f, 0.04f, 0.01f, 0f };

            case SpawnPreset.Rare32:
            default:
                return new float[] { 0.82f, 0.13f, 0.04f, 0.009f, 0.001f };
        }
    }

    private float[] LerpWeights(float[] from, float[] to, float t)
    {
        float[] normalizedFrom = NormalizeWeights(from);
        float[] normalizedTo = NormalizeWeights(to);

        int len = Mathf.Min(normalizedFrom.Length, normalizedTo.Length);
        float[] result = new float[len];

        for (int i = 0; i < len; i++)
            result[i] = Mathf.Lerp(normalizedFrom[i], normalizedTo[i], t);

        return result;
    }

    private float[] NormalizeWeights(float[] weights)
    {
        float total = 0f;

        for (int i = 0; i < weights.Length; i++)
            total += Mathf.Max(0f, weights[i]);

        if (total <= 0f)
        {
            float[] fallback = new float[weights.Length];
            if (weights.Length > 0)
                fallback[0] = 1f;
            return fallback;
        }

        float[] result = new float[weights.Length];
        for (int i = 0; i < weights.Length; i++)
            result[i] = Mathf.Max(0f, weights[i]) / total;

        return result;
    }

    private bool IsEarlyGameActive()
    {
        return useEarlyGameTuning && earlyGameMoveWindow > 0 && successfulMovesThisRun < earlyGameMoveWindow;
    }

    private float GetEarlyGameProgress01()
    {
        if (!useEarlyGameTuning || earlyGameMoveWindow <= 0)
            return 1f;

        return Mathf.Clamp01((float)successfulMovesThisRun / Mathf.Max(1, earlyGameMoveWindow));
    }

    private int GetOpeningMinimumValidMoves()
    {
        return useEarlyGameTuning ? Mathf.Max(1, openingMinValidMoves) : 1;
    }

    private float GetAverageTileExponent()
    {
        if (grid == null) return 1f;

        int count = 0;
        float sum = 0f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CandyTile t = grid[x, y];
                if (t == null) continue;

                int v = t.Value;
                if (v <= 0) continue;

                sum += Log2Int(v);
                count++;
            }
        }

        return count > 0 ? (sum / count) : 1f;
    }

    private int Log2Int(int v)
    {
        int e = 0;
        while (v > 1)
        {
            v >>= 1;
            e++;
        }
        return e;
    }

    private void SpawnAt(int x, int y, int value, bool instant)
    {
        if (tilePrefab == null) return;
        if (tilesRoot == null) tilesRoot = transform;

        Vector3 world = GridToWorld(x, y);

        var t = Instantiate(tilePrefab, world, Quaternion.identity, tilesRoot);
        t.Init(this, x, y, value);
        grid[x, y] = t;

        t.RefreshColor();
        ApplyTileLabelRotation(t);

        if (instant) t.SetWorldPosInstant(world);
        else t.MoveToWorld(world, DurationForFall());
    }

    private float DurationForFall() => 0.20f;

    // --------------------------
    // Resolve loop
    // --------------------------
    private IEnumerator ResolveLoop(bool scoreThisResolve)
    {
        yield return ResolveLoop(
            scoreThisResolve: scoreThisResolve,
            animate: true,
            allowMilestoneCascadeScore: false
        );
    }

    private IEnumerator ResolveLoop(bool scoreThisResolve, bool animate)
    {
        yield return ResolveLoop(
            scoreThisResolve: scoreThisResolve,
            animate: animate,
            allowMilestoneCascadeScore: false
        );
    }

    private IEnumerator ResolveLoop(bool scoreThisResolve, bool animate, bool allowMilestoneCascadeScore)
    {
        int safety = 0;
        bool scoreCurrentPass = scoreThisResolve;

        while (true)
        {
            if (++safety > 70) break;

            var groups = FindGroupsIncludingCross();
            if (groups.Count == 0) break;

            ApplyMerges(groups, scoreCurrentPass, allowMilestoneCascadeScore);

            scoreCurrentPass = false;

            yield return null;

            if (animate)
            {
                ApplyGravityAnimated();
                yield return new WaitForSeconds(DurationForFall());
                SnapAllTilesToGridInstant();

                RefillEmptyAnimated();
                yield return new WaitForSeconds(DurationForFall());
                SnapAllTilesToGridInstant();
            }
            else
            {
                ApplyGravityInstant();
                SnapAllTilesToGridInstant();

                RefillEmptyInstant();
                SnapAllTilesToGridInstant();
            }
        }

        SnapAllTilesToGridInstant();

        if (scoreThisResolve)
        {
            if (!HasAnyValidMove()) EndGameNoMoves();
        }
    }


    private void ApplyMerges(List<Group> groups, bool scoreThisResolve, bool allowMilestoneCascadeScore)
    {
        var removed = new HashSet<CandyTile>();
        var usedCenter = new HashSet<CandyTile>();

        foreach (var g in groups)
        {
            if (g.center == null) continue;
            if (usedCenter.Contains(g.center)) continue;
            usedCenter.Add(g.center);

            int x = g.value;
            int n = Mathf.Max(1, g.count);
            long newValueLong = (long)x << (n - 1);

            if (newValueLong > int.MaxValue)
                newValueLong = int.MaxValue;

            int newValue = (int)newValueLong;

            foreach (var t in g.tiles)
            {
                if (t == null) continue;
                if (t == g.center) continue;
                if (removed.Contains(t)) continue;

                removed.Add(t);

                if (grid != null)
                    grid[t.x, t.y] = null;

                SpawnMergeGhost(t);
                Destroy(t.gameObject);
            }

            if (g.center == null || removed.Contains(g.center)) continue;

            g.center.SetValue(newValue);

            if (newValue < 2048)
                AudioManager.I?.PlayLayered(SfxId.MergeCrack, SfxId.MergeBody);

            bool is2048Plus = newValue >= 2048;
            bool shouldScoreMilestone = allowMilestoneCascadeScore && is2048Plus;
            bool shouldScore = scoreThisResolve || is2048Plus;

            if (shouldScore)
            {
                GameManager.I?.AddScore(newValue, ignorePlayerMovedCheck: is2048Plus);
            }
            else if (shouldScoreMilestone)
            {
                GameManager.I?.AddScore(newValue, ignorePlayerMovedCheck: true);
            }

            var centerSr = g.center.spriteRenderer != null
                ? g.center.spriteRenderer
                : g.center.GetComponent<SpriteRenderer>();

            if (centerSr != null)
            {
                SpawnMergeSparkles(g.center.transform.position, centerSr.color, newValue);
            }

            if (newValue >= 2048)
            {
                AudioManager.I?.PlayLayered(SfxId.Merge2048Sparkle, SfxId.Merge2048Air);

                var sr = g.center.spriteRenderer != null
                    ? g.center.spriteRenderer
                    : g.center.GetComponent<SpriteRenderer>();

                if (sr != null)
                {
                    SpawnMergeFirework(g.center.transform.position, sr.color);
                }

                grid[g.center.x, g.center.y] = null;
                SpawnMergeGhost(g.center);
                Destroy(g.center.gameObject);

                AudioManager.I?.PlayLayered(SfxId.Merge2048Sparkle, SfxId.Merge2048Air);
                ThemeManager.I?.NotifyValueCreated(newValue);
                StartCoroutine(RefreshTilesNextFrame());
            }
        }
    }


    private void ApplyGravityAnimated()
    {
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int y = 0; y < height; y++)
            {
                var t = grid[x, y];
                if (t == null) continue;

                if (y != writeY)
                {
                    grid[x, writeY] = t;
                    grid[x, y] = null;

                    t.x = x;
                    t.y = writeY;

                    t.MoveToWorld(GridToWorld(x, writeY), DurationForFall());
                }

                writeY++;
            }
        }
    }

    private void ApplyGravityInstant()
    {
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;

            for (int y = 0; y < height; y++)
            {
                CandyTile t = grid[x, y];
                if (t == null) continue;

                if (y != writeY)
                {
                    grid[x, writeY] = t;
                    grid[x, y] = null;

                    t.x = x;
                    t.y = writeY;

                    t.SetWorldPosInstant(GridToWorld(x, writeY));
                }

                writeY++;
            }
        }
    }

    private void RefillEmptyAnimated()
    {
        bool helperAvailable = ShouldUseDangerHelperSpawn();

        if (grid == null) return;
        if (tilesRoot == null) tilesRoot = transform;

        for (int x = 0; x < width; x++)
        {
            int spawnOffset = 0;

            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null) continue;

                int v = PickRefillValue(x, y, ref helperAvailable);

                // Each new tile in the same column starts higher than the previous one.
                Vector3 spawnWorld = GridToWorld(x, height + 2 + spawnOffset);
                Vector3 targetWorld = GridToWorld(x, y);

                var t = Instantiate(tilePrefab, spawnWorld, Quaternion.identity, tilesRoot);
                t.Init(this, x, y, v);
                grid[x, y] = t;

                t.RefreshColor();
                ApplyTileLabelRotation(t);

                t.SetWorldPosInstant(spawnWorld);
                t.MoveToWorld(targetWorld, DurationForFall());

                spawnOffset++;
            }
        }
    }

    private void RefillEmptyInstant()
    {
        bool helperAvailable = ShouldUseDangerHelperSpawn();

        if (grid == null) return;
        if (tilesRoot == null) tilesRoot = transform;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null) continue;

                int v = PickRefillValue(x, y, ref helperAvailable);

                Vector3 world = GridToWorld(x, y);

                var t = Instantiate(tilePrefab, world, Quaternion.identity, tilesRoot);
                t.Init(this, x, y, v);
                grid[x, y] = t;

                t.RefreshColor();
                ApplyTileLabelRotation(t);
                t.SetWorldPosInstant(world);
            }
        }
    }

    // --------------------------
    // Matching
    // --------------------------
    private class Group
    {
        public int value;
        public int count;
        public CandyTile center;
        public List<CandyTile> tiles = new List<CandyTile>();
    }

    private List<Group> FindGroupsIncludingCross()
    {
        var groups = new List<Group>();
        if (grid == null) return groups;

        bool[,] horiz = new bool[width, height];
        bool[,] vert = new bool[width, height];
        bool[,] match = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            int x = 0;
            while (x < width)
            {
                var t = grid[x, y];
                if (t == null) { x++; continue; }

                int v = t.Value;
                int start = x;
                int count = 1;

                int xx = x + 1;
                while (xx < width && grid[xx, y] != null && grid[xx, y].Value == v)
                {
                    count++;
                    xx++;
                }

                if (count >= 3)
                {
                    for (int k = 0; k < count; k++)
                    {
                        horiz[start + k, y] = true;
                        match[start + k, y] = true;
                    }
                }

                x = start + count;
            }
        }

        for (int x = 0; x < width; x++)
        {
            int y = 0;
            while (y < height)
            {
                var t = grid[x, y];
                if (t == null) { y++; continue; }

                int v = t.Value;
                int start = y;
                int count = 1;

                int yy = y + 1;
                while (yy < height && grid[x, yy] != null && grid[x, yy].Value == v)
                {
                    count++;
                    yy++;
                }

                if (count >= 3)
                {
                    for (int k = 0; k < count; k++)
                    {
                        vert[x, start + k] = true;
                        match[x, start + k] = true;
                    }
                }

                y = start + count;
            }
        }

        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                var a = grid[x, y];
                var b = grid[x + 1, y];
                var c = grid[x, y + 1];
                var d = grid[x + 1, y + 1];

                if (a == null || b == null || c == null || d == null) continue;

                int v = a.Value;
                if (b.Value != v || c.Value != v || d.Value != v) continue;

                match[x, y] = true;
                match[x + 1, y] = true;
                match[x, y + 1] = true;
                match[x + 1, y + 1] = true;
            }
        }

        bool[,] visited = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!match[x, y] || visited[x, y]) continue;

                CandyTile seed = grid[x, y];
                if (seed == null) { visited[x, y] = true; continue; }

                int v = seed.Value;

                var q = new Queue<CandyTile>();
                var list = new List<CandyTile>();

                q.Enqueue(seed);
                visited[x, y] = true;

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    list.Add(cur);

                    Try(cur.x + 1, cur.y);
                    Try(cur.x - 1, cur.y);
                    Try(cur.x, cur.y + 1);
                    Try(cur.x, cur.y - 1);

                    void Try(int nx, int ny)
                    {
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) return;
                        if (visited[nx, ny]) return;
                        if (!match[nx, ny]) return;

                        var nt = grid[nx, ny];
                        if (nt == null) { visited[nx, ny] = true; return; }
                        if (nt.Value != v) return;

                        visited[nx, ny] = true;
                        q.Enqueue(nt);
                    }
                }

                if (list.Count < 3) continue;

                CandyTile center = null;

                for (int i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    if (horiz[t.x, t.y] && vert[t.x, t.y])
                    {
                        center = t;
                        break;
                    }
                }

                if (center == null)
                {
                    bool hasHoriz = false;
                    bool hasVert = false;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var t = list[i];
                        if (horiz[t.x, t.y]) hasHoriz = true;
                        if (vert[t.x, t.y]) hasVert = true;
                    }

                    if (hasHoriz && !hasVert)
                    {
                        list.Sort((a, b) => a.x.CompareTo(b.x));
                        center = list[list.Count / 2];
                    }
                    else if (hasVert && !hasHoriz)
                    {
                        list.Sort((a, b) => a.y.CompareTo(b.y));
                        center = list[list.Count / 2];
                    }
                    else
                    {
                        float ax = 0f, ay = 0f;
                        for (int i = 0; i < list.Count; i++)
                        {
                            ax += list[i].x;
                            ay += list[i].y;
                        }
                        ax /= list.Count;
                        ay /= list.Count;

                        CandyTile best = list[0];
                        float bestD = float.MaxValue;

                        for (int i = 0; i < list.Count; i++)
                        {
                            float dx = list[i].x - ax;
                            float dy = list[i].y - ay;
                            float d = dx * dx + dy * dy;
                            if (d < bestD)
                            {
                                bestD = d;
                                best = list[i];
                            }
                        }

                        center = best;
                    }
                }

                groups.Add(new Group
                {
                    value = v,
                    count = list.Count,
                    tiles = list,
                    center = center
                });
            }
        }

        return groups;
    }

    private bool HasAnyValidMove()
    {
        if (grid == null) return false;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (grid[x, y] == null)
                    return true;

        int[,] vals = new int[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                vals[x, y] = grid[x, y].Value;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x + 1 < width)
                {
                    if (SwapCreatesMatch(vals, x, y, x + 1, y))
                        return true;
                }

                if (y + 1 < height)
                {
                    if (SwapCreatesMatch(vals, x, y, x, y + 1))
                        return true;
                }
            }
        }

        return false;
    }

    private bool SwapCreatesMatch(int[,] vals, int x1, int y1, int x2, int y2)
    {
        int a = vals[x1, y1];
        int b = vals[x2, y2];
        vals[x1, y1] = b;
        vals[x2, y2] = a;

        bool ok = HasMatchAt(vals, x1, y1) || HasMatchAt(vals, x2, y2);

        vals[x1, y1] = a;
        vals[x2, y2] = b;

        return ok;
    }

    private bool HasMatchAt(int[,] vals, int x, int y)
    {
        int v = vals[x, y];
        if (v <= 0) return false;

        int count = 1;
        int lx = x - 1;
        while (lx >= 0 && vals[lx, y] == v) { count++; lx--; }
        int rx = x + 1;
        while (rx < width && vals[rx, y] == v) { count++; rx++; }
        if (count >= 3) return true;

        count = 1;
        int dy = y - 1;
        while (dy >= 0 && vals[x, dy] == v) { count++; dy--; }
        int uy = y + 1;
        while (uy < height && vals[x, uy] == v) { count++; uy++; }
        return count >= 3;
    }

    private void EndGameNoMoves()
    {
        ClearActiveHint();
        gameOver = true;
        GameManager.I?.GameOver();
    }

    private void SaveUndoSnapshot()
    {
        lastUndoSnap = ExportState();
        hasUndoSnap = (lastUndoSnap != null);
    }

    [Serializable]
    public class BoardState
    {
        public int w;
        public int h;
        public int[] values;
        public int currentPlayer;
        public int successfulMoves;

        // Score snapshot for Undo
        public long soloScore;
        public long p1Score;
        public long p2Score;
    }

    public void ForceRefreshAllColorsInstant()
    {
        if (grid == null) return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                    grid[x, y].RefreshColor();
            }
        }

        if (hasActiveHint)
            ReapplyActiveHintVisuals();
    }

    private void EnsureMinimumValidMoves(int minValidMoves)
    {
        minValidMoves = Mathf.Max(1, minValidMoves);

        const int maxAttempts = 30;
        int attempt = 0;

        while (CountValidMovesFast(minValidMoves) < minValidMoves && attempt < maxAttempts)
        {
            ShuffleBoard();
            attempt++;
        }
    }

    private void EnsureAtLeastOneMove()
    {
        EnsureMinimumValidMoves(1);
    }

    private void ShuffleBoard()
    {
        List<CandyTile> tiles = new List<CandyTile>();

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (grid[x, y] != null)
                    tiles.Add(grid[x, y]);

        List<int> values = new List<int>();
        foreach (var t in tiles)
            values.Add(t.Value);

        for (int i = 0; i < values.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, values.Count);
            int tmp = values[i];
            values[i] = values[rnd];
            values[rnd] = tmp;
        }

        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y].SetValue(values[index]);
                    index++;
                }
            }
        }
    }

    private void SwitchTurn()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;

        isPlayer1Turn = (currentPlayer == 1);
        ApplyTurnView();
        SnapAllTilesToGridInstant();
    }

    private void ApplyTurnView()
    {
        if (boardRoot == null) return;
        if (grid == null) return;

        float targetZ = isPlayer1Turn ? 0f : 180f;
        Quaternion rot = Quaternion.Euler(0f, 0f, targetZ);
        boardRoot.rotation = rot;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                    grid[x, y].SetLabelRotation(rot);
            }
        }
    }

    private void ApplyTileLabelRotation(CandyTile tile)
    {
        if (tile == null) return;

        float targetZ = isPlayer1Turn ? 0f : 180f;
        Quaternion rot = Quaternion.Euler(0f, 0f, targetZ);
        tile.SetLabelRotation(rot);
    }

    private void ApplyModeVisuals(GameManager.PlayType playType)
    {
        if (playType == GameManager.PlayType.Solo)
        {
            currentPlayer = 1;
            isPlayer1Turn = true;

            if (boardRoot != null)
                boardRoot.rotation = Quaternion.identity;

            if (grid != null)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (grid[x, y] != null)
                            grid[x, y].SetLabelRotation(Quaternion.identity);
                    }
                }
            }
        }
        else
        {
            isPlayer1Turn = (currentPlayer == 1);
            ApplyTurnView();
        }
    }

    private void HardResetRuntimeState()
    {
        ClearActiveHint();
        StopAllCoroutines();
        busy = false;
        gameOver = false;

        hasUndoSnap = false;
        lastUndoSnap = null;
        successfulMovesThisRun = 0;

        pressedTile = null;
        pressing = false;

        ResetHintTimer(clearHint: false);
    }

    private void SnapAllTilesToGridInstant()
    {
        if (grid == null) return;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var t = grid[x, y];
                if (t == null) continue;

                t.SetWorldPosInstant(GridToWorld(x, y));
                ApplyTileLabelRotation(t);
            }
        }
    }

    private void SpawnMergeGhost(CandyTile tile)
    {
        if (mergeGhostPrefab == null) return;
        if (tile == null) return;

        SpriteRenderer sr = tile.spriteRenderer != null
            ? tile.spriteRenderer
            : tile.GetComponent<SpriteRenderer>();

        if (sr == null) return;

        int extra = tile.Value >= 2048 ? 1 : 0;
        int count = Mathf.Clamp(mergeGhostBurstCount + extra, 1, mergeGhostBurstCap);

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * mergeGhostSpawnRadius;
            Vector3 pos = tile.transform.position + new Vector3(offset.x, offset.y, 0f);

            GameObject ghostObj = Instantiate(mergeGhostPrefab, pos, Quaternion.identity);

            MergeGhost ghost = ghostObj.GetComponent<MergeGhost>();
            if (ghost != null)
                ghost.Init(sr.sprite, sr.color, tile.Value);
        }
    }

    [SerializeField] private GameObject mergeSparklePrefab;
    [SerializeField] private int sparkleCount = 6;
    [SerializeField] private int sparkleCount2048Plus = 10;
    [SerializeField] private float sparkleSpawnRadius = 0.10f;

    private void SpawnMergeSparkles(Vector3 worldPos, Color tileColor, int mergedValue)
    {
        if (mergeSparklePrefab == null) return;

        bool is2048Plus = mergedValue >= 2048;
        int count = is2048Plus ? sparkleCount2048Plus : sparkleCount;
        count = Mathf.Clamp(count, 0, 14);

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * sparkleSpawnRadius;
            Vector3 pos = worldPos + new Vector3(offset.x, offset.y, 0f);

            GameObject obj = Instantiate(mergeSparklePrefab, pos, Quaternion.identity);

            MergeSparkle sp = obj.GetComponent<MergeSparkle>();
            if (sp != null)
                sp.Init(tileColor, is2048Plus);
        }
    }

    [SerializeField] private GameObject mergeFireworkPrefab;
    [SerializeField] private int fireworkCount = 6;
    [SerializeField] private float fireworkSpawnRadius = 0.08f;
    [SerializeField] private float minFireworkSpeed = 9.0f;
    [SerializeField] private float maxFireworkSpeed = 13.0f;

    private void SpawnMergeFirework(Vector3 worldPos, Color color)
    {
        if (mergeFireworkPrefab == null) return;

        int baseCount = Mathf.Clamp(fireworkCount, 1, 18);
        int count = Mathf.Clamp(Mathf.CeilToInt(baseCount * 1.5f), 1, 24);

        float angleOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * fireworkSpawnRadius;
            Vector3 pos = worldPos + new Vector3(offset.x, offset.y, 0f);

            GameObject obj = Instantiate(mergeFireworkPrefab, pos, Quaternion.identity);

            MergeFirework fw = obj.GetComponent<MergeFirework>();
            if (fw == null) continue;

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            Sprite sprite = sr != null ? sr.sprite : null;

            float ang = angleOffset + (i * (Mathf.PI * 2f / count));
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            float spd = UnityEngine.Random.Range(minFireworkSpeed, maxFireworkSpeed);
            fw.Init(sprite, color, dir, spd);
        }
    }

    private IEnumerator RefreshTilesNextFrame()
    {
        yield return null;
        ThemeManager.I?.RefreshAllTiles();
    }

    private void LateUpdate()
    {
        if (!autoFitCameraToBoard) return;

        if (Screen.width != lastScreenW || Screen.height != lastScreenH)
        {
            lastScreenW = Screen.width;
            lastScreenH = Screen.height;
            FitCameraToBoard();
        }
    }

    private void FitCameraToBoard()
    {
        Camera cam = GetCam();
        if (cam == null) return;

        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);

        float halfW = ((width - 1) * cellSize) * 0.5f + tileWorldSize * 0.5f + cameraPadding;
        float halfH = ((height - 1) * cellSize) * 0.5f + tileWorldSize * 0.5f + cameraPadding;

        float requiredOrthoSize = Mathf.Max(halfH, halfW / aspect);

        if (!cam.orthographic) cam.orthographic = true;
        cam.orthographicSize = requiredOrthoSize;

        Vector3 camPos = cam.transform.position;
        Vector3 center = tilesRoot != null ? tilesRoot.position : transform.position;
        camPos.x = center.x;
        camPos.y = center.y;
        cam.transform.position = camPos;
    }

    public void PrepareBoardForSave()
    {
        ClearActiveHint();
        StopAllCoroutines();
        busy = false;
        pressedTile = null;
        pressing = false;

        NormalizeBoardInstantNoScore();
        SnapAllTilesToGridInstant();
    }

    private void NormalizeBoardInstantNoScore()
    {
        if (grid == null) return;

        int safety = 0;

        while (safety++ < 70)
        {
            bool hasEmpty = false;

            for (int y = 0; y < height && !hasEmpty; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[x, y] == null)
                    {
                        hasEmpty = true;
                        break;
                    }
                }
            }

            if (hasEmpty)
            {
                ApplyGravityInstant();
                SnapAllTilesToGridInstant();

                RefillEmptyInstant();
                SnapAllTilesToGridInstant();
            }

            var groups = FindGroupsIncludingCross();
            if (groups.Count == 0)
            {
                if (!hasEmpty) break;
                continue;
            }

            ApplyMerges(groups, false, false);
            SnapAllTilesToGridInstant();
        }

        SnapAllTilesToGridInstant();
    }

}