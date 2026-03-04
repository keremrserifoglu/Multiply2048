using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public enum PlayType { Solo, Versus1v1 }
    public PlayType CurrentPlayType { get; private set; } = PlayType.Solo;

    [Header("Scene Roots")]
    public GameObject gameBoardRoot;
    public BoardController board;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;


    [Header("Panels - Game Over Ad Offer")]
    public GameObject gameOverAdPanel;
    public Button gameOverAdWatchAdButton;
    public Button gameOverAdCloseButton;
    public Image gameOverAdTimeFill;
    public TMP_Text gameOverAdTimeText;

    [Tooltip("How many seconds the player has to choose to watch an ad before the normal Game Over screen is shown.")]
    public float gameOverAdOfferSeconds = 5f;
    [Header("Texts - Main Menu")]
    public TMP_Text totalScoreText;
    public TMP_Text maxScoreText;

    [Header("Texts - HUD")]
    public TMP_Text scoreText;
    public TMP_Text undoText;
    public Button shuffleButton;
    public TMP_Text shuffleText;
    public TMP_Text player1ScoreText;
    public TMP_Text player2ScoreText;

    [Header("Texts - Game Over")]
    public TMP_Text gameOverScoreText;
    public TMP_Text gameOverMaxScoreText;
    public TMP_Text winnerText;

    [Header("Undo")]
    public int startingUndoCredits = 3;
    public Button undoButton;
    public bool unlimitedUndoForTesting = true;

    [Header("Shuffle")]
    public int startingShuffleCredits = 3;
    public bool unlimitedShuffleForTesting = true;

    [Header("Limited Credits Panel")]
    public GameObject limitedCreditsPanel;
    public TMP_Text limitedCreditsInfoText;
    public Button limitedCreditsWatchAdButton;
    public Button limitedCreditsCloseButton;

    [Header("Credit Regen")]
    [Tooltip("One credit is added every X minutes, even while the app is closed.")]
    public int creditRegenMinutes = 15;

    [Tooltip("Optional cap to stop credits growing forever. Set to 0 for no cap.")]
    public int maxCreditsCap = 0;

    private long lastRunScore;

    public long Score { get; private set; }
    public long TotalScore { get; private set; }
    public long MaxScore { get; private set; }

    public int UndoCredits { get; private set; }
    public int ShuffleCredits { get; private set; }

    // 1v1 scores
    private long player1Score;
    private long player2Score;

    public bool PlayerHasMoved { get; private set; }

    // Per-mode runtime state (in-memory)
    private BoardController.BoardState soloBoardState;
    private long soloScore;
    private bool soloPlayerHasMoved;
    private bool soloHasState;

    private BoardController.BoardState versusBoardState;
    private long versusP1Score;
    private long versusP2Score;
    private bool versusPlayerHasMoved;
    private bool versusHasState;

    private enum CreditType { Undo, Shuffle }
    private CreditType lastRequestedCreditType = CreditType.Undo;

    private float nextCreditTick;



    // GameOver ad offer runtime
    private float gameOverAdRemaining;
    private bool gameOverAdOfferActive;

    // Snapshot taken at the moment the board reports Game Over (used to restore after rewarded ad)
    private BoardController.BoardState gameOverSnapshotState;
    private long gameOverSnapshotSoloScore;
    private long gameOverSnapshotP1Score;
    private long gameOverSnapshotP2Score;
    private bool gameOverSnapshotPlayerHasMoved;
    private const string PP_TOTAL = "TOTAL_SCORE_STR";
    private const string PP_MAX = "MAX_SCORE_STR";

    private const string PP_UNDO = "UNDO_CREDITS";
    private const string PP_SHUFFLE = "SHUFFLE_CREDITS";
    private const string PP_LAST_GRANT_UTC = "CREDITS_LAST_GRANT_UTC";

    // Persistent board save (survives app close)
    private const string PP_SOLO_STATE = "SOLO_BOARD_STATE_JSON";
    private const string PP_VERSUS_STATE = "VERSUS_BOARD_STATE_JSON";

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        LoadMetaScores();

        UndoCredits = PlayerPrefs.GetInt(PP_UNDO, startingUndoCredits);
        ShuffleCredits = PlayerPrefs.GetInt(PP_SHUFFLE, startingShuffleCredits);

        // Apply offline/online credit regen before showing UI
        RefreshTimedCredits();

        // Load persisted board states (if any) into memory
        LoadPersistentBoardStates();

        ShowMainMenu();

        if (winnerText) winnerText.text = "";

        HookLimitedCreditsPanelButtons();
        EnsureLimitedCreditsPanelUnderSafeArea();

        HideLimitedCreditsPanel();

        HookGameOverAdPanelButtons();
        EnsureGameOverAdPanelUnderSafeArea();
        HideGameOverAdPanel();

        UpdateUI();
    }

    private void HookLimitedCreditsPanelButtons()
    {
        if (limitedCreditsWatchAdButton)
        {
            limitedCreditsWatchAdButton.onClick.RemoveListener(OnLimitedCreditsWatchAdPressed);
            limitedCreditsWatchAdButton.onClick.AddListener(OnLimitedCreditsWatchAdPressed);
        }
        if (limitedCreditsCloseButton)
        {
            limitedCreditsCloseButton.onClick.RemoveListener(HideLimitedCreditsPanel);
            limitedCreditsCloseButton.onClick.AddListener(HideLimitedCreditsPanel);
        }
    }

    private void EnsureLimitedCreditsPanelUnderSafeArea()
    {
        if (limitedCreditsPanel == null) return;

        RectTransform panelRt = limitedCreditsPanel.GetComponent<RectTransform>();
        if (panelRt == null) return;

        SafeAreaFitter safeArea = null;
#if UNITY_2023_1_OR_NEWER
        safeArea = UnityEngine.Object.FindFirstObjectByType<SafeAreaFitter>();
#else
        safeArea = FindObjectOfType<SafeAreaFitter>();
#endif
        if (safeArea == null)
        {
            // Try to locate inactive objects too (works in older Unity versions)
            SafeAreaFitter[] all = Resources.FindObjectsOfTypeAll<SafeAreaFitter>();
            if (all != null && all.Length > 0) safeArea = all[0];
        }
        if (safeArea == null) return;

        Transform safeParent = safeArea.transform;
        if (panelRt.parent != safeParent)
        {
            panelRt.SetParent(safeParent, false);
        }

        // Stretch to fill the safe area by default; you can adjust later in the scene
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
    }

    // Public helpers for BoardController snapshots
    public long GetPlayer1Score() => player1Score;
    public long GetPlayer2Score() => player2Score;

    public void SetVersusScores(long p1, long p2)
    {
        player1Score = p1;
        player2Score = p2;
        UpdateUI();
    }

    // -----------------------------------------------------
    // UI BUTTONS
    // -----------------------------------------------------

    public void StartSolo()
    {
        AudioManager.I?.Play(SfxId.MenuModeSelect);
        CurrentPlayType = PlayType.Solo;
        StartOrResume();
    }

    public void Start1v1()
    {
        AudioManager.I?.Play(SfxId.MenuModeSelect);
        CurrentPlayType = PlayType.Versus1v1;
        StartOrResume();
    }

    public void RestartSameMode() => ForceNewGame();

    public void PlayAgain() => ForceNewGame();

    public void ReturnToMainMenu()
    {
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        // Save current run if not ended
        if (board != null)
        {
            bool ended = board.IsGameOver;
            if (!ended)
            {
                SaveRuntimeStateForCurrentMode();
                SavePersistentStateForCurrentMode();
            }
            else
            {
                ClearRuntimeStateForCurrentMode();
                ClearPersistentStateForCurrentMode();
                board.ResetBoardForMenu();
            }
        }

        if (gameBoardRoot) gameBoardRoot.SetActive(false);
        if (hudPanel) hudPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);

        UpdateUI();
    }

    public void ShufflePressed()
    {
        if (CurrentPlayType != PlayType.Solo) return;

        RefreshTimedCredits();

        if (!unlimitedShuffleForTesting && ShuffleCredits <= 0)
        {
            ShowLimitedCreditsPanel(CreditType.Shuffle);
            return;
        }

        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        if (board == null) return;

        board.TryShuffle();

        if (!unlimitedShuffleForTesting)
        {
            ShuffleCredits--;
            PersistCredits();
        }

        // Save after shuffle so it persists across app close
        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();
        UpdateUI();
    }

    public void UndoPressed()
    {
        // Must have made a move first
        if (!PlayerHasMoved) return;

        RefreshTimedCredits();

        if (!unlimitedUndoForTesting && UndoCredits <= 0)
        {
            ShowLimitedCreditsPanel(CreditType.Undo);
            return;
        }

        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null) return;
        board.TryUndoLastMove();
        if (!unlimitedUndoForTesting)
        {
            UndoCredits--;
            PersistCredits();
        }

        // Prevent back-to-back undo
        PlayerHasMoved = false;

        // Persist after undo
        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();
        UpdateUI();
    }

    // -----------------------------------------------------
    // GAME FLOW
    // -----------------------------------------------------

    private void StartOrResume()
    {
        if (gameBoardRoot) gameBoardRoot.SetActive(true);
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        bool hasSaved = (CurrentPlayType == PlayType.Solo) ? soloHasState : versusHasState;

        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Solo-only Undo
        bool allowUndo = (CurrentPlayType == PlayType.Solo);
        if (undoButton) undoButton.gameObject.SetActive(allowUndo);
        if (undoText) undoText.gameObject.SetActive(allowUndo);

        // Solo-only Shuffle
        bool allowShuffle = (CurrentPlayType == PlayType.Solo);
        if (shuffleButton) shuffleButton.gameObject.SetActive(allowShuffle);
        if (shuffleText) shuffleText.gameObject.SetActive(allowShuffle);

        // 1v1: show player score texts, hide single scoreText
        if (scoreText) scoreText.gameObject.SetActive(CurrentPlayType == PlayType.Solo);
        if (player1ScoreText) player1ScoreText.gameObject.SetActive(CurrentPlayType == PlayType.Versus1v1);
        if (player2ScoreText) player2ScoreText.gameObject.SetActive(CurrentPlayType == PlayType.Versus1v1);

        if (!hasSaved)
        {
            // Fresh run for this mode
            if (CurrentPlayType == PlayType.Solo)
            {
                Score = 0;
                PlayerHasMoved = false;
                board.NewGame(CurrentPlayType);
            }
            else
            {
                player1Score = 0;
                player2Score = 0;
                PlayerHasMoved = false;
                board.NewGame(CurrentPlayType);
            }

            // Persist fresh state as well
            SaveRuntimeStateForCurrentMode();
            SavePersistentStateForCurrentMode();
        }
        else
        {
            // Restore runtime state for this mode
            RestoreRuntimeStateForCurrentMode();
            board.ResumeGame(CurrentPlayType);
        }

        UpdateUI();
    }

    private void ForceNewGame()
    {
        if (gameBoardRoot) gameBoardRoot.SetActive(true);
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        // Wipe saved runtime + persistent for this mode
        ClearRuntimeStateForCurrentMode();
        ClearPersistentStateForCurrentMode();

        Score = 0;
        PlayerHasMoved = false;

        if (CurrentPlayType == PlayType.Versus1v1)
        {
            player1Score = 0;
            player2Score = 0;
        }

        ThemeManager.I?.ResetTheme();
        board.NewGame(CurrentPlayType);

        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (hudPanel) hudPanel.SetActive(true);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        bool allowUndo = (CurrentPlayType == PlayType.Solo);
        if (undoButton) undoButton.gameObject.SetActive(allowUndo);
        if (undoText) undoText.gameObject.SetActive(allowUndo);

        bool allowShuffle = (CurrentPlayType == PlayType.Solo);
        if (shuffleButton) shuffleButton.gameObject.SetActive(allowShuffle);
        if (shuffleText) shuffleText.gameObject.SetActive(allowShuffle);

        if (scoreText) scoreText.gameObject.SetActive(CurrentPlayType == PlayType.Solo);
        if (player1ScoreText) player1ScoreText.gameObject.SetActive(CurrentPlayType == PlayType.Versus1v1);
        if (player2ScoreText) player2ScoreText.gameObject.SetActive(CurrentPlayType == PlayType.Versus1v1);

        // Persist the new run
        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();

        UpdateUI();
    }

    public void MarkPlayerMoved() => PlayerHasMoved = true;

    public void SetScore(long v)
    {
        Score = v;
        UpdateUI();
    }

    public void SetPlayerHasMoved(bool v) => PlayerHasMoved = v;

    public void AddScore(long amount)
    {
        amount *= 2; // x2 score

        if (!PlayerHasMoved) return;

        if (CurrentPlayType == PlayType.Versus1v1)
        {
            int p = (board != null) ? board.ScoringPlayer : 1;
            if (p == 1) player1Score += amount;
            else player2Score += amount;
        }
        else
        {
            Score += amount;
        }

        UpdateUI();

        // Persist score changes in case the app closes unexpectedly
        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();
    }


    public void GameOver()
    {
        // If the ad-offer panel is not set up in the scene yet, fall back to the classic behavior.
        if (!gameOverAdPanel)
        {
            ConfirmGameOverAndShowPanel();
            return;
        }

        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null)
        {
            ConfirmGameOverAndShowPanel();
            return;
        }

        // Take a snapshot so we can restore and continue if the player watches a rewarded ad.
        gameOverSnapshotState = board.ExportState();
        gameOverSnapshotPlayerHasMoved = PlayerHasMoved;

        if (CurrentPlayType == PlayType.Solo)
        {
            gameOverSnapshotSoloScore = Score;
        }
        else
        {
            gameOverSnapshotP1Score = player1Score;
            gameOverSnapshotP2Score = player2Score;
        }

        ShowGameOverAdPanel();
    }

    private void ConfirmGameOverAndShowPanel()
    {
        long runScore = (CurrentPlayType == PlayType.Versus1v1) ? (player1Score + player2Score) : Score;
        lastRunScore = runScore;

        if (CurrentPlayType == PlayType.Solo)
        {
            TotalScore += runScore;
            if (runScore > MaxScore) MaxScore = runScore;
            PlayerPrefs.SetString(PP_TOTAL, TotalScore.ToString());
            PlayerPrefs.SetString(PP_MAX, MaxScore.ToString());
            PlayerPrefs.Save();
        }

        if (hudPanel) hudPanel.SetActive(false);
        if (gameOverAdPanel) gameOverAdPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(true);

        AudioManager.I?.PlayLayered(SfxId.GameOverClose, SfxId.GameOverHope);

        // Game ended: do not keep persistent board for this mode
        ClearRuntimeStateForCurrentMode();
        ClearPersistentStateForCurrentMode();

        gameOverSnapshotState = null;
        gameOverAdOfferActive = false;
        gameOverAdRemaining = 0f;

        UpdateUI();
    }

    private void ShowGameOverAdPanel()
    {
        if (hudPanel) hudPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        gameOverAdPanel.SetActive(true);
        gameOverAdOfferActive = true;

        gameOverAdRemaining = Mathf.Max(0.1f, gameOverAdOfferSeconds);

        if (gameOverAdCloseButton)
            gameOverAdCloseButton.interactable = true;

        if (gameOverAdWatchAdButton)
            gameOverAdWatchAdButton.interactable = true;

        UpdateGameOverAdOfferUI();
    }

    private void UpdateGameOverAdOfferUI()
    {
        if (!gameOverAdOfferActive) return;

        float total = Mathf.Max(0.001f, gameOverAdOfferSeconds);
        float t = Mathf.Clamp(gameOverAdRemaining, 0f, total);

        if (gameOverAdTimeFill)
            gameOverAdTimeFill.fillAmount = t / total;

        if (gameOverAdTimeText)
            gameOverAdTimeText.text = $"{Mathf.CeilToInt(t)}";
    }

    private void OnGameOverAdClosePressed()
    {
        if (!gameOverAdOfferActive) return;

        HideGameOverAdPanel();
        ConfirmGameOverAndShowPanel();
    }

    private void OnGameOverAdWatchAdPressed()
    {
        if (!gameOverAdOfferActive) return;

        // Placeholder hook for rewarded ads.
        // Integrate your ad SDK here and call ContinueAfterRewardedAd() on reward callback.
#if UNITY_EDITOR
        ContinueAfterRewardedAd();
#else
        Debug.Log("Rewarded ad hook: integrate your ad SDK, then continue after reward.");
#endif
    }

    private void ContinueAfterRewardedAd()
    {
        HideGameOverAdPanel();

        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null) return;

        // Restore the snapshot first (this should clear IsGameOver inside the board if it's stored in state).
        if (gameOverSnapshotState != null)
        {
            board.ImportState(gameOverSnapshotState);

            if (CurrentPlayType == PlayType.Solo)
            {
                Score = gameOverSnapshotSoloScore;
            }
            else
            {
                player1Score = gameOverSnapshotP1Score;
                player2Score = gameOverSnapshotP2Score;
            }

            PlayerHasMoved = gameOverSnapshotPlayerHasMoved;
        }

        // Ensure the board is in playing state and then shuffle once.
        board.ResumeGame(CurrentPlayType);
        board.TryShuffle();

        // Show HUD again and persist the continued run.
        if (hudPanel) hudPanel.SetActive(true);

        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();

        UpdateUI();
    }

    private void LoadMetaScores
