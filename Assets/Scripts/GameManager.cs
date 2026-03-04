using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public enum PlayType { Solo, Versus1v1 }
    public PlayType CurrentPlayType { get; private set; } = PlayType.Solo;

    [Header("Scene refs")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameBoardRoot;

    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text player1ScoreText;
    [SerializeField] private TMP_Text player2ScoreText;

    [Header("Buttons")]
    [SerializeField] private Button undoButton;
    [SerializeField] private TMP_Text undoText;
    [SerializeField] private Button shuffleButton;
    [SerializeField] private TMP_Text shuffleText;

    [Header("Winner UI")]
    [SerializeField] private TMP_Text winnerText;

    [Header("Credits")]
    public int startingUndoCredits = 3;
    public int startingShuffleCredits = 3;
    [Tooltip("Credits regen every N minutes")]
    public int creditsRegenMinutes = 30;
    [Tooltip("Max credits cap. Set to 0 for no cap.")]
    public int maxCreditsCap = 0;

    [Header("Testing")]
    public bool unlimitedUndoForTesting = false;
    public bool unlimitedShuffleForTesting = false;

    // Internal refs
    private BoardController board;

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

        RefreshTimedCredits();
        LoadPersistentBoardStates();

        ShowMainMenu();

        if (winnerText) winnerText.text = "";

        UpdateUI();
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

    public void NotifyBoardStable()
    {
        // Called by BoardController when a background fall/resolve finishes.
        // Persist only if the game is not over.
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board != null && !board.IsGameOver)
        {
            SaveRuntimeStateForCurrentMode();
            SavePersistentStateForCurrentMode();
        }
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

        if (board != null)
        {
            bool ended = board.IsGameOver;

            if (ended)
            {
                ClearRuntimeStateForCurrentMode();
                ClearPersistentStateForCurrentMode();
            }
            else
            {
                // Only save when stable; if busy, keep previous save intact
                if (!board.IsBusy)
                {
                    SaveRuntimeStateForCurrentMode();
                    SavePersistentStateForCurrentMode();
                }
            }
        }

        // Keep the board active so falls/resolves can continue in the background.
        if (board != null) board.OnExitToMainMenu();

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
        if (board == null) return;

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

            SaveRuntimeStateForCurrentMode();
            SavePersistentStateForCurrentMode();
        }
        else
        {
            RestoreRuntimeStateForCurrentMode();
            board.ResumeGame(CurrentPlayType);
        }

        board.OnEnterGameMode(CurrentPlayType);
        UpdateUI();
    }

    private void ForceNewGame()
    {
        if (gameBoardRoot) gameBoardRoot.SetActive(true);
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null) return;

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
        board.OnEnterGameMode(CurrentPlayType);

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

        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();
        UpdateUI();
    }

    public void MarkPlayerMoved() => PlayerHasMoved = true;
    public void SetScore(long v) { Score = v; UpdateUI(); }
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
        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();
    }

    public void GameOver()
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
        if (gameOverPanel) gameOverPanel.SetActive(true);

        AudioManager.I?.PlayLayered(SfxId.GameOverClose, SfxId.GameOverHope);

        ClearRuntimeStateForCurrentMode();
        ClearPersistentStateForCurrentMode();

        UpdateUI();
    }

    public void ShowMainMenu()
    {
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board != null) board.OnExitToMainMenu();

        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (hudPanel) hudPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateUI();
    }

    // -----------------------------------------------------
    // State save/load (unchanged logic)
    // -----------------------------------------------------
    private void SaveRuntimeStateForCurrentMode()
    {
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null) return;

        if (CurrentPlayType == PlayType.Solo)
        {
            soloBoardState = board.ExportState();
            if (!IsStateComplete(soloBoardState))
            {
                soloBoardState = null;
                soloHasState = false;
                return;
            }
            soloScore = Score;
            soloPlayerHasMoved = PlayerHasMoved;
            soloHasState = (soloBoardState != null);
        }
        else
        {
            versusBoardState = board.ExportState();
            if (!IsStateComplete(versusBoardState))
            {
                versusBoardState = null;
                versusHasState = false;
                return;
            }
            versusP1Score = player1Score;
            versusP2Score = player2Score;
            versusPlayerHasMoved = PlayerHasMoved;
            versusHasState = (versusBoardState != null);
        }
    }

    private void RestoreRuntimeStateForCurrentMode()
    {
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null) return;

        if (CurrentPlayType == PlayType.Solo)
        {
            if (soloBoardState != null) board.ImportState(soloBoardState);
            Score = soloScore;
            PlayerHasMoved = soloPlayerHasMoved;
        }
        else
        {
            if (versusBoardState != null) board.ImportState(versusBoardState);
            player1Score = versusP1Score;
            player2Score = versusP2Score;
            PlayerHasMoved = versusPlayerHasMoved;
        }
    }

    private void ClearRuntimeStateForCurrentMode()
    {
        if (CurrentPlayType == PlayType.Solo)
        {
            soloBoardState = null;
            soloScore = 0;
            soloPlayerHasMoved = false;
            soloHasState = false;
        }
        else
        {
            versusBoardState = null;
            versusP1Score = 0;
            versusP2Score = 0;
            versusPlayerHasMoved = false;
            versusHasState = false;
        }
    }

    private void SavePersistentStateForCurrentMode()
    {
        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null) return;

        string key = (CurrentPlayType == PlayType.Solo) ? PP_SOLO_STATE : PP_VERSUS_STATE;

        var s = board.ExportState();
        if (s == null) return;
        if (!IsStateComplete(s)) return;

        string json = JsonUtility.ToJson(s);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    private void ClearPersistentStateForCurrentMode()
    {
        string key = (CurrentPlayType == PlayType.Solo) ? PP_SOLO_STATE : PP_VERSUS_STATE;
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    private void LoadPersistentBoardStates()
    {
        soloHasState = false;
        versusHasState = false;

        if (PlayerPrefs.HasKey(PP_SOLO_STATE))
        {
            string json = PlayerPrefs.GetString(PP_SOLO_STATE, "");
            if (!string.IsNullOrEmpty(json))
            {
                soloBoardState = JsonUtility.FromJson<BoardController.BoardState>(json);
                if (soloBoardState != null && !IsStateComplete(soloBoardState))
                {
                    soloBoardState = null;
                    soloHasState = false;
                    PlayerPrefs.DeleteKey(PP_SOLO_STATE);
                }
                soloHasState = (soloBoardState != null);
                if (soloBoardState != null) soloScore = soloBoardState.soloScore;
            }
        }

        if (PlayerPrefs.HasKey(PP_VERSUS_STATE))
        {
            string json = PlayerPrefs.GetString(PP_VERSUS_STATE, "");
            if (!string.IsNullOrEmpty(json))
            {
                versusBoardState = JsonUtility.FromJson<BoardController.BoardState>(json);
                if (versusBoardState != null && !IsStateComplete(versusBoardState))
                {
                    versusBoardState = null;
                    versusHasState = false;
                    PlayerPrefs.DeleteKey(PP_VERSUS_STATE);
                }
                versusHasState = (versusBoardState != null);
                if (versusBoardState != null)
                {
                    versusP1Score = versusBoardState.p1Score;
                    versusP2Score = versusBoardState.p2Score;
                }
            }
        }
    }

    private void LoadMetaScores()
    {
        string totalStr = PlayerPrefs.GetString(PP_TOTAL, "0");
        string maxStr = PlayerPrefs.GetString(PP_MAX, "0");

        if (!long.TryParse(totalStr, out long total)) total = 0;
        if (!long.TryParse(maxStr, out long max)) max = 0;

        TotalScore = total;
        MaxScore = max;
    }

    private void PersistCredits()
    {
        PlayerPrefs.SetInt(PP_UNDO, UndoCredits);
        PlayerPrefs.SetInt(PP_SHUFFLE, ShuffleCredits);
        PlayerPrefs.Save();
    }

    private void RefreshTimedCredits()
    {
        if (creditsRegenMinutes <= 0) return;

        DateTime nowUtc = DateTime.UtcNow;
        string lastUtcStr = PlayerPrefs.GetString(PP_LAST_GRANT_UTC, "");
        DateTime lastUtc = nowUtc;

        if (!string.IsNullOrEmpty(lastUtcStr))
        {
            DateTime.TryParse(lastUtcStr, out lastUtc);
        }

        double minutes = (nowUtc - lastUtc).TotalMinutes;
        if (minutes < creditsRegenMinutes) return;

        int ticks = Mathf.FloorToInt((float)(minutes / creditsRegenMinutes));
        if (ticks <= 0) return;

        int addUndo = ticks;
        int addShuffle = ticks;

        UndoCredits += addUndo;
        ShuffleCredits += addShuffle;

        if (maxCreditsCap > 0)
        {
            UndoCredits = Mathf.Min(UndoCredits, maxCreditsCap);
            ShuffleCredits = Mathf.Min(ShuffleCredits, maxCreditsCap);
        }

        PlayerPrefs.SetString(PP_LAST_GRANT_UTC, nowUtc.ToString("o"));
        PersistCredits();
    }

    private void ShowLimitedCreditsPanel(CreditType t)
    {
        lastRequestedCreditType = t;
        // Panel logic in your scene
    }

    public void UpdateUI()
    {
        if (scoreText) scoreText.text = Score.ToString();

        if (player1ScoreText) player1ScoreText.text = player1Score.ToString();
        if (player2ScoreText) player2ScoreText.text = player2Score.ToString();

        if (board == null) board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);
        if (board == null) return;

        // Undo (Solo only)
        if (undoButton)
        {
            bool canPress = (CurrentPlayType == PlayType.Solo) && PlayerHasMoved && !board.IsBusy && !board.IsGameOver;
            undoButton.interactable = canPress;
        }

        // Shuffle (Solo only)
        if (shuffleButton)
        {
            bool canPress = (CurrentPlayType == PlayType.Solo) && !board.IsBusy && !board.IsGameOver;
            shuffleButton.interactable = canPress;
        }
    }

    private bool IsStateComplete(BoardController.BoardState s)
    {
        if (s == null) return false;
        if (s.values == null) return false;
        if (s.w <= 0 || s.h <= 0) return false;
        if (s.values.Length != s.w * s.h) return false;

        for (int i = 0; i < s.values.Length; i++)
        {
            if (s.values[i] <= 0) return false;
        }

        return true;
    }

    public void NotifyBoardBecameStable()
    {
        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();
        UpdateUI();
    }
}