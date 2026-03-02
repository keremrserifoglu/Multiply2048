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

    private long lastRunScore;

    public bool unlimitedUndoForTesting = true;

    public long Score { get; private set; }
    public long TotalScore { get; private set; }
    public long MaxScore { get; private set; }
    public int UndoCredits { get; private set; }

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

    private const string PP_TOTAL = "TOTAL_SCORE_STR";
    private const string PP_MAX = "MAX_SCORE_STR";
    private const string PP_UNDO = "UNDO_CREDITS";

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

        // Load persisted board states (if any) into memory
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

    public void RestartSameMode()
    {
        ForceNewGame();
    }

    public void PlayAgain()
    {
        ForceNewGame();
    }

    public void ReturnToMainMenu()
    {
        if (board == null)
            board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

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

        if (board == null)
            board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        board?.TryShuffle();

        // Save after shuffle so it persists across app close
        SaveRuntimeStateForCurrentMode();
        SavePersistentStateForCurrentMode();
    }

    public void UndoPressed()
    {
        // Must have made a move first
        if (!PlayerHasMoved) return;

        if (board == null)
            board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        if (board == null) return;

        if (!unlimitedUndoForTesting && UndoCredits <= 0)
            return;

        bool ok = board.TryUndoLastMove();
        if (!ok) return;

        if (!unlimitedUndoForTesting)
        {
            UndoCredits--;
            PlayerPrefs.SetInt(PP_UNDO, UndoCredits);
            PlayerPrefs.Save();
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

        if (board == null)
            board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

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

            // Persist fresh state as well (optional, but keeps consistent)
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

        if (board == null)
            board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

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

    public void MarkPlayerMoved()
    {
        PlayerHasMoved = true;
    }

    public void SetScore(long v)
    {
        Score = v;
        UpdateUI();
    }

    public void SetPlayerHasMoved(bool v)
    {
        PlayerHasMoved = v;
    }

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
        long runScore = (CurrentPlayType == PlayType.Versus1v1)
            ? (player1Score + player2Score)
            : Score;

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

        // Game ended: do not keep persistent board for this mode
        ClearRuntimeStateForCurrentMode();
        ClearPersistentStateForCurrentMode();

        UpdateUI();
    }

    private void LoadMetaScores()
    {
        if (!long.TryParse(PlayerPrefs.GetString(PP_TOTAL, "0"), out long total))
            total = 0;

        if (!long.TryParse(PlayerPrefs.GetString(PP_MAX, "0"), out long max))
            max = 0;

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
                soloPlayerHasMoved = soloHasState; // safe default
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
                versusPlayerHasMoved = versusHasState; // safe default
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

        if (CurrentPlayType == PlayType.Solo)
        {
            PlayerPrefs.SetString(PP_SOLO_STATE, json);
        }
        else
        {
            PlayerPrefs.SetString(PP_VERSUS_STATE, json);
        }

        PlayerPrefs.Save();
    }

    private void ClearPersistentStateForCurrentMode()
    {
        if (CurrentPlayType == PlayType.Solo)
        {
            PlayerPrefs.DeleteKey(PP_SOLO_STATE);
        }
        else
        {
            PlayerPrefs.DeleteKey(PP_VERSUS_STATE);
        }

        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pause)
    {
        // Save when app goes to background
        if (pause)
        {
            if (board == null)
                board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

            if (board != null && gameBoardRoot != null && gameBoardRoot.activeInHierarchy && !board.IsGameOver)
            {
                SaveRuntimeStateForCurrentMode();
                SavePersistentStateForCurrentMode();
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Save on quit
        if (board == null)
            board = FindFirstObjectByType<BoardController>(FindObjectsInactive.Include);

        if (board != null && gameBoardRoot != null && gameBoardRoot.activeInHierarchy && !board.IsGameOver)
        {
            SaveRuntimeStateForCurrentMode();
            SavePersistentStateForCurrentMode();
        }
    }

    // --------------------------
    // UI
    // --------------------------
    private void UpdateUI()
    {
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
            undoText.text = unlimitedUndoForTesting
                ? "Undo: ∞"
                : $"Undo: {UndoCredits}";

        if (totalScoreText) totalScoreText.text = $"Total Score: {TotalScore}";
        if (maxScoreText) maxScoreText.text = $"Max Score: {MaxScore}";

        if (gameOverScoreText)
        {
            if (CurrentPlayType == PlayType.Versus1v1)
                gameOverScoreText.text = $"P1: {player1Score}  |  P2: {player2Score}";
            else
                gameOverScoreText.text = $"Your Score: {lastRunScore}";
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

        // Undo (Solo only)
        if (undoButton)
        {
            bool hasCredits = unlimitedUndoForTesting || UndoCredits > 0;
            undoButton.interactable =
                (CurrentPlayType == PlayType.Solo) &&
                hasCredits &&
                PlayerHasMoved &&
                !board.IsBusy &&
                !board.IsGameOver;
        }

        // Shuffle (Solo only)
        if (shuffleButton)
        {
            shuffleButton.interactable =
                (CurrentPlayType == PlayType.Solo) &&
                !board.IsBusy &&
                !board.IsGameOver;
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