()
    {
        if (!long.TryParse(PlayerPrefs.GetString(PP_TOTAL, "0"), out long total)) total = 0;
        if (!long.TryParse(PlayerPrefs.GetString(PP_MAX, "0"), out long max)) max = 0;
        TotalScore = total;
        MaxScore = max;
    }

    private void SaveRuntimeStateForCurrentMode()
    {
        if (board == null) return;

        if (CurrentPlayType == PlayType.Solo)
        {
            soloBoardState = board.ExportState();
            soloScore = Score;
            soloPlayerHasMoved = PlayerHasMoved;
            soloHasState = (soloBoardState != null);
        }
        else
        {
            versusBoardState = board.ExportState();
            versusP1Score = player1Score;
            versusP2Score = player2Score;
            versusPlayerHasMoved = PlayerHasMoved;
            versusHasState = (versusBoardState != null);
        }
    }

    private void RestoreRuntimeStateForCurrentMode()
    {
        if (board == null) return;

        if (CurrentPlayType == PlayType.Solo)
        {
            if (soloHasState && soloBoardState != null)
            {
                board.ImportState(soloBoardState);
                Score = soloScore;
                PlayerHasMoved = soloPlayerHasMoved;
            }
        }
        else
        {
            if (versusHasState && versusBoardState != null)
            {
                board.ImportState(versusBoardState);
                player1Score = versusP1Score;
                player2Score = versusP2Score;
                PlayerHasMoved = versusPlayerHasMoved;
            }
        }
    }

    private void ClearRuntimeStateForCurrentMode()
    {
        if (CurrentPlayType == PlayType.Solo)
        {
            soloBoardState = null;
            soloHasState = false;
            soloScore = 0;
            soloPlayerHasMoved = false;
        }
        else
        {
            versusBoardState = null;
            versusHasState = false;
            versusP1Score = 0;
            versusP2Score = 0;
            versusPlayerHasMoved = false;
        }
    }

    // --------------------------
    // Persistent state (PlayerPrefs JSON)
    // --------------------------

    private void LoadPersistentBoardStates()
    {
        if (PlayerPrefs.HasKey(PP_SOLO_STATE))
        {
            string json = PlayerPrefs.GetString(PP_SOLO_STATE, "");
            if (!string.IsNullOrEmpty(json))
            {
                soloBoardState = JsonUtility.FromJson<BoardController.BoardState>(json);
                soloHasState = (soloBoardState != null);
                soloScore = soloBoardState != null ? soloBoardState.soloScore : 0;
                soloPlayerHasMoved = soloHasState;
            }
        }

        if (PlayerPrefs.HasKey(PP_VERSUS_STATE))
        {
            string json = PlayerPrefs.GetString(PP_VERSUS_STATE, "");
            if (!string.IsNullOrEmpty(json))
            {
                versusBoardState = JsonUtility.FromJson<BoardController.BoardState>(json);
                versusHasState = (versusBoardState != null);
                versusP1Score = versusBoardState != null ? versusBoardState.p1Score : 0;
                versusP2Score = versusBoardState != null ? versusBoardState.p2Score : 0;
                versusPlayerHasMoved = versusHasState;
            }
        }
    }

    private void SavePersistentStateForCurrentMode()
    {
        if (board == null) return;
        if (board.IsGameOver) return;

        // Always export fresh before persisting, so board + score are consistent
        var state = board.ExportState();
        if (state == null) return;

        string json = JsonUtility.ToJson(state);

        if (CurrentPlayType == PlayType.Solo) PlayerPrefs.SetString(PP_SOLO_STATE, json);
        else PlayerPrefs.SetString(PP_VERSUS_STATE, json);

        PlayerPrefs.Save();
    }

    private void ClearPersistentStateForCurrentMode()
    {
        if (CurrentPlayType == PlayType.Solo) PlayerPrefs.DeleteKey(PP_SOLO_STATE);
        else PlayerPrefs.DeleteKey(PP_VERSUS_STATE);

        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;

        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        if (board != null && gameBoardRoot != null && gameBoardRoot.activeInHierarchy && !board.IsGameOver)
        {
            SaveRuntimeStateForCurrentMode();
            SavePersistentStateForCurrentMode();
        }

        PersistCredits();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        // When returning to the app, apply offline regen immediately
        RefreshTimedCredits();
        UpdateUI();
    }

    private void OnApplicationQuit()
    {
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        if (board != null && gameBoardRoot != null && gameBoardRoot.activeInHierarchy && !board.IsGameOver)
        {
            SaveRuntimeStateForCurrentMode();
            SavePersistentStateForCurrentMode();
        }

        PersistCredits();
    }

    // --------------------------
    // Credits regen + UI
    // --------------------------

    private void PersistCredits()
    {
        PlayerPrefs.SetInt(PP_UNDO, UndoCredits);
        PlayerPrefs.SetInt(PP_SHUFFLE, ShuffleCredits);

        if (!PlayerPrefs.HasKey(PP_LAST_GRANT_UTC))
            PlayerPrefs.SetString(PP_LAST_GRANT_UTC, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        PlayerPrefs.Save();
    }

    private void RefreshTimedCredits()
    {
        int intervalSeconds = Mathf.Max(1, creditRegenMinutes) * 60;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long last = now;

        if (long.TryParse(PlayerPrefs.GetString(PP_LAST_GRANT_UTC, now.ToString()), out long parsed))
            last = parsed;

        long elapsed = Mathf.Max(0, (int)(now - last));
        long ticks = elapsed / intervalSeconds;

        if (ticks <= 0) return;

        if (!unlimitedUndoForTesting)
            UndoCredits = AddWithOptionalCap(UndoCredits, (int)ticks);

        if (!unlimitedShuffleForTesting)
            ShuffleCredits = AddWithOptionalCap(ShuffleCredits, (int)ticks);

        long newLast = last + (ticks * intervalSeconds);
        PlayerPrefs.SetString(PP_LAST_GRANT_UTC, newLast.ToString());
        PersistCredits();
    }

    private int AddWithOptionalCap(int current, int add)
    {
        if (add <= 0) return current;

        long v = (long)current + add;

        if (maxCreditsCap > 0)
            v = Mathf.Min((int)v, maxCreditsCap);

        return (int)v;
    }

    private TimeSpan GetTimeUntilNextCredit()
    {
        int intervalSeconds = Mathf.Max(1, creditRegenMinutes) * 60;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long last = now;

        if (long.TryParse(PlayerPrefs.GetString(PP_LAST_GRANT_UTC, now.ToString()), out long parsed))
            last = parsed;

        long elapsed = Mathf.Max(0, (int)(now - last));
        long rem = intervalSeconds - (elapsed % intervalSeconds);

        if (rem == intervalSeconds) rem = 0;

        return TimeSpan.FromSeconds(rem);
    }

    private void ShowLimitedCreditsPanel(CreditType type)
    {
        lastRequestedCreditType = type;

        if (!limitedCreditsPanel)
        {
            Debug.LogWarning("LimitedCreditsPanel is not assigned on GameManager.");
            return;
        }

        limitedCreditsPanel.SetActive(true);
        UpdateLimitedCreditsPanelText();

        // Ad button is prepared as a hook; actual ad integration can be added later.
        if (limitedCreditsWatchAdButton)
            limitedCreditsWatchAdButton.interactable = true;
    }

    private void HideLimitedCreditsPanel()
    {
        if (limitedCreditsPanel)
            limitedCreditsPanel.SetActive(false);
    }


    private void HookGameOverAdPanelButtons()
    {
        if (gameOverAdWatchAdButton)
        {
            gameOverAdWatchAdButton.onClick.RemoveListener(OnGameOverAdWatchAdPressed);
            gameOverAdWatchAdButton.onClick.AddListener(OnGameOverAdWatchAdPressed);
        }
        if (gameOverAdCloseButton)
        {
            gameOverAdCloseButton.onClick.RemoveListener(OnGameOverAdClosePressed);
            gameOverAdCloseButton.onClick.AddListener(OnGameOverAdClosePressed);
        }
    }

    private void EnsureGameOverAdPanelUnderSafeArea()
    {
        if (gameOverAdPanel == null) return;

        RectTransform panelRt = gameOverAdPanel.GetComponent<RectTransform>();
        if (panelRt == null) return;

        SafeAreaFitter safeArea = null;
#if UNITY_2023_1_OR_NEWER
        safeArea = UnityEngine.Object.FindFirstObjectByType<SafeAreaFitter>();
#else
        safeArea = FindObjectOfType<SafeAreaFitter>();
#endif
        if (safeArea == null)
        {
            SafeAreaFitter[] all = Resources.FindObjectsOfTypeAll<SafeAreaFitter>();
            if (all != null && all.Length > 0) safeArea = all[0];
        }
        if (safeArea == null) return;

        Transform safeParent = safeArea.transform;
        if (panelRt.parent != safeParent)
        {
            panelRt.SetParent(safeParent, false);
        }

        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
    }

    private void HideGameOverAdPanel()
    {
        if (gameOverAdPanel)
            gameOverAdPanel.SetActive(false);

        gameOverAdOfferActive = false;
        gameOverAdRemaining = 0f;

        if (gameOverAdTimeFill)
            gameOverAdTimeFill.fillAmount = 1f;

        if (gameOverAdTimeText)
            gameOverAdTimeText.text = "";
    }

    private void UpdateLimitedCreditsPanelText()
    {
        if (!limitedCreditsInfoText) return;

        TimeSpan t = GetTimeUntilNextCredit();
        string mmss = string.Format("{0:D2}:{1:D2}", (int)t.TotalMinutes, t.Seconds);

        string label = lastRequestedCreditType == CreditType.Undo ? "Undo" : "Shuffle";
        limitedCreditsInfoText.text = $"{label} hakkın bitti. Yeni hak için kalan süre: {mmss}";
    }

    private void OnLimitedCreditsWatchAdPressed()
    {
        // Placeholder hook for rewarded ads.
        // Integrate your ad SDK here and call GrantOneCredit(lastRequestedCreditType) on reward callback.
#if UNITY_EDITOR
        GrantOneCredit(lastRequestedCreditType);
#else
        Debug.Log("Rewarded ad hook: integrate your ad SDK, then grant credits on reward.");
#endif
    }

    private void GrantOneCredit(CreditType type)
    {
        if (type == CreditType.Undo)
        {
            if (!unlimitedUndoForTesting)
                UndoCredits = AddWithOptionalCap(UndoCredits, 1);
        }
        else
        {
            if (!unlimitedShuffleForTesting)
                ShuffleCredits = AddWithOptionalCap(ShuffleCredits, 1);
        }

        PersistCredits();
        HideLimitedCreditsPanel();
        UpdateUI();
    }

    // --------------------------
    // UI
    // --------------------------

    private void UpdateUI()
    {
        RefreshTimedCredits();

        if (CurrentPlayType == PlayType.Solo)
        {
            if (scoreText) scoreText.text = $"Score: {Score}";
        }
        else
        {
            if (player1ScoreText) player1ScoreText.text = $"Player 1: {player1Score}";
            if (player2ScoreText) player2ScoreText.text = $"Player 2: {player2Score}";
        }

        if (undoText)
            undoText.text = unlimitedUndoForTesting ? "Undo: ∞" : $"Undo: {UndoCredits}";

        if (shuffleText)
            shuffleText.text = unlimitedShuffleForTesting ? "Shuffle: ∞" : $"Shuffle: {ShuffleCredits}";

        if (totalScoreText) totalScoreText.text = $"Total Score: {TotalScore}";
        if (maxScoreText) maxScoreText.text = $"Max Score: {MaxScore}";

        if (gameOverScoreText)
        {
            if (CurrentPlayType == PlayType.Versus1v1) gameOverScoreText.text = $"P1: {player1Score} | P2: {player2Score}";
            else gameOverScoreText.text = $"Your Score: {lastRunScore}";
        }

        if (gameOverMaxScoreText)
        {
            if (CurrentPlayType == PlayType.Solo)
            {
                gameOverMaxScoreText.gameObject.SetActive(true);
                gameOverMaxScoreText.text = $"Max Score: {MaxScore}";
            }
            else
            {
                gameOverMaxScoreText.gameObject.SetActive(false);
            }
        }

        if (winnerText)
        {
            if (CurrentPlayType == PlayType.Versus1v1)
            {
                if (player1Score > player2Score) winnerText.text = "Winner: Player 1";
                else if (player2Score > player1Score) winnerText.text = "Winner: Player 2";
                else winnerText.text = "Draw!";
            }
            else
            {
                winnerText.text = "";
            }
        }
    }

    private void Update()
    {
        if (board == null) return;


        // GameOver ad offer countdown (unscaled)
        if (gameOverAdOfferActive)
        {
            gameOverAdRemaining -= Time.unscaledDeltaTime;
            if (gameOverAdRemaining <= 0f)
            {
                HideGameOverAdPanel();
                ConfirmGameOverAndShowPanel();
                return;
            }

            UpdateGameOverAdOfferUI();
        }

        // Periodically apply regen + refresh texts (unscaled so it still ticks in pause menus)
        if (Time.unscaledTime >= nextCreditTick)
        {
            nextCreditTick = Time.unscaledTime + 0.5f;
            RefreshTimedCredits();

            if (limitedCreditsPanel && limitedCreditsPanel.activeSelf)
                UpdateLimitedCreditsPanelText();

            // Update texts for new credits
            if (!unlimitedUndoForTesting && undoText) undoText.text = $"Undo: {UndoCredits}";
            if (!unlimitedShuffleForTesting && shuffleText) shuffleText.text = $"Shuffle: {ShuffleCredits}";
        }

        // Undo (Solo only)
        if (undoButton)
        {
            bool canPress = (CurrentPlayType == PlayType.Solo) && PlayerHasMoved && !board.IsBusy && !board.IsGameOver;
            // Keep interactable even with 0 credits, so we can show the panel on click.
            undoButton.interactable = canPress;
        }

        // Shuffle (Solo only)
        if (shuffleButton)
        {
            bool canPress = (CurrentPlayType == PlayType.Solo) && !board.IsBusy && !board.IsGameOver;
            // Keep interactable even with 0 credits, so we can show the panel on click.
            shuffleButton.interactable = canPress;
        }
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (hudPanel) hudPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateUI();
    }
}
